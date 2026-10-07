using System;
using System.Collections.Generic;
using AnimusForge.SiegeAftermathIntervention;

internal static class TownScenePerceptionTests
{
    internal static void Run(Action<bool, string> check, TownPromptTextCatalog supplied = null)
    {
        TownPromptTextCatalog catalog = TownPromptTextCatalog.Resolve(supplied);
        Dictionary<string, string> text = catalog.ScenePerceptionTexts;
        var session = new TownScenePerceptionSession();
        var a = new TownPerceivedPerson("a", "Alice");
        var b = new TownPerceivedPerson("b", "Bob");
        string Prompt(string observer = "speaker", bool owned = false) => session.BuildPrompt("town", observer, owned, catalog);
        string Deaths(int count) => text["count"].Replace("{count}", count.ToString());
        session.Observe("town", "speaker", new[] { a });
        check(Prompt() == "", "perception: inactive capture is rejected");
        session.Begin("town");
        session.Observe("town", "speaker", new[] { a });
        check(Prompt().Contains("Alice") && !Prompt().Contains(text["arrived"].Replace("{name}", "Alice")), "perception: first view is a baseline, not an invented arrival");
        session.Observe("town", "speaker", new[] { a, b });
        check(Prompt().Contains(text["arrived"].Replace("{name}", "Bob")), "perception: new nearby person is observed");
        session.Observe("town", "speaker", new[] { b });
        check(Prompt().Contains(text["gone"].Replace("{name}", "Alice")) && Prompt().Contains(Deaths(0)), "perception: loss of view does not invent death");
        session.RecordRemoval("town", b, TownObservedRemoval.Unconscious, "speaker", "Player");
        session.Observe("town", "quiet", new[] { b });
        session.RecordRemoval("town", b, TownObservedRemoval.Unconscious, "quiet", "Player");
        string quietPrompt = Prompt("quiet");
        check(quietPrompt.Contains(text["none"]) && quietPrompt.Contains(Deaths(0))
            && !quietPrompt.Contains("Bob") && !quietPrompt.Contains(text["cause"].Replace("{actor}", "Player")),
            "perception: unconscious removal is silent and never reported as a killing");
        session.RecordRemoval("town", b, TownObservedRemoval.Killed, "speaker", "Player");
        session.RecordRemoval("town", b, TownObservedRemoval.Killed, "speaker", "Player");
        check(Prompt().Contains(Deaths(1)), "perception: later confirmed death counts once");
        session.Observe("town", "speaker", new[] { b });
        check(Prompt().Contains(text["none"]), "perception: stale live snapshot cannot resurrect removed identity");
        session.Observe("town", "newcomer", new[] { a });
        check(Prompt("newcomer").Contains(Deaths(0)) && !Prompt("newcomer").Contains("Bob"), "perception: newcomer does not inherit another person's deaths");
        check(Prompt(owned: true).Contains(text["owned"]) && !Prompt(owned: true).Contains(text["occupied"]), "perception: internal incident is not enemy conquest");
        check(Prompt().Contains(text["occupied"]), "perception: conquest provenance remains available");
        var c = new TownPerceivedPerson("c", "Carol");
        session.RecordRemoval("town", c, TownObservedRemoval.Killed, "speaker", "");
        check(Prompt().Contains(text["unknown_cause"]) && Prompt().Contains(Deaths(2)), "perception: unseen attacker is not attributed to player");
        session.RecordRemoval("town", a, TownObservedRemoval.Left, "speaker", "");
        check(Prompt().Contains(text["left"].Replace("{name}", "Alice")) && Prompt().Contains(Deaths(2)), "perception: departure is not a casualty");
        session.Observe("town", "speaker", new[] { new TownPerceivedPerson("b-new", "Bob") });
        check(Prompt().Contains(text["arrived"].Replace("{name}", "Bob")), "perception: same name on a new identity is a new person");
        session.RecordRemoval("other_town", new TownPerceivedPerson("other", "OtherTownVictim"), TownObservedRemoval.Killed, "speaker", "Player");
        check(!Prompt().Contains("OtherTownVictim") && Prompt().Contains(Deaths(2)), "perception: wrong town event is ignored");
        for (int i = 0; i < 9; i++)
            session.RecordRemoval("town", new TownPerceivedPerson("away" + i, "away" + i), TownObservedRemoval.Left, "speaker", "");
        check(!Prompt().Contains("away0") && Prompt().Contains("away8") && Prompt().Contains(Deaths(2)), "perception: recent event text is bounded without losing total witnessed deaths");
        session.ForgetObserver("speaker");
        check(Prompt() == "", "perception: removed observer loses prompt access");
        session.EndScene();
        check(Prompt("newcomer") == "", "perception: leaving scene clears all perception");
        session.Begin("town");
        session.Observe("town", "speaker", new[] { a });
        check(Prompt().Contains(Deaths(0)) && !Prompt().Contains("away8"), "perception: reentry starts without stale events");
        check(new TownPerceivedPerson("x", "Alice\nInjected").Name == "Alice Injected", "perception: names cannot inject additional prompt lines");
        var partial = new TownPromptTextCatalog { ScenePerceptionTexts = new Dictionary<string,string> { ["heading"] = "Custom" } };
        check(session.BuildPrompt("town", "speaker", false, partial).StartsWith("Custom")
            && TownPromptTextCatalog.Resolve(partial).ScenePerceptionTexts.ContainsKey("killed"), "perception: old or partial resources receive safe fallback keys");
    }
}
