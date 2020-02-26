
using OpenProtocolInterpreter;
using OpenProtocolInterpreter.Communication;
using OpenProtocolInterpreter.Job;
using OpenProtocolInterpreter.Job.Advanced;
using OpenProtocolInterpreter.KeepAlive;
using OpenProtocolInterpreter.Vin;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace NSTcp_listener.Threads {
	partial class MyThread {

		//internal bool isRunning;

		void handleResponse(string data,NetworkStream stream) {
			int mid;

			Logger.log(MethodBase.GetCurrentMethod(),"Data=["+data+"]");
			switch (mid=readMid(data)) {
				case 1:
					// respond with 2 or 4;
					sendReply(stream,new Mid0002(),mid);
					break;
				case 34: handleJobInfoSubscription(stream,data); break;
				case 38: handleSelectJob(stream,data); break;
				case 50: handleVehicleDownloadRequest(stream,data); break;
				case 60: sendTighteningSubscriptionReply(stream,data); break;
				case 127: handleAbortJob(stream,data); break;
				case 9999:
					sendReply(stream,new Mid9999(),mid,false);
					break;
				default:
					Logger.log(MethodBase.GetCurrentMethod(),"unhandled MID="+mid+".");
					break;
			}
		}

		void handleAbortJob(NetworkStream stream,string package) {
			Mid0127 m127 = MidInterpreterMessagesExtensions.UseAllMessages(_mi).Parse<Mid0127>(package);

			sendReply(stream,new Mid0005(m127.HeaderData.Mid),m127.HeaderData.Mid);
		}

		void handleVehicleDownloadRequest(NetworkStream stream,string package) {
			Mid0050 m50 = MidInterpreterMessagesExtensions.UseAllMessages(_mi).Parse<Mid0050>(package);

			_currentVIN=m50.VinNumber;
			sendReply(stream,new Mid0005(m50.HeaderData.Mid),m50.HeaderData.Mid);
		}

		void handleSelectJob(NetworkStream stream,string package) {
			// 0005 if accepted, 0004 if invalid job, or invalid data.
			//Mid oldMid=MidInterpreterMessagesExtensions.UseAllMessages(_mi).Parse(package);
			Mid0038 m38 = MidInterpreterMessagesExtensions.UseAllMessages(_mi).Parse<Mid0038>(package);
			int jobId;

			jobId=m38.JobId;
			sendReply(stream,new Mid0005(m38.HeaderData.Mid),m38.HeaderData.Mid);
		}

		void handleJobInfoSubscription(NetworkStream stream,string package) {
			// 0005 if accepted, 0004 if already exists.
			// reply with 0005 for accepted, 0004 for error.
			Mid oldMid = MidInterpreterMessagesExtensions.UseAllMessages(_mi).Parse(package);
			Mid mid;
			Mid0004 m4;
			Mid0005 m5;
			if (_subscribedJobInfo) {
				Logger.log(MethodBase.GetCurrentMethod(),"already subscribed");
				mid=m4=new Mid0004(oldMid.HeaderData.Mid,Error.JOB_INFO_SUBSCRIPTION_ALREADY_EXISTS);
			} else {
				Logger.log(MethodBase.GetCurrentMethod(),"new subscription");
				mid=m5=new Mid0005(oldMid.HeaderData.Mid);
				_subscribedJobInfo=true;
			}
			sendReply(stream,mid,oldMid.HeaderData.Mid);
		}

		void sendTighteningSubscriptionReply(NetworkStream stream,string package) {
			// reply with 0005 for accepted, 0004 for error.
			Mid oldMid = MidInterpreterMessagesExtensions.UseAllMessages(_mi).Parse(package);
			Mid mid;
			Mid0004 m4;
			Mid0005 m5;
			if (_subscribedTightening) {
				Logger.log(MethodBase.GetCurrentMethod(),"already subscribed");
				mid=m4=new Mid0004(oldMid.HeaderData.Mid,Error.SUBSCRIPTION_ALREADY_EXISTS);
			} else {
				Logger.log(MethodBase.GetCurrentMethod(),"new subscription");
				mid=m5=new Mid0005(oldMid.HeaderData.Mid);
				_subscribedTightening=true;
			}
			sendReply(stream,mid,oldMid.HeaderData.Mid);
		}

		void sendReply(NetworkStream stream,Mid amid,int midNo,bool logSend = true) {
			byte[] bytes;
			string replyData;

			if (amid!=null) {
				replyData=amid.Pack()+'\0';
				if (logSend)
					Logger.log(MethodBase.GetCurrentMethod(),"Replying with "+amid.GetType().Name+" to Mid"+midNo.ToString("000#")+".");
				bytes=Encoding.ASCII.GetBytes(replyData);
				stream.Write(bytes,0,bytes.Length);
			}
		}

		int readMid(string data) {
			int len, ntmp;
			string tmp;

			if (!string.IsNullOrEmpty(data)&&(len=data.Length)>8) {
				if (int.TryParse(tmp=data.Substring(4,4),out ntmp))
					if (ntmp>=0)
						return ntmp;
			}
			return -1;
		}

	}
}