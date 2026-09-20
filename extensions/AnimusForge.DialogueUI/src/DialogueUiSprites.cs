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
        private static readonly string[] Names = { "afdui_scroll_left", "afdui_scroll_body", "afdui_scroll_right", "afdui_parchment_panel", "afdui_input_panel", "afdui_button_normal", "afdui_button_hover", "afdui_button_pressed", "afdui_wax_seal" };
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
                        string filePath = Path.Combine(DialogueUiRuntime.ModuleRoot, "GUI", "SpriteParts", name + ".png");
                        if (!File.Exists(filePath)) throw new FileNotFoundException("Sprite file not found: " + filePath);

                        TryReadPngSize(filePath, out int pngWidth, out int pngHeight);
                        var texture = TryLoadTexture(filePath, name);
                        if (texture == null) throw new InvalidOperationException("Texture creation returned null: " + name);

                        int width = texture.Width > 0 ? texture.Width : (pngWidth > 0 ? pngWidth : 512);
                        int height = texture.Height > 0 ? texture.Height : (pngHeight > 0 ? pngHeight : 512);

                        int border = name == "afdui_parchment_panel" ? 40 : name == "afdui_input_panel" ? 16 : 0;
                        var nine = border == 0 ? SpriteNinePatchParameters.Empty : new SpriteNinePatchParameters(border, border, border, border);
                        sprite = new RuntimeSprite(name, texture, width, height, nine);
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

        private static EngineTextureType TryLoadTexture(string filePath, string name)
        {
            try
            {
                var texture = EngineTextureType.LoadTextureFromPath(Path.GetFileName(filePath), Path.GetDirectoryName(filePath));
                if (texture != null)
                {
                    texture.Name = name;
                    texture.SetTextureAsAlwaysValid();
                    texture.PreloadTexture(true);
                    return texture;
                }
            }
            catch { }

            try
            {
                byte[] png = File.ReadAllBytes(filePath);
                var texture = EngineTextureType.CreateFromMemory(png);
                if (texture != null)
                {
                    texture.Name = name;
                    texture.SetTextureAsAlwaysValid();
                    texture.PreloadTexture(true);
                    return texture;
                }
            }
            catch { }

            return null;
        }

        private static bool TryReadPngSize(string filePath, out int width, out int height)
        {
            width = 0;
            height = 0;
            try
            {
                byte[] header = new byte[24];
                using (FileStream stream = File.OpenRead(filePath))
                {
                    if (stream.Read(header, 0, header.Length) != header.Length) return false;
                }
                if (header[0] != 0x89 || header[1] != 0x50 || header[2] != 0x4E || header[3] != 0x47) return false;
                width = ReadBigEndianInt32(header, 16);
                height = ReadBigEndianInt32(header, 20);
                return width > 0 && height > 0;
            }
            catch
            {
                return false;
            }
        }

        private static int ReadBigEndianInt32(byte[] bytes, int offset)
        {
            return (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
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

        private sealed class FallbackEngineTexture : EngineTexture, ITexture
        {
            private readonly int _fallbackWidth;
            private readonly int _fallbackHeight;
            internal FallbackEngineTexture(EngineTextureType texture, int width, int height) : base(texture)
            {
                _fallbackWidth = width;
                _fallbackHeight = height;
            }
            int ITexture.Width => Texture != null && Texture.Width > 0 ? Texture.Width : _fallbackWidth;
            int ITexture.Height => Texture != null && Texture.Height > 0 ? Texture.Height : _fallbackHeight;
        }

        private sealed class RuntimeSprite : Sprite
        {
            private readonly EngineTextureType _engineTexture;
            private readonly UiTexture _texture;
            internal RuntimeSprite(string name, EngineTextureType texture, int width, int height, SpriteNinePatchParameters nine)
                : base(name, width, height, nine)
            {
                _engineTexture = texture;
                _texture = new UiTexture(new FallbackEngineTexture(texture, width, height));
            }
            public override UiTexture Texture => _texture;
            public override Vec2 GetMinUvs() => Vec2.Zero;
            public override Vec2 GetMaxUvs() => Vec2.One;
            internal void Dispose() { _engineTexture?.Release(); }
        }
    }
}
