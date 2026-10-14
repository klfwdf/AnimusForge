// The production VMs and binding notifications are real; game state, localization and actions are doubles.
namespace TaleWorlds.Library
{
    public class MBBindingList<T> : List<T> { }
    public readonly struct Color { public static Color FromUint(uint value) => new(); }
}
namespace TaleWorlds.Core
{
    public static class CharacterCode { public static string CreateFrom(TaleWorlds.CampaignSystem.CharacterObject character) => "portrait"; }
}
namespace TaleWorlds.Core.ImageIdentifiers
{
    public class ImageIdentifier { public string Id => "portrait"; public string AdditionalArgs => ""; public string TextureProviderName => ""; }
    public sealed class CharacterImageIdentifier : ImageIdentifier { public CharacterImageIdentifier(string code) { } }
}
namespace AnimusForge.DialogueUI
{

    internal static class DialogueUiSprites { public static bool EnsureShoutScrollLoaded() => false; }
}
namespace AnimusForge.DialogueUI.Scene
{
    public sealed class SceneTradeVM : TaleWorlds.Library.ViewModel
    {
        private readonly Action _changed;
        public bool IsOpen;
        public int LayoutVersion;
        public SceneTradeVM(Action<string> status, Action changed) { _changed = changed; }
        public void Open(string mode) { IsOpen = true; _changed(); }
        public void Close() { IsOpen = false; _changed(); }
    }
}
namespace AnimusForge
{


    public enum ScenePresentationParticipantState { Participating, Excluded, Locked }
    public sealed class ScenePresentationParticipantInfo
    {
        public int AgentIndex; public TaleWorlds.CampaignSystem.CharacterObject Character;
        public string Name, Role; public ScenePresentationParticipantState State; public bool IsAddressee, IsInRange;
    }
    public sealed class ScenePresentationHistoryLine { public string Speaker, Text, Kind; }
    public partial class ShoutBehavior
    {
        public static bool CanRewriteNativeConversationForExternal, CanRewriteSceneConversationForExternal;
        public static bool RewriteSucceeds = true;
        public static int NativeRewriteCalls, SceneRewriteCalls, SceneSubmitCalls, HistoryReads;
        public static string RewritePlayerText = "old player line", RewriteStatus = "round removed";
        public static List<ScenePresentationHistoryLine> SceneHistory = new();
        public static bool TryRewriteNativeConversationForExternal(out string text, out string status)
        {
            NativeRewriteCalls++; text = RewritePlayerText; status = RewriteStatus;
            if (RewriteSucceeds) CanRewriteNativeConversationForExternal = false;
            return RewriteSucceeds;
        }
        public static bool TryRewriteSceneConversationForExternal(out string text, out string status)
        {
            SceneRewriteCalls++; text = RewritePlayerText; status = RewriteStatus;
            if (RewriteSucceeds) { CanRewriteSceneConversationForExternal = false; SceneHistory.RemoveAll(x => x.Text == "old player line" || x.Text == "old npc line"); }
            return RewriteSucceeds;
        }
        public static bool SceneIllustrationAvailable;
        public static bool IsSceneIllustrationAvailableForExternal => SceneIllustrationAvailable;
        public static bool IsSceneIllustrationBusyForExternal => false;
        public static string SceneIllustrationStatusForExternal => "";
        public static bool IsScenePresentationActiveForExternal => true;
        public static bool IsScenePresentationCollapsedForExternal => false;
        public static bool HasScenePresentationStagedTradeForExternal;
        public static string GetScenePresentationStagedTradeSummaryForExternal() => "gift";
        public static string ConsumeScenePresentationTradeRequestForExternal() => null;
        public static void CancelScenePresentationTradeForExternal() { }
        public static void RequestSceneIllustrationForExternal(Func<bool> current) { }
        public static void OpenSceneIllustrationGalleryForUi() { }
        public static bool OpenScenePresentationEncyclopediaForExternal() => false;
        public static void EndScenePresentationForExternal(string reason) { }
        public static void SetScenePresentationCollapsedForExternal(bool collapsed) { }
        public static void SetAllScenePresentationParticipantsForExternal(bool all) { }
        public static bool SetScenePresentationAddresseeForExternal(int index) => true;
        public static void CycleScenePresentationParticipantForExternal(int index) { }
        public static bool SubmitScenePresentationTextForExternal(string text, out string status) { SceneSubmitCalls++; status = ""; return true; }
        public static bool InterruptScenePresentationForExternal() => true;
        public static List<ScenePresentationParticipantInfo> GetScenePresentationParticipantsForExternal() => new();
        public static List<ScenePresentationHistoryLine> GetScenePresentationHistoryForExternal(int count) { HistoryReads++; return SceneHistory.ToList(); }
    }
}
