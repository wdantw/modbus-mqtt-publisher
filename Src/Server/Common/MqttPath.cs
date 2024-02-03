namespace ModbusMqttPublisher.Server.Common
{
    public static class MqttPath
    {
        public const string TopicPathDelimeter = "/";

        public static string CombineTopicPath(string? basePath, string tailPath)
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
    }
}
