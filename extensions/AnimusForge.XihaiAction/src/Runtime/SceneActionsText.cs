using TaleWorlds.Localization;

namespace AnimusForge.XihaiAction
{
    internal static class SceneActionsText
    {
        public static TextObject Loaded()
        {
            return new TextObject("{=SAX_Loaded}AnimusForge Xihai Action loaded (v1.1 / Framework V4 + Battle Speech V2).");
        }

        public static TextObject Disabled()
        {
            return new TextObject("{=SAX_Disabled}AnimusForge Xihai Action disabled: the required AnimusForge runtime contract is unavailable.");
        }

        public static TextObject InitializationFailed(string reason)
        {
            return new TextObject("{=SAX_InitializationFailed}AnimusForge Xihai Action initialization failed: {REASON}")
                .SetTextVariable("REASON", reason ?? string.Empty);
        }

        public static TextObject BattleSpeechStarted(string speaker, int audienceCount)
        {
            return new TextObject("{=SAX_BattleSpeechStarted}Battle speech started: {SPEAKER}, addressing {COUNT} soldiers.")
                .SetTextVariable("SPEAKER", speaker ?? "Unknown")
                .SetTextVariable("COUNT", audienceCount);
        }

        public static TextObject BattleSpeechCancelled(string reason)
        {
            return new TextObject("{=SAX_BattleSpeechCancelled}Battle speech ended: {REASON}")
                .SetTextVariable("REASON", reason ?? "Unknown reason");
        }

        public static TextObject BattleSpeechLine(string speaker, string speech)
        {
            return new TextObject("{=SAX_BattleSpeechLine}[Battle Speech] {SPEAKER}: {SPEECH}")
                .SetTextVariable("SPEAKER", speaker ?? "Unknown")
                .SetTextVariable("SPEECH", speech ?? string.Empty);
        }

        public static TextObject BattleSpeechNoAudience()
        {
            return new TextObject("{=SAX_BattleSpeechNoAudience}Battle speech did not start: no eligible allied soldiers were found.");
        }

        public static TextObject BattleSpeechNotReady()
        {
            return new TextObject("{=SAX_BattleSpeechNotReady}Battle speech is available only while deployment or an active battle is continuing.");
        }

        public static TextObject BattleSpeechSpeakerUnavailable()
        {
            return new TextObject("{=SAX_BattleSpeechSpeakerUnavailable}Battle speech did not start: the selected speaker is unavailable.");
        }

        public static TextObject BattleSpeechSpeakerInCombat()
        {
            return new TextObject("{=SAX_BattleSpeechSpeakerInCombat}Battle speech did not start: the selected speaker is attacking or preparing an attack.");
        }

        public static TextObject BattleSpeechEnemyNearby()
        {
            return new TextObject("{=SAX_BattleSpeechEnemyNearby}Battle speech did not start: an enemy is too close to the speaker.");
        }
    }
}
