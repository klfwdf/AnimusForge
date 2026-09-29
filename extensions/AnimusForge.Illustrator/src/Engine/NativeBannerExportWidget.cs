using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using TaleWorlds.Engine;
using TaleWorlds.GauntletUI;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.TwoDimension;

namespace AnimusForge.Illustrator.Engine
{
    // Constructed in code; no prefab registration. Gauntlet drives the native provider.
    internal sealed class NativeBannerExportWidget : BannerTableauWidget
    {
        private static readonly FieldInfo SceneField = typeof(BannerTableau).GetField("_scene", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo ProviderTypesField = typeof(TextureProviderFactory).GetField("_textureProvidertypes", BindingFlags.Static | BindingFlags.NonPublic);
        private static FieldInfo _ownerField;
        private static readonly MethodInfo AddPaintHandler = typeof(RenderTargetComponent)
            .GetEvent("PaintNeeded", BindingFlags.Instance | BindingFlags.NonPublic)?.GetAddMethod(true);
        private TaleWorlds.Engine.Texture _configuredTexture;
        private BannerTableau _tableau;
        private RenderTargetComponent _configuredComponent;
        private int _preparedFrames;
        internal bool ReadyForExport => Volatile.Read(ref _preparedFrames) >= 2;

        public NativeBannerExportWidget(UIContext context) : base(context) { }

        internal static bool SupportsDeferredSceneClear
        {
            get
            {
                if (SceneField == null || SceneField.FieldType != typeof(Scene) || AddPaintHandler == null) return false;
                // Use the exact type Gauntlet will instantiate. Bannerlord's module
                // load context does not necessarily resolve Assembly.Load by name.
                if (_ownerField == null && ProviderTypesField?.GetValue(null) is Dictionary<string, Type> providers &&
                    providers.TryGetValue("BannerTableauTextureProvider", out var provider))
                    _ownerField = provider.GetField("_bannerTableau", BindingFlags.Instance | BindingFlags.NonPublic);
                return _ownerField != null && _ownerField.FieldType == typeof(BannerTableau);
            }
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
            if (!SupportsDeferredSceneClear || TextureProvider == null) return;
            if (_tableau == null) _tableau = _ownerField.GetValue(TextureProvider) as BannerTableau;
            var texture = _tableau?.Texture;
            if (texture == null || ReferenceEquals(texture, _configuredTexture)) return;
            // Subscribe after vanilla's handler, on this export texture only.
            // Vanilla re-applies RenderWithPostfx=false in every paint callback,
            // so setting the flag once from the UI tick is insufficient.
            BindExportPaintHandler(texture.RenderTargetComponent);
            _configuredTexture = texture;
        }

        internal void BindExportPaintHandler(RenderTargetComponent component)
        {
            if (component == null || ReferenceEquals(component, _configuredComponent)) return;
            Volatile.Write(ref _preparedFrames, 0);
            AddPaintHandler.Invoke(component,
                new object[] { new RenderTargetComponent.TextureUpdateEventHandler(PrepareExportFrame) });
            _configuredComponent = component;
        }

        private void PrepareExportFrame(TaleWorlds.Engine.Texture texture, EventArgs args)
        {
            if (ReferenceEquals(texture, null) || !(texture.UserData is Scene scene)) return;
            var view = texture.TableauView;
            scene.EnsurePostfxSystem();
            scene.SetDofMode(false);
            scene.SetMotionBlurMode(false);
            scene.SetBloom(false);
            scene.SetShadow(true);
            scene.SetMinExposure(1f);
            scene.SetMaxExposure(1f);
            scene.SetTargetExposure(1f);
            // Native 1.4.8 writes final/depth/shadow passes even for a final PNG.
            // The no-postfx banner path has no shadow-render context and crashes
            // in that dump after the PNG is already written. Match the character
            // export's initialized render path without artistic post effects.
            view.SetRenderWithPostfx(true);
            view.SetPostfxConfigParams(0);
            view.SetSceneUsesShadows(true);
            var center = TaleWorlds.Library.Vec3.Zero;
            view.SetFocusedShadowmap(true, ref center, 1.55f);
            if (Interlocked.Increment(ref _preparedFrames) == 2)
                TaleWorlds.Library.Debug.Print("[NativeBanner] Export render path initialized (postfx/shadow, two paint callbacks)");
        }

        public override void OnClearTextureProvider()
        {
            // BannerTableau.OnFinalize clears its scene immediately, unlike the
            // engine's CharacterTableau/SceneTableau deferred cleanup. A PNG on
            // disk does not fence rendering. Transfer scene ownership to the
            // engine clear queue before invoking the normal provider finalizer.
            if (TextureProvider != null)
            {
                var tableau = _ownerField?.GetValue(TextureProvider) as BannerTableau;
                if (!SupportsDeferredSceneClear || tableau == null)
                    throw new NotSupportedException("Native banner deferred cleanup contract is unavailable.");
                var scene = SceneField.GetValue(tableau) as Scene;
                var view = tableau.Texture?.TableauView;
                if (scene != null && view != null)
                {
                    TaleWorlds.Library.Debug.Print("[NativeBanner] Queueing deferred scene clear before provider release");
                    view.SetSaveFinalResultToDisk(false);
                    view.SetEnable(false);
                    view.AddClearTask();
                    // Prevent BannerTableau.OnFinalize from calling ClearAll on
                    // the same scene. Native clear task now owns its retirement.
                    SceneField.SetValue(tableau, null);
                    scene.ManualInvalidate();
                }
            }
            base.OnClearTextureProvider();
            _configuredTexture = null;
            _configuredComponent = null;
            _tableau = null;
            Volatile.Write(ref _preparedFrames, 0);
            TaleWorlds.Library.Debug.Print("[NativeBanner] Provider release completed");
        }

        protected override void OnRender(TwoDimensionContext context, TwoDimensionDrawContext drawContext)
        {
            _isRenderRequestedPreviousFrame = true;
            // Never call base.OnRender or drawContext.Draw. Vanilla banner drawing
            // ignores the widget's AlphaFactor; skipping the blit prevents flashing.
        }
    }
}
