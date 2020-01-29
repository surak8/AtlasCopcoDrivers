//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;
using OpenProtocolInterpreter.MIDs;
using OpenProtocolInterpreter.MIDs.Alarm;
using OpenProtocolInterpreter.MIDs.Communication;

namespace NSAtlasCopcoBreech {

	public partial class MainWindow : Window, IDisposable {
		MainWindowViewModel _vm;
		MyController _opc;
		CommStatus _prevStatus = CommStatus.Unknown;
		public MainWindow() {
			//OpenProtocol.
			this.DataContext = (_vm = new MainWindowViewModel());
			InitializeComponent();
		}

		//~MainWindow() {
		//}
		void btnStop_Click(object sender, RoutedEventArgs e) {
			Utility.logger.log(MethodBase.GetCurrentMethod());
			try {
				if (_opc != null) {
					_opc.shutdown();
					if (_opc!=null) {
						_opc.close();
						_opc = null;
					}
				}
			} catch (Exception ex) {
				Utility.logger.log(MethodBase.GetCurrentMethod(), ex);
			}
		}

		void btnStart_Click(object sender, RoutedEventArgs e) {
			Utility.logger.log(MethodBase.GetCurrentMethod());
			if (_opc == null) {
				try {
					_opc = new MyController();
					var avar = _opc.initialize(_vm.ipAddress, _vm.portNumber, myMidProc, myDispStatus, myCommStatus);
					Utility.logger.log(MethodBase.GetCurrentMethod());
					if (avar) {
						_opc.ThreadsShutdown+=_opc_ThreadsShutdown;
						_vm.startButtonEnabled = false;
						_vm.stopButtonEnabled = true;
						_vm.newLogFileEnabled=true;
						//_opc.AddLastTighteningResultSubscription();
						//_opc.AlarmSubscribe = true;
						//_opc.RelaySubscribe = true;
						//_opc.DigitalInputSubscribe = true;
					}

				} catch (Exception ex) {
					Utility.logger.log(MethodBase.GetCurrentMethod(), ex);
				}
			}
			//var avar = new OPController();

		}

		void _opc_ThreadsShutdown(object sender, EventArgs e) {
			Utility.logger.log(MethodBase.GetCurrentMethod());
			if (_opc!=null) {
				_opc.ThreadsShutdown-= _opc_ThreadsShutdown;
				_opc.close();
				_opc=null;
				_vm.startButtonEnabled=true;
				_vm.stopButtonEnabled=false;
				_vm.newLogFileEnabled=false;
			}
		}

		void myMidProc(MessageType messageType, MID messageObject, string messagestring) {
			if (messageType == MessageType.KeepAlive)
				return;
			string midType = messagestring.Substring(4, 4);
			int midNo;

			if (Int32.TryParse(midType, out midNo)) {
				switch (midNo) {
					case 2:
						MID_0002 v = new MID_0002();
						v.processPackage(messagestring);
						Utility.logger.log(MethodBase.GetCurrentMethod(), "have " + v.ToString());

						break;
					case 61:
						OpenProtocolInterpreter.MIDs.Tightening.MID_0061 v3 = new OpenProtocolInterpreter.MIDs.Tightening.MID_0061();
						v3.processPackage(messagestring);
						Utility.logger.log(MethodBase.GetCurrentMethod(), "have " + v3.ToString());
						break;
					case 71:
						OpenProtocolInterpreter.MIDs.Alarm.MID_0071 v4 = new OpenProtocolInterpreter.MIDs.Alarm.MID_0071();
						v4.processPackage(messagestring);
						Utility.logger.log(MethodBase.GetCurrentMethod(), "have " + v4.ToString());
						break;
					case 76:
						MID_0076 v2 = new MID_0076();
						v2.processPackage(messagestring);
						Utility.logger.log(
							MethodBase.GetCurrentMethod(),
							"ErrMsg = " + v2.AlarmStatusData.ErrorCode + ", " +
							"Error = " + v2.AlarmStatusData.AlarmStatus + ", " +
							"ControllerReady = " + v2.AlarmStatusData.ControllerReadyStatus + ", " +
							"ToolReady= " + v2.AlarmStatusData.ToolReadyStatus + ", " +
							"Time = " + v2.AlarmStatusData.Time.ToString("ddMMyy hh:mm:ss"));
						//"Error = " + v2.AlarmStatusData.AlarmStatus + ", "
						//"have " + v2.ToString());

						break;
					default: Utility.logger.log(MethodBase.GetCurrentMethod(), "unhandled MID: " + midNo + "."); break;
				}
			}
			//switch(midType)
			//Utility.logger.log(MethodBase.GetCurrentMethod(), "msgtype=" + messageType);
		}

		void myCommStatus(CommStatus cs) {

			if (_prevStatus == cs) return;
			//if (_prevStatus == CommStatus.Unknown) {
			_prevStatus = cs;
			//} else if (commStatus == _prevStatus)
			//    return;
			Utility.logger.log(MethodBase.GetCurrentMethod(), cs.ToString());
		}

		void myDispStatus(string msg) {
			Utility.logger.log(MethodBase.GetCurrentMethod(), "MSG=" + msg);
		}

		#region IDisposable Support
		bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				if (disposing) {
					// TODO: dispose managed state (managed objects).
					if (_opc != null) {
						_opc.Dispose();
						_opc = null;
					}
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~MainWindow() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		void IDisposable.Dispose() {
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			GC.SuppressFinalize(this);
		}
		#endregion


		string _previousLogFile;
		const string KEY="Previous Log folder";

		void showLogData(object sender, RoutedEventArgs e) {
			bool? brc;
			OpenFileDialog ofd;

			if (string.IsNullOrEmpty(_previousLogFile))
				_previousLogFile=Utility.readRegistryValue(KEY, string.Empty);
			ofd=new OpenFileDialog {
				AddExtension=true,
				CheckFileExists=true,
				CheckPathExists=true,
				ValidateNames=true,
				Multiselect=true,
				DefaultExt=".log",
				Filter="Log files (*.log)|*.log|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
				FilterIndex=0,
				Title="Select AC files:",
			};
			ofd.RestoreDirectory=true;
			if (!string.IsNullOrEmpty(_previousLogFile)) {
				ofd.InitialDirectory=Path.GetDirectoryName(_previousLogFile);
				ofd.FileName=Path.GetFileName(_previousLogFile);
			} else {
				ofd.InitialDirectory=MyController.logFilePath;

			}

			if ((brc=ofd.ShowDialog()).HasValue&&brc.Value) {
				if (ofd.FileNames.Length>0) {
					Utility.saveRegistryValue(KEY, _previousLogFile= ofd.FileNames[0]);
					MIDUtil.showMidDetails(ofd.FileNames);
				} else {
					MIDUtil.showMidDetail(_previousLogFile=ofd.FileName);
					Utility.saveRegistryValue(KEY, ofd.FileName);
				}
			}
		}



		void startNewLogFile(object sender, RoutedEventArgs e) {
			if (_opc!=null)
				_opc.createNewLogFile();
			//_vm.createNewLogFile();
		}

		void Window1_Initialized(object sender, EventArgs e) {
			//Utility.logger.log(MethodBase.GetCurrentMethod(), "size: ");
			//_vm.setWindowCoords(this);
			double left,top,width,height;

			if (Utility.retrieveWindowBounds("Window data", out left, out top, out width, out height)) {
				_vm.windowTop=top;
				_vm.windowLeft=left;
				_vm.windowWidth=width;
				_vm.windowHeight=height;
				//Trace.WriteLine("here");
				//if (left!=double.NaN) this.Left=left;
				//if (top!=double.NaN) this.Top=left;
				//if (width!=double.NaN) this.Width=left;
				//if (height!=double.NaN) this.Height=left;
				//if (left!=double.NaN) this.Left=left;
			}
		}

		void Window1_SizeChanged(object sender, SizeChangedEventArgs e) {
			Utility.saveWindowBoundsToRegistry(this,"Window data");
		}

		void Window1_LocationChanged(object sender, EventArgs e) {
			//saveWindowBoundsToRegistry(this);
			//this.lef
			Utility.saveWindowBoundsToRegistry(this, "Window data");
		}

	}
}