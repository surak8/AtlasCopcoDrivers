using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenProtocolInterpreter.MIDs;
using OpenProtocolInterpreter.MIDs.Alarm;
using OpenProtocolInterpreter.MIDs.ApplicationSelector;
using OpenProtocolInterpreter.MIDs.ApplicationToolLocationSystem;
using OpenProtocolInterpreter.MIDs.Communication;
using OpenProtocolInterpreter.MIDs.IOInterface;
using OpenProtocolInterpreter.MIDs.Job;
using OpenProtocolInterpreter.MIDs.Job.Advanced;
using OpenProtocolInterpreter.MIDs.KeepAlive;
using OpenProtocolInterpreter.MIDs.MultipleIdentifiers;
using OpenProtocolInterpreter.MIDs.OpenProtocolCommandsDisabled;
using OpenProtocolInterpreter.MIDs.ParameterSet;
using OpenProtocolInterpreter.MIDs.Tightening;
using OpenProtocolInterpreter.MIDs.VIN;
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
	public delegate void ProcessMidDelegate(MessageType messageType, MID messageObject, string messagestring);
	public enum CommStatus {
		Down,
		Up,
		Unknown,
	}
	public class MyController : IDisposable {
		#region constants
		const int CLIENT_BUFF_SIZE = 1024;
		#endregion
		#region fields
		DateTime _lastMessage;
		DisplayStatusDelegate _displayStatusDelegate = null;
		NetworkStream _clientStream = null;
		ProcessCommStatusDelegate _processCommStatusDelegate = null;
		ProcessMidDelegate _processMidDelegate = null;
		Task _taskKeepAlive;
		Task _taskMonitor;
		Task _taskReceive;
		TcpClient _tcpClient = null;
		TextWriter _midLogFile;
		bool _TryingToConnectInProgress = false;
		bool _lastTcpConnectionIsOkForRead = false;
		bool disposedValue = false;
		int _port;
		internal event EventHandler ThreadsShutdown;
		object _writeLock = new object();
		readonly DateTime _TimeOfLastLogicalConnectedToController = new DateTime(1948, 8, 24);
		FileStream _midLogStream;
		readonly byte[] _clientBuff = new byte[CLIENT_BUFF_SIZE];
		readonly object _dictLock = new object();
		static bool _verVerbose = false;
		static int _nextFileNumber=-1;
		static readonly ManualResetEvent _mreThreads=new ManualResetEvent(true);
		static readonly object midLogLock = new object();
		 static readonly bool showMidContent=true;
		string _ipAddress = string.Empty;
		#endregion
		#region cctor
		static MyController() {
			string asmName = Assembly.GetEntryAssembly().GetName().Name;
			logFilePath = Path.Combine(
				Environment.GetEnvironmentVariable("TEMP"),
				asmName);
		}

		#endregion
		#region ctor
		public MyController() {
			createNewLogFile();
		}
		#endregion

		#region properties
		public static bool veryVerbose { get { return _verVerbose; } set { _verVerbose = value; } }
		public static string logFilePath { get; private set; }
		#endregion
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
					if (_clientStream != null) {
						Utility.logger.log(ColtLogLevel.Info, " Attempting To Close Stream");
						_clientStream.Close();
						Utility.logger.log(ColtLogLevel.Info, " TcpClient Stream Closed");
						_clientStream = null;
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
			Utility.logger.log(MethodBase.GetCurrentMethod(), "notifying threads");
			_mreThreads.Reset();

			// wait for reconnection thread.
			_taskMonitor.Wait();
			_taskMonitor.Dispose();
			//_taskMonitor=null;

			// now, send disconnect and unsubscribe messages.
			sendMid(new MID_0037()); // unsubscribe job-info
			sendMid(new MID_0054()); // unsubscribe vehicle

			Thread.Sleep(10*1000);

			// send Communication Stop message.
			sendMid(new MID_0003());

			Thread.Sleep(10*1000);

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
			//lock (midLogLock) {
			if (_midLogStream!=null) {
				_midLogStream.Flush();
				_midLogStream.Close();
				_midLogStream.Dispose();
			}
			if (_midLogFile!=null) {
				_midLogFile=null;
			}
			//}
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
				if (Int32.TryParse(tmp, out val))
					fileno=val;
			}
			return fileno;
		}
		void processObject(MessageType messageType, MID messageObject, string messagestring) {
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
				_clientStream = _tcpClient.GetStream();
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
		static void sendCommunicationStart(ref object objLock, ref DateTime lastMsg, ref NetworkStream clStream, ref bool attemptingConnection, ref TcpClient clTCP) {
			MID_0001 mid;
			MethodBase mb;
			string package, midName;
			byte[] command;

			mb = MethodBase.GetCurrentMethod();
			mid = new MID_0001();
			midName = mid.GetType().Name;
			command = Encoding.ASCII.GetBytes(package = mid.buildPackage() + "\0");
			Utility.logger.log(ColtLogLevel.Info, mb, "send " + midName + ".");
			lock (objLock) {
				lastMsg = DateTime.Now;
				if (clStream == null) {
					Utility.logger.log(ColtLogLevel.Info, mb, "OHOH  _clientStream is null");
					if (clTCP == null)
						Utility.logger.log(ColtLogLevel.Info, mb, "OHOH _tcpClient is null too");
					Utility.logger.log(ColtLogLevel.Info, mb, "connection in progress finished due to problems");
					attemptingConnection = false;
				} else {
					clStream.Write(command, 0, command.Length);
					Utility.logger.log(ColtLogLevel.Info, mb, midName + " sent.");
				}
			}
		}
		#endregion
		#region thread-handling methods
		void monitorCommunicationLinkThread() {
			bool shutDown=false;
			Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod());
			while (!shutDown) {
				try {
					if (!_mreThreads.WaitOne(100)) {
						Utility.logger.log(MethodBase.GetCurrentMethod(), "signaled!");
						shutDown=true;
						break;
					}
					if (_tcpClient == null) connect();
				} catch (Exception ex) {
					Utility.logger.log(MethodBase.GetCurrentMethod(), ex);
				}
				if (!shutDown)
					Thread.Sleep(1000);
			}
		}
		void sendKeepAliveThread() {
			MethodBase mb = MethodBase.GetCurrentMethod();
			DateTime now;
			//MID_9999 mid;
			//string package;
			//byte[] command;
			bool shutdownThread=false;
			while (!shutdownThread) {
				try {
					if (!_mreThreads.WaitOne(100)) {
						Utility.logger.log(MethodBase.GetCurrentMethod(), "signaled!");
						shutdownThread=true;
						break;
					}
					if (_clientStream == null) {
						Thread.Sleep(1000);
						continue;
					} else if (_TryingToConnectInProgress) {
						Utility.logger.log(ColtLogLevel.Info, mb, "connection in progress - don't send keep-alive");
						Thread.Sleep(1000);
						continue;
					}
					now = DateTime.Now;
					if (now.Subtract(_lastMessage) > new TimeSpan(0, 0, 10)) {
#if true
						sendMid(new MID_9999());
#else
						mid = new MID_9999();
						command = Encoding.ASCII.GetBytes(package = mid.buildPackage() + "\0");
						lock (_writeLock) {
							_lastMessage = DateTime.Now;
							_clientStream.Write(command, 0, command.Length);
							if (veryVerbose)
								Utility.logger.log(ColtLogLevel.Info, mb, mid.GetType().Name + " Sent");
						}
#endif
					}
					if (!shutdownThread)
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
						close();
						Utility.logger.log(ColtLogLevel.Error, mb, "Read returned 0 bytes - CLOSING");
						Thread.Sleep(500);
						continue;
					}
					while (bytesRead < 4)
						bytesRead += _clientStream.Read(_clientBuff, bytesRead, 4 - bytesRead);
					length = int.Parse(Encoding.ASCII.GetString(_clientBuff, 0, 4)) + 1;
					bytesRead += _clientStream.Read(_clientBuff, 4, length - 4);
					if (veryVerbose)
						Utility.logger.log(ColtLogLevel.Info, mb, bytesRead + " of " + length + " Bytes Read.");
					while (bytesRead < length)
						bytesRead += _clientStream.Read(_clientBuff, bytesRead, length - bytesRead);
					bytesRead = 0;
					package = Encoding.ASCII.GetString(_clientBuff, 0, length);
					if (!string.IsNullOrEmpty(package) && _midLogFile != null) {
						if (!string.IsNullOrEmpty(package) &&
							package.Length > 8 &&
							string.Compare(package.Substring(4, 4), "9999", true) != 0)
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
					switch (package.Substring(4, 4)) {
						case "0002":
							_TryingToConnectInProgress = false;
							if (veryVerbose)
								Utility.logger.log(ColtLogLevel.Info, "Connect() received MID0002 - connection in progress has completed");
							MID_0002 mid0002 = new MID_0002();
							mid0002.processPackage(package);
							processObject(MessageType.CommStartAcknowledge, mid0002, package);
							processCommunicationStatus(CommStatus.Up);
							DateTime now = DateTime.Now;
							if (now.Subtract(_TimeOfLastLogicalConnectedToController) > TimeSpan.FromMinutes(5)) {
								/* */
								sendMid(new MID_0014()); // PSET subscribe, respond with MID_0016
#warning removed MID_0021
								//sendMid(new MID_0021()); // lock-at-batch-done
								sendMid(new MID_0034());    // Job info subscribe, 

								sendMid(new MID_0051());    // VIN subscribe
								sendMid(new MID_0060());    // Last tightening
								sendMid(new MID_0070());    // Alarm subscribe

								// Command Error ErrorCode:CONTROLLER_IS_NOT_A_SYNC_MASTER_OR_STATION_CONTROLLER for MID=90.
								//sendMid(new MID_0090());    // MultiSpindle status subscribe

#warning MID_0100 is missing?
								//sendMid(new MID_0100());    // MultiSpindle result subscribe

								// Command Error ErrorCode:UNKNOWN_MID for MID=105.
								//sendMid(new MID_0105());    // PowerMACS result subscribe

								sendMid(new MID_0120());    // Job line control info subscribe
								sendMid(new MID_0151());    // multi-ident result parts subscribe
								sendMid(new MID_0210());    // extern inputs subscribe
								sendMid(new MID_0216());    // relay function subscribe
								sendMid(new MID_0220());    // digital input subscribe

								// Command Error ErrorCode:UNKNOWN_MID for MID=241.
								//sendMid(new MID_0241());    // user-data subscribe

								sendMid(new MID_0250());    // selector-socket subscribe
								sendMid(new MID_0261());    // tool tag id subscribe

								// Command Error ErrorCode:UNKNOWN_MID for MID=400.
								//sendMid(new MID_0400());    // auto/manual mode id subscribe

								sendMid(new MID_0420());    // open proto disable subscribe

								/*
								if (_subscriptions.Contains(Subscriptions.LastTighteningResult)) {
									sendMid(subscribeLastTightening_0060());
								}
								if (_subscriptions.Contains(Subscriptions.Alarm)) {
									sendMid(createAlarmSubscription_0070());
								}
								if (_subscriptions.Contains(Subscriptions.Relay)) {
									sendMid(createRelay_0216());
								}
								if (_subscriptions.Contains(Subscriptions.DigitalInput)) {
									sendMid(createDigitalInput_0220());
								}
								*/
							}
#if true
							sendMid(new MID_0030());
#else
							MID_0030 mid0030 = new MID_0030();
							package = mid0030.buildPackage() + "\0";
							command = Encoding.ASCII.GetBytes(package);
							lock (_writeLock) {
								_lastMessage = DateTime.Now;
								_clientStream.Write(command, 0, command.Length);
								Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(), "MID0030 Sent");
							}
#endif
							break;
						case "0004": handleCommandError_0004(package); break;
						case "0005": handleCommandAccepted_0005(package); break;
						case "0015": handleCommandAccepted_0015(package); break; // show PSET subscribed
						case "0021": sendMid(new MID_0023()); break; // notification
						case "0022": break; // look this up!

						case "0031": captureJobNumber(package); break;
						case "0052": captureVehicleID(package); break;
						case "0061":
							MID_0061 mid0061 = new MID_0061();
							mid0061.processPackage(package);
							Utility.logger.log(ColtLogLevel.Info, string.Format("Batch: {0} of {1} SetId: {2} Id: {3} Torque: {4} Target: {5} Min: {6} Max: {7} Status: Torque:{8} Angle:{9} Tightening:{10}",
													mid0061.BatchCounter, mid0061.BatchSize, mid0061.ParameterSetID, mid0061.TighteningID, mid0061.Torque, mid0061.TorqueFinalTarget, mid0061.TorqueMinLimit, mid0061.TorqueMaxLimit,
													mid0061.TorqueStatus, mid0061.AngleStatus, mid0061.TighteningStatus));
							/*************************************************************************************************************
                             * *** rik -- 27-JUN-19                                                                                    ***
                             * ***                   Undid my previous change.  I was trying to solve the "17th" pair                  ***
                             *                       issue for the bolt-carrier operation, and may have damaged the processing of      ***
                             *                       torque wrench tightening.                                                         ***
                             * ***                   There are several problems with this approach.  First of all, the 'ProcessObject' ***
                             *                       delegate is void, and as a result, the ACK is always performed, regardless        ***
                             *                       of the result of what's done (or FAILS) within the target of 'ProcessObject.      ***
                             ************************************************************************************************************/
							processObject(MessageType.LastTighteningResult, mid0061, package);
							sendMid(createMid0062());
							break;
						case "0071": handleAlarm_0071(package, processObject); sendMid(createAlarmAcknowledgement()); break;
						case "0074": handleControllerAlarmAck(package, processObject, displayStatus); sendMid(createControllerAlarmAcknowledged()); break;
						case "0076": handleAlarmStatus(package); break;
						case "0152": handleMultiIdentAndParts_0152(package); sendMid(new MID_0153()); break;
						case "0211": handleExternalInputs_0211(package); sendMid(new MID_0212()); break;
						case "9999": handleKeepAlive(package, processObject); break;
						default: displayStatus(ColtLogger.makeSig(mb) + "*** Unsupported package received [" + package.Substring(4, 4) + "] ***"); break;
					}
				} catch (SocketException exSock) {
					Utility.logger.log(MethodBase.GetCurrentMethod(), exSock);
					close();
				} catch (Exception ex) {
					Utility.logger.log(MethodBase.GetCurrentMethod(), ex);
					close();
				}
			}
		}

		void handleCommandAccepted_0015(string package) {
			MID_0015 mid=new MID_0015();

			mid.processPackage(package);
			Utility.logger.log(MethodBase.GetCurrentMethod());
			sendMid(new MID_0016());
			if (mid.ParameterSetID==0) {
				Utility.logger.log(ColtLogLevel.Debug, MethodBase.GetCurrentMethod(), "ParmSet="+mid.ParameterSetID+". Creating a new log-file.");
				createNewLogFile();
			}
		}

		void handleExternalInputs_0211(string package) {
			MyMid_211 mid = new MyMid_211();
			mid.processPackage(package);
			Utility.logger.log(MethodBase.GetCurrentMethod());
		}
		void handleMultiIdentAndParts_0152(string package) {
			MyMid_152 mid = new MyMid_152();
			mid.processPackage(package);
			Utility.logger.log(MethodBase.GetCurrentMethod());
		}
		MID createDigitalInput_0220() {
			return new MID_0220();
		}
		static void handleCommandError_0004(string package) {
			MID_0004 mid0004 = new MID_0004();
			mid0004.processPackage(package);
			Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(), "Command Error ErrorCode:" + mid0004.ErrorCode + " for MID="+mid0004.FailedMid+".");
		}
		static void handleCommandAccepted_0005(string package) {
			MID_0005 mid = new MID_0005();
			string blah;
			MethodBase mb = MethodBase.GetCurrentMethod();
			mid.processPackage(package);
			Utility.logger.log(ColtLogLevel.Info, mb, "Command Accepted: " + mid.MIDAccepted + ".");
			blah = package.Substring(20, 4);
			if (blah == "0018") {
				Utility.logger.log(ColtLogLevel.Info, "MID0018 Accepted");
			} else if (blah == "0031") {
				Utility.logger.log(ColtLogLevel.Info, "MID0031 Accepted");
			}
		}
		MID createRelay_0216() { return new MID_0216(); }
		MID createMid0062() { return new MID_0062(); }
		static void handleAlarm_0071(string package, ProcessMidDelegate po) {
			MID_0071 mid = new MID_0071();
			mid.processPackage(package);
			po(MessageType.AlarmUpload, mid, package);
		}
		MID createAlarmSubscription_0070() {
			return new MID_0070();
		}
		MID createAlarmAcknowledgement() {
			return new MID_0072();
		}
		static void handleControllerAlarmAck(string package, ProcessMidDelegate po, DisplayStatusDelegate ds) {
			MID_0074 mid = new MID_0074();
			mid.processPackage(package);
			po(MessageType.AlarmAcknowledgeTorqueController, mid, package);
			if (veryVerbose)
				ds("Error: [" + mid.ErrorCode + "]");
		}
		void sendMid(MID mid) {
			string tmp,package = (tmp=mid.buildPackage()) + "\0";
			byte[] command = Encoding.ASCII.GetBytes(package);
			lock (_writeLock) {
				_lastMessage = DateTime.Now;
				if (_clientStream!=null)
					_clientStream.Write(command, 0, command.Length);
				if (mid.HeaderData.Mid==9999
					) {
					if (veryVerbose)
						Utility.logger.log(ColtLogLevel.Debug, "*** SENT: ["+tmp+"]");
				} else
					Utility.logger.log(ColtLogLevel.Debug, "*** SENT: ["+tmp+"]");
				//if (veryVerbose)
				//	Utility.logger.log(ColtLogLevel.Info, mid.GetType().Name + " Sent");
			}
		}
		MID createControllerAlarmAcknowledged() { return new MID_0075(); }
		void handleAlarmStatus(string package) {
			MID_0076 mid = new MID_0076();
			mid.processPackage(package);
			processObject(MessageType.AlarmStatus, mid, package);
			if (!string.IsNullOrEmpty(mid.AlarmStatusData.ErrorCode)) {
				displayStatus("Controller Status: " + mid.AlarmStatusData.ControllerReadyStatus + ", Alarm Error: " + mid.AlarmStatusData.ErrorCode + ".");
			} else {
				displayStatus("Controller Status: " + mid.AlarmStatusData.ControllerReadyStatus + ".");
			}
			sendMid(createAckAlarmStatus_0077());
		}
		static MID createAckAlarmStatus_0077() { return new MID_0077(); }
		static MID subscribeLastTightening_0060() { return new MID_0060(); }
		static void handleKeepAlive(string package, ProcessMidDelegate processObject) {
			MID_9999 mid = new MID_9999();
			mid.processPackage(package);
			if (veryVerbose)
				Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(), mid.ToString());
			processObject(MessageType.KeepAlive, mid, package);
		}
		static void captureVehicleID(string package) {
			MID_0052 mid = new MID_0052();
			mid.processPackage(package);
			Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(), mid.ToString());
		}
		static void captureJobNumber(string package) {
			MID_0031 mid = new MID_0031();
			mid.processPackage(package);
			foreach (int jobId in mid.JobIds)
				Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(), "Job Id: " + jobId + ".");
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