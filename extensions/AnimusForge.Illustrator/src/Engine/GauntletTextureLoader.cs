using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;
using BannerlordEngineTexture = TaleWorlds.Engine.Texture;
using BannerlordUiSprite = TaleWorlds.TwoDimension.Sprite;
using BannerlordUiTexture = TaleWorlds.TwoDimension.Texture;

namespace AnimusForge.Illustrator.Engine
{
    public static class GauntletTextureLoader
    {
        private static readonly ConcurrentDictionary<string, BannerlordUiSprite> LoadedSprites =
            new ConcurrentDictionary<string, BannerlordUiSprite>(StringComparer.OrdinalIgnoreCase);

        public static bool TryGetSprite(string spriteName, out BannerlordUiSprite sprite)
        {
            Core.IllustratorRuntime.AssertMainThread();
            sprite = null;
            if (string.IsNullOrWhiteSpace(spriteName))
            {
                return false;
            }

            if (LoadedSprites.TryGetValue(spriteName, out sprite))
            {
                return true;
            }

            if (UIResourceManager.SpriteData != null &&
                UIResourceManager.SpriteData.Sprites.TryGetValue(spriteName, out sprite))
            {
                return true;
            }

            return false;
        }

        public static BannerlordUiSprite LoadOrRegisterPngBytes(string spriteName, byte[] bytes, int fallbackWidth = 1024, int fallbackHeight = 1024, bool fixColorChannels = false)
        {
            Core.IllustratorRuntime.AssertMainThread();
            if (string.IsNullOrWhiteSpace(spriteName) || bytes == null || bytes.Length == 0)
            {
                return null;
            }

            try
            {
                bytes = PrepareEncodedImageForUi(bytes, fixColorChannels);
                if (LoadedSprites.TryGetValue(spriteName, out var previous))
                {
                    ReleaseSprite(spriteName, previous);
                }
                else if (UIResourceManager.SpriteData != null &&
                         UIResourceManager.SpriteData.Sprites.TryGetValue(spriteName, out var registered) &&
                         registered is RuntimeIllustrationSprite runtimeSprite)
                {
                    UIResourceManager.SpriteData.Sprites.Remove(spriteName);
                    runtimeSprite.ReleaseTexture();
                }

                byte[] bytesToLoad = bytes;
                int detectedWidth = 0;
                int detectedHeight = 0;

                BannerlordEngineTexture engineTexture = BannerlordEngineTexture.CreateFromMemory(bytesToLoad);
                if (engineTexture == null)
                {
                    Debug.Print($"[Illustrator] CreateFromMemory returned null for {spriteName}");
                    return null;
                }

                try
                {
                    engineTexture.Name = spriteName;
                    engineTexture.SetTextureAsAlwaysValid();
                    engineTexture.PreloadTexture(true);
                }
                catch
                {
                }

                int width = engineTexture.Width > 0 ? engineTexture.Width : (detectedWidth > 0 ? detectedWidth : fallbackWidth);
                int height = engineTexture.Height > 0 ? engineTexture.Height : (detectedHeight > 0 ? detectedHeight : fallbackHeight);

                BannerlordUiTexture uiTexture = new BannerlordUiTexture(new EngineTexture(engineTexture));
                var sprite = new RuntimeIllustrationSprite(spriteName, uiTexture, width, height);

                if (UIResourceManager.SpriteData != null)
                {
                    UIResourceManager.SpriteData.Sprites[spriteName] = sprite;
                }
                LoadedSprites[spriteName] = sprite;

                Debug.Print($"[Illustrator] Successfully loaded dynamic sprite: {spriteName} ({width}x{height}, colorPolicy=encoded-rgb, legacyFixIgnored={fixColorChannels})");
                return sprite;
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] Failed to load PNG sprite {spriteName}: {ex.Message}");
                return null;
            }
        }

        public static void ReleaseSprite(string spriteName, BannerlordUiSprite expected = null)
        {
            Core.IllustratorRuntime.AssertMainThread();
            if (string.IsNullOrWhiteSpace(spriteName)) return;
            if (!LoadedSprites.TryGetValue(spriteName, out var sprite)) return;
            if (expected != null && !ReferenceEquals(sprite, expected)) return;

            LoadedSprites.TryRemove(spriteName, out _);
            if (UIResourceManager.SpriteData != null &&
                UIResourceManager.SpriteData.Sprites.TryGetValue(spriteName, out var registered) &&
                ReferenceEquals(registered, sprite))
            {
                UIResourceManager.SpriteData.Sprites.Remove(spriteName);
            }

            try
            {
                if (sprite is RuntimeIllustrationSprite runtimeSprite)
                {
                    runtimeSprite.ReleaseTexture();
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] Failed to release sprite {spriteName}: {ex.Message}");
            }
        }

        public static void ReleaseAllSprites()
        {
            Core.IllustratorRuntime.AssertMainThread();
            foreach (var spriteName in LoadedSprites.Keys.ToArray())
            {
                ReleaseSprite(spriteName);
            }
        }

        // Encoded PNG/JPEG are not raw BGRA buffers. CreateFromMemory decodes their color channels.
        // Keep the legacy argument for callers/saved settings, but NEVER swap a decoded PNG here.
        // 2026-09-17: same cached portrait was normal RGB; legacy UI swap caused blue skin/gold->blue.
        internal static byte[] PrepareEncodedImageForUi(byte[] bytes, bool legacyFixColorChannels)
        {
            return ImagePayload.Normalize(bytes);
        }

        private sealed class RuntimeIllustrationSprite : BannerlordUiSprite
        {
            private readonly BannerlordUiTexture _texture;

            public RuntimeIllustrationSprite(string name, BannerlordUiTexture texture, int width, int height)
                : base(name, width, height, SpriteNinePatchParameters.Empty)
            {
                _texture = texture;
            }

            public void ReleaseTexture()
            {
                _texture?.PlatformTexture?.Release();
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
}
