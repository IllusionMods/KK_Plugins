using System;
using System.Reflection;
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
        static string PluginKey = "SpawnLock";

        [IgnoreMember]
        static IFormatterResolver resolver = UnityEngineTypeFormatterResolver.Instance;

        public static SpawnLockData Load( PluginData data )
        {
            if (data?.data == null)
                return null;

            object bytesObj;
            if (!data.data.TryGetValue(PluginKey, out bytesObj))
                return null;

            var bytes = bytesObj as byte[];

            if (bytes == null)
                return null;

            return LZ4MessagePackSerializer.Deserialize<SpawnLockData>(bytes, resolver);
        }

        public PluginData Save()
        {
            PluginData pluginData = new PluginData();

            byte[] bytes = LZ4MessagePackSerializer.Serialize(this, resolver);
            pluginData.data.Add(PluginKey, bytes);

            return pluginData;
        }
    }

    public class UnityEngineTypeFormatterResolver : IFormatterResolver
    {
        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            System.Object formatter;
            formatter = MessagePack.Unity.UnityResolver.Instance.GetFormatter<T>();

            if (formatter != null)
                return (IMessagePackFormatter<T>)formatter;

            return MessagePack.Resolvers.StandardResolver.Instance.GetFormatter<T>();
        }

        public static UnityEngineTypeFormatterResolver Instance = new UnityEngineTypeFormatterResolver();
    }
}