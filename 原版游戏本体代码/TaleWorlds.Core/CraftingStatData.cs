using System;
using TaleWorlds.Localization;

namespace TaleWorlds.Core
{
	// Token: 0x02000046 RID: 70
	public struct CraftingStatData
	{
		// Token: 0x17000203 RID: 515
		// (get) Token: 0x06000615 RID: 1557 RVA: 0x000154B2 File Offset: 0x000136B2
		public bool IsValid
		{
			get
			{
				return this.MaxValue >= 0f;
			}
		}

		// Token: 0x06000616 RID: 1558 RVA: 0x000154C4 File Offset: 0x000136C4
		public CraftingStatData(TextObject descriptionText, float curValue, float maxValue, CraftingTemplate.CraftingStatTypes type, DamageTypes damageType = DamageTypes.Invalid)
		{
			this.DescriptionText = descriptionText;
			this.CurValue = curValue;
			this.MaxValue = maxValue;
			this.Type = type;
			this.DamageType = damageType;
		}

		// Token: 0x040002C5 RID: 709
		public readonly TextObject DescriptionText;

		// Token: 0x040002C6 RID: 710
		public readonly float CurValue;

		// Token: 0x040002C7 RID: 711
		public readonly float MaxValue;

		// Token: 0x040002C8 RID: 712
		public readonly CraftingTemplate.CraftingStatTypes Type;

		// Token: 0x040002C9 RID: 713
		public readonly DamageTypes DamageType;
	}
}
