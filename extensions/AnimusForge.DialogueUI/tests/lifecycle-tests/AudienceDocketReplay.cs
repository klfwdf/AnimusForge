using System.Reflection;
using System.Xml.Linq;
using AnimusForge.DialogueUI.Scene;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

internal static class AudienceDocketReplay
{
    internal static void Run(string root, Action<bool, string> check)
    {
        var update = typeof(AFSceneAudienceDocketWidget).GetMethod("OnUpdate", BindingFlags.NonPublic | BindingFlags.Instance)!;
        void Step(AFSceneAudienceDocketWidget widget, float dt) => update.Invoke(widget, new object[] { dt });
        var widget = new AFSceneAudienceDocketWidget(new UIContext()) { SuggestedHeight = 640f };
        Step(widget, 0f);
        check(widget.IsVisible && widget.ClipContents && widget.SuggestedHeight == 640f, "initial open snapshot is fully displayed and clipped");
        int writes = widget.HeightWrites;
        for (int i = 0; i < 500; i++) Step(widget, 0.016f);
        check(widget.HeightWrites == writes, "settled docket has no per-frame height writes");
        widget.RollOpen = false;
        check(widget.IsVisible && widget.SuggestedHeight == 640f && widget.DoNotPassEventsToChildren, "roll click keeps visible content and blocks its stale buttons");
        Step(widget, 0.10f);
        float partial = widget.SuggestedHeight;
        check(partial > 0f && partial < 640f && widget.IsVisible, "mid-close reveals a smaller top anchored clip without hiding");
        widget.RollOpen = true;
        check(widget.SuggestedHeight == partial && !widget.DoNotPassEventsToChildren, "mid-close reversal starts at the current visual height");
        Step(widget, 0.10f);
        check(widget.SuggestedHeight > partial && widget.SuggestedHeight < 640f, "reverse opening expands continuously");
        Step(widget, 0.15f);
        check(widget.SuggestedHeight == 640f && widget.IsVisible, "opening finishes in one quarter second");
        widget.RollOpen = false;
        Step(widget, 0.25f);
        check(widget.SuggestedHeight == 0f && !widget.IsVisible, "closing hides only at the zero-height endpoint");
        writes = widget.HeightWrites;
        for (int i = 0; i < 500; i++) Step(widget, 0.016f);
        check(widget.HeightWrites == writes, "closed idle docket does not rebuild layout");
        widget.RollOpen = true;
        check(widget.IsVisible && widget.SuggestedHeight == 0f, "opening binding wakes a hidden docket before ticking");
        Step(widget, 0.125f);
        float ratio = widget.SuggestedHeight / 640f;
        widget.FullHeight = 480f;
        check(Math.Abs(widget.SuggestedHeight / 480f - ratio) < 0.001f, "resize preserves the revealed fraction");
        Step(widget, 0.125f);
        check(widget.SuggestedHeight == 480f && widget.IsVisible, "resized opening completes at the new logical size");
        widget.RollOpen = false;
        Step(widget, float.NaN); Step(widget, -1f);
        check(widget.IsVisible && widget.SuggestedHeight == 480f, "invalid or paused frame time cannot corrupt the clip");
        Step(widget, 1f);
        check(!widget.IsVisible && widget.SuggestedHeight == 0f, "long frame finishes rather than stranding the animation");
        var loadedClosed = new AFSceneAudienceDocketWidget(new UIContext()) { RollOpen = false };
        Step(loadedClosed, 0f);
        check(!loadedClosed.IsVisible && loadedClosed.SuggestedHeight == 0f, "initial closed binding does not animate an unwanted open panel");

        var xml = XDocument.Load(Path.Combine(root, "extensions/AnimusForge.DialogueUI/GUI/Prefabs/AFSceneSessionScroll.xml"));
        var docket = xml.Descendants("AFSceneAudienceDocketWidget").Single();
        check((string)docket.Attribute("RollOpen")! == "@IsDocketOpen" && docket.Attribute("IsVisible") == null,
            "existing VM bool drives animation without immediate visibility binding");
        check((string)docket.Attribute("VerticalAlignment")! == "Top" && (string)docket.Attribute("MarginTop")! == "0",
            "audience clip is anchored to the top of the screen");
        var content = docket.Descendants("Widget").Single(e => (string?)e.Attribute("Id") == "AFAudienceDocketContent");
        check((string)content.Attribute("PositionYOffset")! == "-55" && (string)docket.Attribute("FullHeight")! == "585",
            "wooden roller reaches the screen edge while art and click targets move together");
        check(content.Descendants("ButtonWidget").Any(e => (string?)e.Attribute("Id") == "AFTabRollDocket"),
            "rolling button remains inside the same displaced content instead of an offset click mask");
        var art = docket.Descendants("Widget").Single(e => (string?)e.Attribute("Sprite") == "afdui_audience_docket_pure_clean");
        check((string)art.Attribute("SuggestedHeight")! == "640" && (string)art.Attribute("MarginTop")! == "0",
            "fixed artwork retains its dimensions rather than stretching during rolling");
        var roll = docket.Descendants("ButtonWidget").Single(e => (string?)e.Attribute("Id") == "AFTabRollDocket");
        var tag = xml.Descendants("Widget").Single(e => (string?)e.Attribute("IsVisible") == "@IsDocketRolled");
        check((string)roll.Attribute("Command.Click")! == "ExecuteToggleDocket" && (string)tag.Attribute("MarginTop")! == "0",
            "roll callback and top-aligned rolled tag stay on the audience path");
        check(!docket.Descendants().Any(e => (string?)e.Attribute("Command.Click") == "ExecuteCollapse"),
            "docket folding never invokes collapse of the whole session");
    }
}

namespace TaleWorlds.GauntletUI { public sealed class UIContext { } }
namespace TaleWorlds.GauntletUI.BaseTypes
{
    public enum SizePolicy { Fixed, StretchToParent }
    // Rendering/input are native leaves. The production widget owns every tested transition.
    public class Widget
    {
        private float _height;
        public int HeightWrites;
        public Widget(TaleWorlds.GauntletUI.UIContext context) { }
        public bool ClipContents, DoNotPassEventsToChildren, IsVisible = true;
        public SizePolicy HeightSizePolicy;
        public float SuggestedHeight { get => _height; set { _height = value; HeightWrites++; } }
        protected virtual void OnUpdate(float dt) { }
    }
}
