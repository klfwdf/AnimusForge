using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RichExecutions.Core;
using RichExecutions.Scene;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal static class PublicExecutionOrderRuntime
{
    internal sealed class Permit
    {
        internal readonly string Token = Guid.NewGuid().ToString("N");
        internal Mission Mission;
        internal TownExecutionMissionBehavior Controller;
        internal Agent Agent;
        internal Guid Session;
        internal long Generation;
        internal bool Native;
        internal DateTime Expires = DateTime.UtcNow.AddMinutes(3);
    }
    private static readonly object Gate = new object();
    private static readonly Dictionary<string, Permit> Permits = new Dictionary<string, Permit>();
    private static ConversationManager _manager;
    private static Action _handler;
    private static readonly Regex Tags = new Regex(@"\[ACTION:PUBLIC_EXECUTION_START(?::([a-fA-F0-9]{32}))?\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    internal static bool IsEligible(int agentIndex)
    {
        var mission = Mission.Current;
        var controller = mission?.GetMissionBehavior<TownExecutionMissionBehavior>();
        var agent = mission?.Agents?.FirstOrDefault(a => a.Index == agentIndex && a.IsActive());
        return controller?.Request != null && controller.State == ExecutionSessionState.WaitingForPlayer
            && agent != null && controller.IsCeremonyExecutioner(agent);
    }
    internal static Permit Capture(int agentIndex, string chain, bool selected, bool direct, string playerText, string npcReply = null)
    {
        if (!selected || !direct || (chain ?? "").IndexOf("courier", StringComparison.OrdinalIgnoreCase) >= 0 ||
            !PublicExecutionOrderPolicy.AllowsImmediateOrder(playerText) || PublicExecutionOrderPolicy.ReplyRefusesImmediateOrder(npcReply) || !IsEligible(agentIndex)) return null;
        var mission = Mission.Current;
        var controller = mission.GetMissionBehavior<TownExecutionMissionBehavior>();
        return new Permit { Mission = mission, Controller = controller,
            Agent = mission.Agents.First(a => a.Index == agentIndex), Session = controller.Request.SessionId,
            Generation = SaveRuntimeGuard.CurrentGeneration, Native = chain == "native_conversation" || chain == "meeting" };
    }
    internal static string Normalize(Permit permit, string raw)
    {
        if (permit == null || (raw ?? "").IndexOf(PublicExecutionOrderPolicy.Tag, StringComparison.OrdinalIgnoreCase) < 0) return "";
        lock (Gate)
        {
            foreach (var key in Permits.Where(x => x.Value.Expires < DateTime.UtcNow ||
                !SaveRuntimeGuard.IsCurrentGeneration(x.Value.Generation)).Select(x => x.Key).ToArray()) Permits.Remove(key);
            if (Permits.Count >= 128) return "";
            Permits[permit.Token] = permit;
        }
        // Only this internal receipt carries the opaque binding; the model sees the bare tag.
        return "[ACTION:PUBLIC_EXECUTION_START:" + permit.Token + "]";
    }
    internal static bool Consume(int agentIndex, ref string text)
    {
        var matches = Tags.Matches(text ?? "");
        if (matches.Count == 0) return false;
        text = Tags.Replace(text ?? "", "").Trim();
        foreach (Match match in matches)
        {
            Permit permit;
            lock (Gate)
            {
                string token = match.Groups[1].Value;
                if (!Permits.TryGetValue(token, out permit)) continue;
                Permits.Remove(token);
            }
            if (permit.Agent.Index != agentIndex || !IsCurrent(permit)) { Log("rejected stale target/session"); continue; }
            if (permit.Native)
            {
                var manager = Campaign.Current?.ConversationManager;
                if (manager == null) { Log("rejected missing conversation"); continue; }
                ClearConversation();
                // Once accepted, the native order lasts until this conversation closes;
                // mission/session/generation checks still reject retired scenes.
                permit.Expires = DateTime.MaxValue;
                _manager = manager;
                _handler = () => { ClearConversation(); Start(permit); };
                manager.ConversationEndOneShot += _handler;
                Log("accepted order; closing executioner conversation session=" + permit.Session);
                // The confirmed order is an execution command, not a promise
                // requiring a second click on Leave. The one-shot callback
                // starts only this bound ceremony while native dialogue unwinds.
                manager.EndConversation();
            }
            else Start(permit);
        }
        return true;
    }
    private static bool IsCurrent(Permit permit) => permit.Expires >= DateTime.UtcNow &&
        SaveRuntimeGuard.IsCurrentGeneration(permit.Generation) && ReferenceEquals(Mission.Current, permit.Mission) &&
        ReferenceEquals(permit.Mission.GetMissionBehavior<TownExecutionMissionBehavior>(), permit.Controller) &&
        permit.Controller.Request.SessionId == permit.Session && permit.Agent.IsActive() &&
        permit.Controller.State == ExecutionSessionState.WaitingForPlayer && permit.Controller.IsCeremonyExecutioner(permit.Agent);
    private static void Start(Permit permit)
    {
        if (!IsCurrent(permit)) { Log("rejected before start"); return; }
        bool started = permit.Controller.TryBeginExecution(ExecutionActor.Executioner);
        Log("start session=" + permit.Session + " accepted=" + started);
        if (started)
            ShoutBehavior.AppendExternalTargetedSceneNpcFactForExternal("刽子手已接受玩家命令并开始执行本场公开处刑；此记录只确认开始行刑，不代表死亡已经结算。", permit.Agent.Index);
    }
    private static void Log(string text) => Logger.Log("PublicExecutionOrder", text);
    private static void ClearConversation()
    {
        if (_manager != null && _handler != null) _manager.ConversationEndOneShot -= _handler;
        _manager = null; _handler = null;
    }
    internal static void Reset()
    {
        ClearConversation();
        lock (Gate) Permits.Clear();
    }
}
