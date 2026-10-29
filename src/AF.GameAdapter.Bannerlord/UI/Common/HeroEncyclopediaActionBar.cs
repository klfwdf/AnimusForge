using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.Layout;

namespace AnimusForge;

// Shared source with the optional Illustrator build. Creation-time only; no UI polling.
internal static class HeroEncyclopediaActionBar
{
    private const string BarId = "AnimusForgeHeroActionBar";

    internal static Widget GetOrCreate(object root, Widget contentParent)
    {
        if (contentParent == null) return null;
        var existing = (root as Widget)?.FindChild(BarId, true);
        if (existing != null) return existing;
        var bar = new ListPanel(contentParent.Context)
        {
            Id = BarId, WidthSizePolicy = SizePolicy.CoverChildren,
            HeightSizePolicy = SizePolicy.CoverChildren,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            MarginTop = 8f, MarginBottom = 8f, DoNotAcceptEvents = true
        };
        bar.StackLayout.LayoutMethod = LayoutMethod.VerticalTopToBottom;
        // Native Hero information has a fixed 739-unit column. Three 220-unit
        // buttons fit both languages without clipping or changing native text width.
        for (int index = 0; index < 2; index++)
        {
            var row = new ListPanel(contentParent.Context)
            {
                WidthSizePolicy = SizePolicy.CoverChildren, HeightSizePolicy = SizePolicy.CoverChildren,
                HorizontalAlignment = HorizontalAlignment.Left, MarginBottom = index == 0 ? 4f : 0f,
                DoNotAcceptEvents = true
            };
            row.StackLayout.LayoutMethod = LayoutMethod.HorizontalLeftToRight;
            bar.AddChild(row);
        }
        contentParent.AddChildAtIndex(bar, 0);
        return bar;
    }

    internal static void AddButton(Widget bar, ButtonWidget button)
    {
        button.HorizontalAlignment = HorizontalAlignment.Left;
        button.MarginTop = button.MarginBottom = button.MarginLeft = 0f;
        button.MarginRight = 6f;
        int order = Order(button.Id);
        button.SuggestedWidth = 220f;
        Widget row = bar.GetChild(order < 3 ? 0 : 1);
        int index = 0;
        while (index < row.ChildCount && Order(row.GetChild(index).Id) <= order) index++;
        row.AddChildAtIndex(button, index);
    }

    private static int Order(string id)
    {
        switch (id)
        {
            case "AnimusForgeHeroGptSoVitsVoiceButton": return 0;
            case "AnimusForgeHeroPersonaRerollButton": return 1;
            case "AnimusForgeHeroPersonaPromptedRerollButton": return 2;
            case "AnimusForgeHeroPersonaEditButton": return 3;
            case "AnimusForgeHeroCourierButton": return 4;
            default: return 5;
        }
    }
}
