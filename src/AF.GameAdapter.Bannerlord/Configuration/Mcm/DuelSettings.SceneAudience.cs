using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace AnimusForge;

public partial class DuelSettings
{
    [SettingPropertyBool("自动屏蔽未框选人物", Order = 15, RequireRestart = false,
        HintText = "默认关闭。开启后，场景喊话只让框选或手动邀请的人物参与，后来走近的旁观者不会自动加入；当前会话中此前自动加入的人物也会被屏蔽。重新框选、点击人物或手动设为参与/锁定可邀请加入。手动屏蔽始终保留，保存后从下一轮喊话生效。")]
    [SettingPropertyGroup("3. 场景喊话")]
    public bool AutoExcludeUnframedShoutParticipants { get; set; } = false;

    public static bool ShouldAutoExcludeUnframedShoutParticipants()
    {
        try
        {
            // Audience and panel reads use loaded settings without prompt initialization or migrations.
            return GlobalSettings<DuelSettings>.Instance?.AutoExcludeUnframedShoutParticipants ?? false;
        }
        catch
        {
            return false;
        }
    }
}
