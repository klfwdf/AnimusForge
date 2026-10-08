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
    // Controlled leaves only: JSON and chunking below use real production code.
    public sealed class WorldDiplomacyStorage
    {
        public string Marker { get; set; } = "fresh";
        public List<string> Documents { get; set; } = new();
    }
    internal static class Logger
    {
        internal static bool IsModLogicEnabled => false;
        internal static void Log(string source, string message) { }
    }
}
