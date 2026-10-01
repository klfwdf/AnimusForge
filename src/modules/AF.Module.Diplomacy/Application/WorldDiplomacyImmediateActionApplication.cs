using System;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal readonly struct WorldDiplomacyImmediateActionReceipt
{
    internal readonly bool Known;
    internal readonly bool Applied;
    internal readonly string Message;
    internal readonly string Diagnostic;
    internal WorldDiplomacyImmediateActionReceipt(bool applied, string message, string diagnostic = null, bool known = true)
    {
        Known = known;
        Applied = known && applied;
        Message = message ?? "";
        Diagnostic = diagnostic;
    }
}

internal interface IWorldDiplomacyImmediateActionPort
{
    WorldDiplomacyStorage Storage { get; }
    int CurrentDay { get; }
    bool CanAiAuthor(string authorId, out string reason);
    WorldDiplomacyImmediateActionReceipt DeclareWar(string authorId, string targetId, WorldDiplomacyDocument document);
    WorldDiplomacyImmediateActionReceipt BreakAlliance(string authorId, string targetId, WorldDiplomacyDocument document);
    WorldDiplomacyImmediateActionReceipt CancelTrade(string authorId, string targetId, WorldDiplomacyDocument document);
    void Log(string message);
}

internal static class WorldDiplomacyImmediateActionApplication
{
    internal static void Execute(
        IWorldDiplomacyImmediateActionPort port,
        string authorId, string targetId, string intent, WorldDiplomacyDocument document)
    {
        if (document != null && !document.IsPlayerAuthored && !port.CanAiAuthor(authorId, out string reason))
        {
            document.MechanicalResult = "外交行动未执行：发文者当前没有有效的自主发文权限。";
            port.Log("AI diplomatic action blocked author=" + (authorId ?? "") + " document=" + (document.DocumentId ?? "") + " reason=" + reason);
            return;
        }
        WorldDiplomacyImmediateActionReceipt receipt = intent switch
        {
            "declare_war" => port.DeclareWar(authorId, targetId, document),
            "break_alliance" => port.BreakAlliance(authorId, targetId, document),
            "cancel_trade" => port.CancelTrade(authorId, targetId, document),
            _ => default
        };
        if (document == null || receipt.Message == null) return;
        document.MechanicalResult = receipt.Message;
        if (receipt.Applied) document.ChangedDiplomaticState = true;
        if (receipt.Applied && intent == "declare_war")
        {
            WorldDiplomacyWarPressureRules.ClearWarPressure(port.Storage?.WarPressure, authorId, targetId, port.CurrentDay);
            port.Storage.LastOffensiveWarDayByKingdom[authorId] = port.CurrentDay;
        }
        if (receipt.Diagnostic != null) port.Log(receipt.Diagnostic);
    }
}
