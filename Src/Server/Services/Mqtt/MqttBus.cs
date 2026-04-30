using Microsoft.Extensions.Options;
using ModbusMqttPublisher.Server.Common;
using ModbusMqttPublisher.Server.Contracts.Configs;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;

namespace ModbusMqttPublisher.Server.Services.Mqtt
{
    public sealed class MqttBus : BackgroundService, IMqttBus
    {
        private readonly IMqttClientFactory _mqttClientFactory;
        private readonly IMqttConsumer _mqttConsumer;
        private readonly IOptions<MqttOptions> _options;
        private readonly ILogger<MqttBus> _logger;
        
        private readonly TaskCompletionSource _createClientTaskSource;
        private volatile IManagedMqttClient? _mqttClient;
        private volatile TaskCompletionSource? _shutdownTaskSource;
        private CancellationToken _stoppingToken = default;
        private volatile bool _stopping = false;
        private volatile bool _disposed = false;

        public MqttBus(IMqttClientFactory mqttClientFactory, IMqttConsumer mqttConsumer, IOptions<MqttOptions> options, ILogger<MqttBus> logger)
        {
            _mqttClientFactory = mqttClientFactory ?? throw new ArgumentNullException(nameof(mqttClientFactory));
            _mqttConsumer = mqttConsumer ?? throw new ArgumentNullException(nameof(mqttConsumer));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _createClientTaskSource = new TaskCompletionSource();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _stoppingToken = stoppingToken;
            stoppingToken.Register(() => _createClientTaskSource.TrySetCanceled());
            _mqttClient = await _mqttClientFactory.Create(stoppingToken).ConfigureAwait(false);
            _createClientTaskSource.TrySetResult();

            try
            {
                _mqttClient.ApplicationMessageReceivedAsync += MqttClientApplicationMessageReceivedAsync;
                _mqttClient.ApplicationMessageProcessedAsync += MqttClientApplicationMessageProcessedAsync;
                _mqttClient.ApplicationMessageSkippedAsync += MqttClientApplicationMessageSkippedAsync;

                var topicFilter = new MqttTopicFilterBuilder()
                    .WithTopic(MqttPath.CombineTopicPath(_options.Value.BaseTopicPath, MqttPath.WildcardMultyLevel))
                    .WithAtLeastOnceQoS()
                    .Build();

                await _mqttClient.SubscribeAsync(new[] { topicFilter }).WithCancellation(stoppingToken).ConfigureAwait(false);
                await stoppingToken.WhenCancelled().ConfigureAwait(false);

            }
            catch (OperationCanceledException)
            {
                // Остановка сервиса или вызван диспоз (или произошла отмена запуска)
                _stopping = true;
                _shutdownTaskSource = new TaskCompletionSource();
                if (_mqttClient.PendingApplicationMessagesCount > 0 && !_disposed)
                    await _shutdownTaskSource.Task.ConfigureAwait(false);
            }
            finally
            {
                _stopping = true;
                await _mqttClient.StopAsync().ConfigureAwait(false);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            if (_disposed) return;
            if (_mqttClient != null)
            {
                _mqttClient.Dispose();
                _disposed = true;
            }
        }

        private Task MqttClientApplicationMessageSkippedAsync(ApplicationMessageSkippedEventArgs args)
        {
            if (_shutdownTaskSource == null)
                return Task.CompletedTask;

            if (_mqttClient!.PendingApplicationMessagesCount == 0)
                _shutdownTaskSource.TrySetResult();

            return Task.CompletedTask;
        }

        private Task MqttClientApplicationMessageProcessedAsync(ApplicationMessageProcessedEventArgs args)
        {
            if (_shutdownTaskSource == null)
                return Task.CompletedTask;

            if (_mqttClient!.PendingApplicationMessagesCount == 0)
                _shutdownTaskSource.TrySetResult();

            return Task.CompletedTask;
        }

        private async Task MqttClientApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
        {
            var topicName = args.ApplicationMessage.Topic;
            var baseTopicName = _options.Value.BaseTopicPath;
            var relativeTopicName = MqttPath.GetRelativeTopicName(topicName, baseTopicName);

            if (relativeTopicName == null)
            {
                _logger.LogWarning("Пришло сообщение из топика {topicName} не соответсвующее настроенному базовому пути {baseTopicName}", topicName, baseTopicName);
                return;
            }

            if (_stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning("Обработка входящего MQTT сообщения из топика {topicName} отменена из за остановки сервиса", topicName);
                return;
            }

            _logger.LogDebug("Принято сообщение и MQTT топика {topicName}", topicName);
            await _mqttConsumer.Consume(relativeTopicName, args.ApplicationMessage.PayloadSegment, _stoppingToken).ConfigureAwait(false);
        }

        public async Task EnqueueMessage(string fullTopicName, ArraySegment<byte> payload, bool retain, CancellationToken cancellationToken)
        {
            await _createClientTaskSource.Task.WithCancellation(cancellationToken).ConfigureAwait(false);

            var applicationMessage = new MqttApplicationMessageBuilder()
                   .WithTopic(fullTopicName)
                   .WithPayload(payload)
                   .WithRetainFlag(retain)
                   .Build();

            _logger.LogDebug("Отправка MQTT сообщения в топик {fullTopicName}", fullTopicName);

            if (_stopping)
                throw new OperationCanceledException("Служба обмена по MQTT остановлена. Отправка сообщения невозможна");

            // если остановка сервиса начнется во время вызова EnqueueAsync, то в зависимости от его реализации может это последнее сообщени не отправится
            // можно вернуть ошибку за счет использования _stoppingToken, но вроде особого смысла нет
            await _mqttClient!.EnqueueAsync(applicationMessage).WithCancellation(cancellationToken).ConfigureAwait(false);
        }
    }
}
