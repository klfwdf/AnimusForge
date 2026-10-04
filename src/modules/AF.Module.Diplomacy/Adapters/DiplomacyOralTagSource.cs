using TaleWorlds.CampaignSystem;

namespace AnimusForge;

internal readonly struct DiplomacyOralTagSource : IDiplomacyOralTagSource, IDiplomacyCommitmentTagSource
{
    private readonly Hero _npc;
    internal DiplomacyOralTagSource(Hero npc) => _npc = npc;
    public bool HasSpeaker => _npc != null;
    public bool IsAvailable => DiplomacyBehavior.Instance != null
        || Campaign.Current?.GetCampaignBehavior<DiplomacyBehavior>() != null;
    public string SpeakerHeroId => _npc.StringId;
    public string SpeakerKingdomId => _npc?.Clan?.Kingdom?.StringId ?? "";
    public string SubmitCommitment(string payload) => WorldDiplomacyBehavior.SubmitOralDiplomaticCommitment(_npc, payload);
    public string ControlCommitment(string payload) => WorldDiplomacyBehavior.ControlOralDiplomaticCommitment(_npc, payload);
    public string SubmitLegacyCommitment(string action, string payload) => WorldDiplomacyBehavior.SubmitLegacyOralCommitment(_npc, action, payload);
    public string DeclareWar(string payload)
    {
        var source = new DiplomacyOralDeclareWarSource(_npc);
        return DiplomacyOralDeclareWarApplication.Execute(ref source, payload);
    }
    public string MakePeace(string payload)
    {
        var source = new DiplomacyOralMakePeaceSource(_npc);
        return DiplomacyOralMakePeaceApplication.Execute(ref source, payload);
    }
    public string IndependentClanPeace(string payload)
    {
        var source = new DiplomacyOralIndependentPeaceSource(_npc);
        return DiplomacyOralIndependentPeaceApplication.Execute(ref source, payload);
    }
    public string FormAlliance(string payload)
    {
        var source = new DiplomacyOralFormAllianceSource(_npc);
        return DiplomacyOralFormAllianceApplication.Execute(ref source, payload);
    }
    public string BreakAlliance(string payload)
    {
        var source = new DiplomacyOralBreakAllianceSource(_npc);
        return DiplomacyOralBreakAllianceApplication.Execute(ref source, payload);
    }
    public string MakeTrade(string payload)
    {
        var source = new DiplomacyOralMakeTradeSource(_npc);
        return DiplomacyOralMakeTradeApplication.Execute(ref source, payload);
    }
    public string CancelTrade(string payload)
    {
        var source = new DiplomacyOralCancelTradeSource(_npc);
        return DiplomacyOralCancelTradeApplication.Execute(ref source, payload);
    }
    public void Log(string message) => Logger.Log("DiplomacyBehavior", message);
}
