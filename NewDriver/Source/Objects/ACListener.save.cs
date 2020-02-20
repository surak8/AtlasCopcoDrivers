using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using OpenProtocolInterpreter.MIDs;
#if KENNY
using OpenProtocolInterpreter.MIDs.KeepAlive;
#else
using OpenProtocolInterpreter;
using OpenProtocolInterpreter.Communication;
using OpenProtocolInterpreter.Job;
using OpenProtocolInterpreter.KeepAlive;
using OpenProtocolInterpreter.ParameterSet;
#endif

namespace NSNewDriver {
	class ACListener<M> {
#region fields
		TcpClient _tcpClient;
		NetworkStream _stream;
		Task _taskReceive;

		[Obsolete("remove this",true)]
		DispatcherTimer _keepAliveTimer;

		static  IColtLogger _logger;
		readonly object _streamLock=new object();
		ManualResetEvent _mre=new ManualResetEvent(true);
		bool _commStarted =false;
#if KENNY
#else
		readonly MidInterpreter mi=new MidInterpreter();
#endif
		byte[] readData=new byte[4096];
		private DateTime _lastKeepAlive;

		delegate bool MidHandler(Mid m, string package);
#endregion fields

#region ctor
		public ACListener(string addr, int nport) {
			this.ipAddress = addr;
			this.portNumber = nport;
			_logger = new ColtLogger();
		}

#endregion

#region properties
		public string ipAddress { get; private set; }
		public int portNumber { get; private set; }


#endregion

#region methods
		internal void start() {
			_tcpClient = new TcpClient(ipAddress, portNumber);
			if (_tcpClient.Connected) {
				_stream = _tcpClient.GetStream();
				_taskReceive = Task.Run(() => { receiveThread(); });
				//setupKeepAliveTimer();
			}
		}

		[Obsolete("remove this", true)]
		void setupKeepAliveTimer() {
			_keepAliveTimer = new DispatcherTimer {
				Interval = new TimeSpan(0, 0, 10),
			};
			_keepAliveTimer.Tick += sendKeepAliveMessage;
		}

		void sendKeepAliveMessage(object sender, EventArgs e) {
			//_logger.log(MethodBase.GetCurrentMethod());
#if KENNY
			this.sendMessage(new MID_9999(), new MidHandler(handleKeepAlive));
#else
			this.sendMessage(new Mid9999(), new MidHandler(handleKeepAlive));
#endif
		}

//		#region mid-handling messages
//#if KENNY

//		bool handleCommunicationStart(MID m, string package) {
//#else
			bool handleCommunicationStart(Mid m, string package) {
//#endif
				int midNo;
			bool ret=false;
			StringBuilder sb;

			switch (midNo = m.HeaderData.Mid) {
				case 2:
					Mid0002 m2=(Mid0002) m;

					if (_showControllerResponse) {
						using (StringWriter sw = new StringWriter(sb = new StringBuilder())) {
							writeCSVProperties<Mid>(m2.GetType().GetProperties(), m2, sw);
						}
						_logger.log(sb.ToString());
					}
					_commStarted = true;
					ret = true;
					break;
				case 4:
					Mid0004 m4=(Mid0004) m;
					Trace.WriteLine("Command error: MID=" + m4.FailedMid + ", Error=" + m4.ErrorCode + ".");
					break;
				default:
					_logger.log(MethodBase.GetCurrentMethod(), "unhandled mid " + midNo);
					break;
			}
			return ret;
		}
		bool sendMessage(Mid mid, MidHandler mh) {
			byte[] data;
			string package,tmp,newPackage=null;
			Mid newMid=null;

			data = Encoding.ASCII.GetBytes(package = (tmp = mid.Pack()) + '\0');
			Trace.WriteLine("send [" + tmp.Replace('\0', '*') + ']');
			lock (_streamLock) {
				_stream.Write(data, 0, data.Length);
				newPackage = readMidStream();
			}
			if (!string.IsNullOrEmpty(newPackage)) {
				try {
					newMid = MidInterpreterMessagesExtensions.UseAllMessages(mi).Parse(newPackage);
				} catch (Exception ex) {
					_logger.log(MethodBase.GetCurrentMethod(), ex);
				}
				if (newMid != null) {
					if (mh != null) {
						return mh(newMid, newPackage);
					} else
						_logger.log(MethodBase.GetCurrentMethod(), "Unhandled Mid " + newMid.HeaderData.Mid);
				}
			}
			return false;
		}

		string readMidStream(bool doLog=false) {
			string ret=null;
			int nreadAtttempts=0,nbytesread ;
			bool dataFound=false;

			do {
				if (nreadAtttempts > 10)
					break;
				if (!_stream.DataAvailable) {
					nreadAtttempts++;
					Thread.Sleep(100);
					continue;
				}
				nbytesread = _stream.Read(readData, 0, readData.Length);
				ret = Encoding.ASCII.GetString(readData, 0, nbytesread);
				//_logger.log()
				Trace.WriteLine("received [" + ret.Replace('\0', '*') + "]");
				//Trace.WriteLine("received [" + ret  + "]");
				dataFound = true;
			} while (!dataFound);
			if (doLog)
			Trace.WriteLine("Attempts: " + nreadAtttempts + ", Found=" + dataFound + " " +
				(dataFound ? "[" + ret.Replace('\0', '*') + "]" : string.Empty) + ".");
			return ret;
		}
#endregion mid-handling messages

		Queue _midQueue=new Queue();
		private bool veryVerbose;
		readonly object _queueLock=new object();

#endregion methods

		void receiveThread() {
			bool exitLoop=false;
			string package;
			Mid qmid;

			while (!exitLoop) {
				if (!_commStarted) {
					this.sendMessage(new Mid0001(), new MidHandler(handleCommunicationStart));
					if (_commStarted) {
						//this._keepAliveTimer.Start();
						_lastKeepAlive = DateTime.Now.Add(new TimeSpan(0, 0, -20));
					}
				} else {
					if (_commStarted)
						sendKeepAliveIfRequired();

					if (_stream.DataAvailable) {
						lock (_streamLock) {
							package = readMidStream();
							_logger.log(MethodBase.GetCurrentMethod(), "read: [" + package.Substring(0, package.Length - 1));
						}
					} else {
						if (_midQueue != null) {
							qmid = null;

							lock (_queueLock) {
								if (_midQueue.Count > 0) {
									qmid = _midQueue.Dequeue() as Mid;
								}
							}
							if (qmid != null)
								addMidRequest(qmid);
						}
						Thread.Sleep(100);
					}
				}
			}
			//}
		}

		void addMidRequest(Mid qmid) {
			Trace.WriteLine("Add " + qmid.GetType().Name + " [" + qmid.Pack().Replace('\0', '*'));
			if (qmid.HeaderData.Mid==10)
				sendMessage(qmid, new MidHandler(handlePSetList));
			else
			sendMessage(qmid, null);
		}

		  bool handlePSetList(Mid m, string package) {
			Trace.WriteLine("here");
			return true;
		}

		internal void enqueueMid(Mid amid) {
			lock (this._queueLock) {
				_midQueue.Enqueue(amid);
			}
		}

		internal void requestPSetIDs() {
			enqueueMid(new Mid0010());
		}
		internal void requestJobIDs() {
			enqueueMid(new Mid0030());
		}

		void sendKeepAliveIfRequired() {
			double tdiff;
			if ((tdiff = ((DateTime.Now - _lastKeepAlive).TotalSeconds)) > 10) {
				if (veryVerbose)
				Trace.WriteLine("TDiff=" + tdiff + ".");
				this.sendMessage(new Mid9999(), new MidHandler(handleKeepAlive));
			}
			//else
			//	Trace.WriteLine("KeepAlive=" + ((DateTime.Now - _lastKeepAlive).TotalSeconds) + "."); ;
		}
		bool handleKeepAlive(Mid m, string package) {

			if (veryVerbose)
			_logger.log(MethodBase.GetCurrentMethod(), "found: " + m.Pack().Replace('\0', '*'));
			if (m.HeaderData.Mid == 9999)
				_lastKeepAlive = DateTime.Now;
			return false;
		}

#region CSV-generation

		static readonly BindingFlags bfProps= BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.GetProperty;
		static readonly object[] nullArgs=new object[0];
		readonly bool _showControllerResponse=false;

		static void writeCSVProperties<V>(PropertyInfo[] pis, V mid, TextWriter tw) {
			int n=0;
			string dispValue;
			object propValue;

			tw.WriteLine();
			tw.WriteLine(mid.GetType().FullName);
			// write data-fields
			foreach (PropertyInfo pi in pis) {
				//if (n > 0)
				// extract the property.
				try {
					propValue = mid.GetType().InvokeMember(pi.Name, bfProps, null, mid, nullArgs);
					if (propValue == null)
						dispValue = "NULL";
					else {
						if (pi.PropertyType.Equals(typeof(int)) ||
							pi.PropertyType.Equals(typeof(decimal)) ||
							pi.PropertyType.Equals(typeof(bool)))
							dispValue = propValue.ToString();
						else if (pi.PropertyType.Equals(typeof(string))) dispValue = "\"" + ((string) propValue).Trim() + "\"";
						else if (pi.PropertyType.Equals(typeof(DateTime))) dispValue = "\"" + ((DateTime) propValue).ToString("dd-MMM-yy hh:mm:ss tt") + "\"";
						else {
							if (pi.PropertyType.IsEnum)
								dispValue = propValue.ToString();
							else {
								Trace.WriteLine(pi.PropertyType.FullName);
								dispValue = propValue.ToString();
								dispValue = "\"" + propValue.ToString() + "\"";
							}
						}
					}
					tw.WriteLine("\t" + pi.Name + " = " + dispValue);
					n++;
				} catch (Exception ex) {
					_logger.log(MethodBase.GetCurrentMethod(), ex);
					tw.WriteLine("error with:" + pi.Name + Environment.NewLine + "\ttype=" + pi.PropertyType.FullName);
				}
			}
			tw.WriteLine();
		}
#endregion

	}
}