using System;
using System.Reflection;

namespace NSAtlasCopcoBreech.MyMid {
	abstract class Mid {

		public Header header { get; set; }
		public DataField[] _registeredFields;

		public DataField[] registeredFields { get { return _registeredFields; } protected set { _registeredFields=value; } }

		public Mid() {
			header=new Header();
			registerFields();
		}

		protected virtual void registerFields() {
			_registeredFields=new DataField[0];
		}

		internal virtual void buildPackage(string line) {
			//int pos,len;

			header.buildPackage(line);
			//pos=line.IndexOf('\0');
			//len=line.Length;

			//switch (header.revision) {
			//	case 5: Utility.logger.log(MethodBase.GetCurrentMethod(), "Mid="+header.mid+", Length="+header.length+", Revision="+header.revision); break;
			//	default: Utility.logger.log(MethodBase.GetCurrentMethod(), "unhandled revision "+header.revision+" for Mid="+header.mid+", Length="+header.length); break;
			//}

			//Utility.logger.log(MethodBase.GetCurrentMethod());

		}

		internal bool readParameter(string line, DataField df, out int fldNo, out string data) {
			return readParameter(line, df.startIndex, df.length, out fldNo, out data);
		}

		bool readParameter(string line, int startIndex, int length, out int fldNo, out string data) {
			string tmp;

			fldNo=-1;
			data=null;
			if (int.TryParse(tmp=line.Substring(startIndex, 2), out fldNo)) {
				data=line.Substring(startIndex+2, length).Trim();
				return true;
			}
			return false;
		}

		internal virtual void display() {
			Utility.logger.log(MethodBase.GetCurrentMethod());
		}
	}
}