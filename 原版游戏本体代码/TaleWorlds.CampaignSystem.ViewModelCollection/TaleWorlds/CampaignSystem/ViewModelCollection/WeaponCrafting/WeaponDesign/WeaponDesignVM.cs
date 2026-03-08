using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign.Order;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign
{
	// Token: 0x0200010E RID: 270
	public class WeaponDesignVM : ViewModel
	{
		// Token: 0x060017F3 RID: 6131 RVA: 0x0005ADBC File Offset: 0x00058FBC
		public WeaponDesignVM(Crafting crafting, ICraftingCampaignBehavior craftingBehavior, Action onRefresh, Action onWeaponCrafted, Func<CraftingAvailableHeroItemVM> getCurrentCraftingHero, Action<CraftingOrder> refreshHeroAvailabilities, Func<WeaponComponentData, ItemObject.ItemUsageSetFlags> getItemUsageSetFlags)
		{
			this._crafting = crafting;
			this._craftingBehavior = craftingBehavior;
			this._onRefresh = onRefresh;
			this._onWeaponCrafted = onWeaponCrafted;
			this._getCurrentCraftingHero = getCurrentCraftingHero;
			this._getItemUsageSetFlags = getItemUsageSetFlags;
			this._refreshHeroAvailabilities = refreshHeroAvailabilities;
			this.MaxDifficulty = 300;
			this._currentCraftingSkillText = new TextObject("{=LEiZWuZm}{SKILL_NAME}: {SKILL_VALUE}", null);
			this.PrimaryPropertyList = new MBBindingList<CraftingListPropertyItem>();
			this.DesignResultPropertyList = new MBBindingList<WeaponDesignResultPropertyItemVM>();
			this._newlyUnlockedPieces = new List<CraftingPiece>();
			this._pieceTierComparer = new WeaponDesignVM.PieceTierComparer();
			this.BladePieceList = new CraftingPieceListVM(new MBBindingList<CraftingPieceVM>(), CraftingPiece.PieceTypes.Blade, new Action<CraftingPiece.PieceTypes, bool>(this.OnSelectPieceType));
			this.GuardPieceList = new CraftingPieceListVM(new MBBindingList<CraftingPieceVM>(), CraftingPiece.PieceTypes.Guard, new Action<CraftingPiece.PieceTypes, bool>(this.OnSelectPieceType));
			this.HandlePieceList = new CraftingPieceListVM(new MBBindingList<CraftingPieceVM>(), CraftingPiece.PieceTypes.Handle, new Action<CraftingPiece.PieceTypes, bool>(this.OnSelectPieceType));
			this.PommelPieceList = new CraftingPieceListVM(new MBBindingList<CraftingPieceVM>(), CraftingPiece.PieceTypes.Pommel, new Action<CraftingPiece.PieceTypes, bool>(this.OnSelectPieceType));
			this.PieceLists = new MBBindingList<CraftingPieceListVM> { this.BladePieceList, this.GuardPieceList, this.HandlePieceList, this.PommelPieceList };
			this._pieceListsDictionary = new Dictionary<CraftingPiece.PieceTypes, CraftingPieceListVM>
			{
				{
					CraftingPiece.PieceTypes.Blade,
					this.BladePieceList
				},
				{
					CraftingPiece.PieceTypes.Guard,
					this.GuardPieceList
				},
				{
					CraftingPiece.PieceTypes.Handle,
					this.HandlePieceList
				},
				{
					CraftingPiece.PieceTypes.Pommel,
					this.PommelPieceList
				}
			};
			this._pieceVMs = new Dictionary<CraftingPiece, CraftingPieceVM>();
			this.TierFilters = new MBBindingList<TierFilterTypeVM>
			{
				new TierFilterTypeVM(WeaponDesignVM.CraftingPieceTierFilter.All, new Action<WeaponDesignVM.CraftingPieceTierFilter>(this.OnSelectPieceTierFilter), GameTexts.FindText("str_crafting_tier_filter_all", null).ToString()),
				new TierFilterTypeVM(WeaponDesignVM.CraftingPieceTierFilter.Tier1, new Action<WeaponDesignVM.CraftingPieceTierFilter>(this.OnSelectPieceTierFilter), GameTexts.FindText("str_tier_one", null).ToString()),
				new TierFilterTypeVM(WeaponDesignVM.CraftingPieceTierFilter.Tier2, new Action<WeaponDesignVM.CraftingPieceTierFilter>(this.OnSelectPieceTierFilter), GameTexts.FindText("str_tier_two", null).ToString()),
				new TierFilterTypeVM(WeaponDesignVM.CraftingPieceTierFilter.Tier3, new Action<WeaponDesignVM.CraftingPieceTierFilter>(this.OnSelectPieceTierFilter), GameTexts.FindText("str_tier_three", null).ToString()),
				new TierFilterTypeVM(WeaponDesignVM.CraftingPieceTierFilter.Tier4, new Action<WeaponDesignVM.CraftingPieceTierFilter>(this.OnSelectPieceTierFilter), GameTexts.FindText("str_tier_four", null).ToString()),
				new TierFilterTypeVM(WeaponDesignVM.CraftingPieceTierFilter.Tier5, new Action<WeaponDesignVM.CraftingPieceTierFilter>(this.OnSelectPieceTierFilter), GameTexts.FindText("str_tier_five", null).ToString())
			};
			this._templateComparer = new WeaponDesignVM.TemplateComparer();
			this._primaryUsages = CraftingTemplate.All.ToList<CraftingTemplate>();
			this._primaryUsages.Sort(this._templateComparer);
			this.SecondaryUsageSelector = new SelectorVM<CraftingSecondaryUsageItemVM>(new List<string>(), 0, null);
			this.CraftingOrderPopup = new CraftingOrderPopupVM(new Action<CraftingOrderItemVM>(this.OnCraftingOrderSelected), this._getCurrentCraftingHero, new Func<CraftingOrder, IEnumerable<CraftingStatData>>(this.GetOrderStatDatas));
			this.WeaponClassSelectionPopup = new WeaponClassSelectionPopupVM(this._craftingBehavior, this._primaryUsages, delegate(int x)
			{
				this.RefreshWeaponDesignMode(null, x, false);
			}, new Func<CraftingTemplate, int>(this.GetUnlockedPartsCount));
			this.WeaponFlagIconsList = new MBBindingList<ItemFlagVM>();
			this.CraftedItemVisual = new ItemCollectionElementViewModel();
			CampaignEvents.CraftingPartUnlockedEvent.AddNonSerializedListener(this, new Action<CraftingPiece>(this.OnNewPieceUnlocked));
			this.CraftingHistory = new CraftingHistoryVM(this._crafting, this._craftingBehavior, delegate()
			{
				CraftingOrderItemVM activeCraftingOrder = this.ActiveCraftingOrder;
				if (activeCraftingOrder == null)
				{
					return null;
				}
				return activeCraftingOrder.CraftingOrder;
			}, new Action<WeaponDesignSelectorVM>(this.OnSelectItemFromHistory));
			this.RefreshWeaponDesignMode(null, -1, false);
			this._selectedWeaponClassIndex = this._primaryUsages.IndexOf(this._crafting.CurrentCraftingTemplate);
		}

		// Token: 0x060017F4 RID: 6132 RVA: 0x0005B18C File Offset: 0x0005938C
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.ShowOnlyUnlockedPiecesHint = new HintViewModel(new TextObject("{=dOa7frHR}Show only unlocked pieces", null), null);
			this.ComponentSizeLbl = new TextObject("{=OkWLI5C8}Size:", null).ToString();
			this.AlternativeUsageText = new TextObject("{=13wo3QQB}Secondary", null).ToString();
			this.DefaultUsageText = new TextObject("{=ta4R2RR7}Primary", null).ToString();
			this.DifficultyText = GameTexts.FindText("str_difficulty", null).ToString();
			this.ScabbardHint = new HintViewModel(GameTexts.FindText("str_toggle_scabbard", null), null);
			this.RandomizeHint = new HintViewModel(GameTexts.FindText("str_randomize", null), null);
			this.UndoHint = new HintViewModel(GameTexts.FindText("str_undo", null), null);
			this.RedoHint = new HintViewModel(GameTexts.FindText("str_redo", null), null);
			this.OrderDisabledReasonHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetOrdersDisabledReasonTooltip(this.CraftingOrderPopup.CraftingOrders, this._getCurrentCraftingHero().Hero));
			this._primaryPropertyList.ApplyActionOnAllItems(delegate(CraftingListPropertyItem x)
			{
				x.RefreshValues();
			});
			CraftingPieceVM selectedBladePiece = this._selectedBladePiece;
			if (selectedBladePiece != null)
			{
				selectedBladePiece.RefreshValues();
			}
			CraftingPieceVM selectedGuardPiece = this._selectedGuardPiece;
			if (selectedGuardPiece != null)
			{
				selectedGuardPiece.RefreshValues();
			}
			CraftingPieceVM selectedHandlePiece = this._selectedHandlePiece;
			if (selectedHandlePiece != null)
			{
				selectedHandlePiece.RefreshValues();
			}
			CraftingPieceVM selectedPommelPiece = this._selectedPommelPiece;
			if (selectedPommelPiece != null)
			{
				selectedPommelPiece.RefreshValues();
			}
			this._secondaryUsageSelector.RefreshValues();
			this._craftingOrderPopup.RefreshValues();
			this.ChooseOrderText = this.CraftingOrderPopup.OrderCountText;
			this.ChooseWeaponTypeText = new TextObject("{=Gd6zuUwh}Free Build", null).ToString();
			this.CurrentCraftedWeaponTypeText = this._crafting.CurrentCraftingTemplate.TemplateName.ToString();
			this.CurrentCraftedWeaponTemplateId = this._crafting.CurrentCraftingTemplate.StringId;
		}

		// Token: 0x060017F5 RID: 6133 RVA: 0x0005B360 File Offset: 0x00059560
		public override void OnFinalize()
		{
			base.OnFinalize();
			CampaignEvents.CraftingPartUnlockedEvent.ClearListeners(this);
			CraftingHistoryVM craftingHistory = this.CraftingHistory;
			if (craftingHistory != null)
			{
				craftingHistory.OnFinalize();
			}
			ItemCollectionElementViewModel craftedItemVisual = this.CraftedItemVisual;
			if (craftedItemVisual != null)
			{
				craftedItemVisual.OnFinalize();
			}
			WeaponDesignResultPopupVM craftingResultPopup = this.CraftingResultPopup;
			if (craftingResultPopup != null)
			{
				craftingResultPopup.OnFinalize();
			}
			this.CraftedItemVisual = null;
		}

		// Token: 0x060017F6 RID: 6134 RVA: 0x0005B3B8 File Offset: 0x000595B8
		internal void OnCraftingLogicRefreshed(Crafting newCraftingLogic)
		{
			this._crafting = newCraftingLogic;
			this.InitializeDefaultFromLogic();
		}

		// Token: 0x060017F7 RID: 6135 RVA: 0x0005B3C8 File Offset: 0x000595C8
		private void FilterPieces(WeaponDesignVM.CraftingPieceTierFilter filter)
		{
			List<int> list = new List<int>();
			switch (filter)
			{
			case WeaponDesignVM.CraftingPieceTierFilter.None:
				goto IL_9B;
			case WeaponDesignVM.CraftingPieceTierFilter.Tier1:
				list.Add(1);
				goto IL_9B;
			case WeaponDesignVM.CraftingPieceTierFilter.Tier2:
				list.Add(2);
				goto IL_9B;
			case WeaponDesignVM.CraftingPieceTierFilter.Tier1 | WeaponDesignVM.CraftingPieceTierFilter.Tier2:
			case WeaponDesignVM.CraftingPieceTierFilter.Tier1 | WeaponDesignVM.CraftingPieceTierFilter.Tier3:
			case WeaponDesignVM.CraftingPieceTierFilter.Tier2 | WeaponDesignVM.CraftingPieceTierFilter.Tier3:
			case WeaponDesignVM.CraftingPieceTierFilter.Tier1 | WeaponDesignVM.CraftingPieceTierFilter.Tier2 | WeaponDesignVM.CraftingPieceTierFilter.Tier3:
				break;
			case WeaponDesignVM.CraftingPieceTierFilter.Tier3:
				list.Add(3);
				goto IL_9B;
			case WeaponDesignVM.CraftingPieceTierFilter.Tier4:
				list.Add(4);
				goto IL_9B;
			default:
				if (filter == WeaponDesignVM.CraftingPieceTierFilter.Tier5)
				{
					list.Add(5);
					goto IL_9B;
				}
				if (filter == WeaponDesignVM.CraftingPieceTierFilter.All)
				{
					list.AddRange(new int[] { 1, 2, 3, 4, 5 });
					goto IL_9B;
				}
				break;
			}
			Debug.FailedAssert("Invalid tier filter", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Crafting\\WeaponDesign\\WeaponDesignVM.cs", "FilterPieces", 217);
			IL_9B:
			foreach (TierFilterTypeVM tierFilterTypeVM in this.TierFilters)
			{
				tierFilterTypeVM.IsSelected = filter.HasAllFlags(tierFilterTypeVM.FilterType);
			}
			foreach (CraftingPieceListVM craftingPieceListVM in this.PieceLists)
			{
				foreach (CraftingPieceVM craftingPieceVM in craftingPieceListVM.Pieces)
				{
					bool flag = list.Contains(craftingPieceVM.CraftingPiece.CraftingPiece.PieceTier);
					bool flag2 = this.ShowOnlyUnlockedPieces && !craftingPieceVM.PlayerHasPiece;
					craftingPieceVM.IsFilteredOut = !flag || flag2;
				}
			}
			this._currentTierFilter = filter;
		}

		// Token: 0x060017F8 RID: 6136 RVA: 0x0005B570 File Offset: 0x00059770
		private void OnNewPieceUnlocked(CraftingPiece piece)
		{
			if (piece.IsValid && !piece.IsHiddenOnDesigner)
			{
				this.SetPieceNewlyUnlocked(piece);
				CraftingPieceVM craftingPieceVM;
				if (this._pieceVMs.TryGetValue(piece, out craftingPieceVM))
				{
					craftingPieceVM.PlayerHasPiece = true;
					craftingPieceVM.IsNewlyUnlocked = true;
				}
			}
		}

		// Token: 0x060017F9 RID: 6137 RVA: 0x0005B5B4 File Offset: 0x000597B4
		private int GetUnlockedPartsCount(CraftingTemplate template)
		{
			return template.Pieces.Count((CraftingPiece piece) => this._craftingBehavior.IsOpened(piece, template) && !string.IsNullOrEmpty(piece.MeshName));
		}

		// Token: 0x060017FA RID: 6138 RVA: 0x0005B5F1 File Offset: 0x000597F1
		private WeaponClassVM GetCurrentWeaponClass()
		{
			if (this._selectedWeaponClassIndex >= 0 && this._selectedWeaponClassIndex < this.WeaponClassSelectionPopup.WeaponClasses.Count)
			{
				return this.WeaponClassSelectionPopup.WeaponClasses[this._selectedWeaponClassIndex];
			}
			return null;
		}

		// Token: 0x060017FB RID: 6139 RVA: 0x0005B62C File Offset: 0x0005982C
		private void OnSelectItemFromHistory(WeaponDesignSelectorVM selector)
		{
			WeaponDesign design = selector.Design;
			if (design == null)
			{
				Debug.FailedAssert("History design returned null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Crafting\\WeaponDesign\\WeaponDesignVM.cs", "OnSelectItemFromHistory", 283);
				return;
			}
			ValueTuple<CraftingPiece, int>[] array = new ValueTuple<CraftingPiece, int>[design.UsedPieces.Length];
			for (int i = 0; i < design.UsedPieces.Length; i++)
			{
				array[i] = new ValueTuple<CraftingPiece, int>(design.UsedPieces[i].CraftingPiece, design.UsedPieces[i].ScalePercentage);
			}
			this.SetDesignManually(design.Template, array, true);
		}

		// Token: 0x060017FC RID: 6140 RVA: 0x0005B6B8 File Offset: 0x000598B8
		public void SetPieceNewlyUnlocked(CraftingPiece piece)
		{
			if (!this._newlyUnlockedPieces.Contains(piece))
			{
				this._newlyUnlockedPieces.Add(piece);
			}
		}

		// Token: 0x060017FD RID: 6141 RVA: 0x0005B6D4 File Offset: 0x000598D4
		private void UnsetPieceNewlyUnlocked(CraftingPieceVM pieceVM)
		{
			CraftingPiece craftingPiece = pieceVM.CraftingPiece.CraftingPiece;
			if (this._newlyUnlockedPieces.Contains(craftingPiece))
			{
				this._newlyUnlockedPieces.Remove(craftingPiece);
				pieceVM.IsNewlyUnlocked = false;
			}
		}

		// Token: 0x060017FE RID: 6142 RVA: 0x0005B70F File Offset: 0x0005990F
		private void OnSelectPieceTierFilter(WeaponDesignVM.CraftingPieceTierFilter filter)
		{
			if (this._currentTierFilter != filter)
			{
				this.FilterPieces(filter);
			}
		}

		// Token: 0x060017FF RID: 6143 RVA: 0x0005B724 File Offset: 0x00059924
		private void OnSelectPieceType(CraftingPiece.PieceTypes pieceType, bool fromClick = false)
		{
			CraftingPieceListVM craftingPieceListVM = this.PieceLists.ElementAt(this.SelectedPieceTypeIndex);
			if (craftingPieceListVM != null && fromClick)
			{
				foreach (CraftingPieceVM craftingPieceVM in craftingPieceListVM.Pieces)
				{
					if (craftingPieceVM.IsNewlyUnlocked)
					{
						this.UnsetPieceNewlyUnlocked(craftingPieceVM);
					}
				}
			}
			foreach (CraftingPieceListVM craftingPieceListVM2 in this.PieceLists)
			{
				craftingPieceListVM2.Refresh();
				if (craftingPieceListVM2.PieceType == pieceType)
				{
					craftingPieceListVM2.IsSelected = true;
					this.ActivePieceList = craftingPieceListVM2;
				}
				else
				{
					craftingPieceListVM2.IsSelected = false;
				}
			}
			this.SelectedPieceTypeIndex = (int)pieceType;
			base.OnPropertyChanged("ActivePieceSize");
		}

		// Token: 0x06001800 RID: 6144 RVA: 0x0005B804 File Offset: 0x00059A04
		private void SelectDefaultPiecesForCurrentTemplate()
		{
			CraftingOrderItemVM activeCraftingOrder = this.ActiveCraftingOrder;
			string text = ((activeCraftingOrder != null) ? activeCraftingOrder.CraftingOrder.GetStatWeapon().WeaponDescriptionId : null);
			WeaponDescription statWeaponUsage = ((text != null) ? MBObjectManager.Instance.GetObject<WeaponDescription>(text) : null);
			WeaponClassVM currentWeaponClass = this.GetCurrentWeaponClass();
			this._shouldRecordHistory = false;
			this._isAutoSelectingPieces = true;
			Func<CraftingPieceVM, bool> <>9__3;
			foreach (CraftingPieceListVM craftingPieceListVM in this.PieceLists)
			{
				if (this._crafting.CurrentCraftingTemplate.IsPieceTypeUsable(craftingPieceListVM.PieceType))
				{
					CraftingPieceVM craftingPieceVM = null;
					if (this.IsInFreeMode && currentWeaponClass != null)
					{
						string selectedPieceID = currentWeaponClass.GetSelectedPieceData(craftingPieceListVM.PieceType);
						craftingPieceVM = craftingPieceListVM.Pieces.FirstOrDefault((CraftingPieceVM p) => p.CraftingPiece.CraftingPiece.StringId == selectedPieceID);
					}
					if (craftingPieceVM == null)
					{
						IOrderedEnumerable<CraftingPieceVM> source = from p in craftingPieceListVM.Pieces
							orderby p.PlayerHasPiece descending, !p.IsNewlyUnlocked descending
							select p;
						Func<CraftingPieceVM, bool> keySelector;
						if ((keySelector = <>9__3) == null)
						{
							keySelector = (<>9__3 = (CraftingPieceVM p) => statWeaponUsage == null || statWeaponUsage.AvailablePieces.Any((CraftingPiece x) => x.StringId == p.CraftingPiece.CraftingPiece.StringId));
						}
						craftingPieceVM = source.ThenByDescending(keySelector).FirstOrDefault<CraftingPieceVM>();
					}
					if (craftingPieceVM != null)
					{
						craftingPieceVM.ExecuteSelect();
					}
				}
			}
			this._shouldRecordHistory = true;
			this._isAutoSelectingPieces = false;
		}

		// Token: 0x06001801 RID: 6145 RVA: 0x0005B9A8 File Offset: 0x00059BA8
		private void InitializeDefaultFromLogic()
		{
			this.PrimaryPropertyList.Clear();
			this.BladePieceList.Pieces.Clear();
			this.GuardPieceList.Pieces.Clear();
			this.HandlePieceList.Pieces.Clear();
			this.PommelPieceList.Pieces.Clear();
			this.SelectedBladePiece = new CraftingPieceVM();
			this.SelectedGuardPiece = new CraftingPieceVM();
			this.SelectedHandlePiece = new CraftingPieceVM();
			this.SelectedPommelPiece = new CraftingPieceVM();
			this._pieceVMs.Clear();
			bool flag = Campaign.Current.GameMode == CampaignGameMode.Tutorial;
			foreach (CraftingPieceListVM craftingPieceListVM in this.PieceLists)
			{
				if (this._crafting.CurrentCraftingTemplate.IsPieceTypeUsable(craftingPieceListVM.PieceType))
				{
					int pieceType = (int)craftingPieceListVM.PieceType;
					for (int i = 0; i < this._crafting.UsablePiecesList[pieceType].Count; i++)
					{
						WeaponDesignElement weaponDesignElement = this._crafting.UsablePiecesList[pieceType][i];
						if (flag || !weaponDesignElement.CraftingPiece.IsHiddenOnDesigner)
						{
							bool flag2 = this._craftingBehavior.IsOpened(weaponDesignElement.CraftingPiece, this._crafting.CurrentCraftingTemplate);
							CraftingPieceVM craftingPieceVM = new CraftingPieceVM(new Action<CraftingPieceVM>(this.OnSetItemPieceManually), this._crafting.CurrentCraftingTemplate.StringId, this._crafting.UsablePiecesList[pieceType][i], pieceType, i, flag2);
							craftingPieceListVM.Pieces.Add(craftingPieceVM);
							craftingPieceVM.IsNewlyUnlocked = flag2 && this._newlyUnlockedPieces.Contains(weaponDesignElement.CraftingPiece);
							if (this._crafting.SelectedPieces[pieceType].CraftingPiece == craftingPieceVM.CraftingPiece.CraftingPiece)
							{
								craftingPieceListVM.SelectedPiece = craftingPieceVM;
								craftingPieceVM.IsSelected = true;
							}
							this._pieceVMs.Add(this._crafting.UsablePiecesList[pieceType][i].CraftingPiece, craftingPieceVM);
						}
					}
					craftingPieceListVM.Pieces.Sort(this._pieceTierComparer);
				}
			}
			CraftingPieceListVM craftingPieceListVM2 = this.PieceLists.FirstOrDefault((CraftingPieceListVM x) => x.PieceType == CraftingPiece.PieceTypes.Blade);
			this.SelectedBladePiece = ((craftingPieceListVM2 != null) ? craftingPieceListVM2.SelectedPiece : null);
			CraftingPieceListVM craftingPieceListVM3 = this.PieceLists.FirstOrDefault((CraftingPieceListVM x) => x.PieceType == CraftingPiece.PieceTypes.Guard);
			this.SelectedGuardPiece = ((craftingPieceListVM3 != null) ? craftingPieceListVM3.SelectedPiece : null);
			CraftingPieceListVM craftingPieceListVM4 = this.PieceLists.FirstOrDefault((CraftingPieceListVM x) => x.PieceType == CraftingPiece.PieceTypes.Handle);
			this.SelectedHandlePiece = ((craftingPieceListVM4 != null) ? craftingPieceListVM4.SelectedPiece : null);
			CraftingPieceListVM craftingPieceListVM5 = this.PieceLists.FirstOrDefault((CraftingPieceListVM x) => x.PieceType == CraftingPiece.PieceTypes.Pommel);
			this.SelectedPommelPiece = ((craftingPieceListVM5 != null) ? craftingPieceListVM5.SelectedPiece : null);
			this.ItemName = this._crafting.CraftedWeaponName.ToString();
			this.PommelSize = 0;
			this.GuardSize = 0;
			this.HandleSize = 0;
			this.BladeSize = 0;
			this.RefreshPieceFlags();
			this.RefreshItem();
			this.RefreshAlternativeUsageList();
		}

		// Token: 0x06001802 RID: 6146 RVA: 0x0005BD28 File Offset: 0x00059F28
		private void RefreshPieceFlags()
		{
			foreach (CraftingPieceListVM craftingPieceListVM in this.PieceLists)
			{
				craftingPieceListVM.IsEnabled = this._crafting.CurrentCraftingTemplate.IsPieceTypeUsable(craftingPieceListVM.PieceType);
				foreach (CraftingPieceVM craftingPieceVM in craftingPieceListVM.Pieces)
				{
					craftingPieceVM.RefreshFlagIcons();
					if (craftingPieceListVM.PieceType == CraftingPiece.PieceTypes.Blade)
					{
						this.AddClassFlagsToPiece(craftingPieceVM);
					}
				}
			}
			this.RefreshWeaponFlags();
		}

		// Token: 0x06001803 RID: 6147 RVA: 0x0005BDDC File Offset: 0x00059FDC
		private void AddClassFlagsToPiece(CraftingPieceVM piece)
		{
			WeaponComponentData weaponWithUsageIndex = this._crafting.GetCurrentCraftedItemObject(false, null).GetWeaponWithUsageIndex(this.SecondaryUsageSelector.SelectedIndex);
			int indexOfUsageDataWithId = this._crafting.CurrentCraftingTemplate.GetIndexOfUsageDataWithId(weaponWithUsageIndex.WeaponDescriptionId);
			WeaponDescription weaponDescription = this._crafting.CurrentCraftingTemplate.WeaponDescriptions.ElementAtOrDefault(indexOfUsageDataWithId);
			if (weaponDescription != null)
			{
				using (List<ValueTuple<string, TextObject>>.Enumerator enumerator = CampaignUIHelper.GetWeaponFlagDetails(weaponDescription.WeaponFlags, null).GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						ValueTuple<string, TextObject> flagPath = enumerator.Current;
						if (!piece.ItemAttributeIcons.Any((CraftingItemFlagVM x) => x.Icon.Contains(flagPath.Item1)))
						{
							piece.ItemAttributeIcons.Add(new CraftingItemFlagVM(flagPath.Item1, flagPath.Item2, false));
						}
					}
				}
			}
			using (List<ValueTuple<string, TextObject>>.Enumerator enumerator = CampaignUIHelper.GetFlagDetailsForWeapon(weaponWithUsageIndex, this._getItemUsageSetFlags(weaponWithUsageIndex), null).GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ValueTuple<string, TextObject> usageFlag = enumerator.Current;
					if (!piece.ItemAttributeIcons.Any((CraftingItemFlagVM x) => x.Icon.Contains(usageFlag.Item1)))
					{
						piece.ItemAttributeIcons.Add(new CraftingItemFlagVM(usageFlag.Item1, usageFlag.Item2, false));
					}
				}
			}
		}

		// Token: 0x06001804 RID: 6148 RVA: 0x0005BF68 File Offset: 0x0005A168
		private void UpdateSecondaryUsageIndex(SelectorVM<CraftingSecondaryUsageItemVM> selector)
		{
			if (selector.SelectedIndex != -1)
			{
				this.RefreshStats();
				this.RefreshPieceFlags();
			}
		}

		// Token: 0x06001805 RID: 6149 RVA: 0x0005BF80 File Offset: 0x0005A180
		private MBBindingList<WeaponDesignResultPropertyItemVM> GetResultPropertyList(CraftingSecondaryUsageItemVM usageItem)
		{
			MBBindingList<WeaponDesignResultPropertyItemVM> mbbindingList = new MBBindingList<WeaponDesignResultPropertyItemVM>();
			if (usageItem == null)
			{
				return mbbindingList;
			}
			int usageIndex = usageItem.UsageIndex;
			this.TrySetSecondaryUsageIndex(usageIndex);
			this.RefreshStats();
			ItemModifier currentItemModifier = this._craftingBehavior.GetCurrentItemModifier();
			foreach (CraftingListPropertyItem craftingListPropertyItem in this.PrimaryPropertyList)
			{
				float changeAmount = 0f;
				bool showFloatingPoint = craftingListPropertyItem.Type == CraftingTemplate.CraftingStatTypes.Weight;
				if (currentItemModifier != null)
				{
					float num = craftingListPropertyItem.PropertyValue;
					if (craftingListPropertyItem.Type == CraftingTemplate.CraftingStatTypes.SwingDamage)
					{
						num = (float)currentItemModifier.ModifyDamage((int)craftingListPropertyItem.PropertyValue);
					}
					else if (craftingListPropertyItem.Type == CraftingTemplate.CraftingStatTypes.SwingSpeed)
					{
						num = (float)currentItemModifier.ModifySpeed((int)craftingListPropertyItem.PropertyValue);
					}
					else if (craftingListPropertyItem.Type == CraftingTemplate.CraftingStatTypes.ThrustDamage)
					{
						num = (float)currentItemModifier.ModifyDamage((int)craftingListPropertyItem.PropertyValue);
					}
					else if (craftingListPropertyItem.Type == CraftingTemplate.CraftingStatTypes.ThrustSpeed)
					{
						num = (float)currentItemModifier.ModifySpeed((int)craftingListPropertyItem.PropertyValue);
					}
					else if (craftingListPropertyItem.Type == CraftingTemplate.CraftingStatTypes.Handling)
					{
						num = (float)currentItemModifier.ModifySpeed((int)craftingListPropertyItem.PropertyValue);
					}
					if (num != craftingListPropertyItem.PropertyValue)
					{
						changeAmount = num - craftingListPropertyItem.PropertyValue;
					}
				}
				if (this.IsInOrderMode)
				{
					mbbindingList.Add(new WeaponDesignResultPropertyItemVM(craftingListPropertyItem.Description, craftingListPropertyItem.PropertyValue, craftingListPropertyItem.TargetValue, changeAmount, showFloatingPoint, craftingListPropertyItem.IsExceedingBeneficial, true));
				}
				else
				{
					mbbindingList.Add(new WeaponDesignResultPropertyItemVM(craftingListPropertyItem.Description, craftingListPropertyItem.PropertyValue, changeAmount, showFloatingPoint));
				}
			}
			return mbbindingList;
		}

		// Token: 0x06001806 RID: 6150 RVA: 0x0005C120 File Offset: 0x0005A320
		public void SelectPrimaryWeaponClass(CraftingTemplate template)
		{
			int selectedWeaponClassIndex = this._primaryUsages.IndexOf(template);
			this._selectedWeaponClassIndex = selectedWeaponClassIndex;
			if (this._crafting.CurrentCraftingTemplate != template)
			{
				CraftingHelper.ChangeCurrentCraftingTemplate(template);
				return;
			}
			this.AddHistoryKey();
		}

		// Token: 0x06001807 RID: 6151 RVA: 0x0005C15C File Offset: 0x0005A35C
		private void RefreshWeaponDesignMode(CraftingOrderItemVM orderToSelect, int classIndex = -1, bool doNotAutoSelectPieces = false)
		{
			bool flag = false;
			CraftingTemplate selectedCraftingTemplate = null;
			this.SecondaryUsageSelector.SelectedIndex = 0;
			if (orderToSelect != null)
			{
				this.IsInOrderMode = true;
				this.ActiveCraftingOrder = orderToSelect;
				selectedCraftingTemplate = orderToSelect.CraftingOrder.PreCraftedWeaponDesignItem.WeaponDesign.Template;
				this.SelectPrimaryWeaponClass(selectedCraftingTemplate);
				flag = true;
			}
			else
			{
				this.IsInOrderMode = false;
				this.ActiveCraftingOrder = null;
				if (classIndex >= 0)
				{
					selectedCraftingTemplate = this._primaryUsages[classIndex];
					this.SelectPrimaryWeaponClass(selectedCraftingTemplate);
					flag = true;
				}
			}
			WeaponClassVM weaponClassVM = this.WeaponClassSelectionPopup.WeaponClasses.FirstOrDefault((WeaponClassVM x) => x.Template == selectedCraftingTemplate);
			if (weaponClassVM != null)
			{
				weaponClassVM.NewlyUnlockedPieceCount = 0;
			}
			this.CraftingOrderPopup.RefreshOrders();
			this.CraftingHistory.RefreshAvailability();
			this.IsOrderButtonActive = this.CraftingOrderPopup.HasEnabledOrders;
			Action onRefresh = this._onRefresh;
			if (onRefresh != null)
			{
				onRefresh();
			}
			Action<CraftingOrder> refreshHeroAvailabilities = this._refreshHeroAvailabilities;
			if (refreshHeroAvailabilities != null)
			{
				CraftingOrderItemVM activeCraftingOrder = this.ActiveCraftingOrder;
				refreshHeroAvailabilities((activeCraftingOrder != null) ? activeCraftingOrder.CraftingOrder : null);
			}
			if (!flag)
			{
				this.InitializeDefaultFromLogic();
			}
			this.RefreshValues();
			this.RefreshItem();
			this.OnSelectPieceType(CraftingPiece.PieceTypes.Blade, false);
			this.FilterPieces(this._currentTierFilter);
			this.RefreshCurrentHeroSkillLevel();
			if (!doNotAutoSelectPieces)
			{
				this.SelectDefaultPiecesForCurrentTemplate();
			}
		}

		// Token: 0x06001808 RID: 6152 RVA: 0x0005C2AC File Offset: 0x0005A4AC
		private void OnCraftingOrderSelected(CraftingOrderItemVM selectedOrder)
		{
			this.RefreshWeaponDesignMode(selectedOrder, -1, false);
		}

		// Token: 0x06001809 RID: 6153 RVA: 0x0005C2B8 File Offset: 0x0005A4B8
		public void ExecuteOpenOrderPopup()
		{
			this.CraftingOrderPopup.ExecuteOpenPopup();
			MBBindingList<CraftingOrderItemVM> craftingOrders = this.CraftingOrderPopup.CraftingOrders;
			CraftingOrderItemVM craftingOrderItemVM = ((craftingOrders != null) ? craftingOrders.FirstOrDefault(delegate(CraftingOrderItemVM x)
			{
				CraftingOrder craftingOrder = x.CraftingOrder;
				CraftingOrderItemVM activeCraftingOrder = this.ActiveCraftingOrder;
				return craftingOrder == ((activeCraftingOrder != null) ? activeCraftingOrder.CraftingOrder : null);
			}) : null);
			if (craftingOrderItemVM != null)
			{
				craftingOrderItemVM.IsSelected = true;
			}
		}

		// Token: 0x0600180A RID: 6154 RVA: 0x0005C2FE File Offset: 0x0005A4FE
		public void ExecuteCloseOrderPopup()
		{
			this.CraftingOrderPopup.IsVisible = false;
		}

		// Token: 0x0600180B RID: 6155 RVA: 0x0005C30C File Offset: 0x0005A50C
		public void ExecuteOpenOrdersTab()
		{
			if (this.IsInFreeMode)
			{
				MBBindingList<CraftingOrderItemVM> craftingOrders = this.CraftingOrderPopup.CraftingOrders;
				CraftingOrderItemVM craftingOrderItemVM;
				if (craftingOrders == null)
				{
					craftingOrderItemVM = null;
				}
				else
				{
					craftingOrderItemVM = craftingOrders.FirstOrDefault((CraftingOrderItemVM x) => x.IsEnabled);
				}
				CraftingOrderItemVM craftingOrderItemVM2 = craftingOrderItemVM;
				if (craftingOrderItemVM2 != null)
				{
					this.CraftingOrderPopup.SelectOrder(craftingOrderItemVM2);
				}
				else
				{
					this.CraftingOrderPopup.ExecuteOpenPopup();
				}
				Game game = Game.Current;
				if (game == null)
				{
					return;
				}
				game.EventManager.TriggerEvent<CraftingOrderTabOpenedEvent>(new CraftingOrderTabOpenedEvent(true));
			}
		}

		// Token: 0x0600180C RID: 6156 RVA: 0x0005C38E File Offset: 0x0005A58E
		public void ExecuteOpenWeaponClassSelectionPopup()
		{
			this.WeaponClassSelectionPopup.UpdateNewlyUnlockedPiecesCount(this._newlyUnlockedPieces);
			this.WeaponClassSelectionPopup.WeaponClasses.ApplyActionOnAllItems(delegate(WeaponClassVM x)
			{
				x.IsSelected = x.SelectionIndex == this._selectedWeaponClassIndex;
			});
			this.WeaponClassSelectionPopup.ExecuteOpenPopup();
		}

		// Token: 0x0600180D RID: 6157 RVA: 0x0005C3C8 File Offset: 0x0005A5C8
		public void ExecuteOpenFreeBuildTab()
		{
			if (this.IsInOrderMode)
			{
				this.WeaponClassSelectionPopup.UpdateNewlyUnlockedPiecesCount(this._newlyUnlockedPieces);
				this.WeaponClassSelectionPopup.WeaponClasses.ApplyActionOnAllItems(delegate(WeaponClassVM x)
				{
					x.IsSelected = false;
				});
				this.WeaponClassSelectionPopup.ExecuteSelectWeaponClass(0);
				Game game = Game.Current;
				if (game == null)
				{
					return;
				}
				game.EventManager.TriggerEvent<CraftingOrderTabOpenedEvent>(new CraftingOrderTabOpenedEvent(false));
			}
		}

		// Token: 0x0600180E RID: 6158 RVA: 0x0005C444 File Offset: 0x0005A644
		public void CreateCraftingResultPopup()
		{
			this.CraftedItemVisual.StringId = this.CraftedItemObject.StringId;
			this.IsWeaponCivilian = this.CraftedItemObject.IsCivilian;
			WeaponDesignResultPopupVM craftingResultPopup = this.CraftingResultPopup;
			if (craftingResultPopup != null)
			{
				craftingResultPopup.OnFinalize();
			}
			ItemObject craftedItemObject = this.CraftedItemObject;
			TextObject craftedWeaponName = this._crafting.CraftedWeaponName;
			Action onFinalize = new Action(this.ExecuteFinalizeCrafting);
			Crafting crafting = this._crafting;
			CraftingOrderItemVM activeCraftingOrder = this.ActiveCraftingOrder;
			this.CraftingResultPopup = new WeaponDesignResultPopupVM(craftedItemObject, craftedWeaponName, onFinalize, crafting, (activeCraftingOrder != null) ? activeCraftingOrder.CraftingOrder : null, this._craftedItemVisual, this.WeaponFlagIconsList, new Func<CraftingSecondaryUsageItemVM, MBBindingList<WeaponDesignResultPropertyItemVM>>(this.GetResultPropertyList), new Action<CraftingSecondaryUsageItemVM>(this.OnSecondaryUsageChangedFromPopup));
		}

		// Token: 0x0600180F RID: 6159 RVA: 0x0005C4F0 File Offset: 0x0005A6F0
		private void OnSecondaryUsageChangedFromPopup(CraftingSecondaryUsageItemVM usage)
		{
			for (int i = 0; i < this.SecondaryUsageSelector.ItemList.Count; i++)
			{
				if (this.SecondaryUsageSelector.ItemList[i].UsageIndex == usage.UsageIndex)
				{
					this.SecondaryUsageSelector.SelectedIndex = i;
					return;
				}
			}
		}

		// Token: 0x06001810 RID: 6160 RVA: 0x0005C543 File Offset: 0x0005A743
		public void ExecuteToggleShowOnlyUnlockedPieces()
		{
			this.ShowOnlyUnlockedPieces = !this.ShowOnlyUnlockedPieces;
			this.FilterPieces(this._currentTierFilter);
		}

		// Token: 0x06001811 RID: 6161 RVA: 0x0005C560 File Offset: 0x0005A760
		public void ExecuteUndo()
		{
			if (this._crafting.Undo())
			{
				Action onRefresh = this._onRefresh;
				if (onRefresh != null)
				{
					onRefresh();
				}
				this._updatePiece = false;
				int i2;
				int i;
				for (i = 0; i < 4; i = i2 + 1)
				{
					CraftingPiece.PieceTypes j = (CraftingPiece.PieceTypes)i;
					if (this._crafting.CurrentCraftingTemplate.IsPieceTypeUsable(j))
					{
						CraftingPieceVM piece2 = this._pieceListsDictionary[j].Pieces.First((CraftingPieceVM piece) => piece.CraftingPiece.CraftingPiece == this._crafting.SelectedPieces[i].CraftingPiece);
						this.OnSetItemPiece(piece2, 0, true, false);
					}
					i2 = i;
				}
				this.RefreshItem();
				this._updatePiece = true;
			}
		}

		// Token: 0x06001812 RID: 6162 RVA: 0x0005C618 File Offset: 0x0005A818
		public void ExecuteRedo()
		{
			if (this._crafting.Redo())
			{
				Action onRefresh = this._onRefresh;
				if (onRefresh != null)
				{
					onRefresh();
				}
				this._updatePiece = false;
				int i2;
				int i;
				for (i = 0; i < 4; i = i2 + 1)
				{
					CraftingPiece.PieceTypes j = (CraftingPiece.PieceTypes)i;
					if (this._crafting.CurrentCraftingTemplate.IsPieceTypeUsable(j))
					{
						CraftingPieceVM piece2 = this._pieceListsDictionary[j].Pieces.First((CraftingPieceVM piece) => piece.CraftingPiece.CraftingPiece == this._crafting.SelectedPieces[i].CraftingPiece);
						this.OnSetItemPiece(piece2, 0, true, false);
					}
					i2 = i;
				}
				this.RefreshItem();
				this._updatePiece = true;
			}
		}

		// Token: 0x06001813 RID: 6163 RVA: 0x0005C6D0 File Offset: 0x0005A8D0
		internal void OnCraftingHeroChanged(CraftingAvailableHeroItemVM newHero)
		{
			this.RefreshCurrentHeroSkillLevel();
			this.RefreshDifficulty();
			this.CraftingOrderPopup.RefreshOrders();
			this.IsOrderButtonActive = this.CraftingOrderPopup.HasEnabledOrders;
		}

		// Token: 0x06001814 RID: 6164 RVA: 0x0005C6FC File Offset: 0x0005A8FC
		public void ChangeModeIfHeroIsUnavailable()
		{
			CraftingAvailableHeroItemVM craftingAvailableHeroItemVM = this._getCurrentCraftingHero();
			if (this.IsInOrderMode && craftingAvailableHeroItemVM.IsDisabled)
			{
				this.RefreshWeaponDesignMode(null, -1, false);
			}
		}

		// Token: 0x06001815 RID: 6165 RVA: 0x0005C730 File Offset: 0x0005A930
		public void ExecuteBeginHeroHint()
		{
			CraftingOrderItemVM activeCraftingOrder = this._activeCraftingOrder;
			if (((activeCraftingOrder != null) ? activeCraftingOrder.CraftingOrder.OrderOwner : null) != null)
			{
				InformationManager.ShowTooltip(typeof(Hero), new object[]
				{
					this._activeCraftingOrder.CraftingOrder.OrderOwner,
					false
				});
			}
		}

		// Token: 0x06001816 RID: 6166 RVA: 0x0005C787 File Offset: 0x0005A987
		public void ExecuteEndHeroHint()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x06001817 RID: 6167 RVA: 0x0005C790 File Offset: 0x0005A990
		public void ExecuteRandomize()
		{
			for (int i = 0; i < 4; i++)
			{
				CraftingPiece.PieceTypes pieceTypes = (CraftingPiece.PieceTypes)i;
				if (this._crafting.CurrentCraftingTemplate.IsPieceTypeUsable(pieceTypes))
				{
					CraftingPieceVM randomElementWithPredicate = this._pieceListsDictionary[pieceTypes].Pieces.GetRandomElementWithPredicate((CraftingPieceVM p) => p.PlayerHasPiece);
					if (randomElementWithPredicate != null)
					{
						this.OnSetItemPiece(randomElementWithPredicate, (int)(90f + MBRandom.RandomFloat * 20f), false, true);
					}
				}
			}
			this._updatePiece = false;
			this.RefreshItem();
			this.AddHistoryKey();
			this._updatePiece = true;
		}

		// Token: 0x06001818 RID: 6168 RVA: 0x0005C82C File Offset: 0x0005AA2C
		public void ExecuteChangeScabbardVisibility()
		{
			if (!this._crafting.CurrentCraftingTemplate.UseWeaponAsHolsterMesh)
			{
				this.IsScabbardVisible = !this.IsScabbardVisible;
			}
		}

		// Token: 0x06001819 RID: 6169 RVA: 0x0005C850 File Offset: 0x0005AA50
		public void SelectWeapon(ItemObject itemObject)
		{
			this._crafting.SwitchToCraftedItem(itemObject);
			Action onRefresh = this._onRefresh;
			if (onRefresh != null)
			{
				onRefresh();
			}
			this._updatePiece = false;
			int i;
			int i2;
			for (i = 0; i < 4; i = i2 + 1)
			{
				CraftingPiece.PieceTypes j = (CraftingPiece.PieceTypes)i;
				if (this._crafting.CurrentCraftingTemplate.IsPieceTypeUsable(j))
				{
					CraftingPieceVM piece2 = this._pieceListsDictionary[j].Pieces.First((CraftingPieceVM piece) => piece.CraftingPiece.CraftingPiece == this._crafting.CurrentWeaponDesign.UsedPieces[i].CraftingPiece);
					this.OnSetItemPiece(piece2, this._crafting.CurrentWeaponDesign.UsedPieces[i].ScalePercentage, true, false);
				}
				i2 = i;
			}
			this.RefreshItem();
			this.AddHistoryKey();
			this._updatePiece = true;
		}

		// Token: 0x0600181A RID: 6170 RVA: 0x0005C928 File Offset: 0x0005AB28
		public bool CanCompleteOrder()
		{
			bool result = true;
			if (this.IsInOrderMode)
			{
				ItemObject currentCraftedItemObject = this._crafting.GetCurrentCraftedItemObject(false, null);
				result = this.ActiveCraftingOrder.CraftingOrder.CanHeroCompleteOrder(this._getCurrentCraftingHero().Hero, currentCraftedItemObject);
			}
			return result;
		}

		// Token: 0x0600181B RID: 6171 RVA: 0x0005C970 File Offset: 0x0005AB70
		public void ExecuteFinalizeCrafting()
		{
			if (this._craftingBehavior != null && Campaign.Current.GameMode == CampaignGameMode.Campaign)
			{
				if (GameStateManager.Current.ActiveState is CraftingState)
				{
					if (this.IsInOrderMode)
					{
						this._craftingBehavior.CompleteOrder(Settlement.CurrentSettlement.Town, this.ActiveCraftingOrder.CraftingOrder, this.CraftedItemObject, this._getCurrentCraftingHero().Hero);
						this.CraftedItemObject = null;
						this.CraftingOrderPopup.RefreshOrders();
						CraftingOrderItemVM craftingOrderItemVM = this.CraftingOrderPopup.CraftingOrders.FirstOrDefault((CraftingOrderItemVM x) => x.IsEnabled);
						if (craftingOrderItemVM != null)
						{
							this.CraftingOrderPopup.SelectOrder(craftingOrderItemVM);
						}
						else
						{
							this.ExecuteOpenFreeBuildTab();
						}
					}
					else
					{
						int bladeSize = this.BladeSize;
						int guardSize = this.GuardSize;
						int handleSize = this.HandleSize;
						int pommelSize = this.PommelSize;
						this.RefreshWeaponDesignMode(null, this._selectedWeaponClassIndex, false);
						this.BladeSize = bladeSize;
						this.GuardSize = guardSize;
						this.HandleSize = handleSize;
						this.PommelSize = pommelSize;
					}
				}
				this.IsInFinalCraftingStage = false;
			}
			TextObject textObject = new TextObject("{=uZhHh7pm}Crafted {CURR_TEMPLATE_NAME}", null);
			textObject.SetTextVariable("CURR_TEMPLATE_NAME", this._crafting.CurrentCraftingTemplate.TemplateName);
			this._crafting.SetCraftedWeaponName(textObject);
		}

		// Token: 0x0600181C RID: 6172 RVA: 0x0005CACD File Offset: 0x0005ACCD
		private bool DoesCurrentItemHaveSecondaryUsage(int usageIndex)
		{
			return usageIndex >= 0 && usageIndex < this._crafting.GetCurrentCraftedItemObject(false, null).Weapons.Count;
		}

		// Token: 0x0600181D RID: 6173 RVA: 0x0005CAF0 File Offset: 0x0005ACF0
		private void TrySetSecondaryUsageIndex(int usageIndex)
		{
			int num = 0;
			if (this.DoesCurrentItemHaveSecondaryUsage(usageIndex))
			{
				CraftingSecondaryUsageItemVM craftingSecondaryUsageItemVM = this.SecondaryUsageSelector.ItemList.FirstOrDefault((CraftingSecondaryUsageItemVM x) => x.UsageIndex == usageIndex);
				if (craftingSecondaryUsageItemVM != null)
				{
					num = craftingSecondaryUsageItemVM.SelectorIndex;
				}
			}
			if (num >= 0 && num < this.SecondaryUsageSelector.ItemList.Count)
			{
				this.SecondaryUsageSelector.SelectedIndex = num;
				this.SecondaryUsageSelector.ItemList[num].IsSelected = true;
			}
		}

		// Token: 0x0600181E RID: 6174 RVA: 0x0005CB7C File Offset: 0x0005AD7C
		private void RefreshAlternativeUsageList()
		{
			int usageIndex = this.SecondaryUsageSelector.SelectedIndex;
			this.SecondaryUsageSelector.Refresh(new List<string>(), 0, new Action<SelectorVM<CraftingSecondaryUsageItemVM>>(this.UpdateSecondaryUsageIndex));
			MBReadOnlyList<WeaponComponentData> weapons = this._crafting.GetCurrentCraftedItemObject(false, null).Weapons;
			int num = 0;
			for (int i = 0; i < weapons.Count; i++)
			{
				if (CampaignUIHelper.IsItemUsageApplicable(weapons[i]))
				{
					TextObject name = GameTexts.FindText("str_weapon_usage", weapons[i].WeaponDescriptionId);
					this.SecondaryUsageSelector.AddItem(new CraftingSecondaryUsageItemVM(name, num, i, this.SecondaryUsageSelector));
					CraftingOrderItemVM activeCraftingOrder = this.ActiveCraftingOrder;
					if (((activeCraftingOrder != null) ? activeCraftingOrder.CraftingOrder.GetStatWeapon().WeaponDescriptionId : null) == weapons[i].WeaponDescriptionId)
					{
						usageIndex = num;
					}
					num++;
				}
			}
			this.TrySetSecondaryUsageIndex(usageIndex);
		}

		// Token: 0x0600181F RID: 6175 RVA: 0x0005CC58 File Offset: 0x0005AE58
		private void RefreshStats()
		{
			if (!this.DoesCurrentItemHaveSecondaryUsage(this.SecondaryUsageSelector.SelectedIndex))
			{
				this.TrySetSecondaryUsageIndex(0);
			}
			List<CraftingStatData> list = this._crafting.GetStatDatas(this.SecondaryUsageSelector.SelectedIndex).ToList<CraftingStatData>();
			WeaponComponentData weaponComponentData = (this.IsInOrderMode ? this.ActiveCraftingOrder.CraftingOrder.GetStatWeapon() : null);
			IEnumerable<CraftingStatData> enumerable = (this.IsInOrderMode ? this.GetOrderStatDatas(this.ActiveCraftingOrder.CraftingOrder) : null);
			ItemObject currentCraftedItemObject = this._crafting.GetCurrentCraftedItemObject(false, null);
			WeaponComponentData weaponWithUsageIndex = currentCraftedItemObject.GetWeaponWithUsageIndex(this.SecondaryUsageSelector.SelectedIndex);
			bool flag = weaponComponentData == null || weaponComponentData.WeaponDescriptionId == weaponWithUsageIndex.WeaponDescriptionId;
			if (enumerable != null)
			{
				using (IEnumerator<CraftingStatData> enumerator = enumerable.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						CraftingStatData orderStatData = enumerator.Current;
						if (!list.Any((CraftingStatData x) => x.Type == orderStatData.Type && x.DamageType == orderStatData.DamageType))
						{
							if ((orderStatData.Type == CraftingTemplate.CraftingStatTypes.SwingDamage && orderStatData.DamageType != weaponWithUsageIndex.SwingDamageType) || (orderStatData.Type == CraftingTemplate.CraftingStatTypes.ThrustDamage && orderStatData.DamageType != weaponWithUsageIndex.ThrustDamageType))
							{
								list.Add(new CraftingStatData(orderStatData.DescriptionText, 0f, orderStatData.MaxValue, orderStatData.Type, orderStatData.DamageType));
							}
							else
							{
								list.Add(orderStatData);
							}
						}
					}
				}
			}
			this.PrimaryPropertyList.Clear();
			using (List<CraftingStatData>.Enumerator enumerator2 = list.GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					CraftingStatData statData = enumerator2.Current;
					if (statData.IsValid)
					{
						float num = 0f;
						if (this.IsInOrderMode && flag)
						{
							WeaponAttributeVM weaponAttributeVM = this.ActiveCraftingOrder.WeaponAttributes.FirstOrDefault((WeaponAttributeVM x) => x.AttributeType == statData.Type && x.DamageType == statData.DamageType);
							num = ((weaponAttributeVM != null) ? weaponAttributeVM.AttributeValue : 0f);
						}
						float maxValue = MathF.Max(statData.MaxValue, num);
						CraftingListPropertyItem craftingListPropertyItem = new CraftingListPropertyItem(statData.DescriptionText, maxValue, statData.CurValue, num, statData.Type, false);
						this.PrimaryPropertyList.Add(craftingListPropertyItem);
						craftingListPropertyItem.IsValidForUsage = true;
					}
				}
			}
			this.PrimaryPropertyList.Sort(new WeaponDesignVM.WeaponPropertyComparer());
			CraftingOrderItemVM activeCraftingOrder = this.ActiveCraftingOrder;
			this.MissingPropertyWarningText = CampaignUIHelper.GetCraftingOrderMissingPropertyWarningText((activeCraftingOrder != null) ? activeCraftingOrder.CraftingOrder : null, currentCraftedItemObject);
		}

		// Token: 0x06001820 RID: 6176 RVA: 0x0005CF40 File Offset: 0x0005B140
		private IEnumerable<CraftingStatData> GetOrderStatDatas(CraftingOrder order)
		{
			if (order == null)
			{
				return null;
			}
			WeaponComponentData weaponComponentData;
			return order.GetStatDataForItem(order.PreCraftedWeaponDesignItem, out weaponComponentData);
		}

		// Token: 0x06001821 RID: 6177 RVA: 0x0005CF60 File Offset: 0x0005B160
		private void RefreshWeaponFlags()
		{
			this.WeaponFlagIconsList.Clear();
			foreach (CraftingPieceListVM craftingPieceListVM in this.PieceLists)
			{
				if (craftingPieceListVM.SelectedPiece != null)
				{
					using (IEnumerator<CraftingItemFlagVM> enumerator2 = craftingPieceListVM.SelectedPiece.ItemAttributeIcons.GetEnumerator())
					{
						while (enumerator2.MoveNext())
						{
							CraftingItemFlagVM iconData = enumerator2.Current;
							if (!this.WeaponFlagIconsList.Any((ItemFlagVM x) => x.Icon == iconData.Icon))
							{
								this.WeaponFlagIconsList.Add(iconData);
							}
						}
					}
				}
			}
		}

		// Token: 0x06001822 RID: 6178 RVA: 0x0005D028 File Offset: 0x0005B228
		private void OnSetItemPieceManually(CraftingPieceVM piece)
		{
			this.OnSetItemPiece(piece, 0, true, false);
			this.RefreshItem();
			this.AddHistoryKey();
		}

		// Token: 0x06001823 RID: 6179 RVA: 0x0005D040 File Offset: 0x0005B240
		private void OnSetItemPiece(CraftingPieceVM piece, int scalePercentage = 0, bool shouldUpdateWholeWeapon = true, bool forceUpdatePiece = false)
		{
			CraftingPiece.PieceTypes pieceType = (CraftingPiece.PieceTypes)piece.PieceType;
			this._pieceListsDictionary[pieceType].SelectedPiece.IsSelected = false;
			bool updatePiece = this._updatePiece;
			if (!this._isAutoSelectingPieces)
			{
				this.UnsetPieceNewlyUnlocked(piece);
			}
			if (updatePiece)
			{
				this._crafting.SwitchToPiece(piece.CraftingPiece);
				if (!forceUpdatePiece)
				{
					this._updatePiece = false;
				}
			}
			piece.IsSelected = true;
			this._pieceListsDictionary[pieceType].SelectedPiece = piece;
			int num = ((scalePercentage != 0) ? scalePercentage : this._crafting.SelectedPieces[(int)pieceType].ScalePercentage) - 100;
			switch (pieceType)
			{
			case CraftingPiece.PieceTypes.Blade:
				this.BladeSize = num;
				this.SelectedBladePiece = piece;
				break;
			case CraftingPiece.PieceTypes.Guard:
				this.GuardSize = num;
				this.SelectedGuardPiece = piece;
				break;
			case CraftingPiece.PieceTypes.Handle:
				this.HandleSize = num;
				this.SelectedHandlePiece = piece;
				break;
			case CraftingPiece.PieceTypes.Pommel:
				this.PommelSize = num;
				this.SelectedPommelPiece = piece;
				break;
			}
			if (this.IsInFreeMode)
			{
				WeaponClassVM currentWeaponClass = this.GetCurrentWeaponClass();
				if (currentWeaponClass != null)
				{
					currentWeaponClass.RegisterSelectedPiece(pieceType, piece.CraftingPiece.CraftingPiece.StringId);
				}
			}
			this._updatePiece = updatePiece;
			this.RefreshAlternativeUsageList();
			if (shouldUpdateWholeWeapon)
			{
				Action onRefresh = this._onRefresh;
				if (onRefresh != null)
				{
					onRefresh();
				}
			}
			this.PieceLists.ApplyActionOnAllItems(delegate(CraftingPieceListVM x)
			{
				x.Refresh();
			});
		}

		// Token: 0x06001824 RID: 6180 RVA: 0x0005D19F File Offset: 0x0005B39F
		public void RefreshItem()
		{
			this.RefreshStats();
			this.RefreshWeaponFlags();
			this.RefreshDifficulty();
			Action onRefresh = this._onRefresh;
			if (onRefresh == null)
			{
				return;
			}
			onRefresh();
		}

		// Token: 0x06001825 RID: 6181 RVA: 0x0005D1C4 File Offset: 0x0005B3C4
		private void RefreshDifficulty()
		{
			this.CurrentDifficulty = Campaign.Current.Models.SmithingModel.CalculateWeaponDesignDifficulty(this._crafting.CurrentWeaponDesign);
			if (this.IsInOrderMode)
			{
				this.CurrentOrderDifficulty = MathF.Round(this.ActiveCraftingOrder.CraftingOrder.OrderDifficulty);
			}
			this._currentCraftingSkillText.SetTextVariable("SKILL_VALUE", this.CurrentHeroCraftingSkill);
			this._currentCraftingSkillText.SetTextVariable("SKILL_NAME", DefaultSkills.Crafting.Name);
			this.CurrentCraftingSkillValueText = this._currentCraftingSkillText.ToString();
			this.CurrentDifficultyText = this.GetCurrentDifficultyText(this.CurrentHeroCraftingSkill, this.CurrentDifficulty);
			this.CurrentOrderDifficultyText = this.GetCurrentOrderDifficultyText(this.CurrentOrderDifficulty);
		}

		// Token: 0x06001826 RID: 6182 RVA: 0x0005D286 File Offset: 0x0005B486
		private string GetCurrentDifficultyText(int skillValue, int difficultyValue)
		{
			this._difficultyTextobj.SetTextVariable("DIFFICULTY", difficultyValue);
			return this._difficultyTextobj.ToString();
		}

		// Token: 0x06001827 RID: 6183 RVA: 0x0005D2A5 File Offset: 0x0005B4A5
		private string GetCurrentOrderDifficultyText(int orderDifficulty)
		{
			this._orderDifficultyTextObj.SetTextVariable("DIFFICULTY", orderDifficulty.ToString());
			return this._orderDifficultyTextObj.ToString();
		}

		// Token: 0x06001828 RID: 6184 RVA: 0x0005D2CC File Offset: 0x0005B4CC
		private void RefreshCurrentHeroSkillLevel()
		{
			Func<CraftingAvailableHeroItemVM> getCurrentCraftingHero = this._getCurrentCraftingHero;
			int? num;
			if (getCurrentCraftingHero == null)
			{
				num = null;
			}
			else
			{
				CraftingAvailableHeroItemVM craftingAvailableHeroItemVM = getCurrentCraftingHero();
				num = ((craftingAvailableHeroItemVM != null) ? new int?(craftingAvailableHeroItemVM.Hero.CharacterObject.GetSkillValue(DefaultSkills.Crafting)) : null);
			}
			this.CurrentHeroCraftingSkill = num ?? 0;
			this.IsCurrentHeroAtMaxCraftingSkill = this.CurrentHeroCraftingSkill >= 300;
			this._currentCraftingSkillText.SetTextVariable("SKILL_VALUE", this.CurrentHeroCraftingSkill);
			this.CurrentCraftingSkillValueText = this._currentCraftingSkillText.ToString();
			this.CurrentDifficultyText = this.GetCurrentDifficultyText(this.CurrentHeroCraftingSkill, this.CurrentDifficulty);
		}

		// Token: 0x06001829 RID: 6185 RVA: 0x0005D38C File Offset: 0x0005B58C
		public bool HaveUnlockedAllSelectedPieces()
		{
			foreach (CraftingPieceListVM craftingPieceListVM in this.PieceLists)
			{
				if (craftingPieceListVM.IsEnabled)
				{
					CraftingPieceVM selectedPiece = craftingPieceListVM.SelectedPiece;
					if (((selectedPiece != null) ? selectedPiece.CraftingPiece : null) != null && !craftingPieceListVM.SelectedPiece.PlayerHasPiece)
					{
						return false;
					}
				}
			}
			return true;
		}

		// Token: 0x0600182A RID: 6186 RVA: 0x0005D404 File Offset: 0x0005B604
		private void AddHistoryKey()
		{
			if (this._shouldRecordHistory)
			{
				this._crafting.UpdateHistory();
			}
		}

		// Token: 0x0600182B RID: 6187 RVA: 0x0005D41C File Offset: 0x0005B61C
		public void SwitchToPiece(WeaponDesignElement usedPiece)
		{
			CraftingPieceVM piece = this._pieceListsDictionary[usedPiece.CraftingPiece.PieceType].Pieces.FirstOrDefault((CraftingPieceVM p) => p.CraftingPiece.CraftingPiece == usedPiece.CraftingPiece);
			this.OnSetItemPiece(piece, usedPiece.ScalePercentage, true, false);
		}

		// Token: 0x0600182C RID: 6188 RVA: 0x0005D47C File Offset: 0x0005B67C
		internal void SetDesignManually(CraftingTemplate craftingTemplate, ValueTuple<CraftingPiece, int>[] pieces, bool forceChangeTemplate = false)
		{
			int num = this._primaryUsages.IndexOf(craftingTemplate);
			if ((this.IsInFreeMode && forceChangeTemplate) || num == this._selectedWeaponClassIndex)
			{
				this.RefreshWeaponDesignMode(this.ActiveCraftingOrder, this._primaryUsages.IndexOf(craftingTemplate), true);
				for (int i = 0; i < pieces.Length; i++)
				{
					ValueTuple<CraftingPiece, int> currentPiece = pieces[i];
					if (currentPiece.Item1 != null)
					{
						CraftingPieceVM craftingPieceVM = this._pieceListsDictionary[currentPiece.Item1.PieceType].Pieces.FirstOrDefault((CraftingPieceVM piece) => piece.CraftingPiece.CraftingPiece == currentPiece.Item1);
						if (craftingPieceVM != null)
						{
							this.OnSetItemPiece(craftingPieceVM, currentPiece.Item2, true, false);
							this._crafting.ScaleThePiece(currentPiece.Item1.PieceType, currentPiece.Item2);
						}
					}
				}
				this.RefreshDifficulty();
				Action onRefresh = this._onRefresh;
				if (onRefresh == null)
				{
					return;
				}
				onRefresh();
			}
		}

		// Token: 0x17000805 RID: 2053
		// (get) Token: 0x0600182D RID: 6189 RVA: 0x0005D57E File Offset: 0x0005B77E
		// (set) Token: 0x0600182E RID: 6190 RVA: 0x0005D586 File Offset: 0x0005B786
		[DataSourceProperty]
		public MBBindingList<TierFilterTypeVM> TierFilters
		{
			get
			{
				return this._tierFilters;
			}
			set
			{
				if (value != this._tierFilters)
				{
					this._tierFilters = value;
					base.OnPropertyChangedWithValue<MBBindingList<TierFilterTypeVM>>(value, "TierFilters");
				}
			}
		}

		// Token: 0x17000806 RID: 2054
		// (get) Token: 0x0600182F RID: 6191 RVA: 0x0005D5A4 File Offset: 0x0005B7A4
		// (set) Token: 0x06001830 RID: 6192 RVA: 0x0005D5AC File Offset: 0x0005B7AC
		[DataSourceProperty]
		public string CurrentCraftedWeaponTemplateId
		{
			get
			{
				return this._currentCraftedWeaponTemplateId;
			}
			set
			{
				if (value != this._currentCraftedWeaponTemplateId)
				{
					this._currentCraftedWeaponTemplateId = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentCraftedWeaponTemplateId");
				}
			}
		}

		// Token: 0x17000807 RID: 2055
		// (get) Token: 0x06001831 RID: 6193 RVA: 0x0005D5CF File Offset: 0x0005B7CF
		// (set) Token: 0x06001832 RID: 6194 RVA: 0x0005D5D7 File Offset: 0x0005B7D7
		[DataSourceProperty]
		public string ChooseOrderText
		{
			get
			{
				return this._chooseOrderText;
			}
			set
			{
				if (value != this._chooseOrderText)
				{
					this._chooseOrderText = value;
					base.OnPropertyChangedWithValue<string>(value, "ChooseOrderText");
				}
			}
		}

		// Token: 0x17000808 RID: 2056
		// (get) Token: 0x06001833 RID: 6195 RVA: 0x0005D5FA File Offset: 0x0005B7FA
		// (set) Token: 0x06001834 RID: 6196 RVA: 0x0005D602 File Offset: 0x0005B802
		[DataSourceProperty]
		public string ChooseWeaponTypeText
		{
			get
			{
				return this._chooseWeaponTypeText;
			}
			set
			{
				if (value != this._chooseWeaponTypeText)
				{
					this._chooseWeaponTypeText = value;
					base.OnPropertyChangedWithValue<string>(value, "ChooseWeaponTypeText");
				}
			}
		}

		// Token: 0x17000809 RID: 2057
		// (get) Token: 0x06001835 RID: 6197 RVA: 0x0005D625 File Offset: 0x0005B825
		// (set) Token: 0x06001836 RID: 6198 RVA: 0x0005D62D File Offset: 0x0005B82D
		[DataSourceProperty]
		public string CurrentCraftedWeaponTypeText
		{
			get
			{
				return this._currentCraftedWeaponTypeText;
			}
			set
			{
				if (value != this._currentCraftedWeaponTypeText)
				{
					this._currentCraftedWeaponTypeText = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentCraftedWeaponTypeText");
				}
			}
		}

		// Token: 0x1700080A RID: 2058
		// (get) Token: 0x06001837 RID: 6199 RVA: 0x0005D650 File Offset: 0x0005B850
		// (set) Token: 0x06001838 RID: 6200 RVA: 0x0005D658 File Offset: 0x0005B858
		[DataSourceProperty]
		public MBBindingList<CraftingPieceListVM> PieceLists
		{
			get
			{
				return this._pieceLists;
			}
			set
			{
				if (value != this._pieceLists)
				{
					this._pieceLists = value;
					base.OnPropertyChangedWithValue<MBBindingList<CraftingPieceListVM>>(value, "PieceLists");
				}
			}
		}

		// Token: 0x1700080B RID: 2059
		// (get) Token: 0x06001839 RID: 6201 RVA: 0x0005D676 File Offset: 0x0005B876
		// (set) Token: 0x0600183A RID: 6202 RVA: 0x0005D67E File Offset: 0x0005B87E
		[DataSourceProperty]
		public int SelectedPieceTypeIndex
		{
			get
			{
				return this._selectedPieceTypeIndex;
			}
			set
			{
				if (value != this._selectedPieceTypeIndex)
				{
					this._selectedPieceTypeIndex = value;
					base.OnPropertyChangedWithValue(value, "SelectedPieceTypeIndex");
				}
			}
		}

		// Token: 0x1700080C RID: 2060
		// (get) Token: 0x0600183B RID: 6203 RVA: 0x0005D69C File Offset: 0x0005B89C
		// (set) Token: 0x0600183C RID: 6204 RVA: 0x0005D6A4 File Offset: 0x0005B8A4
		[DataSourceProperty]
		public bool ShowOnlyUnlockedPieces
		{
			get
			{
				return this._showOnlyUnlockedPieces;
			}
			set
			{
				if (value != this._showOnlyUnlockedPieces)
				{
					this._showOnlyUnlockedPieces = value;
					base.OnPropertyChangedWithValue(value, "ShowOnlyUnlockedPieces");
				}
			}
		}

		// Token: 0x1700080D RID: 2061
		// (get) Token: 0x0600183D RID: 6205 RVA: 0x0005D6C2 File Offset: 0x0005B8C2
		// (set) Token: 0x0600183E RID: 6206 RVA: 0x0005D6CA File Offset: 0x0005B8CA
		[DataSourceProperty]
		public string MissingPropertyWarningText
		{
			get
			{
				return this._missingPropertyWarningText;
			}
			set
			{
				if (value != this._missingPropertyWarningText)
				{
					this._missingPropertyWarningText = value;
					base.OnPropertyChangedWithValue<string>(value, "MissingPropertyWarningText");
				}
			}
		}

		// Token: 0x1700080E RID: 2062
		// (get) Token: 0x0600183F RID: 6207 RVA: 0x0005D6ED File Offset: 0x0005B8ED
		// (set) Token: 0x06001840 RID: 6208 RVA: 0x0005D6F5 File Offset: 0x0005B8F5
		[DataSourceProperty]
		public WeaponDesignResultPopupVM CraftingResultPopup
		{
			get
			{
				return this._craftingResultPopup;
			}
			set
			{
				if (value != this._craftingResultPopup)
				{
					this._craftingResultPopup = value;
					base.OnPropertyChangedWithValue<WeaponDesignResultPopupVM>(value, "CraftingResultPopup");
				}
			}
		}

		// Token: 0x1700080F RID: 2063
		// (get) Token: 0x06001841 RID: 6209 RVA: 0x0005D713 File Offset: 0x0005B913
		// (set) Token: 0x06001842 RID: 6210 RVA: 0x0005D71B File Offset: 0x0005B91B
		[DataSourceProperty]
		public bool IsOrderButtonActive
		{
			get
			{
				return this._isOrderButtonActive;
			}
			set
			{
				if (value != this._isOrderButtonActive)
				{
					this._isOrderButtonActive = value;
					base.OnPropertyChangedWithValue(value, "IsOrderButtonActive");
				}
			}
		}

		// Token: 0x17000810 RID: 2064
		// (get) Token: 0x06001843 RID: 6211 RVA: 0x0005D739 File Offset: 0x0005B939
		// (set) Token: 0x06001844 RID: 6212 RVA: 0x0005D741 File Offset: 0x0005B941
		[DataSourceProperty]
		public bool IsInOrderMode
		{
			get
			{
				return this._isInOrderMode;
			}
			set
			{
				if (value != this._isInOrderMode)
				{
					this._isInOrderMode = value;
					base.OnPropertyChangedWithValue(value, "IsInOrderMode");
					base.OnPropertyChanged("IsInFreeMode");
				}
			}
		}

		// Token: 0x17000811 RID: 2065
		// (get) Token: 0x06001845 RID: 6213 RVA: 0x0005D76A File Offset: 0x0005B96A
		// (set) Token: 0x06001846 RID: 6214 RVA: 0x0005D775 File Offset: 0x0005B975
		[DataSourceProperty]
		public bool IsInFreeMode
		{
			get
			{
				return !this._isInOrderMode;
			}
			set
			{
				if (value != this.IsInFreeMode)
				{
					this._isInOrderMode = !value;
					base.OnPropertyChangedWithValue(value, "IsInFreeMode");
					base.OnPropertyChanged("IsInOrderMode");
				}
			}
		}

		// Token: 0x17000812 RID: 2066
		// (get) Token: 0x06001847 RID: 6215 RVA: 0x0005D7A1 File Offset: 0x0005B9A1
		// (set) Token: 0x06001848 RID: 6216 RVA: 0x0005D7A9 File Offset: 0x0005B9A9
		[DataSourceProperty]
		public string FreeModeButtonText
		{
			get
			{
				return this._freeModeButtonText;
			}
			set
			{
				if (value != this._freeModeButtonText)
				{
					this._freeModeButtonText = value;
					base.OnPropertyChangedWithValue<string>(value, "FreeModeButtonText");
				}
			}
		}

		// Token: 0x17000813 RID: 2067
		// (get) Token: 0x06001849 RID: 6217 RVA: 0x0005D7CC File Offset: 0x0005B9CC
		// (set) Token: 0x0600184A RID: 6218 RVA: 0x0005D7D4 File Offset: 0x0005B9D4
		[DataSourceProperty]
		public CraftingOrderItemVM ActiveCraftingOrder
		{
			get
			{
				return this._activeCraftingOrder;
			}
			set
			{
				if (value != this._activeCraftingOrder)
				{
					this._activeCraftingOrder = value;
					base.OnPropertyChangedWithValue<CraftingOrderItemVM>(value, "ActiveCraftingOrder");
				}
			}
		}

		// Token: 0x17000814 RID: 2068
		// (get) Token: 0x0600184B RID: 6219 RVA: 0x0005D7F2 File Offset: 0x0005B9F2
		// (set) Token: 0x0600184C RID: 6220 RVA: 0x0005D7FA File Offset: 0x0005B9FA
		[DataSourceProperty]
		public CraftingOrderPopupVM CraftingOrderPopup
		{
			get
			{
				return this._craftingOrderPopup;
			}
			set
			{
				if (value != this._craftingOrderPopup)
				{
					this._craftingOrderPopup = value;
					base.OnPropertyChangedWithValue<CraftingOrderPopupVM>(value, "CraftingOrderPopup");
				}
			}
		}

		// Token: 0x17000815 RID: 2069
		// (get) Token: 0x0600184D RID: 6221 RVA: 0x0005D818 File Offset: 0x0005BA18
		// (set) Token: 0x0600184E RID: 6222 RVA: 0x0005D820 File Offset: 0x0005BA20
		[DataSourceProperty]
		public WeaponClassSelectionPopupVM WeaponClassSelectionPopup
		{
			get
			{
				return this._weaponClassSelectionPopup;
			}
			set
			{
				if (value != this._weaponClassSelectionPopup)
				{
					this._weaponClassSelectionPopup = value;
					base.OnPropertyChangedWithValue<WeaponClassSelectionPopupVM>(value, "WeaponClassSelectionPopup");
				}
			}
		}

		// Token: 0x17000816 RID: 2070
		// (get) Token: 0x0600184F RID: 6223 RVA: 0x0005D83E File Offset: 0x0005BA3E
		// (set) Token: 0x06001850 RID: 6224 RVA: 0x0005D846 File Offset: 0x0005BA46
		[DataSourceProperty]
		public MBBindingList<CraftingListPropertyItem> PrimaryPropertyList
		{
			get
			{
				return this._primaryPropertyList;
			}
			set
			{
				if (value != this._primaryPropertyList)
				{
					this._primaryPropertyList = value;
					base.OnPropertyChangedWithValue<MBBindingList<CraftingListPropertyItem>>(value, "PrimaryPropertyList");
				}
			}
		}

		// Token: 0x17000817 RID: 2071
		// (get) Token: 0x06001851 RID: 6225 RVA: 0x0005D864 File Offset: 0x0005BA64
		// (set) Token: 0x06001852 RID: 6226 RVA: 0x0005D86C File Offset: 0x0005BA6C
		[DataSourceProperty]
		public MBBindingList<WeaponDesignResultPropertyItemVM> DesignResultPropertyList
		{
			get
			{
				return this._designResultPropertyList;
			}
			set
			{
				if (value != this._designResultPropertyList)
				{
					this._designResultPropertyList = value;
					base.OnPropertyChangedWithValue<MBBindingList<WeaponDesignResultPropertyItemVM>>(value, "DesignResultPropertyList");
				}
			}
		}

		// Token: 0x17000818 RID: 2072
		// (get) Token: 0x06001853 RID: 6227 RVA: 0x0005D88A File Offset: 0x0005BA8A
		// (set) Token: 0x06001854 RID: 6228 RVA: 0x0005D892 File Offset: 0x0005BA92
		[DataSourceProperty]
		public SelectorVM<CraftingSecondaryUsageItemVM> SecondaryUsageSelector
		{
			get
			{
				return this._secondaryUsageSelector;
			}
			set
			{
				if (value != this._secondaryUsageSelector)
				{
					this._secondaryUsageSelector = value;
					base.OnPropertyChangedWithValue<SelectorVM<CraftingSecondaryUsageItemVM>>(value, "SecondaryUsageSelector");
				}
			}
		}

		// Token: 0x17000819 RID: 2073
		// (get) Token: 0x06001855 RID: 6229 RVA: 0x0005D8B0 File Offset: 0x0005BAB0
		// (set) Token: 0x06001856 RID: 6230 RVA: 0x0005D8B8 File Offset: 0x0005BAB8
		[DataSourceProperty]
		public ItemCollectionElementViewModel CraftedItemVisual
		{
			get
			{
				return this._craftedItemVisual;
			}
			set
			{
				if (value != this._craftedItemVisual)
				{
					this._craftedItemVisual = value;
					base.OnPropertyChangedWithValue<ItemCollectionElementViewModel>(value, "CraftedItemVisual");
				}
			}
		}

		// Token: 0x1700081A RID: 2074
		// (get) Token: 0x06001857 RID: 6231 RVA: 0x0005D8D6 File Offset: 0x0005BAD6
		// (set) Token: 0x06001858 RID: 6232 RVA: 0x0005D8DE File Offset: 0x0005BADE
		[DataSourceProperty]
		public bool IsInFinalCraftingStage
		{
			get
			{
				return this._isInFinalCraftingStage;
			}
			set
			{
				if (value != this._isInFinalCraftingStage)
				{
					this._isInFinalCraftingStage = value;
					base.OnPropertyChangedWithValue(value, "IsInFinalCraftingStage");
				}
			}
		}

		// Token: 0x1700081B RID: 2075
		// (get) Token: 0x06001859 RID: 6233 RVA: 0x0005D8FC File Offset: 0x0005BAFC
		// (set) Token: 0x0600185A RID: 6234 RVA: 0x0005D904 File Offset: 0x0005BB04
		[DataSourceProperty]
		public string ItemName
		{
			get
			{
				return this._itemName;
			}
			set
			{
				if (value != this._itemName)
				{
					this._itemName = value;
					base.OnPropertyChangedWithValue<string>(value, "ItemName");
				}
			}
		}

		// Token: 0x1700081C RID: 2076
		// (get) Token: 0x0600185B RID: 6235 RVA: 0x0005D927 File Offset: 0x0005BB27
		// (set) Token: 0x0600185C RID: 6236 RVA: 0x0005D92F File Offset: 0x0005BB2F
		[DataSourceProperty]
		public bool IsScabbardVisible
		{
			get
			{
				return this._isScabbardVisible;
			}
			set
			{
				if (value != this._isScabbardVisible)
				{
					this._isScabbardVisible = value;
					base.OnPropertyChangedWithValue(value, "IsScabbardVisible");
					this._crafting.ReIndex(false);
					Action onRefresh = this._onRefresh;
					if (onRefresh == null)
					{
						return;
					}
					onRefresh();
				}
			}
		}

		// Token: 0x1700081D RID: 2077
		// (get) Token: 0x0600185D RID: 6237 RVA: 0x0005D969 File Offset: 0x0005BB69
		// (set) Token: 0x0600185E RID: 6238 RVA: 0x0005D971 File Offset: 0x0005BB71
		[DataSourceProperty]
		public bool CurrentWeaponHasScabbard
		{
			get
			{
				return this._currentWeaponHasScabbard;
			}
			set
			{
				if (value != this._currentWeaponHasScabbard)
				{
					this._currentWeaponHasScabbard = value;
					base.OnPropertyChangedWithValue(value, "CurrentWeaponHasScabbard");
				}
			}
		}

		// Token: 0x1700081E RID: 2078
		// (get) Token: 0x0600185F RID: 6239 RVA: 0x0005D98F File Offset: 0x0005BB8F
		// (set) Token: 0x06001860 RID: 6240 RVA: 0x0005D997 File Offset: 0x0005BB97
		[DataSourceProperty]
		public int CurrentDifficulty
		{
			get
			{
				return this._currentDifficulty;
			}
			set
			{
				if (value != this._currentDifficulty)
				{
					this._currentDifficulty = value;
					base.OnPropertyChangedWithValue(value, "CurrentDifficulty");
				}
			}
		}

		// Token: 0x1700081F RID: 2079
		// (get) Token: 0x06001861 RID: 6241 RVA: 0x0005D9B5 File Offset: 0x0005BBB5
		// (set) Token: 0x06001862 RID: 6242 RVA: 0x0005D9BD File Offset: 0x0005BBBD
		[DataSourceProperty]
		public int CurrentOrderDifficulty
		{
			get
			{
				return this._currentOrderDifficulty;
			}
			set
			{
				if (value != this._currentOrderDifficulty)
				{
					this._currentOrderDifficulty = value;
					base.OnPropertyChangedWithValue(value, "CurrentOrderDifficulty");
				}
			}
		}

		// Token: 0x17000820 RID: 2080
		// (get) Token: 0x06001863 RID: 6243 RVA: 0x0005D9DB File Offset: 0x0005BBDB
		// (set) Token: 0x06001864 RID: 6244 RVA: 0x0005D9E3 File Offset: 0x0005BBE3
		[DataSourceProperty]
		public int MaxDifficulty
		{
			get
			{
				return this._maxDifficulty;
			}
			set
			{
				if (value != this._maxDifficulty)
				{
					this._maxDifficulty = value;
					base.OnPropertyChangedWithValue(value, "MaxDifficulty");
				}
			}
		}

		// Token: 0x17000821 RID: 2081
		// (get) Token: 0x06001865 RID: 6245 RVA: 0x0005DA01 File Offset: 0x0005BC01
		// (set) Token: 0x06001866 RID: 6246 RVA: 0x0005DA09 File Offset: 0x0005BC09
		[DataSourceProperty]
		public bool IsCurrentHeroAtMaxCraftingSkill
		{
			get
			{
				return this._isCurrentHeroAtMaxCraftingSkill;
			}
			set
			{
				if (value != this._isCurrentHeroAtMaxCraftingSkill)
				{
					this._isCurrentHeroAtMaxCraftingSkill = value;
					base.OnPropertyChangedWithValue(value, "IsCurrentHeroAtMaxCraftingSkill");
				}
			}
		}

		// Token: 0x17000822 RID: 2082
		// (get) Token: 0x06001867 RID: 6247 RVA: 0x0005DA27 File Offset: 0x0005BC27
		// (set) Token: 0x06001868 RID: 6248 RVA: 0x0005DA2F File Offset: 0x0005BC2F
		[DataSourceProperty]
		public int CurrentHeroCraftingSkill
		{
			get
			{
				return this._currentHeroCraftingSkill;
			}
			set
			{
				if (value != this._currentHeroCraftingSkill)
				{
					this._currentHeroCraftingSkill = value;
					base.OnPropertyChangedWithValue(value, "CurrentHeroCraftingSkill");
				}
			}
		}

		// Token: 0x17000823 RID: 2083
		// (get) Token: 0x06001869 RID: 6249 RVA: 0x0005DA4D File Offset: 0x0005BC4D
		// (set) Token: 0x0600186A RID: 6250 RVA: 0x0005DA55 File Offset: 0x0005BC55
		[DataSourceProperty]
		public string CurrentDifficultyText
		{
			get
			{
				return this._currentDifficultyText;
			}
			set
			{
				if (value != this._currentDifficultyText)
				{
					this._currentDifficultyText = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentDifficultyText");
				}
			}
		}

		// Token: 0x17000824 RID: 2084
		// (get) Token: 0x0600186B RID: 6251 RVA: 0x0005DA78 File Offset: 0x0005BC78
		// (set) Token: 0x0600186C RID: 6252 RVA: 0x0005DA80 File Offset: 0x0005BC80
		[DataSourceProperty]
		public string CurrentOrderDifficultyText
		{
			get
			{
				return this._currentOrderDifficultyText;
			}
			set
			{
				if (value != this._currentOrderDifficultyText)
				{
					this._currentOrderDifficultyText = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentOrderDifficultyText");
				}
			}
		}

		// Token: 0x17000825 RID: 2085
		// (get) Token: 0x0600186D RID: 6253 RVA: 0x0005DAA3 File Offset: 0x0005BCA3
		// (set) Token: 0x0600186E RID: 6254 RVA: 0x0005DAAB File Offset: 0x0005BCAB
		[DataSourceProperty]
		public string CurrentCraftingSkillValueText
		{
			get
			{
				return this._currentCraftingSkillValueText;
			}
			set
			{
				if (value != this._currentCraftingSkillValueText)
				{
					this._currentCraftingSkillValueText = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentCraftingSkillValueText");
				}
			}
		}

		// Token: 0x17000826 RID: 2086
		// (get) Token: 0x0600186F RID: 6255 RVA: 0x0005DACE File Offset: 0x0005BCCE
		// (set) Token: 0x06001870 RID: 6256 RVA: 0x0005DAD6 File Offset: 0x0005BCD6
		[DataSourceProperty]
		public string DifficultyText
		{
			get
			{
				return this._difficultyText;
			}
			set
			{
				if (value != this._difficultyText)
				{
					this._difficultyText = value;
					base.OnPropertyChangedWithValue<string>(value, "DifficultyText");
				}
			}
		}

		// Token: 0x17000827 RID: 2087
		// (get) Token: 0x06001871 RID: 6257 RVA: 0x0005DAF9 File Offset: 0x0005BCF9
		// (set) Token: 0x06001872 RID: 6258 RVA: 0x0005DB01 File Offset: 0x0005BD01
		[DataSourceProperty]
		public string DefaultUsageText
		{
			get
			{
				return this._defaultUsageText;
			}
			set
			{
				if (value != this._defaultUsageText)
				{
					this._defaultUsageText = value;
					base.OnPropertyChangedWithValue<string>(value, "DefaultUsageText");
				}
			}
		}

		// Token: 0x17000828 RID: 2088
		// (get) Token: 0x06001873 RID: 6259 RVA: 0x0005DB24 File Offset: 0x0005BD24
		// (set) Token: 0x06001874 RID: 6260 RVA: 0x0005DB2C File Offset: 0x0005BD2C
		[DataSourceProperty]
		public string AlternativeUsageText
		{
			get
			{
				return this._alternativeUsageText;
			}
			set
			{
				if (value != this._alternativeUsageText)
				{
					this._alternativeUsageText = value;
					base.OnPropertyChangedWithValue<string>(value, "AlternativeUsageText");
				}
			}
		}

		// Token: 0x17000829 RID: 2089
		// (get) Token: 0x06001875 RID: 6261 RVA: 0x0005DB4F File Offset: 0x0005BD4F
		// (set) Token: 0x06001876 RID: 6262 RVA: 0x0005DB57 File Offset: 0x0005BD57
		[DataSourceProperty]
		public BasicTooltipViewModel OrderDisabledReasonHint
		{
			get
			{
				return this._orderDisabledReasonHint;
			}
			set
			{
				if (value != this._orderDisabledReasonHint)
				{
					this._orderDisabledReasonHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "OrderDisabledReasonHint");
				}
			}
		}

		// Token: 0x1700082A RID: 2090
		// (get) Token: 0x06001877 RID: 6263 RVA: 0x0005DB75 File Offset: 0x0005BD75
		// (set) Token: 0x06001878 RID: 6264 RVA: 0x0005DB7D File Offset: 0x0005BD7D
		[DataSourceProperty]
		public HintViewModel ShowOnlyUnlockedPiecesHint
		{
			get
			{
				return this._showOnlyUnlockedPiecesHint;
			}
			set
			{
				if (value != this._showOnlyUnlockedPiecesHint)
				{
					this._showOnlyUnlockedPiecesHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ShowOnlyUnlockedPiecesHint");
				}
			}
		}

		// Token: 0x1700082B RID: 2091
		// (get) Token: 0x06001879 RID: 6265 RVA: 0x0005DB9B File Offset: 0x0005BD9B
		// (set) Token: 0x0600187A RID: 6266 RVA: 0x0005DBA3 File Offset: 0x0005BDA3
		[DataSourceProperty]
		public CraftingPieceListVM ActivePieceList
		{
			get
			{
				return this._activePieceList;
			}
			set
			{
				if (value != this._activePieceList)
				{
					this._activePieceList = value;
					base.OnPropertyChangedWithValue<CraftingPieceListVM>(value, "ActivePieceList");
				}
			}
		}

		// Token: 0x1700082C RID: 2092
		// (get) Token: 0x0600187B RID: 6267 RVA: 0x0005DBC1 File Offset: 0x0005BDC1
		// (set) Token: 0x0600187C RID: 6268 RVA: 0x0005DBC9 File Offset: 0x0005BDC9
		[DataSourceProperty]
		public CraftingPieceListVM BladePieceList
		{
			get
			{
				return this._bladePieceList;
			}
			set
			{
				if (value != this._bladePieceList)
				{
					this._bladePieceList = value;
					base.OnPropertyChangedWithValue<CraftingPieceListVM>(value, "BladePieceList");
				}
			}
		}

		// Token: 0x1700082D RID: 2093
		// (get) Token: 0x0600187D RID: 6269 RVA: 0x0005DBE7 File Offset: 0x0005BDE7
		// (set) Token: 0x0600187E RID: 6270 RVA: 0x0005DBEF File Offset: 0x0005BDEF
		[DataSourceProperty]
		public CraftingPieceListVM GuardPieceList
		{
			get
			{
				return this._guardPieceList;
			}
			set
			{
				if (value != this._guardPieceList)
				{
					this._guardPieceList = value;
					base.OnPropertyChangedWithValue<CraftingPieceListVM>(value, "GuardPieceList");
				}
			}
		}

		// Token: 0x1700082E RID: 2094
		// (get) Token: 0x0600187F RID: 6271 RVA: 0x0005DC0D File Offset: 0x0005BE0D
		// (set) Token: 0x06001880 RID: 6272 RVA: 0x0005DC15 File Offset: 0x0005BE15
		[DataSourceProperty]
		public CraftingPieceListVM HandlePieceList
		{
			get
			{
				return this._handlePieceList;
			}
			set
			{
				if (value != this._handlePieceList)
				{
					this._handlePieceList = value;
					base.OnPropertyChangedWithValue<CraftingPieceListVM>(value, "HandlePieceList");
				}
			}
		}

		// Token: 0x1700082F RID: 2095
		// (get) Token: 0x06001881 RID: 6273 RVA: 0x0005DC33 File Offset: 0x0005BE33
		// (set) Token: 0x06001882 RID: 6274 RVA: 0x0005DC3B File Offset: 0x0005BE3B
		[DataSourceProperty]
		public CraftingPieceListVM PommelPieceList
		{
			get
			{
				return this._pommelPieceList;
			}
			set
			{
				if (value != this._pommelPieceList)
				{
					this._pommelPieceList = value;
					base.OnPropertyChangedWithValue<CraftingPieceListVM>(value, "PommelPieceList");
				}
			}
		}

		// Token: 0x17000830 RID: 2096
		// (get) Token: 0x06001883 RID: 6275 RVA: 0x0005DC59 File Offset: 0x0005BE59
		// (set) Token: 0x06001884 RID: 6276 RVA: 0x0005DC61 File Offset: 0x0005BE61
		[DataSourceProperty]
		public CraftingPieceVM SelectedBladePiece
		{
			get
			{
				return this._selectedBladePiece;
			}
			set
			{
				if (value != this._selectedBladePiece)
				{
					this._selectedBladePiece = value;
					base.OnPropertyChangedWithValue<CraftingPieceVM>(value, "SelectedBladePiece");
				}
			}
		}

		// Token: 0x17000831 RID: 2097
		// (get) Token: 0x06001885 RID: 6277 RVA: 0x0005DC7F File Offset: 0x0005BE7F
		// (set) Token: 0x06001886 RID: 6278 RVA: 0x0005DC87 File Offset: 0x0005BE87
		[DataSourceProperty]
		public CraftingPieceVM SelectedGuardPiece
		{
			get
			{
				return this._selectedGuardPiece;
			}
			set
			{
				if (value != this._selectedGuardPiece)
				{
					this._selectedGuardPiece = value;
					base.OnPropertyChangedWithValue<CraftingPieceVM>(value, "SelectedGuardPiece");
				}
			}
		}

		// Token: 0x17000832 RID: 2098
		// (get) Token: 0x06001887 RID: 6279 RVA: 0x0005DCA5 File Offset: 0x0005BEA5
		// (set) Token: 0x06001888 RID: 6280 RVA: 0x0005DCAD File Offset: 0x0005BEAD
		[DataSourceProperty]
		public CraftingPieceVM SelectedHandlePiece
		{
			get
			{
				return this._selectedHandlePiece;
			}
			set
			{
				if (value != this._selectedHandlePiece)
				{
					this._selectedHandlePiece = value;
					base.OnPropertyChangedWithValue<CraftingPieceVM>(value, "SelectedHandlePiece");
				}
			}
		}

		// Token: 0x17000833 RID: 2099
		// (get) Token: 0x06001889 RID: 6281 RVA: 0x0005DCCB File Offset: 0x0005BECB
		// (set) Token: 0x0600188A RID: 6282 RVA: 0x0005DCD3 File Offset: 0x0005BED3
		[DataSourceProperty]
		public CraftingPieceVM SelectedPommelPiece
		{
			get
			{
				return this._selectedPommelPiece;
			}
			set
			{
				if (value != this._selectedPommelPiece)
				{
					this._selectedPommelPiece = value;
					base.OnPropertyChangedWithValue<CraftingPieceVM>(value, "SelectedPommelPiece");
				}
			}
		}

		// Token: 0x17000834 RID: 2100
		// (get) Token: 0x0600188B RID: 6283 RVA: 0x0005DCF4 File Offset: 0x0005BEF4
		// (set) Token: 0x0600188C RID: 6284 RVA: 0x0005DD58 File Offset: 0x0005BF58
		[DataSourceProperty]
		public int ActivePieceSize
		{
			get
			{
				if (this.ActivePieceList == null)
				{
					return 0;
				}
				switch (this.ActivePieceList.PieceType)
				{
				case CraftingPiece.PieceTypes.Blade:
					return this.BladeSize;
				case CraftingPiece.PieceTypes.Guard:
					return this.GuardSize;
				case CraftingPiece.PieceTypes.Handle:
					return this.HandleSize;
				case CraftingPiece.PieceTypes.Pommel:
					return this.PommelSize;
				}
				return 0;
			}
			set
			{
				if (value == this.ActivePieceSize || this.ActivePieceList == null)
				{
					return;
				}
				switch (this.ActivePieceList.PieceType)
				{
				case CraftingPiece.PieceTypes.Invalid:
				case CraftingPiece.PieceTypes.NumberOfPieceTypes:
					break;
				case CraftingPiece.PieceTypes.Blade:
					this.BladeSize = value;
					return;
				case CraftingPiece.PieceTypes.Guard:
					this.GuardSize = value;
					return;
				case CraftingPiece.PieceTypes.Handle:
					this.HandleSize = value;
					return;
				case CraftingPiece.PieceTypes.Pommel:
					this.PommelSize = value;
					break;
				default:
					return;
				}
			}
		}

		// Token: 0x17000835 RID: 2101
		// (get) Token: 0x0600188D RID: 6285 RVA: 0x0005DDC3 File Offset: 0x0005BFC3
		// (set) Token: 0x0600188E RID: 6286 RVA: 0x0005DDCC File Offset: 0x0005BFCC
		[DataSourceProperty]
		public int BladeSize
		{
			get
			{
				return this._bladeSize;
			}
			set
			{
				if (value != this._bladeSize)
				{
					this._bladeSize = value;
					base.OnPropertyChangedWithValue(value, "BladeSize");
					if (this._crafting != null && this._updatePiece && this._crafting.CurrentCraftingTemplate.IsPieceTypeUsable(CraftingPiece.PieceTypes.Blade))
					{
						int percentage = 100 + value;
						this._crafting.ScaleThePiece(CraftingPiece.PieceTypes.Blade, percentage);
						this.RefreshItem();
					}
					base.OnPropertyChanged("ActivePieceSize");
				}
			}
		}

		// Token: 0x17000836 RID: 2102
		// (get) Token: 0x0600188F RID: 6287 RVA: 0x0005DE3B File Offset: 0x0005C03B
		// (set) Token: 0x06001890 RID: 6288 RVA: 0x0005DE44 File Offset: 0x0005C044
		[DataSourceProperty]
		public int GuardSize
		{
			get
			{
				return this._guardSize;
			}
			set
			{
				if (value != this._guardSize)
				{
					this._guardSize = value;
					base.OnPropertyChangedWithValue(value, "GuardSize");
					if (this._crafting != null && this._updatePiece && this._crafting.CurrentCraftingTemplate.IsPieceTypeUsable(CraftingPiece.PieceTypes.Guard))
					{
						int percentage = 100 + value;
						this._crafting.ScaleThePiece(CraftingPiece.PieceTypes.Guard, percentage);
						this.RefreshItem();
					}
					base.OnPropertyChanged("ActivePieceSize");
				}
			}
		}

		// Token: 0x17000837 RID: 2103
		// (get) Token: 0x06001891 RID: 6289 RVA: 0x0005DEB3 File Offset: 0x0005C0B3
		// (set) Token: 0x06001892 RID: 6290 RVA: 0x0005DEBC File Offset: 0x0005C0BC
		[DataSourceProperty]
		public int HandleSize
		{
			get
			{
				return this._handleSize;
			}
			set
			{
				if (value != this._handleSize)
				{
					this._handleSize = value;
					base.OnPropertyChangedWithValue(value, "HandleSize");
					if (this._crafting != null && this._updatePiece && this._crafting.CurrentCraftingTemplate.IsPieceTypeUsable(CraftingPiece.PieceTypes.Handle))
					{
						int percentage = 100 + value;
						this._crafting.ScaleThePiece(CraftingPiece.PieceTypes.Handle, percentage);
						this.RefreshItem();
					}
					base.OnPropertyChanged("ActivePieceSize");
				}
			}
		}

		// Token: 0x17000838 RID: 2104
		// (get) Token: 0x06001893 RID: 6291 RVA: 0x0005DF2B File Offset: 0x0005C12B
		// (set) Token: 0x06001894 RID: 6292 RVA: 0x0005DF34 File Offset: 0x0005C134
		[DataSourceProperty]
		public int PommelSize
		{
			get
			{
				return this._pommelSize;
			}
			set
			{
				if (value != this._pommelSize)
				{
					this._pommelSize = value;
					base.OnPropertyChangedWithValue(value, "PommelSize");
					if (this._crafting != null && this._updatePiece && this._crafting.CurrentCraftingTemplate.IsPieceTypeUsable(CraftingPiece.PieceTypes.Pommel))
					{
						int percentage = 100 + value;
						this._crafting.ScaleThePiece(CraftingPiece.PieceTypes.Pommel, percentage);
						this.RefreshItem();
					}
					base.OnPropertyChanged("ActivePieceSize");
				}
			}
		}

		// Token: 0x17000839 RID: 2105
		// (get) Token: 0x06001895 RID: 6293 RVA: 0x0005DFA3 File Offset: 0x0005C1A3
		// (set) Token: 0x06001896 RID: 6294 RVA: 0x0005DFAB File Offset: 0x0005C1AB
		[DataSourceProperty]
		public string ComponentSizeLbl
		{
			get
			{
				return this._componentSizeLbl;
			}
			set
			{
				if (value != this._componentSizeLbl)
				{
					this._componentSizeLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "ComponentSizeLbl");
				}
			}
		}

		// Token: 0x1700083A RID: 2106
		// (get) Token: 0x06001897 RID: 6295 RVA: 0x0005DFCE File Offset: 0x0005C1CE
		// (set) Token: 0x06001898 RID: 6296 RVA: 0x0005DFD6 File Offset: 0x0005C1D6
		[DataSourceProperty]
		public bool IsWeaponCivilian
		{
			get
			{
				return this._isWeaponCivilian;
			}
			set
			{
				if (value != this._isWeaponCivilian)
				{
					this._isWeaponCivilian = value;
					base.OnPropertyChangedWithValue(value, "IsWeaponCivilian");
				}
			}
		}

		// Token: 0x1700083B RID: 2107
		// (get) Token: 0x06001899 RID: 6297 RVA: 0x0005DFF4 File Offset: 0x0005C1F4
		// (set) Token: 0x0600189A RID: 6298 RVA: 0x0005DFFC File Offset: 0x0005C1FC
		[DataSourceProperty]
		public HintViewModel ScabbardHint
		{
			get
			{
				return this._scabbardHint;
			}
			set
			{
				if (value != this._scabbardHint)
				{
					this._scabbardHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ScabbardHint");
				}
			}
		}

		// Token: 0x1700083C RID: 2108
		// (get) Token: 0x0600189B RID: 6299 RVA: 0x0005E01A File Offset: 0x0005C21A
		// (set) Token: 0x0600189C RID: 6300 RVA: 0x0005E022 File Offset: 0x0005C222
		[DataSourceProperty]
		public HintViewModel RandomizeHint
		{
			get
			{
				return this._randomizeHint;
			}
			set
			{
				if (value != this._randomizeHint)
				{
					this._randomizeHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "RandomizeHint");
				}
			}
		}

		// Token: 0x1700083D RID: 2109
		// (get) Token: 0x0600189D RID: 6301 RVA: 0x0005E040 File Offset: 0x0005C240
		// (set) Token: 0x0600189E RID: 6302 RVA: 0x0005E048 File Offset: 0x0005C248
		[DataSourceProperty]
		public HintViewModel UndoHint
		{
			get
			{
				return this._undoHint;
			}
			set
			{
				if (value != this._undoHint)
				{
					this._undoHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "UndoHint");
				}
			}
		}

		// Token: 0x1700083E RID: 2110
		// (get) Token: 0x0600189F RID: 6303 RVA: 0x0005E066 File Offset: 0x0005C266
		// (set) Token: 0x060018A0 RID: 6304 RVA: 0x0005E06E File Offset: 0x0005C26E
		[DataSourceProperty]
		public HintViewModel RedoHint
		{
			get
			{
				return this._redoHint;
			}
			set
			{
				if (value != this._redoHint)
				{
					this._redoHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "RedoHint");
				}
			}
		}

		// Token: 0x1700083F RID: 2111
		// (get) Token: 0x060018A1 RID: 6305 RVA: 0x0005E08C File Offset: 0x0005C28C
		// (set) Token: 0x060018A2 RID: 6306 RVA: 0x0005E094 File Offset: 0x0005C294
		[DataSourceProperty]
		public MBBindingList<ItemFlagVM> WeaponFlagIconsList
		{
			get
			{
				return this._weaponFlagIconsList;
			}
			set
			{
				if (value != this._weaponFlagIconsList)
				{
					this._weaponFlagIconsList = value;
					base.OnPropertyChangedWithValue<MBBindingList<ItemFlagVM>>(value, "WeaponFlagIconsList");
				}
			}
		}

		// Token: 0x17000840 RID: 2112
		// (get) Token: 0x060018A3 RID: 6307 RVA: 0x0005E0B2 File Offset: 0x0005C2B2
		// (set) Token: 0x060018A4 RID: 6308 RVA: 0x0005E0BA File Offset: 0x0005C2BA
		[DataSourceProperty]
		public CraftingHistoryVM CraftingHistory
		{
			get
			{
				return this._craftingHistory;
			}
			set
			{
				if (value != this._craftingHistory)
				{
					this._craftingHistory = value;
					base.OnPropertyChangedWithValue<CraftingHistoryVM>(value, "CraftingHistory");
				}
			}
		}

		// Token: 0x04000B01 RID: 2817
		private WeaponDesignVM.CraftingPieceTierFilter _currentTierFilter = WeaponDesignVM.CraftingPieceTierFilter.All;

		// Token: 0x04000B02 RID: 2818
		public const int MAX_SKILL_LEVEL = 300;

		// Token: 0x04000B03 RID: 2819
		public ItemObject CraftedItemObject;

		// Token: 0x04000B04 RID: 2820
		private int _selectedWeaponClassIndex;

		// Token: 0x04000B05 RID: 2821
		private readonly List<CraftingPiece> _newlyUnlockedPieces;

		// Token: 0x04000B06 RID: 2822
		private readonly List<CraftingTemplate> _primaryUsages;

		// Token: 0x04000B07 RID: 2823
		private readonly WeaponDesignVM.PieceTierComparer _pieceTierComparer;

		// Token: 0x04000B08 RID: 2824
		private readonly WeaponDesignVM.TemplateComparer _templateComparer;

		// Token: 0x04000B09 RID: 2825
		private readonly ICraftingCampaignBehavior _craftingBehavior;

		// Token: 0x04000B0A RID: 2826
		private readonly Action _onRefresh;

		// Token: 0x04000B0B RID: 2827
		private readonly Action _onWeaponCrafted;

		// Token: 0x04000B0C RID: 2828
		private readonly Func<CraftingAvailableHeroItemVM> _getCurrentCraftingHero;

		// Token: 0x04000B0D RID: 2829
		private readonly Action<CraftingOrder> _refreshHeroAvailabilities;

		// Token: 0x04000B0E RID: 2830
		private Func<WeaponComponentData, ItemObject.ItemUsageSetFlags> _getItemUsageSetFlags;

		// Token: 0x04000B0F RID: 2831
		private Crafting _crafting;

		// Token: 0x04000B10 RID: 2832
		private bool _updatePiece = true;

		// Token: 0x04000B11 RID: 2833
		private Dictionary<CraftingPiece.PieceTypes, CraftingPieceListVM> _pieceListsDictionary;

		// Token: 0x04000B12 RID: 2834
		private Dictionary<CraftingPiece, CraftingPieceVM> _pieceVMs;

		// Token: 0x04000B13 RID: 2835
		private TextObject _difficultyTextobj = new TextObject("{=cbbUzYX3}Difficulty: {DIFFICULTY}", null);

		// Token: 0x04000B14 RID: 2836
		private TextObject _orderDifficultyTextObj = new TextObject("{=8szijlHj}Order Difficulty: {DIFFICULTY}", null);

		// Token: 0x04000B15 RID: 2837
		private bool _isAutoSelectingPieces;

		// Token: 0x04000B16 RID: 2838
		private bool _shouldRecordHistory;

		// Token: 0x04000B17 RID: 2839
		private MBBindingList<TierFilterTypeVM> _tierFilters;

		// Token: 0x04000B18 RID: 2840
		private string _currentCraftedWeaponTemplateId;

		// Token: 0x04000B19 RID: 2841
		private string _chooseOrderText;

		// Token: 0x04000B1A RID: 2842
		private string _chooseWeaponTypeText;

		// Token: 0x04000B1B RID: 2843
		private string _currentCraftedWeaponTypeText;

		// Token: 0x04000B1C RID: 2844
		private MBBindingList<CraftingPieceListVM> _pieceLists;

		// Token: 0x04000B1D RID: 2845
		private int _selectedPieceTypeIndex;

		// Token: 0x04000B1E RID: 2846
		private bool _showOnlyUnlockedPieces;

		// Token: 0x04000B1F RID: 2847
		private string _missingPropertyWarningText;

		// Token: 0x04000B20 RID: 2848
		private bool _isInFinalCraftingStage;

		// Token: 0x04000B21 RID: 2849
		private string _componentSizeLbl;

		// Token: 0x04000B22 RID: 2850
		private string _itemName;

		// Token: 0x04000B23 RID: 2851
		private string _difficultyText;

		// Token: 0x04000B24 RID: 2852
		private int _bladeSize;

		// Token: 0x04000B25 RID: 2853
		private int _pommelSize;

		// Token: 0x04000B26 RID: 2854
		private int _handleSize;

		// Token: 0x04000B27 RID: 2855
		private int _guardSize;

		// Token: 0x04000B28 RID: 2856
		private CraftingPieceVM _selectedBladePiece;

		// Token: 0x04000B29 RID: 2857
		private CraftingPieceVM _selectedGuardPiece;

		// Token: 0x04000B2A RID: 2858
		private CraftingPieceVM _selectedHandlePiece;

		// Token: 0x04000B2B RID: 2859
		private CraftingPieceVM _selectedPommelPiece;

		// Token: 0x04000B2C RID: 2860
		private CraftingPieceListVM _activePieceList;

		// Token: 0x04000B2D RID: 2861
		private CraftingPieceListVM _bladePieceList;

		// Token: 0x04000B2E RID: 2862
		private CraftingPieceListVM _guardPieceList;

		// Token: 0x04000B2F RID: 2863
		private CraftingPieceListVM _handlePieceList;

		// Token: 0x04000B30 RID: 2864
		private CraftingPieceListVM _pommelPieceList;

		// Token: 0x04000B31 RID: 2865
		private string _alternativeUsageText;

		// Token: 0x04000B32 RID: 2866
		private string _defaultUsageText;

		// Token: 0x04000B33 RID: 2867
		private bool _isScabbardVisible;

		// Token: 0x04000B34 RID: 2868
		private bool _currentWeaponHasScabbard;

		// Token: 0x04000B35 RID: 2869
		public SelectorVM<CraftingSecondaryUsageItemVM> _secondaryUsageSelector;

		// Token: 0x04000B36 RID: 2870
		private ItemCollectionElementViewModel _craftedItemVisual;

		// Token: 0x04000B37 RID: 2871
		private MBBindingList<CraftingListPropertyItem> _primaryPropertyList;

		// Token: 0x04000B38 RID: 2872
		private MBBindingList<WeaponDesignResultPropertyItemVM> _designResultPropertyList;

		// Token: 0x04000B39 RID: 2873
		private int _currentDifficulty;

		// Token: 0x04000B3A RID: 2874
		private int _currentOrderDifficulty;

		// Token: 0x04000B3B RID: 2875
		private int _maxDifficulty;

		// Token: 0x04000B3C RID: 2876
		private string _currentDifficultyText;

		// Token: 0x04000B3D RID: 2877
		private string _currentOrderDifficultyText;

		// Token: 0x04000B3E RID: 2878
		private string _currentCraftingSkillValueText;

		// Token: 0x04000B3F RID: 2879
		private bool _isCurrentHeroAtMaxCraftingSkill;

		// Token: 0x04000B40 RID: 2880
		private int _currentHeroCraftingSkill;

		// Token: 0x04000B41 RID: 2881
		private bool _isWeaponCivilian;

		// Token: 0x04000B42 RID: 2882
		private HintViewModel _scabbardHint;

		// Token: 0x04000B43 RID: 2883
		private HintViewModel _randomizeHint;

		// Token: 0x04000B44 RID: 2884
		private HintViewModel _undoHint;

		// Token: 0x04000B45 RID: 2885
		private HintViewModel _redoHint;

		// Token: 0x04000B46 RID: 2886
		private HintViewModel _showOnlyUnlockedPiecesHint;

		// Token: 0x04000B47 RID: 2887
		private BasicTooltipViewModel _orderDisabledReasonHint;

		// Token: 0x04000B48 RID: 2888
		private CraftingOrderItemVM _activeCraftingOrder;

		// Token: 0x04000B49 RID: 2889
		private CraftingOrderPopupVM _craftingOrderPopup;

		// Token: 0x04000B4A RID: 2890
		private WeaponClassSelectionPopupVM _weaponClassSelectionPopup;

		// Token: 0x04000B4B RID: 2891
		private string _freeModeButtonText;

		// Token: 0x04000B4C RID: 2892
		private bool _isOrderButtonActive;

		// Token: 0x04000B4D RID: 2893
		private bool _isInOrderMode;

		// Token: 0x04000B4E RID: 2894
		private WeaponDesignResultPopupVM _craftingResultPopup;

		// Token: 0x04000B4F RID: 2895
		private MBBindingList<ItemFlagVM> _weaponFlagIconsList;

		// Token: 0x04000B50 RID: 2896
		private CraftingHistoryVM _craftingHistory;

		// Token: 0x04000B51 RID: 2897
		private TextObject _currentCraftingSkillText;

		// Token: 0x0200025B RID: 603
		[Flags]
		public enum CraftingPieceTierFilter
		{
			// Token: 0x04001271 RID: 4721
			None = 0,
			// Token: 0x04001272 RID: 4722
			Tier1 = 1,
			// Token: 0x04001273 RID: 4723
			Tier2 = 2,
			// Token: 0x04001274 RID: 4724
			Tier3 = 4,
			// Token: 0x04001275 RID: 4725
			Tier4 = 8,
			// Token: 0x04001276 RID: 4726
			Tier5 = 16,
			// Token: 0x04001277 RID: 4727
			All = 31
		}

		// Token: 0x0200025C RID: 604
		public class PieceTierComparer : IComparer<CraftingPieceVM>
		{
			// Token: 0x06002523 RID: 9507 RVA: 0x000805C8 File Offset: 0x0007E7C8
			public int Compare(CraftingPieceVM x, CraftingPieceVM y)
			{
				if (x.Tier != y.Tier)
				{
					return x.Tier.CompareTo(y.Tier);
				}
				return x.CraftingPiece.CraftingPiece.StringId.CompareTo(y.CraftingPiece.CraftingPiece.StringId);
			}
		}

		// Token: 0x0200025D RID: 605
		public class TemplateComparer : IComparer<CraftingTemplate>
		{
			// Token: 0x06002525 RID: 9509 RVA: 0x00080625 File Offset: 0x0007E825
			public int Compare(CraftingTemplate x, CraftingTemplate y)
			{
				return string.Compare(x.StringId, y.StringId, StringComparison.OrdinalIgnoreCase);
			}
		}

		// Token: 0x0200025E RID: 606
		public class WeaponPropertyComparer : IComparer<CraftingListPropertyItem>
		{
			// Token: 0x06002527 RID: 9511 RVA: 0x00080644 File Offset: 0x0007E844
			public int Compare(CraftingListPropertyItem x, CraftingListPropertyItem y)
			{
				return ((int)x.Type).CompareTo((int)y.Type);
			}
		}
	}
}
