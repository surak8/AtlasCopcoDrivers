
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Colt.Utility.Logging;

namespace NSNewDriver {
	partial class ACListener {

		static void logData(IColtLogger logger, string msg) {
			if (logger != null)
				logger.log(msg);
			else
				Trace.WriteLine("[*** TRACE ***] " + msg);
		}
		static StringBuilder showMidProperties(object m2, IColtLogger logger) {
			StringBuilder sb;

			using (StringWriter sw = new StringWriter(sb = new StringBuilder())) {
				writeCSVProperties(m2.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public), m2, sw, logger);
			}
			logData(logger, sb.ToString());
			return sb;
		}

		#region CSV-generation

		static readonly BindingFlags bfProps= BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.GetProperty;
		static readonly object[] nullArgs=new object[0];
		bool _showControllerResponse=false;

		static void writeCSVProperties(PropertyInfo[] pis, object mid, TextWriter tw, IColtLogger logger) {
			int n=0;
			string dispValue;
			object propValue;

			tw.WriteLine();
			tw.WriteLine(mid.GetType().FullName);
			// write data-fields
			foreach (PropertyInfo pi in pis) {
				// extract the property.
				try {
					propValue = mid.GetType().InvokeMember(pi.Name, bfProps, null, mid, nullArgs);
					if (propValue == null)
						dispValue = "NULL";
					else {
						if (pi.PropertyType.Equals(typeof(int)) ||
							pi.PropertyType.Equals(typeof(decimal)) ||
							pi.PropertyType.Equals(typeof(bool)))
							dispValue = propValue.ToString();
						else if (pi.PropertyType.Equals(typeof(string))) dispValue = "\"" + ((string) propValue).Trim() + "\"";
						else if (pi.PropertyType.Equals(typeof(DateTime))) dispValue = "\"" + ((DateTime) propValue).ToString("dd-MMM-yy hh:mm:ss tt") + "\"";
						else {
							if (pi.PropertyType.IsEnum)
								dispValue = propValue.ToString();
							else {
								Trace.WriteLine(pi.PropertyType.FullName);
								dispValue = propValue.ToString();
								dispValue = "\"" + propValue.ToString() + "\"";
							}
						}
					}
					tw.WriteLine("\t" + pi.Name + " = " + dispValue);
					n++;
				} catch (Exception ex) {
					logData(
						logger,
						DefaultColtLogger.displayMessage(
							ColtLogLevel.Error,
							MethodBase.GetCurrentMethod(),
							DefaultColtLogger.exceptionValue(ex),
							logger == null ? true : logger.showTimestamp));
					tw.WriteLine("error with:" + pi.Name + Environment.NewLine + "\ttype=" + pi.PropertyType.FullName);
				}
			}
			tw.WriteLine();
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing) {
			_logger.log(MethodBase.GetCurrentMethod());
			if (!disposedValue) {
				if (disposing) {
					// TODO: dispose managed state (managed objects).
					if (_logger != null) {
						//Trace.WriteLine("close logger: "+_logger.)
						_logger.Dispose();
						//_logger = null;
					}
					if (_dataLogger != null) {
						_dataLogger.Dispose();
						//_dataLogger = null;
					}
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~ACListener() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose() {
			_logger.log(MethodBase.GetCurrentMethod());
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
		#endregion

	}
}