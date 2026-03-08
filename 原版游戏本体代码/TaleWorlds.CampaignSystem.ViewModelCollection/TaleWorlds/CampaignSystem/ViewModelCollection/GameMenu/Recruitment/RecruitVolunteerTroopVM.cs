using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Recruitment
{
	// Token: 0x020000B2 RID: 178
	public class RecruitVolunteerTroopVM : ViewModel
	{
		// Token: 0x0600115C RID: 4444 RVA: 0x000454FC File Offset: 0x000436FC
		public RecruitVolunteerTroopVM(RecruitVolunteerVM owner, CharacterObject character, int index, Action<RecruitVolunteerTroopVM> onClick, Action<RecruitVolunteerTroopVM> onRemoveFromCart)
		{
			if (character != null)
			{
				this.NameText = character.Name.ToString();
				this._character = character;
				GameTexts.SetVariable("LEVEL", character.Level);
				this.Level = GameTexts.FindText("str_level_with_value", null).ToString();
				this.Character = character;
				this.Wage = this.Character.TroopWage;
				this.Cost = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(this.Character, Hero.MainHero, false).RoundedResultNumber;
				this.IsTroopEmpty = false;
				CharacterCode characterCode = CampaignUIHelper.GetCharacterCode(character, false);
				this.ImageIdentifier = new CharacterImageIdentifierVM(characterCode);
				this.TierIconData = CampaignUIHelper.GetCharacterTierData(character, false);
				this.TypeIconData = CampaignUIHelper.GetCharacterTypeData(character, false);
			}
			else
			{
				this.IsTroopEmpty = true;
			}
			this.Owner = owner;
			if (this.Owner != null)
			{
				this._currentRelation = Hero.MainHero.GetRelation(this.Owner.OwnerHero);
			}
			this._maximumIndexCanBeRecruit = Campaign.Current.Models.VolunteerModel.MaximumIndexHeroCanRecruitFromHero(Hero.MainHero, this.Owner.OwnerHero, -101);
			for (int i = -100; i < 100; i++)
			{
				if (index < Campaign.Current.Models.VolunteerModel.MaximumIndexHeroCanRecruitFromHero(Hero.MainHero, this.Owner.OwnerHero, i))
				{
					this._requiredRelation = i;
					break;
				}
			}
			this._onClick = onClick;
			this.Index = index;
			this._onRemoveFromCart = onRemoveFromCart;
			this.RefreshValues();
		}

		// Token: 0x0600115D RID: 4445 RVA: 0x0004568C File Offset: 0x0004388C
		public override void RefreshValues()
		{
			base.RefreshValues();
			if (this._character != null)
			{
				this.NameText = this._character.Name.ToString();
				GameTexts.SetVariable("LEVEL", this._character.Level);
				this.Level = GameTexts.FindText("str_level_with_value", null).ToString();
			}
		}

		// Token: 0x0600115E RID: 4446 RVA: 0x000456E8 File Offset: 0x000438E8
		public void ExecuteRecruit()
		{
			if (this.CanBeRecruited)
			{
				this._onClick(this);
				return;
			}
			if (this.IsInCart)
			{
				this._onRemoveFromCart(this);
			}
		}

		// Token: 0x0600115F RID: 4447 RVA: 0x00045713 File Offset: 0x00043913
		public void ExecuteOpenEncyclopedia()
		{
			if (this.Character != null)
			{
				Campaign.Current.EncyclopediaManager.GoToLink(this.Character.EncyclopediaLink);
			}
		}

		// Token: 0x06001160 RID: 4448 RVA: 0x00045737 File Offset: 0x00043937
		public void ExecuteRemoveFromCart()
		{
			if (this.IsInCart)
			{
				this._onRemoveFromCart(this);
			}
		}

		// Token: 0x06001161 RID: 4449 RVA: 0x00045750 File Offset: 0x00043950
		public virtual void ExecuteBeginHint()
		{
			if (this._character != null)
			{
				if (this.PlayerHasEnoughRelation)
				{
					InformationManager.ShowTooltip(typeof(CharacterObject), new object[] { this._character });
					return;
				}
				List<TooltipProperty> list = new List<TooltipProperty>();
				string text = "";
				list.Add(new TooltipProperty(text, this._character.Name.ToString(), 1, false, TooltipProperty.TooltipPropertyFlags.None));
				list.Add(new TooltipProperty(text, text, -1, false, TooltipProperty.TooltipPropertyFlags.None));
				GameTexts.SetVariable("LEVEL", this._character.Level);
				GameTexts.SetVariable("newline", "\n");
				list.Add(new TooltipProperty(text, GameTexts.FindText("str_level_with_value", null).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				GameTexts.SetVariable("REL1", this._currentRelation);
				GameTexts.SetVariable("REL2", this._requiredRelation);
				list.Add(new TooltipProperty(text, GameTexts.FindText("str_recruit_volunteers_not_enough_relation", null).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				InformationManager.ShowTooltip(typeof(List<TooltipProperty>), new object[] { list });
				return;
			}
			else
			{
				if (this.PlayerHasEnoughRelation)
				{
					MBInformationManager.ShowHint(GameTexts.FindText("str_recruit_volunteers_new_troop", null).ToString());
					return;
				}
				GameTexts.SetVariable("newline", "\n");
				GameTexts.SetVariable("REL1", this._currentRelation);
				GameTexts.SetVariable("REL2", this._requiredRelation);
				GameTexts.SetVariable("STR1", GameTexts.FindText("str_recruit_volunteers_new_troop", null));
				GameTexts.SetVariable("STR2", GameTexts.FindText("str_recruit_volunteers_not_enough_relation", null));
				MBInformationManager.ShowHint(GameTexts.FindText("str_string_newline_string", null).ToString());
				return;
			}
		}

		// Token: 0x06001162 RID: 4450 RVA: 0x000458F2 File Offset: 0x00043AF2
		public virtual void ExecuteEndHint()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x06001163 RID: 4451 RVA: 0x000458F9 File Offset: 0x00043AF9
		public void ExecuteFocus()
		{
			if (!this.IsTroopEmpty)
			{
				Action<RecruitVolunteerTroopVM> onFocused = RecruitVolunteerTroopVM.OnFocused;
				if (onFocused == null)
				{
					return;
				}
				onFocused(this);
			}
		}

		// Token: 0x06001164 RID: 4452 RVA: 0x00045913 File Offset: 0x00043B13
		public void ExecuteUnfocus()
		{
			Action<RecruitVolunteerTroopVM> onFocused = RecruitVolunteerTroopVM.OnFocused;
			if (onFocused == null)
			{
				return;
			}
			onFocused(null);
		}

		// Token: 0x170005AA RID: 1450
		// (get) Token: 0x06001165 RID: 4453 RVA: 0x00045925 File Offset: 0x00043B25
		// (set) Token: 0x06001166 RID: 4454 RVA: 0x0004592D File Offset: 0x00043B2D
		[DataSourceProperty]
		public string Level
		{
			get
			{
				return this._level;
			}
			set
			{
				if (value != this._level)
				{
					this._level = value;
					base.OnPropertyChangedWithValue<string>(value, "Level");
				}
			}
		}

		// Token: 0x170005AB RID: 1451
		// (get) Token: 0x06001167 RID: 4455 RVA: 0x00045950 File Offset: 0x00043B50
		// (set) Token: 0x06001168 RID: 4456 RVA: 0x00045958 File Offset: 0x00043B58
		[DataSourceProperty]
		public bool CanBeRecruited
		{
			get
			{
				return this._canBeRecruited;
			}
			set
			{
				if (value != this._canBeRecruited)
				{
					this._canBeRecruited = value;
					base.OnPropertyChangedWithValue(value, "CanBeRecruited");
				}
			}
		}

		// Token: 0x170005AC RID: 1452
		// (get) Token: 0x06001169 RID: 4457 RVA: 0x00045976 File Offset: 0x00043B76
		// (set) Token: 0x0600116A RID: 4458 RVA: 0x0004597E File Offset: 0x00043B7E
		[DataSourceProperty]
		public bool IsHiglightEnabled
		{
			get
			{
				return this._isHiglightEnabled;
			}
			set
			{
				if (value != this._isHiglightEnabled)
				{
					this._isHiglightEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsHiglightEnabled");
				}
			}
		}

		// Token: 0x170005AD RID: 1453
		// (get) Token: 0x0600116B RID: 4459 RVA: 0x0004599C File Offset: 0x00043B9C
		// (set) Token: 0x0600116C RID: 4460 RVA: 0x000459A4 File Offset: 0x00043BA4
		[DataSourceProperty]
		public int Wage
		{
			get
			{
				return this._wage;
			}
			set
			{
				if (value != this._wage)
				{
					this._wage = value;
					base.OnPropertyChangedWithValue(value, "Wage");
				}
			}
		}

		// Token: 0x170005AE RID: 1454
		// (get) Token: 0x0600116D RID: 4461 RVA: 0x000459C2 File Offset: 0x00043BC2
		// (set) Token: 0x0600116E RID: 4462 RVA: 0x000459CA File Offset: 0x00043BCA
		[DataSourceProperty]
		public int Cost
		{
			get
			{
				return this._cost;
			}
			set
			{
				if (value != this._cost)
				{
					this._cost = value;
					base.OnPropertyChangedWithValue(value, "Cost");
				}
			}
		}

		// Token: 0x170005AF RID: 1455
		// (get) Token: 0x0600116F RID: 4463 RVA: 0x000459E8 File Offset: 0x00043BE8
		// (set) Token: 0x06001170 RID: 4464 RVA: 0x000459F0 File Offset: 0x00043BF0
		[DataSourceProperty]
		public bool IsInCart
		{
			get
			{
				return this._isInCart;
			}
			set
			{
				if (value != this._isInCart)
				{
					this._isInCart = value;
					base.OnPropertyChangedWithValue(value, "IsInCart");
				}
			}
		}

		// Token: 0x170005B0 RID: 1456
		// (get) Token: 0x06001171 RID: 4465 RVA: 0x00045A0E File Offset: 0x00043C0E
		// (set) Token: 0x06001172 RID: 4466 RVA: 0x00045A16 File Offset: 0x00043C16
		[DataSourceProperty]
		public bool IsTroopEmpty
		{
			get
			{
				return this._isTroopEmpty;
			}
			set
			{
				if (value != this._isTroopEmpty)
				{
					this._isTroopEmpty = value;
					base.OnPropertyChangedWithValue(value, "IsTroopEmpty");
				}
			}
		}

		// Token: 0x170005B1 RID: 1457
		// (get) Token: 0x06001173 RID: 4467 RVA: 0x00045A34 File Offset: 0x00043C34
		// (set) Token: 0x06001174 RID: 4468 RVA: 0x00045A3C File Offset: 0x00043C3C
		[DataSourceProperty]
		public bool PlayerHasEnoughRelation
		{
			get
			{
				return this._playerHasEnoughRelation;
			}
			set
			{
				if (value != this._playerHasEnoughRelation)
				{
					this._playerHasEnoughRelation = value;
					base.OnPropertyChangedWithValue(value, "PlayerHasEnoughRelation");
				}
			}
		}

		// Token: 0x170005B2 RID: 1458
		// (get) Token: 0x06001175 RID: 4469 RVA: 0x00045A5A File Offset: 0x00043C5A
		// (set) Token: 0x06001176 RID: 4470 RVA: 0x00045A62 File Offset: 0x00043C62
		[DataSourceProperty]
		public CharacterImageIdentifierVM ImageIdentifier
		{
			get
			{
				return this._imageIdentifier;
			}
			set
			{
				if (value != this._imageIdentifier)
				{
					this._imageIdentifier = value;
					base.OnPropertyChangedWithValue<CharacterImageIdentifierVM>(value, "ImageIdentifier");
				}
			}
		}

		// Token: 0x170005B3 RID: 1459
		// (get) Token: 0x06001177 RID: 4471 RVA: 0x00045A80 File Offset: 0x00043C80
		// (set) Token: 0x06001178 RID: 4472 RVA: 0x00045A88 File Offset: 0x00043C88
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

		// Token: 0x170005B4 RID: 1460
		// (get) Token: 0x06001179 RID: 4473 RVA: 0x00045AAB File Offset: 0x00043CAB
		// (set) Token: 0x0600117A RID: 4474 RVA: 0x00045AB3 File Offset: 0x00043CB3
		[DataSourceProperty]
		public StringItemWithHintVM TierIconData
		{
			get
			{
				return this._tierIconData;
			}
			set
			{
				if (value != this._tierIconData)
				{
					this._tierIconData = value;
					base.OnPropertyChangedWithValue<StringItemWithHintVM>(value, "TierIconData");
				}
			}
		}

		// Token: 0x170005B5 RID: 1461
		// (get) Token: 0x0600117B RID: 4475 RVA: 0x00045AD1 File Offset: 0x00043CD1
		// (set) Token: 0x0600117C RID: 4476 RVA: 0x00045AD9 File Offset: 0x00043CD9
		[DataSourceProperty]
		public StringItemWithHintVM TypeIconData
		{
			get
			{
				return this._typeIconData;
			}
			set
			{
				if (value != this._typeIconData)
				{
					this._typeIconData = value;
					base.OnPropertyChangedWithValue<StringItemWithHintVM>(value, "TypeIconData");
				}
			}
		}

		// Token: 0x040007E7 RID: 2023
		public static Action<RecruitVolunteerTroopVM> OnFocused;

		// Token: 0x040007E8 RID: 2024
		private readonly Action<RecruitVolunteerTroopVM> _onClick;

		// Token: 0x040007E9 RID: 2025
		private readonly Action<RecruitVolunteerTroopVM> _onRemoveFromCart;

		// Token: 0x040007EA RID: 2026
		private CharacterObject _character;

		// Token: 0x040007EB RID: 2027
		public CharacterObject Character;

		// Token: 0x040007EC RID: 2028
		public int Index;

		// Token: 0x040007ED RID: 2029
		private int _maximumIndexCanBeRecruit;

		// Token: 0x040007EE RID: 2030
		private int _requiredRelation;

		// Token: 0x040007EF RID: 2031
		public RecruitVolunteerVM Owner;

		// Token: 0x040007F0 RID: 2032
		private CharacterImageIdentifierVM _imageIdentifier;

		// Token: 0x040007F1 RID: 2033
		private string _nameText;

		// Token: 0x040007F2 RID: 2034
		private string _level;

		// Token: 0x040007F3 RID: 2035
		private bool _canBeRecruited;

		// Token: 0x040007F4 RID: 2036
		private bool _isInCart;

		// Token: 0x040007F5 RID: 2037
		private int _wage;

		// Token: 0x040007F6 RID: 2038
		private int _cost;

		// Token: 0x040007F7 RID: 2039
		private bool _isTroopEmpty;

		// Token: 0x040007F8 RID: 2040
		private bool _playerHasEnoughRelation;

		// Token: 0x040007F9 RID: 2041
		private int _currentRelation;

		// Token: 0x040007FA RID: 2042
		private bool _isHiglightEnabled;

		// Token: 0x040007FB RID: 2043
		private StringItemWithHintVM _tierIconData;

		// Token: 0x040007FC RID: 2044
		private StringItemWithHintVM _typeIconData;
	}
}
