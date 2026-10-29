using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using AnimusForge;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.Layout;
using TaleWorlds.TwoDimension;

class Program
{
    static BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static object Bare(Type t) => FormatterServices.GetUninitializedObject(t);
    static void Set(object o, string n, object v) => o.GetType().GetProperty(n, All).SetValue(o, v, null);
    static void Field(object o, string n, object v) => o.GetType().GetField(n, All).SetValue(o, v);
    static int checks;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    static int Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
        {
            string p = Path.Combine(args[0], new AssemblyName(e.Name).Name + ".dll");
            return File.Exists(p) ? Assembly.LoadFrom(p) : null;
        };
        try { Run(); Console.WriteLine("PASS " + checks + " actual managed widget checks; no native render/game."); return 0; }
        catch (Exception e) { Console.WriteLine(e); return 1; }
    }
    static void Run()
    {
        var context = (UIContext)Bare(typeof(UIContext));
        Set(context, "EventManager", Bare(typeof(EventManager)));
        var brushes = (BrushFactory)Bare(typeof(BrushFactory));
        Field(brushes, "_brushes", new Dictionary<string, Brush> { { "DefaultBrush", new Brush { Name = "DefaultBrush" } } });
        Set(context, "BrushFactory", brushes);
        typeof(WidgetInfo).GetField("_widgetInfos", All).SetValue(null,
            new[] { typeof(Widget), typeof(ListPanel), typeof(ButtonWidget) }.ToDictionary(t => t, t => new WidgetInfo(t)));
        var containers = typeof(EventManager).GetField("_widgetContainers", All);
        var dictionary = (IDictionary)Activator.CreateInstance(containers.FieldType);
        var types = containers.FieldType.GetGenericArguments();
        foreach (var key in Enum.GetValues(types[0])) dictionary[key] = Activator.CreateInstance(types[1], All, null, new object[] { 32 }, null);
        containers.SetValue(context.EventManager, dictionary);
        string[] ids = { "AnimusForgeHeroGptSoVitsVoiceButton", "AnimusForgeHeroPersonaRerollButton", "AnimusForgeHeroPersonaPromptedRerollButton",
            "AnimusForgeHeroPersonaEditButton", "AnimusForgeHeroCourierButton", "AnimusForgeHeroIllustrateButton" };
        foreach (int[] order in new[] { new[] { 0, 1, 2, 3, 4, 5 }, new[] { 5, 4, 3, 2, 1, 0 }, new[] { 5, 3, 4, 1, 0, 2 } })
        {
            var root = new Widget(context);
            var content = new ListPanel(context); root.AddChild(content);
            var original = new Widget(context); content.AddChild(original);
            var bar = HeroEncyclopediaActionBar.GetOrCreate(root, content);
            Check(ReferenceEquals(bar, HeroEncyclopediaActionBar.GetOrCreate(root, content)) && content.ChildCount == 2, "one bar per real root");
            Check(ReferenceEquals(content.GetChild(0), bar) && ReferenceEquals(content.GetChild(1), original), "bar precedes original content without moving it");
            foreach (int index in order)
            {
                var button = new ButtonWidget(context) { Id = ids[index], SuggestedHeight = 38f, DoNotPassEventsToChildren = true };
                HeroEncyclopediaActionBar.AddButton(bar, button);
                Check(button.MarginTop == 0 && button.MarginBottom == 0 && button.MarginRight == 6, "button vertical spacing removed");
            }
            Check(((ListPanel)bar).StackLayout.LayoutMethod == LayoutMethod.VerticalTopToBottom, "two compact rows");
            for (int r = 0; r < 2; r++)
            {
                var row = (ListPanel)bar.GetChild(r);
                Check(row.StackLayout.LayoutMethod == LayoutMethod.HorizontalLeftToRight && row.ChildCount == 3, "three horizontal buttons");
                float width = 0;
                for (int c = 0; c < 3; c++)
                {
                    var button = row.GetChild(c);
                    Check(button.Id == ids[r * 3 + c], "stable order across core/Illustrator creation orders");
                    width += button.SuggestedWidth + button.MarginRight;
                }
                Check(width <= 739f, "fits native Hero content width");
            }
            bar.GetChild(0).GetChild(0).IsVisible = false;
            Check(!bar.GetChild(0).GetChild(0).IsVisible, "owner visibility remains authoritative");
        }
        Check(HeroEncyclopediaActionBar.GetOrCreate(null, null) == null, "missing root parent remains a no-op");
    }
}
