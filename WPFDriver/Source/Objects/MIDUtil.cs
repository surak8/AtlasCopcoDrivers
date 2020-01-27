using System.Diagnostics;
using System.IO;
using System;
using System.Reflection;
using OpenProtocolInterpreter.MIDs;
using System.Collections.Generic;

namespace NSAtlasCopcoBreech {
	static class MIDUtil {
		//internal static void showMidDetail(string[] fileNames) {
		//	throw new NotImplementedException();
		//}

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
		//new        Dictionary<int, MID>();
		static void showMid(string line) {
			Trace.WriteLine("here");
			//object anObj;
			//MID amid;
			MID realMid;
			string className,midId;
			int midno;

			if (_midMap==null)
				_midMap=createMap(_mident.GetType());

			//anObj=
			midId=line.Substring(4, 4);
			if (Int32.TryParse(midId, out midno)) {
				className="MID_"+midno.ToString("000#");
				if (_midMap.ContainsKey(midno)) {
					//amid=
					//amid=_midMap[midno];
					realMid=_midMap[midno].GetType().InvokeMember(
						null,
						BindingFlags.Public|BindingFlags.Instance|BindingFlags.CreateInstance,
						null,
						null,
						new object[] { }) as MID;
					if (midno!=152&&midno!=211) {
						//amid.processPackage(line);
						realMid.processPackage(line);
						foreach (PropertyInfo pi in realMid.GetType().GetProperties(BindingFlags.Public|  BindingFlags.Instance)) {
							//Trace.WriteLine("hjere");
							Trace.WriteLine(pi.Name+" = "+
								realMid.GetType().InvokeMember(pi.Name,
								BindingFlags.Public|BindingFlags.Instance|BindingFlags.GetProperty,
								null, realMid, new object[] { }));

						}
					} else
						Trace.WriteLine("ACK: bad MID-processing. MID="+midno+".");
					//Trace.WriteLine("hjere");
				} else
					Trace.WriteLine("not found:" +className+"!");
				//anObj=T
				//Trace.WriteLine("here");

			} else
				Utility.logger.log(MethodBase.GetCurrentMethod());

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
					//Trace.WriteLine("here");
					midNumber=midClassName.Substring(4);
					//if (Int32.TryParse(midNumber=))
					if (Int32.TryParse(midNumber, out midNo)) {
						anobj=null;
						try {
							anobj=aType.InvokeMember(null, System.Reflection.BindingFlags.Public|   BindingFlags.CreateInstance|BindingFlags.DeclaredOnly|BindingFlags.Static|BindingFlags.Instance, null, null, new object[] { }) as MID;
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