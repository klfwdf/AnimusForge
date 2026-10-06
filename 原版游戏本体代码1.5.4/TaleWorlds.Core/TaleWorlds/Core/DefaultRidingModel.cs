using TaleWorlds.Library;

namespace TaleWorlds.Core;

public class DefaultRidingModel : RidingModel
{
	public override float CalculateAcceleration(in EquipmentElement mountElement, in EquipmentElement harnessElement, int ridingSkill)
	{
		float num = (float)mountElement.GetModifiedMountManeuver(in harnessElement) * 0.008f;
		if (ridingSkill >= 0)
		{
			num *= 0.7f + 0.003f * ((float)ridingSkill - 1.5f * (float)mountElement.Item.Difficulty);
		}
		return MathF.Clamp(num, 0.15f, 0.7f);
	}
}
