using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using OpenProtocolInterpreter;
using OpenProtocolInterpreter.Communication;
using OpenProtocolInterpreter.Job;
using OpenProtocolInterpreter.Job.Advanced;
using OpenProtocolInterpreter.KeepAlive;
using OpenProtocolInterpreter.Vin;

namespace NSTcp_listener.Threads {
	class MyThread {
		//private readonly string ipAddress;
		Thread _thread;
		TcpListener _server;
		ManualResetEvent _mre;
		readonly          MidInterpreter _mi;
		bool _subscribedTightening;
		private bool _subscribedJobInfo;
		private string _currentVIN;

		//string v1;
		//int v2;

		public MyThread(string anIPAddress, int nPort) {
			Logger.log(MethodBase.GetCurrentMethod());
			//this.v1 = v1;
			//this.v2 = v2;
			this.ipAddress = anIPAddress;
			this.portNumber = nPort;
			_mi=new MidInterpreter();
		}

		public string ipAddress { get; }
		public int portNumber { get; }

		public WaitHandle waitHandle { get { return _mre; } }

		//private object runThread;

		internal void start() {
			Logger.log(MethodBase.GetCurrentMethod(), "creating thread");
			this._thread = new Thread(this.runThread);
			Logger.log(MethodBase.GetCurrentMethod(), "starting thread");
			this._thread.Start(this);
		}
		void runThread(object anObj) {
			bool _exitLoop = false;
			TcpClient client=null;
			NetworkStream stream=null;
			byte[] bytes = new byte[1028];
			string data;
			int i,amid;
			bool dataFound=false;

			//_mi=new MidInterpreter();
			//_mi.

			_mre = new ManualResetEvent(false);
		 
			try {
				_server = new TcpListener(IPAddress.Parse(ipAddress), portNumber);
				_server.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
				_server.Start();
				// this needs to be ASYNC connection
				client = _server.AcceptTcpClient();
				//client=_server.acceptt
				stream = client.GetStream();
				while (!_exitLoop) {
					if (_mre.WaitOne(100)) {
						//Debug.Print("here");
						_exitLoop=true;
					}
					if (stream.DataAvailable) {
						i = stream.Read(bytes, 0, bytes.Length);
						if (i < 1) {
							//_mre.Reset();
							client.Close();
						} else {
							data = Encoding.ASCII.GetString(bytes, 0, i);

							if ((amid=readMid(data))!=9999)
								Trace.WriteLine("read: " + data);
							handleResponse(data, stream);
							dataFound=true;
						}

					} else {
						// await connection
						if (dataFound) {
							Logger.log(MethodBase.GetCurrentMethod(), "no data");
							dataFound=false;
						}
						//_thread.sl
						Thread.Sleep(100);
					}
					//if (_mre.WaitOne()) {
					//	Debug.Print("here");
					//}
					//client.Close
				}
				stream.Close();
				client.Close();
			} catch (Exception ex) {
				Logger.log(MethodBase.GetCurrentMethod(), ex);
			} finally {
				stream.Close();
				client.Close();
			}
			Logger.log(MethodBase.GetCurrentMethod(), "exiting");
		}

		void handleResponse(string data, NetworkStream stream) {
			int mid;
			//string replyData;
			//byte[] bytes;
			//OpenProtocolInterpreter.Mid opmid;

			Logger.log(MethodBase.GetCurrentMethod(), "Data=[" + data + "]");
			switch (mid = readMid(data)) {
				case 1:
					// respond with 2 or 4;
					sendReply(stream, new Mid0002(), mid);
					break;
				case 34: handleJobInfoSubscription(stream, data); break;
				case 38: handleSelectJob(stream, data); break;
				case 50: handleVehicleDownloadRequest(stream, data); break;
				case 60: sendTighteningSubscriptionReply(stream, data); break;
				case 127: handleAbortJob(stream, data); break;
				case 9999:
					sendReply(stream, new Mid9999(), mid, false);
					//replyData = (opmid = new OpenProtocolInterpreter.KeepAlive.Mid9999()).Pack();
					//Logger.log(MethodBase.GetCurrentMethod(), "Replying with " + opmid.GetType().Name + " in response.");
					//bytes = Encoding.ASCII.GetBytes(replyData+'\0');
					//stream.Write(bytes, 0, bytes.Length);
					break;
				default:
					Logger.log(MethodBase.GetCurrentMethod(), "unhandled MID=" + mid + ".");
					break;
			}
		}

		void handleAbortJob(NetworkStream stream, string package) {
			Mid0127 m127=MidInterpreterMessagesExtensions.UseAllMessages(_mi).Parse<Mid0127>(package);
			//Logger.log(MethodBase.GetCurrentMethod());
			sendReply(stream, new Mid0005(m127.HeaderData.Mid), m127.HeaderData.Mid);
		}

		void handleVehicleDownloadRequest(NetworkStream stream, string package) {
			Mid0050 m50=MidInterpreterMessagesExtensions.UseAllMessages(_mi).Parse<Mid0050>(package);

			_currentVIN=m50.VinNumber;
			sendReply(stream, new Mid0005(m50.HeaderData.Mid), m50.HeaderData.Mid);
		}

		void handleSelectJob(NetworkStream stream, string package) {
			// 0005 if accepted, 0004 if invalid job, or invalid data.
			//Mid oldMid=MidInterpreterMessagesExtensions.UseAllMessages(_mi).Parse(package);
			Mid0038 m38=MidInterpreterMessagesExtensions.UseAllMessages(_mi).Parse<Mid0038>(package);
			int jobId;
			//Logger.log(MethodBase.GetCurrentMethod());
			//var avar1=new Mid0038().p
			//jobId=oldMid.
			//Mid0038 m38=MidInterpreterMessagesExtensions.UseAllMessages(_mi).Parse<Mid0038>(package);
			//var avar=new Mid0038().Parse(package);
			//var vvv=new Mid0038().Parse<Mid0038>(package);
			jobId=m38.JobId;
			sendReply(stream, new Mid0005(m38.HeaderData.Mid), m38.HeaderData.Mid);
		}

		void handleJobInfoSubscription(NetworkStream stream, string package) {
			// 0005 if accepted, 0004 if already exists.
			// reply with 0005 for accepted, 0004 for error.
			Mid oldMid=MidInterpreterMessagesExtensions.UseAllMessages(_mi).Parse(package);
			Mid mid;
			Mid0004 m4;
			Mid0005 m5;
			if (_subscribedJobInfo) {
				Logger.log(MethodBase.GetCurrentMethod(), "already subscribed");
				mid=m4=new Mid0004(oldMid.HeaderData.Mid, Error.JOB_INFO_SUBSCRIPTION_ALREADY_EXISTS);
			} else {
				Logger.log(MethodBase.GetCurrentMethod(), "new subscription");
				mid=m5=new Mid0005(oldMid.HeaderData.Mid);
				_subscribedJobInfo=true;
			}
			sendReply(stream, mid, oldMid.HeaderData.Mid);
		}

		void sendTighteningSubscriptionReply(NetworkStream stream, string package) {
			// reply with 0005 for accepted, 0004 for error.
			Mid oldMid=MidInterpreterMessagesExtensions.UseAllMessages(_mi).Parse(package);
			Mid mid;
			Mid0004 m4;
			Mid0005 m5;
			if (_subscribedTightening) {
				Logger.log(MethodBase.GetCurrentMethod(), "already subscribed");
				mid=m4=new Mid0004(oldMid.HeaderData.Mid, Error.SUBSCRIPTION_ALREADY_EXISTS);
			} else {
				Logger.log(MethodBase.GetCurrentMethod(), "new subscription");
				mid=m5=new Mid0005(oldMid.HeaderData.Mid);
				_subscribedTightening=true;
			}
			sendReply(stream, mid, oldMid.HeaderData.Mid);
		}

		void sendReply(NetworkStream stream, Mid amid, int midNo, bool logSend = true) {
			byte[] bytes;
			string replyData ;

			if (amid != null) {
				replyData = amid.Pack() + '\0';
				if (logSend)
					Logger.log(MethodBase.GetCurrentMethod(), "Replying with " + amid.GetType().Name + " to Mid" + midNo.ToString("000#") + ".");
				bytes = Encoding.ASCII.GetBytes(replyData);
				stream.Write(bytes, 0, bytes.Length);
			}
		}

		int readMid(string data) {
			int len, ntmp;
			string tmp;

			if (!string.IsNullOrEmpty(data) && (len = data.Length) > 8) {
				if (int.TryParse(tmp = data.Substring(4, 4), out ntmp))
					if (ntmp >= 0)
						return ntmp;
			}
			return -1;
		}

		internal void stop() {
			Logger.log(MethodBase.GetCurrentMethod(), "starting");
			_mre.Set();
			//_mre.Reset();
			Logger.log(MethodBase.GetCurrentMethod(), "ending");
		}
	}
}