using System;
using AnimusForge.Illustrator.UI.Overlays;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

// Actual production MovableGauntletLayer linked unchanged. UI/input/layout are managed fakes;
// this proves pixel math and Tick order, not native frame pacing or in-game drag feel.
class Program
{
    static int checks;
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; Console.WriteLine("PASS " + message); }
    static bool Near(float a, float b) => Math.Abs(a-b)<0.001f;
    static void Main()
    {
        foreach(float scale in new[] { 0.5f, 0.75f, 1f, 1.5f, 2f })
        {
            var layer = new TestLayer();
            layer.UIContext.CustomScale=scale;
            layer.UIContext.ScaleModifier=0.93f; // deliberately not the effective scale
            var panel = new Widget { PositionXOffset=17, PositionYOffset=29, SuggestedWidth=500, SuggestedHeight=500 };
            layer.AttachMovable(panel, panel); layer.Observed=panel;
            layer.Input.Pressed=true; layer.Input.Down=true; layer.Input.Mouse=new Vec2(40,20);
            layer.UIContext.EventManager.MousePosition=new System.Numerics.Vector2(900,900); // stale previous-frame pointer must not determine drag start
            layer.Frame(); layer.Input.Pressed=false;
            layer.Input.Mouse=new Vec2(140,80); layer.Frame();
            Check(Near(panel.PositionXOffset,17+100/scale) && Near(panel.PositionYOffset,29+60/scale), "one-to-one pixel displacement at scale "+scale);
            Check(Near(layer.RenderedX,panel.PositionXOffset) && Near(layer.RenderedY,panel.PositionYOffset), "same Tick layout observes drag, not previous frame "+scale);
            layer.Input.Mouse=new Vec2(55,25); layer.Frame();
            Check(Near(panel.PositionXOffset,17+15/scale) && Near(panel.PositionYOffset,29+5/scale), "absolute anchor avoids accumulated rounding "+scale);
            layer.Input.Mouse=new Vec2(40,20); layer.Frame();
            Check(Near(panel.PositionXOffset,17) && Near(panel.PositionYOffset,29), "returning mouse to press origin restores exact panel origin "+scale);
            layer.Input.Down=false; layer.Input.Mouse=new Vec2(200,200); layer.Frame();
            Check(Near(panel.PositionXOffset,17) && Near(panel.PositionYOffset,29), "mouse release immediately stops drag "+scale);
        }
        var resized = new TestLayer(); resized.UIContext.CustomScale=1.5f;
        var fixedPanel=new Widget { SuggestedWidth=500,SuggestedHeight=500 };
        resized.AttachMovable(fixedPanel); resized.Observed=fixedPanel;
        resized.Input.Pressed=true;resized.Input.Down=true;resized.Input.Mouse=new Vec2(490,490);
        resized.UIContext.EventManager.MousePosition=new System.Numerics.Vector2(resized.Input.Mouse.X,resized.Input.Mouse.Y);resized.Frame();resized.Input.Pressed=false;
        resized.Input.Mouse=new Vec2(640,580);resized.Frame();
        Check(Near(fixedPanel.SuggestedWidth,600) && Near(fixedPanel.SuggestedHeight,560), "resize uses effective scale too");
        resized.Input.Down=false;resized.Frame();resized.Input.Mouse=new Vec2(900,900);resized.Frame();
        Check(Near(fixedPanel.SuggestedWidth,600), "resize ends on release");
        var passive=new TestLayer();passive.Frame();Check(passive.Ticks==1,"nonmovable/fullscreen layer still executes base Tick");
        Console.WriteLine("TOTAL "+checks+" PASS / 0 FAIL");
    }
    class TestLayer : MovableGauntletLayer
    {
        public TestLayer():base("fixture",1){}
        public void Frame()=>Tick(1f/60);
    }
}
namespace TaleWorlds.Library
{
    public struct Vec2 { public float X,Y; public Vec2(float x,float y){X=x;Y=y;} public static Vec2 operator -(Vec2 a,Vec2 b)=>new Vec2(a.X-b.X,a.Y-b.Y); }
    public static class MathF { public static float Clamp(float x,float min,float max)=>Math.Clamp(x,min,max); public static float Abs(float x)=>Math.Abs(x); }
}
namespace TaleWorlds.InputSystem
{
    public enum InputKey { LeftMouseButton }
    public class InputFixture { public bool Down,Pressed; public TaleWorlds.Library.Vec2 Mouse; public bool IsKeyDown(InputKey key)=>Down;public bool IsKeyPressed(InputKey key)=>Pressed;public TaleWorlds.Library.Vec2 GetMousePositionPixel()=>Mouse; }
}
namespace TaleWorlds.GauntletUI
{
    public enum SizePolicy { Fixed, StretchToParent }
    public class EventManager { public System.Numerics.Vector2 MousePosition; public TaleWorlds.Library.Vec2 PageSize=new TaleWorlds.Library.Vec2(1920,1080); }
    public class UIContext { public float CustomScale=1,ScaleModifier=1; public EventManager EventManager=new EventManager(); }
}
namespace TaleWorlds.GauntletUI.BaseTypes
{
    public class Widget
    {
        public string Id; public int ChildCount=>0; public float SuggestedWidth,SuggestedHeight,PositionXOffset,PositionYOffset;
        public TaleWorlds.GauntletUI.SizePolicy WidthSizePolicy=TaleWorlds.GauntletUI.SizePolicy.Fixed, HeightSizePolicy=TaleWorlds.GauntletUI.SizePolicy.Fixed;
        public Vec2 GlobalPosition=>new Vec2(0,0);public Vec2 Size=>new Vec2(SuggestedWidth,SuggestedHeight);
        public Widget FindChild(string id,bool includeAllChildren)=>null;public Widget GetChild(int i)=>null;
        public bool IsPointInsideMeasuredArea(System.Numerics.Vector2 p)=>p.X>=0 && p.X<=Size.X && p.Y>=0 && p.Y<=Size.Y;
    }
}
namespace TaleWorlds.GauntletUI.Data
{
    public interface IGauntletMovie { Widget RootWidget { get; } }
}
namespace TaleWorlds.Engine.GauntletUI
{
    public class GauntletLayer
    {
        public GauntletLayer(string name,int order,bool shouldClear){}
        public TaleWorlds.InputSystem.InputFixture Input=new TaleWorlds.InputSystem.InputFixture();
        public TaleWorlds.GauntletUI.UIContext UIContext=new TaleWorlds.GauntletUI.UIContext();
        public Widget Observed;public float RenderedX,RenderedY;public int Ticks;
        protected virtual void Tick(float dt){Ticks++;if(Observed!=null){RenderedX=Observed.PositionXOffset;RenderedY=Observed.PositionYOffset;}}
    }
}
