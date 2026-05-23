using System;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation
{
	// Token: 0x0200014E RID: 334
	public class CharacterCreationCultureVM : ViewModel
	{
		// Token: 0x17000AC3 RID: 2755
		// (get) Token: 0x06001F9E RID: 8094 RVA: 0x000738A0 File Offset: 0x00071AA0
		public CultureObject Culture { get; }

		// Token: 0x06001F9F RID: 8095 RVA: 0x000738A8 File Offset: 0x00071AA8
		public CharacterCreationCultureVM(CultureObject culture, Action<CharacterCreationCultureVM> onSelection)
		{
			this._onSelection = onSelection;
			this.Culture = culture;
			CharacterCreationState characterCreationState = GameStateManager.Current.ActiveState as CharacterCreationState;
			CharacterCreationContent characterCreationContent = ((characterCreationState != null) ? characterCreationState.CharacterCreationManager.CharacterCreationContent : null);
			MBTextManager.SetTextVariable("FOCUS_VALUE", characterCreationContent.GetFocusToAddByCulture(culture));
			MBTextManager.SetTextVariable("EXP_VALUE", characterCreationContent.GetSkillLevelToAddByCulture(culture));
			this.DescriptionText = GameTexts.FindText("str_culture_description", this.Culture.StringId).ToString();
			this.ShortenedNameText = GameTexts.FindText("str_culture_rich_name", this.Culture.StringId).ToString();
			this.NameText = GameTexts.FindText("str_culture_rich_name", this.Culture.StringId).ToString();
			this.CultureID = ((culture != null) ? culture.StringId : null) ?? "";
			this.CultureColor1 = Color.FromUint((culture != null) ? culture.Color : Color.White.ToUnsignedInteger());
			this.Feats = new MBBindingList<CharacterCreationCultureFeatVM>();
			foreach (FeatObject featObject in this.Culture.GetCulturalFeats((FeatObject x) => x.IsPositive))
			{
				this.Feats.Add(new CharacterCreationCultureFeatVM(true, featObject.Description.ToString()));
			}
			foreach (FeatObject featObject2 in this.Culture.GetCulturalFeats((FeatObject x) => !x.IsPositive))
			{
				this.Feats.Add(new CharacterCreationCultureFeatVM(false, featObject2.Description.ToString()));
			}
		}

		// Token: 0x06001FA0 RID: 8096 RVA: 0x00073AB0 File Offset: 0x00071CB0
		public void ExecuteSelectCulture()
		{
			this._onSelection(this);
		}

		// Token: 0x17000AC4 RID: 2756
		// (get) Token: 0x06001FA1 RID: 8097 RVA: 0x00073ABE File Offset: 0x00071CBE
		// (set) Token: 0x06001FA2 RID: 8098 RVA: 0x00073AC6 File Offset: 0x00071CC6
		[DataSourceProperty]
		public string CultureID
		{
			get
			{
				return this._cultureID;
			}
			set
			{
				if (value != this._cultureID)
				{
					this._cultureID = value;
					base.OnPropertyChangedWithValue<string>(value, "CultureID");
				}
			}
		}

		// Token: 0x17000AC5 RID: 2757
		// (get) Token: 0x06001FA3 RID: 8099 RVA: 0x00073AE9 File Offset: 0x00071CE9
		// (set) Token: 0x06001FA4 RID: 8100 RVA: 0x00073AF1 File Offset: 0x00071CF1
		[DataSourceProperty]
		public Color CultureColor1
		{
			get
			{
				return this._cultureColor1;
			}
			set
			{
				if (value != this._cultureColor1)
				{
					this._cultureColor1 = value;
					base.OnPropertyChangedWithValue(value, "CultureColor1");
				}
			}
		}

		// Token: 0x17000AC6 RID: 2758
		// (get) Token: 0x06001FA5 RID: 8101 RVA: 0x00073B14 File Offset: 0x00071D14
		// (set) Token: 0x06001FA6 RID: 8102 RVA: 0x00073B1C File Offset: 0x00071D1C
		[DataSourceProperty]
		public string DescriptionText
		{
			get
			{
				return this._descriptionText;
			}
			set
			{
				if (value != this._descriptionText)
				{
					this._descriptionText = value;
					base.OnPropertyChangedWithValue<string>(value, "DescriptionText");
				}
			}
		}

		// Token: 0x17000AC7 RID: 2759
		// (get) Token: 0x06001FA7 RID: 8103 RVA: 0x00073B3F File Offset: 0x00071D3F
		// (set) Token: 0x06001FA8 RID: 8104 RVA: 0x00073B47 File Offset: 0x00071D47
		[DataSourceProperty]
		public string NameText
		{
			get
			{
				return this._nameText;
			}
			set
			{
				if (value != this._nameText)
				{
					this._nameText = value;
					base.OnPropertyChangedWithValue<string>(value, "NameText");
				}
			}
		}

		// Token: 0x17000AC8 RID: 2760
		// (get) Token: 0x06001FA9 RID: 8105 RVA: 0x00073B6A File Offset: 0x00071D6A
		// (set) Token: 0x06001FAA RID: 8106 RVA: 0x00073B72 File Offset: 0x00071D72
		[DataSourceProperty]
		public string ShortenedNameText
		{
			get
			{
				return this._shortenedNameText;
			}
			set
			{
				if (value != this._shortenedNameText)
				{
					this._shortenedNameText = value;
					base.OnPropertyChangedWithValue<string>(value, "ShortenedNameText");
				}
			}
		}

		// Token: 0x17000AC9 RID: 2761
		// (get) Token: 0x06001FAB RID: 8107 RVA: 0x00073B95 File Offset: 0x00071D95
		// (set) Token: 0x06001FAC RID: 8108 RVA: 0x00073B9D File Offset: 0x00071D9D
		[DataSourceProperty]
		public bool IsSelected
		{
			get
			{
				return this._isSelected;
			}
			set
			{
				if (value != this._isSelected)
				{
					this._isSelected = value;
					base.OnPropertyChangedWithValue(value, "IsSelected");
				}
			}
		}

		// Token: 0x17000ACA RID: 2762
		// (get) Token: 0x06001FAD RID: 8109 RVA: 0x00073BBB File Offset: 0x00071DBB
		// (set) Token: 0x06001FAE RID: 8110 RVA: 0x00073BC3 File Offset: 0x00071DC3
		[DataSourceProperty]
		public MBBindingList<CharacterCreationCultureFeatVM> Feats
		{
			get
			{
				return this._feats;
			}
			set
			{
				if (value != this._feats)
				{
					this._feats = value;
					base.OnPropertyChangedWithValue<MBBindingList<CharacterCreationCultureFeatVM>>(value, "Feats");
				}
			}
		}

		// Token: 0x04000EBC RID: 3772
		private readonly Action<CharacterCreationCultureVM> _onSelection;

		// Token: 0x04000EBD RID: 3773
		private string _descriptionText = "";

		// Token: 0x04000EBE RID: 3774
		private string _nameText;

		// Token: 0x04000EBF RID: 3775
		private string _shortenedNameText;

		// Token: 0x04000EC0 RID: 3776
		private bool _isSelected;

		// Token: 0x04000EC1 RID: 3777
		private string _cultureID;

		// Token: 0x04000EC2 RID: 3778
		private Color _cultureColor1;

		// Token: 0x04000EC3 RID: 3779
		private MBBindingList<CharacterCreationCultureFeatVM> _feats;
	}
}
