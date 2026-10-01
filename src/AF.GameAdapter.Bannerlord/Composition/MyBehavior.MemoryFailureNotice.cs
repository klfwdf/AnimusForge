using System;using TaleWorlds.Library;using TaleWorlds.CampaignSystem;
namespace AnimusForge;
public partial class MyBehavior {
 private readonly MemoryFailureNoticeOwner _memoryFailureNotices = new MemoryFailureNoticeOwner();
 private MemoryFailureNoticePort _memoryFailureNoticePort;
 private MemoryFailureNoticeOwner MemoryFailureNotices {get { _memoryFailureNotices.Bind(_memoryFailureNoticePort ??= new MemoryFailureNoticePort {IsCurrentOwner=IsCurrentMemoryFailureOwner,IsCurrentCampaign=IsCurrentMemoryFailureCampaign,IsMainThread=TWParallel.IsMainThread,Observe=ObserveMemoryFailureNotice,Show=(title,message,complete)=>InformationManager.ShowInquiry(new InquiryData(title,message,isAffirmativeOptionShown:true,isNegativeOptionShown:false,"知道了","",complete,null),pauseGameActiveState:true)});return _memoryFailureNotices;}}
private bool IsCurrentMemoryFailureOwner(long generation)
    {
        return ReferenceEquals(Instance, this) && SaveRuntimeGuard.IsCurrentGeneration(generation);
    }
private bool IsCurrentMemoryFailureCampaign()
    {
        try { return ReferenceEquals(Campaign.Current?.GetCampaignBehavior<MyBehavior>(), this); }
        catch (Exception) { return false; }
    }
private static void ObserveMemoryFailureNotice(string title, string message)
    {
        try { Logger.Log("CompressedMemory", "[BLOCK] " + title + " :: " + (message ?? "")); }
        catch (Exception)
        {
            // Optional diagnostics must not hide an actionable memory failure.
            return;
        }
    }
 private void PublishMemoryFailureNotice(string title,string message,long generation)=>MemoryFailureNotices.PublishMemoryFailureNotice(title,message,generation);
 private void ProcessPendingMemoryFailureNotice()=>MemoryFailureNotices.ProcessPendingMemoryFailureNotice();
 private void CompleteMemoryFailureNotice(long generation,long revision)=>MemoryFailureNotices.CompleteMemoryFailureNotice(generation,revision);
 private void ResetMemoryFailureNotices()=>MemoryFailureNotices.ResetMemoryFailureNotices();
}
