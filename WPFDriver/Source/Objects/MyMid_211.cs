using System;
using System.Reflection;
using OpenProtocolInterpreter.MIDs;

namespace NSAtlasCopcoBreech {
	class MyMid_211 : MID {

		#region ctor
		public MyMid_211() : base(20, 211, 1) {
			Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod());
		}

		#endregion

		#region properties
		public string myField { get; set; }
		#endregion

		#region MID implemplementation
		//public override string buildPackage() {
		//	// add content
		//	//string ret = base.buildPackage();
		//	string pkg = string.Empty;

		//	ret += myField;
		//	Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod());
		//	return ret;
		//}

		public override MID processPackage(string package) {
			MID ret = base.processPackage(package);
			string tmp;

			ret.HeaderData.NoAckFlag = 1;
			ret.HeaderData.StationID = 88;
			ret.HeaderData.SpindleID = 99;
			//ret.HeaderData.u
			if (this.RegisteredDataFields.Count > 0)
				this.myField = base.RegisteredDataFields[0].Value.ToString();
			tmp = this.buildPackage();

			Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod(),
				Environment.NewLine +
				package + Environment.NewLine +
				tmp
				);
			return ret;
		}

		//protected override string buildHeader() {
		//	return base.buildHeader();
		//}
		//public override string buildPackage() {
		//	string package = this.buildHeader();

		//	if (this.RegisteredDataFields.Count == 0)
		//		return package;

		//	for (int i = 1; i < this.RegisteredDataFields.Count + 1; i++)
		//		package += i.ToString().PadLeft(2, '0') + RegisteredDataFields[i - 1].getPaddedRightValue();

		//	return package; ;	
		//}

		//public override bool Equals(object obj) {
		//	Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod());
		//	return base.Equals(obj);
		//}

		//public override int GetHashCode() {
		//	Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod());
		//	return base.GetHashCode();
		//}

		//public override string ToString() {
		//	Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod());
		//	return base.ToString();
		//}

		//protected override string buildHeader() {
		//	Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod());
		//	return base.buildHeader();
		//}

		//protected override void loadRevisionsFields() {
		//	Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod());
		//	base.loadRevisionsFields();
		//}

		//protected override Header processHeader(string package) {
		//	Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod());
		//	return base.processHeader(package);
		//}

		protected override void registerDatafields() {
			//Utility.logger.log(ColtLogLevel.Info, MethodBase.GetCurrentMethod());
			//base.registerDatafields();
			base.RegisteredDataFields.Add(new DataField((int) DataFields.SOME_FIELD, 20, 6));
			// 00280211        000000000000
		}

		#endregion
	}
	public enum DataFields {
		SOME_FIELD
	}
}