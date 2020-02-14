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
#if !OTHER_VERSION
using OpenProtocolInterpreter.MIDs;
using OpenProtocolInterpreter.MIDs.Alarm;
using OpenProtocolInterpreter.MIDs.Communication;
#endif

namespace NSAtlasCopcoBreech {

	public partial class MainWindow : Window, IDisposable {
		MainWindowViewModel _vm;
		MyController _opc;
		CommStatus _prevStatus = CommStatus.Unknown;
		public MainWindow() {
			this.DataContext = (_vm = new MainWindowViewModel());
			InitializeComponent();
		}

		void btnStop_Click(object sender, RoutedEventArgs e) {
			Utility.logger.log(MethodBase.GetCurrentMethod());
			try {
				if (_opc != null) {
					_opc.shutdown();
					if (_opc != null) {
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
						_opc.ThreadsShutdown += _opc_ThreadsShutdown;
						_vm.startButtonEnabled = false;
						_vm.stopButtonEnabled = true;
						_vm.newLogFileEnabled = true;
					}

				} catch (Exception ex) {
					Utility.logger.log(MethodBase.GetCurrentMethod(), ex);
				}
			}
		}

		void _opc_ThreadsShutdown(object sender, EventArgs e) {
			Utility.logger.log(MethodBase.GetCurrentMethod());
			if (_opc != null) {
				_opc.ThreadsShutdown -= _opc_ThreadsShutdown;
				_opc.close();
				_opc = null;
				_vm.startButtonEnabled = true;
				_vm.stopButtonEnabled = false;
				_vm.newLogFileEnabled = false;
			}
		}

#if OTHER_VERSION
		void myMidProc(MessageType messageType, object messageObject, string messagestring) {
#else
		void myMidProc(MessageType messageType, MID messageObject, string messagestring) {
#endif
			if (messageType == MessageType.KeepAlive)
				return;
			string midType = messagestring.Substring(4, 4);
			int midNo;

			if (Int32.TryParse(midType, out midNo)) {
				switch (midNo) {
#if !OTHER_VERSION
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

						break;
#endif
					default: Utility.logger.log(MethodBase.GetCurrentMethod(), "unhandled MID: " + midNo + "."); break;
				}
			}
		}

		void myCommStatus(CommStatus cs) {
			if (_prevStatus == cs) return;
			_prevStatus = cs;
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
		const string KEY = "Previous Log folder";

		enum LogFileProcessType {
			NONE = -1,
			MakeHumanReadable,
			GenerateCSV
		}
		void showLogFileData(object sender, RoutedEventArgs e) {
			doShowLogFileContent(LogFileProcessType.MakeHumanReadable);
		}

		void condenseToCSV(object sender, RoutedEventArgs e) {
			doShowLogFileContent(LogFileProcessType.GenerateCSV);
		}


		void doShowLogFileContent(LogFileProcessType lfpt) {
			bool? brc;
			OpenFileDialog ofd;
			string dirName;

			if (lfpt == LogFileProcessType.NONE) {
				MessageBox.Show("Unhandled processing-type '" + lfpt + "'." + Environment.NewLine + "Cannot continue.", "Log-file processing");
				return;
			}
			if (string.IsNullOrEmpty(_previousLogFile)) {
				_previousLogFile = Utility.readRegistryValue(KEY, string.Empty);
				if (!File.Exists(_previousLogFile)) {
					_previousLogFile = null;
					Utility.saveRegistryValue(KEY, string.Empty);
				}
			}
			ofd = new OpenFileDialog {
				AddExtension = true,
				CheckFileExists = true,
				CheckPathExists = true,
				ValidateNames = true,
				Multiselect = true,
				DefaultExt = ".log",
				Filter = "Log files (*.log)|*.log|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
				FilterIndex = 0,
				Title = "Select AC files:",
			};
			ofd.RestoreDirectory = true;
			if (!string.IsNullOrEmpty(_previousLogFile)) {
				dirName = Path.GetDirectoryName(_previousLogFile);
				if (!string.IsNullOrEmpty(ofd.InitialDirectory))
					if (!Directory.Exists(ofd.InitialDirectory))
						Directory.CreateDirectory(ofd.InitialDirectory);
				ofd.InitialDirectory = dirName;
				if (!string.IsNullOrEmpty(_previousLogFile))
					if (File.Exists(_previousLogFile))
						ofd.FileName = Path.GetFileName(_previousLogFile);
			} else {
				ofd.InitialDirectory = MyController.logFilePath;
			}

			if ((brc = ofd.ShowDialog()).HasValue && brc.Value) {
				string[] allFiles;
				if (ofd.FileNames.Length > 0) {
					allFiles = ofd.FileNames;
				} else {
					allFiles = new string[] { ofd.FileName };
				}

				Utility.saveRegistryValue(KEY, _previousLogFile = ofd.FileNames[0]);
				switch (lfpt) {
					case LogFileProcessType.MakeHumanReadable: MIDUtil.showMidDetails(ofd.FileNames); break;
#if !OTHER_VERSION
					case LogFileProcessType.GenerateCSV: new CSVGenerator<MIDIdentifier, MID>().generateCSV(Path.Combine(MIDUtil.midLogPath, "CondensedTightening.csv"), allFiles); break;
#endif
					default:
						MessageBox.Show("Unhandled processing-type '" + lfpt + "'." + Environment.NewLine + "Cannot continue.", "Log-file processing");
						break;
				}
			}
		}

		void startNewLogFile(object sender, RoutedEventArgs e) {
			if (_opc != null)
				_opc.createNewLogFile();
		}

		const string FRAME_KEY = "Main Window Frame";

		void Window1_Initialized(object sender, EventArgs e) {
			double left, top, width, height;

			if (Utility.retrieveWindowBounds(FRAME_KEY, out left, out top, out width, out height)) {
				//System.Diagnostics.Trace.WriteLine("Here");
				_vm.windowTop = top;
				_vm.windowLeft = left;
				_vm.windowWidth = width;
				_vm.windowHeight = height;
			} else {
				_vm.windowWidth = 325;
				_vm.windowHeight = 225;
				_vm.windowLeft = (SystemParameters.PrimaryScreenWidth - _vm.windowWidth) / 2.0;
				_vm.windowTop = (SystemParameters.PrimaryScreenHeight - _vm.windowHeight) / 2.0;
			}
		}

		void Window1_SizeChanged(object sender, SizeChangedEventArgs e) {
			Utility.saveWindowBoundsToRegistry(this, FRAME_KEY);
		}

		void Window1_LocationChanged(object sender, EventArgs e) {
			Utility.saveWindowBoundsToRegistry(this, FRAME_KEY);
		}

		void testFunction(object sender, RoutedEventArgs e) {
#if OTHER_VERSION
			const string ASM_NAME = "RB_OpenProtocolInterpreter";

			var v2 = new OpenProtocolInterpreter.MidInterpreter();
#else
			const string ASM_NAME = "OpenProtocolInterpreter";

			var v2 = new OpenProtocolInterpreter.MIDs.MIDIdentifier();
#endif
			foreach (var v in AppDomain.CurrentDomain.GetAssemblies()) {

				if (v.GetName().Name.CompareTo(ASM_NAME) == 0) {
					generateTestsFrom(v);
					break;
				}
			}
		}

		void readPSets(object sender, RoutedEventArgs e) {
			if (_opc != null)
				_opc.readPSets();
		}
	}
}