using AnimusForge;
using TaleWorlds.Library;

internal static class WorldMapActions
{
    internal static void Run(Action<bool, string> check)
    {
        var hero = new PlayerDirectActionChoice("player", "Player", targetType: "hero");
        var town = new PlayerDirectActionChoice("town_1", "Town [settlement:town_1]", targetType: "settlement", defaultDays: 15);
        var party = new PlayerDirectActionChoice("party_1", "Party [party:party_1]", targetType: "party");
        foreach (var entry in new[] {
            (PlayerDirectActionKind.Follow, hero, "FOLLOW:hero:player:2"),
            (PlayerDirectActionKind.GoToSettlement, town, "GO_TO_SETTLEMENT:settlement:town_1:2"),
            (PlayerDirectActionKind.PatrolSettlement, town, "PATROL_SETTLEMENT:settlement:town_1:2"),
            (PlayerDirectActionKind.FollowTarget, party, "FOLLOW:party:party_1:2"),
            (PlayerDirectActionKind.AttackTarget, town, "ATTACK:settlement:town_1:2:FORCE"),
            (PlayerDirectActionKind.ClearHideout, town, "CLEAR_HIDEOUT:settlement:town_1:2"),
            (PlayerDirectActionKind.MergeToPlayer, (PlayerDirectActionChoice)null, "MERGE_TO_PLAYER:2") })
        {
            check(PlayerDirectActionProtocol.TryBuildWorldMap(entry.Item1, entry.Item2, 2, "FORCE", false, out var raw)
                && raw == "[ACTION:WORLDMAP_ORDER:" + entry.Item3 + "]", "existing world-map tag: " + entry.Item1);
            check(PlayerDirectActionProtocol.TryBuildWorldMap(entry.Item1, entry.Item2, 2, "FORCE", true, out raw)
                && raw.StartsWith("[ACTION:WORLDMAP_ORDER:STOP] [ACTION:WORLDMAP_ORDER:"), "leading STOP replacement: " + entry.Item1);
        }
        check(PlayerDirectActionProtocol.TryBuildWorldMap(PlayerDirectActionKind.StopFollow, null, 0, null, false, out var stop)
            && stop == "[ACTION:WORLDMAP_ORDER:STOP]", "world STOP has no days/target");
        foreach (var id in new[] { "x:y", "x][ACTION:WORLDMAP_ORDER:STOP]", "x\ny", "" })
            check(!PlayerDirectActionProtocol.TryBuildWorldMap(PlayerDirectActionKind.FollowTarget,
                new PlayerDirectActionChoice(id, "bad", targetType: "hero"), 1, "AI", false, out _), "target ID injection rejected");
        check(!PlayerDirectActionProtocol.TryBuildWorldMap(PlayerDirectActionKind.FollowTarget, town, 1, "AI", false, out _), "follow cannot target settlement");
        check(!PlayerDirectActionProtocol.TryBuildWorldMap(PlayerDirectActionKind.GoToSettlement, party, 1, "AI", false, out _), "go cannot target party");
        check(!PlayerDirectActionProtocol.TryBuildWorldMap(PlayerDirectActionKind.AttackTarget, town, 1, "oops", false, out _), "attack mode is closed grammar");
        foreach (var days in new[] { 0, -1 })
            check(!PlayerDirectActionProtocol.TryBuildWorldMap(PlayerDirectActionKind.Follow, hero, days, "AI", false, out _), "positive days required");
        check(PlayerDirectActionProtocol.TagTemplate(PlayerDirectActionKind.Follow, PlayerDirectActionContext.WorldMap).Contains("WORLDMAP_ORDER"), "contextual displayed follow template");
        check(PlayerDirectActionProtocol.TagTemplate(PlayerDirectActionKind.Follow).Contains("SCENE_FOLLOW_PLAYER"), "scene follow remains unchanged");

        var controller = new PlayerDirectActionController();
        int calls = 0, finishes = 0; string executed = null, denial = null; bool current = true, valid = true;
        void Open(PlayerDirectActionContext context = PlayerDirectActionContext.WorldMap) => controller.Open("Actor", () => current, _ => null,
            kind => kind == PlayerDirectActionKind.Follow ? new() { hero } : new() { town, party }, (_, _) => valid,
            (_, _, raw) => { calls++; executed = raw; check(finishes > 0, "focus released before map dispatch"); }, () => finishes++,
            context, _ => denial);
        void Select(object key) { var p = (MultiSelectionInquiryData)InformationManager.Popup; p.AffirmativeAction(new() { p.Elements.Single(x => Equals(x.Identifier, key)) }); }
        void Input(string value) => ((TextInquiryData)InformationManager.Popup).AffirmativeAction(value);
        void Confirm() => ((InquiryData)InformationManager.Popup).AffirmativeAction();
        Open(); Select(PlayerDirectActionKind.Follow); Input(""); Select("append"); Confirm();
        check(executed == "[ACTION:WORLDMAP_ORDER:FOLLOW:hero:player:1]", "map follow blank uses original default");
        Open(); Select(PlayerDirectActionKind.GoToSettlement);
        check(((MultiSelectionInquiryData)InformationManager.Popup).SearchAvailable, "target selector uses native name search");
        Select(town); Input("3"); Select("replace"); var final = (InquiryData)InformationManager.Popup; final.AffirmativeAction(); final.AffirmativeAction();
        check(executed == "[ACTION:WORLDMAP_ORDER:STOP] [ACTION:WORLDMAP_ORDER:GO_TO_SETTLEMENT:settlement:town_1:3]" && calls == 2, "replacement confirmed once");
        Open(); Select(PlayerDirectActionKind.AttackTarget); Select(town); Select("AI"); Input(""); Select("append"); Confirm();
        check(executed == "[ACTION:WORLDMAP_ORDER:ATTACK:settlement:town_1:15:AI]", "target-specific default attack days");
        Open(); Select(PlayerDirectActionKind.StopFollow); check(((InquiryData)InformationManager.Popup).Text.Contains("queue"), "STOP warns whole queue"); Confirm();
        check(executed == stop, "map STOP requires explicit confirmation");
        foreach (string input in new[] { "0", "-1", "2147483648", "ALL", "x" })
        { int before = calls; Open(); Select(PlayerDirectActionKind.Follow); Input(input); check(calls == before, "invalid days cause no effects"); }
        foreach (string failure in new[] { "battle", "source-protected", "target expired" })
        {
            int before = calls; Open(); Select(PlayerDirectActionKind.Follow); Input("2"); Select("replace"); denial = failure; Confirm();
            check(calls == before, "replacement preflight rejection sends no STOP: " + failure); denial = null;
        }
        Open(); Select(PlayerDirectActionKind.Follow); Input("2"); Select("append"); int count = calls; valid = false; Confirm();
        check(calls == count, "candidate revalidation on map confirmation"); valid = true;
        Open(); Select(PlayerDirectActionKind.Follow); Input("2"); Select("append"); current = false; Confirm();
        check(calls == count, "stale final map popup rejected"); current = true;
        Open(); Select(PlayerDirectActionKind.Follow); ((TextInquiryData)InformationManager.Popup).NegativeAction();
        check(calls == count, "map cancel has no dispatch");
    }
}
