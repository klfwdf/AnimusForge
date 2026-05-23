using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.Core
{
	// Token: 0x02000015 RID: 21
	public sealed class BannerEffect : PropertyObject
	{
		// Token: 0x1700003A RID: 58
		// (get) Token: 0x060000E2 RID: 226 RVA: 0x000045B5 File Offset: 0x000027B5
		// (set) Token: 0x060000E3 RID: 227 RVA: 0x000045BD File Offset: 0x000027BD
		public EffectIncrementType IncrementType { get; private set; }

		// Token: 0x060000E4 RID: 228 RVA: 0x000045C6 File Offset: 0x000027C6
		public BannerEffect(string stringId)
			: base(stringId)
		{
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x000045DC File Offset: 0x000027DC
		public void Initialize(string name, string description, float level1Bonus, float level2Bonus, float level3Bonus, EffectIncrementType incrementType)
		{
			TextObject description2 = new TextObject(description, null);
			this._levelBonuses[0] = level1Bonus;
			this._levelBonuses[1] = level2Bonus;
			this._levelBonuses[2] = level3Bonus;
			this.IncrementType = incrementType;
			base.Initialize(new TextObject(name, null), description2);
			base.AfterInitialized();
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x0000462C File Offset: 0x0000282C
		public float GetBonusAtLevel(int bannerLevel)
		{
			int num = bannerLevel - 1;
			num = MBMath.ClampIndex(num, 0, this._levelBonuses.Length);
			return this._levelBonuses[num];
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x00004658 File Offset: 0x00002858
		public string GetBonusStringAtLevel(int bannerLevel)
		{
			float bonusAtLevel = this.GetBonusAtLevel(bannerLevel);
			return string.Format("{0:P2}", bonusAtLevel);
		}

		// Token: 0x060000E8 RID: 232 RVA: 0x00004680 File Offset: 0x00002880
		public TextObject GetDescription(int bannerLevel)
		{
			float bonusAtLevel = this.GetBonusAtLevel(bannerLevel);
			if (bonusAtLevel > 0f)
			{
				TextObject textObject = new TextObject("{=Ffwgecvr}{PLUS_OR_MINUS}{BONUSEFFECT}", null);
				textObject.SetTextVariable("BONUSEFFECT", bonusAtLevel, 2);
				textObject.SetTextVariable("PLUS_OR_MINUS", "{=eTw2aNV5}+");
				return base.Description.SetTextVariable("BONUS_AMOUNT", textObject);
			}
			return base.Description.SetTextVariable("BONUS_AMOUNT", bonusAtLevel, 2);
		}

		// Token: 0x060000E9 RID: 233 RVA: 0x000046EC File Offset: 0x000028EC
		public override string ToString()
		{
			return base.Name.ToString();
		}

		// Token: 0x04000113 RID: 275
		private readonly float[] _levelBonuses = new float[3];
	}
}
