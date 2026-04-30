using ModbusMqttPublisher.Server.Common;
using ModbusMqttPublisher.Server.Domain;
using System.Diagnostics.Metrics;
using System.IO.Ports;

namespace ModbusMqttPublisher.Server.Services.Modbus
{
    public class ModbusSerialPort : IModbusSerialPort
    {
        private const string NewLine = "\r\n";
        
        private readonly SerialPort _serialPort;
        private readonly ILogger<ModbusSerialPort> _logger;

        private readonly Counter<int> _readCallCounter;
        private readonly Counter<int> _readBytesCounter;
        private readonly DiagnosticTimeCounter _readCallDurationCounter;
        private readonly Counter<int> _writeCallCounter;
        private readonly Counter<int> _writeBytesCounter;
        private readonly DiagnosticTimeCounter _writeCallDurationCounter;

        bool _isFirstConnect = true;

        public ModbusSerialPort(
            ReadPort settings,
            IMeterFactory meterFactory,
            ILogger<ModbusSerialPort> logger)
        {
            _logger = logger;

            _serialPort = new SerialPort(
                portName: settings.SerialName,
                baudRate: settings.BaudRate,
                parity: settings.Parity,
                dataBits: settings.DataBits,
                stopBits: settings.StopBits);

            _serialPort.NewLine = NewLine;

            var meter = meterFactory.Create(GetType().FullName!, tags: new Dictionary<string, object?> { { "Serial", settings.SerialName } });
            _readCallCounter = meter.CreateCounter<int>("serial.read.calls", "calls", "Количество вызовов метода чтения из последовательного порта");
            _readBytesCounter = meter.CreateCounter<int>("serial.read.bytes", "byte", "Количество байт, считанных из последовательного порта");
            _readCallDurationCounter = new DiagnosticTimeCounter(meter.CreateCounter<double>("serial.read.duration", "ms", "Время, проведенное в методах чтения из последовательного порта"));
            _writeCallCounter = meter.CreateCounter<int>("serial.write.calls", "calls", "Количество вызовов метода записи в последовательынй порт");
            _writeBytesCounter = meter.CreateCounter<int>("serial.write.bytes", "byte", "Количество байт, записанных в последовательный порт");
            _writeCallDurationCounter = new DiagnosticTimeCounter(meter.CreateCounter<double>("serial.write.duration", "ms", "Время, проведенное в методах записи в последовательный порт"));
        }

        public int InfiniteTimeout => SerialPort.InfiniteTimeout;

        public int ReadTimeout
        {
            get => _serialPort.ReadTimeout;
            set => _serialPort.ReadTimeout = value;
        }

        public int WriteTimeout
        {
            get => _serialPort.WriteTimeout;
            set => _serialPort.WriteTimeout = value;
        }

        public void DiscardInBuffer()
            => _serialPort.DiscardInBuffer();

        public void Read(byte[] buffer, int offset, int count)
        {
            _readCallCounter.Add(1);
            using var _ = _readCallDurationCounter.GetStartHolder();

            var readedCount = 0;
            while (readedCount < count)
                readedCount += _serialPort.Read(buffer, offset + readedCount, count - readedCount);

            _readBytesCounter.Add(count);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            _writeCallCounter.Add(1);
            using var _ = _writeCallDurationCounter.GetStartHolder();
            _serialPort.Write(buffer, offset, count);
            _writeBytesCounter.Add(count);
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

        public async Task CheckConnection(TimeSpan reconnectTimeout, CancellationToken cancellationToken)
        {
            if (_serialPort.IsOpen)
                return;

            while (true)
            {
                if (_isFirstConnect)
                {
                    _logger.LogDebug("Соединение с портом {portName}", _serialPort.PortName);
                }
                else
                {
                    _logger.LogWarning("Порт {portName} закрыт. Попытка соединения.", _serialPort.PortName);
                }

                _isFirstConnect = false;

                try
                {
                    _serialPort.Open();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка открытия порта {portName}", _serialPort.PortName);
                }

                if (_serialPort.IsOpen)
                    return;

                _logger.LogDebug("Следующая попытка открытия порта {portName} через {reconnectTimeout}", _serialPort.PortName, reconnectTimeout);

                await Task.Delay(reconnectTimeout, cancellationToken);
            }
        }
    }
}
