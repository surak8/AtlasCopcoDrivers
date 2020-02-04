using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
//using NSAtlasCopcoBreech;
//using NSAtlasCopcoBreech.MyMid;
//using OpenProtocolInterpreter.MIDs;


namespace NSAtlasCopcoBreech {

	static partial class MIDUtil {
#if !OTHER_VERSION
#if TRUE
#endif

		static OpenProtocolInterpreter.MIDs.MIDIdentifier _mident=new OpenProtocolInterpreter.MIDs.MIDIdentifier();
		static IDictionary<int,OpenProtocolInterpreter.MIDs.MID> _midMap=null;
		internal static void showMidDetail(OpenProtocolInterpreter.MIDs.MID realMid, string line, bool writeCSVHeader = false) {
			bool showContent=false;
			StringBuilder sb=new StringBuilder();
			int midValue;

			switch (midValue=realMid.HeaderData.Mid) {
				case 2: sb.AppendLine("comm-start"); break;
				case 5: break;
				//case 5: sb.AppendLine("accepted MID"); break;
				case 11: sb.AppendLine("pset-upload"); break;
				case 13: sb.AppendLine("pset-def"); constructContent(ref sb, realMid); break;
				case 15: sb.AppendLine("pset-selected"); break;
				case 31: sb.AppendLine("job-upload"); break;
				case 61: sb.Append("tight"); break;
				case 76: sb.AppendLine("alarm"); break;
				default: Trace.WriteLine("unhandled MID="+midValue); showContent=true; break;
			}
			if (sb.Length>0) {
				if (showContent) {
					sb.AppendLine(realMid.GetType().FullName+": ["+line+"]");
					constructContent(ref sb, realMid);
				}
				Utility.logger.log(sb.ToString());
			}
		}
		static void constructContent(ref StringBuilder sb, OpenProtocolInterpreter.MIDs.MID realMid) {
			object propValue;
			string dispValue,svalue;
			//bool showType;

			foreach (PropertyInfo pi in realMid.GetType().GetProperties(bfCommon)) {
				if (pi.Name.CompareTo("HeaderData")==0)
					continue;
				if (pi.Name.CompareTo("RegisteredDataFields")==0)
					continue;
				//showType=false;
				propValue=realMid.GetType().InvokeMember(pi.Name,
					bfProps,
					null, realMid, nullArgs);
				if (pi.PropertyType.Equals(typeof(string))) {
					if (propValue==null)
						dispValue="null(1)";
					else {
						svalue=propValue.ToString().Trim();
						if (string.IsNullOrEmpty(svalue))
							dispValue="null(2)";
						else
							dispValue=svalue;
					}
				} else if (pi.PropertyType.Equals(typeof(int))) {
					dispValue=propValue.ToString();
				} else if (pi.PropertyType.Equals(typeof(bool))) {
					dispValue=propValue.ToString();
				} else {
					if (pi.PropertyType.IsEnum) {
						//dispValue=pi.PropertyType.FullName.Replace("+", ".")+"."+propValue.ToString();
						dispValue=propValue.ToString();
					} else {
						dispValue=propValue.ToString();
						//showType=true;
					}
				}
				//sb.AppendLine("\t"+ pi.Name+" = "+dispValue+(showType ? " ["+pi.PropertyType.FullName+"]" : string.Empty)+".");
				sb.AppendLine("\t"+ pi.Name+" = "+dispValue);
			}
		}
		static Dictionary<int, OpenProtocolInterpreter.MIDs.MID> createMap(Type type) {
			Dictionary<int, OpenProtocolInterpreter.MIDs.MID> ret=new Dictionary<int, OpenProtocolInterpreter.MIDs.MID>();
			int midNo;
			string midNumber,midClassName,midFullName;
			object anobj;

			foreach (Type aType in type.Assembly.GetTypes()) {
				if (aType.IsPublic&&aType.Name.StartsWith("MID_")) {
					midClassName=aType.Name;
					midFullName=aType.FullName;
					midNumber=midClassName.Substring(4);
					if (Int32.TryParse(midNumber, out midNo)) {
						anobj=null;
						try {
							anobj=aType.InvokeMember(null, bfCreate, null, null, nullArgs) as OpenProtocolInterpreter.MIDs.MID;
						} catch (Exception ex) {
							Utility.logger.log(MethodBase.GetCurrentMethod(), ex);
						}
						if (anobj!=null)
							ret.Add(midNo, anobj as OpenProtocolInterpreter.MIDs.MID);
					} else
						Trace.WriteLine("ack!");
				}
			}
			return ret;
		}
		internal static void showMid(string line, bool writeCSVHeader = false, int desiredMid = -1) {
			OpenProtocolInterpreter.MIDs.MID realMid;
			string midId;
			int midno;

			if (_midMap==null)
				_midMap=createMap(_mident.GetType());
			midId=line.Substring(4, 4);
			if (Int32.TryParse(midId, out midno)) {
				if (_midMap.ContainsKey(midno)) {
					realMid=_midMap[midno].GetType().InvokeMember(null, bfCreate, null, null, nullArgs) as OpenProtocolInterpreter.MIDs.MID;
					if (realMid.HeaderData.Mid==9999)
						return;
					if (desiredMid<1) {
						//MyMidUtility.examineMid(line);
						//if (midno!=152&&midno!=211) {
						//	realMid.processPackage(line);
						//	showMidDetail(realMid, line);
						//} else
						//	Trace.WriteLine("ACK: bad MID-processing. MID="+midno+".");
					} else if (midno==desiredMid) {
						realMid.processPackage(line);
						showMidDetail(realMid, line, writeCSVHeader);
					}
				} else
					Trace.WriteLine("MID not found:" +midno+"!");
			} else
				Utility.logger.log(MethodBase.GetCurrentMethod());
		}
#else

		internal static void showMid(string package) {
			throw new NotImplementedException();
		}
		static void showMid(string line, bool writeCSVHeader, int desiredMid) {
			int midNo=midIdent(line);
			OpenProtocolInterpreter.Mid mid;

			//Trace.WriteLine("here");
			switch (midNo) {
				case 2:
					mid=new OpenProtocolInterpreter.Communication.Mid0002();
					mid.Parse(line);
					//Trace.WriteLine("here");
					break;
				case 11:mid=new OpenProtocolInterpreter.ParameterSet.Mid0011();
					mid.Parse(line);
					//mid.Pack();
					break;
				case 15:
					mid=new OpenProtocolInterpreter.ParameterSet.Mid0015();
					mid.Parse(line);
					//mid.Pack();
					break;
				case 31:
					mid=new OpenProtocolInterpreter.Job.Mid0031();
					mid.Parse(line);
					//mid.Pack();
					break;
				case 65:
					mid=new OpenProtocolInterpreter.Tightening.Mid0065();
					mid.Parse(line);
					break;
				case 76:
					mid=new OpenProtocolInterpreter.Alarm.Mid0076();
					mid.Parse(line);
					break;
				case 152:
					mid=new OpenProtocolInterpreter.MultipleIdentifiers.Mid0152();
					mid.Parse(line);
					break;
				case 211:
					mid=new OpenProtocolInterpreter.IOInterface.Mid0211();
					mid.Parse(line);
					break;
				default:
					Utility.logger.log(ColtLogLevel.Debug, MethodBase.GetCurrentMethod(), "Unhandled MID="+midNo+".");
					break;

			}
		}
#endif
	}
}