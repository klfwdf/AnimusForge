using System;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions
{
	// Token: 0x02000077 RID: 119
	public class DecisionSupporterVM : ViewModel
	{
		// Token: 0x060009B3 RID: 2483 RVA: 0x0002A5E8 File Offset: 0x000287E8
		public DecisionSupporterVM(TextObject name, string imagePath, Clan clan, Supporter.SupportWeights weight)
		{
			this._nameObj = name;
			this._clan = clan;
			this._weight = weight;
			this.SupportWeightImagePath = DecisionSupporterVM.GetSupporterWeightImagePath(weight);
			this.RefreshValues();
			this._hero = Hero.FindFirst((Hero H) => H.Name == name);
			if (this._hero != null)
			{
				this.Visual = new CharacterImageIdentifierVM(CampaignUIHelper.GetCharacterCode(this._hero.CharacterObject, false));
				return;
			}
			this.Visual = new CharacterImageIdentifierVM(null);
		}

		// Token: 0x060009B4 RID: 2484 RVA: 0x0002A67E File Offset: 0x0002887E
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Name = this._nameObj.ToString();
		}

		// Token: 0x060009B5 RID: 2485 RVA: 0x0002A697 File Offset: 0x00028897
		private void ExecuteBeginHint()
		{
			if (this._hero != null)
			{
				InformationManager.ShowTooltip(typeof(Hero), new object[] { this._hero, false });
			}
		}

		// Token: 0x060009B6 RID: 2486 RVA: 0x0002A6C8 File Offset: 0x000288C8
		private void ExecuteEndHint()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x060009B7 RID: 2487 RVA: 0x0002A6CF File Offset: 0x000288CF
		internal static string GetSupporterWeightImagePath(Supporter.SupportWeights weight)
		{
			switch (weight)
			{
			case Supporter.SupportWeights.SlightlyFavor:
				return "SPKingdom\\voter_strength1";
			case Supporter.SupportWeights.StronglyFavor:
				return "SPKingdom\\voter_strength2";
			case Supporter.SupportWeights.FullyPush:
				return "SPKingdom\\voter_strength3";
			}
			return string.Empty;
		}

		// Token: 0x170002E6 RID: 742
		// (get) Token: 0x060009B8 RID: 2488 RVA: 0x0002A704 File Offset: 0x00028904
		// (set) Token: 0x060009B9 RID: 2489 RVA: 0x0002A70C File Offset: 0x0002890C
		[DataSourceProperty]
		public CharacterImageIdentifierVM Visual
		{
			get
			{
				return this._visual;
			}
			set
			{
				if (value != this._visual)
				{
					this._visual = value;
					base.OnPropertyChangedWithValue<CharacterImageIdentifierVM>(value, "Visual");
				}
			}
		}

		// Token: 0x170002E7 RID: 743
		// (get) Token: 0x060009BA RID: 2490 RVA: 0x0002A72A File Offset: 0x0002892A
		// (set) Token: 0x060009BB RID: 2491 RVA: 0x0002A732 File Offset: 0x00028932
		[DataSourceProperty]
		public int SupportStrength
		{
			get
			{
				return this._supportStrength;
			}
			set
			{
				if (value != this._supportStrength)
				{
					this._supportStrength = value;
					base.OnPropertyChangedWithValue(value, "SupportStrength");
				}
			}
		}

		// Token: 0x170002E8 RID: 744
		// (get) Token: 0x060009BC RID: 2492 RVA: 0x0002A750 File Offset: 0x00028950
		// (set) Token: 0x060009BD RID: 2493 RVA: 0x0002A758 File Offset: 0x00028958
		[DataSourceProperty]
		public string SupportWeightImagePath
		{
			get
			{
				return this._supportWeightImagePath;
			}
			set
			{
				if (value != this._supportWeightImagePath)
				{
					this._supportWeightImagePath = value;
					base.OnPropertyChangedWithValue<string>(value, "SupportWeightImagePath");
				}
			}
		}

		// Token: 0x170002E9 RID: 745
		// (get) Token: 0x060009BE RID: 2494 RVA: 0x0002A77B File Offset: 0x0002897B
		// (set) Token: 0x060009BF RID: 2495 RVA: 0x0002A783 File Offset: 0x00028983
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
					base.OnPropertyChanged("string");
				}
			}
		}

		// Token: 0x04000449 RID: 1097
		private Supporter.SupportWeights _weight;

		// Token: 0x0400044A RID: 1098
		private Clan _clan;

		// Token: 0x0400044B RID: 1099
		private TextObject _nameObj;

		// Token: 0x0400044C RID: 1100
		private Hero _hero;

		// Token: 0x0400044D RID: 1101
		private CharacterImageIdentifierVM _visual;

		// Token: 0x0400044E RID: 1102
		private string _name;

		// Token: 0x0400044F RID: 1103
		private int _supportStrength;

		// Token: 0x04000450 RID: 1104
		private string _supportWeightImagePath;
	}
}
