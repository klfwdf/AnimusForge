using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items
{
	// Token: 0x020000ED RID: 237
	public class EncyclopediaTroopTreeNodeVM : ViewModel
	{
		// Token: 0x060015AB RID: 5547 RVA: 0x00054A40 File Offset: 0x00052C40
		public EncyclopediaTroopTreeNodeVM(CharacterObject rootCharacter, CharacterObject activeCharacter, bool isAlternativeUpgrade, PerkObject alternativeUpgradePerk = null)
		{
			this.Branch = new MBBindingList<EncyclopediaTroopTreeNodeVM>();
			this.IsActiveUnit = rootCharacter == activeCharacter;
			this.IsAlternativeUpgrade = isAlternativeUpgrade;
			if (alternativeUpgradePerk != null && this.IsAlternativeUpgrade)
			{
				this.AlternativeUpgradeTooltip = new BasicTooltipViewModel(delegate()
				{
					TextObject textObject = new TextObject("{=LVJKy6a8}This troop requires {PERK_NAME} ({PERK_SKILL}) perk to upgrade.", null);
					textObject.SetTextVariable("PERK_NAME", alternativeUpgradePerk.Name);
					textObject.SetTextVariable("PERK_SKILL", alternativeUpgradePerk.Skill.Name);
					return textObject.ToString();
				});
			}
			this.Unit = new EncyclopediaUnitVM(rootCharacter, this.IsActiveUnit);
			foreach (CharacterObject characterObject in rootCharacter.UpgradeTargets)
			{
				if (characterObject == rootCharacter)
				{
					Debug.FailedAssert("A character cannot be it's own upgrade target!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Encyclopedia\\Items\\EncyclopediaTroopTreeNodeVM.cs", ".ctor", 36);
				}
				else if (Campaign.Current.EncyclopediaManager.GetPageOf(typeof(CharacterObject)).IsValidEncyclopediaItem(characterObject))
				{
					bool isAlternativeUpgrade2 = rootCharacter.Culture.IsBandit && !characterObject.Culture.IsBandit;
					PerkObject alternativeUpgradePerk2;
					Campaign.Current.Models.PartyTroopUpgradeModel.DoesPartyHaveRequiredPerksForUpgrade(PartyBase.MainParty, rootCharacter, characterObject, out alternativeUpgradePerk2);
					this.Branch.Add(new EncyclopediaTroopTreeNodeVM(characterObject, activeCharacter, isAlternativeUpgrade2, alternativeUpgradePerk2));
				}
			}
		}

		// Token: 0x060015AC RID: 5548 RVA: 0x00054B66 File Offset: 0x00052D66
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Branch.ApplyActionOnAllItems(delegate(EncyclopediaTroopTreeNodeVM x)
			{
				x.RefreshValues();
			});
			this.Unit.RefreshValues();
		}

		// Token: 0x1700072B RID: 1835
		// (get) Token: 0x060015AD RID: 5549 RVA: 0x00054BA3 File Offset: 0x00052DA3
		// (set) Token: 0x060015AE RID: 5550 RVA: 0x00054BAB File Offset: 0x00052DAB
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

		// Token: 0x1700072C RID: 1836
		// (get) Token: 0x060015AF RID: 5551 RVA: 0x00054BC9 File Offset: 0x00052DC9
		// (set) Token: 0x060015B0 RID: 5552 RVA: 0x00054BD1 File Offset: 0x00052DD1
		[DataSourceProperty]
		public bool IsAlternativeUpgrade
		{
			get
			{
				return this._isAlternativeUpgrade;
			}
			set
			{
				if (value != this._isAlternativeUpgrade)
				{
					this._isAlternativeUpgrade = value;
					base.OnPropertyChangedWithValue(value, "IsAlternativeUpgrade");
				}
			}
		}

		// Token: 0x1700072D RID: 1837
		// (get) Token: 0x060015B1 RID: 5553 RVA: 0x00054BEF File Offset: 0x00052DEF
		// (set) Token: 0x060015B2 RID: 5554 RVA: 0x00054BF7 File Offset: 0x00052DF7
		[DataSourceProperty]
		public MBBindingList<EncyclopediaTroopTreeNodeVM> Branch
		{
			get
			{
				return this._branch;
			}
			set
			{
				if (value != this._branch)
				{
					this._branch = value;
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaTroopTreeNodeVM>>(value, "Branch");
				}
			}
		}

		// Token: 0x1700072E RID: 1838
		// (get) Token: 0x060015B3 RID: 5555 RVA: 0x00054C15 File Offset: 0x00052E15
		// (set) Token: 0x060015B4 RID: 5556 RVA: 0x00054C1D File Offset: 0x00052E1D
		[DataSourceProperty]
		public EncyclopediaUnitVM Unit
		{
			get
			{
				return this._unit;
			}
			set
			{
				if (value != this._unit)
				{
					this._unit = value;
					base.OnPropertyChangedWithValue<EncyclopediaUnitVM>(value, "Unit");
				}
			}
		}

		// Token: 0x1700072F RID: 1839
		// (get) Token: 0x060015B5 RID: 5557 RVA: 0x00054C3B File Offset: 0x00052E3B
		// (set) Token: 0x060015B6 RID: 5558 RVA: 0x00054C43 File Offset: 0x00052E43
		[DataSourceProperty]
		public BasicTooltipViewModel AlternativeUpgradeTooltip
		{
			get
			{
				return this._alternativeUpgradeTooltip;
			}
			set
			{
				if (value != this._alternativeUpgradeTooltip)
				{
					this._alternativeUpgradeTooltip = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "AlternativeUpgradeTooltip");
				}
			}
		}

		// Token: 0x040009DD RID: 2525
		private MBBindingList<EncyclopediaTroopTreeNodeVM> _branch;

		// Token: 0x040009DE RID: 2526
		private EncyclopediaUnitVM _unit;

		// Token: 0x040009DF RID: 2527
		private bool _isActiveUnit;

		// Token: 0x040009E0 RID: 2528
		private bool _isAlternativeUpgrade;

		// Token: 0x040009E1 RID: 2529
		private BasicTooltipViewModel _alternativeUpgradeTooltip;
	}
}
