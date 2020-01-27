using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

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