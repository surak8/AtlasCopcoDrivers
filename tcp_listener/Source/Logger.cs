using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace NSTcp_listener {
	static class Logger {
		//internal static void log(MethodBase mb) {
		//    log(mb,null);
		//}
		internal static void log(MethodBase mb, Exception ex) {
			log(mb, exceptionValue(ex));
		}

		  static string exceptionValue(Exception ex) {
			StringBuilder sb=new StringBuilder();
			Exception ex0=ex;
			while (ex0!=null) {
				sb.AppendLine("["+ex0.GetType().Name+"] "+ex0.Message);
				ex0=ex0.InnerException;
			}
			return sb.ToString();
		}

		internal static void log(MethodBase mb, string msg = null) {
			string displayMsg = makeSignature(mb) +
			   (string.IsNullOrEmpty(msg) ? string.Empty :
				(":" + msg.Replace('\0', '*')));
			//":"+string.IsInterned
			Trace.WriteLine(displayMsg);
		}

		static string makeSignature(MethodBase mb) {
			if (mb != null)
				return mb.ReflectedType.Name + "." + mb.Name;
			return string.Empty;
		}
		//trace
	}
}