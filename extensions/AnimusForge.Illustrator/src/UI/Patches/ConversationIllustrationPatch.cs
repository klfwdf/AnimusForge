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
        private static bool _patched;
        private static bool _nativeConversationReplyPatchInstalled;
        private static TaleWorlds.CampaignSystem.Conversation.ConversationManager _conversationManagerSubscription;
        private static string _lastAutoRedrawSentence = string.Empty;

        internal static void Reset()
        {
            DetachConversationContinuedHandler();
            _patched = false;
            _nativeConversationReplyPatchInstalled = false;
            _lastAutoRedrawSentence = string.Empty;
        }

        public static void EnsurePatched(Harmony harmony)
        {
            if (_patched) return;
            _patched = true;

            try
            {
                Harmony activeHarmony = harmony ?? new Harmony("AnimusForge.Illustrator.Conversation");
                TryPatchHostNativeConversationReply(activeHarmony);
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[Illustrator] Failed to patch conversation: {ex.Message}");
            }
        }

        // DialogueUI owns the single scene-illustration button and resolves this callback.
        // Do not inject another button into MapConversation, MissionConversation or the overlay.
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
