using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using AnimusForge.Illustrator.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.ScreenSystem;

namespace AnimusForge.Illustrator.Engine
{
    public static partial class ScreenCaptureHelper
    {
        internal const string ScreenshotMaskReferenceNote =
            "图中均匀灰色平涂块是被插画或画廊界面遮住的未知区域，不是墙面、建筑或现场物体；忽略灰块和所有UI文字，不据遮挡处补造环境。";

        // Called only for an explicit generation, on the game thread. All UI access is read-only.
        internal static string CaptureUnobstructedConversationSceneBase64()
            => CaptureUnobstructedConversationSceneBase64(out _);

        internal static string CaptureUnobstructedConversationSceneBase64(out string reason)
        {
            IllustratorRuntime.AssertMainThread();
            reason = "unavailable";
            try
            {
                IntPtr window = GetForegroundWindow();
                if (window == IntPtr.Zero) { reason = "no_foreground_window"; return null; }
                GetWindowThreadProcessId(window, out uint processId);
                if (processId != (uint)Process.GetCurrentProcess().Id) { reason = "game_not_foreground"; return null; }
                if (!GetClientRect(window, out RECT client)) { reason = "client_rect_unavailable"; return null; }
                int width = client.Right - client.Left, height = client.Bottom - client.Top;
                if (width <= 0 || height <= 0) { reason = "empty_client_rect"; return null; }
                var origin = new POINT();
                if (!ClientToScreen(window, ref origin)) { reason = "client_origin_unavailable"; return null; }

                var masks = new List<Rectangle>(3);
                if (!TryReadIllustratorMasks(masks))
                {
                    reason = "overlay_bounds_unavailable_or_modal";
                    TaleWorlds.Library.Debug.Print("[Illustrator] Passive scene skipped: overlay bounds unavailable or modal report visible.");
                    return null;
                }
                using (var source = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                {
                    using (Graphics graphics = Graphics.FromImage(source))
                        graphics.CopyFromScreen(origin.X, origin.Y, 0, 0, source.Size, CopyPixelOperation.SourceCopy);
                    if (GetForegroundWindow() != window) { reason = "foreground_changed"; return null; }
                    using (Bitmap masked = SceneScreenshotMask.Prepare(source, masks, false))
                    {
                        if (masked == null)
                        {
                            reason = "overlay_covers_most_scene";
                            TaleWorlds.Library.Debug.Print("[Illustrator] Passive scene skipped: overlays obscure most of the scene.");
                            return null;
                        }
                        // Encoding uses the established screenshot colour path; no channel transform.
                        string encoded = ConvertBitmapToBase64(masked, 1024, 90);
                        reason = string.IsNullOrWhiteSpace(encoded) ? "encode_unavailable" : "captured";
                        return encoded;
                    }
                }
            }
            catch (Exception ex)
            {
                reason = "capture_exception_" + ex.GetType().Name;
                TaleWorlds.Library.Debug.Print("[Illustrator] Passive scene screenshot unavailable: " + ex.Message);
                return null;
            }
        }

        private static bool TryReadIllustratorMasks(List<Rectangle> masks)
        {
            var screen = ScreenManager.TopScreen;
            if (screen?.Layers == null) return false;
            foreach (var layer in screen.Layers)
            {
                if (!layer.IsActive || layer.IsFinalized) continue;
                string name = layer.Name ?? string.Empty;
                string panelId;
                if (name == "IllustrationCardOverlay" || name == "WeeklyReportIllustrationOverlay") panelId = "CardPanel";
                else if (name == "IllustrationFullscreenOverlay") panelId = "TopPanel";
                else if (name == "IllustrationFullscreenBackdrop") panelId = "BackdropPanel";
                else if (name == "IllustratorGalleryPopup") panelId = "MainPanel";
                else if (name == "DevWeeklyReportPopup") return false; // Modal full-screen dimmer; not the original scene colour.
                else
                {
                    if (name.IndexOf("Illustrator", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("Illustration", StringComparison.OrdinalIgnoreCase) >= 0) return false;
                    continue;
                }
                Widget root = (layer as GauntletLayer)?.UIContext?.Root;
                if (root == null) return false;
                if (!root.IsVisible) continue;
                Widget panel = root.Id == panelId ? root : root.FindChild(panelId, includeAllChildren: true);
                if (panel == null) return false;
                if (!IsVisibleThroughParents(panel)) continue;
                // GlobalPosition and Size are already measured pixels, as in MovableGauntletLayer.
                // Only the prefab's logical outer border needs UI scale conversion.
                float scale = root.Context?.Scale ?? 1f;
                if (scale <= 0 || float.IsNaN(scale) || float.IsInfinity(scale)) return false;
                if (!SceneScreenshotMask.TryCreatePixelBounds(panel.GlobalPosition.X, panel.GlobalPosition.Y,
                    panel.Size.X, panel.Size.Y, 24f * scale, out Rectangle bounds)) return false;
                masks.Add(bounds);
                if (masks.Count > SceneScreenshotMask.MaxMasks) return false;
            }
            return true;
        }

        private static bool IsVisibleThroughParents(Widget widget)
        {
            for (Widget current = widget; current != null; current = current.ParentWidget)
                if (!current.IsVisible) return false;
            return true;
        }
    }
}
