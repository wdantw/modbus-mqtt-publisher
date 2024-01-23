using Microsoft.Win32;
using MudbusMqttPublisher.Server.Contracts.Configs;
using MudbusMqttPublisher.Server.Contracts.Settings;
using System.Data.SqlTypes;
using System.Runtime.CompilerServices;

namespace MudbusMqttPublisher.Server.Services.Configuration
{
    public static class Mappers
    {
        const string TopicPathDelimeter = "/";

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

        private static string CombineTopicPath(string? basePath, string tailPath)
        {
            if (tailPath.StartsWith(TopicPathDelimeter))
                return tailPath[1..];

            if (basePath == null)
                return tailPath;

            if (basePath.EndsWith(TopicPathDelimeter))
                basePath = basePath[..^1];

            if (basePath.StartsWith(TopicPathDelimeter))
                basePath = basePath[1..];

            return basePath + TopicPathDelimeter + tailPath;
        }

        public static PortSettings MapToSettings(this ModbusPortComplete port)
        {
            var boudRate = port.BaudRate ?? DefaultSettings.BoudRate;
            var serialName = port.SerialName.AssertNotNull();

            return new PortSettings(
                portName: serialName,
                baudRate: boudRate,
                dataBits: port.DataBits ?? DefaultSettings.DataBits,
                parity: port.Parity ?? DefaultSettings.Parity,
                stopBits: port.StopBits ?? DefaultSettings.StopBits,
                minSleepTimeout: port.MinSleepTimeout ?? DefaultSettings.MinSleepTimeout(boudRate),
                devices: port.Devices.Select(d => d.MapToSettings(port.Name, serialName)).ToArray()
                );
        }

        public static DeviceSettings MapToSettings(this ModbusDeviceComplete device, string? baseName, string baseSerialName)
        {
            var slaveAddress = device.SlaveAddress.AssertNotNull();
            var defaultDeviceName = TopicPathDelimeter + CombineTopicPath(baseName ?? baseSerialName, $"Dev{slaveAddress}");
            var devNmae = device.Name ?? defaultDeviceName;

            return new DeviceSettings(
                slaveAddress: slaveAddress,
                maxRegHole: device.MaxRegHole ?? DefaultSettings.MaxRegHole,
                maxBitHole: device.MaxBitHole ?? DefaultSettings.MaxBitHole,
                maxReadRegisters: device.MaxReadRegisters ?? DefaultSettings.MaxReadRegisters,
                maxReadBit: device.MaxReadBit ?? DefaultSettings.MaxReadBit,
                registers: device.Registers.Select(r => MapToSettings(r, CombineTopicPath(baseName, devNmae))).ToArray()
                );
        }

        public static RegisterSettings MapToSettings(this ModbusRegisterCompleted register, string baseName)
        {
            return new RegisterSettings(
                name: CombineTopicPath(baseName, register.Name.AssertNotNull()),
                number: register.Number.AssertNotNull(),
                regType: register.RegType.AssertNotNull().GetRegisterType(),
                regFormat: register.RegType.AssertNotNull().GetRegisterFormat(),
                readPeriod: register.ReadPeriod ?? TimeSpan.MaxValue,
                length: register.Length,
                wbEvents: register.WbEvents ?? DefaultSettings.AllowWbEvents,
                scale: register.Scale,
                precision: register.Precision
                );
        }
    }
}
