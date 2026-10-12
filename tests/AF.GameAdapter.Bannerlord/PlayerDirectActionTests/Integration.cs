using AnimusForge;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

internal static class Integration
{
    internal static void Run(Action<bool,string> check)
    {
        foreach(var stock in new[]{0,5,20}) {
            var giver=new Hero{Gold=stock};Hero.MainHero.Gold=0;
            int result=RewardSystemBehavior.Instance.TransferGold(giver,Hero.MainHero,10,true);
            check(result==10 && Hero.MainHero.Gold==10 && giver.Gold==Math.Max(0,stock-10),"complete normal gold owner: sufficient/shortfall/zero stock");
        }
        var ordinaryGiver=new Hero{Gold=5};var ordinaryReceiver=new Hero();
        check(RewardSystemBehavior.Instance.TransferGold(ordinaryGiver,ordinaryReceiver,10,true)==5 && ordinaryReceiver.Gold==5,"forceComplete still NPC-to-player only");
        check(RewardSystemBehavior.Instance.TransferGold(null,Hero.MainHero,10,true)==0,"normal gold invalid giver unchanged");
        void Reset() {
            Campaign.Current=new(); Mission.Current=new(){Mode=TaleWorlds.Core.MissionMode.StartUp}; Settlement.CurrentSettlement=new(){IsTown=true};
            ShoutBehavior.Target=new(); ShoutBehavior.TargetCharacter=new(){HeroObject=ShoutBehavior.Target};
            Mission.Current.Agents.Add(new(){Index=7,Character=ShoutBehavior.TargetCharacter});
            ShoutBehavior.CurrentInstance=new(); ShoutBehavior.MainThread=true; ShoutBehavior.Busy=false; RewardSystemBehavior.Instance=new();
        }
        void Select(object id) {var m=(MultiSelectionInquiryData)InformationManager.Popup;m.AffirmativeAction(new(){m.Elements.Single(e=>Equals(e.Identifier,id))});}
        void StartMoney() { check(ShoutBehavior.OpenNativePlayerDirectActions(()=>{}),"native direct without API setup"); Select(PlayerDirectActionKind.Gold); }
        Reset(); StartMoney(); var money=(TextInquiryData)InformationManager.Popup; money.AffirmativeAction("100000");
        check(ShoutBehavior.CurrentInstance._nativeGameEffects.Tags.Single()=="[ACTION:GIVE_ASSET:GOLD:100000]","normal executor receives exact tag only");
        foreach (var mutation in new Action[]{()=>Campaign.Current=new(),()=>Mission.Current=new(),()=>Campaign.Current.ConversationManager.ActiveToken++,
            ()=>SaveRuntimeGuard.Generation++,()=>ShoutBehavior.CurrentInstance.NextEpoch(),()=>ShoutBehavior.CurrentInstance=new(),
            ()=>ShoutBehavior.TargetCharacter=new(){HeroObject=ShoutBehavior.Target},()=>Mission.Current.Agents[0].Active=false,
            ()=>Mission.Current.Agents[0]=new(){Index=7,Character=ShoutBehavior.TargetCharacter},()=>Mission.Current.IsMissionEnding=true,
            ()=>Campaign.Current.ConversationManager.IsConversationInProgress=false,()=>ShoutBehavior.MainThread=false}) {
            Reset(); var owner=ShoutBehavior.CurrentInstance; StartMoney(); money=(TextInquiryData)InformationManager.Popup; mutation();money.AffirmativeAction("10");
            check(owner._nativeGameEffects.Tags.Count==0,"stale campaign/mission/token/generation/epoch/owner/target/agent/main thread denies");
        }
        Reset(); ShoutBehavior.Busy=true; check(!ShoutBehavior.OpenNativePlayerDirectActions(()=>{}),"busy native admission rejected");
        Reset(); check(ShoutBehavior.OpenNativePlayerDirectActions(()=>{}),"native follow open");Select(PlayerDirectActionKind.Follow);
        check(ShoutBehavior.CurrentInstance.QueueCalls==1 && ShoutBehavior.CurrentInstance.CloseCalls==1 && ShoutBehavior.CurrentInstance._sceneMovement.Calls==0,"native follow queue then close, not synchronous movement");
        Reset(); Campaign.Current.ConversationManager.IsConversationInProgress=false;
        var agent=Mission.Current.Agents[0];var npc=new ShoutBehavior.NpcDataPacket{AgentIndex=7,Name="NPC"};int releases=0;
        check(ShoutBehavior.CurrentInstance.OpenScenePlayerDirectActions(npc,agent,()=>releases++),"scene primary open");Select(PlayerDirectActionKind.Follow);
        check(releases==1 && ShoutBehavior.CurrentInstance._sceneMovement.Calls==1 && ShoutBehavior.CurrentInstance.QueueCalls==0,"scene releases controls then executes existing movement");
        Mission.Current.Agents[0]=new(){Index=7,Character=ShoutBehavior.TargetCharacter};
        check(!ShoutBehavior.CurrentInstance.OpenScenePlayerDirectActions(npc,agent,()=>{}),"Y menu captured reference prevents index reuse before child popup");
        Reset(); var itemObject=new object();RewardSystemBehavior.Instance.Items.Add(new(){Item=itemObject,StringId="grain",Name="grain",Count=2});
        check(ShoutBehavior.OpenNativePlayerDirectActions(()=>{}),"item open");Select(PlayerDirectActionKind.Item);Select("grain");Select("quantity");RewardSystemBehavior.Instance.Items.Clear();
        ((TextInquiryData)InformationManager.Popup).AffirmativeAction("100");check(ShoutBehavior.CurrentInstance._nativeGameEffects.Tags.Count==0,"removed candidate denied, no generated RP fallback");
        Reset(); RewardSystemBehavior.Instance.Items.Add(new(){Item=new(),StringId="grain",Name="grain",Count=2});
        ShoutBehavior.OpenNativePlayerDirectActions(()=>{});Select(PlayerDirectActionKind.Item);Select("grain");Select("quantity");((TextInquiryData)InformationManager.Popup).AffirmativeAction("100");
        check(ShoutBehavior.CurrentInstance._nativeGameEffects.Tags.Single()=="[ACTION:GIVE_ASSET:grain:100]","quantity not capped to stock; normal owner retains supplement");
        Reset(); ShoutBehavior.OpenNativePlayerDirectActions(()=>{});Select(PlayerDirectActionKind.Debt);
        ((TextInquiryData)InformationManager.Popup).AffirmativeAction("500");((TextInquiryData)InformationManager.Popup).AffirmativeAction("0");((TextInquiryData)InformationManager.Popup).AffirmativeAction("承诺");
        check(ShoutBehavior.CurrentInstance._nativeGameEffects.Tags.Single()=="[AD:500:0:P:承诺]","zero deadline reaches normal owner unchanged");
        Reset(); RewardSystemBehavior.Instance.Items.Add(new(){Item=new(),StringId="grain",Name="grain",Count=2});
        ShoutBehavior.OpenNativePlayerDirectActions(()=>{});Select(PlayerDirectActionKind.Item);Select("grain");Select("ALL");
        check(ShoutBehavior.CurrentInstance._nativeGameEffects.Tags.Single()=="[ACTION:GIVE_ASSET:grain:ALL]","single-item ALL button distinct from all ordinary items");
        Reset();RewardSystemBehavior.Instance.Items.AddRange(new[]{new RewardSystemBehavior.RewardItemInfo{Item=new(),StringId="grain",Name="grain",Count=2},
            new RewardSystemBehavior.RewardItemInfo{Item=new(),StringId="fish",Name="fish",Count=3},new RewardSystemBehavior.RewardItemInfo{Item=new(),StringId="private",Name="private",Count=1,IsPrivateEquipment=true}});
        ShoutBehavior.OpenNativePlayerDirectActions(()=>{});Select(PlayerDirectActionKind.Item);Select("ALL");
        check(ShoutBehavior.CurrentInstance._nativeGameEffects.Tags.Single()=="[ACTION:GIVE_ASSET:grain:ALL] [ACTION:GIVE_ASSET:fish:ALL]","bulk ordinary items excludes private equipment and preserves original per-item ALL semantics");
        Reset(); var source=new object();var troop=new CharacterObject();
        PartyAssetTransferBannerlordAdapter.Candidates=new(){IndexedTroops=new(){new(){PromptIndex=1,Character=troop,Count=3,OwnerParty=source,DisplayName="troop"}}};
        ShoutBehavior.OpenNativePlayerDirectActions(()=>{});Select(PlayerDirectActionKind.Troops);Select("1");Select("quantity");
        PartyAssetTransferBannerlordAdapter.Candidates.IndexedTroops[0]=new(){PromptIndex=1,Character=new(),Count=3,OwnerParty=source};
        ((TextInquiryData)InformationManager.Popup).AffirmativeAction("2");
        check(ShoutBehavior.CurrentInstance._nativeGameEffects.Tags.Count==0,"renumbered roster cannot redirect original selection");
        Reset();PartyAssetTransferBannerlordAdapter.Candidates=new(){AllTroops=new(){new(){PromptIndex=1,Character=troop,Count=3,OwnerParty=source,DisplayName="troop"}}};
        ShoutBehavior.OpenNativePlayerDirectActions(()=>{});Select(PlayerDirectActionKind.Troops);
        PartyAssetTransferBannerlordAdapter.Candidates.AllTroops[0].OwnerParty=new();Select("ALL");
        check(ShoutBehavior.CurrentInstance._nativeGameEffects.Tags.Count==0,"ALL cannot switch source party while popup is open");
        Reset();RewardSystemBehavior.Instance.Items.Add(new(){Item=new(),StringId="private",Name="private",Count=1,IsPrivateEquipment=true});
        ShoutBehavior.OpenNativePlayerDirectActions(()=>{});Select(PlayerDirectActionKind.Item);
        check(!((MultiSelectionInquiryData)InformationManager.Popup).Elements.Any(x=>Equals(x.Identifier,"ALL")),"ordinary ALL not offered for private-equipment-only candidates");
        Reset(); RewardSystemBehavior.Instance.Debts.Add(new(){DebtId="debt_7",RemainingAmount=7,DebtNote="note",IsDueUnlimited=true});
        ShoutBehavior.OpenNativePlayerDirectActions(()=>{});Select(PlayerDirectActionKind.ResolveDebt);Select("debt_7");
        check(ShoutBehavior.CurrentInstance._nativeGameEffects.Tags.Single()=="[ADP:debt_7]","actual current debt id to owner");
        Reset(); var asset=new MyBehavior.SettlementTransferPromptEntry{AssetId="town_7",DisplayName="town",OwnerClan=new()};RewardSystemBehavior.Instance.Assets.Add(asset);
        ShoutBehavior.OpenNativePlayerDirectActions(()=>{});Select(PlayerDirectActionKind.FixedAsset);Select("town_7");
        RewardSystemBehavior.Instance.Assets[0]=new(){AssetId="town_7",OwnerClan=new()};((InquiryData)InformationManager.Popup).AffirmativeAction();
        check(ShoutBehavior.CurrentInstance._nativeGameEffects.Tags.Count==0,"asset ownership changed during confirmation rejects");

        void PresentationSession()
        {
            Reset(); Campaign.Current.ConversationManager.IsConversationInProgress = false;
            var p = ShoutBehavior.CurrentInstance.Presentation;
            p.Active = true; p._presentationAddresseeIndex = 7;
            p.Members.Add(new() { Agent = Mission.Current.Agents[0], Character = ShoutBehavior.TargetCharacter, AgentIndex = 7, Name = "Headman" });
            var other = new Agent { Index = 9, Character = new CharacterObject { HeroObject = new Hero() } };
            Mission.Current.Agents.Add(other);
            p.Members.Add(new() { Agent = other, Character = other.Character, AgentIndex = 9, Name = "Bystander" });
        }
        PresentationSession(); int finishes = 0;
        check(ShoutBehavior.OpenScenePresentationPlayerDirectActionsForExternal(() => finishes++), "persistent panel opens manual tags without API setup");
        var panelMenu = (MultiSelectionInquiryData)InformationManager.Popup;
        check(panelMenu.Title.EndsWith("Headman") && panelMenu.Elements.Count == 20, "manual-tag menu bound to selected headman's nameplate, not audience");
        Select(PlayerDirectActionKind.Gold); money = (TextInquiryData)InformationManager.Popup;
        check(money.Text.Contains("[ACTION:GIVE_ASSET:GOLD:<amount>]"), "money parameter view displays the selected tag");
        money.AffirmativeAction("23"); money.AffirmativeAction("23");
        check(finishes == 1 && ShoutBehavior.CurrentInstance._nativeGameEffects.Tags.SequenceEqual(new[] {"[ACTION:GIVE_ASSET:GOLD:23]"}), "persistent panel confirmation consumed once");
        check(ReferenceEquals(ShoutBehavior.CurrentInstance._nativeGameEffects.LastHero, ShoutBehavior.Target), "money applied only to selected headman, not bystander");
        foreach (var mutate in new Action[] {
            () => ShoutBehavior.CurrentInstance.Presentation._presentationAddresseeIndex = 9,
            () => ShoutBehavior.CurrentInstance.Presentation.Active = false,
            () => ShoutBehavior.CurrentInstance.Presentation.Members.Clear(),
            () => { var p = ShoutBehavior.CurrentInstance.Presentation; p.Members[0] = new() { Agent = p.Members[0].Agent, Character = p.Members[0].Character, AgentIndex = 7 }; },
            () => ShoutBehavior.CurrentInstance.Presentation.Members[0].InRange = false,
            () => ShoutBehavior.Busy = true,
            () => Mission.Current.Agents[0].Active = false,
            () => Mission.Current = new(), () => SaveRuntimeGuard.Generation++, () => ShoutBehavior.CurrentInstance.NextEpoch()
        })
        {
            PresentationSession(); var owner = ShoutBehavior.CurrentInstance;
            check(ShoutBehavior.OpenScenePresentationPlayerDirectActionsForExternal(() => {}), "persistent selected-target popup starts");
            Select(PlayerDirectActionKind.Gold); money = (TextInquiryData)InformationManager.Popup;
            mutate(); money.AffirmativeAction("23");
            check(owner._nativeGameEffects.Tags.Count == 0, "changed target/member/session/range/busy/agent/mission/generation/epoch denies persistent tag");
        }
        PresentationSession(); ShoutBehavior.Busy = true;
        check(!ShoutBehavior.OpenScenePresentationPlayerDirectActionsForExternal(() => {}), "pending reply cannot race manual tag");
        PresentationSession(); ShoutBehavior.CurrentInstance.Presentation._presentationAddresseeIndex = 99;
        check(!ShoutBehavior.OpenScenePresentationPlayerDirectActionsForExternal(() => {}), "missing selected nameplate never substitutes another NPC");
        PresentationSession(); finishes = 0;
        ShoutBehavior.OpenScenePresentationPlayerDirectActionsForExternal(() => finishes++);
        ((MultiSelectionInquiryData)InformationManager.Popup).NegativeAction(null);
        check(finishes == 1 && ShoutBehavior.CurrentInstance._nativeGameEffects.Tags.Count == 0, "persistent cancel restores UI without tag dispatch");
        PresentationSession(); ShoutBehavior.OpenScenePresentationPlayerDirectActionsForExternal(() => {}); Select(PlayerDirectActionKind.Follow);
        check(ShoutBehavior.CurrentInstance.MovementReleases == 1 && ShoutBehavior.CurrentInstance._sceneMovement.Calls == 1,
            "persistent follow releases conversation movement suppression before original command");
    }
}
