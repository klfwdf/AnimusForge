using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;

namespace AnimusForge;

public partial class DuelSettings
{
    [SettingPropertyBool("{=AF_TERMINAL_UI_TerminalHotkeyEnabled_Title}Enable terminal shortcut", Order = 0, RequireRestart = false,
        HintText = "{=AF_TERMINAL_UI_TerminalHotkeyEnabled_Hint}Allows opening the terminal with its configured key. At least one terminal entry remains available.")]
    [SettingPropertyGroup("{=AF_TERMINAL_UI_Entry_Group}Terminal entry")]
    public bool TerminalHotkeyEnabled { get; set; } = true;

    [SettingPropertyBool("{=AF_TERMINAL_UI_TerminalMapIconEnabled_Title}Show terminal navigation icon", Order = 1, RequireRestart = false,
        HintText = "{=AF_TERMINAL_UI_TerminalMapIconEnabled_Hint}Shows the terminal entry on the campaign map navigation bar. At least one terminal entry remains available.")]
    [SettingPropertyGroup("{=AF_TERMINAL_UI_Entry_Group}Terminal entry")]
    public bool TerminalMapIconEnabled { get; set; } = true;
}
