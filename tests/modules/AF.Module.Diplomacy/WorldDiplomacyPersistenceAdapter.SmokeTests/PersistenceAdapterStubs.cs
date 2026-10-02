using System;
using System.Collections.Generic;

namespace TaleWorlds.CampaignSystem
{
    public interface IDataStore
    {
        bool IsSaving { get; }
        bool IsLoading { get; }
        bool SyncData<T>(string key, ref T data);
    }
}

namespace Newtonsoft.Json
{
    public static class JsonConvert
    {
        internal static Func<object, string> SerializeHandler { get; set; } = _ => "{}";
        internal static Func<string, Type, object> DeserializeHandler { get; set; } = (_, _) => null;

        public static string SerializeObject(object value) => SerializeHandler(value);

        public static T DeserializeObject<T>(string value)
        {
            return (T)DeserializeHandler(value, typeof(T));
        }
    }
}

namespace AnimusForge
{
    using TaleWorlds.CampaignSystem;

    public sealed class WorldDiplomacyStorage
    {
        public string Marker { get; set; } = "";
    }

    public static class CampaignSaveChunkHelper
    {
        internal static bool ThrowOnSave { get; set; }
        internal static bool ThrowOnLoad { get; set; }
        internal static string LastKey { get; private set; } = "";
        internal static string LastSource { get; private set; } = "";
        internal static string LastSavedJson { get; private set; } = "";
        internal static string NextLoadedJson { get; set; } = "";

        internal static void Reset()
        {
            ThrowOnSave = false;
            ThrowOnLoad = false;
            LastKey = "";
            LastSource = "";
            LastSavedJson = "";
            NextLoadedJson = "";
        }

        public static void SaveChunkedString(IDataStore dataStore, string baseKey, string text, string logTag = "Persistence")
        {
            if (ThrowOnSave) throw new InvalidOperationException("synthetic save failure");
            LastKey = baseKey;
            LastSource = logTag;
            LastSavedJson = text;
        }

        public static string LoadChunkedString(IDataStore dataStore, string baseKey, string logTag = "Persistence")
        {
            if (ThrowOnLoad) throw new InvalidOperationException("synthetic load failure");
            LastKey = baseKey;
            LastSource = logTag;
            return NextLoadedJson;
        }
    }
}
