using System.Reflection;
using OpenProtocolInterpreter.MIDs;

namespace NSAtlasCopcoBreech {
	class MyMid_152 : MID {

		#region ctor
		public MyMid_152() : base(20, 152, 1) {
			Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod());
		}
		#endregion ctor

		#region MID implementation

		public override MID processPackage(string package) {
			MID ret = base.processPackage(package);
			string tmp;

			// have 4 fields, in 128 chars

			tmp = this.buildPackage();
			Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod());
			return ret;
		}
		protected override void registerDatafields() {
			base.RegisteredDataFields.AddRange(
				new DataField[] {
					new DataField(1,18,32),
					new DataField(2,50,32),
					new DataField(3,82,32),
					new DataField(4,114,32)
				});
		}
		#endregion MID implementation
	}
}