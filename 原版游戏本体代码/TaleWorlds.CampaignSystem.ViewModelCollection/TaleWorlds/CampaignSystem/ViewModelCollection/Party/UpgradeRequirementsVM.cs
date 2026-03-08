using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Party
{
	// Token: 0x02000031 RID: 49
	public class UpgradeRequirementsVM : ViewModel
	{
		// Token: 0x060004D2 RID: 1234 RVA: 0x0001BC56 File Offset: 0x00019E56
		public UpgradeRequirementsVM()
		{
			this.IsItemRequirementMet = true;
			this.IsPerkRequirementMet = true;
		}

		// Token: 0x060004D3 RID: 1235 RVA: 0x0001BC82 File Offset: 0x00019E82
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.UpdateItemRequirementHint();
			this.UpdatePerkRequirementHint();
		}

		// Token: 0x060004D4 RID: 1236 RVA: 0x0001BC96 File Offset: 0x00019E96
		public void SetItemRequirement(ItemCategory category)
		{
			if (category != null)
			{
				this.HasItemRequirement = true;
				this._category = category;
				this.ItemRequirement = category.StringId.ToLower();
				this.UpdateItemRequirementHint();
			}
		}

		// Token: 0x060004D5 RID: 1237 RVA: 0x0001BCC0 File Offset: 0x00019EC0
		public void SetPerkRequirement(PerkObject perk)
		{
			if (perk != null)
			{
				this.HasPerkRequirement = true;
				this._perk = perk;
				this.PerkRequirement = perk.Skill.StringId.ToLower();
				this.UpdatePerkRequirementHint();
			}
		}

		// Token: 0x060004D6 RID: 1238 RVA: 0x0001BCEF File Offset: 0x00019EEF
		public void SetRequirementsMet(bool isItemRequirementMet, bool isPerkRequirementMet)
		{
			this.IsItemRequirementMet = !this.HasItemRequirement || isItemRequirementMet;
			this.IsPerkRequirementMet = !this.HasPerkRequirement || isPerkRequirementMet;
		}

		// Token: 0x060004D7 RID: 1239 RVA: 0x0001BD14 File Offset: 0x00019F14
		private void UpdateItemRequirementHint()
		{
			if (this._category == null)
			{
				return;
			}
			TextObject textObject = new TextObject("{=Q0j1umAt}Requirement: {REQUIREMENT_NAME}", null);
			textObject.SetTextVariable("REQUIREMENT_NAME", this._category.GetName().ToString());
			this.ItemRequirementHint = new HintViewModel(textObject, null);
		}

		// Token: 0x060004D8 RID: 1240 RVA: 0x0001BD60 File Offset: 0x00019F60
		private void UpdatePerkRequirementHint()
		{
			if (this._perk == null)
			{
				return;
			}
			TextObject textObject = new TextObject("{=Q0j1umAt}Requirement: {REQUIREMENT_NAME}", null);
			textObject.SetTextVariable("REQUIREMENT_NAME", this._perk.Name.ToString());
			this.PerkRequirementHint = new HintViewModel(textObject, null);
		}

		// Token: 0x1700015B RID: 347
		// (get) Token: 0x060004D9 RID: 1241 RVA: 0x0001BDAB File Offset: 0x00019FAB
		// (set) Token: 0x060004DA RID: 1242 RVA: 0x0001BDB3 File Offset: 0x00019FB3
		[DataSourceProperty]
		public bool IsItemRequirementMet
		{
			get
			{
				return this._isItemRequirementMet;
			}
			set
			{
				if (value != this._isItemRequirementMet)
				{
					this._isItemRequirementMet = value;
					base.OnPropertyChangedWithValue(value, "IsItemRequirementMet");
				}
			}
		}

		// Token: 0x1700015C RID: 348
		// (get) Token: 0x060004DB RID: 1243 RVA: 0x0001BDD1 File Offset: 0x00019FD1
		// (set) Token: 0x060004DC RID: 1244 RVA: 0x0001BDD9 File Offset: 0x00019FD9
		[DataSourceProperty]
		public bool IsPerkRequirementMet
		{
			get
			{
				return this._isPerkRequirementMet;
			}
			set
			{
				if (value != this._isPerkRequirementMet)
				{
					this._isPerkRequirementMet = value;
					base.OnPropertyChangedWithValue(value, "IsPerkRequirementMet");
				}
			}
		}

		// Token: 0x1700015D RID: 349
		// (get) Token: 0x060004DD RID: 1245 RVA: 0x0001BDF7 File Offset: 0x00019FF7
		// (set) Token: 0x060004DE RID: 1246 RVA: 0x0001BDFF File Offset: 0x00019FFF
		[DataSourceProperty]
		public bool HasItemRequirement
		{
			get
			{
				return this._hasItemRequirement;
			}
			set
			{
				if (value != this._hasItemRequirement)
				{
					this._hasItemRequirement = value;
					base.OnPropertyChangedWithValue(value, "HasItemRequirement");
				}
			}
		}

		// Token: 0x1700015E RID: 350
		// (get) Token: 0x060004DF RID: 1247 RVA: 0x0001BE1D File Offset: 0x0001A01D
		// (set) Token: 0x060004E0 RID: 1248 RVA: 0x0001BE25 File Offset: 0x0001A025
		[DataSourceProperty]
		public bool HasPerkRequirement
		{
			get
			{
				return this._hasPerkRequirement;
			}
			set
			{
				if (value != this._hasPerkRequirement)
				{
					this._hasPerkRequirement = value;
					base.OnPropertyChangedWithValue(value, "HasPerkRequirement");
				}
			}
		}

		// Token: 0x1700015F RID: 351
		// (get) Token: 0x060004E1 RID: 1249 RVA: 0x0001BE43 File Offset: 0x0001A043
		// (set) Token: 0x060004E2 RID: 1250 RVA: 0x0001BE4B File Offset: 0x0001A04B
		[DataSourceProperty]
		public string PerkRequirement
		{
			get
			{
				return this._perkRequirement;
			}
			set
			{
				if (value != this._perkRequirement)
				{
					this._perkRequirement = value;
					base.OnPropertyChangedWithValue<string>(value, "PerkRequirement");
				}
			}
		}

		// Token: 0x17000160 RID: 352
		// (get) Token: 0x060004E3 RID: 1251 RVA: 0x0001BE6E File Offset: 0x0001A06E
		// (set) Token: 0x060004E4 RID: 1252 RVA: 0x0001BE76 File Offset: 0x0001A076
		[DataSourceProperty]
		public string ItemRequirement
		{
			get
			{
				return this._itemRequirement;
			}
			set
			{
				if (value != this._itemRequirement)
				{
					this._itemRequirement = value;
					base.OnPropertyChangedWithValue<string>(value, "ItemRequirement");
				}
			}
		}

		// Token: 0x17000161 RID: 353
		// (get) Token: 0x060004E5 RID: 1253 RVA: 0x0001BE99 File Offset: 0x0001A099
		// (set) Token: 0x060004E6 RID: 1254 RVA: 0x0001BEA1 File Offset: 0x0001A0A1
		[DataSourceProperty]
		public HintViewModel ItemRequirementHint
		{
			get
			{
				return this._itemRequirementHint;
			}
			set
			{
				if (value != this._itemRequirementHint)
				{
					this._itemRequirementHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ItemRequirementHint");
				}
			}
		}

		// Token: 0x17000162 RID: 354
		// (get) Token: 0x060004E7 RID: 1255 RVA: 0x0001BEBF File Offset: 0x0001A0BF
		// (set) Token: 0x060004E8 RID: 1256 RVA: 0x0001BEC7 File Offset: 0x0001A0C7
		[DataSourceProperty]
		public HintViewModel PerkRequirementHint
		{
			get
			{
				return this._perkRequirementHint;
			}
			set
			{
				if (value != this._perkRequirementHint)
				{
					this._perkRequirementHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "PerkRequirementHint");
				}
			}
		}

		// Token: 0x04000215 RID: 533
		private ItemCategory _category;

		// Token: 0x04000216 RID: 534
		private PerkObject _perk;

		// Token: 0x04000217 RID: 535
		private bool _isItemRequirementMet;

		// Token: 0x04000218 RID: 536
		private bool _isPerkRequirementMet;

		// Token: 0x04000219 RID: 537
		private bool _hasItemRequirement;

		// Token: 0x0400021A RID: 538
		private bool _hasPerkRequirement;

		// Token: 0x0400021B RID: 539
		private string _perkRequirement = "";

		// Token: 0x0400021C RID: 540
		private string _itemRequirement = "";

		// Token: 0x0400021D RID: 541
		private HintViewModel _itemRequirementHint;

		// Token: 0x0400021E RID: 542
		private HintViewModel _perkRequirementHint;
	}
}
