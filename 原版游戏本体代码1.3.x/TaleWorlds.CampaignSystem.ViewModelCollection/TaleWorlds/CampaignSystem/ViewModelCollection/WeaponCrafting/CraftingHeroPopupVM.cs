using System;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting
{
	// Token: 0x020000F9 RID: 249
	public class CraftingHeroPopupVM : ViewModel
	{
		// Token: 0x06001662 RID: 5730 RVA: 0x00056E68 File Offset: 0x00055068
		public CraftingHeroPopupVM(Func<MBBindingList<CraftingAvailableHeroItemVM>> getCraftingHeroes)
		{
			this.GetCraftingHeroes = getCraftingHeroes;
			this.SelectHeroText = new TextObject("{=xaeXEj8J}Select character for smithing", null).ToString();
		}

		// Token: 0x06001663 RID: 5731 RVA: 0x00056E8D File Offset: 0x0005508D
		public void ExecuteOpenPopup()
		{
			this.IsVisible = true;
		}

		// Token: 0x06001664 RID: 5732 RVA: 0x00056E96 File Offset: 0x00055096
		public void ExecuteClosePopup()
		{
			this.IsVisible = false;
		}

		// Token: 0x06001665 RID: 5733 RVA: 0x00056E9F File Offset: 0x0005509F
		public override void OnFinalize()
		{
			base.OnFinalize();
			InputKeyItemVM exitInputKey = this.ExitInputKey;
			if (exitInputKey == null)
			{
				return;
			}
			exitInputKey.OnFinalize();
		}

		// Token: 0x1700076E RID: 1902
		// (get) Token: 0x06001666 RID: 5734 RVA: 0x00056EB7 File Offset: 0x000550B7
		// (set) Token: 0x06001667 RID: 5735 RVA: 0x00056EBF File Offset: 0x000550BF
		[DataSourceProperty]
		public bool IsVisible
		{
			get
			{
				return this._isVisible;
			}
			set
			{
				if (value != this._isVisible)
				{
					this._isVisible = value;
					base.OnPropertyChangedWithValue(value, "IsVisible");
				}
			}
		}

		// Token: 0x1700076F RID: 1903
		// (get) Token: 0x06001668 RID: 5736 RVA: 0x00056EDD File Offset: 0x000550DD
		// (set) Token: 0x06001669 RID: 5737 RVA: 0x00056EE5 File Offset: 0x000550E5
		[DataSourceProperty]
		public string SelectHeroText
		{
			get
			{
				return this._selectHeroText;
			}
			set
			{
				if (value != this._selectHeroText)
				{
					this._selectHeroText = value;
					base.OnPropertyChangedWithValue<string>(value, "SelectHeroText");
				}
			}
		}

		// Token: 0x17000770 RID: 1904
		// (get) Token: 0x0600166A RID: 5738 RVA: 0x00056F08 File Offset: 0x00055108
		[DataSourceProperty]
		public MBBindingList<CraftingAvailableHeroItemVM> CraftingHeroes
		{
			get
			{
				return this.GetCraftingHeroes();
			}
		}

		// Token: 0x0600166B RID: 5739 RVA: 0x00056F15 File Offset: 0x00055115
		public void SetExitInputKey(HotKey hotKey)
		{
			this.ExitInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x17000771 RID: 1905
		// (get) Token: 0x0600166C RID: 5740 RVA: 0x00056F24 File Offset: 0x00055124
		// (set) Token: 0x0600166D RID: 5741 RVA: 0x00056F2C File Offset: 0x0005512C
		[DataSourceProperty]
		public InputKeyItemVM ExitInputKey
		{
			get
			{
				return this._exitInputKey;
			}
			set
			{
				if (value != this._exitInputKey)
				{
					this._exitInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "ExitInputKey");
				}
			}
		}

		// Token: 0x04000A3F RID: 2623
		private readonly Func<MBBindingList<CraftingAvailableHeroItemVM>> GetCraftingHeroes;

		// Token: 0x04000A40 RID: 2624
		private bool _isVisible;

		// Token: 0x04000A41 RID: 2625
		private string _selectHeroText;

		// Token: 0x04000A42 RID: 2626
		private InputKeyItemVM _exitInputKey;
	}
}
