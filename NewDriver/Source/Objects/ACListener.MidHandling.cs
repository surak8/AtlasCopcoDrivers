
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Colt.Utility.Logging;

namespace NSNewDriver {
	partial class ACListener {
		bool handleCommunicationStart(object m,string package) {
			int midNo;
			bool ret = false;
			StringBuilder sb;

			switch (midNo=MidFinder.extractMidNumber(m)) {
				case 2:
					object m2 = MidFinder.createMidInstance(package);

					if (_showControllerResponse)
						sb=showMidProperties(m2,_logger);
#if KENNY
					OpenProtocolInterpreter.MIDs.Communication.MID_0002 m2_rik;
					m2_rik=(OpenProtocolInterpreter.MIDs.Communication.MID_0002) m2;
					Trace.WriteLine("here");
					// field 5, 2.5 starts at 64, len=19
#else
#endif

					_commStarted=true;
					ret=true;
					break;
				case 4:
					handleCmdError(MidFinder.createMidInstance(package),package);
					//handleCmdError(package);
					break;
				default:
					_logger.log(MethodBase.GetCurrentMethod(),"unhandled mid "+midNo);
					break;
			}
			return ret;
		}

		bool handleCmdError(object m,string package) {
#if KENNY
			OpenProtocolInterpreter.MIDs.Communication.MID_0004 m4;

			m4=m as OpenProtocolInterpreter.MIDs.Communication.MID_0004;
			_logger.log(
				ColtLogLevel.Debug,
				MethodBase.GetCurrentMethod(),
				"Bad MID="+
				m4.FailedMid+","+
				"Error="+
				m4.ErrorCode);
			return true;
#else
			showMidProperties(m,_logger);
			_logger.log(MethodBase.GetCurrentMethod(),"non-Kenny!");
			throw new InvalidOperationException("non-Kenny!");
			return false;
#endif
		}


		bool handleTighteningResult(object m,string package) {
#if KENNY
			OpenProtocolInterpreter.MIDs.Tightening.MID_0061 m61;

			m61=m as OpenProtocolInterpreter.MIDs.Tightening.MID_0061;
			sendMessage(MidFinder.createNewMid(62),null);
			showMidProperties(m61,_logger);
			return true;
#else
			showMidProperties(m,_logger);
			_logger.log(MethodBase.GetCurrentMethod(),"non-Kenny!");
			throw new InvalidOperationException("non-Kenny!");
			return false;
#endif
			//return false;
		}


		bool _amHandlingAlarm = false;

		bool handleAlarm(object m,string package) {
#if KENNY
			if (_amHandlingAlarm)
				return false;
			OpenProtocolInterpreter.MIDs.Alarm.MID_0071 m71;
			m71=new OpenProtocolInterpreter.MIDs.Alarm.MID_0071();
			_amHandlingAlarm=true;
			sendMessage(MidFinder.createNewMid(72),null,false);
			_amHandlingAlarm=false;
			showMidProperties(m71,_logger);
			return true;
#else
#endif
		}

		bool handleDisableACCommands(object m,string package) {
			// send 422 in response to 421
#if KENNY
			OpenProtocolInterpreter.MIDs.OpenProtocolCommandsDisabled.MID_0421 m421;
			m421=m as OpenProtocolInterpreter.MIDs.OpenProtocolCommandsDisabled.MID_0421;

			sendMessage(MidFinder.createNewMid(422),null);
			showMidProperties(m421,_logger);
			return true;
#else
			showMidProperties(m,_logger);
			_logger.log(MethodBase.GetCurrentMethod(),"non-Kenny!");
			throw new InvalidOperationException("non-Kenny!");
			return false;
#endif
			//return false;
		}

		bool handleMultiIdentResults(object m,string package) {
			// send 153 in response to 152
#if KENNY
			OpenProtocolInterpreter.MIDs.MultipleIdentifiers.MID_0152 m152;
			m152=m as OpenProtocolInterpreter.MIDs.MultipleIdentifiers.MID_0152;
			sendMessage(MidFinder.createNewMid(153),null);
			Trace.WriteLine(m152);
#else
#endif
			return false;
		}

		bool handleExtInputStatus(object m,string package) {
			// send 211 in response to 210
#if KENNY
			sendMessage(MidFinder.createNewMid(212),null);
			OpenProtocolInterpreter.MIDs.IOInterface.MID_0211 m211;
			m211=m as OpenProtocolInterpreter.MIDs.IOInterface.MID_0211;
			Trace.WriteLine("here");
#else
#endif
			return false;
		}
		bool handleJobIDReply(object m,string package) {
			_logger.log(MethodBase.GetCurrentMethod());
			if (m!=null)
				showMidProperties(m,_logger);
			return false;
		}

		//void handleErrorMid(object m) {
		//	throw new NotImplementedException();
		//}

		bool handleKeepAlive(object m,string package) {

			if (veryVerbose)
				_logger.log(MethodBase.GetCurrentMethod(),"found: "+MidFinder.extractMidContent(m));
			if (MidFinder.extractMidNumber(m)==9999)
				_lastKeepAlive=DateTime.Now;
			return false;
		}

		bool handlePSetList(object m,string package) {

#if KENNY
			OpenProtocolInterpreter.MIDs.ParameterSet.MID_0011 m11;
			StringBuilder sb = new StringBuilder();
			int n, nindex;

			m11=m as OpenProtocolInterpreter.MIDs.ParameterSet.MID_0011;
			sb.Append("have "+(n=m11.TotalParameterSets)+" psets");
			if (n<1)
				sb.Append(".");
			else {
				//_logger.log(MethodBase.GetCurrentMethod(), "have " + m11.TotalParameterSets + " psets");
				sb.AppendLine(": ");
				nindex=0;
				foreach (var avar in m11.ParameterSets) {
					if (nindex>0)
						sb.Append(",");
					sb.Append(avar);
					nindex++;
				}
				sb.AppendLine();
			}
			_logger.log(MethodBase.GetCurrentMethod(),sb.ToString());
#else
			Trace.WriteLine("here");
			showMidProperties(m);
#endif
			return true;
		}

		bool handleParmSetSeLected(object m,string package) {
			// respond with MID 16
			//_logger.log(MethodBase.GetCurrentMethod());
#if KENNY
			OpenProtocolInterpreter.MIDs.ParameterSet.MID_0015 m15;
			m15=m as OpenProtocolInterpreter.MIDs.ParameterSet.MID_0015;
			//System.Diagnostics.Trace.WriteLine("here");
			_logger.log(
					ColtLogLevel.Debug,
					MethodBase.GetCurrentMethod(),
					"Pset="+m15.ParameterSetID+", When="+m15.LastChangeInParameterSet);

			//showMidProperties(m);
			sendMessage(MidFinder.createNewMid(16),null);
#else
			_logger.log(MethodBase.GetCurrentMethod());
#endif
			return false;
		}

		internal void subscribeVIN() {
			_logger.log(
				ColtLogLevel.Debug,
				MethodBase.GetCurrentMethod());
			//)
			enqueueMid(MidFinder.createNewMid(51));
		}

		bool handleAlarmStatus(object m,string package) {
#if KENNY
			sendMessage(MidFinder.createNewMid(77),null);
			OpenProtocolInterpreter.MIDs.Alarm.MID_0076 m76;
			m76=m as OpenProtocolInterpreter.MIDs.Alarm.MID_0076;

			_logger.log(
				ColtLogLevel.Debug,
				MethodBase.GetCurrentMethod(),
				"ErrorCode="+m76.AlarmStatusData.ErrorCode+", "+
				"Alarm="+m76.AlarmStatusData.AlarmStatus+", "+
				"Tool ready="+m76.AlarmStatusData.ToolReadyStatus+", "+
				"Controller ready="+m76.AlarmStatusData.ControllerReadyStatus+", "+
				"When="+m76.AlarmStatusData.Time
				);
			return true;
#else
			_logger.log(MethodBase.GetCurrentMethod());
			return false;
#endif
		}

		internal void setVinNumber(string vinNumber) {

#if KENNY
			OpenProtocolInterpreter.MIDs.VIN.MID_0050 m50;
			m50=new OpenProtocolInterpreter.MIDs.VIN.MID_0050();

			m50.VINNumber=vinNumber;
			showMidProperties(m50,_logger);
			enqueueMid(m50);


			OpenProtocolInterpreter.MIDs.MultipleIdentifiers.MID_0150 m150;
			m150=new OpenProtocolInterpreter.MIDs.MultipleIdentifiers.MID_0150();
			m150.IdentifierData=vinNumber;
			showMidProperties(m150,_logger);
			enqueueMid(m150);
#else
#endif
		}

		internal void unsubscribeLastTightening63() {
			_logger.log(
						ColtLogLevel.Debug,
						MethodBase.GetCurrentMethod());
			enqueueMid(MidFinder.createNewMid(63));
		}

		internal void unsubscribeAlarm73() {
			_logger.log(
				ColtLogLevel.Debug,
				MethodBase.GetCurrentMethod());
			enqueueMid(MidFinder.createNewMid(73));
		}

		internal void subscribeDisableACProto420() {
			_logger.log(
				ColtLogLevel.Debug,
				MethodBase.GetCurrentMethod());
			enqueueMid(MidFinder.createNewMid(420));
		}

		internal void subscribeToolTagID261() {
			_logger.log(
				ColtLogLevel.Debug,
				MethodBase.GetCurrentMethod());
			enqueueMid(MidFinder.createNewMid(261));

		}

		internal void subscribeMultiIdent151() {
			_logger.log(
				ColtLogLevel.Debug,
				MethodBase.GetCurrentMethod());
			enqueueMid(MidFinder.createNewMid(151));
		}

		internal void subscribeAutoManualProto400() {
			_logger.log(
				ColtLogLevel.Debug,
				MethodBase.GetCurrentMethod());
			enqueueMid(MidFinder.createNewMid(400));

		}

		internal void subscribeJobLineInfo120() {
			_logger.log(
	ColtLogLevel.Debug,
	MethodBase.GetCurrentMethod());
			enqueueMid(MidFinder.createNewMid(120));

		}

		internal void subscribeMSpindleResults() {
			_logger.log(
				ColtLogLevel.Debug,
				MethodBase.GetCurrentMethod());
			enqueueMid(MidFinder.createNewMid(100));
		}

		internal void subscribeSelectorSocketInfo250() {
			_logger.log(
				ColtLogLevel.Debug,
				MethodBase.GetCurrentMethod());
			enqueueMid(MidFinder.createNewMid(250));

		}

		internal void subscribeMSpindleStatus() {
			_logger.log(
				ColtLogLevel.Debug,
				MethodBase.GetCurrentMethod());
			enqueueMid(MidFinder.createNewMid(90));
		}

		internal void subscribeUserData240() {
			_logger.log(
				ColtLogLevel.Debug,
				MethodBase.GetCurrentMethod());
			enqueueMid(MidFinder.createNewMid(240));

		}

		internal void subscribeRelayFunction216() {
			_logger.log(
				ColtLogLevel.Debug,
				MethodBase.GetCurrentMethod());
			enqueueMid(MidFinder.createNewMid(216));

		}

		bool handleCmdAccepted(object m,string package) {
			//_logger.log(MethodBase.GetCurrentMethod());
#if KENNY
			OpenProtocolInterpreter.MIDs.Communication.MID_0005 m5;
			m5=m as OpenProtocolInterpreter.MIDs.Communication.MID_0005;
			_logger.log(
				ColtLogLevel.Debug,
				MethodBase.GetCurrentMethod(),
				"Accepted MID "+m5.MIDAccepted+".");
			//)
			return true;
#else
			showMidProperties(m,_logger);
			//_logger.log(MethodBase.GetCurrentMethod());
			_logger.log(MethodBase.GetCurrentMethod(),"non-Kenny!");
			throw new InvalidOperationException("non-Kenny!");
			return false;
#endif
			//return false;
		}

		internal void subscribeExtInputs210() {
			_logger.log(
				ColtLogLevel.Debug,
				MethodBase.GetCurrentMethod());
			enqueueMid(MidFinder.createNewMid(210));

		}

		bool handleLockBatchUpload(object m,string package) {
#if KENNY
			OpenProtocolInterpreter.MIDs.ParameterSet.MID_0022 m22;
			m22=m as OpenProtocolInterpreter.MIDs.ParameterSet.MID_0022;

			sendMessage(MidFinder.createNewMid(23),null);
			_logger.log(
				ColtLogLevel.Debug,
				MethodBase.GetCurrentMethod(),
				"RelayStatus="+m22.RelayStatus);
			return true;
#else
			showMidProperties(m,_logger);
			//_logger.log(MethodBase.GetCurrentMethod());
			_logger.log(MethodBase.GetCurrentMethod(),"non-Kenny!");
			throw new InvalidOperationException("non-Kenny!");
			return false;
#endif
			//return false;
		}


		void addMidRequest(object qmid) {
			_logger.log(
				ColtLogLevel.Debug,
				MethodBase.GetCurrentMethod(),
				"Add "+qmid.GetType().Name+" ["+MidFinder.fixupPackage(MidFinder.extractMidContent(qmid))+"]");
			if (MidFinder.extractMidNumber(qmid)==10)
				sendMessage(qmid,new MidHandler(handlePSetList));
			else
				sendMessage(qmid,null);
		}

		internal void suscribeLastTightening() {
			_logger.log(MethodBase.GetCurrentMethod());
			enqueueMid(MidFinder.createNewMid(60));
		}

		internal void subscribeAlarm() {
			_logger.log(MethodBase.GetCurrentMethod());
			enqueueMid(MidFinder.createNewMid(70));
		}

		internal void suscribeLockBatch() {
			_logger.log(MethodBase.GetCurrentMethod());
			enqueueMid(MidFinder.createNewMid(21));
		}

		internal void suscribeJobInfo() {
			_logger.log(MethodBase.GetCurrentMethod());
			enqueueMid(MidFinder.createNewMid(34));
		}

		internal void suscribePSets() {
			_logger.log(MethodBase.GetCurrentMethod());
			enqueueMid(MidFinder.createNewMid(14));
		}

		bool handleCommShutdown(object m,string package) {
			_logger.log(MethodBase.GetCurrentMethod(),"test");
			//writeCSVProperties(m.GetType().GetProperties(),m,
			showMidProperties(m,_logger);
			return true;
		}
	}
}