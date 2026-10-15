using System;
using AnimusForge.SiegeAftermathIntervention;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using TaleWorlds.Library;
namespace AnimusForge;
public partial class DuelSettings
{
    [SettingPropertyInteger("{=AF_GCCZ_VILLAGE_Initial}GCCZ village initial cooperation", 0, 5, "0", Order = 9, RequireRestart = false,
        HintText = "{=AF_GCCZ_VILLAGE_InitialHint}Initializes only unrecorded villages. Cooperation is saved per village, gained by persuasion and spent by successful voluntary aid; no numeric gameplay display.")]
    [SettingPropertyGroup(SiegeNpcResponseLimitProfile.McmGroupName)]
    public int GcczVillageInitialAssistance { get; set; } = 0;
    [SettingPropertyButton("{=AF_GCCZ_VILLAGE_Tutorial}GCCZ village tutorial", -1, true, "", Content = "{=AF_GCCZ_VILLAGE_View}View tutorial", Order = 8, RequireRestart = false,
        HintText = "{=AF_GCCZ_VILLAGE_TutorialHint}Foreign village entry, budgets, cooperation, personnel settlement and currently unavailable features.")]
    [SettingPropertyGroup(SiegeNpcResponseLimitProfile.McmGroupName)]
    public Action ShowGcczVillageTutorial { get; set; } = () => GcczVillageCastleTutorialPresenter.Show(false);
    [SettingPropertyButton("{=AF_GCCZ_TownTutorial}GCCZ town tutorial", -1, true, "", Content = "{=AF_GCCZ_VILLAGE_View}View tutorial", Order = 7, RequireRestart = false)]
    [SettingPropertyGroup(SiegeNpcResponseLimitProfile.McmGroupName)]
    public Action ShowGcczTownTutorial { get; set; } = GcczTownManualInquiryPresenter.Open;
    [SettingPropertyButton("{=AF_GCCZ_CastleTutorial}GCCZ castle tutorial", -1, true, "", Content = "{=AF_GCCZ_VILLAGE_View}View tutorial", Order = 9, RequireRestart = false)]
    [SettingPropertyGroup(SiegeNpcResponseLimitProfile.McmGroupName)]
    public Action ShowGcczCastleTutorial { get; set; } = () => GcczVillageCastleTutorialPresenter.Show(true);
}
