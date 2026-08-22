using System;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using AnimusForge.SiegeAftermathIntervention;

namespace AnimusForge;

/// <summary>
/// Keeps the GCCZ manual entry isolated from the main AF settings implementation.
/// </summary>
public partial class DuelSettings
{
	[SettingPropertyButton("{=gccz_town_manual_setting_name}", -1, true, "", Content = "{=gccz_town_manual_setting_content}", Order = -100, RequireRestart = false, HintText = "{=gccz_town_manual_setting_hint}")]
	[SettingPropertyGroup(SiegeNpcResponseLimitProfile.McmGroupName)]
	public Action OpenGcczTownManual { get; set; } = GcczTownManualInquiryPresenter.Open;
}
