namespace NSAtlasCopcoShared {

	public interface ILogViewer { }
	public class LogViewer<T> : ILogViewer where T : new() {
		//private string kEY;
		//public static Dictionary<>

		public string lastFileLoaded { get; set; }
		public string registryKey { get; private set; }
		public LogViewer(string key) {
			this.registryKey=key;
		}
	}

	//public class KDMid:MID
}