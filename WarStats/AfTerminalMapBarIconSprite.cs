using System;
using System.IO;
using AnimusForge;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.Library;
using Logger = AnimusForge.Logger;
using BannerlordEngineTexture = TaleWorlds.Engine.Texture;
using BannerlordUiSprite = TaleWorlds.TwoDimension.Sprite;
using BannerlordUiTexture = TaleWorlds.TwoDimension.Texture;

namespace AFWarStatsTerminal.UI;

// Loads GUI/SpriteParts/af_terminal/af_terminal_icon.png at runtime and points the map-bar icon brush layer at it.
// The brush XML ships a native fallback sprite so the button still renders if the PNG is missing.
internal static class AfTerminalMapBarIconSprite
{
    private const string Category = "af_terminal";

    private const string FileName = "af_terminal_icon.png";

    private const string SpriteName = Category + "\\af_terminal_icon";

    private const string IconBrushName = "MapBar.Left.Icons";

    // A texture's pixel resolution is not a widget size. The native icon slot is 60 x 40.
    private const int LogicalIconSize = 40;

    private static BannerlordUiSprite _sprite;

    private static bool _loadFailed;

    // Called whenever the nav item is inserted; cheap once the layer already uses the runtime sprite.
    internal static void EnsureApplied()
    {
        try
        {
            Brush brush = UIResourceManager.BrushFactory?.GetBrush(IconBrushName);
            BrushLayer layer = brush?.GetLayer(AfWarStatsMapNavigationEntry.ItemId);
            if (layer == null)
            {
                return;
            }
            BannerlordUiSprite sprite = GetOrCreateSprite();
            if (sprite != null && !ReferenceEquals(layer.Sprite, sprite))
            {
                // Style layers read through to this source layer unless they override the sprite themselves.
                layer.Sprite = sprite;
            }
            // Apply even for a cached sprite or the native fallback: inherited Original policies
            // must not turn the texture resolution into an oversized map-bar icon.
            layer.WidthPolicy = BrushLayerSizePolicy.StretchToTarget;
            layer.HeightPolicy = BrushLayerSizePolicy.StretchToTarget;
            layer.ImageFitType = ImageFit.ImageFitTypes.Contain;
            layer.ImageFitHorizontalAlignment = ImageFit.ImageHorizontalAlignments.Center;
            layer.ImageFitVerticalAlignment = ImageFit.ImageVerticalAlignments.Center;
        }
        catch (Exception ex)
        {
            Logger.Log("Terminal", "[WARN] map bar terminal icon apply failed: " + ex.Message);
        }
    }

    private static BannerlordUiSprite GetOrCreateSprite()
    {
        if (_sprite != null || _loadFailed || UIResourceManager.SpriteData == null)
        {
            return _sprite;
        }
        if (UIResourceManager.SpriteData.Sprites.TryGetValue(SpriteName, out BannerlordUiSprite existing) && existing is RuntimeTextureSprite)
        {
            _sprite = existing;
            return _sprite;
        }
        string filePath = Path.Combine(AnimusForgeModulePaths.GetCurrentModuleRoot(), "GUI", "SpriteParts", Category, FileName);
        try
        {
            if (!File.Exists(filePath))
            {
                _loadFailed = true;
                Logger.Log("Terminal", "[WARN] map bar terminal icon missing: " + filePath);
                return null;
            }
            BannerlordEngineTexture engineTexture = BannerlordEngineTexture.CreateFromMemory(File.ReadAllBytes(filePath));
            if (engineTexture == null)
            {
                _loadFailed = true;
                Logger.Log("Terminal", "[WARN] map bar terminal icon texture load returned null.");
                return null;
            }
            try
            {
                engineTexture.Name = SpriteName;
                engineTexture.SetTextureAsAlwaysValid();
                engineTexture.PreloadTexture(true);
            }
            catch
            {
                // Native texture validity can be reported lazily while still rendering correctly later.
            }
            _sprite = new RuntimeTextureSprite(SpriteName, new BannerlordUiTexture(new EngineTexture(engineTexture)));
            UIResourceManager.SpriteData.Sprites[SpriteName] = _sprite;
            return _sprite;
        }
        catch (Exception ex)
        {
            _loadFailed = true;
            Logger.Log("Terminal", "[WARN] map bar terminal icon load failed: " + ex.Message);
            return null;
        }
    }

    private sealed class RuntimeTextureSprite : BannerlordUiSprite
    {
        private readonly BannerlordUiTexture _texture;

        public RuntimeTextureSprite(string name, BannerlordUiTexture texture)
            : base(name, LogicalIconSize, LogicalIconSize, TaleWorlds.TwoDimension.SpriteNinePatchParameters.Empty)
        {
            _texture = texture;
        }

        public override BannerlordUiTexture Texture => _texture;

        public override Vec2 GetMinUvs()
        {
            return Vec2.Zero;
        }

        public override Vec2 GetMaxUvs()
        {
            return Vec2.One;
        }
    }
}
