namespace NSAtlasCopcoBreech.MyMid {
	class DataField {
		//private int v1;
		//private int v2;
		//private int v3;

		public DataField(int fldTag, int fldStart, int fldLen) {
			tag=fldTag;
			startIndex=fldStart;
			length=fldLen;
			this.value=null;
		}

		public int tag { get; private set; }
		public int startIndex { get; private set; }
		public int length { get; private set; }
		public object value { get; private set; }

		public override string ToString() {
			//return base.ToString()+": FldNo="+tag+" Index="+startIndex+" Length="+length+" value="+value;
			return GetType().Name+": FldNo="+tag+" Index="+startIndex+" Length="+length+" value="+value;
		}
	}
}