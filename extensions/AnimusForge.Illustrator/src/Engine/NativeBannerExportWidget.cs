using System;
using System.Reflection;
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
        private static readonly Type ProviderType = Type.GetType("TaleWorlds.MountAndBlade.GauntletUI.TextureProviders.BannerTableauTextureProvider, TaleWorlds.MountAndBlade.GauntletUI", false);
        private static readonly FieldInfo OwnerField = ProviderType?.GetField("_bannerTableau", BindingFlags.Instance | BindingFlags.NonPublic);

        public NativeBannerExportWidget(UIContext context) : base(context) { }

        internal static bool SupportsDeferredSceneClear => SceneField != null && SceneField.FieldType == typeof(Scene) &&
            OwnerField != null && OwnerField.FieldType == typeof(BannerTableau);

        public override void OnClearTextureProvider()
        {
            // BannerTableau.OnFinalize clears its scene immediately, unlike the
            // engine's CharacterTableau/SceneTableau deferred cleanup. A PNG on
            // disk does not fence rendering. Transfer scene ownership to the
            // engine clear queue before invoking the normal provider finalizer.
            if (TextureProvider != null)
            {
                var tableau = OwnerField?.GetValue(TextureProvider) as BannerTableau;
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
