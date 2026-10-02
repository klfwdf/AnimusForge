using AnimusForge;
using TaleWorlds.CampaignSystem;

internal static class PoliticalBoundaryReplay
{
    internal static void Run(Action<bool,string> check)
    {
        foreach (var kind in new[] { DiplomacyPoliticalRewardKind.Vassalage, DiplomacyPoliticalRewardKind.Annexation })
        foreach (bool eligible in new[] { true, false })
        foreach (bool applied in new[] { true, false })
        {
            var port = new Port { Eligible = eligible, Applied = applied };
            string valid = kind == DiplomacyPoliticalRewardKind.Vassalage ? "[ACTION:VASSALAGE:SUBMIT:VASSAL:k]" : "[ACTION:KINGDOM_ANNEX:target_kingdom_id=k]";
            string invalid = kind == DiplomacyPoliticalRewardKind.Vassalage ? "[ACTION:VASSALAGE:BAD]" : "[ACTION:KINGDOM_ANNEX:BAD]";
            string text = "a" + invalid + valid + "b";
            var giver = new List<string>(); var receiver = new List<string>();
            bool result = DiplomacyPoliticalRewardApplication.Apply(port, kind, ref text, giver, receiver);
            check(text == "ab", "supported and unsupported political tags are stripped for every role/result");
            check(result == (eligible && applied), "political success requires eligible applied nonblank receipt");
            check(giver.SequenceEqual(receiver) && giver.Count == (eligible ? 2 : 0), "facts are appended once to each existing writer");
            check(port.Events[0] == "matched" && port.Events.IndexOf("unsupported") > port.Events.IndexOf("matched"), "supported grammar is processed before unsupported regardless of text order");
            check(port.Actions == (eligible ? 1 : 0), "ineligible political tags cannot invoke capabilities");
            if (eligible) check(port.Events.IndexOf("applied") < port.Events.IndexOf("show"), "applied diagnostics precede fact presentation");
        }
        foreach (var kind in new[] { DiplomacyPoliticalRewardKind.Vassalage, DiplomacyPoliticalRewardKind.Annexation })
        {
            var port = new Port { Eligible = true, Applied = true, Status = "" };
            string text = kind == DiplomacyPoliticalRewardKind.Vassalage ? "[action:vassalage:submit:military:k-1]" : "[action:kingdom_annex:target_kingdom_id=k-1]";
            check(!DiplomacyPoliticalRewardApplication.Apply(port,kind,ref text,new(),new()) && text=="", "empty status preserves old false result even if capability applied");
        }
        var peace = new DiplomacyRecentPeaceApplication(); var now = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc);
        peace.Register(" a ", "B", now);
        check(peace.ShouldBlock("b","A",now.AddSeconds(45)), "recent peace includes exactly 45 seconds with symmetric normalized IDs");
        check(peace.ShouldBlock("A","b",now.AddSeconds(-10)), "clock rewind preserves active recent peace");
        check(!peace.ShouldBlock("A","b",now.AddSeconds(45).AddTicks(1)), "recent peace expires beyond boundary");
        peace.Register("a","b",now); peace.Register("B","A",now.AddSeconds(40));
        check(peace.ShouldBlock("a","b",now.AddSeconds(80)), "registration refreshes the same pair");
        peace.Register("a","a",now); peace.Register(null,"b",now);
        check(!peace.ShouldBlock("a","a",now) && !peace.ShouldBlock(null,"b",now), "invalid peace identities cannot block hostility");
        peace.Register("x","y",now.AddSeconds(100));
        check(!peace.ShouldBlock("a","b",now.AddSeconds(100)), "registration sweep evicts expired pairs");
        var giverHero = new Hero { StringId="npc" }; var receiverHero = new Hero { StringId="player" };
        Recording.Result=true;
        string both="[ACTION:VASSALAGE:SUBMIT:VASSAL:k][ACTION:KINGDOM_ANNEX:target_kingdom_id=k]";
        var facts=new List<string>(); var receiverFacts=new List<string>();
        check(DiplomacyConversationBridge.ApplyVassalageRewardTags(giverHero,receiverHero,ref both,facts,receiverFacts),"real bridge enters vassalage Application");
        check(Recording.Method=="political" && (string)Recording.Args[0]=="npc" && (string)Recording.Args[1]=="player", "political bridge maps stable actor IDs");
        check(DiplomacyConversationBridge.ApplyKingdomAnnexationRewardTags(giverHero,receiverHero,ref both,facts,receiverFacts) && both=="" && facts.Count==2,"two political flags and ordering remain independent");
        Action close=()=>{};
        check(DiplomacyPresentationBridge.OpenComposeFromTerminal(close) && ReferenceEquals(Recording.Args[0],close),"compose callback crosses bridge unchanged");
        check(DiplomacyPresentationBridge.ShowRoyalAnnouncementArchive(close) && ReferenceEquals(Recording.Args[0],close),"archive callback crosses bridge unchanged");
        check(DiplomacyPresentationBridge.IsOpen,"popup state crosses module boundary");
        WorldDiplomacyBehavior.Available=true; WorldDiplomacyBehavior.Applied=true;
        check(DiplomacyPresentationBridge.MarkDocumentRead(" Diplomacy:x "),"UI command preserves trim and case-insensitive timeline prefix");
        check(!DiplomacyPresentationBridge.MarkDocumentRead("diplomacy:"),"prefix-only UI identity is rejected");
        Recording.Failure=new InvalidOperationException("read failure");
        check(!DiplomacyPresentationBridge.MarkDocumentRead("diplomacy:x"),"UI read failure returns false");
        check(DiplomacyPresentationBridge.GetRecentDocumentsOrEmpty(10).Count==0,"UI query failure returns empty snapshot");
        Recording.Failure=null;
    }
    private sealed class Port : IDiplomacyPoliticalRewardPort
    {
        internal bool Eligible,Applied; internal string Status="receipt"; internal int Actions;
        internal List<string> Events=new();
        public bool GiverIsPlayer=>false; public bool ReceiverIsPlayer=>Eligible;
        public DiplomacyPoliticalRewardReceipt Apply(DiplomacyPoliticalRewardKind kind,string type,string target)
        { Actions++; Events.Add("effect"); return new(true,Applied,Status); }
        public void Record(DiplomacyPoliticalRewardKind kind,string stage,string tag,string type,string target,bool applied,string status)=>Events.Add(stage);
        public void Show(DiplomacyPoliticalRewardKind kind,bool applied,string status)=>Events.Add("show");
        public void Unsupported(DiplomacyPoliticalRewardKind kind,string tag)=>Events.Add("unsupported-log");
    }
}
