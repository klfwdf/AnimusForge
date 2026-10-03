using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.DialogueUI.Shout
{
    /// <summary>Presentation-only adapter. All submission, pause and target selection remain owned by AF.</summary>
    public static class ShoutUiAdapter
    {
        [ThreadStatic] private static ShoutContext _openingContext;
        private static readonly ConditionalWeakTable<object, ShoutPresentationVM> Wrappers = new ConditionalWeakTable<object, ShoutPresentationVM>();
        private static readonly List<ShoutPresentationVM> Active = new List<ShoutPresentationVM>();
        private static Type _hostVmType;
        private static bool _installed;
        private static Func<object, int> _packetIndex;
        private static Func<object, object> _tradePacket;
        private static Func<object, Agent> _tradeAgent;
        private static Func<object, bool> _tradeActionOnly;
        private static Func<object, object> _targetingContext;
        private static Func<object, int> _conversationEpoch;
        private static Func<object, object> _popupDataSource;
        private static Func<int> _sceneSession;
        private static Func<int, int, List<string>> _history;
        private static Action<object> _submit;
        private static Action<object> _cancel;
        private static Func<object, string> _subtitle;

        public static void Install(Harmony harmony)
        {
            if (_installed || harmony == null) return;
            try
            {
                Type behavior = AccessTools.TypeByName("AnimusForge.ShoutBehavior");
                Type packet = AccessTools.TypeByName("AnimusForge.NpcDataPacket");
                Type popup = AccessTools.TypeByName("AnimusForge.ShoutTextInputPopup");
                _hostVmType = AccessTools.TypeByName("AnimusForge.ShoutTextInputPopupVM");
                if (behavior == null || packet == null || popup == null || _hostVmType == null)
                    throw new MissingMemberException("AF scene input types are unavailable.");

                MethodInfo direct = RequireMethod(behavior, "OpenShoutTextInput", packet, typeof(string), typeof(string));
                MethodInfo trade = RequireMethod(behavior, "ShowShoutTradeChatInput");
                MethodInfo close = RequireMethod(popup, "Close", typeof(bool));
                MethodInfo submit = RequireMethod(_hostVmType, "ExecuteSubmit");
                _packetIndex = FieldReader<int>(packet, "AgentIndex");
                _tradePacket = FieldReader<object>(behavior, "_shoutTradeTargetNpc");
                _tradeAgent = FieldReader<Agent>(behavior, "_shoutTradeTargetAgentSnapshot");
                _tradeActionOnly = FieldReader<bool>(behavior, "_shoutTradeActionOnly");
                _targetingContext = FieldReader<object>(behavior, "_activeShoutTargetingContext");
                _conversationEpoch = PropertyReader<int>(behavior, "_sceneConversationEpoch");
                _popupDataSource = FieldReader<object>(popup, "_dataSource");
                _sceneSession = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), RequireMethod(behavior, "GetCurrentSceneHistorySessionIdForExternal"));
                _history = (Func<int, int, List<string>>)Delegate.CreateDelegate(typeof(Func<int, int, List<string>>),
                    RequireMethod(behavior, "GetAuxiliarySceneDialogueHistoryLinesForExternal", typeof(int), typeof(int)));
                _submit = InstanceCommand(submit);
                _cancel = InstanceCommand(RequireMethod(_hostVmType, "ExecuteCancel"));
                _subtitle = PropertyReader<string>(_hostVmType, "SubtitleText");

                harmony.Patch(direct, prefix: Patch(nameof(DirectInputPrefix)), finalizer: Patch(nameof(InputFinalizer)));
                harmony.Patch(trade, prefix: Patch(nameof(TradeInputPrefix)), finalizer: Patch(nameof(InputFinalizer)));
                harmony.Patch(submit, prefix: Patch(nameof(SubmitPrefix)));
                harmony.Patch(close, postfix: Patch(nameof(PopupClosedPostfix)));
                _installed = true;
                DialogueUiRuntime.Log("Scene input adapter installed; native tag input and speech channels excluded.");
            }
            catch (Exception ex)
            {
                _installed = false;
                DialogueUiRuntime.Log("Scene input skin unavailable; retaining AF input. " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        public static bool TryWrap(IViewModel original, out ViewModel wrapper)
        {
            wrapper = null;
            if (!_installed || original == null || original.GetType() != _hostVmType)
                return false;
            try
            {
                if (Wrappers.TryGetValue(original, out var existing))
                {
                    // Gauntlet resource refresh reloads the owner's movie outside its opening call.
                    // Reuse presentation state until the original popup closes, including draft/history.
                    if (!existing.RefreshAvailability()) return false;
                    wrapper = existing;
                    return true;
                }
                var host = original as ViewModel;
                if (host == null || _openingContext == null || !_openingContext.IsCurrent()) return false;
                var created = new ShoutPresentationVM(host, _openingContext, _subtitle(original), _submit, _cancel, _history);
                Wrappers.Add(original, created);
                Active.Add(created);
                wrapper = created;
                return true;
            }
            catch (Exception ex)
            {
                DialogueUiRuntime.Log("Scene input wrapping failed; retaining AF input. " + ex.Message);
                return false;
            }
        }

        // Called on the game UI thread. No scene enumeration, reflection lookup or history read here.
        public static void Tick()
        {
            for (int i = Active.Count - 1; i >= 0; i--) Active[i].RefreshAvailability();
        }

        public static void Release(IViewModel original)
        {
            if (original == null || !Wrappers.TryGetValue(original, out var wrapper)) return;
            Wrappers.Remove(original);
            Active.Remove(wrapper);
            wrapper.OnFinalize();
        }

        public static void Shutdown()
        {
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                var wrapper = Active[i];
                if (wrapper.Host != null) Wrappers.Remove(wrapper.Host);
                wrapper.OnFinalize();
            }
            Active.Clear();
            _openingContext = null;
            _installed = false;
        }

        private static void DirectInputPrefix(object __instance, object[] __args, out ShoutContext __state)
        {
            __state = _openingContext;
            _openingContext = Capture(__instance, __args != null && __args.Length > 0 ? __args[0] : null, false);
        }

        private static void TradeInputPrefix(object __instance, out ShoutContext __state)
        {
            __state = _openingContext;
            _openingContext = Capture(__instance, null, true);
        }

        private static Exception InputFinalizer(Exception __exception, ShoutContext __state)
        {
            _openingContext = __state;
            return __exception;
        }

        private static bool SubmitPrefix(object __instance)
        {
            return !Wrappers.TryGetValue(__instance, out var wrapper) || wrapper.RefreshAvailability();
        }

        private static void PopupClosedPostfix(object __instance)
        {
            try { Release(_popupDataSource?.Invoke(__instance) as IViewModel); }
            catch (Exception ex) { DialogueUiRuntime.Log("Scene presentation release failed: " + ex.Message); }
        }

        private static ShoutContext Capture(object owner, object packet, bool trade)
        {
            if (!_installed || owner == null) return null;
            try
            {
                Mission mission = Mission.Current;
                if (mission == null || Campaign.Current?.ConversationManager?.IsConversationInProgress == true) return null;
                if (trade && _tradeActionOnly(owner)) return null;
                if (trade) packet = _tradePacket(owner);
                if (packet == null) return null;
                int index = _packetIndex(packet);
                if (index < 0) return null;
                Agent target = trade ? _tradeAgent(owner) : null;
                if (target == null && !trade)
                {
                    // One bounded scan when the original input opens, never on Tick or history expansion.
                    foreach (Agent candidate in mission.Agents)
                    {
                        if (candidate != null && candidate.Index == index) { target = candidate; break; }
                    }
                }
                if (target == null || target.Index != index) return null;
                var context = new ShoutContext(owner, packet, mission, target, _targetingContext(owner),
                    _sceneSession(), _conversationEpoch(owner));
                return context.IsCurrent() ? context : null;
            }
            catch (Exception ex)
            {
                DialogueUiRuntime.Log("Scene input context unavailable; retaining AF input. " + ex.Message);
                return null;
            }
        }

        internal sealed class ShoutContext
        {
            private object _owner;
            private object _packet;
            private Mission _mission;
            private Agent _target;
            private object _targeting;
            private readonly int _session;
            private readonly int _epoch;
            internal int AgentIndex { get; }

            internal ShoutContext(object owner, object packet, Mission mission, Agent target, object targeting, int session, int epoch)
            {
                _owner = owner; _packet = packet; _mission = mission; _target = target; _targeting = targeting;
                _session = session; _epoch = epoch; AgentIndex = target.Index;
            }

            internal bool IsCurrent()
            {
                try
                {
                    return _owner != null && _target != null && ReferenceEquals(Mission.Current, _mission)
                        && ReferenceEquals(_target.Mission, _mission) && _target.IsActive() && _target.IsHuman
                        && _target.Index == AgentIndex && _packetIndex(_packet) == AgentIndex
                        && _sceneSession() == _session && _conversationEpoch(_owner) == _epoch
                        && ReferenceEquals(_targetingContext(_owner), _targeting)
                        && Campaign.Current?.ConversationManager?.IsConversationInProgress != true;
                }
                catch { return false; }
            }

            internal void Release()
            {
                _owner = null; _packet = null; _mission = null; _target = null; _targeting = null;
            }
        }

        private static HarmonyMethod Patch(string name) => new HarmonyMethod(typeof(ShoutUiAdapter), name);

        private static MethodInfo RequireMethod(Type type, string name, params Type[] parameters)
        {
            return AccessTools.Method(type, name, parameters) ?? throw new MissingMethodException(type.FullName, name);
        }

        private static Func<object, T> FieldReader<T>(Type type, string name)
        {
            FieldInfo field = AccessTools.Field(type, name) ?? throw new MissingFieldException(type.FullName, name);
            var instance = Expression.Parameter(typeof(object), "instance");
            return Expression.Lambda<Func<object, T>>(Expression.Convert(Expression.Field(Expression.Convert(instance, type), field), typeof(T)), instance).Compile();
        }

        private static Func<object, T> PropertyReader<T>(Type type, string name)
        {
            PropertyInfo property = AccessTools.Property(type, name) ?? throw new MissingMemberException(type.FullName, name);
            var instance = Expression.Parameter(typeof(object), "instance");
            return Expression.Lambda<Func<object, T>>(Expression.Convert(Expression.Property(Expression.Convert(instance, type), property), typeof(T)), instance).Compile();
        }

        private static Action<object> InstanceCommand(MethodInfo method)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            return Expression.Lambda<Action<object>>(Expression.Call(Expression.Convert(instance, method.DeclaringType), method), instance).Compile();
        }
    }
}
