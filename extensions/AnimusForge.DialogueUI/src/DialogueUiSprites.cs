using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;
using EngineTextureType = TaleWorlds.Engine.Texture;
using UiTexture = TaleWorlds.TwoDimension.Texture;

namespace AnimusForge.DialogueUI
{
    internal static class DialogueUiSprites
    {
        private static readonly Dictionary<string, RuntimeSprite> Sprites = new Dictionary<string, RuntimeSprite>(StringComparer.Ordinal);
        private static readonly string[] Names = { "afdui_scroll_left", "afdui_scroll_body", "afdui_scroll_right", "afdui_parchment_panel", "afdui_input_panel", "afdui_button_normal", "afdui_button_hover", "afdui_button_pressed", "afdui_wax_seal", "afdui_console_base" };
        private static bool _failed;

        internal static void Install(Harmony harmony)
        {
            var refresh = AccessTools.Method(typeof(UIResourceManager), "RefreshSpriteData");
            if (refresh != null) harmony.Patch(refresh, postfix: new HarmonyMethod(typeof(DialogueUiSprites), nameof(OnSpriteDataRefresh)));
        }

        private static void OnSpriteDataRefresh()
        {
            if (Sprites.Count == 0) return;
            EnsureLoaded();
        }

        internal static bool EnsureLoaded()
        {
            if (_failed || !DialogueUiRuntime.Enabled || UIResourceManager.SpriteData == null) return false;
            try
            {
                foreach (string name in Names)
                {
                    if (!Sprites.TryGetValue(name, out RuntimeSprite sprite))
                    {
                        byte[] png = File.ReadAllBytes(Path.Combine(DialogueUiRuntime.ModuleRoot, "GUI", "SpriteParts", name + ".png"));
                        var texture = EngineTextureType.CreateFromMemory(png);
                        if (texture == null) throw new InvalidOperationException("Texture creation returned null: " + name);
                        texture.Name = name;
                        texture.SetTextureAsAlwaysValid();
                        texture.PreloadTexture(true);
                        int border = name == "afdui_parchment_panel" ? 40 : name == "afdui_input_panel" ? 16 : 0;
                        var nine = border == 0 ? SpriteNinePatchParameters.Empty : new SpriteNinePatchParameters(border, border, border, border);
                        sprite = new RuntimeSprite(name, texture, nine);
                        Sprites.Add(name, sprite);
                    }
                    if (UIResourceManager.SpriteData.Sprites.TryGetValue(name, out Sprite existing) && !ReferenceEquals(existing, sprite))
                        throw new InvalidOperationException("Sprite name already registered by another owner: " + name);
                    UIResourceManager.SpriteData.Sprites[name] = sprite;
                }
                return true;
            }
            catch (Exception ex)
            {
                _failed = true;
                DialogueUiRuntime.Log("Artwork unavailable; retaining original UI: " + ex.Message);
                return false;
            }
        }

        internal static Sprite Get(string name)
        {
            return Sprites.TryGetValue(name, out RuntimeSprite sprite) ? sprite : null;
        }

        internal static void Shutdown()
        {
            foreach (var pair in Sprites)
            {
                if (UIResourceManager.SpriteData != null && UIResourceManager.SpriteData.Sprites.TryGetValue(pair.Key, out Sprite value) && ReferenceEquals(value, pair.Value))
                    UIResourceManager.SpriteData.Sprites.Remove(pair.Key);
                pair.Value.Dispose();
            }
            Sprites.Clear();
        }

        private sealed class RuntimeSprite : Sprite
        {
            private readonly EngineTextureType _engineTexture;
            private readonly UiTexture _texture;
            internal RuntimeSprite(string name, EngineTextureType texture, SpriteNinePatchParameters nine)
                : base(name, texture.Width, texture.Height, nine)
            {
                _engineTexture = texture;
                _texture = new UiTexture(new EngineTexture(texture));
            }
            public override UiTexture Texture => _texture;
            public override Vec2 GetMinUvs() => Vec2.Zero;
            public override Vec2 GetMaxUvs() => Vec2.One;
            internal void Dispose() { _engineTexture.Release(); }
        }
    }
}
