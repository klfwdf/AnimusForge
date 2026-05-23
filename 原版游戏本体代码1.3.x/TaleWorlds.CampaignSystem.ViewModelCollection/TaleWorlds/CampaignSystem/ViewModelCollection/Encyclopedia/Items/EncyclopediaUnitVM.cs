using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items
{
	// Token: 0x020000EF RID: 239
	public class EncyclopediaUnitVM : ViewModel
	{
		// Token: 0x060015BF RID: 5567 RVA: 0x00054EA0 File Offset: 0x000530A0
		public EncyclopediaUnitVM(CharacterObject character, bool isActive)
		{
			if (character != null)
			{
				CharacterCode characterCode = CharacterCode.CreateFrom(character);
				this.ImageIdentifier = new CharacterImageIdentifierVM(characterCode);
				this._character = character;
				this.IsActiveUnit = isActive;
				this.TierIconData = CampaignUIHelper.GetCharacterTierData(character, true);
				this.TypeIconData = CampaignUIHelper.GetCharacterTypeData(character, true);
			}
			else
			{
				this.IsActiveUnit = false;
			}
			this.RefreshValues();
		}

		// Token: 0x060015C0 RID: 5568 RVA: 0x00054F00 File Offset: 0x00053100
		public override void RefreshValues()
		{
			base.RefreshValues();
			if (this._character != null)
			{
				this.NameText = this._character.Name.ToString();
			}
		}

		// Token: 0x060015C1 RID: 5569 RVA: 0x00054F26 File Offset: 0x00053126
		public void ExecuteLink()
		{
			Campaign.Current.EncyclopediaManager.GoToLink(this._character.EncyclopediaLink);
		}

		// Token: 0x060015C2 RID: 5570 RVA: 0x00054F42 File Offset: 0x00053142
		public virtual void ExecuteBeginHint()
		{
			InformationManager.ShowTooltip(typeof(CharacterObject), new object[] { this._character });
		}

		// Token: 0x060015C3 RID: 5571 RVA: 0x00054F62 File Offset: 0x00053162
		public virtual void ExecuteEndHint()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x17000733 RID: 1843
		// (get) Token: 0x060015C4 RID: 5572 RVA: 0x00054F69 File Offset: 0x00053169
		// (set) Token: 0x060015C5 RID: 5573 RVA: 0x00054F71 File Offset: 0x00053171
		[DataSourceProperty]
		public bool IsActiveUnit
		{
			get
			{
				return this._isActiveUnit;
			}
			set
			{
				if (value != this._isActiveUnit)
				{
					this._isActiveUnit = value;
					base.OnPropertyChangedWithValue(value, "IsActiveUnit");
				}
			}
		}

		// Token: 0x17000734 RID: 1844
		// (get) Token: 0x060015C6 RID: 5574 RVA: 0x00054F8F File Offset: 0x0005318F
		// (set) Token: 0x060015C7 RID: 5575 RVA: 0x00054F97 File Offset: 0x00053197
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

		// Token: 0x17000735 RID: 1845
		// (get) Token: 0x060015C8 RID: 5576 RVA: 0x00054FB5 File Offset: 0x000531B5
		// (set) Token: 0x060015C9 RID: 5577 RVA: 0x00054FBD File Offset: 0x000531BD
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

		// Token: 0x17000736 RID: 1846
		// (get) Token: 0x060015CA RID: 5578 RVA: 0x00054FE0 File Offset: 0x000531E0
		// (set) Token: 0x060015CB RID: 5579 RVA: 0x00054FE8 File Offset: 0x000531E8
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

		// Token: 0x17000737 RID: 1847
		// (get) Token: 0x060015CC RID: 5580 RVA: 0x00055006 File Offset: 0x00053206
		// (set) Token: 0x060015CD RID: 5581 RVA: 0x0005500E File Offset: 0x0005320E
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

		// Token: 0x040009E5 RID: 2533
		private CharacterObject _character;

		// Token: 0x040009E6 RID: 2534
		private CharacterImageIdentifierVM _imageIdentifier;

		// Token: 0x040009E7 RID: 2535
		private string _nameText;

		// Token: 0x040009E8 RID: 2536
		private bool _isActiveUnit;

		// Token: 0x040009E9 RID: 2537
		private StringItemWithHintVM _tierIconData;

		// Token: 0x040009EA RID: 2538
		private StringItemWithHintVM _typeIconData;
	}
}
