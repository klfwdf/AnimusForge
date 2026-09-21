using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;
using AnimusForge.Illustrator.Context;
using AnimusForge.Illustrator.Core;
using AnimusForge.Illustrator.Engine;
using AnimusForge.Illustrator;
using AnimusForge.Illustrator.UI.Gallery;
using AnimusForge.Illustrator.UI.Overlays;

namespace AnimusForge.Illustrator.UI.Patches
{
    public static class ConversationIllustrationPatch
    {
        private const string ButtonId = "AnimusForgeConversationIllustrateButton";
        private static readonly List<WeakReference<ButtonWidget>> InjectedButtons = new List<WeakReference<ButtonWidget>>();
        private static bool _patched;
        private static bool _nativeConversationReplyPatchInstalled;
        private static TaleWorlds.CampaignSystem.Conversation.ConversationManager _conversationManagerSubscription;
        private static string _lastAutoRedrawSentence = string.Empty;

        public static void EnsurePatched(Harmony harmony)
        {
            if (_patched) return;
            _patched = true;

            try
            {
                Harmony activeHarmony = harmony ?? new Harmony("AnimusForge.Illustrator.Conversation");
                MethodInfo loadMovie = AccessTools.Method(typeof(GauntletMovie), nameof(GauntletMovie.Load));
                if (loadMovie != null)
                {
                    activeHarmony.Patch(loadMovie, postfix: new HarmonyMethod(typeof(ConversationIllustrationPatch), nameof(LoadMoviePostfix)));
                }
                TryPatchHostNativeConversationReply(activeHarmony);
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[Illustrator] Failed to patch conversation: {ex.Message}");
            }
        }

        public static void LoadMoviePostfix(string movieName, IViewModel datasource, IGauntletMovie __result)
        {
            try
            {
                Widget root = __result?.RootWidget;
                if (root == null) return;

                if (string.Equals(movieName, "AnimusForgeNativeConversationOverlay", StringComparison.Ordinal))
                {
                    EnsureOverlayButton(root);
                    return;
                }

                if (string.Equals(movieName, "MapConversation", StringComparison.Ordinal) ||
                    string.Equals(movieName, "MissionConversation", StringComparison.Ordinal))
                {
                    EnsureFallbackButton(root);
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[Illustrator] Conversation LoadMoviePostfix error: {ex.Message}");
            }
        }

        private static void EnsureOverlayButton(Widget root)
        {
            if (root == null) return;

            var existing = root.FindChild(ButtonId, includeAllChildren: true) as ButtonWidget;
            if (existing != null)
            {
                TrackButton(existing);
                existing.IsVisible = IllustratorRuntime.IsEnabled("conversation");
                return;
            }

            if (!IllustratorRuntime.IsEnabled("conversation"))
            {
                return;
            }

            ButtonWidget button = new ButtonWidget(root.Context)
            {
                Id = ButtonId,
                WidthSizePolicy = SizePolicy.CoverChildren,
                HeightSizePolicy = SizePolicy.CoverChildren,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                MarginTop = 330f,
                MarginRight = 60f,
                IsEnabled = true,
                DoNotAcceptEvents = false,
                DoNotPassEventsToChildren = false,
                IsVisible = true
            };

            button.ClickEventHandlers.Add(delegate
            {
                HandleConversationIllustrateClicked();
            });
            TrackButton(button);

            TextWidget textWidget = new TextWidget(root.Context)
            {
                WidthSizePolicy = SizeSizeToCoverChildren(root.Context),
                HeightSizePolicy = SizePolicy.Fixed,
                SuggestedHeight = 30f,
                Text = "场景插画",
                Brush = root.Context.GetBrush("Conversation.HeaderText"),
                DoNotAcceptEvents = true
            };

            if (textWidget.Brush != null)
            {
                textWidget.Brush.FontSize = 26;
                textWidget.Brush.TextHorizontalAlignment = TextHorizontalAlignment.Right;
            }

            button.AddChild(textWidget);
            root.AddChild(button);
            TaleWorlds.Library.Debug.Print("[Illustrator] Successfully injected '场景插画' button into AnimusForge overlay.");
        }

        private static SizePolicy SizeSizeToCoverChildren(UIContext context)
        {
            return SizePolicy.CoverChildren;
        }

        private static void EnsureFallbackButton(Widget root)
        {
            if (root == null) return;

            var existing = root.FindChild(ButtonId, includeAllChildren: true) as ButtonWidget;
            if (existing != null)
            {
                TrackButton(existing);
                existing.IsVisible = IllustratorRuntime.IsEnabled("conversation");
                return;
            }

            if (!IllustratorRuntime.IsEnabled("conversation"))
            {
                return;
            }

            ButtonWidget button = new ButtonWidget(root.Context)
            {
                Id = ButtonId,
                WidthSizePolicy = SizePolicy.Fixed,
                HeightSizePolicy = SizePolicy.Fixed,
                SuggestedWidth = 150f,
                SuggestedHeight = 38f,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                MarginTop = 60f,
                MarginRight = 30f,
                Brush = root.Context.GetBrush("Popup.Done.Button.NineGrid") ?? root.Context.GetBrush("ButtonBrush2"),
                IsEnabled = true,
                DoNotAcceptEvents = false,
                DoNotPassEventsToChildren = true,
                IsVisible = true
            };

            button.ClickEventHandlers.Add(delegate
            {
                HandleConversationIllustrateClicked();
            });
            TrackButton(button);

            TextWidget textWidget = new TextWidget(root.Context)
            {
                WidthSizePolicy = SizePolicy.StretchToParent,
                HeightSizePolicy = SizePolicy.StretchToParent,
                Text = "【场景插画】",
                Brush = root.Context.GetBrush("Popup.Button.Text") ?? root.Context.GetBrush("Encyclopedia.SubPage.Info.Text"),
                DoNotAcceptEvents = true
            };

            if (textWidget.Brush != null)
            {
                textWidget.Brush.FontSize = 18;
                textWidget.Brush.TextHorizontalAlignment = TextHorizontalAlignment.Center;
                textWidget.Brush.TextVerticalAlignment = TextVerticalAlignment.Center;
            }

            button.AddChild(textWidget);
            root.AddChild(button);
        }

        private static void TrackButton(ButtonWidget button)
        {
            if (button == null) return;
            for (int i = InjectedButtons.Count - 1; i >= 0; i--)
            {
                if (!InjectedButtons[i].TryGetTarget(out var existing) || ReferenceEquals(existing, button))
                {
                    if (ReferenceEquals(existing, button)) return;
                    InjectedButtons.RemoveAt(i);
                }
            }
            InjectedButtons.Add(new WeakReference<ButtonWidget>(button));
        }

        public static void RefreshInjectedButtons()
        {
            IllustratorRuntime.AssertMainThread();
            bool visible = IllustratorRuntime.IsEnabled("conversation");
            for (int i = InjectedButtons.Count - 1; i >= 0; i--)
            {
                if (!InjectedButtons[i].TryGetTarget(out var button))
                {
                    InjectedButtons.RemoveAt(i);
                    continue;
                }
                button.IsVisible = visible;
            }
        }

        private static void HandleConversationIllustrateClicked()
        {
            ConversationVisualContext convContext = ConversationContextExtractor.ExtractFromCurrentConversation();
            IllustrationCardPopup.ShowForConversation(convContext);
        }

        private static void TryPatchHostNativeConversationReply(Harmony harmony)
        {
            try
            {
                Type shoutType = AccessTools.TypeByName("AnimusForge.ShoutBehavior");
                if (shoutType == null) return;
                // The native conversation overlay uses this shared presentation entry point;
                // its callback is the authoritative completed main-reply signal in-game.
                MethodInfo overlayMethod = AccessTools.Method(shoutType, "SubmitNativeConversationForOverlayAsync");
                if (overlayMethod != null)
                {
                    harmony.Patch(overlayMethod, prefix: new HarmonyMethod(typeof(ConversationIllustrationPatch), nameof(WrapNativeConversationReplyCallbackPrefix)));
                    _nativeConversationReplyPatchInstalled = true;
                }
                MethodInfo textMethod = AccessTools.Method(shoutType, "SubmitNativeConversationTextForExternalAsync",
                    new[] { typeof(string), typeof(Action<string>), typeof(string), typeof(Action<string>), typeof(Action<string, Hero, CharacterObject>) });
                if (textMethod != null)
                {
                    harmony.Patch(textMethod, prefix: new HarmonyMethod(typeof(ConversationIllustrationPatch), nameof(WrapNativeConversationReplyCallbackPrefix)));
                    _nativeConversationReplyPatchInstalled = true;
                }
                MethodInfo openingMethod = AccessTools.Method(shoutType, "SubmitNativeConversationNpcInitiatedOpeningForExternalAsync",
                    new[] { typeof(Action<string>), typeof(string), typeof(Action<string>), typeof(Action<string, Hero, CharacterObject>) });
                if (openingMethod != null)
                {
                    harmony.Patch(openingMethod, prefix: new HarmonyMethod(typeof(ConversationIllustrationPatch), nameof(WrapNativeOpeningCallbackPrefix)));
                }
                if (_nativeConversationReplyPatchInstalled)
                    LogAutoRedraw("hook_attached overlay=" + (overlayMethod != null) + " external=" + (textMethod != null));
                else LogAutoRedraw("hook_missing");
            }
            catch (Exception ex)
            {
                LogAutoRedraw("hook_failed " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static void WrapNativeConversationReplyCallbackPrefix(string playerText, ref Action<string, Hero, CharacterObject> onMainReplyReady)
        {
            WrapReplyCallback(playerText, ref onMainReplyReady);
        }

        private static void WrapNativeOpeningCallbackPrefix(ref Action<string, Hero, CharacterObject> onMainReplyReady)
        {
            WrapReplyCallback(null, ref onMainReplyReady);
        }

        private static void WrapReplyCallback(string playerText, ref Action<string, Hero, CharacterObject> callback)
        {
            Action<string, Hero, CharacterObject> observer;
            try { observer = IllustrationCardPopup.CaptureAutoReplyObserver(playerText); }
            catch (Exception ex) { LogAutoRedraw("capture_failed " + ex.GetType().Name); return; }
            if (observer == null) return;
            Action<string, Hero, CharacterObject> original = callback;
            callback = (content, targetHero, targetCharacter) =>
            {
                try { original?.Invoke(content, targetHero, targetCharacter); }
                finally
                {
                    try { observer(content, targetHero, targetCharacter); }
                    catch (Exception ex) { LogAutoRedraw("callback_failed " + ex.GetType().Name); }
                }
            };
        }

        internal static void LogAutoRedraw(string message)
        {
            try { global::AnimusForge.Logger.Log("Illustrator", "[AutoRedraw] " + message); } catch { }
        }

        internal static void OnAgentJoinedConversation(IAgent agent)
        {
            AttachConversationContinuedHandler();
            _lastAutoRedrawSentence = string.Empty;
        }

        internal static void OnConversationEnded(IEnumerable<CharacterObject> characters)
        {
            DetachConversationContinuedHandler();
            _lastAutoRedrawSentence = string.Empty;
            IllustrationCardPopup.ClearConversationSessionCache();
            IllustratorRuntime.Post(IllustrationCardPopup.CloseActiveConversation);
        }

        private static void AttachConversationContinuedHandler()
        {
            var manager = Campaign.Current?.ConversationManager;
            if (manager == null || ReferenceEquals(manager, _conversationManagerSubscription)) return;
            DetachConversationContinuedHandler();
            _conversationManagerSubscription = manager;
            manager.ConversationContinued += OnConversationContinued;
        }

        private static void DetachConversationContinuedHandler()
        {
            if (_conversationManagerSubscription == null) return;
            _conversationManagerSubscription.ConversationContinued -= OnConversationContinued;
            _conversationManagerSubscription = null;
        }

        private static void OnConversationContinued()
        {
            if (_nativeConversationReplyPatchInstalled) return;
            if (IllustratorSettings.Instance?.AutoGenerateConversationIllustrationFullscreen != true) return;
            string sentence = string.Empty;
            try { sentence = Campaign.Current?.ConversationManager?.CurrentSentenceText ?? string.Empty; } catch { }
            sentence = sentence.Trim();
            if (sentence.Length == 0 || string.Equals(sentence, _lastAutoRedrawSentence, StringComparison.Ordinal)) return;
            _lastAutoRedrawSentence = sentence;
            IllustratorRuntime.Post(IllustrationCardPopup.AutoRedrawActiveConversation);
        }

    }
}
