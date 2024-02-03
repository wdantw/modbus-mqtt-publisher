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

        public void Log(LoggingLevel level, string message)
        {
            switch (level)
            {
                case LoggingLevel.Trace:
                    logger.LogTrace(message);
                    break;
                case LoggingLevel.Debug:
                    logger.LogDebug(message);
                    break;
                case LoggingLevel.Information:
                    logger.LogInformation(message);
                    break;
                case LoggingLevel.Warning:
                    logger.LogWarning(message);
                    break;
                case LoggingLevel.Error:
                    logger.LogError(message);
                    break;
                case LoggingLevel.Critical:
                    logger.LogCritical(message);
                    break;
                default:
                    logger.LogInformation(message);
                    break;
            }

        }

        public bool ShouldLog(LoggingLevel level)
        {
            return true;
        }
    }
}
