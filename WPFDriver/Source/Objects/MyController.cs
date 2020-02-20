#define SKIP_HISTORICAL
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using NSAtlasCopcoShared;
#if !OTHER_VERSION
//using OpenProtocolInterpreter.MIDs;
//using OpenProtocolInterpreter.MIDs.Alarm;
//using OpenProtocolInterpreter.MIDs.ApplicationSelector;
//using OpenProtocolInterpreter.MIDs.ApplicationToolLocationSystem;
//using OpenProtocolInterpreter.MIDs.Communication;
//using OpenProtocolInterpreter.MIDs.IOInterface;
//using OpenProtocolInterpreter.MIDs.Job;
//using OpenProtocolInterpreter.MIDs.Job.Advanced;
//using OpenProtocolInterpreter.MIDs.KeepAlive;
//using OpenProtocolInterpreter.MIDs.MultipleIdentifiers;
//using OpenProtocolInterpreter.MIDs.OpenProtocolCommandsDisabled;
//using OpenProtocolInterpreter.MIDs.ParameterSet;
//using OpenProtocolInterpreter.MIDs.Tightening;
//using OpenProtocolInterpreter.MIDs.VIN;
#endif
namespace NSAtlasCopcoBreech {
	public enum MessageType {
		AlarmAcknowledgeTorqueController,
		AlarmUpload,
		AlarmStatus,
		CommStartAcknowledge,
		KeepAlive,
		LastTighteningResult
	}
	public delegate void DisplayStatusDelegate(string msg);
	public delegate void ProcessCommStatusDelegate(CommStatus commStatus);
#if !OTHER_VERSION
	public delegate void ProcessMidDelegate(MessageType messageType, OpenProtocolInterpreter.MIDs.MID messageObject, string messagestring);
#else
	public delegate void ProcessMidDelegate(MessageType messageType, object messageObject, string messagestring);
#endif
	public enum CommStatus {
		Down,
		Up,
		Unknown,
	}
	public partial class MyController : IDisposable {
		#region constants
#if true
		const int CLIENT_BUFF_SIZE = 4096;
#else
		const int CLIENT_BUFF_SIZE = 1024;
#endif
#endregion

		#region fields
		DateTime _lastMessage;
		DisplayStatusDelegate _displayStatusDelegate = null;
		FileStream _csvLogStream;
		FileStream _midLogStream;
		NetworkStream _clientStream = null;
		ProcessCommStatusDelegate _processCommStatusDelegate = null;
		ProcessMidDelegate _processMidDelegate = null;
		StreamWriter _csvLogFile;
		Task _taskKeepAlive;
		Task _taskMonitor;
		Task _taskReceive;
		TcpClient _tcpClient = null;
		TextWriter _midLogFile;
		bool _TryingToConnectInProgress = false;
		bool _csvWroteCSVHeader;
		bool _initialTighteningData;
		bool _lastTcpConnectionIsOkForRead = false;
		bool _shuttingDown;
		bool disposedValue = false;
		int _lastTighteningID;
		int _port;
		int _thisTighteningID;
		internal event EventHandler ThreadsShutdown;
		object _writeLock = new object();
		readonly DateTime _TimeOfLastLogicalConnectedToController = new DateTime(1948, 8, 24);
		readonly byte[] _clientBuff = new byte[CLIENT_BUFF_SIZE];
		readonly object _csvLock=new object();
		readonly object _dictLock = new object();
		static bool _verVerbose = true;
		static int _nextFileNumber=-1;
		static readonly IDictionary<int,MidData> _tighteningMap=new Dictionary<int, MidData>();
		static readonly ManualResetEvent _mreShutdown=new ManualResetEvent(false);
		static readonly ManualResetEvent _mreThreads=new ManualResetEvent(true);
		static readonly bool showMidContent=true;
		static readonly object _tighteningLock=new object();
		static readonly object midLogLock = new object();
		string _csvTighteningName;
		string _ipAddress = string.Empty;
		#endregion

		#region cctor
		static MyController() {
#if true
			logFilePath=MIDUtil.midLogPath;
#else
			string asmName = Assembly.GetEntryAssembly().GetName().Name;
			logFilePath = Path.Combine(
				Environment.GetEnvironmentVariable("TEMP"),
				asmName);
#endif
		}

		#endregion
		
		#region ctor
		public MyController() {
			createNewLogFile();
		}
		#endregion

		#region properties
		public static bool veryVerbose { get { return _verVerbose; } set { _verVerbose = value; } }
		public static string logFilePath { get; set; }
		#endregion

		static readonly object _streamLock=new object();
		#region public methods
		public bool initialize(string ipAddress, int port, ProcessMidDelegate processMidDelegate,
			DisplayStatusDelegate displayStatusDelegate, ProcessCommStatusDelegate processCommStatusDelegate) {
			return initialize(ipAddress, port, processMidDelegate, displayStatusDelegate, processCommStatusDelegate, null);
		}
		public bool initialize(string ipAddress, int port, ProcessMidDelegate processMidDelegate,
			DisplayStatusDelegate displayStatusDelegate, ProcessCommStatusDelegate processCommStatusDelegate, IColtLogger logger) {
			try {
				Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(), string.Format("{0} {1} {2} {3} {4} {5}", ipAddress, port, processMidDelegate, displayStatusDelegate, processCommStatusDelegate, logger));
				if (_ipAddress != string.Empty) {
					Utility.logger.log(ColtLogLevel.Error, MethodBase.GetCurrentMethod(), "Initialize Can Only Be Called Once");
					return false;
				}
				if (string.IsNullOrEmpty(ipAddress)) {
					Utility.logger.log(ColtLogLevel.Error, MethodBase.GetCurrentMethod(), "IP Address is invalid!");
					return false;
				}
				_ipAddress = ipAddress;
				_port = port;
				Utility.logger.log(
					ColtLogLevel.Info,
					MethodBase.GetCurrentMethod(),
					"Connecting to: "+ipAddress+":"+port+".");
				_processMidDelegate = processMidDelegate;
				_displayStatusDelegate = displayStatusDelegate;
				_processCommStatusDelegate = processCommStatusDelegate;
				_taskReceive=Task.Run(() => { receiveThread(); });
				Thread.Sleep(1000);
				_taskKeepAlive=Task.Run(() => { sendKeepAliveThread(); });
				_taskMonitor=Task.Run(() => { monitorCommunicationLinkThread(); });
				return true;
			} catch (Exception ex) {
				Utility.logger.log(MethodBase.GetCurrentMethod(), ex);
				return false;
			}
		}
		public void close() {
			try {
				processCommunicationStatus(CommStatus.Down);
				try {
					lock (_streamLock) {
						if (_clientStream != null) {
							Utility.logger.log(ColtLogLevel.Info, " Attempting To Close Stream");
							_clientStream.Close();
							Utility.logger.log(ColtLogLevel.Info, " TcpClient Stream Closed");
							_clientStream = null;
						}
					}
				} catch {
				}
				try {
					if (_tcpClient != null) {
						if (veryVerbose) Utility.logger.log(ColtLogLevel.Info, " Attempting To Close Client");
						_tcpClient.Close();
						if (veryVerbose) Utility.logger.log(ColtLogLevel.Info, " TcpClient Closed");
						_tcpClient = null;
					}
				} catch {
				}
			} catch (Exception ex) {
				Utility.logger.log(MethodBase.GetCurrentMethod(), ex);
			} finally {
				_clientStream = null;
				_tcpClient = null;
			}
		}
		#endregion
		#region methods
		internal void shutdown() {


			Utility.logger.log(MethodBase.GetCurrentMethod());
			_shuttingDown=true;
			unsubscribeFromEvents();

			_mreShutdown.Set();
			_mreShutdown.WaitOne(Timeout.Infinite);

			//Thread.Sleep(10*1000);


			Utility.logger.log(MethodBase.GetCurrentMethod(), "notifying threads");
			_mreThreads.Reset();

			// wait for reconnection thread.
			_taskMonitor.Wait();
			_taskMonitor.Dispose();
			//_taskMonitor=null;

			// now, send disconnect and unsubscribe messages.

			//Thread.Sleep(10*1000);

			//// send Communication Stop message.
			//sendMid(new MID_0005());

			//Thread.Sleep(10*1000);

			Task.WaitAll(new Task[] {
				_taskKeepAlive,
				//_taskMonitor,
				_taskReceive
			});
			Utility.logger.log(MethodBase.GetCurrentMethod(), "threads closed");
			lock (midLogLock) {
				closeLogFiles();
			}
			this.ThreadsShutdown?.Invoke(this, new EventArgs());
			_mreThreads.Set();
		}

		void closeLogFiles() {
			try {
				if (_midLogStream!=null) {
					_midLogStream.Flush();
					_midLogStream.Close();
					_midLogStream.Dispose();
					_midLogStream=null;
				}
			} catch (Exception ex) {
				Trace.WriteLine("**** close of MID_LOG_STREAM failed!"+Environment.NewLine+ex.Message);
			}
			try {
				if (_midLogFile!=null) {
					_midLogFile=null;
				}
			} catch (Exception ex) {
				Trace.WriteLine("**** close of MID_LOG_FILE failed!"+Environment.NewLine+ex.Message);
			}
			lock (_csvLock) {
				try {
					if (_csvLogStream!=null) {
						_csvLogStream.Flush();
						_csvLogStream.Close();
						_csvLogStream.Dispose();
						_csvLogStream=null;
					}
				} catch (Exception ex) {
					Trace.WriteLine("**** close of CSV_LOG_STREAM failed!"+Environment.NewLine+ex.Message);
				}
				try {
					if (_csvLogFile!=null) {
						_csvLogFile=null;
					}
				} catch (Exception ex) {
					Trace.WriteLine("**** close of CSV_LOG_FILE failed!"+Environment.NewLine+ex.Message);
				}
			}
		}

		internal void createNewLogFile() {
			string logName = findNextLogFileName();

			Utility.logger.log(ColtLogLevel.Debug, "log-file: "+logName+".");
			lock (midLogLock) {
				closeLogFiles();
				_midLogStream = new FileStream(logName, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite);
				_midLogFile = new StreamWriter(_midLogStream);
			}
		}

		static string findNextLogFileName() {
			string  logName,asmName = Assembly.GetEntryAssembly().GetName().Name;
			if (_nextFileNumber<0)
				_nextFileNumber=findLogFileNumber2(asmName);
			logName = Path.Combine(logFilePath, asmName + "_" + (++_nextFileNumber).ToString("000#") + ".data.log");
			return logName;
		}
		static int findLogFileNumber2(string asmName) {
			string[] logFiles;
			string tmp;
			List<string> logFileList;
			int fileno=-1,val;
			logFiles = Directory.GetFiles(logFilePath, asmName + "_*.data.log");
			logFileList = new List<string>(logFiles);
			logFileList.Sort();
			if (logFileList.Count>0) {
				tmp = Path.GetFileNameWithoutExtension(logFileList[logFileList.Count - 1]).Substring((asmName + "_").Length, 4);
				if (int.TryParse(tmp, out val))
					fileno=val;
			}
			return fileno;
		}
#if OTHER_VERSION
		void processObject(MessageType messageType, object messageObject, string messagestring)
#else
		void processObject(MessageType messageType, OpenProtocolInterpreter.MIDs.MID messageObject, string messagestring)
#endif
			{

			try {
				if (_processMidDelegate == null) {
					Utility.logger.log(ColtLogLevel.Warning, MethodBase.GetCurrentMethod(), "_ProcessObjectDelegate == null");
					return;
				}
				_processMidDelegate.BeginInvoke(messageType, messageObject, messagestring, null, null);
				if (veryVerbose)
					Utility.logger.log(ColtLogLevel.Info, messageType + " sent to parent");
			} catch (Exception ex) {
				Utility.logger.log(MethodBase.GetCurrentMethod(), ex);
			}
		}
		void displayStatus(string status) {
			MethodBase mb = MethodBase.GetCurrentMethod();
			try {
				if (_displayStatusDelegate == null) {
					Utility.logger.log(ColtLogLevel.Info, mb, "Status=" + status + ".");
					return;
				}
				_displayStatusDelegate.BeginInvoke(ColtLogger.makeSig(mb) + "Status=" + status + ".", null, null);
			} catch (Exception ex) {
				Utility.logger.log(mb, ex);
			}
		}


		void processCommunicationStatus(CommStatus commStatus) {
			try {
				if (_processCommStatusDelegate == null) {
					Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(), "disabled");
					return;
				}
				_processCommStatusDelegate.BeginInvoke(commStatus, null, null);
			} catch (Exception ex) {
				Utility.logger.log(MethodBase.GetCurrentMethod(), ex);
			}
		}
		void connect() {
			try {
				if (veryVerbose)
					Utility.logger.log(MethodBase.GetCurrentMethod());
				_lastMessage = DateTime.Now;
				Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(), "connection in progress");
				_TryingToConnectInProgress = true;
				_tcpClient = new TcpClient(_ipAddress, _port) {
					NoDelay = true
				};
				lock (_streamLock) {
					_clientStream = _tcpClient.GetStream();
				}
				Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(), "Connected");
				processCommunicationStatus(CommStatus.Up);
				Thread.Sleep(1000);
				sendCommunicationStart(ref _writeLock, ref _lastMessage, ref _clientStream, ref _TryingToConnectInProgress, ref _tcpClient);
				Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(), "OK");
				return;
			} catch (Exception ex) {
				Utility.logger.log(MethodBase.GetCurrentMethod(), ex);
				close();
				return;
			}
		}

		#endregion
		#region thread-handling methods
		void monitorCommunicationLinkThread() {
			//bool shutDown=false;
			Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod());
			while (!_shuttingDown) {
				try {
					if (!_mreThreads.WaitOne(100)) {
						Utility.logger.log(MethodBase.GetCurrentMethod(), "signaled!");
						//shutDown=true;
						break;
					}
					if (_tcpClient == null&&!_shuttingDown) connect();
				} catch (Exception ex) {
					Utility.logger.log(MethodBase.GetCurrentMethod(), ex);
				}
				if (!_shuttingDown)
					Thread.Sleep(1000);
			}
		}
		void sendKeepAliveThread() {
			MethodBase mb = MethodBase.GetCurrentMethod();
			DateTime now;

			while (!_shuttingDown) {
				try {
					if (!_mreThreads.WaitOne(100)) {
						Utility.logger.log(MethodBase.GetCurrentMethod(), "signaled!");
						//shutdownThread=true;
						break;
					}
					if (_clientStream == null&&!_shuttingDown) {
						Thread.Sleep(1000);
						continue;
					} else if (_TryingToConnectInProgress) {
						Utility.logger.log(ColtLogLevel.Info, mb, "connection in progress - don't send keep-alive");
						Thread.Sleep(1000);
						continue;
					}
					now = DateTime.Now;
					if (now.Subtract(_lastMessage) > new TimeSpan(0, 0, 10)) {
						sendKeepAlive();
					}
					if (!_shuttingDown)
						Thread.Sleep(1000);
				} catch (Exception ex) {
					close();
					Utility.logger.log(mb, ex);
				}
			}
		}

		void receiveThread() {
			MethodBase mb = MethodBase.GetCurrentMethod();
			int bytesRead = 0, length;
			string package;
			//byte[] command;
			bool shutdownThread=false;
			if (veryVerbose)
				Utility.logger.log(ColtLogLevel.Info, mb);
			while (!shutdownThread) {
				try {
					if (!_mreThreads.WaitOne(100)) {
						Utility.logger.log(MethodBase.GetCurrentMethod(), "signaled!");
						shutdownThread=true;
						this.close();
						break;
					}
					if (_clientStream == null) {
						processCommunicationStatus(CommStatus.Down);
						if (_lastTcpConnectionIsOkForRead) {
							Utility.logger.log(ColtLogLevel.Info, mb, "Stream Just Went Down");
						} else {
							_lastTcpConnectionIsOkForRead = false;
							if (veryVerbose)
								Utility.logger.log(ColtLogLevel.Info, mb, "_LastTcpConnectionIsOkForRead set false");
						}
						Thread.Sleep(500);
						continue;
					}
					if (_lastTcpConnectionIsOkForRead == false) {
						_lastTcpConnectionIsOkForRead = true;
						if (veryVerbose)
							Utility.logger.log(ColtLogLevel.Info, mb, "Reading (4 byte length) After Stream Just Came Up");
					} else {
						if (veryVerbose)
							Utility.logger.log(ColtLogLevel.Info, mb, "Reading (4 byte length)");
					}
					bytesRead = _clientStream.Read(_clientBuff, 0, 4);
					if (veryVerbose)
						Utility.logger.log(ColtLogLevel.Info, mb, "Read Completed " + bytesRead + " Bytes");
					if (bytesRead == 0) {
						if (!_shuttingDown) {
							close();
							Utility.logger.log(ColtLogLevel.Error, mb, "Read returned 0 bytes - CLOSING");
						}
						Thread.Sleep(500);
						continue;
					}
					while (bytesRead < 4)
						bytesRead += _clientStream.Read(_clientBuff, bytesRead, 4 - bytesRead);
					length = int.Parse(Encoding.ASCII.GetString(_clientBuff, 0, 4)) + 1;
					lock (_streamLock) {
						bytesRead += _clientStream.Read(_clientBuff, 4, length - 4);
					}
					if (veryVerbose)
						Utility.logger.log(ColtLogLevel.Info, mb, bytesRead + " of " + length + " Bytes Read.");
					lock (_streamLock) {
						while (bytesRead < length)
							bytesRead += _clientStream.Read(_clientBuff, bytesRead, length - bytesRead);
					}
					bytesRead = 0;
					package = Encoding.ASCII.GetString(_clientBuff, 0, length);
					if (!string.IsNullOrEmpty(package) && _midLogFile != null) {
						if (!string.IsNullOrEmpty(package) &&
							package.Length > 8 &&
							(MIDUtil.midIdent(package)!=9999 /* keep-alive */							&&
							MIDUtil.midIdent(package)!=5) /* accepted */)

							//string.Compare(package.Substring(4, 4), "9999", true) != 0)
							lock (midLogLock) {
								_midLogFile.WriteLine(package);
								_midLogFile.Flush();
								_midLogStream.Flush();
							}
						if (showMidContent)
							MIDUtil.showMid(package);
					}
					if (!string.IsNullOrEmpty(package) && package.Length > 8 && string.Compare(package.Substring(4, 4), "9999") != 0)
						Utility.logger.log(ColtLogLevel.Info, mb, "[" + package + "]");
					processCommunicationStatus(CommStatus.Up);
					handlePackage(mb, package);
				} catch (SocketException exSock) {
					Utility.logger.log(MethodBase.GetCurrentMethod(), exSock);
					close();
				} catch (Exception ex) {
					Utility.logger.log(MethodBase.GetCurrentMethod(), ex);
					close();
				}
			}
		}

		void generateTighteningRequests() {
			int nIDS= _thisTighteningID-_lastTighteningID;
#if true
			Utility.logger.log(
				ColtLogLevel.Debug,
				MethodBase.GetCurrentMethod(),
				"LastTID="+_lastTighteningID+", ThisTID="+_thisTighteningID+", nTIDS="+nIDS+".");
#else

			MID_0064 oldMid;

			StringBuilder sb=       new StringBuilder();

			if (nIDS>0) {
#if SKIP_HISTORICAL
				sb.Append("NOT sending TIDs: ");
#else
#endif
				// request "missing" results.
				oldMid=new MID_0064();
				lock (_tighteningLock) {
					for (int newID = _lastTighteningID+1; newID<_thisTighteningID; newID++) {
						if (!_tighteningMap.ContainsKey(newID)) {
							_tighteningMap.Add(newID, new MidData(newID));
						} else
							_tighteningMap[newID].reset();
						oldMid.TighteningID=newID;
						if (n>0)
							sb.Append(", ");
						sb.Append(newID);
#if !SKIP_HISTORICAL
						sendMid(oldMid);
#endif
					}
				}
				_lastTighteningID=_thisTighteningID;
				_thisTighteningID=-1;
			} else {
				sb.Append("good");
			}
			if (sb.Length<1)
				sb.AppendLine("NO TIDS");
			Utility.logger.log(ColtLogLevel.Debug, sb.ToString());
#endif
		}

		#endregion
		#region IDisposable Support
		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				if (disposing) {
					if (_midLogFile != null) {
						lock (midLogLock) {
							_midLogFile.Flush();
							_midLogFile.Close();
							_midLogFile = null;
						}
					}
				}
				disposedValue = true;
			}
		}
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}