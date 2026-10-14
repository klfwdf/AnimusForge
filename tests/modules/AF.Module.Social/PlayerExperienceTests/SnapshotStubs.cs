using System.Collections.Generic;

// Only game-object reads/knowledge policy/rendering are substitutes. Selection/cache/source
// validation below uses the production partial and the production receipt owner directly.
namespace TaleWorlds.CampaignSystem {
    public sealed class Hero {
        public string StringId;
        public bool KnowsHistory, KnowsRecent;
        public TaleWorlds.CampaignSystem.Party.MobileParty PartyBelongedTo;
    }
}
namespace TaleWorlds.CampaignSystem.Party {
    public sealed class MobileParty { public static MobileParty MainParty; }
}
namespace AnimusForge {
    public sealed partial class PlayerNotorietyBehavior {
        internal static PlayerNotorietyBehavior Instance;
        internal static int TestDay = 10;
        private State _state = new();
        private class State { public List<PlayerActionEntry> RecentActions = new(); public string MajorSummary = ""; }
        private class PlayerActionEntry { public int Day, Sequence; public string Text, StableKey; }
        private static bool IsValidObserver(TaleWorlds.CampaignSystem.Hero hero) => hero != null;
        private static int GetCurrentGameDayIndex() => TestDay;
        private bool CanObserverKnowRecentActions(TaleWorlds.CampaignSystem.Hero hero, bool courier) => hero.KnowsRecent;
        private bool DoesObserverKnowPlayer(TaleWorlds.CampaignSystem.Hero hero) => hero.KnowsHistory;
        private static string BuildPlayerDisplayNameForPrompt(TaleWorlds.CampaignSystem.Hero hero) => "Known alias";
        private static string BuildPlayerHistoryDisplayName() => "Known alias";
        private static string RenderPlayerActionTextForPrompt(string text, string name) => (text ?? "").Replace("PLAYER_RAW", name);
        private static string RenderPlayerHistoryTextForPrompt(string text, string name) => (text ?? "").Replace("PLAYER_RAW", name);
        internal void Add(int day, int sequence, string key, string text) => _state.RecentActions.Add(new PlayerActionEntry { Day=day, Sequence=sequence, StableKey=key, Text=text });
        internal void SetHistory(string text) => _state.MajorSummary = text;
        internal void RemoveEvents() => _state.RecentActions.Clear();
    }
    public sealed partial class ProactiveNpcRequestBehavior {
        internal static PlayerExperienceContactState TestState = new();
        internal static bool CanOfferExperience(TaleWorlds.CampaignSystem.Hero hero, PlayerExperienceMotive motive) => TestState.CanOffer(hero.StringId, motive, PlayerNotorietyBehavior.TestDay);
    }
}
