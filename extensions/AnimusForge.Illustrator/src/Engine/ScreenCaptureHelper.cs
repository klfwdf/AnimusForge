using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;
using TaleWorlds.ScreenSystem;

namespace AnimusForge.Illustrator.Engine
{
    public static class ScreenCaptureHelper
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        public static string CaptureActiveWindowBase64(Rectangle? cropRect = null, int maxDimension = 768)
        {
            try
            {
                IntPtr hWnd = GetForegroundWindow();
                if (hWnd == IntPtr.Zero)
                {
                    return null;
                }

                if (!GetClientRect(hWnd, out RECT clientRect))
                {
                    if (!GetWindowRect(hWnd, out clientRect))
                    {
                        return null;
                    }
                }

                var origin = new POINT { X = clientRect.Left, Y = clientRect.Top };
                ClientToScreen(hWnd, ref origin);

                int clientWidth = clientRect.Right - clientRect.Left;
                int clientHeight = clientRect.Bottom - clientRect.Top;
                if (clientWidth <= 0 || clientHeight <= 0)
                {
                    return null;
                }

                using (var fullBmp = new Bitmap(clientWidth, clientHeight, PixelFormat.Format32bppArgb))
                {
                    using (var g = Graphics.FromImage(fullBmp))
                    {
                        g.CopyFromScreen(origin.X, origin.Y, 0, 0, new Size(clientWidth, clientHeight), CopyPixelOperation.SourceCopy);
                    }

                    Bitmap targetBmp = fullBmp;
                    bool disposeTarget = false;

                    if (cropRect.HasValue)
                    {
                        Rectangle crop = cropRect.Value;
                        crop.Intersect(new Rectangle(0, 0, clientWidth, clientHeight));
                        if (crop.Width >= 60 && crop.Height >= 60)
                        {
                            targetBmp = fullBmp.Clone(crop, fullBmp.PixelFormat);
                            disposeTarget = true;
                        }
                    }

                    try
                    {
                        return ConvertBitmapToBase64(targetBmp, maxDimension, 85);
                    }
                    finally
                    {
                        if (disposeTarget) targetBmp.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[ScreenCaptureHelper] Screen capture failed: {ex.Message}");
                return null;
            }
        }

        public static string CaptureWidgetBase64(Widget widget, int maxDimension = 768)
        {
            if (widget == null)
            {
                TaleWorlds.Library.Debug.Print("[ScreenCaptureHelper] Widget is null, falling back to full window capture.");
                return CaptureActiveWindowBase64(null, maxDimension);
            }

            try
            {
                float scale = widget.Context?.Scale ?? 1f;
                int x = (int)(widget.GlobalPosition.X * scale);
                int y = (int)(widget.GlobalPosition.Y * scale);
                int w = (int)(widget.Size.X * scale);
                int h = (int)(widget.Size.Y * scale);

                TaleWorlds.Library.Debug.Print($"[ScreenCaptureHelper] Capturing widget ({widget.GetType().Name}): pos=({x},{y}), size=({w}x{h}), scale={scale}");

                if (w > 20 && h > 20)
                {
                    string result = CaptureActiveWindowBase64(new Rectangle(x, y, w, h), maxDimension);
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        TaleWorlds.Library.Debug.Print($"[ScreenCaptureHelper] Successfully captured cropped widget base64 (length={result.Length})");
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[ScreenCaptureHelper] CaptureWidgetBase64 error: {ex.Message}");
            }

            return CaptureActiveWindowBase64(null, maxDimension);
        }

        public static Widget FindChildRecursive(Widget root, Predicate<Widget> predicate)
        {
            if (root == null || predicate == null) return null;
            if (predicate(root)) return root;

            for (int i = 0; i < root.ChildCount; i++)
            {
                var found = FindChildRecursive(root.GetChild(i), predicate);
                if (found != null) return found;
            }
            return null;
        }

        public static string TryExtractFromTopScreen(int maxDimension = 768)
        {
            try
            {
                var topScreen = ScreenManager.TopScreen;
                if (topScreen == null) return null;

                var layers = topScreen.Layers;
                if (layers != null)
                {
                    foreach (var layer in layers)
                    {
                        if (layer is GauntletLayer gl && gl.UIContext?.Root != null)
                        {
                            var candidate = FindChildRecursive(gl.UIContext.Root, w =>
                            {
                                if (w is TextureWidget tw)
                                {
                                    if (tw.Id != null && tw.Id.IndexOf("Illustration", StringComparison.OrdinalIgnoreCase) >= 0)
                                        return false;
                                    return true;
                                }
                                return false;
                            });

                            if (candidate != null)
                            {
                                string b64 = TryExtractOffscreenTextureBase64(candidate, maxDimension);
                                if (!string.IsNullOrWhiteSpace(b64))
                                {
                                    return b64;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] TryExtractFromTopScreen error: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 从 UI 控件中深度解析出底层的 3D TableauView 渲染视口
        /// </summary>
        public static TaleWorlds.Engine.TableauView ResolveTableauView(Widget widget)
        {
            if (widget == null) return null;

            try
            {
                // 1. 若为 CharacterTableauWidget 或其子类 (如 EncyclopediaCharacterTableauWidget)
                if (widget is CharacterTableauWidget ctw && ctw.TextureProvider != null)
                {
                    var view = ExtractTableauViewFromProvider(ctw.TextureProvider);
                    if (view != null) return view;
                }

                // 2. 递归查找子节点中的 CharacterTableauWidget
                var foundCharWidget = FindChildRecursive(widget, w => w is CharacterTableauWidget) as CharacterTableauWidget;
                if (foundCharWidget?.TextureProvider != null)
                {
                    var view = ExtractTableauViewFromProvider(foundCharWidget.TextureProvider);
                    if (view != null) return view;
                }

                // 3. 兜底：从 TextureWidget 的 Engine.Texture 中获取 TableauView
                var tw = widget as TextureWidget ?? FindChildRecursive(widget, w => w is TextureWidget) as TextureWidget;
                if (tw != null)
                {
                    var engTex = ExtractTextureFromWidget(tw);
                    if (engTex != null)
                    {
                        var view = engTex.TableauView;
                        if (view != null) return view;
                    }
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] ResolveTableauView error: {ex.Message}");
            }

            return null;
        }

        private static TaleWorlds.Engine.TableauView ExtractTableauViewFromProvider(TextureProvider provider)
        {
            if (provider == null) return null;
            try
            {
                var provType = provider.GetType();
                var field = provType.GetField("_characterTableau", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field != null)
                {
                    var ct = field.GetValue(provider);
                    if (ct != null)
                    {
                        var texProp = ct.GetType().GetProperty("Texture", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (texProp?.GetValue(ct) is TaleWorlds.Engine.Texture tex && tex != null)
                        {
                            return tex.TableauView;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] ExtractTableauViewFromProvider error: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 在主线程触发引擎原生安全的异步离屏渲染落盘 (利用引擎 render 同步点，杜绝任何 DirectX 11 冲突)
        /// </summary>
        public static bool TriggerTableauViewSave(TaleWorlds.Engine.TableauView tableauView, out string tempDir, out string filePrefix)
        {
            tempDir = null;
            filePrefix = null;
            if (tableauView == null) return false;

            try
            {
                tempDir = Path.Combine(Path.GetTempPath(), "AnimusForgeIllustrator");
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }

                // 清理旧的遗留临时文件
                try
                {
                    foreach (var f in Directory.GetFiles(tempDir, "af_offscreen_*"))
                    {
                        try { File.Delete(f); } catch {}
                    }
                }
                catch {}

                filePrefix = $"af_offscreen_{Guid.NewGuid():N}";
                string fileName = $"{filePrefix}.png";

                string safeDir = tempDir.TrimEnd('\\', '/') + Path.DirectorySeparatorChar;

                TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Triggering native TableauView save to: {safeDir}{fileName}");

                tableauView.SetFilePathToSaveResult(safeDir);
                tableauView.SetFileNameToSaveResult(fileName);
                tableauView.SetFileTypeToSave(TaleWorlds.Engine.View.TextureSaveFormat.TextureTypePng);
                tableauView.SetSaveFinalResultToDisk(true);

                return true;
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] TriggerTableauViewSave failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 在后台异步等待引擎在当前/下一帧 GPU 渲染完成后安全落盘出的 PNG 文件
        /// </summary>
        public static async Task<string> WaitForOffscreenFileAsync(string tempDir, string filePrefix, int timeoutMs = 450, int maxDimension = 768)
        {
            if (string.IsNullOrEmpty(tempDir) || string.IsNullOrEmpty(filePrefix)) return null;

            int waited = 0;
            string foundFile = null;

            while (waited < timeoutMs)
            {
                await Task.Delay(25).ConfigureAwait(false);
                waited += 25;

                try
                {
                    // 检查指定临时目录
                    if (Directory.Exists(tempDir))
                    {
                        var matches = Directory.GetFiles(tempDir, $"{filePrefix}*");
                        if (matches.Length > 0 && new FileInfo(matches[0]).Length > 0)
                        {
                            foundFile = matches[0];
                            break;
                        }
                    }

                    // 兜底检查 Temp 根目录 (以防引擎将路径作为相对路径处理)
                    string tempRoot = Path.GetTempPath();
                    var rootMatches = Directory.GetFiles(tempRoot, $"{filePrefix}*");
                    if (rootMatches.Length > 0 && new FileInfo(rootMatches[0]).Length > 0)
                    {
                        foundFile = rootMatches[0];
                        break;
                    }
                }
                catch
                {
                }
            }

            if (foundFile != null)
            {
                try
                {
                    // 确保文件写入完成并允许读取
                    await Task.Delay(20).ConfigureAwait(false);
                    byte[] pngBytes = File.ReadAllBytes(foundFile);
                    try { File.Delete(foundFile); } catch {}

                    if (pngBytes != null && pngBytes.Length > 0)
                    {
                        using (var ms = new MemoryStream(pngBytes))
                        using (var bmp = new Bitmap(ms))
                        {
                            // 引擎原生导出的 PNG 文件来自显卡 BGRA 渲染目标，需互换红蓝通道恢复 100% 真实肉色与服饰色彩 (杜绝金黄变蓝)
                            SwapRedAndBlueInBitmap(bmp);
                            string b64 = ConvertBitmapToBase64(bmp, maxDimension);
                            if (!string.IsNullOrWhiteSpace(b64))
                            {
                                TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Native TableauView offscreen render SUCCESS ({bmp.Width}x{bmp.Height}) in {waited}ms!");
                                return b64;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Error reading generated offscreen file: {ex.Message}");
                }
            }
            else
            {
                TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Native offscreen file not created within {timeoutMs}ms, falling back to viewport.");
            }

            return null;
        }

        public static TaleWorlds.Engine.Texture ResolveEngineTexture(Widget widget)
        {
            if (widget == null) return null;

            if (widget is TextureWidget tw)
            {
                var tex = ExtractTextureFromWidget(tw);
                if (tex != null) return tex;
            }

            var found = FindChildRecursive(widget, w => w is TextureWidget) as TextureWidget;
            if (found != null)
            {
                var tex = ExtractTextureFromWidget(found);
                if (tex != null) return tex;
            }

            return null;
        }

        private static TaleWorlds.Engine.Texture ExtractTextureFromWidget(TextureWidget tw)
        {
            if (tw == null) return null;

            var tdTex = tw.Texture;
            if (tdTex == null && tw.TextureProvider != null && tw.Context?.TwoDimensionContext != null)
            {
                try
                {
                    tdTex = tw.TextureProvider.GetTextureForRender(tw.Context.TwoDimensionContext, null);
                }
                catch
                {
                }
            }

            if (tdTex?.PlatformTexture is EngineTexture engPlatformTex && engPlatformTex.Texture != null)
            {
                return engPlatformTex.Texture;
            }

            if (tw.TextureProvider != null)
            {
                var provider = tw.TextureProvider;
                var provType = provider.GetType();

                var texProp = provType.GetProperty("Texture", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (texProp?.GetValue(provider) is TaleWorlds.Engine.Texture t1) return t1;

                var texField = provType.GetField("_texture", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (texField?.GetValue(provider) is TaleWorlds.Engine.Texture t2) return t2;

                var provTexProp = provType.GetProperty("ProvidedTexture", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (provTexProp?.GetValue(provider) is TaleWorlds.TwoDimension.Texture td1 && td1.PlatformTexture is EngineTexture et1)
                {
                    return et1.Texture;
                }

                var provTexField = provType.GetField("_providedTexture", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (provTexField?.GetValue(provider) is TaleWorlds.TwoDimension.Texture td2 && td2.PlatformTexture is EngineTexture et2)
                {
                    return et2.Texture;
                }

                var allFields = provType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                foreach (var field in allFields)
                {
                    object val = field.GetValue(provider);
                    if (val == null) continue;
                    if (val is TaleWorlds.Engine.Texture tEngine) return tEngine;

                    var valType = val.GetType();
                    var innerTexProp = valType.GetProperty("Texture", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (innerTexProp?.GetValue(val) is TaleWorlds.Engine.Texture tSub) return tSub;

                    var innerTexField = valType.GetField("_texture", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (innerTexField?.GetValue(val) is TaleWorlds.Engine.Texture tSubF) return tSubF;
                }
            }

            return null;
        }

        public static string ConvertBitmapToBase64(Bitmap srcBmp, int maxDimension = 768, int quality = 88)
        {
            if (srcBmp == null) return null;

            int targetW = srcBmp.Width;
            int targetH = srcBmp.Height;
            Bitmap targetBmp = srcBmp;
            bool disposeTarget = false;

            try
            {
                if (targetW > maxDimension || targetH > maxDimension)
                {
                    float scale = Math.Min((float)maxDimension / targetW, (float)maxDimension / targetH);
                    targetW = Math.Max(1, (int)(targetW * scale));
                    targetH = Math.Max(1, (int)(targetH * scale));

                    var scaledBmp = new Bitmap(targetW, targetH, PixelFormat.Format24bppRgb);
                    using (var g = Graphics.FromImage(scaledBmp))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                        g.DrawImage(srcBmp, 0, 0, targetW, targetH);
                    }
                    targetBmp = scaledBmp;
                    disposeTarget = true;
                }

                using (var ms = new MemoryStream())
                {
                    ImageCodecInfo encoder = GetEncoder(ImageFormat.Jpeg);
                    if (encoder != null)
                    {
                        var encoderParams = new EncoderParameters(1);
                        encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)quality);
                        targetBmp.Save(ms, encoder, encoderParams);
                    }
                    else
                    {
                        targetBmp.Save(ms, ImageFormat.Jpeg);
                    }

                    return Convert.ToBase64String(ms.ToArray());
                }
            }
            finally
            {
                if (disposeTarget) targetBmp.Dispose();
            }
        }

        public static string ConvertEngineTextureToBase64(TaleWorlds.Engine.Texture engineTexture, int maxDimension = 768)
        {
            if (engineTexture == null) return null;

            int w = engineTexture.Width;
            int h = engineTexture.Height;
            if (w < 10 || h < 10) return null;

            TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Processing Engine Texture: name={engineTexture.Name}, size=({w}x{h})");

            // 策略 1: 优先使用引擎原生 C++ 提供的 SaveToFile 导出
            // 原生引擎会在显卡内部自动分配 Staging 贴图并处理 Row Pitch 跨度与格式转换，完全杜绝内存越界崩溃
            string tempPngPath = Path.Combine(Path.GetTempPath(), $"af_offscreen_{Guid.NewGuid():N}.png");
            try
            {
                engineTexture.SaveToFile(tempPngPath, false);
                if (File.Exists(tempPngPath))
                {
                    var fileInfo = new FileInfo(tempPngPath);
                    if (fileInfo.Length > 0)
                    {
                        using (var fs = new FileStream(tempPngPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        using (var bmp = new Bitmap(fs))
                        {
                            SwapRedAndBlueInBitmap(bmp);
                            string b64 = ConvertBitmapToBase64(bmp, maxDimension);
                            if (!string.IsNullOrWhiteSpace(b64))
                            {
                                TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Native SaveToFile GPU extraction SUCCESS! Result length={b64.Length}");
                                return b64;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] SaveToFile failed: {ex.Message}, attempting safe buffer fallback...");
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPngPath))
                    {
                        File.Delete(tempPngPath);
                    }
                }
                catch
                {
                }
            }

            // 策略 2: 显卡行跨度安全对齐的 GetPixelData 缓冲回退 (向上对齐 256 字节并在外层安全保护)
            try
            {
                int bytesPerPixel = 4;
                int rawRowBytes = w * bytesPerPixel;
                int alignedPitch = ((rawRowBytes + 255) / 256) * 256;
                int safeBufferSize = Math.Max(w * h * 4, alignedPitch * h) + 65536;

                byte[] rawPixels = new byte[safeBufferSize];
                engineTexture.GetPixelData(rawPixels);

                // 检查并补齐 Alpha 通道
                bool hasAlpha = false;
                int step = Math.Max(4, (w * h * 4 / 100) & ~3);
                for (int i = 3; i < w * h * 4; i += step)
                {
                    if (rawPixels[i] > 10)
                    {
                        hasAlpha = true;
                        break;
                    }
                }
                if (!hasAlpha)
                {
                    for (int i = 3; i < w * h * 4; i += 4)
                    {
                        rawPixels[i] = 255;
                    }
                }

                using (var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
                {
                    var rect = new Rectangle(0, 0, w, h);
                    var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    try
                    {
                        int copyLen = Math.Min(rawPixels.Length, bmpData.Stride * h);
                        Marshal.Copy(rawPixels, 0, bmpData.Scan0, copyLen);
                    }
                    finally
                    {
                        bmp.UnlockBits(bmpData);
                    }

                    SwapRedAndBlueInBitmap(bmp);
                    return ConvertBitmapToBase64(bmp, maxDimension);
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Safe GetPixelData fallback failed: {ex.Message}");
            }

            return null;
        }

        public static string TryExtractOffscreenTextureBase64(Widget widget, int maxDimension = 768)
        {
            if (widget == null)
            {
                TaleWorlds.Library.Debug.Print("[OffscreenRenderer] Target widget is null, cannot perform offscreen GPU extraction.");
                return null;
            }

            try
            {
                TaleWorlds.Engine.Texture engineTexture = ResolveEngineTexture(widget);
                if (engineTexture != null)
                {
                    return ConvertEngineTextureToBase64(engineTexture, maxDimension);
                }
                else
                {
                    TaleWorlds.Library.Debug.Print("[OffscreenRenderer] No valid Engine Texture found on target widget.");
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Offscreen texture extraction failed: {ex.Message}");
            }

            return null;
        }

        public static string ExtractHeroOffscreenBase64(Hero hero, Widget tableauWidget, int maxDimension = 768)
        {
            if (hero == null && tableauWidget == null) return null;

            // 1. 优先尝试从现场正在活跃渲染的 TableauWidget 提取
            if (tableauWidget != null)
            {
                TaleWorlds.Engine.Texture tex = ResolveEngineTexture(tableauWidget);
                if (tex != null)
                {
                    string b64 = ConvertEngineTextureToBase64(tex, maxDimension);
                    if (!string.IsNullOrWhiteSpace(b64))
                    {
                        TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Extracted hero 3D tableau texture for {hero?.Name}");
                        return b64;
                    }
                }
            }

            // 2. 尝试从原版 ThumbnailCacheManager 获取离屏烘焙缩略图
            if (hero?.CharacterObject != null)
            {
                try
                {
                    var cacheMgr = ThumbnailCacheManager.Current;
                    if (cacheMgr != null)
                    {
                        CharacterCode code = CharacterCode.CreateFrom(hero.CharacterObject);
                        if (code != null)
                        {
                            TaleWorlds.Engine.Texture cachedTex = null;
                            var creationData = new CharacterThumbnailCreationData(code, t => cachedTex = t, () => { }, true, maxDimension, maxDimension);
                            cacheMgr.CreateTexture(creationData);

                            if (cachedTex != null)
                            {
                                string b64 = ConvertEngineTextureToBase64(cachedTex, maxDimension);
                                if (!string.IsNullOrWhiteSpace(b64))
                                {
                                    TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Extracted hero thumbnail texture from ThumbnailCacheManager for {hero.Name}");
                                    return b64;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] ThumbnailCache extraction error: {ex.Message}");
                }
            }

            return null;
        }

        public static string ExtractItemOffscreenBase64(ItemObject item, int maxDimension = 512)
        {
            if (item == null) return null;

            try
            {
                var cacheMgr = ThumbnailCacheManager.Current;
                if (cacheMgr != null)
                {
                    TaleWorlds.Engine.Texture cachedTex = null;
                    var creationData = new ItemThumbnailCreationData(item, "", t => cachedTex = t, () => { });
                    cacheMgr.CreateTexture(creationData);

                    if (cachedTex != null)
                    {
                        return ConvertEngineTextureToBase64(cachedTex, maxDimension);
                    }
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Item thumbnail extraction error: {ex.Message}");
            }

            return null;
        }

        public static void SwapRedAndBlueInBitmap(Bitmap bmp)
        {
            if (bmp == null) return;
            try
            {
                var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                var bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                try
                {
                    unsafe
                    {
                        byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                        int totalBytes = bmpData.Stride * bmp.Height;
                        for (int i = 0; i < totalBytes; i += 4)
                        {
                            byte temp = ptr[i];
                            ptr[i] = ptr[i + 2];
                            ptr[i + 2] = temp;
                        }
                    }
                }
                finally
                {
                    bmp.UnlockBits(bmpData);
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[ScreenCaptureHelper] SwapRedAndBlueInBitmap error: {ex.Message}");
            }
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}
