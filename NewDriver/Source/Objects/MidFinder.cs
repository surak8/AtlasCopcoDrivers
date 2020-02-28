using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace NSNewDriver {
	static class MidFinder {
		static readonly BindingFlags bfCommon=BindingFlags.Instance|BindingFlags.Public;
		public static readonly BindingFlags bfCreate=bfCommon|BindingFlags.CreateInstance;
		public static readonly object[] nullArgs=new object[0];
		static readonly BindingFlags bfProp=bfCommon|BindingFlags.GetProperty;
		static readonly BindingFlags bfCallMethod=bfCommon | BindingFlags.InvokeMethod;

		static string extractionMethodName {
			get {
#if KENNY
				return "buildPackage";
#else
				return "Pack";
#endif
			}
		}
		static string setMidMethodName {
			get {
#if KENNY
				return "processPackage";
#else
				return "Parse";
#endif
			}
		}
		internal static Type midType { get; private set; }

		internal static IDictionary<int, Type> midTypeMap { get; private set; }

		// "HeaderData", "Mid"  for RB-protocol
		//internal static IDictionary<int, Type> createMap(Type[] types, string midClassName, out Type midType) {
		internal static bool createMap(Type[] types, string midClassName, string headerPropertyName, string midNoPropertyName) {
			//bool bret=false;
			//IDictionary<int, Type> ret =new  Dictionary<int, Type>();
			int midNo;

			midTypeMap = new Dictionary<int, Type>();
			if ((midType = findMidType(types, midClassName)) != null) {
				//ret.Clear();
				foreach (Type atype in types) {
					if (atype.BaseType != null && atype.BaseType.Equals(midType)) {
						if (string.Compare(midClassName = atype.Name, midType.Name, true) == 0) {
							// Kenny's version
							Trace.WriteLine("Found mid-class: " + midClassName);
						} else {
							midNo = findMidNumber(atype, headerPropertyName, midNoPropertyName);
							if (midNo > 0) {
								if (!midTypeMap.ContainsKey(midNo))
									midTypeMap.Add(midNo, atype);
								else
									Trace.WriteLine("mid=" + midNo + " already exists!");
							}
						}
					}
				}
			}
			return midTypeMap.Count > 0;
			//return bret;
			//return ret;
		}

		static int findMidNumber(Type midType, string headerPropertyName, string midPropertyName) {
			int midNo=-1;
			object amid,header,objMidNo;

			amid = midType.InvokeMember(null, bfCreate, null, null, nullArgs);
			header = midType.InvokeMember(headerPropertyName, bfProp, null, amid, nullArgs);
			objMidNo = header.GetType().InvokeMember(midPropertyName, bfProp, null, header, nullArgs);
			if (objMidNo != null)
				midNo = (int) objMidNo;
			return midNo;
		}

		internal static Type findMidType(Type[] typeVector, string partialClassName) {
			Type ret=null;
			string upperType,upperClass;

			upperClass = partialClassName.ToUpper();
			foreach (Type atype in typeVector) {
				if (string.Compare(upperType = atype.Name.ToUpper(), upperClass, true) == 0) {
					ret = atype;
					break;
				}
			}
			return ret;
		}

		internal static object createNewMid(int v) {
			if (midTypeMap.ContainsKey(v))
				return midTypeMap[v].InvokeMember(null, MidFinder.bfCreate, null, null, MidFinder.nullArgs);
			return null;
		}

		internal static int findMidNumberFromPackage(string package) {
			int plen, ntmp;

			if (!string.IsNullOrEmpty(package) && (plen = package.Length) > 8) {
				if (int.TryParse(package.Substring(4, 4), out ntmp))
					return ntmp;
			}
			return -1;
		}
		internal static object createMidInstance(string package) {
			int midNo,plen,midRevision;
			string strMid;
			object midObj;
			Type midType;

			if (string.IsNullOrEmpty(package))
				throw new ArgumentNullException("package", "package is null");
			plen = package.Length;
			if (!int.TryParse(strMid = package.Substring(4, 4), out midNo))
				throw new InvalidOperationException("problem with MID '" + strMid + "'!");
			if (!midTypeMap.ContainsKey(midNo))
				throw new InvalidOperationException("MID " + midNo + " not in map!");
			midType = midTypeMap[midNo];
			try {
#if KENNY
				// need to create MID by revision.
				midRevision = extractMidRevision(package);
				if (midRevision < 0)
					throw new InvalidOperationException("Invalid Revision!");
				//midObj = midType.InvokeMember(null, bfCreate, null, null, new object[midRevision]);
				if (midRevision > 1)
					midObj = midType.InvokeMember(null, bfCreate, null, null, new object[] { midRevision });
				else
					midObj = midType.InvokeMember(null, bfCreate, null, null, nullArgs);
				//midObj = midType.InvokeMember(null, bfCreate|BindingFlags.ExactBinding , null, null, new object[midRevision]);

#else
				midObj = midType.InvokeMember(null, bfCreate, null, null, nullArgs);
#endif
			} catch (Exception ex) {
				throw new InvalidOperationException("Error creating MID " + midType.FullName + ".", ex);
			}
			try {
				//midType.InvokeMember("Parse", bfCommon | BindingFlags.InvokeMethod, null, midObj, new object[] { package });
				midType.InvokeMember(setMidMethodName, bfCommon | BindingFlags.InvokeMethod, null, midObj, new object[] { package });
			} catch (Exception ex) {
				throw new InvalidOperationException("Error creating MID " + midType.FullName + ".", ex);
			}
			return midObj;
		}

		static int extractMidRevision(string package) {
			const int REVISION_LOCATION=8;
			const int REVISION_LENGTH=3;
			string revision;
			int plen,ret;

			if (!string.IsNullOrEmpty(package) && (plen = package.Length) > (REVISION_LOCATION + REVISION_LENGTH)) {
				if (string.IsNullOrEmpty(revision = package.Substring(REVISION_LOCATION, REVISION_LENGTH).Trim()))
					return 1;
				if (int.TryParse(revision, out ret))
					if (ret >= 0 && ret <= 5)
						return ret;
			}
			return -1;
		}

		internal static string extractMidContent(object mid) {
			object pkgObj=null;
			Type midType;

			if (mid == null)
				throw new ArgumentNullException("mid", "mid is null!");
			midType = mid.GetType();
			try {
				pkgObj = midType.InvokeMember(extractionMethodName, bfCommon | BindingFlags.InvokeMethod, null, mid, nullArgs);
			} catch (Exception ex) {
				throw new InvalidOperationException("Error extracting MID " + midType.FullName + "!", ex);
			}
			if (pkgObj != null)
				return pkgObj.ToString() + "\0";
			return null;
		}

		internal static int extractMidNumber(object m) {
			object header,midNoObj;
			Type midType;

			if (m != null) {
				midType = m.GetType();
				try {
					header = midType.InvokeMember("HeaderData", bfProp, null, m, nullArgs);
				} catch (Exception ex) {
					throw new InvalidOperationException("Failed to retrieve header for MID " + midType.FullName + "!", ex);
				}
				try {
					midNoObj = header.GetType().InvokeMember("Mid", bfProp, null, header, nullArgs);
				} catch (Exception ex) {
					throw new InvalidOperationException("Failed to retrieve Mid-number for " + midType.FullName + "!", ex);
				}
				return Convert.ToInt32(midNoObj);
			}
			return -1;
		}

		internal static string fixupPackage(string package) {
			if (!string.IsNullOrEmpty(package))
				if (package.EndsWith("\0"))
					return package.Substring(0, package.Length - 1);
			return package;
		}

	}
}