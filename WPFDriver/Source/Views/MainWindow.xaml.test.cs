using System;
using System.Reflection;

namespace NSAtlasCopcoBreech {

	public partial class MainWindow  {
		void blah(Assembly asm) {
			foreach(Type atype in asm.GetTypes()) {
				if (atype.IsPublic && atype.IsClass) {
					Utility.logger.log(MethodBase.GetCurrentMethod(), atype.Name);
				}
			}
		}
	}
}