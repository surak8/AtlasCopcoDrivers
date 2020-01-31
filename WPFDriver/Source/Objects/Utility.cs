using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace NSAtlasCopcoBreech {
	public static class Utility {
		public static IColtLogger logger = new ColtLogger();

		public static void shutdown() {
			if (logger != null) {
				logger.flush();
				((IDisposable) logger).Dispose();
				logger = null;
			}
		}


		internal static void saveWindowBoundsToRegistry(Window w, string key) {
			//if (w!=null)
			saveRegistryValue(key, makeWindowData(w));
		}

		internal static string makeWindowData(Window w) {
			if (w!=null)
				return "Top="+w.Top+", Left="+w.Left+", Width="+w.Width+", Height="+w.Height;
			return string.Empty;
		}

		internal static bool retrieveWindowBounds(string key, out double left, out double top, out double width, out double height) {
			bool ret;
			string regText=readRegistryValue(key,string.Empty),strKey,strValue,tmp;
			string[] parts;
			double d;
			int pos;

			left=top=width=height=Double.NaN;
			if (string.IsNullOrEmpty(regText)) {
				ret=false;
			} else {
				ret=true;
				parts=regText.Split(',');
				//return true;
				//Trace.WriteLine("here");
				if (parts!=null&&parts.Length>0) {
					foreach (string apart in parts) {
						if (!string.IsNullOrEmpty(tmp=apart.Trim())) {
							//Trace.WriteLine("here");
							if ((pos=tmp.IndexOf("="))>=0) {
								//Trace.WriteLine("here");
								strKey=tmp.Substring(0, pos);
								strValue=tmp.Substring(pos+1);
								if (double.TryParse(strValue, out d)) {
									switch (strKey.ToUpper()) {
										case "TOP": top=d; break;
										case "LEFT": left=d; break;
										case "WIDTH": width=d; break;
										case "HEIGHT": height=d; break;
										default: logger.log(ColtLogLevel.Warning, MethodBase.GetCurrentMethod(), "unhandled key: '"+strKey+"'!"); break;
									}
								}
							}
						}
					}
				} else ret=false;
				//ret=true;
			}
			return ret;
		}

		static string companyName<T>(Assembly asm) {
			var v=asm.GetCustomAttribute<AssemblyCompanyAttribute>();

			if (v!=null&&typeof(T).Equals(typeof(AssemblyCompanyAttribute)))
				return ((AssemblyCompanyAttribute) v).Company;
			Trace.WriteLine("fix this");
			return null;
		}


		static string registryPath {
			get {
				Assembly asm=Assembly.GetEntryAssembly();
				AssemblyName an=asm.GetName();

				return "Software\\"+companyName<AssemblyCompanyAttribute>(asm)+"\\"+an.Name+"\\" +an.Version; ;
			}
		}
		internal static void saveRegistryValue(string key, string strValue) {
			RegistryKey v = openOrCreateKey(  RegistryHive.CurrentUser);

			v.SetValue(key, strValue);
			v.Dispose();
		}

		internal static string readRegistryValue(string key, string defaultValue) {
			string ret;
			RegistryKey v = openOrCreateKey(  RegistryHive.CurrentUser);

			ret=v.GetValue(key, defaultValue) as string;
			v.Dispose();
			return ret;
		}

		internal static RegistryKey openOrCreateKey(RegistryHive rh) {
			RegistryKey rk;
			string path=registryPath;

			rk=RegistryKey.OpenBaseKey(rh, RegistryView.Default);
			var v=rk.OpenSubKey(path, true);
			if (v==null)
				v=rk.CreateSubKey(path, true);
			return v;
		}

	}

	public enum ColtLogLevel {
		UNKNOWN = -1,
		Info = 0,
		Warning = 1,
		Error = 2,
		Debug = 3
	}

	public interface IColtLogger {
		void flush();
		void log(MethodBase mb, string msg);
		void log(MethodBase mb, Exception ex);
		void log(MethodBase mb);
		void log(string msg);
		void log(ColtLogLevel logLevel, string msg);
		void log(ColtLogLevel logLevel, MethodBase mb);
		void log(ColtLogLevel error, MethodBase mb, string v);
		void write(ColtLogLevel logLevel, string msg);
	}
	public class ColtLogger : IColtLogger, IDisposable {

		#region ctor
		internal ColtLogger() { }
		#endregion

		#region methods
		public static string exceptionValue(Exception ex) {
			StringBuilder sb ;
			Exception ex0;

			if ((ex0=ex)== null)
				return string.Empty;
			sb = new StringBuilder();
			while (ex0 != null) {
				sb.AppendLine("[" + ex0.GetType().FullName + "] " + ex0.Message);
				ex0 = ex0.InnerException;
			}
			return sb.ToString();
		}
		public static string makeSig(MethodBase mb) {
			if (mb != null)
				return mb.ReflectedType.Name + "." + mb.Name;
			return string.Empty;
		}
		#endregion methods

		#region IDisposable implementation
		public void Dispose() {
			//((IColtLogger)this).write(ColtLogLevel.Debug, makeSig(MethodBase.GetCurrentMethod()));
			((IColtLogger) this).write(ColtLogLevel.Debug, makeSig(MethodBase.GetCurrentMethod()));
		}
		#endregion IDisposable implementation

		#region IColtLogger implementation

		void IColtLogger.flush() {
			((IColtLogger) this).write(
				ColtLogLevel.Debug,
				makeSig(MethodBase.GetCurrentMethod()));
		}

		void IColtLogger.log(MethodBase mb, string msg) {
			((IColtLogger) this).write(ColtLogLevel.Info, makeSig(mb)+":"+msg);
		}

		void IColtLogger.log(MethodBase mb, Exception ex) {
			((IColtLogger) this).write(
				ColtLogLevel.Error,
				makeSig(mb) + ":" + exceptionValue(ex) + ex.StackTrace);
		}

		void IColtLogger.log(MethodBase mb) {
			((IColtLogger) this).log(ColtLogLevel.Info, makeSig(mb));
		}

		void IColtLogger.log(string msg) {
			((IColtLogger) this).write(ColtLogLevel.Info, msg);
		}

		void IColtLogger.write(ColtLogLevel logLevel, string msg) {
			if (msg.Contains("\0")) {
				//Debug.WriteLine("ack");
				msg = msg.Replace('\0', ' ');
			}
			Trace.WriteLine("[" + logLevel + "] " + msg);
		}

		void IColtLogger.log(ColtLogLevel logLevel, string msg) {
			((IColtLogger) this).write(logLevel, msg);
		}

		void IColtLogger.log(ColtLogLevel ll, MethodBase mb, string msg) {
			((IColtLogger) this).log(ll, makeSig(mb) + ":" + msg);
		}

		void IColtLogger.log(ColtLogLevel logLevel, MethodBase mb) {
			((IColtLogger) this).log(logLevel, makeSig(mb));
		}

		//void IColtLogger.log(ColtLogLevel error, MethodBase methodBase, string v) {
		//	throw new NotImplementedException();
		//}

		/*

		public void log(MethodBase methodBase) {
			log(makeSig(methodBase));
		}

		public void log(MethodBase methodBase, Exception ex) {
			write(
				ColtLogLevel.Error,
				makeSig(methodBase) + ":" + exceptionValue(ex) + ex.StackTrace);
		}


		public void log(MethodBase methodBase, string msg) {
			log(makeSig(methodBase) + ":" + msg);
		}

	

		public void log(string msg) {
			//Trace.WriteLine(msg);
			write(ColtLogLevel.Info, msg);
		}

		public void write(ColtLogLevel logLevel, string msg) {
			Trace.WriteLine("[" + logLevel + "] " + msg);
		}

		void IColtLogger.flush() {
			//throw new NotImplementedException();
			write(ColtLogLevel.Debug, makeSig(MethodBase.GetCurrentMethod()));
		}
		*/
		#endregion IColtLogger implementation

	}
}