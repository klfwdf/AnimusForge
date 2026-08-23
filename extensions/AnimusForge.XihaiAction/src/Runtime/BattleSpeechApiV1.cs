using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SceneActions.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    public interface IBattleSpeechEffectV1
    {
        void OnSpeechStarted(BattleSpeechSessionSnapshotV1 speech);
        void OnSpeechCompleted(BattleSpeechSessionSnapshotV1 speech);
        void OnSpeechCancelled(BattleSpeechSessionSnapshotV1 speech, string reason);
    }

    internal interface IBattleSpeechRuntimeEffectV1
    {
        void OnSpeechStarted(BattleSpeechRuntimeContextV1 speech);
        void OnSpeechCompleted(BattleSpeechRuntimeContextV1 speech);
        void OnSpeechCancelled(BattleSpeechRuntimeContextV1 speech, string reason);
    }

    internal sealed class BattleSpeechRuntimeContextV1
    {
        public BattleSpeechRuntimeContextV1(
            BattleSpeechSessionSnapshotV1 snapshot,
            Mission mission,
            Agent speaker,
            IReadOnlyList<Agent> frozenAudience,
            ActionProgramV4 actionProgram,
            BattleSpeechTacticV2 tactic,
            IReadOnlyList<string> audienceReplies = null)
        {
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            Mission = mission ?? throw new ArgumentNullException(nameof(mission));
            Speaker = speaker ?? throw new ArgumentNullException(nameof(speaker));
            FrozenAudience = (frozenAudience ?? Array.Empty<Agent>()).ToArray();
            ActionProgram = actionProgram;
            Tactic = tactic;
            AudienceReplies = (audienceReplies ?? Array.Empty<string>()).ToArray();
        }

        public BattleSpeechSessionSnapshotV1 Snapshot { get; }
        public Mission Mission { get; }
        public Agent Speaker { get; }
        public Agent[] FrozenAudience { get; }
        public ActionProgramV4 ActionProgram { get; }
        public BattleSpeechTacticV2 Tactic { get; }
        public string[] AudienceReplies { get; }
    }

    public static class BattleSpeechApiV1
    {
        private static readonly object Sync = new object();
        private static readonly List<IBattleSpeechEffectV1> Effects =
            new List<IBattleSpeechEffectV1>();
        private static readonly List<IBattleSpeechRuntimeEffectV1> RuntimeEffects =
            new List<IBattleSpeechRuntimeEffectV1>();
        private static BattleSpeechSessionSnapshotV1 _current;

        public static int ContractVersion => BattleSpeechFrameworkV1.ContractVersion;

        public static bool TryGetCurrent(out BattleSpeechSessionSnapshotV1 speech)
        {
            lock (Sync)
            {
                speech = _current;
                return speech != null;
            }
        }

        public static IDisposable RegisterEffect(IBattleSpeechEffectV1 effect)
        {
            if (effect == null) throw new ArgumentNullException(nameof(effect));
            lock (Sync)
            {
                if (Effects.Contains(effect))
                {
                    throw new InvalidOperationException("Battle speech effect is already registered.");
                }
                Effects.Add(effect);
            }
            return new EffectRegistration(effect);
        }

        internal static IDisposable RegisterRuntimeEffect(IBattleSpeechRuntimeEffectV1 effect)
        {
            if (effect == null) throw new ArgumentNullException(nameof(effect));
            lock (Sync)
            {
                if (RuntimeEffects.Contains(effect))
                {
                    throw new InvalidOperationException(
                        "Battle speech runtime effect is already registered.");
                }
                RuntimeEffects.Add(effect);
            }
            return new RuntimeEffectRegistration(effect);
        }

        internal static void PublishStarted(
            BattleSpeechSessionSnapshotV1 speech,
            BattleSpeechRuntimeContextV1 context)
        {
            IBattleSpeechEffectV1[] effects;
            IBattleSpeechRuntimeEffectV1[] runtimeEffects;
            lock (Sync)
            {
                _current = speech;
                effects = Effects.ToArray();
                runtimeEffects = RuntimeEffects.ToArray();
            }
            foreach (IBattleSpeechEffectV1 effect in effects)
            {
                InvokeSafely(() => effect.OnSpeechStarted(speech));
            }
            foreach (IBattleSpeechRuntimeEffectV1 effect in runtimeEffects)
            {
                InvokeSafely(() => effect.OnSpeechStarted(context));
            }
        }

        internal static void PublishCompleted(
            BattleSpeechSessionSnapshotV1 speech,
            BattleSpeechRuntimeContextV1 context)
        {
            IBattleSpeechEffectV1[] effects;
            IBattleSpeechRuntimeEffectV1[] runtimeEffects;
            lock (Sync)
            {
                if (_current != null && _current.SessionId == speech.SessionId) _current = null;
                effects = Effects.ToArray();
                runtimeEffects = RuntimeEffects.ToArray();
            }
            foreach (IBattleSpeechEffectV1 effect in effects)
            {
                InvokeSafely(() => effect.OnSpeechCompleted(speech));
            }
            foreach (IBattleSpeechRuntimeEffectV1 effect in runtimeEffects)
            {
                InvokeSafely(() => effect.OnSpeechCompleted(context));
            }
        }

        internal static void PublishCancelled(
            BattleSpeechSessionSnapshotV1 speech,
            BattleSpeechRuntimeContextV1 context,
            string reason)
        {
            IBattleSpeechEffectV1[] effects;
            IBattleSpeechRuntimeEffectV1[] runtimeEffects;
            lock (Sync)
            {
                if (_current != null && _current.SessionId == speech.SessionId) _current = null;
                effects = Effects.ToArray();
                runtimeEffects = RuntimeEffects.ToArray();
            }
            foreach (IBattleSpeechEffectV1 effect in effects)
            {
                InvokeSafely(() => effect.OnSpeechCancelled(speech, reason));
            }
            foreach (IBattleSpeechRuntimeEffectV1 effect in runtimeEffects)
            {
                InvokeSafely(() => effect.OnSpeechCancelled(context, reason));
            }
        }

        internal static void Reset()
        {
            lock (Sync) _current = null;
        }

        internal static bool ShouldSuppressOrdinarySceneFollowups(Mission mission)
        {
            return BattleSpeechRuntimeHost.IsPerformanceActive(mission);
        }

        private static void InvokeSafely(Action action)
        {
            try { action(); }
            catch (Exception ex)
            {
                SceneActionsLog.Error("BATTLE_SPEECH", "Registered effect failed closed.", ex);
            }
        }

        private sealed class EffectRegistration : IDisposable
        {
            private IBattleSpeechEffectV1 _effect;
            public EffectRegistration(IBattleSpeechEffectV1 effect) { _effect = effect; }
            public void Dispose()
            {
                IBattleSpeechEffectV1 effect = _effect;
                if (effect == null) return;
                _effect = null;
                lock (Sync) Effects.Remove(effect);
            }
        }

        private sealed class RuntimeEffectRegistration : IDisposable
        {
            private IBattleSpeechRuntimeEffectV1 _effect;

            public RuntimeEffectRegistration(IBattleSpeechRuntimeEffectV1 effect)
            {
                _effect = effect;
            }

            public void Dispose()
            {
                IBattleSpeechRuntimeEffectV1 effect = _effect;
                if (effect == null) return;
                _effect = null;
                lock (Sync) RuntimeEffects.Remove(effect);
            }
        }
    }
}
