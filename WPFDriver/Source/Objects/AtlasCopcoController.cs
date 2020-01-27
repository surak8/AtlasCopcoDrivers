using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace NSAtlasCopcoBreech {
	[TypeConverter(typeof(AtlasCopcoControllerConverter))]
	public class AtlasCopcoController {
		//private readonly string ipAddress;

		internal AtlasCopcoController() { }
		internal AtlasCopcoController(string ipaddr, int nport, string desc) : this() {
			//AtlasCopcoController
			ipAddress = ipaddr;
			portNumber = nport;
			controllerDescription = desc;
		}

		public string ipAddress { get; private set; }
		public int portNumber { get; private set; }
		public string controllerDescription { get; private set; }
	}

}