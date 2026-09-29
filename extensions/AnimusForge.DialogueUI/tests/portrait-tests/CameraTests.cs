using System;
using System.Reflection;
using AnimusForge.DialogueUI.Native;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using TaleWorlds.MountAndBlade.GauntletUI.TextureProviders;

static class CameraTests
{
    static readonly MethodInfo Frame = typeof(PortraitCamera).GetMethod("FrameHead", BindingFlags.NonPublic | BindingFlags.Static);
    static readonly MethodInfo Ready = typeof(PortraitCamera).GetMethod("ProviderReady", BindingFlags.NonPublic | BindingFlags.Static);
    static MatrixFrame Apply(CharacterTableau tableau, AgentVisuals visuals, MatrixFrame camera)
    {
        object[] args = { tableau, visuals, camera };
        Frame.Invoke(null, args);
        return (MatrixFrame)args[2];
    }
    internal static void Run(Action<bool,string> check)
    {
        PortraitCamera.Install(new HarmonyLib.Harmony());
        var original = new MatrixFrame { origin = new Vec3(9,8,7), rotation = new Mat3 {
            s = new Vec3(1,0,0), f = new Vec3(0,0,1), u = new Vec3(0,-1,0) } };
        var visuals = new AgentVisuals { Eye = new Vec3(2,3,1.55f), Scale = 0.85f };
        var unrelated = new CharacterTableau();
        check(Apply(unrelated, visuals, original).Equals(original) && visuals.Samples == 0,
            "Unregistered inventory/encyclopedia camera is unchanged and never sampled");
        var widget = new CharacterTableauWidget();
        PortraitCamera.Register(widget); // Provider is lazy and does not exist at movie load.
        var provider = new CharacterTableauTextureProvider();
        widget.TextureProvider = provider;
        Ready.Invoke(null, new object[] { widget });
        var camera = Apply(provider._characterTableau, visuals, original);
        check(!camera.origin.Equals(original.origin) && camera.rotation.Equals(original.rotation),
            "Late provider creation frames head while preserving native orientation");
        PortraitFraming.TryGetCameraOffsets(visuals.Scale, out float distance, out _);
        float projectedEye = (0.5f - (visuals.Eye.z-camera.origin.z)/(2*distance*(float)Math.Tan(Math.PI/8))) * 630 - 134;
        check(Math.Abs(projectedEye-68) < 0.001f && camera.origin.x == visuals.Eye.x,
            "Production camera places eyes at crop target and horizontal center");
        check(Apply(provider._characterTableau, visuals, camera).Equals(camera), "Repeated refresh has no cumulative camera drift");
        int refreshes = provider._characterTableau.Refreshes;
        PortraitCamera.Register(widget);
        check(provider._characterTableau.Refreshes == refreshes, "Repeated registration does not dirty visuals");
        var replacement = new CharacterTableauTextureProvider();
        widget.TextureProvider = replacement;
        Ready.Invoke(null, new object[] { widget });
        check(Apply(provider._characterTableau, visuals, original).Equals(original), "Provider recreation detaches old camera");
        check(!Apply(replacement._characterTableau, visuals, original).Equals(original), "Provider recreation attaches new camera");
        PortraitCamera.Unregister(widget);
        check(Apply(replacement._characterTableau, visuals, original).Equals(original), "Closing portrait removes camera ownership");
        PortraitCamera.Register(widget); // An existing provider must also register immediately.
        check(!Apply(replacement._characterTableau, visuals, original).Equals(original), "Existing provider binds on reopen");
        visuals.Eye = new Vec3(float.NaN,0,0);
        check(Apply(replacement._characterTableau, visuals, original).Equals(original), "Invalid eye position preserves native camera");
        PortraitCamera.Shutdown();
        check(Apply(replacement._characterTableau, visuals, original).Equals(original), "Shutdown clears ownership");
    }
}
