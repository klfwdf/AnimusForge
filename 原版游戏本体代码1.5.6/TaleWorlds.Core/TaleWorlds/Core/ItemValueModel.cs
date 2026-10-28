namespace TaleWorlds.Core;

public abstract class ItemValueModel : MBGameModel<ItemValueModel>
{
	public abstract float GetEquipmentValueFromTier(float itemTierf);

	public abstract float CalculateTier(ItemObject item);

	public abstract int CalculateValue(ItemObject item);

	public abstract bool GetIsTransferable(ItemObject item);
}
