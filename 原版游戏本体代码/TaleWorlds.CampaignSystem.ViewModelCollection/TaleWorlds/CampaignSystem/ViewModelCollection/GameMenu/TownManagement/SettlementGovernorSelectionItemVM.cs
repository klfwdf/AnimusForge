using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TownManagement
{
	// Token: 0x020000A4 RID: 164
	public class SettlementGovernorSelectionItemVM : ViewModel
	{
		// Token: 0x1700051B RID: 1307
		// (get) Token: 0x06000FD0 RID: 4048 RVA: 0x00040F00 File Offset: 0x0003F100
		public Hero Governor { get; }

		// Token: 0x06000FD1 RID: 4049 RVA: 0x00040F08 File Offset: 0x0003F108
		public SettlementGovernorSelectionItemVM(Hero governor, Action<SettlementGovernorSelectionItemVM> onSelection)
		{
			this.Governor = governor;
			this._onSelection = onSelection;
			if (governor != null)
			{
				this.Visual = new CharacterImageIdentifierVM(CampaignUIHelper.GetCharacterCode(this.Governor.CharacterObject, true));
				this.GovernorHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetHeroGovernorEffectsTooltip(this.Governor, Settlement.CurrentSettlement));
			}
			else
			{
				this.Visual = new CharacterImageIdentifierVM(null);
				this.GovernorHint = new BasicTooltipViewModel();
			}
			this.RefreshValues();
		}

		// Token: 0x06000FD2 RID: 4050 RVA: 0x00040F80 File Offset: 0x0003F180
		public override void RefreshValues()
		{
			base.RefreshValues();
			if (this.Governor != null)
			{
				this.Name = this.Governor.Name.ToString();
				return;
			}
			this.Visual = new CharacterImageIdentifierVM(null);
			this.Name = new TextObject("{=koX9okuG}None", null).ToString();
		}

		// Token: 0x06000FD3 RID: 4051 RVA: 0x00040FD4 File Offset: 0x0003F1D4
		public void OnSelection()
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			Hero hero;
			if (currentSettlement == null)
			{
				hero = null;
			}
			else
			{
				Town town = currentSettlement.Town;
				hero = ((town != null) ? town.Governor : null);
			}
			Hero hero2 = hero;
			bool flag = this.Governor == null;
			if (hero2 != this.Governor && (!flag || hero2 != null))
			{
				ValueTuple<TextObject, TextObject> governorSelectionConfirmationPopupTexts = CampaignUIHelper.GetGovernorSelectionConfirmationPopupTexts(hero2, this.Governor, currentSettlement);
				InformationManager.ShowInquiry(new InquiryData(governorSelectionConfirmationPopupTexts.Item1.ToString(), governorSelectionConfirmationPopupTexts.Item2.ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), delegate()
				{
					this._onSelection(this);
				}, null, "", 0f, null, null, null), false, false);
			}
		}

		// Token: 0x1700051C RID: 1308
		// (get) Token: 0x06000FD4 RID: 4052 RVA: 0x00041085 File Offset: 0x0003F285
		// (set) Token: 0x06000FD5 RID: 4053 RVA: 0x0004108D File Offset: 0x0003F28D
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

		// Token: 0x1700051D RID: 1309
		// (get) Token: 0x06000FD6 RID: 4054 RVA: 0x000410AB File Offset: 0x0003F2AB
		// (set) Token: 0x06000FD7 RID: 4055 RVA: 0x000410B3 File Offset: 0x0003F2B3
		[DataSourceProperty]
		public BasicTooltipViewModel GovernorHint
		{
			get
			{
				return this._governorHint;
			}
			set
			{
				if (value != this._governorHint)
				{
					this._governorHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "GovernorHint");
				}
			}
		}

		// Token: 0x1700051E RID: 1310
		// (get) Token: 0x06000FD8 RID: 4056 RVA: 0x000410D1 File Offset: 0x0003F2D1
		// (set) Token: 0x06000FD9 RID: 4057 RVA: 0x000410D9 File Offset: 0x0003F2D9
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

		// Token: 0x04000735 RID: 1845
		private readonly Action<SettlementGovernorSelectionItemVM> _onSelection;

		// Token: 0x04000737 RID: 1847
		private CharacterImageIdentifierVM _visual;

		// Token: 0x04000738 RID: 1848
		private string _name;

		// Token: 0x04000739 RID: 1849
		private BasicTooltipViewModel _governorHint;
	}
}
