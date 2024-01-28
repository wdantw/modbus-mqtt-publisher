using MudbusMqttPublisher.Server.Common;
using MudbusMqttPublisher.Server.Contracts.Configs;
using MudbusMqttPublisher.Server.Contracts.Settings;
using System.Runtime.CompilerServices;

namespace MudbusMqttPublisher.Server.Services.Configuration
{
    public static class Mappers
    {
        

        private static T AssertNotNull<T>(this T? instance, [CallerArgumentExpression(nameof(instance))] string varName = "value")
            where T : class
        {
            if (instance == null)
                throw new ArgumentNullException(varName);

            return instance;
        }

        private static T AssertNotNull<T>(this T? instance, [CallerArgumentExpression(nameof(instance))] string varName = "value")
            where T : struct
        {
            if (!instance.HasValue)
                throw new ArgumentNullException(varName);

            return instance.Value;
        }


        public static PortSettings MapToSettings(this ModbusPortComplete port)
        {
            var baudRate = port.BaudRate ?? DefaultSettings.BaudRate;
            var serialName = port.SerialName.AssertNotNull();

            return new PortSettings(
                portName: serialName,
                baudRate: baudRate,
                dataBits: port.DataBits ?? DefaultSettings.DataBits,
                parity: port.Parity ?? DefaultSettings.Parity,
                stopBits: port.StopBits ?? DefaultSettings.StopBits,
                minSleepTimeout: port.MinSleepTimeout ?? DefaultSettings.MinSleepTimeout(baudRate),
                devices: port.Devices.Select(d => d.MapToSettings(port.Name, serialName, baudRate)).ToArray(),
                readTimeout: port.ReadTimeout ?? DefaultSettings.DefaultPortTimeout,
                writeTimeout: port.WriteTimeout ?? DefaultSettings.DefaultPortTimeout
				);
        }

        public static DeviceSettings MapToSettings(this ModbusDeviceComplete device, string? baseName, string baseSerialName, int baudRate)
        {
            var slaveAddress = device.SlaveAddress.AssertNotNull();
            var defaultDeviceName = MqttPath.TopicPathDelimeter + MqttPath.CombineTopicPath(baseName ?? baseSerialName, $"Dev{slaveAddress}");
            var devNmae = device.Name ?? defaultDeviceName;

            return new DeviceSettings(
                slaveAddress: slaveAddress,
                maxRegHole: device.MaxRegHole ?? DefaultSettings.MaxRegHole,
                maxBitHole: device.MaxBitHole ?? DefaultSettings.MaxBitHole,
                maxReadRegisters: device.MaxReadRegisters ?? DefaultSettings.MaxReadRegisters,
                maxReadBit: device.MaxReadBit ?? DefaultSettings.MaxReadBit,
                minSleepTimeout: device.MinSleepTimeout ?? DefaultSettings.MinSleepTimeout(baudRate),
                registers: device.Registers.Select(r => MapToSettings(r, MqttPath.CombineTopicPath(baseName, devNmae))).ToArray()
                );
        }

        public static RegisterSettings MapToSettings(this ModbusRegisterCompleted register, string baseName)
        {
            return new RegisterSettings(
                name: MqttPath.CombineTopicPath(baseName, register.Name.AssertNotNull()),
                number: register.Number.AssertNotNull(),
                regType: register.RegType.AssertNotNull().GetRegisterType(),
                regFormat: register.RegType.AssertNotNull().GetRegisterFormat(),
                readPeriod: register.ReadPeriod,
                length: register.Length,
                wbEvents: register.WbEvents ?? DefaultSettings.AllowWbEvents,
                scale: register.Scale,
                precision: register.Precision,
                decimalSeparator: register.DecimalSeparator,
                compareDiff: register.CompareDiff
				);
        }
    }
}
