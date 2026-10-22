using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace AnimusForge;

public partial class DuelSettings
{
    [SettingPropertyBool("{=AF_MCM_SceneAudience_AutoExclude}Auto-exclude unframed participants", Order = 15, RequireRestart = false,
        HintText = "{=AF_MCM_SceneAudience_AutoExclude_Hint}When enabled, scene shout includes only framed or manually invited characters; later bystanders are not added automatically. Takes effect on the next shout.")]
    [SettingPropertyGroup("{=AF_MCM_Group_SceneShout}Scene shout")]
    public bool AutoExcludeUnframedShoutParticipants { get; set; } = true;

    public static bool ShouldAutoExcludeUnframedShoutParticipants()
    {
        try
        {
            // Audience and panel reads use loaded settings without prompt initialization or migrations.
            return GlobalSettings<DuelSettings>.Instance?.AutoExcludeUnframedShoutParticipants ?? true;
        }
        catch
        {
            return true;
        }
    }
}
