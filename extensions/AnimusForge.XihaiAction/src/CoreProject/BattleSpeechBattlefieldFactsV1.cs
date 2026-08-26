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
            string phase)
        {
            FriendlyActiveHumanCount = NormalizeCount(friendlyActiveHumanCount);
            EnemyActiveHumanCount = NormalizeCount(enemyActiveHumanCount);
            FriendlyRemovedSinceBaseline = NormalizeCount(friendlyRemovedSinceBaseline);
            EnemyRemovedSinceBaseline = NormalizeCount(enemyRemovedSinceBaseline);
            EnemyNearby = enemyNearby;
            SpeakerInCombat = speakerInCombat;
            BattleType = string.IsNullOrWhiteSpace(battleType) ? "未知战斗类型" : battleType.Trim();
            Phase = string.IsNullOrWhiteSpace(phase) ? "未知阶段" : phase.Trim();
        }

        public int FriendlyActiveHumanCount { get; }
        public int EnemyActiveHumanCount { get; }
        public int FriendlyRemovedSinceBaseline { get; }
        public int EnemyRemovedSinceBaseline { get; }
        public bool EnemyNearby { get; }
        public bool SpeakerInCombat { get; }
        public string BattleType { get; }
        public string Phase { get; }

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

            return "【当前战场事实快照（程序事实，不是预先给出的主题结论）】" +
                   string.Join("；", facts) + "。" +
                   "地形、天气、阵线缺口和具体眼前动静只可使用 AF 当前场景上下文中已经提供的内容；" +
                   "快照没有提供的事实不得补造。请你根据这些事实、角色身份和当前场景自行选择一种主风格，" +
                   "不要把快照原样写成战况报告。";
        }

        private static int NormalizeCount(int value)
        {
            return value < 0 ? -1 : value;
        }
    }
}
