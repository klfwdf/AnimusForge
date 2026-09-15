using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    /// 格位（BannerIconData.TextureIndex，custom_banner_icons_XX 为 8x8 网格 256px 格）、
    /// 调色板颜色（BannerManager.GetColor）、位置/大小/旋转/镜像（BannerData，
    /// 坐标空间 Banner.BannerFullSize=1528）——读出后用 GDI+ 叠放产出真纹章 PNG。
    ///
    /// 线程模型：只有引擎纹理句柄解析在主线程（快）；GetPixelData 像素拷贝与全部
    /// GDI+ 合成在调用方线程执行，避免主线程长阻塞（2048x2048 图集读回约百毫秒级）。
    ///
    /// 图集掩码语义（debug 落盘实测）：alpha=覆盖率，G=主色(ColorId)填充区，
    /// R=副色(ColorId2)描边/细节区——R 高优先于 G。
    /// </summary>
    internal static class BannerEmblemComposer
    {
        // custom_banner_icons_XX 图集实测 8x8 网格（2048 图集 → 256px 格）
        // 原版 BannerVisual.ConvertToMultiMesh 实锤：u=(texIdx%4)*0.25, v=1-(texIdx/4)*0.25
        // → 图集是 4×4 网格（texture_index 实测范围 0-15），行从纹理底部往上数。
        // 配合 GetPixelData 的自下而上位图行序：bitmap 行 0 = 纹理底 = 索引 0-3。
        private const int AtlasGridSize = 4;
        private const int MaxAtlasBitmapSize = 1024;   // 读回后降采样上限，控制像素工作量
        private static readonly ConcurrentDictionary<string, BannerlordEngineTexture> TextureCache =
            new ConcurrentDictionary<string, BannerlordEngineTexture>(StringComparer.OrdinalIgnoreCase);

        /// <summary>将家族旗帜代码合成为 PNG base64；失败/无效输入返回 null（降级静默）。</summary>
        public static async Task<string> ComposeToBase64Async(string bannerCode, int canvasSize = 256)
        {
            if (string.IsNullOrWhiteSpace(bannerCode)) return null;
            // 阶段 A（主线程，轻量）：解析旗帜配方 + 图集纹理句柄
            var job = await RunOnGameThreadAsync(() => ResolveJob(bannerCode, canvasSize)).ConfigureAwait(false);
            if (job == null || job.Pieces.Count == 0) return null;
            // 阶段 B（当前线程）：像素读回 + GDI+ 合成，不占主线程
            return await ComposeBitmapsAsync(job).ConfigureAwait(false);
        }

        private static Task<T> RunOnGameThreadAsync<T>(Func<T> work)
        {
            if (IllustratorRuntime.IsMainThread) return Task.FromResult(work());
            var tcs = new TaskCompletionSource<T>();
            if (!IllustratorRuntime.Post(() =>
                {
                    try { tcs.TrySetResult(work()); }
                    catch (Exception ex) { tcs.TrySetException(ex); }
                }))
            {
                tcs.TrySetResult(default(T));
            }
            return tcs.Task;
        }

        private sealed class PieceJob
        {
            public int MeshId;
            public BannerlordEngineTexture Atlas;
            public int CellIndex;
            public float Cx, Cy, W, H, Deg;
            public bool Mirror, Stroke;
            public Color C1, C2;
        }

        private sealed class ComposeJob
        {
            public int Canvas;
            public Color BgColor;
            public readonly List<PieceJob> Pieces = new List<PieceJob>();
        }

        /// <summary>主线程：解析 BannerDataList 配方 + 每枚的图集纹理句柄（不读像素）。</summary>
        private static ComposeJob ResolveJob(string bannerCode, int canvasSize)
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

                var job = new ComposeJob { Canvas = canvasSize, BgColor = Color.Gray };
                var bg = banner.GetBannerDataAtIndex(Banner.BackgroundDataIndex);
                if (bg != null) job.BgColor = PaletteColor(bg.ColorId, Color.Gray);

                for (int i = 0; i < count; i++)
                {
                    if (i == Banner.BackgroundDataIndex) continue;
                    var data = banner.GetBannerDataAtIndex(i);
                    if (data == null) continue;

                    BannerIconData? iconData = null;
                    string texName = null;
                    try { iconData = manager.GetIconDataFromIconId(data.MeshId); } catch { }
                    try { texName = manager.GetIconSourceTextureName(data.MeshId); } catch { }
                    if (iconData == null)
                    {
                        TaleWorlds.Library.Debug.Print($"[BannerEmblem] mesh={data.MeshId} has no BannerIconData");
                        continue;
                    }
                    string materialName = iconData.Value.MaterialName;
                    var atlas = ResolveAtlasTexture(texName, materialName);
                    TaleWorlds.Library.Debug.Print($"[BannerEmblem] mesh={data.MeshId} texIdx={iconData.Value.TextureIndex} material='{materialName}' atlas={(atlas != null ? atlas.Name : "null")}");
                    if (atlas == null) continue;

                    job.Pieces.Add(new PieceJob
                    {
                        MeshId = data.MeshId,
                        Atlas = atlas,
                        CellIndex = Math.Max(0, iconData.Value.TextureIndex),
                        Cx = data.Position.X * scale,
                        Cy = data.Position.Y * scale,
                        W = Math.Max(1f, data.Size.X * scale),
                        H = Math.Max(1f, data.Size.Y * scale),
                        Deg = data.Rotation * 57.29578f,
                        Mirror = data.Mirror,
                        Stroke = data.DrawStroke,
                        C1 = PaletteColor(data.ColorId, Color.White),
                        C2 = data.ColorId2 >= 0 ? PaletteColor(data.ColorId2, PaletteColor(data.ColorId, Color.White)) : PaletteColor(data.ColorId, Color.White)
                    });
                }
                return job;
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[BannerEmblem] ResolveJob error: {ex.Message}");
                return null;
            }
        }

        /// <summary>主线程：纹理名→材质名→材质 DiffuseMap→slot0 解析图集纹理句柄。</summary>
        private static BannerlordEngineTexture ResolveAtlasTexture(string textureName, string materialName)
        {
            string key = !string.IsNullOrWhiteSpace(materialName) ? materialName : textureName;
            if (string.IsNullOrWhiteSpace(key)) return null;
            BannerlordEngineTexture tex;
            if (TextureCache.TryGetValue(key, out tex)) return tex;

            tex = null;
            if (!string.IsNullOrWhiteSpace(textureName))
            {
                try { tex = BannerlordEngineTexture.CheckAndGetFromResource(textureName); } catch { }
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
                }
                catch { }
            }
            if (tex == null) return null;
            try
            {
                if (tex.IsRenderTarget || tex.Width < AtlasGridSize || tex.Height < AtlasGridSize) return null;
                if (!tex.IsLoaded()) tex.PreloadTexture(true);
            }
            catch { return null; }
            TextureCache.TryAdd(key, tex);
            return tex;
        }

        /// <summary>调用方线程：像素读回 + 裁剪着色 + 叠放合成。</summary>
        private static async Task<string> ComposeBitmapsAsync(ComposeJob job)
        {
            var atlasBitmaps = new Dictionary<BannerlordEngineTexture, Bitmap>();
            string dbgDir = DebugDumpDir();
            var meta = new System.Text.StringBuilder();
            var canvas = new Bitmap(job.Canvas, job.Canvas, PixelFormat.Format32bppArgb);
            try
            {
                using (var g = Graphics.FromImage(canvas))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.Clear(Color.Transparent);
                    using (var brush = new SolidBrush(job.BgColor))
                    {
                        g.FillRectangle(brush, 0, 0, job.Canvas, job.Canvas);
                    }

                    foreach (var piece in job.Pieces)
                    {
                        Bitmap atlas;
                        if (!atlasBitmaps.TryGetValue(piece.Atlas, out atlas))
                        {
                            atlas = await LoadAtlasPixelsAsync(piece.Atlas).ConfigureAwait(false);
                            atlasBitmaps[piece.Atlas] = atlas;
                        }
                        if (atlas == null) continue;

                        int cellW = atlas.Width / AtlasGridSize;
                        int cellH = atlas.Height / AtlasGridSize;
                        int col = piece.CellIndex % AtlasGridSize;
                        int row = piece.CellIndex / AtlasGridSize;
                        if (col * cellW + cellW > atlas.Width || row * cellH + cellH > atlas.Height) continue;

                        Bitmap cell = null;
                        try
                        {
                            cell = atlas.Clone(new Rectangle(col * cellW, row * cellH, cellW, cellH), PixelFormat.Format32bppArgb);
                            // GetPixelData 行序自下而上：格位索引按原始行序正确，但单格内字形垂直颠倒——裁出后单独翻转回正
                            cell.RotateFlip(RotateFlipType.RotateNoneFlipY);
                        }
                        catch { continue; }

                        meta.AppendLine($"mesh={piece.MeshId} pos=({piece.Cx:0},{piece.Cy:0}) size=({piece.W:0}x{piece.H:0}) rot={piece.Deg:0} mirror={piece.Mirror} c1=#{piece.C1.R:X2}{piece.C1.G:X2}{piece.C1.B:X2} c2=#{piece.C2.R:X2}{piece.C2.G:X2}{piece.C2.B:X2} stroke={piece.Stroke} cell={cell.Width}x{cell.Height}");
                        using (cell)
                        using (var tinted = TintIconCell(cell, piece.C1, piece.C2, piece.Stroke))
                        {
                            DrawPiece(g, tinted, piece);
                            if (dbgDir != null)
                            {
                                try
                                {
                                    cell.Save(Path.Combine(dbgDir, $"cell_{piece.MeshId}_raw.png"), ImageFormat.Png);
                                    tinted.Save(Path.Combine(dbgDir, $"cell_{piece.MeshId}_tinted.png"), ImageFormat.Png);
                                }
                                catch { }
                            }
                        }
                    }
                }
                if (dbgDir != null)
                {
                    try
                    {
                        canvas.Save(Path.Combine(dbgDir, "emblem_final.png"), ImageFormat.Png);
                        File.WriteAllText(Path.Combine(dbgDir, "meta.txt"), meta.ToString());
                        int ai = 0;
                        foreach (var kv in atlasBitmaps)
                        {
                            try { DumpAtlasDebug(kv.Value, dbgDir, $"atlas{ai}"); } catch { }
                            ai++;
                        }
                    }
                    catch { }
                }
                if (!HasVisibleContent(canvas, job.BgColor))
                {
                    TaleWorlds.Library.Debug.Print("[BannerEmblem] Composed emblem has no visible icon content — skipped reference image.");
                    return null;
                }
                string b64 = ScreenCaptureHelper.ConvertBitmapToBase64(canvas, job.Canvas);
                TaleWorlds.Library.Debug.Print($"[BannerEmblem] Composed emblem ({(b64?.Length ?? 0)} chars, pieces={job.Pieces.Count})");
                return b64;
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[BannerEmblem] Compose error: {ex.Message}");
                return null;
            }
            finally
            {
                canvas.Dispose();
                foreach (var kv in atlasBitmaps) kv.Value?.Dispose();
            }
        }

        /// <summary>画布上是否存在与底色明显不同的像素（即图标是否真正画上去了）。</summary>
        private static bool HasVisibleContent(Bitmap bmp, Color bg)
        {
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                int stride = Math.Abs(data.Stride);
                var raw = new byte[stride * bmp.Height];
                Marshal.Copy(data.Scan0, raw, 0, raw.Length);
                for (int y = 0; y < bmp.Height; y++)
                {
                    int row = y * stride;
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        int i = row + x * 4;
                        if (Math.Abs(raw[i] - bg.B) > 8 || Math.Abs(raw[i + 1] - bg.G) > 8 ||
                            Math.Abs(raw[i + 2] - bg.R) > 8 || Math.Abs(raw[i + 3] - bg.A) > 8)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            finally { bmp.UnlockBits(data); }
        }

        /// <summary>调试：图集缩略图 + 8×8 每格内容像素统计，定位格位/朝向问题。</summary>
        private static void DumpAtlasDebug(Bitmap atlas, string dbgDir, string name)
        {
            int thumb = Math.Min(atlas.Width, 512);
            using (var t = new Bitmap(thumb, thumb, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(t))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(atlas, 0, 0, thumb, thumb);
                // 画 8×8 网格 + 索引编号，直接读出每个 texIdx 对应的格子内容
                int cell = thumb / AtlasGridSize;
                using (var pen = new Pen(Color.Red, 1))
                using (var font = new Font("Arial", 9))
                using (var brush = new SolidBrush(Color.Red))
                {
                    for (int i = 0; i <= AtlasGridSize; i++)
                    {
                        g.DrawLine(pen, i * cell, 0, i * cell, thumb);
                        g.DrawLine(pen, 0, i * cell, thumb, i * cell);
                    }
                    for (int r = 0; r < AtlasGridSize; r++)
                        for (int c = 0; c < AtlasGridSize; c++)
                            g.DrawString((r * AtlasGridSize + c).ToString(), font, brush, c * cell + 2, r * cell + 2);
                }
                t.Save(Path.Combine(dbgDir, name + "_thumb.png"), ImageFormat.Png);
            }

            int cw = atlas.Width / AtlasGridSize, ch = atlas.Height / AtlasGridSize;
            var map = new System.Text.StringBuilder();
            var data = atlas.LockBits(new Rectangle(0, 0, atlas.Width, atlas.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                int stride = Math.Abs(data.Stride);
                var raw = new byte[stride * atlas.Height];
                Marshal.Copy(data.Scan0, raw, 0, raw.Length);
                for (int r = 0; r < AtlasGridSize; r++)
                {
                    for (int c = 0; c < AtlasGridSize; c++)
                    {
                        int content = 0;
                        for (int y = r * ch; y < (r + 1) * ch; y += 4)
                        {
                            int row = y * stride;
                            for (int x = c * cw; x < (c + 1) * cw; x += 4)
                            {
                                int i = row + x * 4;
                                int lum = (raw[i] + raw[i + 1] + raw[i + 2]) / 3;
                                if (lum < 220 && raw[i + 3] > 10) content++;
                            }
                        }
                        map.Append(content > 40 ? '#' : '.');
                    }
                    map.AppendLine();
                }
            }
            finally { atlas.UnlockBits(data); }
            File.WriteAllText(Path.Combine(dbgDir, name + "_cells.txt"), map.ToString());
        }

        /// <summary>纹理→Bitmap：调用方线程先尝 GetPixelData；失败再回主线程重试，再退 SaveToFile。</summary>
        private static async Task<Bitmap> LoadAtlasPixelsAsync(BannerlordEngineTexture tex)
        {
            Bitmap bmp = null;
            try { bmp = TexturePixelsToBitmap(tex); } catch { }
            if (bmp == null)
            {
                bmp = await RunOnGameThreadAsync(() =>
                {
                    try { return TexturePixelsToBitmap(tex); } catch { return null; }
                }).ConfigureAwait(false);
            }
            if (bmp == null)
            {
                bmp = await RunOnGameThreadAsync(() => TextureSaveToFileToBitmap(tex)).ConfigureAwait(false);
            }
            if (bmp == null)
            {
                TaleWorlds.Library.Debug.Print($"[BannerEmblem] Atlas '{tex.Name}' pixels unreadable");
                return null;
            }
            if (bmp.Width > MaxAtlasBitmapSize || bmp.Height > MaxAtlasBitmapSize)
            {
                int nw = MaxAtlasBitmapSize, nh = MaxAtlasBitmapSize;
                var small = new Bitmap(nw, nh, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(small))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(bmp, 0, 0, nw, nh);
                }
                bmp.Dispose();
                bmp = small;
            }
            TaleWorlds.Library.Debug.Print($"[BannerEmblem] Atlas '{tex.Name}' bitmap {bmp.Width}x{bmp.Height}");
            return bmp;
        }

        private static Bitmap TexturePixelsToBitmap(BannerlordEngineTexture tex)
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
            // 注意：不能整图翻转——texIdx 直接对应原始行序（图标实测位于未翻转行区），
            // 整图翻转会挪走格位导致裁空；字形颠倒在裁出单格后再翻转纠正。
            return bmp;
        }

        private static Bitmap TextureSaveToFileToBitmap(BannerlordEngineTexture tex)
        {
            try
            {
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
                    if (!File.Exists(tmp) || new FileInfo(tmp).Length <= 0) return null;
                    using (var fs = new FileStream(tmp, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var bmp = new Bitmap(fs);
                        SwapRedBlue(bmp);
                        return bmp;
                    }
                }
                finally
                {
                    try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                }
            }
            catch { return null; }
        }

        /// <summary>
        /// 纹章图标掩码着色（实测语义）：A=覆盖率；G=主色(ColorId)填充；R=副色(ColorId2)
        /// 描边/细节且 R 优先于 G（原图绿实心+白描边）。若切片无 alpha 变化（无透明底），
        /// 退化为亮度×主色。
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

                // 覆盖率语义探测（实测反相图集存在：形状区 A=0、底 A=255）：
                // 图标必然只占格子小半——若按 A 算覆盖率>55% 说明反相，改用 255-A
                long coveredSum = 0;
                int pxCount = w * h;
                for (int i = 3; i < len; i += 4) coveredSum += s[i];
                bool invertAlpha = coveredSum > (long)(pxCount * 255L * 0.55f);
                bool hasAlpha = true;
                if (!invertAlpha && coveredSum > pxCount * 255L * 0.98f) hasAlpha = false; // 全不透明 → 无掩码

                for (int i = 0; i + 3 < len; i += 4)
                {
                    byte b = s[i], g = s[i + 1], r = s[i + 2], a = s[i + 3];
                    if (hasAlpha)
                    {
                        int cover = invertAlpha ? (255 - a) : a;
                        if (cover < 4) continue;
                        // R=副色(描边/细节)，G=主色填充，均无信息则主色
                        float m2 = r / 255f, m1 = g / 255f;
                        float sum = m1 + m2;
                        byte br, bg2, bb;
                        if (sum <= 0.003f)
                        {
                            br = c1.B; bg2 = c1.G; bb = c1.R;
                        }
                        else
                        {
                            br = (byte)Math.Min(255, (c1.B * m1 + c2.B * m2) / sum);
                            bg2 = (byte)Math.Min(255, (c1.G * m1 + c2.G * m2) / sum);
                            bb = (byte)Math.Min(255, (c1.R * m1 + c2.R * m2) / sum);
                        }
                        d[i] = br; d[i + 1] = bg2; d[i + 2] = bb; d[i + 3] = (byte)cover;
                    }
                    else
                    {
                        int lum = (r * 299 + g * 587 + b * 114) / 1000;
                        if (lum < 4) continue;
                        d[i] = (byte)(c1.B * lum / 255);
                        d[i + 1] = (byte)(c1.G * lum / 255);
                        d[i + 2] = (byte)(c1.R * lum / 255);
                        d[i + 3] = (byte)Math.Max(a, lum);
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
                int off = Math.Max(1, w / 128);
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

        private static void DrawPiece(Graphics g, Bitmap tinted, PieceJob piece)
        {
            var state = g.Save();
            try
            {
                g.TranslateTransform(piece.Cx, piece.Cy);
                if (Math.Abs(piece.Deg) > 0.01f) g.RotateTransform(piece.Deg);
                if (piece.Mirror) g.ScaleTransform(-1f, 1f);
                g.DrawImage(tinted, -piece.W / 2f, -piece.H / 2f, piece.W, piece.H);
            }
            finally
            {
                g.Restore(state);
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
    }
}
