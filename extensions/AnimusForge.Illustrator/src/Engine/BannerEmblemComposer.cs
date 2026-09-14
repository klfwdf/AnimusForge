using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AnimusForge.Illustrator.Core;
using TaleWorlds.Core;
using BannerlordEngineTexture = TaleWorlds.Engine.Texture;

namespace AnimusForge.Illustrator.Engine
{
    /// <summary>
    /// 纯托管家族纹章合成器：不走任何舞台/控件/渲染管线，零可见性零闪屏。
    /// 把 Banner.BannerDataList 当"合成配方"——每枚纹章的图集纹理名（GetIconSourceTextureName）、
    /// 格位（BannerIconData.TextureIndex）、调色板颜色（BannerManager.GetColor）、位置/大小/
    /// 旋转/镜像（BannerData，坐标空间 Banner.BannerFullSize=1528）——读出后用 GDI+ 叠放，
    /// 产出像素级准确的真纹章 PNG。
    /// 引擎纹理/ BannerManager 访问统一在游戏主线程执行。
    /// </summary>
    internal static class BannerEmblemComposer
    {
        // custom_banner_icons_XX 图集为 4x4 网格（texture_index 0-15，行优先）
        private const int AtlasGridSize = 4;
        private static readonly ConcurrentDictionary<string, Bitmap> AtlasCache = new ConcurrentDictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);

        /// <summary>将家族旗帜代码合成为 PNG base64；失败/无效输入返回 null（降级静默）。</summary>
        public static Task<string> ComposeToBase64Async(string bannerCode, int canvasSize = 512)
        {
            if (string.IsNullOrWhiteSpace(bannerCode)) return Task.FromResult<string>(null);
            if (IllustratorRuntime.IsMainThread)
            {
                return Task.FromResult(ComposeOnGameThread(bannerCode, canvasSize));
            }
            var tcs = new TaskCompletionSource<string>();
            if (!IllustratorRuntime.Post(() =>
                {
                    try { tcs.TrySetResult(ComposeOnGameThread(bannerCode, canvasSize)); }
                    catch (Exception ex) { tcs.TrySetException(ex); }
                }))
            {
                tcs.TrySetResult(null);
            }
            return tcs.Task;
        }

        private static string ComposeOnGameThread(string bannerCode, int canvasSize)
        {
            try
            {
                var manager = BannerManager.Instance;
                if (manager == null) return null;

                Banner banner;
                try { banner = new Banner(bannerCode); }
                catch { return null; }
                if (banner == null || banner.IsBannerDataListEmpty()) return null;

                int count = banner.GetBannerDataListCount();
                if (count <= 0) return null;
                float full = Math.Max(Banner.BannerFullSize, 1f);
                float scale = canvasSize / full;

                var canvas = new Bitmap(canvasSize, canvasSize, PixelFormat.Format32bppArgb);
                try
                {
                    using (var g = Graphics.FromImage(canvas))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.Clear(Color.Transparent);

                        // 背景件（BannerDataList[0]）：优先尝试图集图样（背景若走同一图集管线则
                        // 按双通道掩码着色，得到真实两色底纹），否则退化为纯色底。
                        var bg = banner.GetBannerDataAtIndex(Banner.BackgroundDataIndex);
                        if (bg != null)
                        {
                            FillBackground(g, canvasSize, bg);
                        }

                        // 纹章件按列表顺序叠放（背景在 0 位，其后即绘制顺序）
                        string dbgDir = DebugDumpDir();
                        var meta = new System.Text.StringBuilder();
                        for (int i = 0; i < count; i++)
                        {
                            if (i == Banner.BackgroundDataIndex) continue;
                            var data = banner.GetBannerDataAtIndex(i);
                            if (data == null) continue;

                            Bitmap cell = LoadIconCell(data.MeshId);
                            if (cell == null) continue;

                            Color c1 = PaletteColor(data.ColorId, Color.White);
                            Color c2 = data.ColorId2 >= 0 ? PaletteColor(data.ColorId2, c1) : c1;
                            meta.AppendLine($"mesh={data.MeshId} pos=({data.Position.X:0},{data.Position.Y:0}) size=({data.Size.X:0}x{data.Size.Y:0}) rot={data.Rotation * 57.29578f:0}° mirror={data.Mirror} c1=#{c1.R:X2}{c1.G:X2}{c1.B:X2} c2=#{c2.R:X2}{c2.G:X2}{c2.B:X2} stroke={data.DrawStroke} cell={cell.Width}x{cell.Height}");
                            using (cell)
                            using (var tinted = TintIconCell(cell, c1, c2, data.DrawStroke))
                            {
                                DrawPiece(g, tinted, data, scale, canvasSize);
                                if (dbgDir != null)
                                {
                                    try
                                    {
                                        cell.Save(Path.Combine(dbgDir, $"cell_{data.MeshId}_raw.png"), ImageFormat.Png);
                                        tinted.Save(Path.Combine(dbgDir, $"cell_{data.MeshId}_tinted.png"), ImageFormat.Png);
                                    }
                                    catch { }
                                }
                            }
                        }
                        if (dbgDir != null)
                        {
                            try
                            {
                                canvas.Save(Path.Combine(dbgDir, "emblem_final.png"), ImageFormat.Png);
                                File.WriteAllText(Path.Combine(dbgDir, "meta.txt"), meta.ToString());
                            }
                            catch { }
                        }
                    }
                    string b64 = ScreenCaptureHelper.ConvertBitmapToBase64(canvas, canvasSize);
                    TaleWorlds.Library.Debug.Print($"[BannerEmblem] Composed emblem from managed data ({(b64?.Length ?? 0)} chars, pieces={count})");
                    return b64;
                }
                finally
                {
                    canvas.Dispose();
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[BannerEmblem] Compose error: {ex.Message}");
                return null;
            }
        }

        /// <summary>调试落盘目录：temp/AnimusForgeIllustrator/banner_debug，供实机后人工核对合成结果。</summary>
        private static string DebugDumpDir()
        {
            try
            {
                string dir = Path.Combine(Path.GetTempPath(), "AnimusForgeIllustrator", "banner_debug");
                Directory.CreateDirectory(dir);
                return dir;
            }
            catch { return null; }
        }

        /// <summary>加载指定纹章图标的图集切片；图集纹理按名缓存（生成频率极低，常驻无压力）。</summary>
        private static Bitmap LoadIconCell(int meshId)
        {
            try
            {
                var manager = BannerManager.Instance;
                BannerIconData? iconData = null;
                string texName = null;
                try { iconData = manager?.GetIconDataFromIconId(meshId); } catch { }
                try { texName = manager?.GetIconSourceTextureName(meshId); } catch { }
                if (iconData == null)
                {
                    TaleWorlds.Library.Debug.Print($"[BannerEmblem] mesh={meshId} has no BannerIconData (bg pattern?)");
                    return null;
                }
                string materialName = iconData.Value.MaterialName;
                TaleWorlds.Library.Debug.Print($"[BannerEmblem] mesh={meshId} texIdx={iconData.Value.TextureIndex} srcTex='{texName}' material='{materialName}'");

                string cacheKey = !string.IsNullOrWhiteSpace(materialName) ? materialName : texName;
                if (string.IsNullOrWhiteSpace(cacheKey))
                {
                    TaleWorlds.Library.Debug.Print($"[BannerEmblem] mesh={meshId} no resolvable texture/material name, skipped");
                    return null;
                }
                // 不缓存失败结果：GetOrAdd 会把 null 永久缓存，首次加载失败后本局永远无法重试
                Bitmap atlas;
                if (!AtlasCache.TryGetValue(cacheKey, out atlas))
                {
                    atlas = LoadAtlasBitmap(texName, materialName);
                    if (atlas != null) AtlasCache.TryAdd(cacheKey, atlas);
                }
                if (atlas == null) return null;

                int cellW = atlas.Width / AtlasGridSize;
                int cellH = atlas.Height / AtlasGridSize;
                if (cellW <= 0 || cellH <= 0) return null;
                int idx = Math.Max(0, iconData.Value.TextureIndex);
                int col = idx % AtlasGridSize;
                int row = idx / AtlasGridSize;
                if (row * cellH + cellH > atlas.Height || col * cellW + cellW > atlas.Width) return null;
                return atlas.Clone(new Rectangle(col * cellW, row * cellH, cellW, cellH), atlas.PixelFormat);
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[BannerEmblem] LoadIconCell {meshId} error: {ex.Message}");
                return null;
            }
        }

        /// <summary>按名解析图集纹理：纹理名→材质名当纹理名→材质 DiffuseMap→slot0，每步留日志。</summary>
        private static Bitmap LoadAtlasBitmap(string textureName, string materialName)
        {
            try
            {
                BannerlordEngineTexture tex = null;
                if (!string.IsNullOrWhiteSpace(textureName))
                {
                    try { tex = BannerlordEngineTexture.CheckAndGetFromResource(textureName); }
                    catch (Exception ex) { TaleWorlds.Library.Debug.Print($"[BannerEmblem] CheckAndGetFromResource('{textureName}') threw: {ex.Message}"); }
                }
                if (tex == null && !string.IsNullOrWhiteSpace(materialName))
                {
                    try { tex = BannerlordEngineTexture.CheckAndGetFromResource(materialName); } catch { }
                }
                if (tex == null && !string.IsNullOrWhiteSpace(materialName))
                {
                    try
                    {
                        var mat = TaleWorlds.Engine.Material.GetFromResource(materialName);
                        if (mat != null)
                        {
                            tex = mat.GetTexture(TaleWorlds.Engine.Material.MBTextureType.DiffuseMap)
                                  ?? mat.GetTextureWithSlot(0);
                        }
                        TaleWorlds.Library.Debug.Print($"[BannerEmblem] Material '{materialName}' resolved: mat={(mat != null)} tex={(tex != null)}");
                    }
                    catch (Exception ex) { TaleWorlds.Library.Debug.Print($"[BannerEmblem] Material '{materialName}' threw: {ex.Message}"); }
                }
                if (tex == null)
                {
                    TaleWorlds.Library.Debug.Print($"[BannerEmblem] No texture resolved: srcTex='{textureName}' material='{materialName}'");
                    return null;
                }
                if (!tex.IsLoaded()) tex.PreloadTexture(true);
                if (tex.IsRenderTarget || tex.Width < AtlasGridSize || tex.Height < AtlasGridSize)
                {
                    TaleWorlds.Library.Debug.Print($"[BannerEmblem] Texture '{tex.Name}' unusable: rt={tex.IsRenderTarget} size={tex.Width}x{tex.Height}");
                    return null;
                }

                // 策略 1：GetPixelData 直读像素（同步，最可靠；压缩图集若引擎内部解压缩则直接可用）
                var viaPixels = TryTexturePixelsToBitmap(tex);
                if (viaPixels != null)
                {
                    TaleWorlds.Library.Debug.Print($"[BannerEmblem] Atlas '{tex.Name}' loaded via GetPixelData {viaPixels.Width}x{viaPixels.Height}");
                    return viaPixels;
                }

                // 策略 2：SaveToFile + 轮询等文件落盘（原生写盘可能是异步入队，立刻检查会误判失败）
                string tmp = Path.Combine(Path.GetTempPath(), $"af_banner_atlas_{Guid.NewGuid():N}.png");
                try
                {
                    tex.SaveToFile(tmp, false);
                    var deadline = DateTime.UtcNow.AddMilliseconds(1500);
                    while (DateTime.UtcNow < deadline)
                    {
                        if (File.Exists(tmp) && new FileInfo(tmp).Length > 0) break;
                        System.Threading.Thread.Sleep(50);
                    }
                    if (!File.Exists(tmp) || new FileInfo(tmp).Length <= 0)
                    {
                        TaleWorlds.Library.Debug.Print($"[BannerEmblem] SaveToFile produced no file for '{tex.Name}'");
                        return null;
                    }
                    using (var fs = new FileStream(tmp, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var bmp = new Bitmap(fs);
                        SwapRedBlue(bmp);
                        TaleWorlds.Library.Debug.Print($"[BannerEmblem] Atlas '{tex.Name}' loaded {bmp.Width}x{bmp.Height}");
                        return bmp;
                    }
                }
                finally
                {
                    try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[BannerEmblem] Atlas load error (tex='{textureName}' mat='{materialName}'): {ex.Message}");
                return null;
            }
        }

        /// <summary>GetPixelData 直读转 Bitmap：宽缓冲+行距对齐，零内容视为失败返回 null。</summary>
        private static Bitmap TryTexturePixelsToBitmap(BannerlordEngineTexture tex)
        {
            try
            {
                int w = tex.Width, h = tex.Height;
                int alignedPitch = ((w * 4 + 255) / 256) * 256;
                byte[] raw = new byte[Math.Max(w * h * 8, alignedPitch * h) + 65536];
                tex.GetPixelData(raw);
                bool any = false;
                for (int i = 0; i < w * h * 4 && !any; i += 4)
                {
                    if (raw[i] != 0 || raw[i + 1] != 0 || raw[i + 2] != 0) any = true;
                }
                if (!any) return null;
                var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
                var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                try { Marshal.Copy(raw, 0, data.Scan0, Math.Min(raw.Length, data.Stride * h)); }
                finally { bmp.UnlockBits(data); }
                SwapRedBlue(bmp);
                return bmp;
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[BannerEmblem] GetPixelData '{tex.Name}' failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 纹章图标双通道掩码着色：R 通道 = 主色(ColorId)掩码，G 通道 = 副色(ColorId2)掩码。
        /// 若切片近似灰度图（R≈G≈B 的非掩码贴图），自动退化为亮度×主色+原 alpha。
        /// DrawStroke 时用主色剪影做深色描边垫底。
        /// </summary>
        private static Bitmap TintIconCell(Bitmap cell, Color c1, Color c2, bool drawStroke)
        {
            int w = cell.Width, h = cell.Height;
            var src = cell.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var dst = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            var dstData = dst.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                int len = src.Stride * h;
                byte[] s = new byte[len];
                byte[] d = new byte[len];
                Marshal.Copy(src.Scan0, s, 0, len);

                // 探测掩码模式：R 与 G 差异明显的像素占比高 → 双通道掩码语义
                int diff = 0, samples = 0;
                for (int i = 0; i + 3 < len; i += 64)
                {
                    samples++;
                    if (Math.Abs(s[i + 2] - s[i + 1]) > 24) diff++;
                }
                bool twoChannel = diff * 4 > samples; // >25%

                for (int i = 0; i + 3 < len; i += 4)
                {
                    byte b = s[i], g = s[i + 1], r = s[i + 2], a = s[i + 3];
                    if (twoChannel)
                    {
                        float m1 = r / 255f, m2 = g / 255f;
                        float cov = m1 + m2;
                        if (cov <= 0.003f) continue;
                        if (cov > 1f) { m1 /= cov; m2 /= cov; cov = 1f; }
                        byte oa = (byte)Math.Min(255, Math.Max(a, (int)(cov * 255f)));
                        d[i] = (byte)Math.Min(255, c1.B * m1 + c2.B * m2);
                        d[i + 1] = (byte)Math.Min(255, c1.G * m1 + c2.G * m2);
                        d[i + 2] = (byte)Math.Min(255, c1.R * m1 + c2.R * m2);
                        d[i + 3] = oa;
                    }
                    else
                    {
                        // 灰度形状：亮度为主色强度，alpha 取原 alpha 与亮度较大者
                        int lum = (r * 299 + g * 587 + b * 114) / 1000;
                        byte oa = (byte)Math.Max(a, lum);
                        if (oa < 4) continue;
                        d[i] = (byte)(c1.B * lum / 255);
                        d[i + 1] = (byte)(c1.G * lum / 255);
                        d[i + 2] = (byte)(c1.R * lum / 255);
                        d[i + 3] = oa;
                    }
                }
                Marshal.Copy(d, 0, dstData.Scan0, len);
            }
            finally
            {
                cell.UnlockBits(src);
                dst.UnlockBits(dstData);
            }

            if (!drawStroke) return dst;
            return ApplyStroke(dst);
        }

        /// <summary>描边：把剪影按 8 方向偏移铺深色垫底，近似编辑器的 stroke 效果。</summary>
        private static Bitmap ApplyStroke(Bitmap tinted)
        {
            int w = tinted.Width, h = tinted.Height;
            var stroked = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(stroked))
            {
                var silhouette = new ImageAttributes();
                var cm = new ColorMatrix { Matrix33 = 0f };
                cm.Matrix00 = cm.Matrix11 = cm.Matrix22 = 0f;
                silhouette.SetColorMatrix(cm);
                int off = Math.Max(2, w / 128);
                for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    g.DrawImage(tinted, new Rectangle(dx * off, dy * off, w, h), 0, 0, w, h, GraphicsUnit.Pixel, silhouette);
                }
                g.DrawImage(tinted, 0, 0);
            }
            tinted.Dispose();
            return stroked;
        }

        private static void DrawPiece(Graphics g, Bitmap tinted, BannerData data, float scale, int canvasSize)
        {
            float dw = Math.Max(1f, data.Size.X * scale);
            float dh = Math.Max(1f, data.Size.Y * scale);
            float cx = data.Position.X * scale;   // Position = 图标中心（1528 空间）
            float cy = data.Position.Y * scale;
            float deg = data.Rotation * 57.29578f;

            var state = g.Save();
            try
            {
                g.TranslateTransform(cx, cy);
                if (Math.Abs(deg) > 0.01f) g.RotateTransform(deg);
                if (data.Mirror) g.ScaleTransform(-1f, 1f);
                g.DrawImage(tinted, -dw / 2f, -dh / 2f, dw, dh);
            }
            finally
            {
                g.Restore(state);
            }
        }

        private static void FillBackground(Graphics g, int canvasSize, BannerData bg)
        {
            Color c1 = PaletteColor(bg.ColorId, Color.Gray);
            // 背景图样若能进同一图集管线则按掩码着色铺满，否则纯色底
            Bitmap cell = LoadIconCell(bg.MeshId);
            if (cell != null)
            {
                Color c2 = bg.ColorId2 >= 0 ? PaletteColor(bg.ColorId2, c1) : c1;
                using (cell)
                using (var tinted = TintIconCell(cell, c1, c2, false))
                {
                    g.DrawImage(tinted, 0, 0, canvasSize, canvasSize);
                }
                return;
            }
            using (var brush = new SolidBrush(c1))
            {
                g.FillRectangle(brush, 0, 0, canvasSize, canvasSize);
            }
        }

        private static Color PaletteColor(int colorId, Color fallback)
        {
            if (colorId < 0) return fallback;
            try
            {
                uint u = BannerManager.GetColor(colorId);
                return Color.FromArgb(255, (int)((u >> 16) & 0xFF), (int)((u >> 8) & 0xFF), (int)(u & 0xFF));
            }
            catch { return fallback; }
        }

        private static void SwapRedBlue(Bitmap bmp)
        {
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            try
            {
                int len = data.Stride * bmp.Height;
                byte[] px = new byte[len];
                Marshal.Copy(data.Scan0, px, 0, len);
                for (int i = 0; i + 3 < len; i += 4)
                {
                    byte t = px[i]; px[i] = px[i + 2]; px[i + 2] = t;
                }
                Marshal.Copy(px, 0, data.Scan0, len);
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }
    }
}
