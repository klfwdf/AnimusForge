using System.Collections.Generic;
using System.Linq;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Common;

namespace AnimusForge;

// Courier stationery preset. Kept in its own partial file so DuelSettings.cs stays untouched.
public partial class DuelSettings
{
	private const string CourierLetterGroup = "7. NPC主动接触/信使信纸";

	// The dropdown's SelectedIndex is what MCM persists; the id is derived from it (same pattern as the reasoning-effort dropdowns).
	private Dropdown<string> _courierLetterThemeDropdown = NormalizeCourierLetterThemeDropdown(null);

	public string CourierLetterThemeId => CourierLetterThemes.All[NormalizeCourierLetterThemeDropdown(_courierLetterThemeDropdown).SelectedIndex].Id;

	[SettingPropertyDropdown("信纸样式", Order = 0, RequireRestart = false, HintText = "选择信使信件弹窗使用的卡拉迪亚国家信纸预设（纹样与蜡封）。所有来信、回信和写信界面统一使用此样式。")]
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
