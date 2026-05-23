namespace TaleWorlds.Core;

public abstract class RidingModel : MBGameModel<RidingModel>
{
	public abstract float CalculateAcceleration(in EquipmentElement mountElement, in EquipmentElement harnessElement, int ridingSkill);
}
