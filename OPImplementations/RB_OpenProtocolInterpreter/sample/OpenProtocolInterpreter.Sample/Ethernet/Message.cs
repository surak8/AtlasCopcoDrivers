using System.Linq;
using System.Net.Sockets;

namespace OpenProtocolInterpreter.Sample.Ethernet {
	/// <summary>
	/// ETHERNET CLASSES TAKEN FROM SimpleTCP Project
	/// <see cref="https://github.com/BrandonPotter/SimpleTCP" />
	/// </summary>
	public class Message {
		#region fields
		TcpClient _tcpClient;
		System.Text.Encoding _encoder = null;
		readonly byte _writeLineDelimiter;
		readonly bool _autoTrim = false;
		#endregion
		#region ctors

		internal Message(byte[] data, System.Net.Sockets.TcpClient tcpClient, System.Text.Encoding stringEncoder, byte lineDelimiter) {
			Data = data;
			_tcpClient = tcpClient;
			_encoder = stringEncoder;
			_writeLineDelimiter = lineDelimiter;
		}

		internal Message(byte[] data, System.Net.Sockets.TcpClient tcpClient, System.Text.Encoding stringEncoder, byte lineDelimiter, bool autoTrim) {
			Data = data;
			_tcpClient = tcpClient;
			_encoder = stringEncoder;
			_writeLineDelimiter = lineDelimiter;
			_autoTrim = autoTrim;
		}
		#endregion

		#region properties
		public byte[] Data { get; set; }
		public TcpClient TcpClient { get { return _tcpClient; } }
		#endregion

		#region methods
		public string MessageString { get { if (_autoTrim) return _encoder.GetString(Data).Trim(); return _encoder.GetString(Data); } }

		public void Reply(byte[] data) { _tcpClient.GetStream().Write(data, 0, data.Length); }

		public void Reply(string data) { if (!string.IsNullOrEmpty(data)) Reply(_encoder.GetBytes(data)); return; }

		public void ReplyLine(string data) {
			if (!string.IsNullOrEmpty(data))
				if (data.LastOrDefault() != _writeLineDelimiter) {
					Reply(data + _encoder.GetString(new byte[] { _writeLineDelimiter }));
				} else {
					Reply(data);
				}
		}
		#endregion

	}
}