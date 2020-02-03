using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using OpenProtocolInterpreter.MIDs;

namespace NSAtlasCopcoBreech {
	static class MIDUtil {

		static string _midLogPath;

		public static string midLogPath {
			get {
				string asmName;

				if (string.IsNullOrEmpty(_midLogPath)) {
					asmName = Assembly.GetEntryAssembly().GetName().Name;
					_midLogPath = Path.Combine(
						Environment.GetEnvironmentVariable("TEMP"),
						asmName);
				}
				try {
					if (!Directory.Exists(_midLogPath))
						Directory.CreateDirectory(_midLogPath);
				} catch (Exception ex) {
					Utility.logger.log(
						ColtLogLevel.Error,
						MethodBase.GetCurrentMethod(),
						"Failed to create log-directory: '"+_midLogPath+"'!"+Environment.NewLine
						+ex.Message);
				}
				return _midLogPath;
			}
		}
		internal static void showMidDetails(string[] fileNames) {
			foreach (string aFile in fileNames)
				showMidDetail(aFile);
		}

		internal static void showMidDetail(string fileName) {
			showMidData(fileName);
		}
		internal static void showMidData(string filename, bool writeCSV = false, bool isFirstFile = false, int desiredMid = -1) {
#if true
			useFileVersion(filename, writeCSV, isFirstFile, desiredMid);
#else
			useTextReaderVersion(filename,writeCSV,isFirstFile,desiredMid);
#endif
		}

		static void useTextReaderVersion(string filename) {
			string[] lines;

			using (TextReader tw = new StreamReader(filename))
				foreach (string aline in lines=tw.ReadToEnd().Split('\n'))
					showSingleMid(aline);
		}

		static void showSingleMid(string aline, bool writeCSVHeader = false, int desiredMid = -1) {
			string line;

			if (!string.IsNullOrEmpty(line=aline.Replace('\r', ' ').Replace('\0', ' ').Trim())&&
				line.Length>8&&
				midIdent(line)!=9999) {
				showMid(line, writeCSVHeader, desiredMid);
			}
		}

		static void useFileVersion(string filename, bool writeCSV, bool isFirstFile, int desiredMid) {
			byte[] data;
			int readLen;
			long len;
			string[] lines;
			string dataStr;

			using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				data=new byte[len=fs.Length];
				readLen=fs.Read(data, 0, (int) len);
				lines=(dataStr=Encoding.ASCII.GetString(data)).Split('\n');
				for (int i = 0; i<lines.Length; i++)
					showSingleMid(lines[i], writeCSV&&isFirstFile&&i==0, desiredMid);
			}
		}

		static MIDIdentifier _mident=new MIDIdentifier();
		static IDictionary<int,MID> _midMap=null;
		internal static readonly BindingFlags bfCommon=BindingFlags.Public|BindingFlags.Instance;
		internal static readonly BindingFlags bfCreate=bfCommon|BindingFlags.CreateInstance;
		internal    static readonly BindingFlags bfProps=bfCommon|  BindingFlags.GetProperty;
		internal    static readonly object[] nullArgs=new object[] { };
		internal static void showMid(string line, bool writeCSVHeader = false, int desiredMid = -1) {
			MID realMid;
			string midId;
			int midno;

			if (_midMap==null)
				_midMap=createMap(_mident.GetType());
			midId=line.Substring(4, 4);
			if (Int32.TryParse(midId, out midno)) {
				if (_midMap.ContainsKey(midno)) {
					realMid=_midMap[midno].GetType().InvokeMember(null, bfCreate, null, null, nullArgs) as MID;
					if (realMid.HeaderData.Mid==9999)
						return;
					if (desiredMid<1) {
						if (midno!=152&&midno!=211) {
							realMid.processPackage(line);
							showMidDetail(realMid, line);
						} else
							Trace.WriteLine("ACK: bad MID-processing. MID="+midno+".");
					} else if (midno==desiredMid) {
						realMid.processPackage(line);
						showMidDetail(realMid, line, writeCSVHeader);
					}
				} else
					Trace.WriteLine("MID not found:" +midno+"!");
			} else
				Utility.logger.log(MethodBase.GetCurrentMethod());
		}

		internal static void showMidDetail(MID realMid, string line, bool writeCSVHeader = false) {
			bool showContent=false;
			StringBuilder sb=new StringBuilder();
			int midValue;

			switch (midValue=realMid.HeaderData.Mid) {
				case 2: sb.AppendLine("comm-start"); break;
				case 5: sb.AppendLine("accepted MID"); break;
				case 11: sb.AppendLine("pset-upload"); break;
				case 13: sb.AppendLine("pset-def"); constructContent(ref sb, realMid); break;
				case 15: sb.AppendLine("pset-selected"); break;
				case 31: sb.AppendLine("job-upload"); break;
				case 76: sb.AppendLine("alarm"); break;
				default: Trace.WriteLine("unhandled MID="+midValue); showContent=true; break;
			}
			if (showContent) {
				sb.AppendLine(realMid.GetType().FullName+": ["+line+"]");
				constructContent(ref sb, realMid);
			}
			Utility.logger.log(sb.ToString());
		}

		static void constructContent(ref StringBuilder sb, MID realMid) {
			object propValue;
			string dispValue,svalue;
			bool showType;

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
						//dispValue=pi.PropertyType.FullName.Replace("+", ".")+"."+propValue.ToString();
						dispValue=propValue.ToString();
					} else {
						dispValue=propValue.ToString();
						showType=true;
					}
				}
				//sb.AppendLine("\t"+ pi.Name+" = "+dispValue+(showType ? " ["+pi.PropertyType.FullName+"]" : string.Empty)+".");
				sb.AppendLine("\t"+ pi.Name+" = "+dispValue);
			}
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

		internal static int midIdent(string package) {
			string strMid;
			int ret;

			if (!string.IsNullOrEmpty(package)&&package.Length>8) {
				if (int.TryParse(strMid=package.Substring(4, 4), out ret))
					return ret;
				Utility.logger.log(ColtLogLevel.Error, "Invalid MID-value: "+strMid);
			}
			Utility.logger.log(ColtLogLevel.Error, "Invalid package: "+package);
			return -1;
		}
	}
}