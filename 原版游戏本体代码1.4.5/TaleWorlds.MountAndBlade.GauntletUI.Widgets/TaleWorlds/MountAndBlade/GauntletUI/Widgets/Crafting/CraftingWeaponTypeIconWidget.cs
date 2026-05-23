using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Crafting;

public class CraftingWeaponTypeIconWidget : Widget
{
	private string _weaponType;

	[Editor(false)]
	public string WeaponType
	{
		get
		{
			return _weaponType;
		}
		set
		{
			if (value != _weaponType)
			{
				_weaponType = value;
				UpdateIconVisual();
				OnPropertyChanged(value, "WeaponType");
			}
		}
	}

	public CraftingWeaponTypeIconWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateIconVisual()
	{
		base.Sprite = base.Context.SpriteData.GetSprite("Crafting\\WeaponTypes\\" + WeaponType);
	}
}
