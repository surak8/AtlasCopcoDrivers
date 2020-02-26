using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
//using OpenProtocolInterpreter.MIDs;
//using OpenProtocolInterpreter.MIDs;
//using OpenProtocolInterpreter.MIDs.ApplicationToolLocationSystem;
//using OpenProtocolInterpreter.MIDs.OpenProtocolCommandsDisabled;

namespace NSAtlasCopcoBreech {
	public partial class MyController {
#if OTHER_VERSION
		class MidData { }
		  void writeAsCSV(object aMid) {
			throw new NotImplementedException();
		}

		  void handlePackage(MethodBase mb, string package) {
			throw new NotImplementedException();
		}
		  void sendCommunicationStart(ref object writeLock, ref DateTime lastMessage, ref NetworkStream clientStream, ref bool tryingToConnectInProgress, ref TcpClient tcpClient) {
			throw new NotImplementedException();
		}
		  void sendKeepAlive() {
			throw new NotImplementedException();
		}
		void unsubscribeFromEvents() {
			throw new NotImplementedException();
		}
#else
		class MidData {
		#region fields
			OpenProtocolInterpreter.MIDs.MID _mid;

		#endregion
		#region ctor
			public MidData(int tid) { tighteningID=tid; }

		#endregion
		#region properties
			public bool dataReceived { get; private set; }

			public OpenProtocolInterpreter.MIDs.MID mid { get { return _mid; } set { _mid=value; dataReceived=mid!=null; } }
			public int tighteningID { get; }

		#endregion
		#region methods
			internal void reset() {
				dataReceived=false;
				mid=null;
			}
		#endregion
		}

		CSVGenerator<OpenProtocolInterpreter.MIDs.MIDIdentifier, OpenProtocolInterpreter.MIDs.MID> _csvGen;

		void writeAsCSV(OpenProtocolInterpreter.MIDs.MID mid) {
			if (_csvGen==null) {
				_csvTighteningName=Path.Combine(
					MIDUtil.midLogPath,
					Assembly.GetEntryAssembly().GetName().Name+
					"CurrentTighteningData.log");
				lock (_csvLock) {

					_csvGen=new CSVGenerator<OpenProtocolInterpreter.MIDs.MIDIdentifier, OpenProtocolInterpreter.MIDs.MID>();

				}
			}
			lock (_csvLock) {
				if (_csvLogStream==null||_csvLogFile==null) {
					//_csvWroteCSVHeader=false;
					if (File.Exists(_csvTighteningName))
						_csvLogStream = new FileStream(_csvTighteningName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
					else
						_csvLogStream = new FileStream(_csvTighteningName, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite);
					_csvLogFile = new StreamWriter(_csvLogStream);
				}
			}

			lock (_csvLock) {
				_csvGen.writeCSVOutputForLine(
				mid.HeaderData.Mid,
				ref _csvWroteCSVHeader,
				mid.buildPackage(), _csvLogFile);
			}
			lock (_csvLock) {
				_csvLogFile.Flush();
				_csvLogStream.Flush();
			}
		}
		void setupSubscriptions(DateTime now) {
			if (now.Subtract(_TimeOfLastLogicalConnectedToController) > TimeSpan.FromMinutes(5)) {
				/* */
				sendMid(new OpenProtocolInterpreter.MIDs.ParameterSet.MID_0010()); // PSET id request;
				_initialTighteningData=true;
				sendMid(new OpenProtocolInterpreter.MIDs.Tightening.MID_0064()); // old tightening results (0 means: most recent); *** capture this *** _lastTighteningID
				_initialTighteningData=false;

				sendMid(new OpenProtocolInterpreter.MIDs.ParameterSet.MID_0014()); // PSET subscribe, respond with MID_0016
#warning removed MID_0021
				//sendMid(new MID_0021()); // lock-at-batch-done
				sendMid(new OpenProtocolInterpreter.MIDs.Job.MID_0034());    // Job info subscribe, 

				sendMid(new OpenProtocolInterpreter.MIDs.VIN.MID_0051());    // VIN subscribe
				sendMid(new OpenProtocolInterpreter.MIDs.Tightening.MID_0060());    // Last tightening
				sendMid(new OpenProtocolInterpreter.MIDs.Alarm.MID_0070());    // Alarm subscribe

				// Command Error ErrorCode:CONTROLLER_IS_NOT_A_SYNC_MASTER_OR_STATION_CONTROLLER for MID=90.
				//sendMid(new MID_0090());    // MultiSpindle status subscribe

#warning MID_0100 is missing?
				//sendMid(new MID_0100());    // MultiSpindle result subscribe

				// Command Error ErrorCode:UNKNOWN_MID for MID=105.
				//sendMid(new MID_0105());    // PowerMACS result subscribe

				sendMid(new OpenProtocolInterpreter.MIDs.Job.Advanced.MID_0120());    // Job line control info subscribe
				sendMid(new OpenProtocolInterpreter.MIDs.MultipleIdentifiers.MID_0151());    // multi-ident result parts subscribe
				sendMid(new OpenProtocolInterpreter.MIDs.IOInterface.MID_0210());    // extern inputs subscribe
				sendMid(new OpenProtocolInterpreter.MIDs.IOInterface.MID_0216());    // relay function subscribe
				sendMid(new OpenProtocolInterpreter.MIDs.IOInterface.MID_0220());    // digital input subscribe

				// Command Error ErrorCode:UNKNOWN_MID for MID=241.
				//sendMid(new MID_0241());    // user-data subscribe

				sendMid(new OpenProtocolInterpreter.MIDs.ApplicationSelector.MID_0250());    // selector-socket subscribe
				sendMid(new OpenProtocolInterpreter.MIDs.ApplicationToolLocationSystem.MID_0261());    // tool tag id subscribe

				// Command Error ErrorCode:UNKNOWN_MID for MID=400.
				//sendMid(new MID_0400());    // auto/manual mode id subscribe

				sendMid(new OpenProtocolInterpreter.MIDs.OpenProtocolCommandsDisabled.MID_0420());    // open proto disable subscribe

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
			sendMid(new OpenProtocolInterpreter.MIDs.Job.MID_0030());
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
		}

		void handleCommandAccepted_0015(string package) {
			var mid=new OpenProtocolInterpreter.MIDs.ParameterSet.MID_0015();

			mid.processPackage(package);
			Utility.logger.log(MethodBase.GetCurrentMethod());
			sendMid(new OpenProtocolInterpreter.MIDs.ParameterSet.MID_0016());
			if (mid.ParameterSetID==0) {
				Utility.logger.log(ColtLogLevel.Debug, MethodBase.GetCurrentMethod(), "ParmSet="+mid.ParameterSetID+". Creating a new log-file.");
				createNewLogFile();
			}
		}
		void sendMid(OpenProtocolInterpreter.MIDs.MID mid) {
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
		OpenProtocolInterpreter.MIDs.MID createControllerAlarmAcknowledged() { return new OpenProtocolInterpreter.MIDs.Alarm.MID_0075(); }
		void handleAlarmStatus(string package) {
			OpenProtocolInterpreter.MIDs.Alarm.MID_0076 mid = new OpenProtocolInterpreter.MIDs.Alarm.MID_0076();
			mid.processPackage(package);
			processObject(MessageType.AlarmStatus, mid, package);
			if (!string.IsNullOrEmpty(mid.AlarmStatusData.ErrorCode)) {
				displayStatus("Controller Status: " + mid.AlarmStatusData.ControllerReadyStatus + ", Alarm Error: " + mid.AlarmStatusData.ErrorCode + ".");
			} else {
				displayStatus("Controller Status: " + mid.AlarmStatusData.ControllerReadyStatus + ".");
			}
			sendMid(createAckAlarmStatus_0077());
		}
		static OpenProtocolInterpreter.MIDs.MID createAckAlarmStatus_0077() { return new OpenProtocolInterpreter.MIDs.Alarm.MID_0077(); }
		static OpenProtocolInterpreter.MIDs.MID subscribeLastTightening_0060() { return new OpenProtocolInterpreter.MIDs.Tightening.MID_0060(); }
		static void handleKeepAlive(string package, ProcessMidDelegate processObject) {
			var mid = new OpenProtocolInterpreter.MIDs.KeepAlive.MID_9999();
			mid.processPackage(package);
			if (veryVerbose)
				Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(), mid.ToString());
			processObject(MessageType.KeepAlive, mid, package);
		}
		static void captureVehicleID(string package) {
			var mid = new OpenProtocolInterpreter.MIDs.VIN.MID_0052();
			mid.processPackage(package);
			Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(), mid.ToString());
		}
		static void captureJobNumber(string package) {
			var mid = new OpenProtocolInterpreter.MIDs.Job.MID_0031();
			mid.processPackage(package);
			foreach (int jobId in mid.JobIds)
				Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(), "Job Id: " + jobId + ".");
		}


		void handle_0065(string package) {
			var  mid=new  OpenProtocolInterpreter.MIDs.Tightening.MID_0065();
			int tid;

			mid.processPackage(package);
			if (_initialTighteningData)
				_lastTighteningID=mid.TighteningID;
			else {
				// look in the list and capture the data.
				lock (_tighteningLock) {
					if (_tighteningMap.ContainsKey(tid=mid.TighteningID)) {
						_tighteningMap[tid].mid=mid;
					}
				}
			}

			MIDUtil.showMidDetail(mid, package);
		}

		static void handleCommandAccepted_0013(string package) {
			var mid=new OpenProtocolInterpreter.MIDs.ParameterSet.MID_0013();
			StringBuilder sb=new StringBuilder();

			mid.processPackage(package);
			MIDUtil.showMidDetail(mid, package);
			//MIDUtil.showMidDetail()
			//sb.AppendLine(mid.
			//mid13.
			//break;
			//handleCommandAccepted_0011(package);
		}

		void handleCommandAccepted_0011(string package) {
			var mid=new OpenProtocolInterpreter.MIDs.ParameterSet.MID_0011();
			StringBuilder sb=new StringBuilder();
			List<int> psets=new List<int>();
			int n;

			mid.processPackage(package);
			//Trace.WriteLine("here");
			if ((n=mid.TotalParameterSets)>0) {
				sb.AppendLine("have "+n+" psets.");
				for (int i = 0; i<n; i++) {
					if (i>0)
						sb.Append(", ");
					sb.Append(mid.ParameterSets[i]);
					psets.Add(mid.ParameterSets[i]);
				}
				sb.AppendLine();
			} else {
				sb.AppendLine("NO PSETS!");
			}
			Utility.logger.log(ColtLogLevel.Debug, MethodBase.GetCurrentMethod(), sb.ToString());
			//if (psets.Count>0) {
			//	MID_0012 psMid=new MID_0012();

			//	foreach (int aPSet in psets) {
			//		psMid.ParameterSetID=aPSet;
			//		//psMid.
			//		sendMid(psMid);
			//	}
			//}
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
		//OpenProtocolInterpreter.MIDs.MID createDigitalInput_0220() {
		//	return new MID_0220();
		//}
		static void handleCommandError_0004(string package) {
			var mid0004 = new OpenProtocolInterpreter.MIDs.Communication.MID_0004();
			mid0004.processPackage(package);
			Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(), "Command Error ErrorCode:" + mid0004.ErrorCode + " for MID="+mid0004.FailedMid+".");
		}
		static void handleCommandAccepted_0005(string package) {
			var mid = new OpenProtocolInterpreter.MIDs.Communication.MID_0005();
			//string blah;
			MethodBase mb = MethodBase.GetCurrentMethod();

			mid.processPackage(package);

			Utility.logger.log(ColtLogLevel.Info, mb, "accepted MID="+mid.MIDAccepted+".");
			if (mid.MIDAccepted== 3) {
				Utility.logger.log(ColtLogLevel.Debug, mb, "comm-shutdown succeeded.");
				_mreShutdown.Reset();
			}
		}
		OpenProtocolInterpreter.MIDs.MID createRelay_0216() { return new OpenProtocolInterpreter.MIDs.IOInterface.MID_0216(); }
		OpenProtocolInterpreter.MIDs.MID createMid0062() { return new OpenProtocolInterpreter.MIDs.Tightening.MID_0062(); }
		static void handleAlarm_0071(string package, ProcessMidDelegate po) {
			var mid = new OpenProtocolInterpreter.MIDs.Alarm.MID_0071();
			mid.processPackage(package);
			po(MessageType.AlarmUpload, mid, package);
		}
		//OpenProtocolInterpreter.MIDs.MID createAlarmSubscription_0070() {
		//	return new MID_0070();
		//}
		//OpenProtocolInterpreter.MIDs.MID createAlarmAcknowledgement() {
		//	return new MID_0072();
		//}
		static void handleControllerAlarmAck(string package, ProcessMidDelegate po, DisplayStatusDelegate ds) {
			var mid = new OpenProtocolInterpreter.MIDs.Alarm.MID_0074();
			mid.processPackage(package);
			po(MessageType.AlarmAcknowledgeTorqueController, mid, package);
			if (veryVerbose)
				ds("Error: [" + mid.ErrorCode + "]");
		}

		static void sendCommunicationStart(ref object objLock, ref DateTime lastMsg, ref NetworkStream clStream, ref bool attemptingConnection, ref TcpClient clTCP) {
			//OpenProtocolInterpreter.MIDs.Communication.MID_0001 mid;
			MethodBase mb;
			string package, midName;
			byte[] command;

			mb = MethodBase.GetCurrentMethod();
			var mid = new OpenProtocolInterpreter.MIDs.Communication.MID_0001();
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

		void sendKeepAlive() {
#if true
			sendMid(new OpenProtocolInterpreter.MIDs.KeepAlive.MID_9999());
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


		void unsubscribeFromEvents() {
			sendMid(new OpenProtocolInterpreter.MIDs.Job.MID_0037()); // unsubscribe job-info
			sendMid(new OpenProtocolInterpreter.MIDs.VIN.MID_0054()); // unsubscribe vehicle
									 // send Communication Stop message.
			sendMid(new OpenProtocolInterpreter.MIDs.Communication.MID_0003());
		}

		void handlePackage(MethodBase mb, string package) {
			switch (package.Substring(4, 4)) {
				case "0002":
					acknowledgeCommunicationStart(package);
					break;
				case "0004": handleCommandError_0004(package); break;
				case "0005": handleCommandAccepted_0005(package); break;
				case "0011": handleCommandAccepted_0011(package); break;
				case "0013":
					handleCommandAccepted_0013(package); // pset-def
					break;
				case "0015": handleCommandAccepted_0015(package); break; // show PSET subscribed
				case "0021": sendMid(new OpenProtocolInterpreter.MIDs.ParameterSet.MID_0023()); break; // notification
				case "0022": break; // look this up!

				case "0031": captureJobNumber(package); break;
				case "0052": captureVehicleID(package); break;
				case "0061":
					 var mid0061 = new OpenProtocolInterpreter.MIDs.Tightening.MID_0061();
					mid0061.processPackage(package);
					_thisTighteningID=mid0061.TighteningID;
					processObject(MessageType.LastTighteningResult, mid0061, package);
					sendMid(createMid0062()); // ack this item.
					writeAsCSV(mid0061);

					generateTighteningRequests();
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
					break;
				case "0065": handle_0065(package); break; // old last tightening.
				case "0071": handleAlarm_0071(package, processObject); sendMid(new OpenProtocolInterpreter.MIDs.Alarm.MID_0072()); break;
				case "0074": handleControllerAlarmAck(package, processObject, displayStatus); sendMid(createControllerAlarmAcknowledged()); break;
				case "0076": handleAlarmStatus(package); break;
				case "0152": handleMultiIdentAndParts_0152(package); sendMid(new OpenProtocolInterpreter.MIDs.MultipleIdentifiers.MID_0153()); break;
				case "0211": handleExternalInputs_0211(package); sendMid(new OpenProtocolInterpreter.MIDs.IOInterface.MID_0212()); break;
				case "9999": handleKeepAlive(package, processObject); break;
				default: displayStatus(ColtLogger.makeSig(mb) + "*** Unsupported package received [" + package.Substring(4, 4) + "] ***"); break;
			}
		}

		//private MID MID_0072() {
		//	throw new NotImplementedException();
		//}

		void acknowledgeCommunicationStart(string package) {
			_TryingToConnectInProgress = false;
			if (veryVerbose)
				Utility.logger.log(ColtLogLevel.Info, "Connect() received MID0002 - connection in progress has completed");
			var mid0002 = new OpenProtocolInterpreter.MIDs.Communication.MID_0002();
			mid0002.processPackage(package);
			processObject(MessageType.CommStartAcknowledge, mid0002, package);
			processCommunicationStatus(CommStatus.Up);
			DateTime now = DateTime.Now;
			setupSubscriptions(now);
		}
#endif
	}
}