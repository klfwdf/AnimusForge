using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper.PerkSelection
{
	// Token: 0x02000147 RID: 327
	public class PerkSelectionItemVM : ViewModel
	{
		// Token: 0x06001F45 RID: 8005 RVA: 0x000729FA File Offset: 0x00070BFA
		public PerkSelectionItemVM(PerkObject perk, Action<PerkSelectionItemVM> onSelection)
		{
			this.Perk = perk;
			this._onSelection = onSelection;
			this.RefreshValues();
		}

		// Token: 0x06001F46 RID: 8006 RVA: 0x00072A18 File Offset: 0x00070C18
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.PickText = new TextObject("{=1CXlqb2U}Pick:", null).ToString();
			this.PerkName = this.Perk.Name.ToString();
			this.PerkDescription = this.Perk.Description.ToString();
			TextObject combinedPerkRoleText = CampaignUIHelper.GetCombinedPerkRoleText(this.Perk);
			this.PerkRole = ((combinedPerkRoleText != null) ? combinedPerkRoleText.ToString() : null) ?? "";
		}

		// Token: 0x06001F47 RID: 8007 RVA: 0x00072A93 File Offset: 0x00070C93
		public void ExecuteSelection()
		{
			this._onSelection(this);
		}

		// Token: 0x17000AA9 RID: 2729
		// (get) Token: 0x06001F48 RID: 8008 RVA: 0x00072AA1 File Offset: 0x00070CA1
		// (set) Token: 0x06001F49 RID: 8009 RVA: 0x00072AA9 File Offset: 0x00070CA9
		[DataSourceProperty]
		public string PickText
		{
			get
			{
				return this._pickText;
			}
			set
			{
				if (value != this._pickText)
				{
					this._pickText = value;
					base.OnPropertyChangedWithValue<string>(value, "PickText");
				}
			}
		}

		// Token: 0x17000AAA RID: 2730
		// (get) Token: 0x06001F4A RID: 8010 RVA: 0x00072ACC File Offset: 0x00070CCC
		// (set) Token: 0x06001F4B RID: 8011 RVA: 0x00072AD4 File Offset: 0x00070CD4
		[DataSourceProperty]
		public string PerkName
		{
			get
			{
				return this._perkName;
			}
			set
			{
				if (value != this._perkName)
				{
					this._perkName = value;
					base.OnPropertyChangedWithValue<string>(value, "PerkName");
				}
			}
		}

		// Token: 0x17000AAB RID: 2731
		// (get) Token: 0x06001F4C RID: 8012 RVA: 0x00072AF7 File Offset: 0x00070CF7
		// (set) Token: 0x06001F4D RID: 8013 RVA: 0x00072AFF File Offset: 0x00070CFF
		[DataSourceProperty]
		public string PerkDescription
		{
			get
			{
				return this._perkDescription;
			}
			set
			{
				if (value != this._perkDescription)
				{
					this._perkDescription = value;
					base.OnPropertyChangedWithValue<string>(value, "PerkDescription");
				}
			}
		}

		// Token: 0x17000AAC RID: 2732
		// (get) Token: 0x06001F4E RID: 8014 RVA: 0x00072B22 File Offset: 0x00070D22
		// (set) Token: 0x06001F4F RID: 8015 RVA: 0x00072B2A File Offset: 0x00070D2A
		[DataSourceProperty]
		public string PerkRole
		{
			get
			{
				return this._perkRole;
			}
			set
			{
				if (value != this._perkRole)
				{
					this._perkRole = value;
					base.OnPropertyChangedWithValue<string>(value, "PerkRole");
				}
			}
		}

		// Token: 0x04000E99 RID: 3737
		private readonly Action<PerkSelectionItemVM> _onSelection;

		// Token: 0x04000E9A RID: 3738
		public readonly PerkObject Perk;

		// Token: 0x04000E9B RID: 3739
		private string _pickText;

		// Token: 0x04000E9C RID: 3740
		private string _perkName;

		// Token: 0x04000E9D RID: 3741
		private string _perkDescription;

		// Token: 0x04000E9E RID: 3742
		private string _perkRole;
	}
}
