using Colt.Utility.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;

namespace NSTcp_listener {
	partial class MyListenerThread : IDisposable {
		#region constants
		const int MAX_NUMBER_IR_FIELDS = 23;
		#endregion

		#region fields
		TcpListener _listener;
		ManualResetEvent _mre;
		Queue<string> _dataQueue;
		List<string> _fileData;
		readonly object _generateIRDataLock = new object();
		bool _generateIRData = false;

		#endregion

		#region properties
		public string ipAddress { get; private set; }
		public int portNumber { get; private set; }
		internal IColtLogger logger { get; private set; }
		internal WaitHandle waitHandle { get { return _mre; } }

		#endregion properties

		#region ctor
		public MyListenerThread(string ipAddress,int portNumber,IColtLogger icl,Stream dataStream) {
			if (icl==null)
				throw new ArgumentNullException("logger is null!","icl");
			loadPreviousData(dataStream,ref _fileData,logger);
			_dataQueue=new Queue<string>();
			enqueuePreviousData(_dataQueue,_fileData);
			logger=icl;
			_mre=new ManualResetEvent(false);
			this.ipAddress=ipAddress;
			this.portNumber=portNumber;
			_listener=new TcpListener(IPAddress.Parse(ipAddress),portNumber);
			_listener.Server.SetSocketOption(SocketOptionLevel.Socket,SocketOptionName.ReuseAddress,1);
			Logger.log(MethodBase.GetCurrentMethod(),"server on "+ipAddress+":"+portNumber+".");
		}

		static void enqueuePreviousData(Queue<string> q,List<string> lines) {
			if (q!=null&&lines!=null&&lines.Count>0)
				foreach (string aline in lines)
					q.Enqueue(aline);

		}
		static void loadPreviousData(Stream dataStream,ref List<string> fileData,IColtLogger logger) {
			string tmp;

			if (fileData==null)
				fileData=new List<string>();
			if (dataStream!=null) {
				try {
					using (TextReader tr = new StreamReader(dataStream)) {
						foreach (string aline in tr.ReadToEnd().Split('\n'))
							if (!string.IsNullOrEmpty(tmp=aline.Trim()))
								fileData.Add(tmp);
					}
				} catch (Exception ex) {
					logger.log(ColtLogLevel.Error,
						MethodBase.GetCurrentMethod(),
						"error reading data-stream : "+
						DefaultColtLogger.exceptionValue(ex));
				}
			}
		}

		#endregion

		internal void runThread999() {
			logger.log(MethodBase.GetCurrentMethod(),"starts");
#if TRACE
			Trace.IndentLevel++;
#endif
			_mre.Reset();
			// start listening
			_listener.Start();
			// perform 'handleConnection' when connection is made.
			_listener.BeginAcceptTcpClient(
				new AsyncCallback(handleConnection),
				_listener);
			// wait on the handler-method to end
			_mre.WaitOne();
			// shutdown the connection;
			_listener.Stop();
			logger.log(MethodBase.GetCurrentMethod(),"ends");
#if TRACE
			Trace.IndentLevel--;
#endif
		}

		void handleConnection(IAsyncResult ar) {
			bool _exitLoop = false,dataFound = false,useQueuedData;
			int i;
			byte[] bytes = new byte[4096];
			NetworkStream stream;
			TcpListener listener;
			TcpClient client;

			logger.log(MethodBase.GetCurrentMethod(),"starts");
#if TRACE
			Trace.IndentLevel++;
#endif
			listener=(TcpListener) ar.AsyncState;

			// End the operation and display the received data on 
			// the console.
			client=listener.EndAcceptTcpClient(ar);
			stream=client.GetStream();

			while (!_exitLoop) {
				if (_mre.WaitOne(100)) {
					logger.log(MethodBase.GetCurrentMethod(),"signalled!");
					_exitLoop=true;
					break;
				}
				if (stream.DataAvailable) {
					i=stream.Read(bytes,0,bytes.Length);
					if (i<1) {
						client.Close();
					} else {
						string data = Encoding.ASCII.GetString(bytes,0,i);
						dataFound=true;
						logger.log(MethodBase.GetCurrentMethod(),"data-found!");
					}
				} else {
					lock (_generateIRDataLock) {
						if (_generateIRData) {
							//Logger.log(MethodBase.GetCurrentMethod(),"starting");
							// create a line of 23 fields, send out through the stream.
							_generateIRData=false;
							useQueuedData=false;
							if (_fileData.Count>0) {
								if (_dataQueue.Count<1)
									enqueuePreviousData(this._dataQueue,this._fileData);
								useQueuedData=this._dataQueue.Count>0;
							}
							if (useQueuedData)
								postIRData(_dataQueue.Dequeue(),stream);
							else
								postIRData(makeFakeIRData(),stream);
							//Logger.log(MethodBase.GetCurrentMethod(),"ending");

						}
					}
					// await connection
					if (dataFound) {
						Logger.log(MethodBase.GetCurrentMethod(),"no data");
						dataFound=false;
					}
					Thread.Sleep(100);
				}
			}
			stream.Close();
			stream.Dispose();
#if TRACE
			Trace.IndentLevel--;
#endif
			logger.log(MethodBase.GetCurrentMethod(),"ends");
		}

		static string makeFakeIRData() {
			StringBuilder sb = new StringBuilder();

			for (int fldIdx = 0; fldIdx<MAX_NUMBER_IR_FIELDS; fldIdx++) {
				if (fldIdx>0)
					sb.Append(",");
				sb.Append((fldIdx+1).ToString("000#"));
			}
			return sb.ToString();
		}

		static void postIRData(string v,Stream s) {
			byte[] outData;

			outData=Encoding.ASCII.GetBytes(v.ToString()+"\0");
			s.Write(outData,0,outData.Length);
		}

		internal void shutdown() {
			_mre.Set();
		}

		internal void sendIRData() {
			lock (_generateIRDataLock) {
				if (!_generateIRData)
					_generateIRData=true;
			}
		}

		#region IDisposable Support
		  bool _alreadyDisposed = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing) {
			logger.log(MethodBase.GetCurrentMethod());
			if (!_alreadyDisposed) {
				logger.log(MethodBase.GetCurrentMethod(),"need to dispose!");
				if (disposing) {
					logger.log(MethodBase.GetCurrentMethod(),"yes, we're disposing");
					// TODO: dispose managed state (managed objects).
					logger.log(MethodBase.GetCurrentMethod(),"callin shutdown");
					shutdown();
					logger.log(MethodBase.GetCurrentMethod(),"waiting");
					waitHandle.WaitOne();
					logger.log(MethodBase.GetCurrentMethod(),"wait complete!");
				} else {
					logger.log(MethodBase.GetCurrentMethod(),"NOT disposing");
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.
				_alreadyDisposed=true;
			} else {
				logger.log(MethodBase.GetCurrentMethod(),"ack, already disposed!");
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~MyListenerThread() {
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

		internal void setSerialNumber(string text) {
			logger.log(MethodBase.GetCurrentMethod(),"have SN='"+text+"'!");
		}
		#endregion
	}
}