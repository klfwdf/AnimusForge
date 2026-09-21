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
        private static bool _shoutResponsePatchInstalled;
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
                TryPatchHostNpcSpeech(activeHarmony);
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

        private static void TryPatchHostNpcSpeech(Harmony harmony)
        {
            try
            {
                Type shoutType = AccessTools.TypeByName("AnimusForge.ShoutBehavior");
                MethodInfo speechMethod = AccessTools.Method(shoutType, "ShowNpcSpeechOutput");
                if (speechMethod == null) return;
                harmony.Patch(speechMethod, postfix: new HarmonyMethod(typeof(ConversationIllustrationPatch), nameof(ShowNpcSpeechOutputPostfix)));
                _shoutResponsePatchInstalled = true;
                Debug.Print("[Illustrator] Auto conversation redraw hook attached to ShoutBehavior.ShowNpcSpeechOutput.");
            }
            catch (Exception ex)
            {
                Debug.Print("[Illustrator] Failed to attach ShoutBehavior response hook: " + ex.GetType().Name);
            }
        }

        internal static void OnAgentJoinedConversation(IAgent agent)
        {
            if (IllustratorSettings.Instance?.AutoGenerateConversationIllustrationFullscreen != true)
                return;
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
            if (_shoutResponsePatchInstalled) return;
            if (IllustratorSettings.Instance?.AutoGenerateConversationIllustrationFullscreen != true) return;
            string sentence = string.Empty;
            try { sentence = Campaign.Current?.ConversationManager?.CurrentSentenceText ?? string.Empty; } catch { }
            sentence = sentence.Trim();
            if (sentence.Length > 0 && string.Equals(sentence, _lastAutoRedrawSentence, StringComparison.Ordinal)) return;
            _lastAutoRedrawSentence = sentence;
            IllustratorRuntime.Post(IllustrationCardPopup.AutoRedrawActiveConversation);
        }

        private static void ShowNpcSpeechOutputPostfix(string content)
        {
            if (string.IsNullOrWhiteSpace(content) || IllustratorSettings.Instance?.AutoGenerateConversationIllustrationFullscreen != true)
                return;
            IllustratorRuntime.Post(IllustrationCardPopup.AutoRedrawActiveConversation);
        }
    }
}
