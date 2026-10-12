using System;
using System.Collections.Generic;

namespace AnimusForge.SceneActions.Core
{
    /// <summary>
    /// Mission-time facts supplied to the AF battle-speech prompt.  This type
    /// intentionally contains no theme/tone decision: AF remains responsible
    /// for interpreting the facts and choosing the prose style.
    /// </summary>
    public sealed class BattleSpeechBattlefieldFactsV1
    {
        public BattleSpeechBattlefieldFactsV1(
            int friendlyActiveHumanCount,
            int enemyActiveHumanCount,
            int friendlyRemovedSinceBaseline,
            int enemyRemovedSinceBaseline,
            bool enemyNearby,
            bool speakerInCombat,
            string battleType,
            string phase,
            string friendlyFactionName = null,
            string speakerFactionName = null,
            string speakerCultureName = null,
            IEnumerable<string> enemyFactionNames = null)
        {
            FriendlyActiveHumanCount = NormalizeCount(friendlyActiveHumanCount);
            EnemyActiveHumanCount = NormalizeCount(enemyActiveHumanCount);
            FriendlyRemovedSinceBaseline = NormalizeCount(friendlyRemovedSinceBaseline);
            EnemyRemovedSinceBaseline = NormalizeCount(enemyRemovedSinceBaseline);
            EnemyNearby = enemyNearby;
            SpeakerInCombat = speakerInCombat;
            BattleType = string.IsNullOrWhiteSpace(battleType) ? BattleSpeechText.FactLabel("UnknownBattleType", true) : battleType.Trim();
            Phase = string.IsNullOrWhiteSpace(phase) ? BattleSpeechText.FactLabel("UnknownPhase", true) : phase.Trim();
            FriendlyFactionName = NormalizeName(friendlyFactionName);
            SpeakerFactionName = NormalizeName(speakerFactionName);
            SpeakerCultureName = NormalizeName(speakerCultureName);
            EnemyFactionNames = new List<string>(enemyFactionNames ?? Array.Empty<string>())
                .ConvertAll(NormalizeName)
                .FindAll(value => !string.IsNullOrWhiteSpace(value))
                .AsReadOnly();
        }

        public int FriendlyActiveHumanCount { get; }
        public int EnemyActiveHumanCount { get; }
        public int FriendlyRemovedSinceBaseline { get; }
        public int EnemyRemovedSinceBaseline { get; }
        public bool EnemyNearby { get; }
        public bool SpeakerInCombat { get; }
        public string BattleType { get; }
        public string Phase { get; }
        public string FriendlyFactionName { get; }
        public string SpeakerFactionName { get; }
        public string SpeakerCultureName { get; }
        public IReadOnlyList<string> EnemyFactionNames { get; }

        public bool HasReliableForceCounts =>
            FriendlyActiveHumanCount >= 0 && EnemyActiveHumanCount >= 0;

        public string ToPromptBlock()
        {
            return ToPromptBlock(true);
        }

        public string ToPromptBlock(bool useChinese)
        {
            List<string> facts = new List<string>();
            if (HasReliableForceCounts)
            {
                facts.Add(
                    BattleSpeechText.Text("Prompt_Facts_01", useChinese) + FriendlyActiveHumanCount +
                    BattleSpeechText.Text("Prompt_Facts_02", useChinese) + EnemyActiveHumanCount + BattleSpeechText.Text("Prompt_Facts_03", useChinese));
            }
            if (FriendlyRemovedSinceBaseline >= 0 || EnemyRemovedSinceBaseline >= 0)
            {
                facts.Add(
                    BattleSpeechText.Text("Prompt_Facts_04", useChinese) +
                    Math.Max(0, FriendlyRemovedSinceBaseline) +
                    BattleSpeechText.Text("Prompt_Facts_05", useChinese) + Math.Max(0, EnemyRemovedSinceBaseline) + BattleSpeechText.Text("Prompt_Facts_06", useChinese));
            }
            facts.Add(BattleSpeechText.Text("Prompt_Facts_07", useChinese) + (EnemyNearby ? BattleSpeechText.Text("Prompt_Facts_08", useChinese) : BattleSpeechText.Text("Prompt_Facts_09", useChinese)));
            facts.Add(BattleSpeechText.Text("Prompt_Facts_10", useChinese) + (SpeakerInCombat ? BattleSpeechText.Text("Prompt_Facts_11", useChinese) : BattleSpeechText.Text("Prompt_Facts_12", useChinese)));
            facts.Add(BattleSpeechText.Text("Prompt_Facts_13", useChinese) + BattleSpeechText.ProjectFactLabel(BattleType, useChinese));
            facts.Add(BattleSpeechText.Text("Prompt_Facts_14", useChinese) + BattleSpeechText.ProjectFactLabel(Phase, useChinese));
            if (!string.IsNullOrWhiteSpace(FriendlyFactionName))
            {
                facts.Add(BattleSpeechText.Text("Prompt_Facts_15", useChinese) + FriendlyFactionName);
            }
            if (!string.IsNullOrWhiteSpace(SpeakerFactionName))
            {
                facts.Add(BattleSpeechText.Text("Prompt_Facts_16", useChinese) + SpeakerFactionName);
            }
            if (!string.IsNullOrWhiteSpace(SpeakerCultureName))
            {
                facts.Add(BattleSpeechText.Text("Prompt_Facts_17", useChinese) + SpeakerCultureName);
            }
            if (EnemyFactionNames.Count > 0)
            {
                facts.Add(BattleSpeechText.Text("Prompt_Facts_18", useChinese) + string.Join(BattleSpeechText.Text("Prompt_Facts_19", useChinese), EnemyFactionNames));
            }

            return BattleSpeechText.Text("Prompt_Facts_20", useChinese) +
                   string.Join(BattleSpeechText.Text("Prompt_Facts_21", useChinese), facts) + BattleSpeechText.Text("Prompt_Facts_22", useChinese) +
                   BattleSpeechText.Text("Prompt_Facts_23", useChinese) +
                   BattleSpeechText.Text("Prompt_Facts_24", useChinese) +
                   (string.IsNullOrWhiteSpace(FriendlyFactionName)
                       ? BattleSpeechText.Text("Prompt_Facts_25", useChinese)
                       : BattleSpeechText.Text("Prompt_Facts_26", useChinese) + FriendlyFactionName + BattleSpeechText.Text("Prompt_Facts_27", useChinese)) +
                   BattleSpeechText.Text("Prompt_Facts_28", useChinese) +
                   BattleSpeechText.Text("Prompt_Facts_29", useChinese);
        }

        public bool IsFriendlyPoliticalName(string value)
        {
            string candidate = NormalizeName(value);
            if (string.IsNullOrWhiteSpace(candidate) ||
                string.IsNullOrWhiteSpace(FriendlyFactionName))
            {
                return false;
            }
            return FriendlyFactionName.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   candidate.IndexOf(FriendlyFactionName, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string NormalizeName(string value)
        {
            return (value ?? string.Empty)
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();
        }

        private static int NormalizeCount(int value)
        {
            return value < 0 ? -1 : value;
        }
    }
}
