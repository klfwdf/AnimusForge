using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Multiplayer.Admin.Internal;

internal class AdminPanelMultiSelectionItem : IAdminPanelMultiSelectionItem
{
	private string _value;

	private string _displayName;

	private bool _isFallbackValue;

	private bool _isDisabled;

	private bool _canBeApplied;

	public string Value => _value;

	public string DisplayName => _displayName?.ToString();

	public bool IsFallbackValue => _isFallbackValue;

	public bool IsDisabled => _isDisabled;

	public bool CanBeApplied => _canBeApplied;

	public AdminPanelMultiSelectionItem(string value, TextObject displayName, bool isFallbackValue = false, bool isDisabled = false, bool canBeApplied = true)
	{
		_value = value;
		_displayName = displayName?.ToString();
		_isFallbackValue = isFallbackValue;
		_isDisabled = isDisabled;
		_canBeApplied = canBeApplied;
	}

	public void SetIsFallbackValue(bool value)
	{
		_isFallbackValue = value;
	}

	public void SetIsDisabled(bool value)
	{
		_isDisabled = value;
	}

	public void SetCanBeApplied(bool value)
	{
		_canBeApplied = value;
	}
}
