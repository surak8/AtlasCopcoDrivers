using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenProtocolInterpreter.MIDs;
using OpenProtocolInterpreter.MIDs.Alarm;
using OpenProtocolInterpreter.MIDs.Communication;
using OpenProtocolInterpreter.MIDs.IOInterface;
using OpenProtocolInterpreter.MIDs.Job;
using OpenProtocolInterpreter.MIDs.KeepAlive;
using OpenProtocolInterpreter.MIDs.MultipleIdentifiers;
using OpenProtocolInterpreter.MIDs.Tightening;
using OpenProtocolInterpreter.MIDs.VIN;
using OpenProtocolUtility;
using OpenProtocolUtility.Serialization;

namespace NSAtlasCopcoBreech {
	//public class MyController { }
	public delegate void DisplayStatusDelegate(string msg);
	public delegate void ProcessCommStatusDelegate(CommStatus commStatus);
	public delegate void ProcessMidDelegate(MessageType messageType, MID messageObject, string messagestring);

	public enum CommStatus {
		Down,
		Up,
		Unknown,
	}

	public class MyController : IDisposable {
		enum Subscriptions {
			LastTighteningResult,
			Alarm,
			Relay,
			DigitalInput
		}


		#region constants
		const int CLIENT_BUFF_SIZE = 1024;
		#endregion

		#region fields
		static bool _verVerbose = false;
		// DATA
		DateTime _lastMessage;
		string _ipAddress = string.Empty;
		object _writeLock = new object();
		readonly object _dictLock = new object();
		int _port;
		readonly byte[] _clientBuff = new byte[CLIENT_BUFF_SIZE];
		bool _relaySubscribe = false;
		bool _lastTcpConnectionIsOkForRead = false;
		bool _digitalInputSubscribe = false;
		bool _alarmSubscribe = false;
		TcpClient _tcpClient = null;
		readonly DateTime _TimeOfLastLogicalConnectedToController = new DateTime(1948, 8, 24);
		ProcessMidDelegate _processMidDelegate = null;
		ProcessCommStatusDelegate _processCommStatusDelegate = null;
		NetworkStream _clientStream = null;
		//[Obsolete("find references to this field", true)]
		//Logger _logger;
		[Obsolete("remove this", true)]
		List<Subscriptions> _subscriptions = new List<Subscriptions>();
		//IDictionary<string, string> _subscriptionsToBeAcked = new Dictionary<string, string>();
		DisplayStatusDelegate _displayStatusDelegate = null;
		//DateTime _dastMessage = new DateTime(1900, 1, 1);
		bool _TryingToConnectInProgress = false;
		//TextWriter midLogFile;
		readonly FileStream _midLogStream;
		TextWriter _midLogFile;
		static readonly object midLogLock = new object();
		#endregion


		static MyController(){
			string asmName = Assembly.GetEntryAssembly().GetName().Name;
			//logPath=
			logFilePath = Path.Combine(
				Environment.GetEnvironmentVariable("TEMP"),
				asmName);
		}

		public MyController() {
			string  logName, asmName = Assembly.GetEntryAssembly().GetName().Name,tmp;
			string[] logFiles;
			int fileno;

			//logPath = Path.Combine(
			//	Environment.GetEnvironmentVariable("TEMP"),
			//	asmName);
			logFiles = Directory.GetFiles(logFilePath, asmName + "_*.data.log");
			List<string> logFileList = new List<string>(logFiles);
			logFileList.Sort();
			//Trace.WriteLine("here");
			logName = "test";
			if (logFileList.Count<1)
				fileno=-1;
			else {
				tmp = Path.GetFileNameWithoutExtension(logFileList[logFileList.Count - 1]).Substring((asmName + "_").Length, 4);
				if (!Int32.TryParse(tmp, out fileno))
					fileno = -1;
			}
			Trace.WriteLine("here");
			logName = Path.Combine(logFilePath, asmName + "_" + (fileno+1).ToString("000#") + ".data.log");


			lock (midLogLock) {
				_midLogStream = new FileStream(logName, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite);
				_midLogFile = new StreamWriter(_midLogStream);
				//_midLogFile.fl
				//midLogFile = new StreamWriter(logName,));
			}
		}

		//~MyController() {
		//	if (midLogFile != null)
		//		lock (midLogLock) {
		//			midLogFile.Flush();
		//			midLogFile.Close();
		//			midLogFile = null;
		//		}
		//}

		//public static bool veryVerbose = false;

		// CTOR

		// PROPERTIES

		#region properties
		public bool DisplayKeepAliveMessage { get; set; }

		public static bool veryVerbose { get { return _verVerbose; } set { _verVerbose = value; } }

		public string RemoteEndPoint {
			get {
				if (_tcpClient == null)
					return string.Empty;
				return _tcpClient.Client.RemoteEndPoint.ToString();
			}
		}
		[Obsolete("remove this", true)]
		public void AddLastTighteningResultSubscription() {
			if (veryVerbose)
				Utility.logger.log(ColtLogLevel.Info, "AddLastTighteningResultSubscription()");
			_subscriptions.Add(Subscriptions.LastTighteningResult);
		}

		[Obsolete("remove this", true)]
		public bool AlarmSubscribe {
			get { return _alarmSubscribe; }
			set {
				try {
					_alarmSubscribe = value;
					if (_alarmSubscribe) _subscriptions.Add(Subscriptions.Alarm);
					else _subscriptions.Remove(Subscriptions.Alarm);
				} catch (Exception ex) {
					Utility.logger.log(MethodBase.GetCurrentMethod(), ex);
					//Utility.logger.log(ColtLogLevel.Error,(string.Format("AlarmSubscribe ex:{0}", ex.Message));
				}
			}
		}

		[Obsolete("remove this", true)]
		public bool RelaySubscribe {
			get { return _relaySubscribe; }
			set {
				try {
					_relaySubscribe = value;
					if (_relaySubscribe) _subscriptions.Add(Subscriptions.Relay);
					else _subscriptions.Remove(Subscriptions.Relay);
				} catch (Exception ex) {
					Utility.logger.log(MethodBase.GetCurrentMethod(), ex);
				}
			}
		}

		[Obsolete("remove this", true)]
		public bool DigitalInputSubscribe {
			get { return _digitalInputSubscribe; }
			set {
				try {
					_digitalInputSubscribe = value;
					if (_digitalInputSubscribe) _subscriptions.Add(Subscriptions.DigitalInput);
					else _subscriptions.Remove(Subscriptions.DigitalInput);
				} catch (Exception ex) {
					Utility.logger.log(MethodBase.GetCurrentMethod(), ex);
					//Utility.logger.log(ColtLogLevel.Error,(string.Format("DigitalInputSubscribe ex:{0}", ex.Message));
				}
			}
		}

		public static string logFilePath { get; private set; }
		#endregion

		#region public methods
		// PUBLIC METHODS

		/// <summary>Overload without Logger-instance.</summary>
		/// <param name="ipAddress"></param>
		/// <param name="port"></param>
		/// <param name="processMidDelegate"></param>
		/// <param name="displayStatusDelegate"></param>
		/// <param name="processCommStatusDelegate"></param>
		/// <returns></returns>
		public bool initialize(string ipAddress, int port, ProcessMidDelegate processMidDelegate,
			DisplayStatusDelegate displayStatusDelegate, ProcessCommStatusDelegate processCommStatusDelegate) {
			return initialize(ipAddress, port, processMidDelegate, displayStatusDelegate, processCommStatusDelegate, null);
		}
		//IColtLogger _logger;
		public bool initialize(string ipAddress, int port, ProcessMidDelegate processMidDelegate,
			DisplayStatusDelegate displayStatusDelegate, ProcessCommStatusDelegate processCommStatusDelegate, IColtLogger logger) {

			try {
				//_logger = logger;
				Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(), string.Format("{0} {1} {2} {3} {4} {5}", ipAddress, port, processMidDelegate, displayStatusDelegate, processCommStatusDelegate, logger));
				if (_ipAddress != string.Empty) {
					Utility.logger.log(ColtLogLevel.Error, MethodBase.GetCurrentMethod(), "Initialize Can Only Be Called Once");
					//Utility.logger.log(ColtLogLevel.Error,("Initialize() ERROR: Initialize Can Only Be Called Once");
					return false;
				}
				if (string.IsNullOrEmpty(ipAddress)) {
					//Utility.logger.log(MethodBase.GetCurrentMethod(),)
					Utility.logger.log(ColtLogLevel.Error, MethodBase.GetCurrentMethod(), "IP Address is invalid!");
					return false;
				}
				_ipAddress = ipAddress;
				_port = port;
				_processMidDelegate = processMidDelegate;
				_displayStatusDelegate = displayStatusDelegate;
				_processCommStatusDelegate = processCommStatusDelegate;

				Events.GetEvents().FlushLoggerEvent += flushLoggerHandler;

				Task.Run(() => { receiveThread(); });
				Thread.Sleep(1000);
				Task.Run(() => { sendKeepAliveThread(); });
				Task.Run(() => { monitorCommunicationLinkThread(); });
				return true;
			} catch (Exception ex) {
				Utility.logger.log(MethodBase.GetCurrentMethod(), ex);
				//Utility.logger.log(ColtLogLevel.Error,(string.Format("Initialize() ex:{0}", ex.Message));
				return false;
			}
		}

		void flushLoggerHandler(object sender, Events.FlushLoggerEventArgs e) {
			Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(), string.Empty);
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
		// PRIVATE METHODS

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

				// Indicate we are trying to connect - keep other thread from sending Keep-Alive while we're trying to connect
				Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(), "connection in progress");
				_TryingToConnectInProgress = true;
				_tcpClient = new TcpClient(_ipAddress, _port) {
					NoDelay = true
				};
				_clientStream = _tcpClient.GetStream();
				Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(), "Connected");
				processCommunicationStatus(CommStatus.Up);

				// Give receive thread some time to initiate a read
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
		// THREADS

		void monitorCommunicationLinkThread() {
			Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod());
			for (; ; ) {
				try {
					if (_tcpClient == null) connect();
				} catch (Exception ex) {
					Utility.logger.log(MethodBase.GetCurrentMethod(), ex);
				}
				Thread.Sleep(1000);
			}
		}

		void sendKeepAliveThread() {
			MethodBase mb = MethodBase.GetCurrentMethod();
			DateTime now;
			MID_9999 mid;
			string package;
			byte[] command;

			Utility.logger.log(ColtLogLevel.Info, mb);
			for (; ; ) {
				try {
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
						mid = new MID_9999();
						command = Encoding.ASCII.GetBytes(package = mid.buildPackage() + "\0");
						lock (_writeLock) {
							_lastMessage = DateTime.Now;
							_clientStream.Write(command, 0, command.Length);
							if (veryVerbose)
								Utility.logger.log(ColtLogLevel.Info, mb, mid.GetType().Name + " Sent");
						}
					}
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
			byte[] command;

			if (veryVerbose)
				Utility.logger.log(ColtLogLevel.Info, mb);
			for (; ; ) {
				try {
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
								//_midLogFile.
								_midLogFile.WriteLine(package);
								_midLogFile.Flush();
								_midLogStream.Flush();
							}
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
							// WHEN WE ARE DISCONNECTED FROM THE CONTROLLER WE HAVE NO WAY OF KNOWING IF IT'S A NETWORK ISSUE
							// OR IF THE CONTROLLER HAS BEEN SHUT OFF - THEREFORE WE HAVE PUT IN LOGIC THAT IF WE ARE DISCONNECTED
							// FROM THE CONTROLLER FOR MORE THAN 5 MINUTES - WE ASSUME THE CONTROLL MUST HAVE BEEN TURNED OFF
							// THIS DISTINCTION IS IMPORTANT BECAUSE IF INDEED THE CONTROLLER POWER HAS BEEN TURNED OFF,
							// WE NEED TO SUBSCRIBE TO DATA AGAIN

							// Have we been logically disconnected more that 5 minutes - (Controller has been turned off)
							if (now.Subtract(_TimeOfLastLogicalConnectedToController) > TimeSpan.FromMinutes(5)) {

								// Yes
								sendMid(new MID_0034()); // job-info subscribe
								sendMid(new MID_0051()); // vehicle-ID subscribe
								sendMid(new MID_0060()); // Last tightening subscription
								sendMid(new MID_0070()); // Alarm subscription
								sendMid(new MID_0151()); // multi ident part subscribe
								sendMid(new MID_0210()); // external-inputs subscription
								sendMid(new MID_0216()); // "relay"? subscription
								sendMid(new MID_0220()); // "digital input"? subscription
														 /*
														 if (_subscriptions.Contains(Subscriptions.LastTighteningResult)) {
															 //subscribeToLastTighteningResult();
															 sendMid(subscribeLastTightening_0060());
															 //subscribeToLastTighteningResult(package, ref _writeLock, ref _lastMessage, ref _clientStream);
														 }
														 if (_subscriptions.Contains(Subscriptions.Alarm)) {
															 sendMid(createAlarmSubscription_0070());
															 //subscribeToAlarm(package);
														 }
														 if (_subscriptions.Contains(Subscriptions.Relay)) {
															 sendMid(createRelay_0216());
															 //MID_0216 mid0216 = new MID_0216();
															 //package = mid0216.buildPackage() + "\0";
															 //command = Encoding.ASCII.GetBytes(package);
															 ////_SubscriptionsToBeAcked.Add("0070", "0070");
															 //lock (_writeLock) {
															 //	_lastMessage = DateTime.Now;
															 //	_clientStream.Write(command, 0, command.Length);
															 //	//if (veryVerbose)
															 //	Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(), "MID0216 Sent");
															 //}
														 }
														 if (_subscriptions.Contains(Subscriptions.DigitalInput)) {
															 sendMid(createDigitalInput_0220());
															 //MID_0220 mid0220 = new MID_0220();
															 //package = mid0220.buildPackage() + "\0";
															 //command = Encoding.ASCII.GetBytes(package);
															 ////_SubscriptionsToBeAcked.Add("0070", "0070");
															 //lock (_writeLock) {
															 //	_lastMessage = DateTime.Now;
															 //	_clientStream.Write(command, 0, command.Length);
															 //	//if (veryVerbose)
															 //	Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(), "MID0220 Sent");
															 //}
														 }
														 */

								//MID m = () => {
								//	return  new MID_0034();
								//	//ret.processPackage
								//};
								//sendMid(() => new MID_0034());
								//sendMid
							}
							// Ask the controller all the valid job numbers
							MID_0030 mid0030 = new MID_0030();
							package = mid0030.buildPackage() + "\0";
							command = Encoding.ASCII.GetBytes(package);
							lock (_writeLock) {
								_lastMessage = DateTime.Now;
								_clientStream.Write(command, 0, command.Length);
								//if (veryVerbose)
								Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(), "MID0030 Sent");
							}
							//displayStatusDelegate.BeginInvoke(string.Format("ReceiveThread() [{0}] written {1}", package, command.Length), null, null);
							break;
						case "0004": handleCommandError_0004(package); break;
						case "0005": handleCommandAccepted_0005(package); break;
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
							//MID_0062 mid0062 = new MID_0062();
							//package = mid0062.buildPackage() + "\0";
							//command = Encoding.ASCII.GetBytes(package);
							//lock (_writeLock) {
							//	_lastMessage = DateTime.Now;
							//	_clientStream.Write(command, 0, command.Length);
							//	//if (veryVerbose)
							//	Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(), "MID0062 Sent");
							//}
							break;
						case "0071": handleAlarm_0071(package, processObject); sendMid(createAlarmAcknowledgement()); break;
						case "0074": handleControllerAlarmAck(package, processObject, displayStatus); sendMid(createControllerAlarmAcknowledged()); break;
						case "0076": handleAlarmStatus(package); break;
						case "0152": handleMultiIdentAndParts_0152(package); sendMid(new MID_0153()); break;
						case "0211": handleExternalInputs_0211(package); sendMid(new MID_0212()); break;
						case "9999": handleKeepAlive(package, processObject); break;
						default: displayStatus(ColtLogger.makeSig(mb) + " Unsupported package received [" + package.Substring(4, 4) + "]"); break;
					}
				} catch (SocketException exSock) {
					Utility.logger.log(MethodBase.GetCurrentMethod(), exSock);
					//Utility.logger.log(ColtLogLevel.Error,(string.Format("ReceiveThread() Socket ex: {0}", exSock.Message));
					close();
				} catch (Exception ex) {
					Utility.logger.log(MethodBase.GetCurrentMethod(), ex);
					//Utility.logger.log(ColtLogLevel.Error,(string.Format("ReceiveThread() ex: {0}", ex.Message));
					close();
				}
			}
		}


		void handleExternalInputs_0211(string package) {
			//MID_0211 mid = new MID_0211();
			//MID mid=new		 MID()
			//mym
			MyMid_211 mid = new MyMid_211();

			mid.processPackage(package);

			Utility.logger.log(MethodBase.GetCurrentMethod());
		}

		void handleMultiIdentAndParts_0152(string package) {
			MyMid_152 mid = new MyMid_152();

			mid.processPackage(package);

			Utility.logger.log(MethodBase.GetCurrentMethod());
		}

		/// <summary>do something</summary>
		/// <returns></returns>
		/// <remarks><para>MID_0220</para></remarks>
		MID createDigitalInput_0220() {
			return new MID_0220();
			//throw new NotImplementedException();
		}

		/// <summary>do something</summary>
		/// <returns></returns>
		/// <remarks><para>MID_0004</para></remarks>
		static void handleCommandError_0004(string package) {
			MID_0004 mid0004 = new MID_0004();

			mid0004.processPackage(package);
			//if (veryVerbose)
			Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(), "Command Error ErrorCode:" + mid0004.ErrorCode + ".");
		}

		static void handleCommandAccepted_0005(string package) {
			MID_0005 mid = new MID_0005();
			string blah;
			MethodBase mb = MethodBase.GetCurrentMethod();

			mid.processPackage(package);
			//if (veryVerbose)
			Utility.logger.log(ColtLogLevel.Info, mb, "Command Accepted: " + mid.MIDAccepted + ".");

			//if (veryVerbose) {
			blah = package.Substring(20, 4);
			if (blah == "0018") {
				Utility.logger.log(ColtLogLevel.Info, "MID0018 Accepted");    // Set Job Id Number accepted
			} else if (blah == "0031") {
				Utility.logger.log(ColtLogLevel.Info, "MID0031 Accepted");    // Request all valid job numbers accepted
			}
			//}
		}

		/// <summary>do something</summary>
		/// <returns></returns>
		/// <remarks><para>MID_0216</para></remarks>
		MID createRelay_0216() { return new MID_0216(); }

		/// <summary>do something</summary>
		/// <returns></returns>
		/// <remarks><para>MID_0062</para></remarks>
		MID createMid0062() { return new MID_0062(); }

		/// <summary>do something</summary>
		/// <returns></returns>
		/// <remarks><para>process MID_0072</para></remarks>
		static void handleAlarm_0071(string package, ProcessMidDelegate po) {
			MID_0071 mid = new MID_0071();

			mid.processPackage(package);
			po(MessageType.AlarmUpload, mid, package);
		}

		/// <summary>do something</summary>
		/// <returns></returns>
		/// <remarks><para>MID_0070</para></remarks>
		MID createAlarmSubscription_0070() {
			//throw new NotImplementedException();
			//subscribeToAlarm
			return new MID_0070();
		}


		/// <summary>do something</summary>
		/// <returns></returns>
		/// <remarks><para>MID_0072</para></remarks>
		MID createAlarmAcknowledgement() {
			return new MID_0072();
			//MID_0072 mid0072 = new MID_0072();
			//package = mid0072.buildPackage() + "\0";
			//command = Encoding.ASCII.GetBytes(package);
			////_SubscriptionsToBeAcked.Add("0072", "0072");
			//lock (_writeLock) {
			//	_lastMessage = DateTime.Now;
			//	_clientStream.Write(command,KW 0, command.Length);
			//	if (veryVerbose)
			//		Utility.logger.log(ColtLogLevel.Info, "ReceiveThread() MID0072 Sent");
			//}
		}

		static void handleControllerAlarmAck(string package, ProcessMidDelegate po, DisplayStatusDelegate ds) {
			MID_0074 mid = new MID_0074();

			mid.processPackage(package);
			po(MessageType.AlarmAcknowledgeTorqueController, mid, package);
			if (veryVerbose)
				ds("Error: [" + mid.ErrorCode + "]");
		}

		void sendMid(MID mid) {
			//MID_0075 mid0075 = new MID_0075();
			string package = mid.buildPackage() + "\0";
			byte[] command = Encoding.ASCII.GetBytes(package);
			//_SubscriptionsToBeAcked.Add("0072", "0072");
			lock (_writeLock) {
				_lastMessage = DateTime.Now;
				_clientStream.Write(command, 0, command.Length);
				if (veryVerbose)
					Utility.logger.log(ColtLogLevel.Info, mid.GetType().Name + " Sent");
			}
		}

		/// <summary>do something</summary>
		/// <returns></returns>
		/// <remarks><para>MID_0075</para></remarks>
		MID createControllerAlarmAcknowledged() { return new MID_0075(); }

		//static void handleAlarmStatus(string package, ProcessMidDelegate pmd, DisplayStatusDelegate dsd, ref object objLock, ref DateTime dtLastMsg, ref NetworkStream clStream) {
		void handleAlarmStatus(string package) {
			MID_0076 mid = new MID_0076();

			mid.processPackage(package);
			processObject(MessageType.AlarmStatus, mid, package);

			if (!string.IsNullOrEmpty(mid.AlarmStatusData.ErrorCode)) {
				displayStatus("Controller Status: " + mid.AlarmStatusData.ControllerReadyStatus + ", Alarm Error: " + mid.AlarmStatusData.ErrorCode + ".");
			} else {
				displayStatus("Controller Status: " + mid.AlarmStatusData.ControllerReadyStatus + ".");
			}
			//acknowledgeAlarmStatus(ref objLock, ref dtLastMsg, ref clStream);
			//send

			// 
			sendMid(createAckAlarmStatus_0077());
		}

		//[Obsolete("replace this",true)]
		//static void acknowledgeAlarmStatus(ref object _writeLock, ref DateTime _lastMessage, ref NetworkStream _clientStream) {
		//	MID_0077 mid = new MID_0077();
		//	string package = mid.buildPackage() + "\0";
		//	byte[] command = Encoding.ASCII.GetBytes(package);

		//	lock (_writeLock) {
		//		_lastMessage = DateTime.Now;
		//		_clientStream.Write(command, 0, command.Length);
		//		Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(), mid.GetType().Name + " Sent");
		//	}
		//}
		/// <summary>do something</summary>
		/// <returns></returns>
		/// <remarks><para>MID_0077</para></remarks>
		static MID createAckAlarmStatus_0077() { return new MID_0077(); }

		//static void subscribeToAlarm(string package, ref object objLock, ref DateTime lastMsgSent, ref NetworkStream clStream) {
		//	MID_0070 mid = new MID_0070();
		//	byte[] command = Encoding.ASCII.GetBytes(package = mid.buildPackage() + "\0");

		//	lock (objLock) {
		//		lastMsgSent = DateTime.Now;
		//		clStream.Write(command, 0, command.Length);
		//		Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(), mid.GetType().Name + " Sent.");
		//	}
		//}

		static MID subscribeLastTightening_0060() { return new MID_0060(); }

		//[Obsolete("replace this", true)]
		//static void subscribeToLastTighteningResult(string package, ref object objLock, ref DateTime lastMsgSent, ref NetworkStream clStream) {
		//	MID_0060 mid0060 = new MID_0060();
		//	byte[] command;

		//	command = Encoding.ASCII.GetBytes(package = mid0060.buildPackage() + "\0");
		//	lock (objLock) {
		//		lastMsgSent = DateTime.Now;
		//		clStream.Write(command, 0, command.Length);
		//		Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(), mid0060.GetType().Name + " Sent.");
		//	}
		//}

		static void handleKeepAlive(string package, ProcessMidDelegate processObject) {
			//if (veryVerbose)
			//	Utility.logger.log(ColtLogLevel.Info, "ReceiveThread() Keep Alive Received");
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
		bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				if (disposing) {
					// TODO: dispose managed state (managed objects).
					if (_midLogFile != null) {
						lock (midLogLock) {
							_midLogFile.Flush();
							_midLogFile.Close();
							_midLogFile = null;
						}
					}
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~MyController() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose() {
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			GC.SuppressFinalize(this);
		}
		#endregion

	}
}