using System;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using AnimusForge.Illustrator.Context;
using AnimusForge.Illustrator.Core;
using AnimusForge.Illustrator.Engine;
using AnimusForge.Illustrator.UI.Gallery;
using AnimusForge.Illustrator.UI.Overlays;

namespace AnimusForge.Illustrator.UI.Patches
{
    public sealed class WeeklyReportIllustrationOverlayVM : ViewModel
    {
        private string _spriteName = string.Empty;
        private bool _hasIllustration;
        private bool _isLoading;
        private string _statusText = "正在构思本周纪事画卷...";
        private string _titleText = string.Empty;
        private string _promptText = string.Empty;
        private bool _showPrompt;
        private readonly Action _onRegenerate;

        public WeeklyReportIllustrationOverlayVM(string title, Action onRegenerate)
        {
            _titleText = title ?? "帝国纪事油画";
            _onRegenerate = onRegenerate;
        }

        [DataSourceProperty]
        public string SpriteName
        {
            get => _spriteName;
            set
            {
                if (value != _spriteName)
                {
                    _spriteName = value;
                    OnPropertyChangedWithValue(value, nameof(SpriteName));
                }
            }
        }

        [DataSourceProperty]
        public bool HasIllustration
        {
            get => _hasIllustration;
            set
            {
                if (value != _hasIllustration)
                {
                    _hasIllustration = value;
                    OnPropertyChangedWithValue(value, nameof(HasIllustration));
                }
            }
        }

        [DataSourceProperty]
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (value != _isLoading)
                {
                    _isLoading = value;
                    OnPropertyChangedWithValue(value, nameof(IsLoading));
                }
            }
        }

        [DataSourceProperty]
        public string StatusText
        {
            get => _statusText;
            set
            {
                if (value != _statusText)
                {
                    _statusText = value;
                    OnPropertyChangedWithValue(value, nameof(StatusText));
                }
            }
        }

        [DataSourceProperty]
        public string TitleText
        {
            get => _titleText;
            set
            {
                if (value != _titleText)
                {
                    _titleText = value;
                    OnPropertyChangedWithValue(value, nameof(TitleText));
                }
            }
        }

        [DataSourceProperty]
        public string PromptText
        {
            get => _promptText;
            set
            {
                if (value != _promptText)
                {
                    _promptText = value;
                    OnPropertyChangedWithValue(value, nameof(PromptText));
                }
            }
        }

        [DataSourceProperty]
        public bool ShowPrompt
        {
            get => _showPrompt;
            set
            {
                if (value != _showPrompt)
                {
                    _showPrompt = value;
                    OnPropertyChangedWithValue(value, nameof(ShowPrompt));
                }
            }
        }

        public void ExecuteRegenerate()
        {
            _onRegenerate?.Invoke();
        }

        public void ExecuteTogglePrompt()
        {
            ShowPrompt = !ShowPrompt;
        }

        public void ExecuteOpenGallery()
        {
            IllustratorGalleryPopup.Show();
        }
    }

    public static class WeeklyReportPopupIllustrationPatch
    {
        private static MovableGauntletLayer _overlayLayer;
        private static WeeklyReportIllustrationOverlayVM _overlayVm;
        private static string _currentEventKey;
        private static WeeklyReportVisualContext _currentContext;

        public static void Patch(Harmony harmony)
        {
            try
            {
                Type targetType = AccessTools.TypeByName("AnimusForge.DevWeeklyReportPopup");
                if (targetType == null)
                {
                    Debug.Print("[Illustrator] DevWeeklyReportPopup type not found; skipping popup hook.");
                    return;
                }

                MethodInfo showMethod = AccessTools.Method(targetType, "Show");
                if (showMethod != null)
                {
                    harmony.Patch(showMethod, postfix: new HarmonyMethod(typeof(WeeklyReportPopupIllustrationPatch), nameof(DevWeeklyReportPopup_Show_Postfix)));
                }

                MethodInfo closeMethod = AccessTools.Method(targetType, "Close");
                if (closeMethod != null)
                {
                    harmony.Patch(closeMethod, postfix: new HarmonyMethod(typeof(WeeklyReportPopupIllustrationPatch), nameof(DevWeeklyReportPopup_Close_Postfix)));
                }

                Debug.Print("[Illustrator] Hooked into DevWeeklyReportPopup successfully.");
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] Failed to hook DevWeeklyReportPopup: {ex.Message}");
            }
        }

        public static void DevWeeklyReportPopup_Show_Postfix(bool __result, string titleText, string subtitleText, string bodyText)
        {
            if (!__result)
            {
                return;
            }

            var settings = IllustratorSettings.Instance;
            if (settings == null || !settings.EnableImageGeneration)
            {
                return;
            }

            try
            {
                _currentContext = WeeklyReportContextExtractor.ExtractFromWeeklyReport(titleText, subtitleText, bodyText);
                _currentEventKey = "weekly_report:" + DiskImageCacheManager.ComputeHash(titleText + ":" + subtitleText);

                AttachOverlay(titleText);
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] Error attaching overlay to weekly report: {ex.Message}");
            }
        }

        public static void DevWeeklyReportPopup_Close_Postfix()
        {
            CloseOverlay();
        }

        private static void AttachOverlay(string title)
        {
            ScreenBase topScreen = ScreenManager.TopScreen;
            if (topScreen == null)
            {
                return;
            }

            CloseOverlay();

            _overlayVm = new WeeklyReportIllustrationOverlayVM(title, TriggerRegenerate);
            var layer = new MovableGauntletLayer("WeeklyReportIllustrationOverlay", 4010, false);
            var movieIdentifier = layer.LoadMovie("WeeklyReportIllustrationOverlay", _overlayVm);
            layer.AutoAttachMovable(movieIdentifier?.Movie, "CardPanel", "TitleBar");
            layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.MouseButtons);
            _overlayLayer = layer;
            topScreen.AddLayer(_overlayLayer);

            // 检查缓存
            if (DiskImageCacheManager.TryGetCachedImage(_currentEventKey, out byte[] cachedBytes, out _))
            {
                GauntletTextureLoader.LoadOrRegisterPngBytes(_currentEventKey, cachedBytes);
                _overlayVm.SpriteName = _currentEventKey;
                _overlayVm.HasIllustration = true;
                _overlayVm.IsLoading = false;
                _overlayVm.StatusText = "【本周纪事油画】";
            }
            else if (IllustratorSettings.Instance.AutoGenerateWeeklyReportIllustration)
            {
                TriggerRegenerate();
            }
            else
            {
                _overlayVm.StatusText = "点击【生成纪事插画】绘制本周大事件";
                _overlayVm.IsLoading = false;
            }
        }

        private static void TriggerRegenerate()
        {
            if (_overlayVm == null || _currentContext == null)
            {
                return;
            }

            _overlayVm.IsLoading = true;
            _overlayVm.HasIllustration = false;
            _overlayVm.StatusText = "正在通过艺术导演提取人物装备与环境描写，构思画卷...";

            string eventKey = _currentEventKey;
            var context = _currentContext;

            Task.Run(async () =>
            {
                try
                {
                    string prompt = await VisualDirectorEngine.ExpandToDetailedPromptAsync(context.BuildCompositeContext());
                    _overlayVm.PromptText = prompt;
                    _overlayVm.StatusText = "AI 画师正在绘制中世纪古典油画...";

                    var result = await UniversalOpenAiImageClient.GenerateImageAsync(prompt);
                    if (result.Success && result.ImageBytes != null)
                    {
                        DiskImageCacheManager.SaveImage(eventKey, result.ImageBytes, prompt, context.Title, "weekly_report");
                        string dynamicKey = $"{eventKey}_{DateTime.UtcNow.Ticks}";
                        GauntletTextureLoader.LoadOrRegisterPngBytes(eventKey, result.ImageBytes);
                        GauntletTextureLoader.LoadOrRegisterPngBytes(dynamicKey, result.ImageBytes);

                        _overlayVm.SpriteName = dynamicKey;
                        _overlayVm.HasIllustration = true;
                        _overlayVm.IsLoading = false;
                        _overlayVm.StatusText = "【本周纪事油画已绘制完成】";
                    }
                    else
                    {
                        _overlayVm.IsLoading = false;
                        _overlayVm.StatusText = "绘制未成功: " + result.ErrorMessage;
                    }
                }
                catch (Exception ex)
                {
                    _overlayVm.IsLoading = false;
                    _overlayVm.StatusText = "异常: " + ex.Message;
                }
            });
        }

        public static void CloseOverlay()
        {
            try
            {
                if (_overlayLayer != null)
                {
                    ScreenBase topScreen = ScreenManager.TopScreen;
                    if (topScreen != null)
                    {
                        topScreen.RemoveLayer(_overlayLayer);
                    }
                    _overlayLayer = null;
                    _overlayVm = null;
                }
            }
            catch
            {
            }
        }
    }
}
