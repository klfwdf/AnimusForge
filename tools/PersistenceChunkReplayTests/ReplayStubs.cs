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

namespace AnimusForge
{
    internal static class Logger
    {
        public static bool IsModLogicEnabled => false;
        public static void Log(string tag, string message) { }
    }
}
