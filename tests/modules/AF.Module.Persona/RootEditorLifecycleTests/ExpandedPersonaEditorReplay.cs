using AnimusForge;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

internal static class ExpandedPersonaEditorReplay
{
    internal static void Run(Action<bool, string> check)
    {
        foreach (bool chinese in new[] { false, true })
        {
            TaleWorlds.Localization.TextObject.UseLanguage(chinese);
            long generation = 1;
            Hero selected = null;
            Hero a = new() { StringId = "expanded-a", Name = "A" };
            Hero b = new() { StringId = "expanded-b", Name = "B" };
            var profiles = new PersonaProfileStateOwner();
            profiles.Save(a.StringId, new()
            {
                Description = "original body", SchemaVersion = 3, VoiceId = "voice-a",
                Traits = new() { new() { Id = "cautious", Strength = 2 } },
                Motivations = new() { "old motivation" }, Values = new() { "old value" },
                Fears = new() { "old fear" }, SpeechStyle = "old speech"
            }, (_, _) => { });
            int writes = 0;
            var port = new PersonaEditorPort
            {
                CaptureGeneration = () => generation, IsCurrent = g => g == generation,
                GetSelectedHero = () => selected, SetSelectedHero = h => selected = h,
                GetNpcPersonaDescription = h => profiles.GetDescription(h.StringId),
                GetNpcVoiceId = h => profiles.GetNpcVoiceId(h.StringId),
                GetPersonaProfile = h => profiles.Get(h.StringId, false),
                UpdatePersonaProfile = (h, update) =>
                {
                    var current = profiles.Get(h.StringId, true);
                    update(current);
                    profiles.Save(h.StringId, current, (_, _) => { });
                    writes++;
                },
                TraitIds = new[] { "cautious", "bold", "merciful", "ruthless", "honorable", "pragmatic" }
            };
            var editor = new PersonaEditorController(port);
            void Choose(object id) => MBInformationManager.Selection.Confirm(new() { new(id, "", null) });
            void Open() { editor.OpenDevPersonaMenuFromExternal(a); Choose("edit_expanded"); }
            Open();
            check(MBInformationManager.Selection.Options.Count == 5, "all five expanded fields are reachable through actual menu");
            check(MBInformationManager.Selection.Options[0].Title == (chinese ? "核心特质" : "Core traits"), "expanded menu uses persona CN/EN UI resources");
            var profile = profiles.Get(a.StringId, false);
            foreach (string field in new[] { "motivations", "values", "fears", "speech_style" })
            {
                Choose(field);
                check(DevTextEditorHelper.Initial.StartsWith("old "), "current structured field prefilled");
                var confirm = DevTextEditorHelper.Confirm;
                confirm("  玩家 {NAME}\r\n second item  ");
                int saved = writes;
                confirm("duplicate");
                check(writes == saved, "duplicate text completion cannot write again");
                Choose(field);
                check(DevTextEditorHelper.Initial == (field == "speech_style" ? "玩家 {NAME}\n second item" : "玩家 {NAME}\nsecond item"), "changed field reopens with literal multiline text");
                var cancelledConfirm = DevTextEditorHelper.Confirm;
                DevTextEditorHelper.Cancel();
                cancelledConfirm("after cancel");
                check(writes == saved, "cancelled editor cannot save through retained callback");
            }
            check(profile.Description == "original body" && profile.VoiceId == "voice-a" && profile.Traits[0].Strength == 2,
                "four field edits preserve body voice and traits");
            check(profile.Motivations.SequenceEqual(new[] { "玩家 {NAME}", "second item" })
                && profile.Values.SequenceEqual(profile.Motivations) && profile.Fears.SequenceEqual(profile.Motivations)
                && profile.SpeechStyle == "玩家 {NAME}\n second item", "actual profile owner receives all edited fields");
            Choose("motivations"); DevTextEditorHelper.Confirm(" \r\n ");
            check(profile.Motivations.Count == 0 && profile.Values.Count == 2, "blank clears only selected field");
            Choose("traits");
            check(MBInformationManager.Selection.Options[0].Title == (chinese ? "谨慎 (2)" : "Cautious (2)"), "trait picker uses localized label and existing strength");
            Choose("cautious");
            var traitCallback = MBInformationManager.Selection;
            Choose(3);
            int traitSaved = writes;
            traitCallback.Confirm(new() { new(1, "", null) });
            check(profile.Traits[0].Strength == 3 && writes == traitSaved, "trait strength updates once");
            foreach (string id in new[] { "bold", "merciful", "ruthless", "honorable" }) { Choose(id); Choose(2); }
            check(profile.Traits.Count == 5, "traits can be added through five");
            int fullWrites = writes;
            Choose("pragmatic"); Choose(1);
            check(profile.Traits.Count == 5 && writes == fullWrites, "sixth trait rejected without changing profile");
            Choose("cautious"); Choose(0);
            Choose("pragmatic"); Choose(1);
            check(profile.Traits.Count == 5 && profile.Traits.All(t => t.Id != "cautious")
                && profile.Traits.Single(t => t.Id == "pragmatic").Strength == 1, "remove then replace trait");
            Choose("bold");
            var cancelledTrait = MBInformationManager.Selection;
            cancelledTrait.Cancel(); int cancelledWrites = writes;
            cancelledTrait.Confirm(new() { new(0, "", null) });
            check(writes == cancelledWrites, "cancelled strength choice does not remove trait");
            MBInformationManager.Selection.Cancel();
            Choose("values"); var oldText = DevTextEditorHelper.Confirm;
            editor.OpenDevPersonaMenuFromExternal(b);
            oldText("wrong NPC");
            check(writes == cancelledWrites && profiles.Get(b.StringId, false) == null, "old NPC text callback cannot write after target switch");
            Open(); Choose("traits"); Choose("bold"); var staleTrait = MBInformationManager.Selection;
            generation++;
            staleTrait.Confirm(new() { new(0, "", null) });
            check(writes == cancelledWrites && profile.Traits.Any(t => t.Id == "bold"), "retired save trait callback rejected");
            Open(); Choose("speech_style"); var staleText = DevTextEditorHelper.Confirm;
            generation++; staleText("wrong save");
            check(writes == cancelledWrites, "retired save text callback rejected");
            editor.OpenDevPersonaMenuFromExternal(b); Choose("edit_expanded"); Choose("fears");
            check(DevTextEditorHelper.Initial == "", "missing profile opens as empty without creation");
            DevTextEditorHelper.Confirm("new fear");
            check(profiles.Get(b.StringId, false).Fears.Single() == "new fear", "empty profile can gain expanded field");
        }
    }
}
