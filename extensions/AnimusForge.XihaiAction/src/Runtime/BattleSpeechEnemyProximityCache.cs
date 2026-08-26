using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    /// <summary>
    /// Mission-scoped, speaker-scoped near-enemy cache shared by the speech
    /// session and performance behaviors.  Both behaviors tick on the main
    /// Mission thread, so a small reference scan is cheaper and safer here
    /// than maintaining two independent Team.ActiveAgents traversals.
    /// </summary>
    internal static class BattleSpeechEnemyProximityCache
    {
        private static readonly List<Entry> Entries = new List<Entry>();

        internal static bool HasNearbyEnemy(
            Mission mission,
            Agent speaker,
            float radiusMeters,
            float intervalSeconds)
        {
            if (mission == null || speaker == null || speaker.Team == null ||
                !speaker.Team.IsValid || !ReferenceEquals(speaker.Mission, mission))
            {
                return false;
            }

            float radiusSquared = Math.Max(0f, radiusMeters) * Math.Max(0f, radiusMeters);
            float interval = Math.Max(0.05f, intervalSeconds);
            Entry entry = Find(mission, speaker);
            double now = mission.CurrentTime;
            bool radiusChanged = !entry.HasValue ||
                                  Math.Abs(entry.RadiusSquared - radiusSquared) > 0.0001f;
            bool intervalChanged = !entry.HasValue ||
                                    Math.Abs(entry.IntervalSeconds - interval) > 0.0001f;
            if (!entry.HasValue || radiusChanged || intervalChanged ||
                now >= entry.NextScanAtMissionTime)
            {
                entry.CachedNearbyEnemy = Scan(mission, speaker, radiusSquared);
                entry.HasValue = true;
                entry.RadiusSquared = radiusSquared;
                entry.IntervalSeconds = interval;
                entry.Dirty = false;
                entry.NextScanAtMissionTime = now + interval;
            }
            return entry.CachedNearbyEnemy;
        }

        internal static void Invalidate(
            Mission mission,
            Agent affectedAgent = null,
            Agent affectorAgent = null)
        {
            if (mission == null)
            {
                return;
            }
            for (int index = Entries.Count - 1; index >= 0; index--)
            {
                Entry entry = Entries[index];
                if (!ReferenceEquals(entry.Mission, mission))
                {
                    continue;
                }
                if (affectedAgent == null && affectorAgent == null ||
                    ReferenceEquals(entry.Speaker, affectedAgent) ||
                    ReferenceEquals(entry.Speaker, affectorAgent))
                {
                    // Dirty is deliberately advisory: a hit invalidates the
                    // result, but the 0.4 s lower-bound still prevents a scan
                    // burst in a large melee scrum.
                    entry.Dirty = true;
                }
            }
        }

        internal static void Reset(Mission mission)
        {
            if (mission == null)
            {
                return;
            }
            for (int index = Entries.Count - 1; index >= 0; index--)
            {
                if (ReferenceEquals(Entries[index].Mission, mission))
                {
                    Entries.RemoveAt(index);
                }
            }
        }

        private static Entry Find(Mission mission, Agent speaker)
        {
            for (int index = 0; index < Entries.Count; index++)
            {
                Entry entry = Entries[index];
                if (ReferenceEquals(entry.Mission, mission) &&
                    ReferenceEquals(entry.Speaker, speaker))
                {
                    return entry;
                }
            }
            Entry created = new Entry
            {
                Mission = mission,
                Speaker = speaker
            };
            Entries.Add(created);
            return created;
        }

        private static bool Scan(Mission mission, Agent speaker, float radiusSquared)
        {
            foreach (Team team in mission.Teams)
            {
                if (team == null || !team.IsValid || team.Side == speaker.Team.Side)
                {
                    continue;
                }
                foreach (Agent enemy in team.ActiveAgents)
                {
                    if (enemy != null && enemy.Team != null && enemy.Team.IsValid &&
                        enemy.IsActive() && enemy.IsHuman &&
                        enemy.Position.AsVec2.DistanceSquared(speaker.Position.AsVec2) <=
                        radiusSquared)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private sealed class Entry
        {
            internal Mission Mission;
            internal Agent Speaker;
            internal bool HasValue;
            internal bool Dirty;
            internal bool CachedNearbyEnemy;
            internal float RadiusSquared;
            internal float IntervalSeconds;
            internal double NextScanAtMissionTime;
        }
    }
}
