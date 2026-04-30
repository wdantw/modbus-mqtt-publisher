namespace ModbusMqttPublisher.Server.Common
{
    public static class MqttPath
    {
        public const string TopicPathDelimeter = "/";
        public const string WildcardMultyLevel = "#";
        public const string WildcardSingleLevel = "+";
        public const string SystemTopicPreffix = "$";

        public static string CombineTopicPath(string? basePath, string tailPath)
        {
            if (tailPath.StartsWith(TopicPathDelimeter))
                return tailPath[1..];

            if (string.IsNullOrWhiteSpace(basePath))
                return tailPath;

            if (basePath.EndsWith(TopicPathDelimeter))
                basePath = basePath[..^1];

            if (basePath.StartsWith(TopicPathDelimeter))
                basePath = basePath[1..];

            return basePath + TopicPathDelimeter + tailPath;
        }

        public static string? GetRelativeTopicName(string fullTopicName, string baseTopicName)
        {
            if (string.IsNullOrWhiteSpace(baseTopicName))
                return fullTopicName;

            if (!fullTopicName.StartsWith(baseTopicName + TopicPathDelimeter))
                return null;

            return fullTopicName[(baseTopicName.Length + TopicPathDelimeter.Length)..];
        }
    }
}
