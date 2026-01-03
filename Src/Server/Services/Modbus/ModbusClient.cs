using ModbusMqttPublisher.Server.Common;
using ModbusMqttPublisher.Server.Contracts;
using ModbusMqttPublisher.Server.Domain;
using ModbusMqttPublisher.Server.Services.Modbus.Handlers;

namespace ModbusMqttPublisher.Server.Services.Modbus
{
    public class ModbusClient : IModbusClient
    {
        private readonly ModbusRtuProtocol _modbusMaster;
        private readonly IModbusSerialPort _serialPort;
        private readonly ILogger<ModbusClient> _logger;

        private readonly int _retryCount;
        private readonly TimeSpan _errorSleepTimeout;
        private readonly TimeSpan _minSleepTimeout;
        
        private TimeSpan _nextAllowedAccessTime;

        public ModbusClient(
            ReadPort settings,
            ILogger<ModbusClient> logger,
            IModbusSerialPortFactory modbusSerialPortFactory)
        {
            _logger = logger;
            _serialPort = modbusSerialPortFactory.Create(settings);
            _modbusMaster = new ModbusRtuProtocol(_serialPort);

            _serialPort.ReadTimeout = (int)settings.ReadTimeout.TotalMilliseconds;
            _serialPort.WriteTimeout = (int)settings.WriteTimeout.TotalMilliseconds;
            
            _retryCount = Math.Max(settings.RetryCount, 1);
            _errorSleepTimeout = settings.ErrorSleepTimeout;
            _minSleepTimeout = settings.MinSleepTimeout;

            _nextAllowedAccessTime = MonotonicTime.TimeSinceStart;
        }

        public void Dispose()
        {
            _serialPort.Dispose();
        }

        public async Task<TResult> PerformRequest<TResult>(IModbusRequestHandler<TResult> handler, CancellationToken cancellationToken)
        {
            var remainingAttempts = _retryCount;

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Отправка запроса \"{requestInformation}\"", handler.GetRequestInformation());
            }

            while (true)
            {
                remainingAttempts--;

                try
                {
                    var currTime = MonotonicTime.TimeSinceStart;

                    if (_nextAllowedAccessTime > currTime)
                        await Task.Delay(_nextAllowedAccessTime - currTime, cancellationToken);

                    await _serialPort.CheckConnection(_errorSleepTimeout, cancellationToken);

                    var result = _modbusMaster.PerformRequest(handler);

                    if (_logger.IsEnabled(LogLevel.Trace))
                    {
                        _logger.LogTrace("Обмен данными через Modbus:" + Environment.NewLine + _modbusMaster.GetLastRequestData());
                    }

                    _nextAllowedAccessTime = MonotonicTime.TimeSinceStart + _minSleepTimeout;

                    return result;
                }
                catch (Exception ex)
                {
                    _nextAllowedAccessTime = MonotonicTime.TimeSinceStart + _errorSleepTimeout;

                    if (remainingAttempts <= 0)
                        throw new Exception($"Ошибка выполнения modbus запроса: \"{handler.GetRequestInformation()}\".", ex);

                    if (_logger.IsEnabled(LogLevel.Warning))
                        _logger.LogWarning(ex, $"Ошибка выполнения modbus запроса \"{handler.GetRequestInformation()}\". Осталось попыток: {remainingAttempts}");
                }
            }
        }

        public async Task<bool[]> ReadBitRegistersAsync(ModbusRequest request, CancellationToken cancellationToken)
        {
            switch (request.RegisterType)
            {
                case RegisterType.Coil:
                    return await PerformRequest(new ReadCoilsHandler(requestSlaveAddress: request.SlaveAddress, requestStartRegister: request.StartRegister, requestRegisterCount: request.RegisterCount), cancellationToken);
                case RegisterType.DiscreteInput:
                    return await PerformRequest(new ReadDescreteInputsHandler(requestSlaveAddress: request.SlaveAddress, requestStartRegister: request.StartRegister, requestRegisterCount: request.RegisterCount), cancellationToken);
                default:
                    throw new NotImplementedException();
            }
        }

        public async Task<ushort[]> ReadShortRegistersAsync(ModbusRequest request, CancellationToken cancellationToken)
        {
            switch (request.RegisterType)
            {
                case RegisterType.HoldingRegister:
                    return await PerformRequest(new ReadHoldingRegistersHandler(requestSlaveAddress: request.SlaveAddress, requestStartRegister: request.StartRegister, requestRegisterCount: request.RegisterCount), cancellationToken);
                case RegisterType.InputRegister:
                    return await PerformRequest(new ReadInputRegistersHandler(requestSlaveAddress: request.SlaveAddress, requestStartRegister: request.StartRegister, requestRegisterCount: request.RegisterCount), cancellationToken);
                default:
                    throw new NotImplementedException();
            }
        }

        public async Task WriteBitRegistersAsync(ModbusRequest request, bool[] data, CancellationToken cancellationToken)
        {
            if (request.RegisterType != RegisterType.Coil)
                throw new Exception($"Нельзя писать в регистр {request.RegisterType}");

            if (request.RegisterCount != data.Length)
                throw new Exception($"Количество переданных данных не соответсвует запрошенному количесву регистров для записи");

            if (request.RegisterCount == 1)
            {
                await PerformRequest(new WriteSingleCoilHandler(requestSlaveAddress: request.SlaveAddress, requestRegisterAddress: request.StartRegister, requestValue: data[0]), cancellationToken);
            }
            else
            {
                await PerformRequest(new WriteMultipleCoilsHandler(requestSlaveAddress: request.SlaveAddress, requestStartRegister: request.StartRegister, requestValues: data), cancellationToken);
            }
        }

        public async Task WriteShortRegistersAsync(ModbusRequest request, ushort[] data, CancellationToken cancellationToken)
        {
            if (request.RegisterType != RegisterType.HoldingRegister)
                throw new Exception($"Нельзя писать в регистр {request.RegisterType}");

            if (request.RegisterCount != data.Length)
                throw new Exception($"Количество переданных данных не соответсвует запрошенному количесву регистров для записи");

            if (request.RegisterCount == 1)
            {
                await PerformRequest(new WriteSingleRegisterHandler(requestSlaveAddress: request.SlaveAddress, requestRegisterAddress: request.StartRegister, requestValue: data[0]), cancellationToken);
            }
            else
            {
                await PerformRequest(new WriteMultipleRegistersHandler(requestSlaveAddress: request.SlaveAddress, requestStartRegister: request.StartRegister, requestValues: data), cancellationToken);
            }
        }

        public async Task<WbEvents?> WbRequestEventsAsync(byte minSlaveAddress, byte acceptEventsSlaveAddress, byte acceptEventsFlag, CancellationToken cancellationToken)
        {
            return await PerformRequest(new WbRequestEventsHandler(minSlaveAddress, acceptEventsSlaveAddress, acceptEventsFlag), cancellationToken);
        }

        public async Task<WbEventConfig[]> WbConfigureEvents(byte slaveAddress, WbEventConfig[] configurations, CancellationToken cancellationToken)
        {
            return await PerformRequest(new WbConfigureEventsHandler(slaveAddress, configurations), cancellationToken);
        }
    }
}
