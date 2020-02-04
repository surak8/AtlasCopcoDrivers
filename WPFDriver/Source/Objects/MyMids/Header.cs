namespace NSAtlasCopcoBreech.MyMid {
	class Header {

		public override string ToString() {
			return base.ToString()+": Mid="+mid+" Revision="+revision+" Length="+length;
		}
		public int length { get; private set; }
		public int mid { get; private set; }
		public int revision { get; set; }

		public bool noAckFlag { get; set; }
		public string stationID { get; set; }
		public string spindleID { get; set; }
		internal void buildPackage(string line) {
			string tmp;
			int blah;
			bool bblah;

			if (int.TryParse(tmp=line.Substring(0, 4), out blah))
				length=blah;
			if (int.TryParse(tmp=line.Substring(4, 4), out blah))
				mid=blah;
			if (int.TryParse(tmp=line.Substring(8, 3), out blah))
				revision=blah;
			if (bool.TryParse(tmp=line.Substring(11, 1), out bblah))
				noAckFlag=bblah;
			stationID=line.Substring(12, 2).Trim();
			spindleID = line.Substring(14, 2).Trim();
			string spare = line.Substring(16, 4).Trim();
		}
	}
}