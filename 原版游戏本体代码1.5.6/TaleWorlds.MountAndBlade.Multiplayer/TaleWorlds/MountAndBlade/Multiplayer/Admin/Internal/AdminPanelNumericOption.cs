using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.Admin.Internal;

internal class AdminPanelNumericOption : AdminPanelOption<int>, IAdminPanelNumericOption, IAdminPanelOption<int>, IAdminPanelOption
{
	private int? _minimumValue;

	private int? _maximumValue;

	public AdminPanelNumericOption(string uniqueId)
		: base(uniqueId)
	{
	}

	protected override bool AreEqualValues(int first, int second)
	{
		return first == second;
	}

	public AdminPanelNumericOption SetMinimumValue(int value)
	{
		_minimumValue = value;
		return this;
	}

	public AdminPanelNumericOption SetMaximumValue(int value)
	{
		_maximumValue = value;
		return this;
	}

	public AdminPanelNumericOption SetMinimumAndMaximumFrom(MultiplayerOptions.OptionType optionType)
	{
		MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
		if (optionProperty != null && optionProperty.HasBounds)
		{
			_minimumValue = optionType.GetMinimumValue();
			_maximumValue = optionType.GetMaximumValue();
			SetValue(MBMath.ClampInt(base.CurrentValue, _minimumValue.Value, _maximumValue.Value));
		}
		return this;
	}

	public int? GetMinimumValue()
	{
		return _minimumValue;
	}

	public int? GetMaximumValue()
	{
		return _maximumValue;
	}
}
