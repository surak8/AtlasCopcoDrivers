
#if !OTHER_VERSION
#if false
using System;
using System.Collections.Generic;

namespace OpenProtocolInterpreter {
	namespace MIDs {
		namespace ApplicationSelector {
			public class MID_0250 : MID { public MID_0250() : base(20, 250, 1) { } }
		}
		namespace ApplicationToolLocationSystem {
			public class MID_0261 : MID { public MID_0261() : base(20, 261, 1) { } }
		}
		namespace Communication {
			public class MID_0001 : MID {
				public MID_0001() : base(20, 1, 1) { }
			}
			public class MID_0002 : MID { public MID_0002() : base(20, 1, 1) { } }
			public class MID_0003 : MID { public MID_0003() : base(20, 1, 1) { } }
			public class MID_0004 : MID {
				public MID_0004() : base(20, 1, 1) { }
				public string ErrorCode { get; internal set; }
				public string FailedMid { get; internal set; }
			}
			public class MID_0005 : MID {
				public MID_0005() : base(20, 5, 1) { }
				public int MIDAccepted { get; internal set; }
			}
		}
		namespace IOInterface {
			public class MID_0210 : MID { public MID_0210() : base(20, 210, 1) { } }
			public class MID_0212 : MID { public MID_0212() : base(20, 212, 1) { } }
			public class MID_0216 : MID { public MID_0216() : base(20, 216, 1) { } }
			public class MID_0220 : MID { public MID_0220() : base(20, 220, 1) { } }


		}
		namespace KeepAlive {
			public class MID_9999 : MID { public MID_9999() : base(20, 9999, 1) { } }
		}
		namespace MultipleIdentifiers {
			public class MID_0151 : MID { public MID_0151() : base(20, 151, 1) { } }
			public class MID_0153 : MID { public MID_0153() : base(20, 153, 1) { } }
		}
		namespace OpenProtocolCommandsDisabled {
			public class MID_0420 : MID { public MID_0420() : base(20, 242, 1) { } }
		}
		namespace ParameterSet {
			public class MID_0010 : MID { public MID_0010() : base(20, 1, 1) { } }
			public class MID_0011 : MID {
				public MID_0011() : base(20, 1, 1) { }
				public int TotalParameterSets { get; internal set; }
				public List<int> ParameterSets { get; set; }
			}
			public class MID_0013 : MID { public MID_0013() : base(20, 1, 1) { } }
			public class MID_0014 : MID { public MID_0014() : base(20, 1, 1) { } }
			public class MID_0015 : MID { public MID_0015() : base(20, 15, 1) { }

				public int ParameterSetID { get; internal set; }
			}
			public class MID_0016 : MID { public MID_0016() : base(20, 16, 1) { } }
			public class MID_0023 : MID { public MID_0023() : base(20, 23, 1) { } }
		}
		namespace Tightening {
			//public class c1 { }
			public class MID_0060 : MID { public MID_0060() : base(20, 1, 1) { } }
			public class MID_0061 : MID {
				public MID_0061() : base(20, 1, 1) { }
				public int TighteningID { get; internal set; }
				public object BatchCounter { get; internal set; }
				public object TorqueStatus { get; internal set; }
				public object BatchSize { get; internal set; }
				public object AngleStatus { get; internal set; }
				public object ParameterSetID { get; internal set; }
				public object TighteningStatus { get; internal set; }
				public object Torque { get; internal set; }
				public object TorqueFinalTarget { get; internal set; }
				public object TorqueMinLimit { get; internal set; }
				public object TorqueMaxLimit { get; internal set; }
			}
			public class MID_0064 : MID { public MID_0064() : base(20, 1, 1) { } }
			public class MID_0062 : MID { public MID_0062() : base(20, 62, 1) { } }
			public class MID_0065 : MID { public MID_0065() : base(20, 65, 1) { }

				public int TighteningID { get; internal set; }
			}

		}
		namespace VIN {
			public class MID_0051 : MID { public MID_0051() : base(20, 51, 1) { } }
			public class MID_0052 : MID { public MID_0052() : base(20, 52, 1) { } }
			public class MID_0054 : MID { public MID_0054() : base(20, 54, 1) { } }
		}

		namespace Alarm {
			public class AlarmStatusData {
				public string ErrorCode { get; internal set; }
				public string ControllerReadyStatus { get; internal set; }
				public string AlarmStatus { get; internal set; }
				public DateTime Time { get; internal set; }
				public string ToolReadyStatus { get; internal set; }
			}

			//public class c1 { }
			public class MID_0070 : MID { public MID_0070() : base(20, 1, 1) { } }
			public class MID_0071 : MID { public MID_0071() : base(20, 1, 1) { } }
			public class MID_0072 : MID { public MID_0072() : base(20, 1, 1) { } }
			public class MID_0074 : MID {
				public MID_0074() : base(20, 1, 1) { }
				public string ErrorCode { get; internal set; }
			}
			public class MID_0075 : MID { public MID_0075() : base(20, 1, 1) { } }
			public class MID_0076 : MID {
				public MID_0076() : base(20, 1, 1) { }
				public AlarmStatusData AlarmStatusData { get; set; }
			}
			public class MID_0077 : MID { public MID_0077() : base(20, 1, 1) { } }
		}
		namespace Job {
			namespace Advanced {
				public class MID_0120 : MID { public MID_0120() : base(20, 120, 1) { } }
			}
			public class MID_0030 : MID { public MID_0030() : base(20, 30, 1) { } }
			public class MID_0051 : MID { public MID_0051() : base(20, 51, 1) { } }
			public class MID_0031 : MID { public MID_0031() : base(20, 31, 1) { }

				public IEnumerable<int> JobIds { get; internal set; }
			}
			public class MID_0034 : MID { public MID_0034() : base(20, 34, 1) { } }
			public class MID_0037 : MID { public MID_0037() : base(20, 37, 1) { } }
		}
		//public class c1 { }

		public abstract class MID {
			protected MID(int headerLen, int midNo, int revisionNumber) { }

			public HeaderData HeaderData { get; set; }
			public virtual List<DataField> RegisteredDataFields { get; internal set; }
			protected virtual void registerDatafields() { }
			public virtual MID processPackage(string line) { return this; }

			internal virtual string buildPackage() { return string.Empty; }
		}
		public class MIDIdentifier { }
		public class DataField {
			public DataField(int v1, int v2, int v3) { }

			public object Value { get; internal set; }
		}

	}
}
public class HeaderData {
	public int Mid { get; set; }
	public int NoAckFlag { get; internal set; }
	public int StationID { get; internal set; }
	public int SpindleID { get; internal set; }
}
#endif
#endif