using System;
using System.Diagnostics;
using System.Reflection;
using Colt.Utility.Logging;

namespace NSTcp_listener {
	class MyTraceLogger : IColtLogger {
		public string outputDirectory { get; set; }
		#region properties

		public bool showTimestamp { get; set; }

		#endregion

		public void flush() {
			log(DefaultColtLogger.makeSignature(MethodBase.GetCurrentMethod()));
		}

		public void log(MethodBase mb,Exception ex) {
			log(ColtLogLevel.Info,mb,DefaultColtLogger.exceptionValue(ex));
		}

		public void log(MethodBase mb,Exception ex,bool showStackTrace) {
			log(
				ColtLogLevel.Info,
				mb,DefaultColtLogger.exceptionValue(ex)+
				(showStackTrace ? (Environment.NewLine+ex.StackTrace) : string.Empty));
		}

		public void log(MethodBase mb,string msg) {
			log(ColtLogLevel.Info,mb,msg);
		}

		public void log(MethodBase mb) {
			log(ColtLogLevel.Info,mb);
		}

		public void log(string msg) {
			log(ColtLogLevel.Info,null,msg);
		}

		public void log(ColtLogLevel level,MethodBase mb,string msg = null) {
			Trace.WriteLine(DefaultColtLogger.displayMessage(level,mb,msg,false));
		}

		public void write(ColtLogLevel logLevel,string msg) {
			log(logLevel,null,msg);
		}

		#region IDisposable Support
		bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				if (disposing) {
					// TODO: dispose managed state (managed objects).
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue=true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~MyTraceLogger() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose() {
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}