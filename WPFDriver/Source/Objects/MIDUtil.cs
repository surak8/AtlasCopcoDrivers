using System.Diagnostics;
using System.IO;
using System;
using System.Reflection;
using OpenProtocolInterpreter.MIDs;
using System.Collections.Generic;
using System.Text;

namespace NSAtlasCopcoBreech {
	static class MIDUtil {
		internal static void showMidDetails(string[] fileNames) {
			foreach (string aFile in fileNames)
				showMidDetail(aFile);
		}
		internal static void showMidDetail(string fileName) {
			showMidData(fileName);
		}
		internal static void showMidData(string filename) {
			string[] lines;
			string line,midDesc;
			using (TextReader tw = new StreamReader(filename)) {
				foreach (string aline in lines=tw.ReadToEnd().Split('\n')) {
					if (!string.IsNullOrEmpty(line=aline.Replace('\r', '\0').Trim())&&
						line.Length>8&&
						string.Compare(midDesc=line.Substring(4, 4), "9999", true)!=0) {
						showMid(line);
					}
				}
			}
		}
		static MIDIdentifier _mident=new MIDIdentifier();
		static IDictionary<int,MID> _midMap=null;
		static readonly BindingFlags bfCommon=BindingFlags.Public|BindingFlags.Instance;
		static readonly BindingFlags bfCreate=bfCommon|BindingFlags.CreateInstance;
		static readonly BindingFlags bfProps=bfCommon|  BindingFlags.GetProperty;
		static readonly object[] nullArgs=new object[] { };
		static void showMid(string line) {
			MID realMid;
			string midId;
			int midno;

			if (_midMap==null)
				_midMap=createMap(_mident.GetType());
			midId=line.Substring(4, 4);
			if (Int32.TryParse(midId, out midno)) {
				//className="MID_"+midno.ToString("000#");
				if (_midMap.ContainsKey(midno)) {
					realMid=_midMap[midno].GetType().InvokeMember(null, bfCreate, null, null, nullArgs) as MID;
					if (midno!=152&&midno!=211) {
						realMid.processPackage(line);
						showMid1(realMid,line);
					
					} else
						Trace.WriteLine("ACK: bad MID-processing. MID="+midno+".");
				} else
					Trace.WriteLine("MID not found:" +midno+"!");
			} else
				Utility.logger.log(MethodBase.GetCurrentMethod());
		}

		static void showMid1(MID realMid, string line) {
			StringBuilder sb=new StringBuilder();
			object propValue;
			string svalue,dispValue,propType;
			bool showType=false;

			sb.AppendLine(realMid.GetType().FullName);
			sb.AppendLine("["+line+"]");
			foreach (PropertyInfo pi in realMid.GetType().GetProperties(bfCommon)) {
				if (pi.Name.CompareTo("HeaderData")==0)
					continue;
				if (pi.Name.CompareTo("RegisteredDataFields")==0)
					continue;
				showType=false;
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
						dispValue=pi.PropertyType.FullName.Replace("+",".")+"."+propValue.ToString();
						//propType=pi.PropertyType.
					} else {
						dispValue=propValue.ToString();
						showType=true;
					}
				}
				sb.AppendLine("\t"+ pi.Name+" = "+dispValue+(showType?" ["+pi.PropertyType.FullName+"]":string.Empty)+".");
			}
			Utility.logger.log(sb.ToString());
		}

		static Dictionary<int, MID> createMap(Type type) {
			Dictionary<int, MID> ret=new Dictionary<int, MID>();
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
							//anobj=aType.InvokeMember(null, BindingFlags.Public|   BindingFlags.CreateInstance|BindingFlags.DeclaredOnly|BindingFlags.Static|BindingFlags.Instance, null, null, new object[] { }) as MID;
							anobj=aType.InvokeMember(null, bfCreate, null, null, nullArgs) as MID;
						} catch (Exception ex) {
							Utility.logger.log(MethodBase.GetCurrentMethod(), ex);
						}
						if (anobj!=null)
							ret.Add(midNo, anobj as MID);
					} else
						Trace.WriteLine("ack!");
				}
			}
			return ret;
		}
	}
}