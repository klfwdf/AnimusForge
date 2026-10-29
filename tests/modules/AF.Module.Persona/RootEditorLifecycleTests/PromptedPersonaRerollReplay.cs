using AnimusForge;
using TaleWorlds.CampaignSystem;

internal static class PromptedPersonaRerollReplay
{
    internal static void Run(Action<bool, string> check)
    {
        long generation = 5;
        Hero selected = null;
        var hero = new Hero { StringId = "prompted-a", Name = "A" };
        var other = new Hero { StringId = "prompted-b", Name = "B" };
        int normalCalls = 0, promptedCalls = 0, closes = 0;
        string capturedPrompt = null;
        Hero capturedHero = null;
        var port = new PersonaEditorPort
        {
            CaptureGeneration = () => generation, IsCurrent = g => g == generation,
            GetSelectedHero = () => selected, SetSelectedHero = h => selected = h,
            GeneratePersona = h => { normalCalls++; return Task.FromResult(""); },
            GeneratePersonaWithPrompt = (h, prompt) =>
            {
                promptedCalls++; capturedHero = h; capturedPrompt = prompt;
                return Task.FromResult("");
            },
            CompleteOnMainThread = (_, callback) => Task.FromResult(callback())
        };
        var editor = new PersonaEditorController(port);
        editor.OpenHeroPersonaPromptedReroll(hero, () => closes++);
        check(DevTextEditorHelper.Initial == "", "prompted regeneration starts with a request-local empty draft");
        var submit = DevTextEditorHelper.Confirm;
        submit("  A cautious scholar {NAME}\nKeep the old oath.  ");
        check(promptedCalls == 1 && normalCalls == 0 && ReferenceEquals(capturedHero, hero)
            && capturedPrompt == "A cautious scholar {NAME}\nKeep the old oath.", "prompt reaches the selected Hero unchanged except boundary whitespace");
        submit("duplicate");
        check(promptedCalls == 1, "one prompt cannot submit twice");
        editor.OpenHeroPersonaPromptedReroll(hero, null);
        DevTextEditorHelper.Cancel();
        DevTextEditorHelper.Confirm("cancelled");
        check(promptedCalls == 1, "cancelled prompt cannot generate");
        editor.OpenHeroPersonaPromptedReroll(hero, null);
        var empty = DevTextEditorHelper.Confirm;
        empty(" \n "); empty("retired empty dialog");
        check(promptedCalls == 1, "empty prompt makes no generation request and retires the old callback");
        editor.OpenHeroPersonaPromptedReroll(hero, null);
        var staleTarget = DevTextEditorHelper.Confirm;
        editor.OpenHeroPersonaPromptedReroll(other, null);
        staleTarget("wrong target");
        check(promptedCalls == 1, "old target prompt cannot generate after target switch");
        var staleSave = DevTextEditorHelper.Confirm;
        generation++;
        staleSave("wrong save");
        check(promptedCalls == 1, "retired save prompt cannot generate");
        editor.RunHeroPersonaRerollAsync(hero, null).GetAwaiter().GetResult();
        check(normalCalls == 1 && promptedCalls == 1, "ordinary regeneration retains its original path without prompt leakage");
    }
}
