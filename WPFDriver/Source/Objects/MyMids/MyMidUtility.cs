using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace NSAtlasCopcoBreech.MyMid {

	public class MyMidUtility {
		public static void examineMid(string line) {
			//int length,blah,midNo=-1,rev;
			//bool noAckFlag,bblah;
			//string tmp,stationID;
			Mid mid;
			string tmp;
			int blah,midNo;


			Utility.logger.log(ColtLogLevel.Debug, MethodBase.GetCurrentMethod(), "["+line+"]");

			if (int.TryParse(tmp=line.Substring(4, 4), out blah))
				midNo=blah;
			else
				midNo=-1;
			//if (int.TryParse(tmp=line.Substring(0, 4), out blah))
			//	length=blah;
			//if (int.TryParse(tmp=line.Substring(8, 3), out blah))
			//	rev=blah;
			//if (bool.TryParse(tmp=line.Substring(11, 1), out bblah))
			//	noAckFlag=bblah;
			if (midNo>0) {
				mid=null;
				switch (midNo) {
					case 2: mid=new Mid0002(); break;
					case 11: mid=new Mid0011(); break;
				}
				if (mid!=null) {
					mid.buildPackage(line);
					mid.display();
				} else
					Utility.logger.log(ColtLogLevel.Warning, MethodBase.GetCurrentMethod(), "unhandled mid="+midNo);
				//readMid(line);
			}

			//;
			//noAckFlag=bblah;
			//stationID=line.Substring(12, 2).Trim();
			//string spindleID = line.Substring(14, 2).Trim();
			//string spare = line.Substring(16, 4).Trim();
			//System.Diagnostics.Trace.WriteLine("here");
		}

		//  static void readMid(string line) {
		//	throw new NotImplementedException();
		//}
		//override re


	}


}