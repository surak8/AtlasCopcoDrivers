namespace NSAtlasCopcoBreech.MyMid {
	class Mid0011 : Mid {
		internal override void buildPackage(string line) {
			base.buildPackage(line);
			//if (base.readParameter())
			int numParms,paramWidth,pos,aset;
			numParms=System.Convert.ToInt32(line.Substring(20, 3));
			paramWidth=System.Convert.ToInt32(line.Substring(23, 3));
			System.Diagnostics.Trace.WriteLine("here");
for (int i = 0; i<numParms; i++) {
				pos=26+i*paramWidth;
				aset=System.Convert.ToInt32(line.Substring(pos, paramWidth));

				System.Diagnostics.Trace.WriteLine("here");

			}
		}

		protected override void registerFields() {
			base.registerFields();
		}
	}
}