using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
                LoadedSprites[spriteName] = sprite;
                return true;
            }

            return false;
        }

        public static BannerlordUiSprite LoadOrRegisterPngBytes(string spriteName, byte[] bytes, int fallbackWidth = 1024, int fallbackHeight = 1024)
        {
            if (string.IsNullOrWhiteSpace(spriteName) || bytes == null || bytes.Length == 0)
            {
                return null;
            }

            try
            {
                byte[] bytesToLoad = bytes;
                int detectedWidth = 0;
                int detectedHeight = 0;

                bool shouldFixColors = IllustratorSettings.Instance == null || IllustratorSettings.Instance.FixColorChannels;
                if (shouldFixColors)
                {
                    bytesToLoad = SwapRedAndBlueInPng(bytes, out detectedWidth, out detectedHeight);
                }

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

                Debug.Print($"[Illustrator] Successfully loaded dynamic sprite: {spriteName} ({width}x{height}, colorFixed={shouldFixColors})");
                return sprite;
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] Failed to load PNG sprite {spriteName}: {ex.Message}");
                return null;
            }
        }

        private static byte[] SwapRedAndBlueInPng(byte[] pngBytes, out int outWidth, out int outHeight)
        {
            outWidth = 0;
            outHeight = 0;
            try
            {
                using (var inStream = new MemoryStream(pngBytes))
                using (var bmp = new Bitmap(inStream))
                {
                    outWidth = bmp.Width;
                    outHeight = bmp.Height;
                    var rect = new Rectangle(0, 0, outWidth, outHeight);
                    var bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                    try
                    {
                        unsafe
                        {
                            byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                            int totalBytes = bmpData.Stride * outHeight;
                            for (int i = 0; i < totalBytes; i += 4)
                            {
                                byte b = ptr[i];
                                ptr[i] = ptr[i + 2];
                                ptr[i + 2] = b;
                            }
                        }
                    }
                    finally
                    {
                        bmp.UnlockBits(bmpData);
                    }

                    using (var outStream = new MemoryStream())
                    {
                        bmp.Save(outStream, ImageFormat.Png);
                        return outStream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] SwapRedAndBlueInPng error: {ex.Message}");
                return pngBytes;
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
