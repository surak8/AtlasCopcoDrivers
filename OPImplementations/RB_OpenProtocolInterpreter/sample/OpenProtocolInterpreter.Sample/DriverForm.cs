using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using OpenProtocolInterpreter.Communication;
using OpenProtocolInterpreter.Job;
using OpenProtocolInterpreter.KeepAlive;
using OpenProtocolInterpreter.Sample.Driver;
using OpenProtocolInterpreter.Sample.Driver.Commands;
using OpenProtocolInterpreter.Sample.Driver.Events;
using OpenProtocolInterpreter.Sample.Driver.Helpers;
using OpenProtocolInterpreter.Tightening;

namespace OpenProtocolInterpreter.Sample {
	public partial class DriverForm : Form {
		Timer keepAliveTimer;
		OpenProtocolDriver driver;

		public DriverForm() {
			InitializeComponent();
			keepAliveTimer = new Timer();
			keepAliveTimer.Tick += KeepAliveTimer_Tick;
			keepAliveTimer.Interval = 1000;
		}

		void BtnConnection_Click(object sender, EventArgs e) {
			//Added list of mids i want to use in my interpreter, every another will be desconsidered
			driver = new OpenProtocolDriver(
				new Type[]
			{
				typeof(Mid0002),
				typeof(Mid0005),
				typeof(Mid0004),
				typeof(Mid0003),

				typeof(ParameterSet.Mid0011),
				typeof(ParameterSet.Mid0013),

				typeof(Mid0035),
				typeof(Mid0031),

				typeof(Alarm.Mid0071),
				typeof(Alarm.Mid0074),
				typeof(Alarm.Mid0076),

				typeof(Vin.Mid0052),

				typeof(Mid0061),
				typeof(Mid0065),

				typeof(Time.Mid0081),

				typeof(Mid9999)
			});

			var client = new Ethernet.SimpleTcpClient().Connect(textIp.Text, (int) numericPort.Value);
			if (driver.BeginCommunication(client)) {
				keepAliveTimer.Start();
				connectionStatus.Text = "Connected!";
				connectionStatus.BackColor = Color.Green;
			} else {
				driver = null;
				connectionStatus.Text = "Disconnected!";
				connectionStatus.BackColor = Color.Red;
			}
		}

		void KeepAliveTimer_Tick(object sender, EventArgs e) {
			if (driver.KeepAlive.ElapsedMilliseconds > 10000) //10 sec
			{
				Console.WriteLine($"Sending Keep Alive...");
				var pack = driver.SendAndWaitForResponse(new Mid9999().Pack(), TimeSpan.FromSeconds(10));
				if (pack != null && pack.HeaderData.Mid == Mid9999.MID) {
					lastMessageArrived.Text = Mid9999.MID.ToString();
					Console.WriteLine($"Keep Alive Received");
				} else
					Console.WriteLine($"Keep Alive Not Received");
			}
		}

		/// <summary>
		/// Job info subscribe
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void BtnJobInfoSubscribe_Click(object sender, EventArgs e) {
			Console.WriteLine($"Sending Job Info Subscribe...");
			var pack = driver.SendAndWaitForResponse(new Mid0034().Pack(), TimeSpan.FromSeconds(10));

			if (pack != null) {
				if (pack.HeaderData.Mid == Mid0004.MID) {
					var mid04 = pack as Mid0004;
					Console.WriteLine($@"Error while subscribing (MID 0004):
                                         Error Code: <{mid04.ErrorCode}>
                                         Failed MID: <{mid04.FailedMid}>");
				} else
					Console.WriteLine($"Job Info Subscribe accepted!");
			}

			driver.AddUpdateOnReceivedCommand(typeof(Mid0035), OnJobInfoReceived);
		}

		/// <summary>
		/// Tightening subscribe
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void BtnTighteningSubscribe_Click(object sender, EventArgs e) {

			Console.WriteLine($"Sending Tightening Subscribe...");
			var pack = driver.SendAndWaitForResponse(new Mid0060().Pack(), TimeSpan.FromSeconds(10));

			if (pack != null) {
				if (pack.HeaderData.Mid == Mid0004.MID) {
					var mid04 = pack as Mid0004;
					Console.WriteLine($@"Error while subscribing (MID 0004):
                                         Error Code: <{mid04.ErrorCode}>
                                         Failed MID: <{mid04.FailedMid}>");
				} else
					Console.WriteLine($"Tightening Subscribe accepted!");
			}

			//register command
			driver.AddUpdateOnReceivedCommand(typeof(Mid0061), OnTighteningReceived);
		}


		void BtnSendJob_Click(object sender, EventArgs e) {
			new SendJobCommand(driver).Execute((int) numericJob.Value);
		}

		/// <summary>
		/// Async method from controller, MID 0035 (Job Info)
		/// </summary>
		/// <param name="e"></param>
		void OnJobInfoReceived(MIDIncome e) {
			driver.SendMessage(e.Mid.BuildAckPackage());

			var jobInfo = e.Mid as Mid0035;
			lastMessageArrived.Text = Mid0035.MID.ToString();
			Console.WriteLine($@"JOB INFO RECEIVED (MID 0035): 
                                 Job ID: <{jobInfo.JobId}>
                                 Job Status: <{(int) jobInfo.JobStatus}> ({jobInfo.JobStatus.ToString()})
                                 Job Batch Mode: <{(int) jobInfo.JobBatchMode}> ({jobInfo.JobBatchMode.ToString()})
                                 Job Batch Size: <{jobInfo.JobBatchSize}>
                                 Job Batch Counter: <{jobInfo.JobBatchCounter}>
                                 TimeStamp: <{jobInfo.TimeStamp.ToString("yyyy-MM-dd:HH:mm:ss")}>");
		}

		/// <summary>
		/// Async method from controller, MID 0061 (Last Tightening)
		/// Basically, on every tightening this method will be called!
		/// </summary>
		/// <param name="e"></param>
		void OnTighteningReceived(MIDIncome e) {
			driver.SendMessage(e.Mid.BuildAckPackage());

			var tighteningMid = e.Mid as Mid0061;
			lastMessageArrived.Text = Mid0061.MID.ToString();
			Console.WriteLine($@"TIGHTENING RECEIVED (MID 0061): 
                                 Cell ID: <{tighteningMid.CellId}>
                                 Channel ID: <{tighteningMid.ChannelId}>
                                 Torque Controller Name: <{tighteningMid.TorqueControllerName}>
                                 VIN Number: <{tighteningMid.VinNumber}>
                                 Job ID: <{tighteningMid.JobId}>
                                 Parameter Set ID: <{tighteningMid.ParameterSetId}>
                                 Batch Size: <{tighteningMid.BatchSize}>
                                 Batch Counter: <{tighteningMid.BatchCounter}>
                                 Tightening Status: <{tighteningMid.TighteningStatus}>
                                 Torque Status: <{(int) tighteningMid.TorqueStatus}> ({tighteningMid.TorqueStatus.ToString()})
                                 Angle Status: <{(int) tighteningMid.AngleStatus}> ({tighteningMid.AngleStatus.ToString()})
                                 Torque Min Limit: <{tighteningMid.TorqueMinLimit}>
                                 Torque Max Limit: <{tighteningMid.TorqueMaxLimit}>
                                 Torque Final Target: <{tighteningMid.TorqueFinalTarget}>
                                 Torque: <{tighteningMid.Torque}>
                                 Angle Min Limit: <{tighteningMid.AngleMinLimit}>
                                 Angle Max Limit: <{tighteningMid.AngleMaxLimit}>
                                 Final Angle Target: <{tighteningMid.AngleFinalTarget}>
                                 Angle: <{tighteningMid.Angle}>
                                 TimeStamp: <{tighteningMid.Timestamp.ToString("yyyy-MM-dd:HH:mm:ss")}>
                                 Last Change In Parameter Set: <{tighteningMid.LastChangeInParameterSet.ToString("yyyy-MM-dd:HH:mm:ss")}>
                                 Batch Status: <{(int) tighteningMid.BatchStatus}> ({tighteningMid.BatchStatus.ToString()})
                                 TighteningID: <{tighteningMid.TighteningId}>");
		}

		void BtnSendProduct_Click(object sender, EventArgs e) {
			new DownloadProductCommand(driver).Execute(txtProduct.Text);
		}

		void BtnAbortJob_Click(object sender, EventArgs e) {
			new AbortJobCommand(driver).Execute();
		}
	}

	//namespace NSCurrent {
	/// <summary>logging class.</summary>
	public static class Logger {

		#region fields
		/// <summary>controls logging-style.</summary>
		public static bool logDebug = false;

		/// <summary>controls logging-style.</summary>
		public static bool logUnique = false;

		/// <summary>messages written.</summary>
		static readonly List<string> msgs = new List<string>();

		static readonly object _syncObject = new object();

		static readonly TextWriter tw;

		#endregion fields

		#region methods

		// https://codereview.stackexchange.com/questions/106724/thread-safe-logging-class-in-c-to-use-from-dll

		/// <summary>cctor.</summary>
		static Logger() {
			// Note1:  
			// My intent was that the log file name is same as the date on which the log
			// entry was added by the user - but apparently this approach as below is not
			// correct since it is in static constructor and the same date will be set for the file names even if log entries were added at different dates
			// Note2: The current answer doesn't address this, I would appreciate if someone can tell me where to move this code too to achieve what I want
			const string FMT = "yyyy-MM-dd";
			DateTime now1 = DateTime.Now;
			string strDate = now1.ToString(FMT), fname;

			fname = setupLogPath() + "\\" + strDate + ".log";
			try {
				tw = TextWriter.Synchronized(File.AppendText(fname));
				//tw.au
			} catch (IOException ioe) {
				Trace.WriteLine(ioe.Message);
				fname = setupLogPath() + "\\" + strDate + "_2" + ".log";
				tw = TextWriter.Synchronized(File.AppendText(fname));
			} catch (Exception ex) {
				Trace.WriteLine(ex.Message);
			}
		}

		public static string setupLogPath() {
			string logPath, asmName;
			Assembly asm;
			List<string> keys;

			logPath = null;
			try {
				keys = new List<string>(ConfigurationManager.AppSettings.AllKeys);
				if (keys.Contains("logPath"))
					logPath = ConfigurationManager.AppSettings["logPath"];
			} catch (ConfigurationErrorsException cee) {
				Trace.WriteLine(cee.Message);
			} catch (Exception ex) {
				Trace.WriteLine(ex.Message);
			}
			if (string.IsNullOrEmpty(logPath)) {
				if ((asm = Assembly.GetEntryAssembly()) != null)
					asmName = asm.GetName().Name;
				else {
					asmName = "SomeAssembly";
				}
				logPath = Path.Combine(
					Environment.GetEnvironmentVariable("SYSTEMDRIVE") + "\\",
					"TEMP", "logs", asmName);
			}
			if (!Directory.Exists(logPath))
				Directory.CreateDirectory(logPath);
#if TRACE
			if (!Trace.AutoFlush)
				Trace.AutoFlush = true;
			Trace.WriteLine("logPath=" + logPath);
#endif
			return logPath;
		}

		#region logging-methods
		/// <summary>log a APP_EXIT_QUERY</summary>
		/// <param name="msg"/>
		/// <seealso cref="Debug"/>
		/// <seealso cref="Trace"/>
		/// <seealso cref="logDebug"/>
		/// <seealso cref="logUnique"/>
		/// <seealso cref="msgs"/>
		public static void log(string msg) {
			if (logUnique) {
				if (msgs.Contains(msg))
					return;
				msgs.Add(msg);
			}
#if DEBUG
			if (logDebug)
				Debug.Print("[DEBUG] " + msg);
#endif

#if TRACE
			Trace.WriteLine("[TRACE] " + msg);
			Trace.Flush();
#endif
			if (tw != null) {
				lock (_syncObject) {
#if true
					tw.WriteLine(
						"[" +
						DateTime.Now.ToString("dd-MMM-yy hh:mm:ss.fffff") +
						"] " + msg);
#else
                    tw.WriteLine("{0} {1} {2}", 
                        DateTime.Now.ToLongTimeString(),
                       DateTime.Now.ToLongDateString(), msg);
#endif

					// Where to call flush??
					tw.Flush();
				}
			}
		}

		/// <summary>log a APP_EXIT_QUERY</summary>
		/// <param name="mb"/>
		/// <seealso cref="makeSig"/>
		/// <seealso cref="log(MethodBase,string)"/>
		public static void log(MethodBase mb) {
			log(mb, string.Empty);
		}

		/// <summary>log a APP_EXIT_QUERY</summary>
		/// <param name="mb"/>
		/// <param name="msg"/>
		/// <seealso cref="makeSig"/>
		/// <seealso cref="log(MethodBase,string)"/>
		public static void log(MethodBase mb, string msg) {
			log(makeSig(mb) + ":" + msg);
		}

		public static void log(MethodBase mb, Exception ex) {
			log(mb, ex, false);
		}

		public static void log(MethodBase mb, string msg, Exception ex, bool showStackTrace) {
			log(makeSig(mb) + ":" + msg + Environment.NewLine +
				exceptionValue(ex) +
				(showStackTrace ? (Environment.NewLine + ex.StackTrace) : string.Empty));
		}

		public static void log(MethodBase mb, Exception ex, bool showStackTrace) {
			log(makeSig(mb) + ":" + exceptionValue(ex) +
				(showStackTrace ? (Environment.NewLine + ex.StackTrace) : string.Empty));
		}

		#endregion logging-methods

		#region misc. methods
		/// <summary>create a method-signature.</summary>
		/// <returns></returns>
		public static string makeSig(MethodBase mb) {
			return mb.ReflectedType.Name + "." + mb.Name;
		}

		public static string exceptionValue(Exception ex) {
			StringBuilder sb;
			Exception ex2;

			if ((ex2 = ex) != null) {
				sb = new StringBuilder();
				while (ex2 != null) {
					sb.AppendLine(ex2.Message);
					ex2 = ex2.InnerException;
				}
				return sb.ToString();
			}
			return string.Empty;
		}

		#endregion misc. methods
		#endregion methods
	}
	//}

}