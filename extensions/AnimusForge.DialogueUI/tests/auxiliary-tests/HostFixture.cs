// Managed host fixture only. Tests execute the real bridge and real Harmony patches, not the game renderer.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
namespace TaleWorlds.Core
{
    public sealed class InquiryElement
    {
        public readonly object Identifier; public readonly string Title; public readonly bool IsEnabled;
        public InquiryElement(object id, string title, bool enabled = true) { Identifier=id; Title=title; IsEnabled=enabled; }
    }
    public sealed class MultiSelectionInquiryData
    {
        public List<InquiryElement> InquiryElements; public int MaxSelectableOptionCount;
        public Action<List<InquiryElement>> AffirmativeAction, NegativeAction;
    }
    public static class MBInformationManager
    {
        public static int NativeInquiries;
        [MethodImpl(MethodImplOptions.NoInlining)] public static void ShowMultiSelectionInquiry(MultiSelectionInquiryData data) { NativeInquiries++; }
    }
}
namespace TaleWorlds.CampaignSystem
{
    public sealed class Campaign
    {
        public static Campaign Current;
        public AnimusForge.ShoutBehavior Behavior = new();
        public T GetCampaignBehavior<T>() where T:class => Behavior as T;
    }
}
namespace AnimusForge.DialogueUI
{
    internal static class DialogueUiRuntime { internal static void Log(string text) => Console.WriteLine(text); }
}
namespace AnimusForge
{
    using TaleWorlds.Core; using TaleWorlds.CampaignSystem;
    public static class MyBehavior
    {
        public sealed class PartyTransferPromptEntry { public int HirePriceDenarsPerUnit,BuyPriceDenarsPerUnit; }
        public sealed class SettlementTransferPromptEntry { public int GuidePriceDenars; public string TypeLabel; }
    }
    public sealed class ShoutBehavior
    {
        internal sealed class ShoutTradeResourceOption
        {
            public string Name,ItemId; public int AvailableAmount,InventoryUnitValue; public bool IsGold;
            public MyBehavior.PartyTransferPromptEntry PartyEntry;
            public MyBehavior.SettlementTransferPromptEntry SettlementEntry;
        }
        internal sealed class ShoutPendingTradeItem
        {
            public int Amount; public bool IsGold; public string ItemId;
            public MyBehavior.PartyTransferPromptEntry PartyEntry;
            public MyBehavior.SettlementTransferPromptEntry SettlementEntry;
        }
        internal readonly SceneTradeController _j17SceneTradeController = new();
        public static bool OpenNativeConversationGiveShowForExternal(Action done)
        {
            Campaign.Current.Behavior._j17SceneTradeController.OpenMenu(done);
            return true;
        }
    }
    internal sealed class SceneTradeController
    {
        private List<ShoutBehavior.ShoutTradeResourceOption> _shoutTradeOptions = new();
        private List<ShoutBehavior.ShoutPendingTradeItem> _shoutPendingTradeItems = new();
        private int _shoutPendingTradeItemIndex;
        private bool _shoutTradeActionOnly;
        private Action _shoutTradeActionOnlyFinished;
        internal Func<MultiSelectionInquiryData, bool> InlineInquiryCapture;
        private long _uiRevision;
        private void PresentTradeInquiry(MultiSelectionInquiryData data)
        {
            if (InlineInquiryCapture?.Invoke(data) == true) return;
            MBInformationManager.ShowMultiSelectionInquiry(data);
        }
        public int Commits,Cancels,NativeAmounts; public bool InjectUnrelated,CorruptOrder,RefuseCategory,ThrowBeforeCommit;
        public string Mode; public int[] LastAmounts; public string[] LastIds; public Action DuringCommit;
        internal void OpenMenu(Action done)
        {
            var self=this;
            long revision = ++_uiRevision;
            self._shoutTradeActionOnly=true; self._shoutTradeActionOnlyFinished=done;
            PresentTradeInquiry(new MultiSelectionInquiryData {
                MaxSelectableOptionCount=1,
                InquiryElements=new[] {"give","show","give_troops","give_prisoners","give_settlements"}.Select(m=>new InquiryElement(m,m)).ToList(),
                AffirmativeAction=selected=>{ if(revision==_uiRevision) self.Begin((string)selected[0].Identifier); }, NegativeAction=_=>{ if(revision==_uiRevision) self.CancelHost(); } });
        }
        private void Begin(string mode)
        {
            Mode=mode;
            if (RefuseCategory) { CancelHost(); return; }
            if (InjectUnrelated) MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData {
                InquiryElements=new(){new InquiryElement("other","other")},MaxSelectableOptionCount=1,AffirmativeAction=_=>{} });
            _shoutTradeOptions=new() {
                new(){Name="gold",ItemId="",IsGold=true,AvailableAmount=100,InventoryUnitValue=1},
                new(){Name="grain",ItemId="grain",AvailableAmount=20,InventoryUnitValue=3} };
            if(mode=="give_settlements") foreach(var option in _shoutTradeOptions)
            { option.AvailableAmount=1; option.SettlementEntry=new(){GuidePriceDenars=1000,TypeLabel="asset"}; }
            if(mode=="give_troops" || mode=="give_prisoners") foreach(var option in _shoutTradeOptions)
                option.PartyEntry=new(){HirePriceDenarsPerUnit=100,BuyPriceDenarsPerUnit=200};
            long revision = _uiRevision;
            PresentTradeInquiry(new MultiSelectionInquiryData {
                MaxSelectableOptionCount=2,InquiryElements=new(){new InquiryElement(0,"gold"),new InquiryElement(1,"grain")},
                AffirmativeAction=selected=>{ if(revision==_uiRevision) OnShoutTradeResourcesSelected(selected); },NegativeAction=_=>{ if(revision==_uiRevision) CancelHost(); } });
        }
        private void OnShoutTradeResourcesSelected(List<InquiryElement> selected)
        {
            _shoutPendingTradeItems=selected.Select(e=>_shoutTradeOptions[(int)e.Identifier]).Select(o=>new ShoutBehavior.ShoutPendingTradeItem {
                IsGold=o.IsGold,ItemId=o.ItemId,PartyEntry=o.PartyEntry,SettlementEntry=o.SettlementEntry,Amount=o.SettlementEntry==null?0:1 }).ToList();
            if(ThrowBeforeCommit) throw new InvalidOperationException("fixture commit error");
            if(CorruptOrder) _shoutPendingTradeItems.Reverse();
            if(Mode=="give_settlements") CommitShoutTradeActionOnly(); else ShowShoutTradeAmountInquiry();
        }
        [MethodImpl(MethodImplOptions.NoInlining)] private void ShowShoutTradeAmountInquiry() { NativeAmounts++; }
        private void CommitShoutTradeActionOnly()
        {
            DuringCommit?.Invoke(); Commits++;
            LastAmounts=_shoutPendingTradeItems.Select(p=>p.Amount).ToArray();
            LastIds=_shoutPendingTradeItems.Select(p=>p.ItemId).ToArray(); Finish();
        }
        private void CancelHost() { Cancels++; Finish(); }
        private void Finish() { var done=_shoutTradeActionOnlyFinished; _shoutTradeActionOnlyFinished=null; _shoutTradeActionOnly=false; done?.Invoke(); }
        public void ReplaceOptions() => _shoutTradeOptions=new();
        public void SupersedeOwner() { _shoutTradeActionOnlyFinished=()=>{}; }
        public void InvalidateUi() { _uiRevision++; }
    }
}
