using TaleWorlds.Localization;

namespace AnimusForge.XihaiAction
{
    internal static class SceneActionsText
    {
        // Package initialization supplies this immutable choice; UI language is separate.
        internal static bool UseChineseLanguage()
            => AnimusForge.SceneActions.Core.SceneActionPromptPackage.UseChinese;

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
                .SetTextVariable("SPEAKER", speaker ?? new TextObject("{=SAX_Reason_UnknownSpeaker}Unknown").ToString())
                .SetTextVariable("COUNT", audienceCount);
        }

        public static TextObject BattleSpeechCancelled(string reason)
        {
            return new TextObject("{=SAX_BattleSpeechCancelled}Battle speech ended: {REASON}")
                .SetTextVariable("REASON", LocalizeSpeechReason(reason));
        }

        public static TextObject BattleSpeechLine(string speaker, string speech)
        {
            return new TextObject("{=SAX_BattleSpeechLine}[Battle Speech] {SPEAKER}: {SPEECH}")
                .SetTextVariable("SPEAKER", speaker ?? new TextObject("{=SAX_Reason_UnknownSpeaker}Unknown").ToString())
                .SetTextVariable("SPEECH", speech ?? string.Empty);
        }

        private static string LocalizeSpeechReason(string reason)
        {
            switch (reason)
            {
                case null:
                case "unknown": return new TextObject("{=SAX_Reason_UnknownReason}Unknown reason").ToString();
                case "The speaker left the Mission.": return new TextObject("{=SAX_Reason_SpeakerLeft}The speaker left the Mission.").ToString();
                case "The speaker changed team.": return new TextObject("{=SAX_Reason_SpeakerTeamChanged}The speaker changed team.").ToString();
                case "Cancelled by player.": return new TextObject("{=SAX_Reason_PlayerCancelled}Cancelled by player.").ToString();
                case "Replaced by a newer battle speech request.": return new TextObject("{=SAX_Reason_RequestReplaced}Replaced by a newer battle speech request.").ToString();
                case "Speech text or speaker is invalid.": return new TextObject("{=SAX_Reason_InvalidSpeech}Speech text or speaker is invalid.").ToString();
                case "Battle speech phase changed.": return new TextObject("{=SAX_Reason_PhaseChanged}Battle speech phase changed.").ToString();
                case "Speaker became unavailable.": return new TextObject("{=SAX_Reason_SpeakerUnavailable}Speaker became unavailable.").ToString();
                case "Battle speech phase closed.": return new TextObject("{=SAX_Reason_PhaseClosed}Battle speech phase closed.").ToString();
                case "Battle speech request timed out.": return new TextObject("{=SAX_Reason_RequestTimedOut}Battle speech request timed out.").ToString();
                case "AF dedicated NPC speech bridge is unavailable or the speaker is no longer active.": return new TextObject("{=SAX_Reason_BridgeUnavailable}AF dedicated NPC speech bridge is unavailable or the speaker is no longer active.").ToString();
                case "AF ShoutBehavior instance is unavailable.": return new TextObject("{=SAX_Reason_ShoutUnavailable}AF ShoutBehavior instance is unavailable.").ToString();
                case "AF could not build the NPC speech packet.": return new TextObject("{=SAX_Reason_PacketUnavailable}AF could not build the NPC speech packet.").ToString();
                case "Dedicated NPC speech session could not claim its prompt.": return new TextObject("{=SAX_Reason_PromptUnclaimed}Dedicated NPC speech session could not claim its prompt.").ToString();
                case "AF returned an unexpected NPC speech task.": return new TextObject("{=SAX_Reason_UnexpectedTask}AF returned an unexpected NPC speech task.").ToString();
                case "Deferred AF reply is no longer replayable.": return new TextObject("{=SAX_Reason_ReplayUnavailable}Deferred AF reply is no longer replayable.").ToString();
                case "AF replay returned no playback result.": return new TextObject("{=SAX_Reason_ReplayNoResult}AF replay returned no playback result.").ToString();
            }
            const string requestPrefix = "NPC speech request could not start: ";
            const string replayPrefix = "Deferred AF reply replay failed: ";
            if (reason.StartsWith(requestPrefix, System.StringComparison.Ordinal))
                return new TextObject("{=SAX_Reason_NpcRequestFailed}NPC speech request could not start: {DETAIL}")
                    .SetTextVariable("DETAIL", "AFSCENEREASONLITERALEND").ToString()
                    .Replace("AFSCENEREASONLITERALEND", LocalizeSpeechReason(reason.Substring(requestPrefix.Length)));
            if (reason.StartsWith(replayPrefix, System.StringComparison.Ordinal))
                return new TextObject("{=SAX_Reason_ReplayFailed}Deferred AF reply replay failed: {DETAIL}")
                    .SetTextVariable("DETAIL", "AFSCENEREASONLITERALEND").ToString()
                    .Replace("AFSCENEREASONLITERALEND", LocalizeSpeechReason(reason.Substring(replayPrefix.Length)));
            // Preserve unknown exception details and third-party reasons verbatim.
            return reason;
        }

        public static TextObject BatchSummary(int accepted, int failed, int skipped)
        {
            return new TextObject("{=SAX_BatchSummary}Scene actions: {ACCEPTED} accepted, {FAILED} failed, {SKIPPED} skipped.")
                .SetTextVariable("ACCEPTED", accepted)
                .SetTextVariable("FAILED", failed)
                .SetTextVariable("SKIPPED", skipped);
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

        public static TextObject BattleSpeechNpcTargetNotAllied()
        {
            return new TextObject("{=SAX_BattleSpeechNpcTargetNotAllied}Battle speech did not start: other-speaker speech requires a selected allied soldier.");
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
