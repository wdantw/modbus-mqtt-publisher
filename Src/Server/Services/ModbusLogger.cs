using NModbus;

namespace ModbusMqttPublisher.Server.Services
{
    public class ModbusLogger : IModbusLogger
    {
        private readonly ILogger<ModbusLogger> logger;

        public ModbusLogger(ILogger<ModbusLogger> logger)
        {
            this.logger = logger;
        }

        private static LogLevel ConverLogLevel(LoggingLevel level)
        {
            switch (level)
            {
                case LoggingLevel.Trace: return LogLevel.Trace;
                case LoggingLevel.Debug: return LogLevel.Debug;
                case LoggingLevel.Information: return LogLevel.Information;
                case LoggingLevel.Warning: return LogLevel.Warning;
                case LoggingLevel.Error: return LogLevel.Error;
                case LoggingLevel.Critical: return LogLevel.Critical;
                default: return LogLevel.Information;
            }
        }

        public void Log(LoggingLevel level, string message)
            => logger.Log(ConverLogLevel(level), message);

        public bool ShouldLog(LoggingLevel level)
            => logger.IsEnabled(ConverLogLevel(level));
    }
}
