using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CharacterDevelopment
{
	// Token: 0x020003A9 RID: 937
	public sealed class FeatObject : PropertyObject
	{
		// Token: 0x17000CD6 RID: 3286
		// (get) Token: 0x060035EC RID: 13804 RVA: 0x000E0F26 File Offset: 0x000DF126
		public static MBReadOnlyList<FeatObject> All
		{
			get
			{
				return Campaign.Current.AllFeats;
			}
		}

		// Token: 0x17000CD7 RID: 3287
		// (get) Token: 0x060035ED RID: 13805 RVA: 0x000E0F32 File Offset: 0x000DF132
		// (set) Token: 0x060035EE RID: 13806 RVA: 0x000E0F3A File Offset: 0x000DF13A
		public float EffectBonus { get; private set; }

		// Token: 0x17000CD8 RID: 3288
		// (get) Token: 0x060035EF RID: 13807 RVA: 0x000E0F43 File Offset: 0x000DF143
		// (set) Token: 0x060035F0 RID: 13808 RVA: 0x000E0F4B File Offset: 0x000DF14B
		public FeatObject.AdditionType IncrementType { get; private set; }

		// Token: 0x17000CD9 RID: 3289
		// (get) Token: 0x060035F1 RID: 13809 RVA: 0x000E0F54 File Offset: 0x000DF154
		// (set) Token: 0x060035F2 RID: 13810 RVA: 0x000E0F5C File Offset: 0x000DF15C
		public bool IsPositive { get; private set; }

		// Token: 0x060035F3 RID: 13811 RVA: 0x000E0F65 File Offset: 0x000DF165
		public FeatObject(string stringId)
			: base(stringId)
		{
		}

		// Token: 0x060035F4 RID: 13812 RVA: 0x000E0F6E File Offset: 0x000DF16E
		public void Initialize(string name, string description, float effectBonus, bool isPositiveEffect, FeatObject.AdditionType incrementType)
		{
			base.Initialize(new TextObject(name, null), new TextObject(description, null));
			this.EffectBonus = effectBonus;
			this.IncrementType = incrementType;
			this.IsPositive = isPositiveEffect;
			base.AfterInitialized();
		}

		// Token: 0x02000777 RID: 1911
		public enum AdditionType
		{
			// Token: 0x04001E29 RID: 7721
			Add,
			// Token: 0x04001E2A RID: 7722
			AddFactor
		}
	}
}
