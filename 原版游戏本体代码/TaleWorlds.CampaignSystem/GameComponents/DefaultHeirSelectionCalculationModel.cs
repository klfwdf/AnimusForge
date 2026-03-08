using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000118 RID: 280
	public class DefaultHeirSelectionCalculationModel : HeirSelectionCalculationModel
	{
		// Token: 0x17000664 RID: 1636
		// (get) Token: 0x060017E4 RID: 6116 RVA: 0x00071B68 File Offset: 0x0006FD68
		public override int HighestSkillPoint
		{
			get
			{
				return 5;
			}
		}

		// Token: 0x060017E5 RID: 6117 RVA: 0x00071B6B File Offset: 0x0006FD6B
		public override int CalculateHeirSelectionPoint(Hero candidateHeir, Hero deadHero, ref Hero maxSkillHero)
		{
			return DefaultHeirSelectionCalculationModel.CalculateHeirSelectionPointInternal(candidateHeir, deadHero, ref maxSkillHero);
		}

		// Token: 0x060017E6 RID: 6118 RVA: 0x00071B78 File Offset: 0x0006FD78
		private static int CalculateHeirSelectionPointInternal(Hero candidateHeir, Hero deadHero, ref Hero maxSkillHero)
		{
			int num = 0;
			if (!candidateHeir.IsFemale)
			{
				num += 10;
			}
			IOrderedEnumerable<Hero> source = from x in candidateHeir.Clan.Heroes
				where x != deadHero
				select x into h
				orderby h.Age
				select h;
			Hero hero = source.LastOrDefault<Hero>();
			float? num2 = ((hero != null) ? new float?(hero.Age) : null);
			Hero hero2 = source.FirstOrDefault<Hero>();
			float? num3 = ((hero2 != null) ? new float?(hero2.Age) : null);
			float age = candidateHeir.Age;
			float? num4 = num2;
			if ((age == num4.GetValueOrDefault()) & (num4 != null))
			{
				num += 5;
			}
			else
			{
				float age2 = candidateHeir.Age;
				num4 = num3;
				if ((age2 == num4.GetValueOrDefault()) & (num4 != null))
				{
					num += -5;
				}
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
			if (((father != null) ? father.Children : null) != null && DefaultHeirSelectionCalculationModel.DoesHaveSameBloodLine((father != null) ? father.Children : null, candidateHeir))
			{
				num += 10;
			}
			int num5 = 0;
			foreach (SkillObject skill in Skills.All)
			{
				num5 += candidateHeir.GetSkillValue(skill);
			}
			int num6 = 0;
			foreach (SkillObject skill2 in Skills.All)
			{
				num6 += maxSkillHero.GetSkillValue(skill2);
			}
			if (num5 > num6)
			{
				maxSkillHero = candidateHeir;
			}
			return num;
		}

		// Token: 0x060017E7 RID: 6119 RVA: 0x00071DC0 File Offset: 0x0006FFC0
		private static bool DoesHaveSameBloodLine(IEnumerable<Hero> children, Hero candidateHeir)
		{
			if (!children.Any<Hero>())
			{
				return false;
			}
			foreach (Hero hero in children)
			{
				if (hero == candidateHeir)
				{
					return true;
				}
				if (DefaultHeirSelectionCalculationModel.DoesHaveSameBloodLine(hero.Children, candidateHeir))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x040007C9 RID: 1993
		private const int MaleHeirPoint = 10;

		// Token: 0x040007CA RID: 1994
		private const int EldestPoint = 5;

		// Token: 0x040007CB RID: 1995
		private const int YoungestPoint = -5;

		// Token: 0x040007CC RID: 1996
		private const int DirectDescendentPoint = 10;

		// Token: 0x040007CD RID: 1997
		private const int CollateralHeirPoint = 10;
	}
}
