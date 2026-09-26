using System;
using System.Collections.Generic;

namespace AnimusForge;

public sealed partial class PlayerNotorietyBehavior
{
    // Nested solely to preserve the existing private serialized DTO identities.
    // This owner holds the one state graph; host properties are compatibility views.
    // Only stable IDs/values enter here. Game qualification, clock and RNG stay in host.
    private sealed class NotorietyObservationOwner
    {
        internal PlayerNotorietyState State = new PlayerNotorietyState();
        internal readonly Dictionary<string, ActiveConversationState> Active =
            new Dictionary<string, ActiveConversationState>(StringComparer.OrdinalIgnoreCase);

        internal bool SetLowProfile(bool enabled)
        {
            if (State.LowProfileModeEnabled == enabled) return false;
            State.LowProfileModeEnabled = enabled;
            // Host abandons exact receipts before clearing Active, in the original order.
            return true;
        }

        internal void MarkKnown(PlayerNpcKnowledgeState state, int day)
        {
            state.KnowsMajorHistory = true;
            if (state.KnownAtDay < 0) state.KnownAtDay = day;
        }

        internal ActiveConversationState BeginConversation(string key, int chance, bool knows, int day, int hour)
        {
            if (Active.TryGetValue(key, out ActiveConversationState current) && current != null) return current;
            var active = new ActiveConversationState
            {
                HeroId = key, StartDay = day, StartHour = hour,
                KnownRollChance = chance, KnowsMajorThisSession = knows, LineCount = 0
            };
            Active[key] = active;
            return active;
        }

        internal bool CanKnowRecent(PlayerNpcKnowledgeState state, bool courier, float threshold)
        {
            return courier ? state.LastCourierSentDistance >= 0f && state.LastCourierSentDistance <= threshold
                : state.CompletedConversationSessions >= 1;
        }

        internal int EffectiveNotoriety(string cultureId, double clanTierBonus, PlayerNpcKnowledgeState npc)
        {
            State.CultureNotoriety.TryGetValue(NormalizeCultureId(cultureId), out double culture);
            return ClampPercent(culture + State.WorldNotoriety + clanTierBonus + (npc?.PersonalKnownBonus ?? 0));
        }

        internal void FinalizeLegacyConversation(string key, ActiveConversationState active, PlayerNpcKnowledgeState state, int day)
        {
            state.CompletedConversationSessions++;
            if (!state.KnowsMajorHistory && active.LineCount > 0)
                state.PersonalKnownBonus = ClampPercentDouble(state.PersonalKnownBonus + active.LineCount * PersonalKnownBonusPerLine);
            state.LastConversationDay = day;
            Active.Remove(key);
        }

        internal void AddCulture(string cultureId, double delta)
        {
            State.CultureNotoriety.TryGetValue(cultureId, out double current);
            State.CultureNotoriety[cultureId] = ClampPercentDouble(current + delta);
        }

        internal void AddWorld(double delta)
        {
            State.WorldNotoriety = ClampPercentDouble(State.WorldNotoriety + delta);
        }

        internal PlayerNpcKnowledgeState GetKnowledge(string observerKey, bool create)
        {
            string key = NormalizeObserverKey(observerKey);
            if (string.IsNullOrWhiteSpace(key) || key == PlayerHeroId)
            {
                return null;
            }
            if (!State.NpcKnowledge.TryGetValue(key, out PlayerNpcKnowledgeState state) || state == null)
            {
                if (!create)
                {
                    return null;
                }
                state = new PlayerNpcKnowledgeState
                {
                    HeroId = key,
                    PersonalKnownBonus = 0,
                    LastCourierSentDistance = -1f
                };
                State.NpcKnowledge[key] = state;
            }
            state.HeroId = key;
            if (state.LastCourierSentDistance < -0.01f)
            {
                state.LastCourierSentDistance = -1f;
            }
            return state;
        }
    }
}
