using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
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

        public void ExecuteCopyPrompt()
        {
            if (string.IsNullOrWhiteSpace(PromptText)) return;
            try
            {
                Input.SetClipboardText(PromptText);
                StatusText = "提示词已复制到剪贴板";
            }
            catch (Exception ex)
            {
                StatusText = "复制失败: " + ex.Message;
            }
        }

        public void ExecuteOpenGallery()
        {
            IllustratorGalleryPopup.Show();
        }

        public void ExecuteClose()
        {
            WeeklyReportPopupIllustrationPatch.CloseOverlay();
        }
    }

    public static class WeeklyReportPopupIllustrationPatch
    {
        private static MovableGauntletLayer _overlayLayer;
        private static WeeklyReportIllustrationOverlayVM _overlayVm;
        private static string _currentEventKey;
        private static WeeklyReportVisualContext _currentContext;
        private static IllustrationScope _scope;
        private static ScreenBase _ownerScreen;
        private static string _activeSpriteName;
        private static bool _closing;

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

            if (!IllustratorRuntime.IsEnabled())
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
            _closing = false;
            _ownerScreen = topScreen;
            _scope = new IllustrationScope(topScreen, null, CloseOverlay);

            _overlayVm = new WeeklyReportIllustrationOverlayVM(title, TriggerRegenerate);
            var layer = new MovableGauntletLayer("WeeklyReportIllustrationOverlay", 4010, false);
            var movieIdentifier = layer.LoadMovie("WeeklyReportIllustrationOverlay", _overlayVm);
            layer.AutoAttachMovable(movieIdentifier?.Movie, "CardPanel", "TitleBar");
            layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.MouseButtons);
            _overlayLayer = layer;
            topScreen.AddLayer(_overlayLayer);

            var cached = DiskImageCacheManager.LoadImage(_currentEventKey, _scope.CampaignKey, "weekly_report");
            if (cached != null && Publish(cached, cached.Prompt))
            {
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
            if (_overlayVm == null || _currentContext == null || _scope == null)
            {
                return;
            }

            _overlayVm.IsLoading = true;
            _overlayVm.HasIllustration = false;
            _overlayVm.StatusText = "正在通过艺术导演提取人物装备与环境描写，构思画卷...";

            string eventKey = _currentEventKey;
            var context = _currentContext;
            var promptPlan = new IllustrationPromptPlan("周报历史纪事插画", context.BuildHardFacts(), context.BuildArtDirection());
            var options = IllustratorRuntime.CaptureOptions();
            string campaignKey = _scope.CampaignKey;

            var protagonist = context.ProtagonistHero;
            string protagonistName = protagonist?.Name?.ToString() ?? "当事人";
            string bannerCode = (protagonist?.Clan?.Banner ?? protagonist?.Clan?.Kingdom?.Banner)?.BannerCode;

            _scope.Run(async token =>
            {
                var refs = new List<IllustrationReferenceImage>();
                // 离屏舞台提取在 scope 内携带 token：关闭弹窗或重新生成时旧任务立即取消并拆舞台
                Task<string> portraitStage = null;
                if (options?.EnableOffscreenRendering == true && protagonist != null)
                {
                    portraitStage = ScreenCaptureHelper.ExtractHeroPortraitOffscreenAsync(protagonist, cancellationToken: token);
                }
                if (portraitStage != null)
                {
                    string b64 = await portraitStage.ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(b64))
                    {
                        refs.Add(new IllustrationReferenceImage(b64, $"登场人物【{protagonistName}】的身份参考图：仅用于锁定其五官、发型、肤色、装备与盾面/罩袍上的家族纹章（旗帜徽记依此纹样绘制）；严禁复制本图的姿势、取景、背景、光影与游戏渲染质感"));
                    }
                }
                // 纹章由纯托管合成（旗帜代码→图集→GDI+），无舞台零闪屏
                if (!string.IsNullOrWhiteSpace(bannerCode))
                {
                    string emblemB64 = await BannerEmblemComposer.ComposeToBase64Async(bannerCode).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(emblemB64))
                    {
                        refs.Add(new IllustrationReferenceImage(emblemB64, "该家族真实纹章标准样图：其底色与徽记形状、配色即纹章本体；当画面出现旗帜、盾徽或罩袍纹章时必须与此完全一致的形状与配色绘制，严禁编造或改动为其他图腾；但不要仅为展示纹章而强行添加盾牌或旗帜"));
                    }
                }

                string prompt = await VisualDirectorEngine.ExpandToDetailedPromptAsync(promptPlan, refs, options, token).ConfigureAwait(false);
                IllustratorRuntime.Post(() => { if (_overlayVm != null) _overlayVm.StatusText = "导演构思完成，正在绘制纪事画卷（等待生图模型返回）..."; });
                var genRefs = options?.EnableReferenceImageForGeneration == false ? null : (System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage>)refs;
                var result = await UniversalOpenAiImageClient.GenerateImageAsync(prompt, genRefs, options, token).ConfigureAwait(false);
                string effectivePrompt = string.IsNullOrWhiteSpace(result.ResolvedPrompt) ? prompt : result.ResolvedPrompt;
                CachedIllustrationItem saved = null;
                if (result.Success && result.ImageBytes != null)
                {
                    saved = DiskImageCacheManager.SaveImage(eventKey, result.ImageBytes, effectivePrompt, context.Title, "weekly_report", campaignKey, options?.MaxCacheCount ?? 200, makeDefault: true);
                }
                return new { Result = result, Saved = saved, Prompt = effectivePrompt };
            }, completion =>
            {
                _overlayVm.PromptText = completion.Prompt;
                if (completion.Result != null && completion.Result.Success && completion.Result.ImageBytes != null && Publish(completion.Saved, completion.Prompt, completion.Result.ImageBytes))
                {
                    _overlayVm.StatusText = "【本周纪事油画已绘制完成】";
                }
                else
                {
                    _overlayVm.IsLoading = false;
                    _overlayVm.StatusText = "绘制未成功: " + (completion.Result?.ErrorMessage ?? "未能保存图像");
                }
            }, error =>
            {
                _overlayVm.IsLoading = false;
                _overlayVm.StatusText = "异常: " + error;
            });
        }

        private static bool Publish(CachedIllustrationItem item, string prompt, byte[] imageBytes = null)
        {
            if (item == null && imageBytes == null) return false;
            if (!string.IsNullOrEmpty(_activeSpriteName))
            {
                GauntletTextureLoader.ReleaseSprite(_activeSpriteName);
                _activeSpriteName = null;
            }
            string spriteName = (item?.Key ?? "weekly_" + Guid.NewGuid().ToString("N")) + "_weekly";
            var bytes = imageBytes ?? item.ImageData;
            var sprite = GauntletTextureLoader.LoadOrRegisterPngBytes(spriteName, bytes, fixColorChannels: IllustratorRuntime.CaptureOptions()?.FixColorChannels ?? true);
            if (sprite == null) return false;
            _activeSpriteName = spriteName;
            _overlayVm.SpriteName = spriteName;
            _overlayVm.PromptText = prompt;
            _overlayVm.HasIllustration = true;
            _overlayVm.IsLoading = false;
            return true;
        }

        public static void CloseOverlay()
        {
            if (_closing) return;
            _closing = true;
            try
            {
                _scope?.Close();
                if (!string.IsNullOrEmpty(_activeSpriteName))
                {
                    GauntletTextureLoader.ReleaseSprite(_activeSpriteName);
                    _activeSpriteName = null;
                }
                if (_overlayLayer != null && _ownerScreen != null)
                {
                    _ownerScreen.RemoveLayer(_overlayLayer);
                }
            }
            catch
            {
            }
            finally
            {
                _overlayLayer = null;
                _overlayVm = null;
                _currentContext = null;
                _currentEventKey = null;
                _scope = null;
                _ownerScreen = null;
            }
        }
    }
}
