using System;
using TaleWorlds.MountAndBlade.Diamond.Cosmetics;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Armory.CosmeticItem;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Armory.CosmeticCategory;

public class MPArmoryTauntCosmeticCategoryVM : MPArmoryCosmeticCategoryBaseVM
{
	public readonly MPArmoryCosmeticsVM.TauntCategoryFlag TauntCategory;

	public static event Action<MPArmoryTauntCosmeticCategoryVM> OnSelected;

	public MPArmoryTauntCosmeticCategoryVM(MPArmoryCosmeticsVM.TauntCategoryFlag tauntCategory)
		: base(CosmeticsManager.CosmeticType.Taunt)
	{
		TauntCategory = tauntCategory;
		base.CosmeticCategoryName = tauntCategory.ToString();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		base.AvailableCosmetics.ApplyActionOnAllItems(delegate(MPArmoryCosmeticItemBaseVM c)
		{
			c.RefreshValues();
		});
	}

	protected override void ExecuteSelectCategory()
	{
		MPArmoryTauntCosmeticCategoryVM.OnSelected?.Invoke(this);
	}
}
