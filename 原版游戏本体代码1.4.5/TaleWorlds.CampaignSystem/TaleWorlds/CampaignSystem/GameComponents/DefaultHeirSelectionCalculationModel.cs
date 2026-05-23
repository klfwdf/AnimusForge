using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultHeirSelectionCalculationModel : HeirSelectionCalculationModel
{
	private const int MaleHeirPoint = 10;

	private const int EldestPoint = 5;

	private const int YoungestPoint = -5;

	private const int DirectDescendentPoint = 10;

	private const int CollateralHeirPoint = 10;

	public override int HighestSkillPoint => 5;

	public override int CalculateHeirSelectionPoint(Hero candidateHeir, Hero deadHero, ref Hero maxSkillHero)
	{
		return CalculateHeirSelectionPointInternal(candidateHeir, deadHero, ref maxSkillHero);
	}

	private static int CalculateHeirSelectionPointInternal(Hero candidateHeir, Hero deadHero, ref Hero maxSkillHero)
	{
		int num = 0;
		if (!candidateHeir.IsFemale)
		{
			num += 10;
		}
		IOrderedEnumerable<Hero> source = from h in candidateHeir.Clan.Heroes
			where h != deadHero
			orderby h.Age
			select h;
		float? num2 = source.LastOrDefault()?.Age;
		float? num3 = source.FirstOrDefault()?.Age;
		if (candidateHeir.Age == num2)
		{
			num += 5;
		}
		else if (candidateHeir.Age == num3)
		{
			num += -5;
		}
		if (deadHero.Father == candidateHeir || deadHero.Mother == candidateHeir || candidateHeir.Father == deadHero || candidateHeir.Mother == deadHero || candidateHeir.Father == deadHero.Father || candidateHeir.Mother == deadHero.Mother)
		{
			num += 10;
		}
		Hero father = deadHero.Father;
		while (father != null && father.Father != null)
		{
			father = father.Father;
		}
		if (father?.Children != null && DoesHaveSameBloodLine(father?.Children, candidateHeir))
		{
			num += 10;
		}
		int num4 = 0;
		foreach (SkillObject item in Skills.All)
		{
			num4 += candidateHeir.GetSkillValue(item);
		}
		int num5 = 0;
		foreach (SkillObject item2 in Skills.All)
		{
			num5 += maxSkillHero.GetSkillValue(item2);
		}
		if (num4 > num5)
		{
			maxSkillHero = candidateHeir;
		}
		return num;
	}

	private static bool DoesHaveSameBloodLine(IEnumerable<Hero> children, Hero candidateHeir)
	{
		if (!children.Any())
		{
			return false;
		}
		foreach (Hero child in children)
		{
			if (child == candidateHeir)
			{
				return true;
			}
			if (DoesHaveSameBloodLine(child.Children, candidateHeir))
			{
				return true;
			}
		}
		return false;
	}
}
