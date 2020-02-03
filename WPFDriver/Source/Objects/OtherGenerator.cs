using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
//using OpenProtocolInterpreter.MIDs;
//using OpenProtocolInterpreter.MIDs;

namespace NSAtlasCopcoBreech {
	public class OtherGenerator  {

		readonly IDictionary<int,V> _midMap1=null;
		readonly Type _asmType;
		readonly Type _dictType;

		public OtherGenerator() {
			Type t=GetType();
			Type[] typeArgs;

			if ((typeArgs=t.GenericTypeArguments).Length==2) {
				_asmType=typeArgs[0];
				_dictType=typeArgs[0];
			}
			_midMap1=createMap(_asmType.Assembly, _dictType);
		}

		internal void generateCSV(string csvResult, string[] fileNames) {
			//string csvResult=v;
			byte[] data;
			//byte[] outdata;

			int readLen,midNo;

			//PropertyInfo[] pis;
			//int n=0;
			bool wroteHeader=false;

			long len;
			string[] lines;
			string dataStr,aline;
			//MID mid;
			//Type tmid;
			//string dispValue;

			try {
				if (File.Exists(csvResult))
					File.Delete(csvResult);
				using (TextWriter tw = new StreamWriter(csvResult)) {
					for (int i = 0; i<fileNames.Length; i++) {
						using (FileStream fs = new FileStream(fileNames[i], FileMode.Open, FileAccess.Read, FileShare.Read)) {
							data=new byte[len=fs.Length];
							readLen=fs.Read(data, 0, (int) len);
							lines=(dataStr=Encoding.ASCII.GetString(data)).Split('\n');
							for (int j = 0; j<lines.Length; j++) {
								if (!string.IsNullOrEmpty(aline=lines[j].Replace('\r', ' ').Replace('\0', ' ').Trim())) {
									if ((midNo=MIDUtil.midIdent(aline))==61) {
										//writeCSVOutputForLine(midNo, out pis, ref n, ref wroteHeader, aline, out mid, out tmid, out dispValue, tw);
										writeCSVOutputForLine(midNo, ref wroteHeader, aline, tw);
									}
								}
							}
						}
					}
				}
			} catch (Exception ex) {

				MessageBox.Show("CSV-file '"+csvResult+"' cannot be accessed."+Environment.NewLine+ex.Message, "Error writing CSV-file.");
			}
		}

		//[Obsolete("fix this",true)]
		static void writeCSVProperties(PropertyInfo[] pis, V mid, TextWriter tw) {
			int n=0;
			string dispValue;
			object propValue;

			// write data-fields
			foreach (PropertyInfo pi in pis) {
				if (n>0)
					tw.Write(",");
				// extract the property.
				propValue=mid.GetType().InvokeMember(pi.Name, MIDUtil.bfProps, null, mid, MIDUtil.nullArgs);
				if (propValue==null)
					dispValue="NULL";
				else {
					if (pi.PropertyType.Equals(typeof(int)) ||
						pi.PropertyType.Equals(typeof(decimal))||
						pi.PropertyType.Equals(typeof(bool)))
						dispValue=propValue.ToString();
					else if (pi.PropertyType.Equals(typeof(string))) dispValue="\""+((string) propValue).Trim()+"\"";
					else if (pi.PropertyType.Equals(typeof(DateTime))) dispValue="\""+((DateTime) propValue).ToString("dd-MMM-yy hh:mm:ss tt")+"\"";
					else {
						Trace.WriteLine(pi.PropertyType.FullName);
						dispValue=propValue.ToString();
						dispValue="\""+propValue.ToString()+"\"";
					}
				}
				tw.Write(dispValue);
				n++;
			}
			tw.WriteLine();
		}

		static void writeCSVHeader(PropertyInfo[] pis, ref bool wroteHeader, TextWriter tw) {
			int n=0;

			// generate header here.
			if (!wroteHeader) {
				wroteHeader=true;
				n=0;
				foreach (PropertyInfo pi in pis) {
					if (n>0)
						tw.Write(",");
					if (pi.Name.Contains(" "))
						tw.Write("\""+pi.Name+"\"");
					else
						tw.Write(pi.Name);
					n++;
				}
				tw.WriteLine();
			}
		}

		void writeCSVOutputForLine(int midNo, ref bool wroteHeader, string aline, TextWriter tw) {
			V mid;
			Type tmid;
			PropertyInfo[] pis;

			if (_midMap1.ContainsKey(midNo)) {
				tmid=_midMap1[midNo].GetType();
				mid= (V) tmid.InvokeMember(null, MIDUtil.bfCreate, null, null, MIDUtil.nullArgs);
				tmid.InvokeMember("processPackage",
					 BindingFlags.Instance|  BindingFlags.NonPublic|  BindingFlags.Public|  BindingFlags.Static | BindingFlags.InvokeMethod,
					 null, mid, new object[] { aline });
				pis=tmid.GetProperties(MIDUtil.bfCommon|  BindingFlags.DeclaredOnly);
				writeCSVHeader(pis, ref wroteHeader, tw);
				writeCSVProperties(pis, mid, tw);
			} else
				MessageBox.Show("MidNo "+midNo+" not mapped!", "MID error");
		}

		IDictionary<int, V> createMap(Assembly asm, Type dictType) {
			IDictionary<int, V> ret=new Dictionary<int, V>();
			string midClassName,midFullName,midNumber;
			int midNo;
			object anobj;

			foreach (Type aType in asm.GetTypes()) {
				if (aType.IsPublic&&aType.Name.StartsWith("MID_")) {
					midClassName=aType.Name;
					midFullName=aType.FullName;
					midNumber=midClassName.Substring(4);
					if (Int32.TryParse(midNumber, out midNo)) {
						anobj=null;
						try {
							//anobj=aType.InvokeMember(null, BindingFlags.Public|   BindingFlags.CreateInstance|BindingFlags.DeclaredOnly|BindingFlags.Static|BindingFlags.Instance, null, null, new object[] { }) as MID;
							anobj=(V) aType.InvokeMember(null, MIDUtil.bfCreate, null, null, MIDUtil.nullArgs);
						} catch (Exception ex) {
							Utility.logger.log(MethodBase.GetCurrentMethod(), ex);
						}
						if (anobj!=null)
							ret.Add(midNo, (V) anobj);
					} else
						Trace.WriteLine("ack!");
				}
			}
			Utility.logger.log(ColtLogLevel.Debug, MethodBase.GetCurrentMethod(), "have "+ret.Count);
			return ret;
		}
	}
}