using System;
using System.Runtime.CompilerServices;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using MCM.Common;

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
        public override string DisplayName => "AnimusForge - 对话界面 (DialogueUI)";
        public override string FolderName => "AnimusForge";
        public override string FormatType => "json";

        [SettingPropertyBool("启用 AnimusForge 对话界面", Order = 0, RequireRestart = false,
            HintText = "总开关。开启时使用本模块的羊皮纸对话、地图对话、场景喊话与历史/给予面板；关闭后全部回到 AnimusForge 原界面与原版对话界面。下次打开界面时生效，无需重启。")]
        [SettingPropertyGroup("1. 界面", GroupOrder = 1)]
        public bool EnableSkin { get; set; } = true;

        [SettingPropertyDropdown("场景喊话面板风格", Order = 1, RequireRestart = false,
            HintText = "原样：保持现有的 T/Y 喊话流程与输入框。卷轴式：底部卷轴输入 + 右侧受众挂札。右侧手札：右侧竖版对谈手札。卷轴式与右侧手札会把 T/Y 合并为「按住框选、松开展开轮盘」，并进入持续的场景多人会话。")]
        [SettingPropertyGroup("1. 界面", GroupOrder = 1)]
        public Dropdown<string> ShoutPanelStyleDropdown { get; set; } =
            new Dropdown<string>(new[] { "原样", "卷轴式", "右侧手札" }, 0);

        [SettingPropertyBool("Hero 对话自动进入 AI 模式", Order = 3, RequireRestart = false,
            HintText = "开启时，仅与 Hero（领主、同伴、要人等有独立身份的人物）对话自动进入 AI 模式；普通劫匪、逃兵、士兵等非 Hero 保持普通模式。关闭时所有对话先用普通模式；均可手动切换。下次打开对话生效。")]
        [SettingPropertyGroup("1. 界面", GroupOrder = 1)]
        public bool AutoEnterAiMode { get; set; } = true;

        [SettingPropertyBool("对话历史显示删除按钮", Order = 2, RequireRestart = false,
            HintText = "开启后，对话历史面板的每条持久记录旁显示删除按钮。删除会同时移除该条可见历史与当日尚未压缩的记忆草稿；已压缩进记忆块的内容不受影响。")]
        [SettingPropertyGroup("1. 界面", GroupOrder = 1)]
        public bool ShowHistoryDelete { get; set; } = true;
    }

    // Reads happen at movie load, hotkey release and panel open, never per frame.
    // Every access to DialogueUiSettings sits in its own non-inlined method so a missing
    // MCM assembly surfaces as a catchable exception here instead of a type-load failure
    // in the caller; the module then keeps working on defaults.
    internal static class DialogueUiOptions
    {
        private static bool _unavailable;

        internal static bool AutoEnterAiMode => Read(ReadAutoEnterAiMode, true);
        internal static bool SkinEnabled => Read(ReadSkin, true);
        internal static bool ShowHistoryDelete => Read(ReadHistoryDelete, true);

        internal static ShoutPanelStyle PanelStyle
        {
            get
            {
                int index = Read(ReadStyleIndex, 0);
                return index >= 0 && index <= (int)ShoutPanelStyle.SideFolio ? (ShoutPanelStyle)index : ShoutPanelStyle.Original;
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
        private static bool ReadSkin() => DialogueUiSettings.Instance?.EnableSkin ?? true;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ReadHistoryDelete() => DialogueUiSettings.Instance?.ShowHistoryDelete ?? true;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ReadStyleIndex() => DialogueUiSettings.Instance?.ShoutPanelStyleDropdown?.SelectedIndex ?? 0;
    }
}
