using ModbusMqttPublisher.Server.Contracts;
using ModbusMqttPublisher.Server.Domain;
using ModbusMqttPublisher.Server.Services.Modbus.Handlers;

namespace ModbusMqttPublisher.Server.Services.Modbus
{
    public class ModbusClient : IModbusClient
    {
        ModbusRtuProtocol modbusMaster;
        IModbusSerialPort serialPort;
        ILogger<ModbusClient> logger;

        public ModbusClient(
            ReadPort settings,
            ILogger<ModbusClient> logger,
            IModbusSerialPortFactory modbusSerialPortFactory)
        {
            this.logger = logger;
            serialPort = modbusSerialPortFactory.Create(settings);
            modbusMaster = new ModbusRtuProtocol(serialPort);
        }

        public void Dispose()
        {
            serialPort.Dispose();
        }

        public Task CheckConnection(TimeSpan reconnectTimeout, CancellationToken cancellationToken)
            => serialPort.CheckConnection(reconnectTimeout, cancellationToken);

        private void InitRequest(string msg, ModbusRequest request)
        {
            logger.LogTrace($"{msg}. Адрес {request.SlaveAddress}, Тип Рег: {request.RegisterType}, Номер рег: {request.StartRegister}, Кол-во: {request.RegisterCount}");

            modbusMaster.RegtryCount = request.RetryCount;
            serialPort.ReadTimeout = (int)request.ReadTimeout.TotalMilliseconds;
            serialPort.WriteTimeout = (int)request.WriteTimeout.TotalMilliseconds;
        }

        private Exception CreteExeption(Exception inner, string message, ModbusRequest request)
        {
            return new Exception($"{message}. Адрес {request.SlaveAddress}, Тип Рег: {request.RegisterType}, Номер рег: {request.StartRegister}, Кол-во: {request.RegisterCount}", inner);
        }

        public Task<bool[]> ReadBitRegistersAsync(ModbusRequest request)
        {
            InitRequest("Чтение из modbus", request);

            try
            {
                switch (request.RegisterType)
                {
                    case RegisterType.Coil:
                        return Task.FromResult(modbusMaster.PerformRequest(new ReadCoilsHandler(requestSlaveAddress: request.SlaveAddress, requestStartRegister: request.StartRegister, requestRegisterCount: request.RegisterCount)));
                    case RegisterType.DiscreteInput:
                        return Task.FromResult(modbusMaster.PerformRequest(new ReadDescreteInputsHandler(requestSlaveAddress: request.SlaveAddress, requestStartRegister: request.StartRegister, requestRegisterCount: request.RegisterCount)));
                    default:
                        throw new NotImplementedException();
                }
            }
            catch (Exception ex)
            {
                throw CreteExeption(ex, "Ошибка чтения из Modbus", request);
            }
        }

        public Task<ushort[]> ReadShortRegistersAsync(ModbusRequest request)
        {
            InitRequest("Чтение из modbus", request);

            try
            {
                switch (request.RegisterType)
                {
                    case RegisterType.HoldingRegister:
                        return Task.FromResult(modbusMaster.PerformRequest(new ReadHoldingRegistersHandler(requestSlaveAddress: request.SlaveAddress, requestStartRegister: request.StartRegister, requestRegisterCount: request.RegisterCount)));
                    case RegisterType.InputRegister:
                        return Task.FromResult(modbusMaster.PerformRequest(new ReadInputRegistersHandler(requestSlaveAddress: request.SlaveAddress, requestStartRegister: request.StartRegister, requestRegisterCount: request.RegisterCount)));
                    default:
                        throw new NotImplementedException();
                }
            }
            catch (Exception ex)
            {
                throw CreteExeption(ex, "Ошибка чтения из Modbus", request);
            }
        }

        public Task WriteBitRegistersAsync(ModbusRequest request, bool[] data)
        {
            InitRequest("Запись в modbus", request);

            try
            {

                if (request.RegisterType != RegisterType.Coil)
                    throw new Exception($"Нельзя писать в регистр {request.RegisterType}");

                if (request.RegisterCount != data.Length)
                    throw new Exception($"Количество переданных данных не соответсвует запрошенному количесву регистров для записи");

                if (request.RegisterCount == 1)
                {
                    modbusMaster.PerformRequest(new WriteSingleCoilHandler(requestSlaveAddress: request.SlaveAddress, requestRegisterAddress: request.StartRegister, requestValue: data[0]));
                }
                else
                {
                    modbusMaster.PerformRequest(new WriteMultipleCoilsHandler(requestSlaveAddress: request.SlaveAddress, requestStartRegister: request.StartRegister, requestValues: data));
                }
            }
            catch (Exception ex)
            {
                throw CreteExeption(ex, "Ошибка записи в Modbus", request);
            }

            return Task.CompletedTask;
        }

        public Task WriteShortRegistersAsync(ModbusRequest request, ushort[] data)
        {
            InitRequest("Запись в modbus", request);

            try
            {
                if (request.RegisterType != RegisterType.HoldingRegister)
                    throw new Exception($"Нельзя писать в регистр {request.RegisterType}");

                if (request.RegisterCount != data.Length)
                    throw new Exception($"Количество переданных данных не соответсвует запрошенному количесву регистров для записи");

                if (request.RegisterCount == 1)
                {
                    modbusMaster.PerformRequest(new WriteSingleRegisterHandler(requestSlaveAddress: request.SlaveAddress, requestRegisterAddress: request.StartRegister, requestValue: data[0]));
                }
                else
                {
                    modbusMaster.PerformRequest(new WriteMultipleRegistersHandler(requestSlaveAddress: request.SlaveAddress, requestStartRegister: request.StartRegister, requestValues: data));
                }
            }
            catch (Exception ex)
            {
                throw CreteExeption(ex, "Ошибка записи в Modbus", request);
            }

            return Task.CompletedTask;
        }
    }
}
