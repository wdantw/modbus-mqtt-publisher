using NModbus.IO;
using System.Diagnostics;
using System.IO.Ports;

namespace ModbusMqttPublisher.Server.Services.Modbus
{
	public class ProfiledSerialPortAdapter : IStreamResource
	{
		private const string NewLine = "\r\n";
		private SerialPort _serialPort;
		private Profiler _profiler;

		public ProfiledSerialPortAdapter(SerialPort serialPort, Profiler profiler)
		{
			Debug.Assert(serialPort != null, "Argument serialPort cannot be null.");

			_serialPort = serialPort;
			_serialPort.NewLine = NewLine;
			_profiler = profiler;
		}

		public int InfiniteTimeout
		{
			get { return SerialPort.InfiniteTimeout; }
		}

		public int ReadTimeout
		{
			get { return _serialPort.ReadTimeout; }
			set { _serialPort.ReadTimeout = value; }
		}

		public int WriteTimeout
		{
			get { return _serialPort.WriteTimeout; }
			set { _serialPort.WriteTimeout = value; }
		}

		public void DiscardInBuffer()
		{
			_serialPort.DiscardInBuffer();
		}

		public int Read(byte[] buffer, int offset, int count)
		{
			return _profiler.WrapMethod("$serial_read_time", () => _serialPort.Read(buffer, offset, count));
		}

		public void Write(byte[] buffer, int offset, int count)
		{
			_profiler.WrapMethod("$serial_write_time", () => _serialPort.Write(buffer, offset, count));
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				_serialPort.Dispose();
			}
		}
	}
}
