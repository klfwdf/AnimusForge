using System;
using TaleWorlds.GauntletUI.BaseTypes;
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
        internal static Widget VisualRoot => _overlayLayer?.UIContext?.Root;
        private static MovableGauntletLayer _overlayLayer;
        private static WeeklyReportIllustrationOverlayVM _overlayVm;
        private static string _currentEventKey;
        private static WeeklyReportVisualContext _currentContext;
        private static IllustrationScope _scope;
        private static ScreenBase _ownerScreen;
        private static string _activeSpriteName;
        private static bool _closing;
        private static int _redrawCount;

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
                AttachOverlay(titleText, subtitleText, bodyText);
            }
            catch (Exception ex)
            {
                CloseOverlay();
                Debug.Print($"[Illustrator] Error attaching overlay to weekly report: {ex.Message}");
            }
        }

        public static void DevWeeklyReportPopup_Close_Postfix()
        {
            CloseOverlay();
        }

        private static void AttachOverlay(string title, string subtitleText, string bodyText)
        {
            ScreenBase topScreen = ScreenManager.TopScreen;
            if (topScreen == null)
            {
                return;
            }

            // 先关旧 overlay，再提取上下文——CloseOverlay 内部会清空 _currentContext，
            // 顺序颠倒会导致新生成被 TriggerRegenerate 因 ctx=null 静默跳过
            CloseOverlay();
            _closing = false;
            _ownerScreen = topScreen;
            IllustrationScope ownerScope = null;
            ownerScope = new IllustrationScope(topScreen, null, () => CloseOverlayForScope(ownerScope));
            _scope = ownerScope;
            _currentContext = WeeklyReportContextExtractor.ExtractFromWeeklyReport(title, subtitleText, bodyText);
            _currentEventKey = "weekly_report:" + DiskImageCacheManager.ComputeHash(title + ":" + subtitleText);
            _redrawCount = 0;
            Debug.Print($"[Illustrator] Weekly report popup opened: '{title}' context={(_currentContext != null)}");

            _overlayVm = new WeeklyReportIllustrationOverlayVM(title, TriggerRegenerate);
            var layer = new MovableGauntletLayer("WeeklyReportIllustrationOverlay", 4010, false);
            _overlayLayer = layer;
            var movieIdentifier = layer.LoadMovie("WeeklyReportIllustrationOverlay", _overlayVm);
            layer.AutoAttachMovable(movieIdentifier?.Movie, "CardPanel", "TitleBar");
            layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.MouseButtons);
            _overlayLayer = layer;
            topScreen.AddLayer(_overlayLayer);

            var cached = DiskImageCacheManager.LoadImage(_currentEventKey, _scope.CampaignKey, "weekly_report");
            Debug.Print($"[Illustrator] Weekly overlay attached: cached={(cached != null)}, autoGen={IllustratorSettings.Instance.AutoGenerateWeeklyReportIllustration}");
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
            try { TriggerRegenerateCore(); }
            catch (Exception ex)
            {
                if (_overlayVm != null) { _overlayVm.IsLoading = false; _overlayVm.StatusText = "生成准备失败：" + ex.Message; }
            }
        }

        private static void TriggerRegenerateCore()
        {
            if (_overlayVm == null || _currentContext == null || _scope == null)
            {
                Debug.Print($"[Illustrator] Weekly regenerate skipped: vm={(_overlayVm != null)} ctx={(_currentContext != null)} scope={(_scope != null)}");
                return;
            }

            _overlayVm.IsLoading = true;
            _overlayVm.HasIllustration = false;
            _overlayVm.StatusText = "正在通过艺术导演提取人物装备与环境描写，构思画卷...";

            string eventKey = _currentEventKey;
            var context = _currentContext;
            // 每次生成都注入随机构图变体；重绘时额外要求导演刻意换镜头，避免同一周报每次出图雷同
            _redrawCount++;
            string artDirection = context.BuildArtDirection();
            string variation = GenerateWeeklyVariation();
            if (!string.IsNullOrWhiteSpace(variation)) artDirection += "\n" + variation;
            if (_redrawCount > 1) artDirection += "\n" + VisualDirectorEngine.BuildRedrawVariationDirective(_redrawCount);
            var promptPlan = new IllustrationPromptPlan("周报历史纪事插画", context.BuildHardFacts(), artDirection, context.BuildDirectorOnlyFacts());
            var options = IllustratorRuntime.CaptureOptions();
            string campaignKey = _scope.CampaignKey;
            var generationScope = _scope;

            var protagonist = context.ProtagonistHero;
            var appearance = context.ProtagonistProfile?.Appearance;
            string protagonistName = protagonist?.Name?.ToString() ?? "当事人";
            string bannerCode = (protagonist?.Clan?.Banner ?? protagonist?.Clan?.Kingdom?.Banner)?.BannerCode;

            bool started = _scope.Run(async token =>
            {
                Debug.Print("[Illustrator] Weekly generation task started.");
                var refs = new List<IllustrationReferenceImage>();
                // 离屏舞台提取在 scope 内携带 token：关闭弹窗或重新生成时旧任务立即取消并拆舞台
                Task<string> portraitStage = null;
                if (options?.EnableOffscreenRendering == true && protagonist != null)
                {
                    portraitStage = ScreenCaptureHelper.ExtractHeroPortraitOffscreenAsync(protagonist, cancellationToken: token, cleanTempFiles: options?.AutoCleanTempFiles == true, appearance: appearance);
                }
                if (portraitStage != null)
                {
                    string b64 = await portraitStage.ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(b64))
                    {
                        refs.Add(new IllustrationReferenceImage(b64, $"登场人物【{protagonistName}】的身份参考图：仅用于锁定其五官、发型、肤色、装备与服饰或其他实际纹章载体上的家族纹章（仅在画面确有该载体时绘制）；严禁复制本图的姿势、取景、背景、光影与游戏渲染质感"));
                    }
                }
                // 纹章由原生渲染导出，保留完整背景、配色、描边与变换
                if (!string.IsNullOrWhiteSpace(bannerCode))
                {
                    string emblemB64 = await BannerEmblemComposer.ComposeToBase64Async(bannerCode, cleanTempFiles: options?.AutoCleanTempFiles == true, cancellationToken: token).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(emblemB64))
                    {
                        refs.Add(new IllustrationReferenceImage(emblemB64, "该家族真实纹章标准样图：其底色与徽记形状、配色即纹章本体；当画面因已确认事实出现纹章载体时，必须与此一致绘制，严禁编造或改动图腾；没有载体证据时不要添加纹章载体"));
                    }
                }

                string prompt = await VisualDirectorEngine.ExpandToDetailedPromptAsync(promptPlan, refs, options, token).ConfigureAwait(false);
                IllustratorRuntime.Post(() => { if (ReferenceEquals(_scope, generationScope) && !token.IsCancellationRequested && _overlayVm != null) _overlayVm.StatusText = "导演构思完成，正在绘制纪事画卷（等待生图模型返回）..."; });
                var genRefs = options?.EnableReferenceImageForGeneration == false ? null : (System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage>)refs;
                var result = await UniversalOpenAiImageClient.GenerateImageAsync(prompt, genRefs, options, token).ConfigureAwait(false);
                string effectivePrompt = string.IsNullOrWhiteSpace(result.ResolvedPrompt) ? prompt : result.ResolvedPrompt;
                CachedIllustrationItem saved = null;
                if (result.Success && result.ImageBytes != null)
                {
                    token.ThrowIfCancellationRequested();
                    saved = DiskImageCacheManager.SaveImage(eventKey, result.ImageBytes, effectivePrompt, context.Title, "weekly_report", campaignKey, options?.MaxCacheCount ?? 200, makeDefault: false, allowImplicitDefault: false);
                }
                return new { Result = result, Saved = saved, Prompt = effectivePrompt };
            }, completion =>
            {
                if (completion.Saved != null) DiskImageCacheManager.SetDefault(completion.Saved, campaignKey);
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
            if (!started && _overlayVm != null)
            {
                _overlayVm.IsLoading = false;
            }
        }

        /// <summary>周报纪事画的随机构图变体——同一事件每次生成应有不同取景。</summary>
        private static string GenerateWeeklyVariation()
        {
            string[] variations =
            {
                "远景史诗画卷：事件全貌与山河城郭交代世界尺度，人物小而可辨",
                "中景群像：数位当事人同框，以动作与视线关系承担叙事",
                "低机位仰拍：以天空或已有建筑线条形成留白，人物庄严",
                "高位俯拍：展示战场、营地或街巷的空间格局与动线",
                "决定性瞬间：事件临界点的动作爆发（冲锋、签约、宣旨、点燃）",
                "余波时刻：事件刚结束后的烟尘、撤离与凝视，不画动作顶点",
                "前景遮挡构图：门框、建筑构件或兵器做前景，人物在中景",
                "侧面横向构图：人物呈半剪影，让光线与烟尘承担主角",
                "特写聚焦：一件关键道具、表情或手势承载事件含义",
                "纵深构图：近景人物背影望向远方的事件现场",
                "非对称动态构图：披风、烟尘、人群形成方向线",
                "克制光影版：不强制黄昏火光，以事件事实决定光线氛围"
            };
            int seed = Math.Abs(Environment.TickCount ^ Guid.NewGuid().GetHashCode());
            return "本次构图变化建议：" + variations[seed % variations.Length] + "。这只是构图选项，若与事件事实冲突应舍弃。";
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
            var sprite = GauntletTextureLoader.LoadOrRegisterPngBytes(spriteName, bytes);
            if (sprite == null) return false;
            _activeSpriteName = spriteName;
            _overlayVm.SpriteName = spriteName;
            _overlayVm.PromptText = prompt;
            _overlayVm.HasIllustration = true;
            _overlayVm.IsLoading = false;
            return true;
        }

        private static void CloseOverlayForScope(IllustrationScope ownerScope)
        {
            if (ownerScope != null && ReferenceEquals(_scope, ownerScope)) CloseOverlay();
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
