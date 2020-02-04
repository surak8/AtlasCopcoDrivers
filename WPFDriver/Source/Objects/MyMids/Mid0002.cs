using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NSAtlasCopcoBreech.MyMid {
	class Mid0002 : Mid {
		#region CTOR
		public Mid0002() : base() { }

		#endregion
		#region properties
		public string cellID { get; private set; }
		public string channelID { get; private set; }
		public string controllerName { get; private set; }
		public string opVersion { get; private set; }
		public string controllerSoftwareVersion { get; private set; }
		public string toolSoftwareVersion { get; private set; }
		public string supplierCode { get; private set; }
		public string rbuType { get; private set; }
		public string controllerSerialNumber { get; private set; }
		public string systemType { get; private set; }
		public string systemSubType { get; private set; }

		#endregion
		internal override void buildPackage(string line) {
			string data;
			int fldNo;

			base.buildPackage(line);

			foreach (DataField df in registeredFields) {
				if (base.readParameter(line, df, out fldNo, out data)) {
					//;
					//data=line.Substring(df.startIndex+2, df.length);
					//tmp=line.Substring(df.startIndex, 2);
					//if (int.TryParse(tmp, out fldNo)) {
					switch (fldNo) {
						case 1: this.cellID=data.Trim(); break;
						case 2: this.channelID=data.Trim(); break;
						case 3: this.controllerName=data.Trim(); break;
						case 4: this.supplierCode=data.Trim(); break;

						case 5: this.opVersion=data.Trim(); break;
						case 6: this.controllerSoftwareVersion=data.Trim(); break;
						case 7: this.toolSoftwareVersion=data.Trim(); break;

						case 8: this.rbuType=data.Trim(); break;
						case 9: this.controllerSerialNumber=data.Trim(); break;

						case 10: this.systemType=data.Trim(); break;
						case 11: this.systemSubType=data.Trim(); break;
						default: Trace.WriteLine("unhandled field# "+fldNo+"."); break;
					}
					//} else
					//	Trace.WriteLine("ACK!");
				} else
					Trace.WriteLine("ACK!");
			}
		}

		//private bool readParam(DataField df, out int fldNo, out string data) {
		//	throw new NotImplementedException();
		//}

		protected override void registerFields() {
			List<DataField> flds=new List<DataField>();

			base.registerFields();
			flds.AddRange(
				new DataField[] {
					new DataField(1,20,2),
					new DataField(2,26,2),
					new DataField(3,30,2),

					new DataField(4,57,3),

					new DataField(5,62,18),
					new DataField(6,83,18),
					new DataField(7,104,18),

					new DataField(8,125,22),
					new DataField(9,151,9),

					new DataField(10,163,3),
					new DataField(11,168,3),
			});
			base._registeredFields=flds.ToArray();
		}
	}
}