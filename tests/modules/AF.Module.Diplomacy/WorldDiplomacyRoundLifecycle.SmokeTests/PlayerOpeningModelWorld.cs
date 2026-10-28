using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;

internal static partial class MaterialCountryThirdReplay
{
    // Coherent detached campaign leaves for provider acceptance. No fabricated
    // wars, offers or unrelated documents from the generic prompt unit fixture.
    private sealed class ModelHost : Host, IDiplomacyEventHost, IDiplomacySilenceEffectHost
    {
        public new string CaptureEventWorld(IReadOnlyCollection<string> ids) => JsonConvert.SerializeObject(ids.Select(id=>new {
            kingdom_id=id,name=PartyNameOrEmpty(id),ruler_id=PartyRulerId(id),ruler=PartyRulerName(id),towns=Array.Empty<object>(),
            ruler_relations=ids.Where(other=>other!=id).Select(other=>new{kingdom=other,value=Relation,minimum=-100,maximum=100}) }));
        public IReadOnlyList<string> KingdomTownIds(string id) => Array.Empty<string>();
        public override string PartyNameOrEmpty(string id) => id switch {
            "p" => "Western Empire", "a" => "Southern Empire", "b" => "Northern Empire", "c" => "Battania", _ => id };
        public override string PartyRulerName(string id) => id switch {
            "p" => "Garios", "a" => "Rhagaea", "b" => "Lucon", "c" => "Caladog", _ => id };
        public override IWorldDiplomacyPromptWorld PromptWorld() => new ModelPromptWorld(this);
        public override IWorldDiplomacyDraftRepairWorld DraftRepairWorld() => new ModelPromptWorld(this);
        public override bool PartiesAtWar(string first, string second) => first != second && Owner.CurrentStorage.Documents.Any(d =>
            d.Actions?.Any(a => a.ChangedDiplomaticState && a.Intent == "declare_war"
                && (d.AuthorKingdomId == first && a.TargetKingdomId == second || d.AuthorKingdomId == second && a.TargetKingdomId == first)) == true);
        string IDiplomacyEventHost.CaptureEventWorld(IReadOnlyCollection<string> ids) => JsonConvert.SerializeObject(ids.Select(id => new {
            kingdom_id=id,name=PartyNameOrEmpty(id),ruler_id=PartyRulerId(id),ruler=PartyRulerName(id),
            enemies=AllKingdomIds().Where(other=>PartiesAtWar(id,other)),towns=Array.Empty<object>() }));
    }

    private sealed class ModelPromptWorld(ModelHost host) : IWorldDiplomacyDraftRepairWorld
    {
        public string BuildGovernmentHardFact(string author) => "Sovereign ruler: " + host.PartyRulerName(author) + "; kingdom: " + host.PartyNameOrEmpty(author);
        public string NewId(string prefix) => host.NewId(prefix);
        public void Log(string text) => host.Log(text);
        public string LocalizePrompt(string id,string fallback) => DiplomacyPromptTexts.Get(id,fallback).Replace("[[AF_JSON_OPEN]]","{").Replace("[[AF_JSON_CLOSE]]","}");
        public string ResolveKingdom(string id) => host.AllKingdomIds().Contains(id) ? id : null;
        public bool IsEliminated(string id) => false;
        public bool HasIndependentWorldDiplomacyAuthority(string id) => ResolveKingdom(id)!=null;
        public IEnumerable<string> KingdomIds() => host.AllKingdomIds();
        public IReadOnlyList<string> CurrentWarKingdomIds(string id) => host.AllKingdomIds().Where(other=>IsAtWar(id,other)).ToList();
        public IReadOnlyList<string> IndependentKingdomIds() => host.AllKingdomIds();
        public bool IsAtWar(string a,string b) => host.PartiesAtWar(a,b);
        public bool IsAlly(string a,string b) => false;
        public bool HasTradeAgreement(string a,string b) => false;
        public WorldDiplomacyRound ResolveRound(string id) => host.Owner.ResolveRound(id);
        public WorldDiplomacyDocument ResolveDocument(string id) => host.Owner.ResolveDocument(id);
        public IReadOnlyList<WorldDiplomacyDocument> Documents() => host.Owner.CurrentStorage.Documents;
        public IReadOnlyList<WorldDiplomacyThreat> DiplomaticThreats() => host.Owner.CurrentStorage.DiplomaticThreats;
        public string KingdomName(string id) => host.PartyNameOrEmpty(id);
        public string RulerName(string id) => host.PartyRulerName(id);
        public int GetRoundParticipantLimit() => 8;
        public int GetActivityLevel() => 2;
        public int CurrentDay() => host.CurrentDayValue;
        public int DaysPerYear() => 84;
        public int RecentBattleRetentionDays() => 14;
        public int NegativeReputationFactRetentionDays() => 21;
        public string FormatCampaignDate(int day) => "day "+day;
        public string GetCommonDiplomacyContract(WorldDiplomacyRound round) => "";
        public string BuildWorldDiplomacyVassalageSnapshot() => "All four kingdoms are sovereign; none is a vassal.";
        public string BuildPolicySnapshot(string id) => "No relevant policy in this test world.";
        public string BuildGatheringSnapshot(IEnumerable<string> ids,int count) => "";
        public IReadOnlyList<string> CessionCandidates(string a,string b,float score) => Array.Empty<string>();
        public WarSituationSnapshot WarSituation(string a,string b) => new(){IsAtWar=IsAtWar(a,b)};
        public WorldDiplomacyRealmRelationProfile RelationProfile(string a,string b) => new();
        public WorldDiplomacyBorderRelation BorderRelation(string a,string b) => new();
        public int RulerRelation(string a,string b) => (int)host.Relation;
        public int CulturalClaimCount(string a,string b) => 0;
        public int WarPressure(string a,string b) => 0;
        public string RulerVoiceContext(string id) => id switch { "a"=>"Rhagaea defends dynastic legitimacy; pragmatic and proud.",
            "b"=>"Lucon defends senatorial legitimacy and legal tradition.","c"=>"Caladog defends Battanian independence.",_=>"Human player; never author their choices." };
        public string RealmInstitutionalVoiceContext(string id) => RulerVoiceContext(id);
        public string AuthorRulerFamilyContext(string id) => "";
        public string BilateralRulerFamilyContext(string a,string b) => "";
        public string RecentNativeSignalContext(string a,string b) => "";
        public string RecentBilateralBattleContext(string a,string b) => "";
        public WorldDiplomacyRulerCaptivity AuthorRulerCaptivity(string id) => new();
    }
}
