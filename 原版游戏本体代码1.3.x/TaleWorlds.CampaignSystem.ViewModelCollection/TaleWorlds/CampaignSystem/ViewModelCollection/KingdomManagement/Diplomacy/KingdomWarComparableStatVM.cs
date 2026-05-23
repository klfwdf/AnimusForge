using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy
{
	// Token: 0x02000071 RID: 113
	public class KingdomWarComparableStatVM : ViewModel
	{
		// Token: 0x0600093B RID: 2363 RVA: 0x0002921C File Offset: 0x0002741C
		public KingdomWarComparableStatVM(int faction1Stat, int faction2Stat, TextObject name, string faction1Color, string faction2Color, int defaultRange, BasicTooltipViewModel faction1Hint = null, BasicTooltipViewModel faction2Hint = null)
		{
			int num = MathF.Max(MathF.Max(faction1Stat, faction2Stat), defaultRange);
			if (num == 0)
			{
				num = 1;
			}
			this.Faction1Color = faction1Color;
			this.Faction2Color = faction2Color;
			this.Faction1Value = faction1Stat;
			this.Faction2Value = faction2Stat;
			this._defaultRange = defaultRange;
			this.Faction1Percentage = MathF.Round((float)faction1Stat / (float)num * 100f);
			this.Faction2Percentage = MathF.Round((float)faction2Stat / (float)num * 100f);
			this._nameObj = name;
			this.Faction1Hint = faction1Hint;
			this.Faction2Hint = faction2Hint;
			this.RefreshValues();
		}

		// Token: 0x0600093C RID: 2364 RVA: 0x000292B2 File Offset: 0x000274B2
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Name = this._nameObj.ToString();
		}

		// Token: 0x170002B6 RID: 694
		// (get) Token: 0x0600093D RID: 2365 RVA: 0x000292CB File Offset: 0x000274CB
		// (set) Token: 0x0600093E RID: 2366 RVA: 0x000292D3 File Offset: 0x000274D3
		[DataSourceProperty]
		public BasicTooltipViewModel Faction1Hint
		{
			get
			{
				return this._faction1Hint;
			}
			set
			{
				if (value != this._faction1Hint)
				{
					this._faction1Hint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "Faction1Hint");
				}
			}
		}

		// Token: 0x170002B7 RID: 695
		// (get) Token: 0x0600093F RID: 2367 RVA: 0x000292F1 File Offset: 0x000274F1
		// (set) Token: 0x06000940 RID: 2368 RVA: 0x000292F9 File Offset: 0x000274F9
		[DataSourceProperty]
		public BasicTooltipViewModel Faction2Hint
		{
			get
			{
				return this._faction2Hint;
			}
			set
			{
				if (value != this._faction2Hint)
				{
					this._faction2Hint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "Faction2Hint");
				}
			}
		}

		// Token: 0x170002B8 RID: 696
		// (get) Token: 0x06000941 RID: 2369 RVA: 0x00029317 File Offset: 0x00027517
		// (set) Token: 0x06000942 RID: 2370 RVA: 0x0002931F File Offset: 0x0002751F
		[DataSourceProperty]
		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				if (value != this._name)
				{
					this._name = value;
					base.OnPropertyChangedWithValue<string>(value, "Name");
				}
			}
		}

		// Token: 0x170002B9 RID: 697
		// (get) Token: 0x06000943 RID: 2371 RVA: 0x00029342 File Offset: 0x00027542
		// (set) Token: 0x06000944 RID: 2372 RVA: 0x0002934A File Offset: 0x0002754A
		[DataSourceProperty]
		public string Faction1Color
		{
			get
			{
				return this._faction1Color;
			}
			set
			{
				if (value != this._faction1Color)
				{
					this._faction1Color = value;
					base.OnPropertyChangedWithValue<string>(value, "Faction1Color");
				}
			}
		}

		// Token: 0x170002BA RID: 698
		// (get) Token: 0x06000945 RID: 2373 RVA: 0x0002936D File Offset: 0x0002756D
		// (set) Token: 0x06000946 RID: 2374 RVA: 0x00029375 File Offset: 0x00027575
		[DataSourceProperty]
		public string Faction2Color
		{
			get
			{
				return this._faction2Color;
			}
			set
			{
				if (value != this._faction2Color)
				{
					this._faction2Color = value;
					base.OnPropertyChangedWithValue<string>(value, "Faction2Color");
				}
			}
		}

		// Token: 0x170002BB RID: 699
		// (get) Token: 0x06000947 RID: 2375 RVA: 0x00029398 File Offset: 0x00027598
		// (set) Token: 0x06000948 RID: 2376 RVA: 0x000293A0 File Offset: 0x000275A0
		[DataSourceProperty]
		public int Faction1Percentage
		{
			get
			{
				return this._faction1Percentage;
			}
			set
			{
				if (value != this._faction1Percentage)
				{
					this._faction1Percentage = value;
					base.OnPropertyChangedWithValue(value, "Faction1Percentage");
				}
			}
		}

		// Token: 0x170002BC RID: 700
		// (get) Token: 0x06000949 RID: 2377 RVA: 0x000293BE File Offset: 0x000275BE
		// (set) Token: 0x0600094A RID: 2378 RVA: 0x000293C6 File Offset: 0x000275C6
		[DataSourceProperty]
		public int Faction1Value
		{
			get
			{
				return this._faction1Value;
			}
			set
			{
				if (value != this._faction1Value)
				{
					this._faction1Value = value;
					base.OnPropertyChangedWithValue(value, "Faction1Value");
				}
			}
		}

		// Token: 0x170002BD RID: 701
		// (get) Token: 0x0600094B RID: 2379 RVA: 0x000293E4 File Offset: 0x000275E4
		// (set) Token: 0x0600094C RID: 2380 RVA: 0x000293EC File Offset: 0x000275EC
		[DataSourceProperty]
		public int Faction2Percentage
		{
			get
			{
				return this._faction2Percentage;
			}
			set
			{
				if (value != this._faction2Percentage)
				{
					this._faction2Percentage = value;
					base.OnPropertyChangedWithValue(value, "Faction2Percentage");
				}
			}
		}

		// Token: 0x170002BE RID: 702
		// (get) Token: 0x0600094D RID: 2381 RVA: 0x0002940A File Offset: 0x0002760A
		// (set) Token: 0x0600094E RID: 2382 RVA: 0x00029412 File Offset: 0x00027612
		[DataSourceProperty]
		public int Faction2Value
		{
			get
			{
				return this._faction2Value;
			}
			set
			{
				if (value != this._faction2Value)
				{
					this._faction2Value = value;
					base.OnPropertyChangedWithValue(value, "Faction2Value");
				}
			}
		}

		// Token: 0x04000406 RID: 1030
		private TextObject _nameObj;

		// Token: 0x04000407 RID: 1031
		private int _defaultRange;

		// Token: 0x04000408 RID: 1032
		private BasicTooltipViewModel _faction1Hint;

		// Token: 0x04000409 RID: 1033
		private BasicTooltipViewModel _faction2Hint;

		// Token: 0x0400040A RID: 1034
		private string _name;

		// Token: 0x0400040B RID: 1035
		private string _faction1Color;

		// Token: 0x0400040C RID: 1036
		private string _faction2Color;

		// Token: 0x0400040D RID: 1037
		private int _faction1Percentage;

		// Token: 0x0400040E RID: 1038
		private int _faction1Value;

		// Token: 0x0400040F RID: 1039
		private int _faction2Percentage;

		// Token: 0x04000410 RID: 1040
		private int _faction2Value;
	}
}
