using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x020000AA RID: 170
	public sealed class SkillEffect : PropertyObject
	{
		// Token: 0x1700052B RID: 1323
		// (get) Token: 0x0600135A RID: 4954 RVA: 0x00059EA6 File Offset: 0x000580A6
		public static MBReadOnlyList<SkillEffect> All
		{
			get
			{
				return Campaign.Current.AllSkillEffects;
			}
		}

		// Token: 0x1700052C RID: 1324
		// (get) Token: 0x0600135B RID: 4955 RVA: 0x00059EB2 File Offset: 0x000580B2
		// (set) Token: 0x0600135C RID: 4956 RVA: 0x00059EBA File Offset: 0x000580BA
		public PartyRole Role { get; private set; }

		// Token: 0x1700052D RID: 1325
		// (get) Token: 0x0600135D RID: 4957 RVA: 0x00059EC3 File Offset: 0x000580C3
		// (set) Token: 0x0600135E RID: 4958 RVA: 0x00059ECB File Offset: 0x000580CB
		public EffectIncrementType IncrementType { get; private set; }

		// Token: 0x1700052E RID: 1326
		// (get) Token: 0x0600135F RID: 4959 RVA: 0x00059ED4 File Offset: 0x000580D4
		// (set) Token: 0x06001360 RID: 4960 RVA: 0x00059EDC File Offset: 0x000580DC
		public SkillObject EffectedSkill { get; private set; }

		// Token: 0x06001361 RID: 4961 RVA: 0x00059EE5 File Offset: 0x000580E5
		public SkillEffect(string stringId)
			: base(stringId)
		{
		}

		// Token: 0x06001362 RID: 4962 RVA: 0x00059EF0 File Offset: 0x000580F0
		public void Initialize(TextObject description, SkillObject effectedSkill, PartyRole role, float bonus, EffectIncrementType incrementType, float baseValue = 0f, float limitMin = -3.4028235E+38f, float limitMax = 3.4028235E+38f)
		{
			base.Initialize(TextObject.GetEmpty(), description);
			this.Role = role;
			this.Bonus = bonus;
			this.IncrementType = incrementType;
			this.EffectedSkill = effectedSkill;
			this.BaseValue = baseValue;
			this.LimitMin = limitMin;
			this.LimitMax = limitMax;
			base.AfterInitialized();
		}

		// Token: 0x06001363 RID: 4963 RVA: 0x00059F45 File Offset: 0x00058145
		public float GetSkillEffectValue(int skillLevel)
		{
			return MathF.Clamp(this.BaseValue + this.Bonus * (float)skillLevel, this.LimitMin, this.LimitMax);
		}

		// Token: 0x0400064D RID: 1613
		private float Bonus;

		// Token: 0x0400064E RID: 1614
		private float BaseValue;

		// Token: 0x0400064F RID: 1615
		private float LimitMin;

		// Token: 0x04000650 RID: 1616
		private float LimitMax;
	}
}
