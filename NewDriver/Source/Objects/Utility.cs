#define NO_LOGGER

using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows;
using Colt.Utility.Logging;
using Microsoft.Win32;

namespace NSNewDriver {
	public static class Utility {
#if NO_LOGGER
		//		[Obsolete("use ColtUtilities version",true)]
#endif
		//		public static IColtLogger logger = new ColtLogger();

		public static IColtLogger _defLogger;
			public static IColtLogger logger {
			get {
				if (_defLogger==null)
					_defLogger = DefaultColtLogger.createDefault();
				return _defLogger;
			}
			private set { _defLogger = value; }
		}
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
			if (w != null)
				return "Top=" + w.Top + ", Left=" + w.Left + ", Width=" + w.Width + ", Height=" + w.Height;
			return string.Empty;
		}

		internal static bool retrieveWindowBounds(string key, out double left, out double top, out double width, out double height) {
			bool ret;
			string regText=readRegistryValue(key,string.Empty),strKey,strValue,tmp;
			string[] parts;
			double d;
			int pos;

			left = top = width = height = Double.NaN;
			if (string.IsNullOrEmpty(regText)) {
				ret = false;
			} else {
				ret = true;
				parts = regText.Split(',');
				//return true;
				//Trace.WriteLine("here");
				if (parts != null && parts.Length > 0) {
					foreach (string apart in parts) {
						if (!string.IsNullOrEmpty(tmp = apart.Trim())) {
							//Trace.WriteLine("here");
							if ((pos = tmp.IndexOf("=")) >= 0) {
								//Trace.WriteLine("here");
								strKey = tmp.Substring(0, pos);
								strValue = tmp.Substring(pos + 1);
								if (double.TryParse(strValue, out d)) {
									switch (strKey.ToUpper()) {
										case "TOP": top = d; break;
										case "LEFT": left = d; break;
										case "WIDTH": width = d; break;
										case "HEIGHT": height = d; break;
										default: logger.log(ColtLogLevel.Warning, MethodBase.GetCurrentMethod(), "unhandled key: '" + strKey + "'!"); break;
									}
								}
							}
						}
					}
				} else ret = false;
				//ret=true;
			}
			return ret;
		}

		static string companyName<T>(Assembly asm) {
			var v=asm.GetCustomAttribute<AssemblyCompanyAttribute>();

			if (v != null && typeof(T).Equals(typeof(AssemblyCompanyAttribute)))
				return ((AssemblyCompanyAttribute) v).Company;
			Trace.WriteLine("fix this");
			return null;
		}


		static string registryPath {
			get {
				Assembly asm=Assembly.GetEntryAssembly();
				AssemblyName an=asm.GetName();

				return "Software\\" + companyName<AssemblyCompanyAttribute>(asm) + "\\" + an.Name + "\\" + an.Version; ;
			}
		}
		internal static void saveRegistryValue(string key, string strValue) {
			RegistryKey v = openOrCreateKey(  RegistryHive.CurrentUser);

			v.SetValue(key, strValue);
			v.Dispose();
		}
		internal static void saveRegistryValue(string key, bool bvalue) {
			RegistryKey v = openOrCreateKey(  RegistryHive.CurrentUser);

			v.SetValue(key, bvalue);
			v.Dispose();
		}

		internal static string readRegistryValue(string key, string defaultValue) {
			string ret;
			RegistryKey v = openOrCreateKey(  RegistryHive.CurrentUser);

			ret = v.GetValue(key, defaultValue) as string;
			v.Dispose();
			return ret;
		}

		internal static bool readRegistryValue(string key,bool bdefValue) {
			object ret;
			bool bret=false;
			RegistryKey v = openOrCreateKey(  RegistryHive.CurrentUser);

			ret = v.GetValue(key, bdefValue);

			if (bool.TryParse(ret.ToString(), out bret))
				return bret;
			return false;
		}
		internal static RegistryKey openOrCreateKey(RegistryHive rh) {
			RegistryKey rk;
			string path=registryPath;

			rk = RegistryKey.OpenBaseKey(rh, RegistryView.Default);
			var v=rk.OpenSubKey(path, true);
			if (v == null)
				v = rk.CreateSubKey(path, true);
			return v;
		}

	}
}
