using System.Reflection;
using System.Text.Json;
using AnimusForge;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.Engine.GauntletUI;

internal static class Program
{
    private static int checks;
    private static void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL " + label); checks++; }
    private static Widget Node(string id, bool button = false) => button ? new ButtonWidget { Id = id } : new Widget { Id = id };
    private static void NotifyRelease(GauntletMovieIdentifier movie)
    {
        // The Harmony seam is explicit in this managed harness; a missing hook preserves the old failing behavior.
        typeof(NativeConversationAnswerAreaController).GetMethod("ReleaseMoviePrefix")?.Invoke(null, new object[] { movie });
    }
    private static int TrackedRoots() => ((System.Collections.ICollection)typeof(NativeConversationAnswerAreaController)
        .GetField("Roots", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null)).Count;
    private static void LifecycleCase()
    {
        int baseline = TrackedRoots();
        var oldRoot = Node("OldRoot"); var oldAnswers = Node("AnswerListContainer"); oldRoot.Children.Add(oldAnswers);
        var liveRoot = Node("LiveRoot"); var liveAnswers = Node("AnswerListContainer"); liveRoot.Children.Add(liveAnswers);
        var oldMovie = new GauntletMovieIdentifier { Movie = new Movie { RootWidget = oldRoot } };
        var liveMovie = new GauntletMovieIdentifier { Movie = new Movie { RootWidget = liveRoot } };
        NativeConversationAnswerAreaController.LoadMoviePostfix("MapConversation", oldMovie);
        NativeConversationAnswerAreaController.LoadMoviePostfix("SPConversation", liveMovie);
        Check(TrackedRoots() == baseline + 2, "lifecycle.two-live-movies");
        NativeConversationAnswerAreaController.SetSuppressed(true);
        NotifyRelease(new GauntletMovieIdentifier { Movie = oldMovie.Movie });
        NotifyRelease(null);
        Check(TrackedRoots() == baseline + 2, "lifecycle.foreign-identifier-is-not-owner");
        NotifyRelease(oldMovie);
        oldRoot.Disposed = true; oldMovie.Movie = null;
        Check(!liveAnswers.IsEnabled, "lifecycle.unload-does-not-release-other-movie");
        Logger.Warnings.Clear();
        NativeConversationAnswerAreaController.ForceRestoreAll();
        Check(Logger.Warnings.Count == 0 && oldRoot.DisposedAccesses == 0, "lifecycle.never-restore-unloaded-tree");
        Check(TrackedRoots() == baseline + 1, "lifecycle.unloaded-root-unregistered");
        Check(liveAnswers.IsVisible && liveAnswers.IsEnabled, "lifecycle.live-movie-restored");
        NotifyRelease(oldMovie);
        Check(TrackedRoots() == baseline + 1, "lifecycle.duplicate-release-is-inert");
        NotifyRelease(liveMovie); liveRoot.Disposed = true; liveMovie.Movie = null;
        NativeConversationAnswerAreaController.SetSuppressed(true);
        NativeConversationAnswerAreaController.OnApplicationTick();
        NativeConversationAnswerAreaController.SetSuppressed(false);
        NativeConversationAnswerAreaController.ForceRestoreAll();
        Check(TrackedRoots() == baseline && oldRoot.DisposedAccesses == 0 && liveRoot.DisposedAccesses == 0,
            "lifecycle.closed-movies-not-retained-or-ticked");
        var context = new UIContext();
        var first = Node("First"); first.Context = context;
        var second = Node("Second"); second.Context = context;
        NativeConversationAnswerAreaController.LoadMoviePostfix("MapConversation", new GauntletMovieIdentifier { Movie = new Movie { RootWidget = first } });
        NativeConversationAnswerAreaController.LoadMoviePostfix("SPConversation", new GauntletMovieIdentifier { Movie = new Movie { RootWidget = second } });
        typeof(NativeConversationAnswerAreaController).GetMethod("ClearContextPrefix")?.Invoke(null, new object[] { new GauntletLayer { UIContext = new UIContext() } });
        Check(TrackedRoots() == baseline + 2, "lifecycle.foreign-context-kept");
        typeof(NativeConversationAnswerAreaController).GetMethod("ClearContextPrefix")?.Invoke(null, new object[] { new GauntletLayer { UIContext = context } });
        first.Disposed = second.Disposed = true;
        NativeConversationAnswerAreaController.ForceRestoreAll();
        Check(Logger.Warnings.Count == 0 && first.DisposedAccesses == 0 && second.DisposedAccesses == 0,
            "lifecycle.context-finalize-bypasses-release-movie");
        Check(TrackedRoots() == baseline, "lifecycle.whole-context-roots-retired");
    }
    private static void Case(string movie, bool skin)
    {
        var root = Node("Root");
        var frame = skin ? Node("AFDialogueConsoleFrame") : root;
        if (skin) root.Children.Add(frame);
        var dialogue = Node("DialogueContainer");
        var answers = Node("AnswerListContainer");
        answers.Size = new Vec2 { X = skin ? 345 : 750, Y = 211 };
        answers.SuggestedHeight = 211; answers.HeightSizePolicy = SizePolicy.CoverChildren;
        var list = Node("AnswerList"); list.Size = answers.Size;
        var row = Node("AnswerRow", true); list.Children.Add(row);
        var scrollbar = Node("AFDialogueAnswerScrollbar");
        answers.Children.Add(list); answers.Children.Add(scrollbar);
        frame.Children.Add(dialogue); frame.Children.Add(answers);
        var cont = Node("ContinueButton", true); frame.Children.Add(cont);
        NativeConversationAnswerAreaController.LoadMoviePostfix(movie, new GauntletMovieIdentifier { Movie = new Movie { RootWidget = root } });
        NativeConversationAnswerAreaController.SetSuppressed(true);
        if (!skin)
        {
            Check(answers.IsVisible, "native.slot-kept");
            Check(answers.AlphaFactor == 0f, "native.slot-invisible-with-scrollbar");
            Check(answers.Size.X == 750 && answers.SuggestedHeight == 211, "native.slot-dimensions-preserved");
        }
        else
        {
            Check(!answers.IsVisible && answers.AlphaFactor == 1f, "skin.legacy-hide-container-unchanged");
            Check(list.AlphaFactor == 1f, "skin.legacy-list-alpha-unchanged");
        }
        Check(!answers.IsEnabled && answers.DoNotAcceptEvents, "slot.no-interaction");
        Check(!row.IsVisible && !row.IsEnabled && row.DoNotAcceptEvents, "rows.hidden-disabled");
        Check(dialogue.IsVisible && dialogue.AlphaFactor == 1f && dialogue.IsEnabled, "npc.untouched");
        Check(!cont.IsVisible, "continue.hidden");
        int searches = Widget.FindCalls;
        for (int i = 0; i < 25; i++) NativeConversationAnswerAreaController.OnApplicationTick();
        Check(Widget.FindCalls == searches, "no-new-tick-tree-searches");
        var late = Node("LateAnswer", true); list.Children.Add(late);
        NativeConversationAnswerAreaController.OnApplicationTick();
        Check(!late.IsVisible && !late.IsEnabled, "late-row.suppressed");
        Console.WriteLine("STATE " + JsonSerializer.Serialize(new { movie, skin, containerVisible = answers.IsVisible,
            containerAlpha = answers.AlphaFactor, containerHeightPolicy = answers.HeightSizePolicy.ToString(),
            containerHeight = answers.SuggestedHeight, listVisible = list.IsVisible, listAlpha = list.AlphaFactor,
            rowVisible = row.IsVisible, scrollbarVisible = scrollbar.IsVisible, npcVisible = dialogue.IsVisible }));
        NativeConversationAnswerAreaController.SetSuppressed(false);
        Check(answers.IsVisible && answers.AlphaFactor == 1f && answers.HeightSizePolicy == SizePolicy.CoverChildren, "slot.original-state-restored");
        Check(row.IsVisible && row.IsEnabled && !row.DoNotAcceptEvents && late.IsVisible && late.IsEnabled, "rows.original-state-restored");
        Check(cont.IsVisible && cont.IsEnabled, "continue.original-state-restored");
        NativeConversationAnswerAreaController.SetSuppressed(true);
        NativeConversationAnswerAreaController.ForceRestoreAll();
        Check(answers.IsVisible && answers.IsEnabled && answers.AlphaFactor == 1f && row.IsEnabled, "force-restore.native-controls");
    }
    public static int Main(string[] args)
    {
        try
        {
            if (!args.Contains("--new-ui-only")) { Case("SPConversation", false); Case("MapConversation", false); }
            Case("SPConversation", true); Case("MapConversation", true);
            LifecycleCase();
            Console.WriteLine($"PASS layout-state checks={checks}; real production controller with synthetic widgets; not native rendering");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
    }
}

namespace TaleWorlds.Library
{
    public struct Vec2 { public float X, Y; }
    public class ViewModel { }
}
namespace TaleWorlds.GauntletUI.BaseTypes
{
    public enum SizePolicy { Fixed, CoverChildren, StretchToParent }
    public class Widget
    {
        public static int FindCalls;
        public bool Disposed;
        public int DisposedAccesses;
        public UIContext Context { get; set; } = new();
        public string Id { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsEnabled { get; set; } = true;
        public bool DoNotAcceptEvents { get; set; }
        public float AlphaFactor { get; set; } = 1f;
        public SizePolicy HeightSizePolicy { get; set; } = SizePolicy.CoverChildren;
        public float SuggestedHeight { get; set; } = 120;
        public Vec2 Size { get; set; } = new Vec2 { X = 750, Y = 211 };
        public Vec2 MeasuredSize => Size;
        public List<Widget> Children { get; } = new();
        public int ChildCount => Children.Count;
        public Widget GetChild(int i) => Children[i];
        public Widget FindChild(string id, bool includeAllChildren)
        {
            if (Disposed) { DisposedAccesses++; throw new NullReferenceException("Synthetic disposed native UI context"); }
            FindCalls++;
            foreach (var c in Children)
            {
                if (c.Id == id) return c;
                if (includeAllChildren) { var found = c.FindChild(id, true); if (found != null) return found; }
            }
            return null;
        }
    }
    public class ButtonWidget : Widget { }
}
namespace TaleWorlds.GauntletUI
{
    public class UIContext { }
    public class Movie { public Widget RootWidget { get; set; } }
}
namespace TaleWorlds.Engine.GauntletUI
{
    public class GauntletMovieIdentifier { public Movie Movie { get; set; } }
    public class GauntletLayer
    {
        public UIContext UIContext { get; set; }
        public GauntletMovieIdentifier LoadMovie(string name, ViewModel vm) => null;
        public void ReleaseMovie(GauntletMovieIdentifier identifier) { }
        private void ClearContext() { UIContext = null; }
    }
}
namespace HarmonyLib
{
    public static class AccessTools { public static MethodInfo Method(Type t, string name, Type[] args = null) => null; }
    public class HarmonyMethod { public HarmonyMethod(Type t, string name) { } }
    public class Harmony { public Harmony(string id) { } public void Patch(MethodInfo method, HarmonyMethod prefix = null, HarmonyMethod postfix = null) { } }
}
namespace AnimusForge
{
    public static class Logger { public static readonly List<string> Warnings = new(); public static void Log(string scope, string text) { Warnings.Add(text); } public static void LogTrace(string scope, string text) { } }
    public static class FreezeWatchdog { public static IDisposable Scope(string name) => new Noop(); private sealed class Noop : IDisposable { public void Dispose() { } } }
}
