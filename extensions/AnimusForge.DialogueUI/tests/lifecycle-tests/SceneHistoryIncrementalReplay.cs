using AnimusForge;
using AnimusForge.DialogueUI.Scene;
internal static class SceneHistoryIncrementalReplay
{
    internal static void Run(Action<bool,string> check)
    {
        static ScenePresentationHistoryLine Line(string text,string speaker="soldier",string kind="npc") => new() {Speaker=speaker,Text=text,Kind=kind};
        ShoutBehavior.HasScenePresentationStagedTradeForExternal=false;
        ShoutBehavior.SceneHistory=new(){Line("first"),Line("middle"),Line("last")};
        var vm=new SceneSessionVM();vm.Refresh();
        var first=vm.HistoryLines[0];var middle=vm.HistoryLines[1];var last=vm.HistoryLines[2];int scroll=vm.HistoryScrollVersion;
        vm.Refresh();check(vm.HistoryScrollVersion==scroll && ReferenceEquals(first,vm.HistoryLines[0]),"unchanged history creates no rows or scroll notification");
        ShoutBehavior.SceneHistory.Add(Line("new reply"));vm.Refresh();
        check(vm.HistoryLines.Count==4 && ReferenceEquals(first,vm.HistoryLines[0]) && ReferenceEquals(middle,vm.HistoryLines[1]) && ReferenceEquals(last,vm.HistoryLines[2]),"one reply appends one row without rebuilding existing native text widgets");
        check(vm.HistoryScrollVersion==scroll+1,"one append requests one scroll update");
        ShoutBehavior.SceneHistory.RemoveAt(0);ShoutBehavior.SceneHistory.Add(Line("next reply"));vm.Refresh();
        check(ReferenceEquals(middle,vm.HistoryLines[0]) && ReferenceEquals(last,vm.HistoryLines[1]),"rolling window retains overlapping row objects");
        vm.Refresh(forceHistory:true);check(ReferenceEquals(middle,vm.HistoryLines[0]),"forced style/scroll refresh preserves row widgets");
        ShoutBehavior.SceneHistory[1]=Line("last","different speaker");vm.Refresh();
        check(vm.HistoryLines[1].Speaker=="different speaker" && ReferenceEquals(middle,vm.HistoryLines[0]),"same text from a different speaker is not hidden by old first/last-text signature");
        ShoutBehavior.SceneHistory[1]=Line("last","different speaker","fact");vm.Refresh();
        check(vm.HistoryLines[1].IsFact,"same text with changed fact role updates correctly");
        ShoutBehavior.SceneHistory.RemoveRange(1,3);vm.Refresh();
        check(vm.HistoryLines.Count==1 && ReferenceEquals(middle,vm.HistoryLines[0]),"rewrite/removal preserves unaffected earlier rows");
        ShoutBehavior.SceneHistory=new(){Line("duplicate"),Line("duplicate"),Line("tail")};vm.Refresh();
        ShoutBehavior.SceneHistory=new(){Line("duplicate"),Line("tail"),Line("fresh")};vm.Refresh();
        check(vm.HistoryLines.Select(x=>x.Text).SequenceEqual(new[]{"duplicate","tail","fresh"}),"duplicate texts cannot misalign the final history contents");
        ShoutBehavior.SceneHistory=Enumerable.Range(0,200).Select(i=>Line("old-"+i)).ToList();vm.Refresh();
        var retained=vm.HistoryLines.Skip(1).ToArray();ShoutBehavior.SceneHistory.RemoveAt(0);ShoutBehavior.SceneHistory.Add(Line("new-200"));vm.Refresh();
        check(retained.Select((row,i)=>ReferenceEquals(row,vm.HistoryLines[i])).All(x=>x),"full 200-row history retains all 199 overlapping widgets at speech append");
        ShoutBehavior.SceneHistory.Clear();vm.Refresh();check(vm.HistoryLines.Count==0,"empty history clears visible rows without stale content");
        vm.OnFinalize();
    }
}
