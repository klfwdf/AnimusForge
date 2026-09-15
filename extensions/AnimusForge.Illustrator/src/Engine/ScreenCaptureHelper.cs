using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.ObjectSystem;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;
using TaleWorlds.ScreenSystem;

namespace AnimusForge.Illustrator.Engine
{
    public static class ScreenCaptureHelper
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

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

                GetWindowThreadProcessId(hWnd, out uint windowPid);
                if (windowPid != (uint)Process.GetCurrentProcess().Id)
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
                // 1. 若为 Tableau 控件或其子类 (如 EncyclopediaCharacterTableauWidget / BannerTableauWidget)
                if (widget is TextureWidget twTop && twTop.TextureProvider != null &&
                    (widget is CharacterTableauWidget || widget is BannerTableauWidget))
                {
                    var view = ExtractTableauViewFromProvider(twTop.TextureProvider);
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
                // 泛化扫描 provider 实例字段：CharacterTableauTextureProvider 是 _characterTableau，
                // BannerTableauTextureProvider 是 _bannerTableau——统一取含 Texture 属性的字段值。
                var fields = provType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                foreach (var field in fields)
                {
                    object val = field.GetValue(provider);
                    if (val == null) continue;
                    var texProp = val.GetType().GetProperty("Texture", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (texProp?.GetValue(val) is TaleWorlds.Engine.Texture tex && tex != null)
                    {
                        return tex.TableauView;
                    }
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] ExtractTableauViewFromProvider error: {ex.Message}");
            }
            return null;
        }

        private static int _offscreenCleanupStarted;

        private static void CleanupStaleOffscreenFiles(string tempDir)
        {
            if (Interlocked.Exchange(ref _offscreenCleanupStarted, 1) != 0) return;
            try
            {
                DateTime threshold = DateTime.UtcNow.AddDays(-1);
                foreach (string file in Directory.EnumerateFiles(tempDir, "af_offscreen_*"))
                {
                    try
                    {
                        if (File.GetLastWriteTimeUtc(file) < threshold) File.Delete(file);
                    }
                    catch { }
                }
            }
            catch { }
        }

        /// <summary>
        /// 只清理调用方已消费完毕的离屏导出前缀。禁止全局通配符，避免误删其他请求。
        /// </summary>
        public static void CleanupTempArtifacts(string tempDir, string filePrefix)
        {
            if (string.IsNullOrWhiteSpace(tempDir) || string.IsNullOrWhiteSpace(filePrefix) ||
                !filePrefix.StartsWith("af_offscreen_", StringComparison.Ordinal) ||
                !Guid.TryParseExact(filePrefix.Substring("af_offscreen_".Length), "N", out _)) return;
            try
            {
                string tempRoot = Path.GetTempPath();
                int deleted = 0;

                foreach (string dir in new[] { tempDir, tempRoot })
                {
                    if (!Directory.Exists(dir)) continue;
                    foreach (string file in Directory.EnumerateFiles(dir, filePrefix + "*"))
                    {
                        try { File.Delete(file); deleted++; } catch { }
                    }
                }

                if (deleted > 0)
                {
                    TaleWorlds.Library.Debug.Print($"[Illustrator] Auto-cleaned {deleted} temp artifact(s).");
                }
            }
            catch { }
        }

        /// <summary>
        /// 在主线程请求原生异步落盘；落盘不构成场景资源释放的渲染同步屏障。
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

                CleanupStaleOffscreenFiles(tempDir);

                filePrefix = $"af_offscreen_{Guid.NewGuid():N}";
                string fileName = $"{filePrefix}.png";

                string safeDir = tempDir.TrimEnd('\\', '/') + Path.DirectorySeparatorChar;

                TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Triggering native TableauView save to: {safeDir}{fileName}");

                tableauView.SetFilePathToSaveResult(safeDir);
                TaleWorlds.Library.Debug.Print("[OffscreenRenderer] save path set");
                tableauView.SetFileNameToSaveResult(fileName);
                TaleWorlds.Library.Debug.Print("[OffscreenRenderer] save name set");
                tableauView.SetFileTypeToSave(TaleWorlds.Engine.View.TextureSaveFormat.TextureTypePng);
                TaleWorlds.Library.Debug.Print("[OffscreenRenderer] save type set");
                tableauView.SetSaveFinalResultToDisk(true);
                TaleWorlds.Library.Debug.Print("[OffscreenRenderer] save flag set");

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
        public static async Task<string> WaitForOffscreenFileAsync(string tempDir, string filePrefix, int timeoutMs = 450, int maxDimension = 768, bool cleanTempFiles = false)
        {
            if (string.IsNullOrEmpty(tempDir) || string.IsNullOrEmpty(filePrefix)) return null;
            try
            {
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
            finally
            {
                if (cleanTempFiles) CleanupTempArtifacts(tempDir, filePrefix);
            }
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

            // 渲染目标纹理（纹章/缩略图缓存/地图铭牌产出的 GPU RenderTarget）不能被 CPU 直接读取，
            // 对其调用 SaveToFile/GetPixelData 会在原生层崩溃（托管 try/catch 拦截不了）；
            // TransformRenderTargetToResource 实测也救不了（转换延迟到渲染同步点，且不能动共享纹理）。
            // RT 一律跳过：调用方如需取像素必须走 TableauView 异步落盘通道。
            bool isRenderTarget = false;
            try
            {
                isRenderTarget = engineTexture.IsRenderTarget;
            }
            catch
            {
                isRenderTarget = true;
            }
            if (isRenderTarget)
            {
                TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Skipping GPU render-target texture read: name={engineTexture.Name}");
                return null;
            }

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
                // 宽缓冲：部分引擎纹理可能为 16F/8BPP 等更高位深格式，缓冲按 8BPP 上限分配，避免原生层写出界
                int bytesPerPixel = 4;
                int rawRowBytes = w * bytesPerPixel;
                int alignedPitch = ((rawRowBytes + 255) / 256) * 256;
                int safeBufferSize = Math.Max(w * h * 8, alignedPitch * h) + 65536;

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
            if (!Core.IllustratorRuntime.IsMainThread) return null; // 引擎对象只能在游戏主线程访问
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

        /// <summary>
        /// 将一段引擎操作调度到游戏主线程执行并返回其结果（调用方在任意线程均安全）。
        /// </summary>
        private static async Task<T> RunOnGameThreadAsync<T>(Func<T> work, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Core.IllustratorRuntime.IsMainThread) return work();
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            int admission = 0; // 0 queued, 1 executing, 2 cancelled before admission.
            using (cancellationToken.Register(() =>
            {
                if (Interlocked.CompareExchange(ref admission, 2, 0) == 0) tcs.TrySetCanceled();
            }))
            {
                if (!Core.IllustratorRuntime.Post(() =>
                {
                    if (Interlocked.CompareExchange(ref admission, 1, 0) != 0) return;
                    // Once admitted, report completion only after work returns. A
                    // cancelled caller must still receive ownership of a created stage.
                    try { tcs.TrySetResult(work()); }
                    catch (Exception ex) { tcs.TrySetException(ex); }
                })) return default;
                return await tcs.Task.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 离屏舞台泵状态：临时 Gauntlet 层内的 Tableau 控件由游戏自身 UI 管线创建并 tick
        /// （渲染时机与原版控件完全一致），本类只负责在预热后设落盘标志、等 PNG、拆舞台。
        /// 绝不自建 Scene/TableauView——手动 OnTick 与渲染线程无同步屏障，已证实会原生崩溃。
        /// </summary>
        private sealed class StageViewModel : TaleWorlds.Library.ViewModel
        {
        }

        private sealed class OffscreenStagePump
        {
            public ScreenBase Screen;
            public GauntletLayer Layer;
            public GauntletMovieIdentifier Movie;
            public Widget Widget;
            public int WarmupTicks;
            public int MaxTicks;
            public int Ticks;
            public bool SaveRequested;
            public string Dir;
            public string Prefix;
            public string SeenPath;
            public long SeenLength;
            public int SeenTick;
            public CancellationToken CancellationToken;
            public int Finished;
            public int CancelRequested;
            public readonly TaskCompletionSource<bool> Retired = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource<string> Done;
        }

        private static string FindOffscreenFile(string dir, string prefix)
        {
            if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(prefix)) return null;
            try
            {
                var matches = Directory.GetFiles(dir, prefix + "*");
                if (matches.Length > 0 && new FileInfo(matches[0]).Length > 0) return matches[0];
            }
            catch { }
            try
            {
                var matches = Directory.GetFiles(Path.GetTempPath(), prefix + "*");
                if (matches.Length > 0 && new FileInfo(matches[0]).Length > 0) return matches[0];
            }
            catch { }
            return null;
        }

        /// <summary>
        /// 单帧泵步（仅游戏主线程执行）：预热帧后解析舞台控件的 TableauView 并请求落盘 →
        /// 文件大小至少稳定 50ms 后完成；同帧可执行多次队列任务，不能仅按观察次数判断。
        /// </summary>
        private static void PumpOffscreenStage(OffscreenStagePump pump)
        {
            if (pump == null || Volatile.Read(ref pump.Finished) != 0) return;
            try
            {
                if (Volatile.Read(ref pump.CancelRequested) != 0 || pump.CancellationToken.IsCancellationRequested || !ReferenceEquals(ScreenManager.TopScreen, pump.Screen) || pump.Screen.IsFinalized)
                {
                    FinishStage(pump);
                    pump.Done.TrySetCanceled();
                    return;
                }

                pump.Ticks++;
                bool nativeBannerReady = !(pump.Widget is NativeBannerExportWidget nativeBanner) || nativeBanner.ReadyForExport;
                if (!pump.SaveRequested && pump.Ticks > pump.WarmupTicks && nativeBannerReady)
                {
                    var view = ResolveTableauView(pump.Widget);
                    if (view != null && TriggerTableauViewSave(view, out pump.Dir, out pump.Prefix))
                    {
                        pump.SaveRequested = true;
                        TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Stage save requested at tick={pump.Ticks}, waiting for file...");
                    }
                    else if (pump.Ticks == pump.MaxTicks)
                    {
                        var tw = pump.Widget as TextureWidget;
                        TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Stage never resolved view (provider={(tw?.TextureProvider != null)})");
                    }
                }

                if (pump.SaveRequested)
                {
                    string path = FindOffscreenFile(pump.Dir, pump.Prefix);
                    if (path != null)
                    {
                        long length = new FileInfo(path).Length;
                        if (path == pump.SeenPath && length > 0 && length == pump.SeenLength)
                        {
                            if (unchecked((uint)(Environment.TickCount - pump.SeenTick)) >= 50)
                            {
                                FinishStage(pump);
                                pump.Done.TrySetResult(path);
                                return;
                            }
                        }
                        else
                        {
                            pump.SeenPath = path;
                            pump.SeenLength = length;
                            pump.SeenTick = Environment.TickCount;
                        }
                    }
                }

                if (pump.Ticks >= pump.MaxTicks || !Core.IllustratorRuntime.Post(() => PumpOffscreenStage(pump)))
                {
                    FinishStage(pump);
                    pump.Done.TrySetResult(null);
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Stage pump error: {ex.Message}");
                FinishStage(pump);
                pump.Done.TrySetResult(null);
            }
        }

        private static OffscreenStagePump _activeStage;

        internal static void CancelActiveStage()
        {
            Core.IllustratorRuntime.AssertMainThread();
            var pump = _activeStage;
            FinishStage(pump);
            pump?.Done.TrySetCanceled();
        }

        private static void FinishStage(OffscreenStagePump pump)
        {
            if (pump == null || Interlocked.Exchange(ref pump.Finished, 1) != 0) return;
            if (ReferenceEquals(_activeStage, pump)) _activeStage = null;
            TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Retiring stage: widget={pump.Widget?.Id}, saveRequested={pump.SaveRequested}, ticks={pump.Ticks}");
            try
            {
                if (pump.Movie != null) pump.Layer?.ReleaseMovie(pump.Movie);
            }
            catch (Exception ex) { TaleWorlds.Library.Debug.Print("[OffscreenRenderer] ReleaseMovie failed: " + ex); }
            try
            {
                if (pump.Layer != null) pump.Screen?.RemoveLayer(pump.Layer);
            }
            catch (Exception ex) { TaleWorlds.Library.Debug.Print("[OffscreenRenderer] RemoveLayer failed: " + ex); }
            pump.Retired.TrySetResult(true);
            TaleWorlds.Library.Debug.Print("[OffscreenRenderer] Stage retirement completed");
        }

        private static Task RetireStageAsync(OffscreenStagePump pump)
        {
            if (pump == null) return Task.CompletedTask;
            Interlocked.Exchange(ref pump.CancelRequested, 1);
            if (Volatile.Read(ref pump.Finished) == 0)
            {
                if (Core.IllustratorRuntime.IsMainThread) FinishStage(pump);
                else Core.IllustratorRuntime.Post(() => FinishStage(pump));
                // If the bounded queue is full, the already queued pump observes
                // CancelRequested. Reset also retires it even when ticks stop.
            }
            return pump.Retired.Task;
        }

        /// <summary>
        /// 后台读已落盘的 PNG：BGRA→RGB 通道互换 + 缩放 JPEG base64，读取后删除临时文件。
        /// </summary>
        private static async Task<string> ReadOffscreenPngBase64(string path, int maxDimension, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(path)) return null;
            try
            {
                await Task.Delay(20, cancellationToken).ConfigureAwait(false);
                byte[] pngBytes = File.ReadAllBytes(path);
                if (pngBytes == null || pngBytes.Length == 0) return null;
                using (var ms = new MemoryStream(pngBytes))
                using (var bmp = new Bitmap(ms))
                {
                    SwapRedAndBlueInBitmap(bmp);
                    return ConvertBitmapToBase64(bmp, maxDimension);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Read offscreen png failed: {ex.Message}");
                return null;
            }
            finally
            {
                // 消费者结束后才删除自身文件；取消和解码失败也走相同归属边界。
                try { File.Delete(path); } catch { }
            }
        }

        /// <summary>
        /// 全局舞台串行锁：同一时刻只允许一个离屏舞台存在。多个舞台并发时，一个舞台的
        /// ReleaseMovie/RemoveLayer 会与另一个舞台的原生 PNG 落盘在渲染线程上撞车（已实锤崩溃），
        /// 且该崩溃发生在原生层、托管 try/catch 接不住。串行代价是参考图多约一秒。
        /// </summary>
        private static readonly SemaphoreSlim _stageLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// 通用离屏舞台提取：临时挂一个 Gauntlet 层（透明控件位于可渲染区域、IsVisible=true），
        /// 游戏自身 UI 管线创建并渲染 TableauView，预热后设落盘标志，后台等 PNG。
        /// 与原版控件同一条渲染路径——不手动 tick 场景、不读渲染目标纹理像素。
        /// </summary>
        private static async Task<string> ExtractViaStageAsync(string widgetId, Action<Widget> configure, int warmupTicks, int maxTicks, int timeoutMs, CancellationToken cancellationToken, bool cleanTempFiles)
        {
            await _stageLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            OffscreenStagePump pump = null;
            bool delivered = false;
            try
            {
                var done = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                bool started = await RunOnGameThreadAsync(() =>
                {
                GauntletLayer layer = null;
                GauntletMovieIdentifier movie = null;
                try
                {
                    var top = ScreenManager.TopScreen;
                    if (top == null)
                    {
                        TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Stage '{widgetId}' aborted: no top screen");
                        return false;
                    }
                    // 优先级取最低：当前屏幕的正常 UI（百科页/会话面板/周报弹窗等不透明层）画在它上面，
                    // 盖住舞台控件——AlphaFactor 对 Tableau 自绘纹理可能不生效（实机闪过），靠层级遮挡兜底
                    layer = new GauntletLayer("IllustratorOffscreenStage", 1, false);
                    movie = layer.LoadMovie("IllustratorOffscreenStage", new StageViewModel());
                    var root = movie?.Movie?.RootWidget;
                    if (root == null)
                    {
                        TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Stage '{widgetId}' aborted: prefab not loaded or empty root");
                        try { if (movie != null) layer.ReleaseMovie(movie); } catch { }
                        return false;
                    }
                    Widget widget;
                    if (widgetId == "NativeBannerExport")
                    {
                        if (!NativeBannerExportWidget.SupportsDeferredSceneClear)
                            throw new NotSupportedException("Native banner deferred scene cleanup is unavailable on this runtime.");
                        // This child must stay in the normal render area so Gauntlet
                        // ticks its provider. Its OnRender never submits a screen draw.
                        widget = new NativeBannerExportWidget(root.Context)
                        {
                            Id = widgetId,
                            WidthSizePolicy = SizePolicy.Fixed, HeightSizePolicy = SizePolicy.Fixed,
                            SuggestedWidth = 512, SuggestedHeight = 512,
                            IsVisible = true, DoNotAcceptEvents = true
                        };
                        root.AddChild(widget);
                    }
                    else widget = FindChildRecursive(root, w => w.Id == widgetId);
                    if (widget == null)
                    {
                        TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Stage '{widgetId}' aborted: widget not found in prefab");
                        try { layer.ReleaseMovie(movie); } catch { }
                        return false;
                    }
                    configure(widget);
                    pump = new OffscreenStagePump
                    {
                        Screen = top,
                        Layer = layer,
                        Movie = movie,
                        Widget = widget,
                        WarmupTicks = warmupTicks,
                        MaxTicks = maxTicks,
                        CancellationToken = cancellationToken,
                        Done = done
                    };
                    _activeStage = pump;
                    top.AddLayer(layer);
                    if (!Core.IllustratorRuntime.Post(() => PumpOffscreenStage(pump)))
                    {
                        TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Stage '{widgetId}' aborted: main-thread queue rejected pump");
                        FinishStage(pump);
                        return false;
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Stage create failed: {ex.Message}");
                    if (pump != null) FinishStage(pump);
                    else try { if (movie != null) layer?.ReleaseMovie(movie); } catch { }
                    return false;
                }
            }, cancellationToken).ConfigureAwait(false);
            if (!started) return null;

            var winner = await Task.WhenAny(done.Task, Task.Delay(timeoutMs, cancellationToken)).ConfigureAwait(false);
            if (winner == done.Task)
            {
                string path = await done.Task.ConfigureAwait(false);
                delivered = !string.IsNullOrEmpty(path);
                return path;
            }
            TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Stage '{widgetId}' timed out after {timeoutMs}ms");
            cancellationToken.ThrowIfCancellationRequested();
            return null;
            }
            finally
            {
                // Never release serialization before native UI retirement finishes.
                await RetireStageAsync(pump).ConfigureAwait(false);
                if (!delivered && cleanTempFiles && pump != null)
                    CleanupTempArtifacts(pump.Dir, pump.Prefix);
                _stageLock.Release();
            }
        }

        // Full native banner render, never a texture CPU read or atlas reconstruction.
        internal static async Task<byte[]> RenderNativeBannerPngAsync(string bannerCode, int size, bool cleanTempFiles, CancellationToken token)
        {
            string path = null;
            try
            {
                path = await ExtractViaStageAsync("NativeBannerExport", widget =>
                {
                    if (!Banner.IsValidBannerCode(bannerCode)) throw new ArgumentException("Invalid banner code.");
                    var banner = (NativeBannerExportWidget)widget;
                    banner.SuggestedWidth = size;
                    banner.SuggestedHeight = size;
                    banner.IsNineGrid = true; // Full banner canvas, not the UI's center-third crop.
                    banner.BannerCodeText = bannerCode;
                }, warmupTicks: 12, maxTicks: 360, timeoutMs: 6000, cancellationToken: token, cleanTempFiles: cleanTempFiles).ConfigureAwait(false);
                if (string.IsNullOrEmpty(path)) return null;
                // The stage already waited for a stable file. Retry reading only
                // at the consumer if the native writer still held the file briefly.
                for (int attempt = 0; attempt < 4; attempt++)
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        var info = new FileInfo(path);
                        if (info.Length > 16 * 1024 * 1024) return null;
                        byte[] bytes = File.ReadAllBytes(path);
                        token.ThrowIfCancellationRequested();
                        if (bytes.Length > 0)
                        {
                            TaleWorlds.Library.Debug.Print($"[NativeBanner] Reading rendered PNG for reference: bytes={bytes.Length}");
                            return bytes;
                        }
                    }
                    catch (IOException) { }
                    await Task.Delay(50, token).ConfigureAwait(false);
                }
                return null;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print("[NativeBanner] Render/export failed: " + ex.Message);
                return null;
            }
            finally
            {
                if (cleanTempFiles && path != null) try { File.Delete(path); } catch { }
            }
        }

        /// <summary>
        /// 离屏渲染指定英雄的真实 3D 立绘（真实体型、五官、发型、装备、家族纹章底色）。
        /// 人物资源加载需要更多预热帧，故 warmup 比纹章长。
        /// </summary>
        public static async Task<string> ExtractHeroPortraitOffscreenAsync(Hero hero, bool useCivilian = false, int maxDimension = 512, int timeoutMs = 3500, CancellationToken cancellationToken = default, bool cleanTempFiles = false)
        {
            if (hero == null) return null;
            string heroName = string.Empty;
            try
            {
                string path = await ExtractViaStageAsync("OffscreenCharacter", widget =>
                {
                    if (widget is CharacterTableauWidget cw)
                    {
                        var character = hero.CharacterObject ?? throw new InvalidOperationException("Hero character is unavailable.");
                        var equipment = useCivilian ? hero.CivilianEquipment : hero.BattleEquipment;
                        heroName = hero.Name?.ToString() ?? hero.StringId;
                        cw.BodyProperties = character.GetBodyProperties(equipment ?? character.Equipment, -1).ToString();
                        cw.IsFemale = hero.IsFemale;
                        cw.Race = character.Race;
                        cw.StanceIndex = 0;
                        string equipmentCode = equipment?.CalculateEquipmentCode();
                        if (!string.IsNullOrEmpty(equipmentCode))
                        {
                            cw.EquipmentCode = equipmentCode;
                        }
                        if (hero.ClanBanner != null)
                        {
                            cw.BannerCodeText = hero.ClanBanner.BannerCode;
                        }
                        cw.ArmorColor1 = hero.MapFaction?.Color ?? 0;
                        cw.ArmorColor2 = hero.MapFaction?.Color2 ?? 0;
                        cw.IsVisible = true;
                    }
                }, warmupTicks: 20, maxTicks: 240, timeoutMs: timeoutMs, cancellationToken: cancellationToken, cleanTempFiles: cleanTempFiles).ConfigureAwait(false);
                string b64 = await ReadOffscreenPngBase64(path, maxDimension, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(b64))
                {
                    TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Hero portrait stage render extracted for {heroName} ({b64.Length} chars)");
                }
                return b64;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Hero portrait offscreen error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 离屏渲染非英雄 CharacterObject（要人、酒馆店主等没有 Hero 对象的对话方）的真实 3D 立绘。
        /// </summary>
        public static async Task<string> ExtractCharacterPortraitOffscreenAsync(CharacterObject character, int maxDimension = 512, int timeoutMs = 3500, CancellationToken cancellationToken = default, string bodyProperties = null, bool cleanTempFiles = false)
        {
            if (character == null) return null;
            string charName = string.Empty;
            try
            {
                string path = await ExtractViaStageAsync("OffscreenCharacter", widget =>
                {
                    if (widget is CharacterTableauWidget cw)
                    {
                        var equipment = character.Equipment ?? character.FirstBattleEquipment;
                        charName = character.Name?.ToString() ?? character.StringId;
                        // 优先使用会话在场 Agent 的真实 BodyProperties（劫匪等随机 NPC 的实际脸），
                        // 否则退回兵种模板体型（模板脸型范围内重新随机）
                        cw.BodyProperties = !string.IsNullOrWhiteSpace(bodyProperties)
                            ? bodyProperties
                            : character.GetBodyProperties(equipment, -1).ToString();
                        cw.IsFemale = character.IsFemale;
                        cw.Race = character.Race;
                        cw.StanceIndex = 0;
                        string equipmentCode = equipment?.CalculateEquipmentCode();
                        if (!string.IsNullOrEmpty(equipmentCode))
                        {
                            cw.EquipmentCode = equipmentCode;
                        }
                        cw.IsVisible = true;
                    }
                }, warmupTicks: 20, maxTicks: 240, timeoutMs: timeoutMs, cancellationToken: cancellationToken, cleanTempFiles: cleanTempFiles).ConfigureAwait(false);
                string b64 = await ReadOffscreenPngBase64(path, maxDimension, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(b64))
                {
                    TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Character portrait stage render extracted for {charName} ({b64.Length} chars)");
                }
                return b64;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[OffscreenRenderer] Character portrait offscreen error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 截取游戏窗口中 3D 场景主体区域（去除底部对话 UI 条带），用作会面场景实景参考图。
        /// topBandFraction 必须避开原会话界面的名牌/字幕条（约自 55% 高度起），默认 0.5。
        /// 该图仅供导演识图，不进生图模型，避免截图质感与 UI 文字被复制进成图。
        /// </summary>
        public static string CaptureConversationSceneBase64(int maxDimension = 768, float topBandFraction = 0.5f)
        {
            try
            {
                IntPtr hWnd = GetForegroundWindow();
                if (hWnd == IntPtr.Zero) return null;
                GetWindowThreadProcessId(hWnd, out uint windowPid);
                if (windowPid != (uint)Process.GetCurrentProcess().Id) return null;
                if (!GetClientRect(hWnd, out RECT clientRect)) return null;

                int clientWidth = clientRect.Right - clientRect.Left;
                int clientHeight = clientRect.Bottom - clientRect.Top;
                if (clientWidth <= 0 || clientHeight <= 0) return null;

                int bandHeight = (int)(clientHeight * topBandFraction);
                if (bandHeight < 120) bandHeight = clientHeight;
                return CaptureActiveWindowBase64(new Rectangle(0, 0, clientWidth, bandHeight), maxDimension);
            }
            catch
            {
                return CaptureActiveWindowBase64(null, maxDimension);
            }
        }

        public static string ExtractItemOffscreenBase64(ItemObject item, int maxDimension = 512)
        {
            if (!Core.IllustratorRuntime.IsMainThread) return null;
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
