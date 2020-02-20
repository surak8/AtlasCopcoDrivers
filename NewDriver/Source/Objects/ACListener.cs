using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Colt.Utility.Logging;
#if KENNY
#else
using OpenProtocolInterpreter;
using OpenProtocolInterpreter.Communication;
using OpenProtocolInterpreter.Job;
using OpenProtocolInterpreter.KeepAlive;
using OpenProtocolInterpreter.ParameterSet;
#endif

namespace NSNewDriver {
	partial class ACListener : IDisposable {
		delegate bool MidHandler(object m, string package);

		#region fields
		TcpClient _tcpClient;
		NetworkStream _stream;
		Task _taskReceive;

		[Obsolete("remove this",true)]
		DispatcherTimer _keepAliveTimer;

		readonly IColtLogger _logger;
		readonly DefaultColtLogger _dataLogger;
		readonly object _streamLock=new object();
		ManualResetEvent _mre=new ManualResetEvent(true);
		bool _commStarted =false;
		readonly IDictionary <int,MidHandler> _midResponseMap=new Dictionary<int, MidHandler>();
#if KENNY
#else
		readonly MidInterpreter mi=new MidInterpreter();
#endif
		byte[] readData=new byte[4096];
		DateTime _lastKeepAlive;
		Queue _midQueue=new Queue();
		bool veryVerbose=false;
		readonly object _queueLock=new object();
		readonly bool _showMidFields=true;

		#endregion fields

		#region ctor
		public ACListener(string addr, int nport) {
			Type aType;

			this.ipAddress = addr;
			this.portNumber = nport;
			//_logger = new ColtLogger();
			_logger = new DefaultColtLogger(DefaultColtLogger.defaultLogDirectory(), "listener");
			_dataLogger = new DefaultColtLogger(DefaultColtLogger.defaultLogDirectory(), "datalogger", false);

#if KENNY
			aType = typeof(OpenProtocolInterpreter.MIDs.MID);
			_midResponseMap.Add(31, new MidHandler(handleJobIDReply)); // reply to MID 30
			_midResponseMap.Add(15, new MidHandler(handleParmSetSeLected)); // reply to MID 30
			_midResponseMap.Add(5, new MidHandler(handleCmdAccepted)); //
			_midResponseMap.Add(4, new MidHandler(handleCmdError)); //
			_midResponseMap.Add(61, new MidHandler(handleTighteningResult)); // send 62 in response to 61
			_midResponseMap.Add(71, new MidHandler(handleAlarm)); //
			_midResponseMap.Add(76, new MidHandler(handleAlarmStatus)); //
			_midResponseMap.Add(22, new MidHandler(handleLockBatchUpload)); // send 23 in response to 22
			_midResponseMap.Add(211, new MidHandler(handleExtInputStatus)); // send 211 in response to 210
			_midResponseMap.Add(152, new MidHandler(handleMultiIdentResults)); // send 153 in response to 152
			_midResponseMap.Add(421, new MidHandler(handleDisableACCommands)); // send 422 in response to 421
#else
			aType = typeof(OpenProtocolInterpreter.Mid);
#endif
			if (!MidFinder.createMap(aType.Assembly.GetTypes(), "mid", "HeaderData", "Mid"))
				throw new InvalidOperationException("Mid-map is null/empty!");
		}


		internal void stop() {
			_logger.log(MethodBase.GetCurrentMethod());
			//_mre.Set();
			_mre.Reset();
			try {
				_taskReceive.Wait();
			} catch (Exception ex) {
				_logger.log(MethodBase.GetCurrentMethod(), ex);
			}
			//_mre.WaitOne();
			_logger.log(MethodBase.GetCurrentMethod());
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
			this.sendMessage(MidFinder.createNewMid(9999), new MidHandler(handleKeepAlive));
		}

		#region mid-handling messages

		bool sendMessage(object mid, MidHandler mh,bool awaitResponse=true) {
			byte[] data;
			string package,newPackage=null,msg,tmp;
			object newMid=null;
			int midNo,newMidNo;

			midNo = MidFinder.extractMidNumber(mid);

			data = Encoding.ASCII.GetBytes(package = MidFinder.extractMidContent(mid));
			tmp = MidFinder.fixupPackage(package);
			if (midNo != 9999) {
				//msg = "send [" + ((package.EndsWith("\0") ? package.Substring(0, package.Length - 1) : package)+"]");
				//_dataLogger.log(msg);
				writeToDataLog("send", tmp);
			}
			lock (_streamLock) {
				try {
					_stream.Write(data, 0, data.Length);
				} catch (IOException ioe) {
					_logger.log(MethodBase.GetCurrentMethod(), ioe);
					_commStarted = false;
					if (_tcpClient != null) {
						if (!_tcpClient.Connected) {
							if (_stream != null) {
								_stream.Close();
								_stream.Dispose();
								_stream = null;
							}
							_tcpClient.Close();
							_tcpClient.Dispose();
							_tcpClient = null;
						}
					}

				} catch (Exception ex) {
					_logger.log(MethodBase.GetCurrentMethod(), ex);

				}
				if (awaitResponse)
					newPackage = readMidStream();
			}
			if (!string.IsNullOrEmpty(newPackage)) {
				try {
					newMid = MidFinder.createMidInstance(newPackage);
				} catch (Exception ex) {
					_logger.log(MethodBase.GetCurrentMethod(), ex);
				}
				if (newMid != null) {
					newMidNo = MidFinder.extractMidNumber(newMid);
					if (mh != null) {
						//_logger.log(ColtLogLevel.Debug, MethodBase.GetCurrentMethod(), "have handler for mid " + newMid + ".");
						return mh(newMid, newPackage);
					} else {
						_logger.log(ColtLogLevel.Debug, MethodBase.GetCurrentMethod(), "check handling-map mid " + newMid + ".");
						if (_midResponseMap.ContainsKey(newMidNo))
							_midResponseMap[newMidNo](newMid, newPackage);
						else
							_logger.log(MethodBase.GetCurrentMethod(), "Unhandled Mid " + MidFinder.extractMidNumber(newMid) + ", response to Mid=" + midNo + ".");
					}
				}
			}
			return false;
		}

		string readMidStream(bool doLog = false) {
			string ret=null,tmp=null;
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
				tmp = MidFinder.fixupPackage(ret);
				if (MidFinder.findMidNumberFromPackage(ret) != 9999) {
					if (_showMidFields)
						showMidProperties(MidFinder.createMidInstance(ret), this._logger);
					this.writeToDataLog("read", ret);
				}

				dataFound = true;
			} while (!dataFound);
			if (doLog)
				Trace.WriteLine("Attempts: " + nreadAtttempts + ", Found=" + dataFound + " " +
					(dataFound ? "[" + tmp + "]" : string.Empty) + ".");
			return ret;
		}


		#endregion mid-handling messages
		void writeToDataLog(string v1, string v2) {
			this._dataLogger.log(v1 + " [" + MidFinder.fixupPackage(v2) + "]");
		}

		#endregion methods

		void receiveThread() {
			bool exitLoop=false;
			string package,tmp;
			object qmid;


			_logger.log(MethodBase.GetCurrentMethod(), "starts");
			//try {
			//	if (string.IsNullOrEmpty(Process.GetCurrentProcess().ProcessName))
			//		Process.GetCurrentProcess().ProcessName = "test";
			//} catch (Exception ex) {
			//	_logger.log(MethodBase.GetCurrentMethod(), ex);
			//}
			while (!exitLoop) {
				if (!_mre.WaitOne(100)) {
					_logger.log(MethodBase.GetCurrentMethod(), "DID waitone.");
					sendMessage(MidFinder.createNewMid(3), new MidHandler(handleCommShutdown));
					exitLoop = true;
				}
				if (!_commStarted) {
					this.sendMessage(MidFinder.createNewMid(1), new MidHandler(handleCommunicationStart));
					if (_commStarted) {
						_lastKeepAlive = DateTime.Now.Add(new TimeSpan(0, 0, -20));
					}
				} else {
					if (_commStarted)
						sendKeepAliveIfRequired();

					if (_stream.DataAvailable) {
						lock (_streamLock) {
							package = readMidStream();
							tmp = MidFinder.fixupPackage(package);
							writeToDataLog("read", tmp);
							//_logger.log(MethodBase.GetCurrentMethod(), "read: [" + package.Substring(0, package.Length - 1));
							int mymidno=MidFinder.findMidNumberFromPackage(package);
							if (_midResponseMap.ContainsKey(mymidno)) {
								bool bret;
								object amid=null;

								try {
									amid = MidFinder.createMidInstance(package);
									bret = _midResponseMap[mymidno](amid, package);
								} catch (Exception ex) {
									_logger.log(MethodBase.GetCurrentMethod(), ex);
								}
								//Trace.WriteLine("here-1");
								//Trace.WriteLine("here-1-1");
							} else
								_logger.log(
									ColtLogLevel.Warning,
									MethodBase.GetCurrentMethod(),
									"Unhandled MID=" + mymidno);
							//	)
							//	`
							//Trace.WriteLine("here-2");
						}
					} else {
						if (_midQueue != null) {
							qmid = null;

							lock (_queueLock) {
								if (_midQueue.Count > 0) {
									qmid = _midQueue.Dequeue();
								}
							}
							if (qmid != null)
								addMidRequest(qmid);
						}
						Thread.Sleep(100);
					}
				}
			}
			_logger.log(MethodBase.GetCurrentMethod(), "ends");
		}

		internal void enqueueMid(object amid) {
			lock (this._queueLock) {
				_midQueue.Enqueue(amid);
			}
		}

		internal void requestPSetIDs() {
			enqueueMid(MidFinder.createNewMid(10));
		}

		internal void requestJobIDs() {
			enqueueMid(MidFinder.createNewMid(30));
		}

		void sendKeepAliveIfRequired() {
			double tdiff;

			if ((tdiff = ((DateTime.Now - _lastKeepAlive).TotalSeconds)) > 10) {
				if (veryVerbose)
					Trace.WriteLine("TDiff=" + tdiff + ".");
				this.sendMessage(MidFinder.createNewMid(9999), new MidHandler(handleKeepAlive));
			}
		}
	}
}