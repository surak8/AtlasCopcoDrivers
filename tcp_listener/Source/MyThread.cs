using OpenProtocolInterpreter;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NSTcp_listener.Threads {
	partial class MyThread {
		#region fields
		Thread _thread;
		TcpListener _server;
		ManualResetEvent _mre;
		readonly MidInterpreter _mi;
		bool _subscribedTightening;
		bool _subscribedJobInfo;
		string _currentVIN;
		readonly object _generateIRDataLock = new object();
		bool _generateIRData = false;
		#endregion

		#region ctor
		public MyThread(string anIPAddress,int nPort) {
			Logger.log(MethodBase.GetCurrentMethod());
			this.ipAddress=anIPAddress;
			this.portNumber=nPort;
			_mi=new MidInterpreter();
		}
		#endregion

		#region properties
		public string ipAddress { get; }
		public int portNumber { get; }

		public WaitHandle waitHandle { get { return _mre; } }

		#endregion

		#region start/stop methods
		internal void start() {
			Logger.log(MethodBase.GetCurrentMethod(),"creating thread");
			this._thread=new Thread(this.runThread2);
			Logger.log(MethodBase.GetCurrentMethod(),"starting thread");
			this._thread.Start(this);
			isRunning=true;
		}

		private void runThread2() {
			throw new NotImplementedException();
		}

		public bool isRunning { get; private set; }

		internal void stop() {
			Logger.log(MethodBase.GetCurrentMethod(),"starting");
			//_mre.Set();
			_mre.Reset();
			Logger.log(MethodBase.GetCurrentMethod(),"ending");
		}

		internal void generateIRTransaction() {
			Logger.log(MethodBase.GetCurrentMethod(),"starting");
			lock (_generateIRDataLock) {
				_generateIRData=true;
			}
			Logger.log(MethodBase.GetCurrentMethod(),"ending");
		}
		#endregion


		class MyObj{}

		async void runThread1(object anObj) {
			bool _exitLoop = false;
			TcpClient client = null;
			NetworkStream stream = null;
			byte[] bytes = new byte[1028];
			string data;
			int i, amid;
			bool dataFound = false;
			StringBuilder sbIRData = null;

			_mre=new ManualResetEvent(true);
			Logger.log(MethodBase.GetCurrentMethod(),"ending");

			try {
				_server=new TcpListener(IPAddress.Parse(ipAddress),portNumber);
				_server.Server.SetSocketOption(SocketOptionLevel.Socket,SocketOptionName.ReuseAddress,1);
				Logger.log(MethodBase.GetCurrentMethod(),"server on "+ipAddress+":"+portNumber+".");
				_server.Start();
				while (!_exitLoop) {
					Trace.WriteLine("loop-top");
					//var something = await _server.AcceptTcpClientAsync().ConfigureAwait(false);
					//var something = await _server.AcceptTcpClientAsync();
					//var something2 = await _server.AcceptTcpClientAsync().ContinueWith ()

					//await _server.AcceptTcpClientAsync().ContinueWith(t => onAccept(t),TaskScheduler.Default);
					//await _server.AcceptTcpClientAsync().GetAwaiter()
					var vvv = _server.BeginAcceptTcpClient(tester,new MyObj());
					if (_mre.WaitOne(100)) {
						Debug.Print("here");
						_exitLoop=true;
					}
					Trace.WriteLine("create client?");

					//var cw = new ClientWorking(something,true);
					//cw.DoSomethingWithClientAsync().NoWarning();
					//if (_mre.WaitOne(100)) {
					//	Debug.Print("here");
					//	_exitLoop=true;
					//}
				}

					//}
					_server.Stop();
					//_server.AcceptTcpClientAsync ()
					// this needs to be ASYNC connection
					//var avar = _server.AcceptTcpClientAsync();
					//if (avar.)
					//client=_server.AcceptTcpClient();
					//Trace.WriteLine("here");
					//stream=client.GetStream();
					//while (!_exitLoop) {
					//	if (_mre.WaitOne(100)) {
					//		Debug.Print("here");
					//		_exitLoop=true;
					//	}
					//	if (stream.DataAvailable) {
					//		i=stream.Read(bytes,0,bytes.Length);
					//		if (i<1) {
					//			client.Close();
					//		} else {
					//			data=Encoding.ASCII.GetString(bytes,0,i);

					//			if ((amid=readMid(data))!=9999)
					//				Trace.WriteLine("read: "+data);
					//			handleResponse(data,stream);
					//			dataFound=true;
					//		}

					//	} else {
					//		lock (_generateIRDataLock) {
					//			if (_generateIRData) {
					//				Logger.log(MethodBase.GetCurrentMethod(),"starting");
					//				// create a line of 23 fields, send out through the stream.
					//				_generateIRData=false;
					//				if (sbIRData==null)
					//					sbIRData=new StringBuilder();
					//				int MAX_NUMBER_IR_FIELDS = 23;
					//				for (int fldIdx = 0; fldIdx<MAX_NUMBER_IR_FIELDS; fldIdx++) {
					//					if (fldIdx>0)
					//						sbIRData.Append(",");
					//					sbIRData.Append(fldIdx.ToString("000#"));
					//					Logger.log(MethodBase.GetCurrentMethod(),"send: ["+sbIRData.ToString()+"]");
					//				}
					//				Logger.log(MethodBase.GetCurrentMethod(),"ending");

					//			}
					//		}
					//		// await connection
					//		if (dataFound) {
					//			Logger.log(MethodBase.GetCurrentMethod(),"no data");
					//			dataFound=false;
					//		}
					//		Thread.Sleep(100);
					//	}
					//}

					stream.Close();
				client.Close();
			} catch (Exception ex) {
				Logger.log(MethodBase.GetCurrentMethod(),ex);
			} finally {
				stream.Close();
				client.Close();
			}
			Logger.log(MethodBase.GetCurrentMethod(),"exiting");
		}

		  void tester(IAsyncResult ar) {
			bool exitLoop = false;
			Logger.log(MethodBase.GetCurrentMethod(),"starts");

			while (!exitLoop) {
				Logger.log(MethodBase.GetCurrentMethod());
			}
			Logger.log(MethodBase.GetCurrentMethod(),"ends");
		}

		void onAccept(Task t) {
			bool exitLoop = false;

			while (!exitLoop) {
				Logger.log(MethodBase.GetCurrentMethod());
			}
		}
	}


	class ClientWorking {
		TcpClient _client;
		bool _ownsClient;

		public ClientWorking(TcpClient client,bool ownsClient) {
			_client=client;
			_ownsClient=ownsClient;
		}

		public async Task DoSomethingWithClientAsync() {
			try {
				using (var stream = _client.GetStream()) {
					using (var sr = new StreamReader(stream))
					using (var sw = new StreamWriter(stream)) {
						await sw.WriteLineAsync("Hi. This is x2 TCP/IP easy-to-use server").ConfigureAwait(false);
						await sw.FlushAsync().ConfigureAwait(false);
						var data = default(string);
						while (!((data=await sr.ReadLineAsync().ConfigureAwait(false)).Equals("exit",StringComparison.OrdinalIgnoreCase))) {
							await sw.WriteLineAsync(data).ConfigureAwait(false);
							await sw.FlushAsync().ConfigureAwait(false);
						}
					}

				}
			} finally {
				if (_ownsClient&&_client!=null) {
					(_client as IDisposable).Dispose();
					_client=null;
				}
			}
		}
	}

	static class TaskExtensions {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NoWarning(this Task t) { }
	}
}