using System.Collections.Generic;
using System.Linq;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Common;

namespace AnimusForge;

// Courier stationery preset. Kept in its own partial file so DuelSettings.cs stays untouched.
public partial class DuelSettings
{
	private const string CourierLetterGroup = "{=AF_MCM_Group_CourierStationery}NPC contact / courier stationery";

	// The dropdown's SelectedIndex is what MCM persists; the id is derived from it (same pattern as the reasoning-effort dropdowns).
	private Dropdown<string> _courierLetterThemeDropdown = NormalizeCourierLetterThemeDropdown(null);

	public string CourierLetterThemeId => CourierLetterThemes.All[NormalizeCourierLetterThemeDropdown(_courierLetterThemeDropdown).SelectedIndex].Id;

	[SettingPropertyDropdown("{=AF_MCM_Courier_Stationery}Letter style", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_Courier_Stationery_Hint}Choose the Calradia faction stationery preset used by courier letters, replies, and composition windows.")]
	[SettingPropertyGroup(CourierLetterGroup)]
	public Dropdown<string> CourierLetterThemeDropdown
	{
		get
		{
			_courierLetterThemeDropdown = NormalizeCourierLetterThemeDropdown(_courierLetterThemeDropdown);
			return _courierLetterThemeDropdown;
		}
		set
		{
			_courierLetterThemeDropdown = NormalizeCourierLetterThemeDropdown(value);
		}
	}

	private static Dropdown<string> NormalizeCourierLetterThemeDropdown(Dropdown<string> dropdown)
	{
		List<string> options = CourierLetterThemes.All.Select(x => x.DisplayName).ToList();
		int index = dropdown?.SelectedIndex ?? 0;
		if (index < 0 || index >= options.Count)
		{
			index = 0;
		}
		return new Dropdown<string>(options, index);
	}
}
