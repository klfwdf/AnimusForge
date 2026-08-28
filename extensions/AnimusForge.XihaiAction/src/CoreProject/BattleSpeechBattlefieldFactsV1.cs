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
            BattleType = string.IsNullOrWhiteSpace(battleType) ? "未知战斗类型" : battleType.Trim();
            Phase = string.IsNullOrWhiteSpace(phase) ? "未知阶段" : phase.Trim();
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
            List<string> facts = new List<string>();
            if (HasReliableForceCounts)
            {
                facts.Add(
                    "我方当前有效人类约" + FriendlyActiveHumanCount +
                    "人，敌方当前有效人类约" + EnemyActiveHumanCount + "人");
            }
            if (FriendlyRemovedSinceBaseline >= 0 || EnemyRemovedSinceBaseline >= 0)
            {
                facts.Add(
                    "本模组记录的战斗中减员：我方" +
                    Math.Max(0, FriendlyRemovedSinceBaseline) +
                    "人、敌方" + Math.Max(0, EnemyRemovedSinceBaseline) + "人");
            }
            facts.Add("敌人是否已接近演讲者：" + (EnemyNearby ? "是" : "否"));
            facts.Add("演讲者是否正在交战：" + (SpeakerInCombat ? "是" : "否"));
            facts.Add("战斗类型：" + BattleType);
            facts.Add("当前阶段：" + Phase);
            if (!string.IsNullOrWhiteSpace(FriendlyFactionName))
            {
                facts.Add("玩家方当前政治势力：" + FriendlyFactionName);
            }
            if (!string.IsNullOrWhiteSpace(SpeakerFactionName))
            {
                facts.Add("演讲者当前随军政治所属：" + SpeakerFactionName);
            }
            if (!string.IsNullOrWhiteSpace(SpeakerCultureName))
            {
                facts.Add("演讲者文化出身：" + SpeakerCultureName);
            }
            if (EnemyFactionNames.Count > 0)
            {
                facts.Add("当前敌方政治势力：" + string.Join("、", EnemyFactionNames));
            }

            return "【当前战场事实快照（程序事实，不是预先给出的主题结论）】" +
                   string.Join("；", facts) + "。" +
                   "地形、天气、阵线缺口和具体眼前动静只可使用 AF 当前场景上下文中已经提供的内容；" +
                   "文化、兵种名和装备名只表示出身或类型，不等于当前政治效忠。若使用国家、王国、统治者或家族口号，" +
                   (string.IsNullOrWhiteSpace(FriendlyFactionName)
                       ? "我方政治势力未知，因此避免使用任何国家万岁、为了某国、某国必胜或效忠某国的口号；改用我方、队伍或阵线。"
                       : "只能以玩家方当前政治势力“" + FriendlyFactionName + "”为我方；不得因文化出身高喊其他国家万岁、荣耀、必胜或效忠口号。") +
                   "快照没有提供的事实不得补造。请你根据这些事实、角色身份和当前场景自行选择一种主风格，" +
                   "不要把快照原样写成战况报告。";
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
