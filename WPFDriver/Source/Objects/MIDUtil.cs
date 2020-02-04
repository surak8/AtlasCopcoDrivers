using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using NSAtlasCopcoBreech.MyMid;
//#if !OTHER_VERSION
//using OpenProtocolInterpreter.MIDs;
//#endif

namespace NSAtlasCopcoBreech {
	static partial class MIDUtil {

		static string _midLogPath;

		public static string midLogPath {
			get {
				string asmName,buildCfg;
				Assembly asm;
				AssemblyName an;
				int pos;

				if (string.IsNullOrEmpty(_midLogPath)) {
					asm=Assembly.GetEntryAssembly();
					an=asm.GetName();
					asmName = an.Name;
					buildCfg=asm.GetCustomAttribute<AssemblyConfigurationAttribute>().Configuration;
					if ((pos=buildCfg.IndexOf(' '))>0) {
						//Trace.WriteLine("(SUBSTR) CFG="+buildCfg.Substring(0, pos)+".");
						buildCfg=buildCfg.Substring(0, pos);
					} else
						Trace.WriteLine("CFG="+buildCfg+".");
#if DEBUG
					_midLogPath = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), asmName, buildCfg, Dns.GetHostName());
#else
					_midLogPath = Path.Combine(@"\\appdeploy\APPDEPLOY\Colt Software\Logs", asmName, buildCfg, Dns.GetHostName());
#endif
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

		internal static readonly BindingFlags bfCommon=BindingFlags.Public|BindingFlags.Instance;
		internal static readonly BindingFlags bfCreate=bfCommon|BindingFlags.CreateInstance;
		internal static readonly BindingFlags bfProps=bfCommon|  BindingFlags.GetProperty;
		internal static readonly object[] nullArgs=new object[] { };




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

		//internal static void showMid(string package) {
		//	throw new NotImplementedException();
		//}
	}
}