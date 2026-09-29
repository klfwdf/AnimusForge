using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
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

        public static BannerlordUiSprite LoadOrRegisterPngBytes(string spriteName, byte[] bytes, int fallbackWidth = 1024, int fallbackHeight = 1024)
        {
            Core.IllustratorRuntime.AssertMainThread();
            if (string.IsNullOrWhiteSpace(spriteName) || bytes == null || bytes.Length == 0)
            {
                return null;
            }

            try
            {
                return PreparedImage.FromBytes(bytes).Register(spriteName, fallbackWidth, fallbackHeight);
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] Failed to prepare PNG sprite {spriteName}: {ex.Message}");
                return null;
            }
        }

        internal static PreparedImage ReadPreparedImageForUi(string path, CancellationToken token)
        {
            return PreparedImage.FromFile(path, token);
        }

        internal static BannerlordUiSprite LoadOrRegisterPreparedImage(string spriteName, PreparedImage image)
        {
            Core.IllustratorRuntime.AssertMainThread();
            if (image == null || string.IsNullOrWhiteSpace(spriteName)) return null;
            return image.Register(spriteName, 1024, 1024);
        }

        private static BannerlordUiSprite RegisterPreparedBytes(string spriteName, byte[] bytes, int detectedWidth, int detectedHeight, int fallbackWidth, int fallbackHeight)
        {
            Core.IllustratorRuntime.AssertMainThread();
            try
            {
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

                BannerlordEngineTexture engineTexture = BannerlordEngineTexture.CreateFromMemory(bytes);
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

                Debug.Print($"[Illustrator] Successfully loaded dynamic sprite: {spriteName} ({width}x{height}, colorPolicy=encoded-rgb)");
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
        // This invariant has NO setting or caller override. NEVER swap a decoded PNG here.
        // 2026-09-17: same cached portrait was normal RGB; legacy UI swap caused blue skin/gold->blue.
        internal static byte[] PrepareEncodedImageForUi(byte[] bytes)
        {
            return ImagePayload.Normalize(bytes);
        }

        // Only these factories can create an instance; raw/unvalidated bytes never enter GPU
        // registration. The encoded buffer is privately owned and is not exposed to callers.
        internal sealed class PreparedImage
        {
            private readonly byte[] _encoded;
            internal int Width { get; }
            internal int Height { get; }

            private PreparedImage(byte[] ownedBytes)
            {
                _encoded = PrepareEncodedImageForUi(ownedBytes);
                Width = (_encoded[16] << 24) | (_encoded[17] << 16) | (_encoded[18] << 8) | _encoded[19];
                Height = (_encoded[20] << 24) | (_encoded[21] << 16) | (_encoded[22] << 8) | _encoded[23];
            }

            internal static PreparedImage FromBytes(byte[] bytes)
            {
                if (bytes == null) throw new ArgumentNullException(nameof(bytes));
                return new PreparedImage((byte[])bytes.Clone());
            }

            internal static PreparedImage FromFile(string path, CancellationToken token)
            {
                var result = new PreparedImage(ImagePayload.ReadEncodedFile(path, token));
                token.ThrowIfCancellationRequested();
                return result;
            }

            internal BannerlordUiSprite Register(string spriteName, int fallbackWidth, int fallbackHeight)
            {
                return RegisterPreparedBytes(spriteName, _encoded, Width, Height, fallbackWidth, fallbackHeight);
            }
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
