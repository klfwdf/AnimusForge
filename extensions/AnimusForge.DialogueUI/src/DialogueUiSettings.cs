using System;
using System.Runtime.CompilerServices;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using MCM.Common;
using TaleWorlds.Localization;

namespace AnimusForge.DialogueUI
{
    internal enum ShoutPanelStyle
    {
        Original = 0,
        Scroll = 1,
        SideFolio = 2
    }

    public sealed class DialogueUiSettings : AttributeGlobalSettings<DialogueUiSettings>
    {
        public override string Id => "AnimusForge_DialogueUI_v1";
        public override string DisplayName => new TextObject("{=AFD_MCM_DisplayName}AnimusForge - Dialogue UI").ToString();
        public override string FolderName => "AnimusForge";
        public override string FormatType => "json";

        [SettingPropertyDropdown("{=AFD_MCM_InterfaceStyle}Interface style", Order = 0, RequireRestart = false,
            HintText = "{=AFD_MCM_InterfaceStyle_Hint}Original uses native conversation; scene shouts keep the paused scroll input. New UI uses parchment conversation, map conversation, and history/give panels. New UI is the default and applies when the interface is opened again; no restart is required.")]
        [SettingPropertyGroup("{=AFD_MCM_InterfaceGroup}1. Interface", GroupOrder = 1)]
        public Dropdown<string> InterfaceStyleDropdown { get; set; } =
            new Dropdown<string>(new[] { Localize("AFD_MCM_Interface_Original", "Original"), Localize("AFD_MCM_Interface_New", "New UI") }, 1);

        // Compatibility accessor for existing integrations. MCM persists only
        // attributed properties, so the new selector has its own saved key;
        // configs without that key use the requested new-UI default.
        public bool EnableSkin
        {
            get => InterfaceStyleDropdown?.SelectedIndex != 0;
            set
            {
                if (InterfaceStyleDropdown == null)
                    InterfaceStyleDropdown = new Dropdown<string>(new[] { Localize("AFD_MCM_Interface_Original", "Original"), Localize("AFD_MCM_Interface_New", "New UI") }, value ? 1 : 0);
                else InterfaceStyleDropdown.SelectedIndex = value ? 1 : 0;
            }
        }

        [SettingPropertyInteger("{=AFD_MCM_BodyFontSize}Body font size", 14, 36, "0", Order = 5, RequireRestart = false,
            HintText = "{=AFD_MCM_BodyFontSize_Hint}Font size for New UI conversation text, options, history, and input. Default: 24. Applies when the interface is opened again; buttons, names, and the native interface are unchanged.")]
        [SettingPropertyGroup("{=AFD_MCM_InterfaceGroup}1. Interface", GroupOrder = 1)]
        public int BodyFontSize { get; set; } = 24;

        [SettingPropertyDropdown("{=AFD_MCM_ShoutStyle}Scene shout panel style", Order = 1, RequireRestart = false,
            HintText = "{=AFD_MCM_ShoutStyle_Hint}Paused scroll reuses the bottom scroll input, pauses the game, hides the audience docket, and is also the fallback. Scroll keeps the input at the bottom with an audience docket on the right. Side folio uses a vertical conversation folio. The latter two use a persistent multi-person session in peaceful scenes; during battles, the shout key opens paused input directly. All three panels provide a manual illustration button when image generation is enabled.")]
        [SettingPropertyGroup("{=AFD_MCM_InterfaceGroup}1. Interface", GroupOrder = 1)]
        public Dropdown<string> ShoutPanelStyleDropdown { get; set; } =
            new Dropdown<string>(new[] { Localize("AFD_MCM_ShoutStyle_PausedScroll", "Paused scroll"), Localize("AFD_MCM_ShoutStyle_Scroll", "Scroll"), Localize("AFD_MCM_ShoutStyle_SideFolio", "Side folio") }, 1);

        [SettingPropertyBool("{=AFD_MCM_AutoAi}Enter AI mode automatically", Order = 3, RequireRestart = false,
            HintText = "{=AFD_MCM_AutoAi_Hint}When enabled, scene or map conversations enter AI input mode automatically. Pair with Only Hero enters AI mode automatically to limit the targets. When disabled, ordinary conversation options appear first and AI can still be selected manually. Applies when the next conversation opens.")]
        [SettingPropertyGroup("{=AFD_MCM_InterfaceGroup}1. Interface", GroupOrder = 1)]
        public bool AutoEnterAiMode { get; set; } = true;

        [SettingPropertyBool("{=AFD_MCM_AutoAiHeroOnly}Only Hero enters AI mode automatically", Order = 4, RequireRestart = false,
            HintText = "{=AFD_MCM_AutoAiHeroOnly_Hint}Applies only when automatic AI mode is enabled. When enabled, only Heroes (lords, companions, notable characters, and other individually identified people) enter AI mode automatically; ordinary bandits, deserters, and soldiers stay in ordinary mode. Disable it to restore automatic switching for all targets. Manual switching remains available.")]
        [SettingPropertyGroup("{=AFD_MCM_InterfaceGroup}1. Interface", GroupOrder = 1)]
        public bool AutoEnterAiModeHeroOnly { get; set; } = true;

        [SettingPropertyBool("{=AFD_MCM_HistoryDelete}Show delete buttons in conversation history", Order = 2, RequireRestart = false,
            HintText = "{=AFD_MCM_HistoryDelete_Hint}Show a delete button beside each persistent conversation-history line. Deleting removes the visible line and that day's uncompressed memory draft; content already compressed into memory blocks is unaffected.")]
        [SettingPropertyGroup("{=AFD_MCM_InterfaceGroup}1. Interface", GroupOrder = 1)]
        public bool ShowHistoryDelete { get; set; } = true;

        private static string Localize(string id, string fallback)
            => new TextObject("{=" + id + "}" + fallback).ToString();
    }

    // Reads happen at movie load, hotkey release and panel open, never per frame.
    // Every access to DialogueUiSettings sits in its own non-inlined method so a missing
    // MCM assembly surfaces as a catchable exception here instead of a type-load failure
    // in the caller; the module then keeps working on defaults.
    internal static class DialogueUiOptions
    {
        private static bool _unavailable;

        internal static bool AutoEnterAiMode => Read(ReadAutoEnterAiMode, true);
        internal static bool AutoEnterAiModeHeroOnly => Read(ReadAutoEnterAiModeHeroOnly, true);
        internal static bool SkinEnabled => Read(ReadSkin, true);
        internal static bool ShowHistoryDelete => Read(ReadHistoryDelete, true);
        internal static int BodyFontSize => Math.Max(14, Math.Min(36, Read(ReadBodyFontSize, 24)));

        internal static ShoutPanelStyle PanelStyle
        {
            get
            {
                int index = Read(ReadStyleIndex, 1);
                return index >= 0 && index <= (int)ShoutPanelStyle.SideFolio ? (ShoutPanelStyle)index : ShoutPanelStyle.Scroll;
            }
        }

        // Wheel and persistent session exist only for the two redesigned styles.
        internal static bool UsesSceneSession => SkinEnabled && PanelStyle != ShoutPanelStyle.Original;

        private static T Read<T>(Func<T> read, T fallback)
        {
            if (_unavailable) return fallback;
            try { return read(); }
            catch (Exception ex)
            {
                _unavailable = true;
                DialogueUiRuntime.Log("MCM settings unavailable; using defaults. " + ex.GetType().Name + ": " + ex.Message);
                return fallback;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ReadAutoEnterAiMode() => DialogueUiSettings.Instance?.AutoEnterAiMode ?? true;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ReadAutoEnterAiModeHeroOnly() => DialogueUiSettings.Instance?.AutoEnterAiModeHeroOnly ?? true;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ReadSkin() => DialogueUiSettings.Instance?.EnableSkin ?? true;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ReadHistoryDelete() => DialogueUiSettings.Instance?.ShowHistoryDelete ?? true;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ReadStyleIndex() => DialogueUiSettings.Instance?.ShoutPanelStyleDropdown?.SelectedIndex ?? 1;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ReadBodyFontSize() => DialogueUiSettings.Instance?.BodyFontSize ?? 24;
    }
}
