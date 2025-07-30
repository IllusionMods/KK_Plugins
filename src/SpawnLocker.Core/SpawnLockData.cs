using ExtensibleSaveFormat;
using MessagePack;
using MessagePack.Formatters;

namespace SpawnLocker
{
    [MessagePackObject]
    public class SpawnLockData
    {
        [Key(0)]
        public bool isLocked;

        [IgnoreMember]
        private static string PluginKey = "SpawnLock";

        [IgnoreMember]
        private static IFormatterResolver resolver = UnityEngineTypeFormatterResolver.Instance;

        public static SpawnLockData Load(PluginData data)
        {
            if (data?.data == null)
                return null;

            if (!data.data.TryGetValue(PluginKey, out object bytesObj))
                return null;

            if (bytesObj is byte[] bytes)
                return LZ4MessagePackSerializer.Deserialize<SpawnLockData>(bytes, resolver);

            return null;
        }

        public PluginData Save()
        {
            PluginData pluginData = new PluginData();

            var bytes = LZ4MessagePackSerializer.Serialize(this, resolver);
            pluginData.data.Add(PluginKey, bytes);

            return pluginData;
        }
    }

    public class UnityEngineTypeFormatterResolver : IFormatterResolver
    {
        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return MessagePack.Unity.UnityResolver.Instance.GetFormatter<T>() ?? MessagePack.Resolvers.StandardResolver.Instance.GetFormatter<T>();
        }

        public static UnityEngineTypeFormatterResolver Instance = new UnityEngineTypeFormatterResolver();
    }
}
