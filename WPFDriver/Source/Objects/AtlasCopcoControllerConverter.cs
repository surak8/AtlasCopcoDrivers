using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace NSAtlasCopcoBreech {
	public class AtlasCopcoControllerConverter : TypeConverter {
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
			bool ret = base.CanConvertFrom(context, sourceType);
			Utility.logger.log(MethodBase.GetCurrentMethod(), "From=" + sourceType.FullName + " returning " + ret);
			return ret;
		}
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
			object ret = null;

			try {
				ret = base.ConvertFrom(context, culture, value);
			} catch (NotSupportedException nse) {
				Utility.logger.log(MethodBase.GetCurrentMethod(), nse);

				foreach (string str in Enum.GetNames(typeof(AtlasCopcoController))) {
					Utility.logger.log("blah");

				}
				//var avar = Enum.GetName(typeof(AtlasCopcoController), value);
				Utility.logger.log("blah");
			} catch (Exception ex) {
				Utility.logger.log(MethodBase.GetCurrentMethod(), ex);
			}
			Utility.logger.log(MethodBase.GetCurrentMethod());
			return ret;
		}
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
			bool ret = base.CanConvertTo(context, destinationType);
			Utility.logger.log(MethodBase.GetCurrentMethod(), "To=" + destinationType.FullName + " returning " + ret);
			return ret;
		}
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
			Utility.logger.log(MethodBase.GetCurrentMethod());
			return base.ConvertTo(context, culture, value, destinationType);
		}
	}
}