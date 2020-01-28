using System.Diagnostics;
using System.IO;
using System;
using System.Reflection;
using System.Text;
#if OTHER_VERSION
using OpenProtocolInterpreter;
#else
using OpenProtocolInterpreter.MIDs;
#endif
using System.Collections.Generic;

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
#if OTHER_VERSION
		static MidInterpreter _mident=new MidInterpreter();
		static IDictionary<int,Mid> _midMap=null;
		const string MID_CLASS_PREFIX="Mid";
#else
		static MIDIdentifier _mident=new MIDIdentifier();
		static IDictionary<int,MID> _midMap=null;
		const string MID_CLASS_PREFIX="MID_";
#endif
		static void showMid(string line) {
#if OTHER_VERSION
			Mid realMid;
#else
			MID realMid;
#endif
			string className,midId;
			int midno;

			if (_midMap==null)
				_midMap=createMap(_mident.GetType());
			midId=line.Substring(4, 4);
			if (Int32.TryParse(midId, out midno)) {
				className=MID_CLASS_PREFIX+midno.ToString("000#");
				if (_midMap.ContainsKey(midno)) {
					realMid=_midMap[midno].GetType().InvokeMember(
						null,
						BindingFlags.Public|BindingFlags.Instance|BindingFlags.CreateInstance,
						null,
						null,
						new object[] { }) as
#if OTHER_VERSION
						Mid
#else
						MID
#endif
						;
#if OTHER_VERSION
					realMid.Parse(line);
					showMidFields(realMid,line);
#else
				if (midno!=152&&midno!=211) {
						realMid.processPackage(line);
						showMidFields(realMid);
					} else
						Trace.WriteLine("ACK: bad MID-processing. MID="+midno+".");
#endif
				} else
					Trace.WriteLine("not found:" +className+"!");
			} else
				Utility.logger.log(MethodBase.GetCurrentMethod());
		}

#if OTHER_VERSION
		static void showMidFields(Mid realMid,string package)
#else
		static void showMidFields(MID realMid)
#endif
			{
			StringBuilder sb=new System.Text.StringBuilder();

			sb.AppendLine(realMid.GetType().FullName);
			sb.AppendLine("["+package.Replace('\0',' ').Trim()+"]");
			foreach (PropertyInfo pi in realMid.GetType().GetProperties(BindingFlags.Public|  BindingFlags.Instance)) {
				sb.AppendLine("\t"+
					pi.Name+" = "+
					realMid.GetType().InvokeMember(pi.Name,
					BindingFlags.Public|BindingFlags.Instance|BindingFlags.GetProperty,
					null, realMid, new object[] { }));
			}
			sb.AppendLine();
			Trace.WriteLine(sb.ToString());
		}

#if OTHER_VERSION
		static Dictionary<int, Mid> createMap(Type type) {
			Dictionary<int, Mid> ret=new Dictionary<int, Mid>();
#else
		static Dictionary<int, MID> createMap(Type type) {
			Dictionary<int, MID> ret=new Dictionary<int, MID>();
#endif
			int midNo,prefixLen=MID_CLASS_PREFIX.Length;
			string midNumber,midClassName,midFullName;
			object anobj;

			foreach (Type aType in type.Assembly.GetTypes()) {
				if (aType.IsPublic&&aType.Name.StartsWith(MID_CLASS_PREFIX)&aType.Name.Length>prefixLen) {
					midClassName=aType.Name;
					midNumber=midClassName.Substring(prefixLen);
					if (!System.Text.RegularExpressions.Regex.IsMatch(midClassName, MID_CLASS_PREFIX+"[0-9]+"))
						continue;
					midFullName=aType.FullName;
					if (Int32.TryParse(midNumber, out midNo)) {
						anobj=null;
						try {
							anobj=aType.InvokeMember(null,
								BindingFlags.Public|   BindingFlags.CreateInstance|BindingFlags.DeclaredOnly|BindingFlags.Static|BindingFlags.Instance,
								null, null, new object[] { }) as
#if OTHER_VERSION
								Mid
#else
								MID
#endif
								;
						} catch (Exception ex) {
							Utility.logger.log(MethodBase.GetCurrentMethod(), ex);
						}

						if (anobj!=null)
#if OTHER_VERSION
							ret.Add(midNo, anobj as Mid);
#else
							ret.Add(midNo, anobj as MID);
#endif
					} else
						Trace.WriteLine("ack!");
				}
			}
			return ret;
		}
	}
}