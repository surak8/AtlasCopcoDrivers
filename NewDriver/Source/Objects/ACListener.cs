using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using OpenProtocolInterpreter;
using OpenProtocolInterpreter.Communication;

namespace NSNewDriver {
	class ACListener {
		TcpClient _tcpClient;
		NetworkStream _stream;
		Task _taskReceive;
		DispatcherTimer _keepAliveTimer;

		public ACListener(string addr, int nport) {
			this.ipAddress = addr;
			this.portNumber = nport;
			_logger = new ColtLogger();
		}

		public string ipAddress { get; private set; }
		public int portNumber { get; private set; }

		IColtLogger _logger;

		internal void start() {

			_tcpClient = new TcpClient(ipAddress, portNumber);
			if (_tcpClient.Connected) {
				_stream = _tcpClient.GetStream();
				_taskReceive = Task.Run(() => { receiveThread(); });
				_keepAliveTimer = new DispatcherTimer {
					Interval = new TimeSpan(0, 0, 10),
				};
				_keepAliveTimer.Tick += sendKeepAliveMessage;
			}
		}

		void sendKeepAliveMessage(object sender, EventArgs e) {
			_logger.log(MethodBase.GetCurrentMethod());
		}

		readonly object _streamLock=new object();
		ManualResetEvent _mre=new ManualResetEvent(true);
		bool _commStarted =false;
		bool _startingComm=false;
		void receiveThread() {
			bool exitLoop=false;

			while (!exitLoop) {
				if (_mre.WaitOne(100))
					_logger.log(MethodBase.GetCurrentMethod(), "did wait-one.");
				if (!_commStarted) {
					if (!_startingComm) {
						// send a comm-start message.
						this.sendMessage(new Mid0001());
					}
				}

			}
		}

		readonly MidInterpreter mi=new MidInterpreter();
		byte[] readData=new byte[4096];
		void sendMessage(Mid mid) {
			byte[] data;
			string package,newPackage;
			int len,midNo;
			Mid newMid;

			data = Encoding.ASCII.GetBytes(package = mid.Pack() + '\0');
			lock (_streamLock) {
				_stream.Write(data, 0, data.Length);
				Thread.Sleep(500);
				if (_stream.DataAvailable) {
					//_stream.ReadAsync ()
					len = _stream.Read(readData, 0, readData.Length);
					newPackage = Encoding.ASCII.GetString(readData, 0, len);
					newMid = MidInterpreterMessagesExtensions.UseAllMessages(mi).Parse(newPackage);
					switch (midNo = newMid.HeaderData.Mid) {
						case 1:
							Mid0001 m1=(Mid0001) newMid;
							Trace.WriteLine("here");
							break;
						case 2:
							Mid0002 m2=(Mid0002) newMid;
							Trace.WriteLine("here");
							_commStarted = true;
							_startingComm = false;
							break;
						case 4:
							Mid0004 m4=(Mid0004) newMid;
							Trace.WriteLine("Command error: MID=" + m4.FailedMid + ", Error=" + m4.ErrorCode + ".");
							break;
						default:
							_logger.log(MethodBase.GetCurrentMethod(), "unhandled mid " + midNo);
							break;
					}
					//if (vvv.HeaderData.Mid == 4) {
					//	Mid0004  m4=MidInterpreterMessagesExtensions.UseAllMessages(mi).Parse<Mid0004>(newPackage);
					//}
				}
			}
		}
	}
}