using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party.PartyTroopManagerPopUp;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Party
{
	// Token: 0x0200002C RID: 44
	public class PartyVM : ViewModel
	{
		// Token: 0x170000E5 RID: 229
		// (get) Token: 0x0600036B RID: 875 RVA: 0x000168E5 File Offset: 0x00014AE5
		// (set) Token: 0x0600036C RID: 876 RVA: 0x000168ED File Offset: 0x00014AED
		public PartyScreenLogic PartyScreenLogic { get; private set; }

		// Token: 0x170000E6 RID: 230
		// (get) Token: 0x0600036D RID: 877 RVA: 0x000168F6 File Offset: 0x00014AF6
		public bool CanRightPartyTakeMoreTroops
		{
			get
			{
				return this.PartyScreenLogic.CurrentData.RightMemberRoster.TotalManCount < this.PartyScreenLogic.RightPartyMembersSizeLimit;
			}
		}

		// Token: 0x170000E7 RID: 231
		// (get) Token: 0x0600036E RID: 878 RVA: 0x0001691A File Offset: 0x00014B1A
		public bool CanRightPartyTakeMorePrisoners
		{
			get
			{
				return this.PartyScreenLogic.CurrentData.RightPrisonerRoster.TotalManCount < this.PartyScreenLogic.RightPartyPrisonersSizeLimit;
			}
		}

		// Token: 0x170000E8 RID: 232
		// (get) Token: 0x0600036F RID: 879 RVA: 0x0001693E File Offset: 0x00014B3E
		// (set) Token: 0x06000370 RID: 880 RVA: 0x00016946 File Offset: 0x00014B46
		[DataSourceProperty]
		public PartyCharacterVM CurrentCharacter
		{
			get
			{
				return this._currentCharacter;
			}
			set
			{
				if (value != null && this._currentCharacter != value)
				{
					this._currentCharacter = value;
					this.RefreshCurrentCharacterInformation();
					base.OnPropertyChangedWithValue<PartyCharacterVM>(value, "CurrentCharacter");
					this.ExecuteRemoveZeroCounts();
				}
			}
		}

		// Token: 0x170000E9 RID: 233
		// (get) Token: 0x06000371 RID: 881 RVA: 0x00016974 File Offset: 0x00014B74
		private List<Tuple<string, TextObject>> FormationNames
		{
			get
			{
				if (this._formationNames == null)
				{
					int num = 8;
					this._formationNames = new List<Tuple<string, TextObject>>(num + 1);
					for (int i = 0; i < num; i++)
					{
						string item = "<img src=\"PartyScreen\\FormationIcons\\" + (i + 1) + "\"/>";
						TextObject item2 = GameTexts.FindText("str_troop_group_name", i.ToString());
						this._formationNames.Add(new Tuple<string, TextObject>(item, item2));
					}
				}
				return this._formationNames;
			}
		}

		// Token: 0x06000372 RID: 882 RVA: 0x000169E8 File Offset: 0x00014BE8
		public PartyVM(PartyScreenLogic partyScreenLogic)
		{
			this.PartyScreenLogic = partyScreenLogic;
			PartyState activePartyState = PartyScreenHelper.GetActivePartyState();
			this._currentMode = ((activePartyState != null) ? activePartyState.PartyScreenMode : PartyScreenHelper.PartyScreenMode.Normal);
			this._viewDataTracker = Campaign.Current.GetCampaignBehavior<IViewDataTracker>();
			this.OtherPartyTroops = new MBBindingList<PartyCharacterVM>();
			this.OtherPartyPrisoners = new MBBindingList<PartyCharacterVM>();
			this.MainPartyTroops = new MBBindingList<PartyCharacterVM>();
			this.MainPartyPrisoners = new MBBindingList<PartyCharacterVM>();
			this.UpgradePopUp = new PartyUpgradeTroopVM(this);
			this.RecruitPopUp = new PartyRecruitTroopVM(this);
			this.SelectedCharacter = new HeroViewModel(CharacterViewModel.StanceTypes.None);
			this.DoneHint = new HintViewModel();
			this.DenarHint = new HintViewModel();
			this.MoraleHint = new HintViewModel();
			this.SpeedHint = new BasicTooltipViewModel();
			this.TotalWageHint = new HintViewModel();
			this.FormationHint = new HintViewModel();
			PartyCharacterVM.ProcessCharacterLock = new Action<PartyCharacterVM, bool>(this.ProcessCharacterLock);
			PartyCharacterVM.OnFocus = new Action<PartyCharacterVM>(this.OnFocusCharacter);
			PartyCharacterVM.OnShift = null;
			PartyCharacterVM.OnTransfer = new Action<PartyCharacterVM, int, int, PartyScreenLogic.PartyRosterSide>(this.OnTransferTroop);
			PartyCharacterVM.SetSelected = new Action<PartyCharacterVM>(this.ExecuteSelectCharacterTuple);
			this.OtherPartyComposition = new PartyCompositionVM();
			this.MainPartyComposition = new PartyCompositionVM();
			this.CanChooseRoles = this._currentMode == PartyScreenHelper.PartyScreenMode.Normal;
			if (this.PartyScreenLogic != null)
			{
				this.PartyScreenLogic.PartyGoldChange += this.OnPartyGoldChanged;
				this.PartyScreenLogic.PartyHorseChange += this.OnPartyHorseChanged;
				this.PartyScreenLogic.PartyInfluenceChange += this.OnPartyInfluenceChanged;
				this.PartyScreenLogic.PartyMoraleChange += this.OnPartyMoraleChanged;
				this.PartyScreenLogic.UpdateDelegate = new PartyScreenLogic.PresentationUpdate(this.Update);
				this.PartyScreenLogic.AfterReset += this.AfterReset;
				this.ShowQuestProgress = this.PartyScreenLogic.ShowProgressBar;
				if (this.ShowQuestProgress)
				{
					this.QuestProgressRequiredCount = this.PartyScreenLogic.GetCurrentQuestRequiredCount();
				}
				this.IsDoneDisabled = !this.PartyScreenLogic.IsDoneActive();
				this.DoneHint.HintText = new TextObject("{=!}" + this.PartyScreenLogic.DoneReasonString, null);
				this.IsCancelDisabled = !this.PartyScreenLogic.IsCancelActive();
				this.InitializeStaticInformation();
				this.InitializeTroopLists();
				this.RefreshPartyInformation();
			}
			this.UpdateTroopManagerPopUpCounts();
			PartyTradeVM.RemoveZeroCounts += this.ExecuteRemoveZeroCounts;
			Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
			this._viewDataTracker.ClearPartyNotification();
			this.IsAnyPopUpOpen = false;
			this.OtherPartySortController = new PartySortControllerVM(PartyScreenLogic.PartyRosterSide.Left, new Action<PartyScreenLogic.PartyRosterSide, PartyScreenLogic.TroopSortType, bool>(this.OnSortTroops));
			this.MainPartySortController = new PartySortControllerVM(PartyScreenLogic.PartyRosterSide.Right, new Action<PartyScreenLogic.PartyRosterSide, PartyScreenLogic.TroopSortType, bool>(this.OnSortTroops));
			this.MainPartySortController.SortWith((PartyScreenLogic.TroopSortType)this._viewDataTracker.GetPartySortType(), this._viewDataTracker.GetIsPartySortAscending());
			this.RefreshValues();
		}

		// Token: 0x06000373 RID: 883 RVA: 0x00016D10 File Offset: 0x00014F10
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.ResetHint = new HintViewModel(GameTexts.FindText("str_reset", null), null);
			this.LevelHint = new HintViewModel(GameTexts.FindText("str_level_tag", null), null);
			this.TitleLbl = GameTexts.FindText("str_party", null).ToString();
			this.OtherPartyAccompanyingLbl = GameTexts.FindText("str_party_list_tag_attached_groups", null).ToString();
			this.MoraleHint.HintText = GameTexts.FindText("str_party_morale", null);
			this.TotalWageHint.HintText = GameTexts.FindText("str_weekly_wage", null);
			this.TalkLbl = GameTexts.FindText("str_talk_button", null).ToString();
			this.InfoLbl = GameTexts.FindText("str_info", null).ToString();
			this.CancelLbl = GameTexts.FindText("str_cancel", null).ToString();
			this.DoneLbl = GameTexts.FindText("str_done", null).ToString();
			this.FormationHint.HintText = GameTexts.FindText("str_party_formation", null);
			this.TroopsLabel = GameTexts.FindText("str_troops_group", null).ToString();
			this.PrisonersLabel = GameTexts.FindText("str_party_category_prisoners_tooltip", null).ToString();
			this.TransferAllMainTroopsHint = new BasicTooltipViewModel(delegate()
			{
				GameTexts.SetVariable("TEXT", new TextObject("{=9WrJP0hD}Transfer All Troops", null));
				GameTexts.SetVariable("HOTKEY", this.GetTransferAllMainTroopsKeyText());
				return GameTexts.FindText("str_hotkey_with_hint", null).ToString();
			});
			this.TransferAllMainPrisonersHint = new BasicTooltipViewModel(delegate()
			{
				GameTexts.SetVariable("TEXT", new TextObject("{=qgK86eSo}Transfer All Prisoners", null));
				GameTexts.SetVariable("HOTKEY", this.GetTransferAllMainPrisonersKeyText());
				return GameTexts.FindText("str_hotkey_with_hint", null).ToString();
			});
			this.TransferAllOtherTroopsHint = new BasicTooltipViewModel(delegate()
			{
				GameTexts.SetVariable("TEXT", new TextObject("{=9WrJP0hD}Transfer All Troops", null));
				GameTexts.SetVariable("HOTKEY", this.GetTransferAllOtherTroopsKeyText());
				return GameTexts.FindText("str_hotkey_with_hint", null).ToString();
			});
			this.TransferAllOtherPrisonersHint = new BasicTooltipViewModel(delegate()
			{
				GameTexts.SetVariable("TEXT", new TextObject("{=qgK86eSo}Transfer All Prisoners", null));
				GameTexts.SetVariable("HOTKEY", this.GetTransferAllOtherPrisonersKeyText());
				return GameTexts.FindText("str_hotkey_with_hint", null).ToString();
			});
			this.WageHint = new HintViewModel(GameTexts.FindText("str_wage", null), null);
			this.UpgradePopUp.RefreshValues();
			this.RecruitPopUp.RefreshValues();
			MBBindingList<PartyCharacterVM> otherPartyTroops = this.OtherPartyTroops;
			if (otherPartyTroops != null)
			{
				otherPartyTroops.ApplyActionOnAllItems(delegate(PartyCharacterVM x)
				{
					x.RefreshValues();
				});
			}
			MBBindingList<PartyCharacterVM> otherPartyPrisoners = this.OtherPartyPrisoners;
			if (otherPartyPrisoners != null)
			{
				otherPartyPrisoners.ApplyActionOnAllItems(delegate(PartyCharacterVM x)
				{
					x.RefreshValues();
				});
			}
			MBBindingList<PartyCharacterVM> mainPartyTroops = this.MainPartyTroops;
			if (mainPartyTroops != null)
			{
				mainPartyTroops.ApplyActionOnAllItems(delegate(PartyCharacterVM x)
				{
					x.RefreshValues();
				});
			}
			MBBindingList<PartyCharacterVM> mainPartyPrisoners = this.MainPartyPrisoners;
			if (mainPartyPrisoners != null)
			{
				mainPartyPrisoners.ApplyActionOnAllItems(delegate(PartyCharacterVM x)
				{
					x.RefreshValues();
				});
			}
			this.UpdateLabelHints();
			this.OnPartyGoldChanged();
			if (this.PartyScreenLogic != null)
			{
				this.InitializeStaticInformation();
			}
		}

		// Token: 0x06000374 RID: 884 RVA: 0x00016FA8 File Offset: 0x000151A8
		private void OnPartyGoldChanged()
		{
			MBTextManager.SetTextVariable("PAY_OR_GET", (this.PartyScreenLogic.CurrentData.PartyGoldChangeAmount > 0) ? 1 : 0);
			MBTextManager.SetTextVariable("TRADE_AMOUNT", MathF.Abs(this.PartyScreenLogic.CurrentData.PartyGoldChangeAmount));
			this.GoldChangeText = ((this.PartyScreenLogic.CurrentData.PartyGoldChangeAmount == 0) ? "" : GameTexts.FindText("str_inventory_trade_label", null).ToString());
		}

		// Token: 0x06000375 RID: 885 RVA: 0x00017024 File Offset: 0x00015224
		private void OnPartyMoraleChanged()
		{
			MBTextManager.SetTextVariable("PAY_OR_GET", (this.PartyScreenLogic.CurrentData.PartyMoraleChangeAmount > 0) ? 1 : 0);
			MBTextManager.SetTextVariable("MORALE_ICON", "{=!}<img src=\"General\\Icons\\Morale@2x\" extend=\"8\">", false);
			MBTextManager.SetTextVariable("TRADE_AMOUNT", MathF.Abs(this.PartyScreenLogic.CurrentData.PartyMoraleChangeAmount));
			this.MoraleChangeText = ((this.PartyScreenLogic.CurrentData.PartyMoraleChangeAmount == 0) ? "" : GameTexts.FindText("str_party_morale_label", null).ToString());
		}

		// Token: 0x06000376 RID: 886 RVA: 0x000170B0 File Offset: 0x000152B0
		private void OnPartyInfluenceChanged()
		{
			int num = this.PartyScreenLogic.CurrentData.PartyInfluenceChangeAmount.Item1 + this.PartyScreenLogic.CurrentData.PartyInfluenceChangeAmount.Item2 + this.PartyScreenLogic.CurrentData.PartyInfluenceChangeAmount.Item3;
			MBTextManager.SetTextVariable("PAY_OR_GET", (num > 0) ? 1 : 0);
			MBTextManager.SetTextVariable("INFLUENCE_ICON", "{=!}<img src=\"General\\Icons\\Influence@2x\" extend=\"7\">", false);
			MBTextManager.SetTextVariable("TRADE_AMOUNT", MathF.Abs(num));
			this.InfluenceChangeText = ((num == 0) ? "" : GameTexts.FindText("str_party_influence_label", null).ToString());
		}

		// Token: 0x06000377 RID: 887 RVA: 0x00017154 File Offset: 0x00015354
		private void OnPartyHorseChanged()
		{
			MBTextManager.SetTextVariable("IS_PLURAL", (this.PartyScreenLogic.CurrentData.PartyHorseChangeAmount > 1) ? 1 : 0);
			MBTextManager.SetTextVariable("TRADE_AMOUNT", MathF.Abs(this.PartyScreenLogic.CurrentData.PartyHorseChangeAmount));
			this.HorseChangeText = ((this.PartyScreenLogic.CurrentData.PartyHorseChangeAmount == 0) ? "" : GameTexts.FindText("str_party_horse_label", null).ToString());
		}

		// Token: 0x06000378 RID: 888 RVA: 0x000171D0 File Offset: 0x000153D0
		private void InitializeTroopLists()
		{
			this.ArePrisonersRelevantOnCurrentMode = this._currentMode != PartyScreenHelper.PartyScreenMode.TroopsManage && this._currentMode != PartyScreenHelper.PartyScreenMode.QuestTroopManage;
			this.AreMembersRelevantOnCurrentMode = this._currentMode != PartyScreenHelper.PartyScreenMode.PrisonerManage && this._currentMode != PartyScreenHelper.PartyScreenMode.Ransom;
			this._lockedTroopIDs = this._viewDataTracker.GetPartyTroopLocks().ToList<string>();
			this._lockedPrisonerIDs = this._viewDataTracker.GetPartyPrisonerLocks().ToList<string>();
			this.InitializePartyList(this.MainPartyPrisoners, this.PartyScreenLogic.PrisonerRosters[1], PartyScreenLogic.TroopType.Prisoner, 1);
			this.InitializePartyList(this.OtherPartyPrisoners, this.PartyScreenLogic.PrisonerRosters[0], PartyScreenLogic.TroopType.Prisoner, 0);
			this.InitializePartyList(this.MainPartyTroops, this.PartyScreenLogic.MemberRosters[1], PartyScreenLogic.TroopType.Member, 1);
			this.InitializePartyList(this.OtherPartyTroops, this.PartyScreenLogic.MemberRosters[0], PartyScreenLogic.TroopType.Member, 0);
			if (this.MainPartyTroops.Count > 0)
			{
				this.CurrentCharacter = this.MainPartyTroops[0];
			}
			else if (this.OtherPartyTroops.Count > 0)
			{
				this.CurrentCharacter = this.OtherPartyTroops[0];
			}
			this.RefreshTopInformation();
			this.OtherPartyComposition.RefreshCounts(this.OtherPartyTroops);
			this.MainPartyComposition.RefreshCounts(this.MainPartyTroops);
		}

		// Token: 0x06000379 RID: 889 RVA: 0x0001731C File Offset: 0x0001551C
		private void RefreshTopInformation()
		{
			this.MainPartyTotalWeeklyCostLbl = MobileParty.MainParty.TotalWage.ToString();
			this.MainPartyTotalGoldLbl = Hero.MainHero.Gold.ToString();
			this.MainPartyTotalMoraleLbl = ((int)MobileParty.MainParty.Morale).ToString("##.0");
			this.MainPartyTotalSpeedLbl = CampaignUIHelper.FloatToString(MobileParty.MainParty.Speed);
			this.UpdateLabelHints();
		}

		// Token: 0x0600037A RID: 890 RVA: 0x00017394 File Offset: 0x00015594
		private void UpdateLabelHints()
		{
			this.SpeedHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartySpeedTooltip(false));
			if (this.PartyScreenLogic.RightOwnerParty != null)
			{
				this.MainPartyTroopSizeLimitHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartyTroopSizeLimitTooltip(this.PartyScreenLogic.RightOwnerParty));
				this.MainPartyPrisonerSizeLimitHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartyPrisonerSizeLimitTooltip(this.PartyScreenLogic.RightOwnerParty));
			}
			if (this.PartyScreenLogic.LeftOwnerParty != null)
			{
				this.OtherPartyTroopSizeLimitHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartyTroopSizeLimitTooltip(this.PartyScreenLogic.LeftOwnerParty));
				this.OtherPartyPrisonerSizeLimitHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartyPrisonerSizeLimitTooltip(this.PartyScreenLogic.LeftOwnerParty));
			}
			this.UsedHorsesHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetUsedHorsesTooltip(this.PartyScreenLogic.CurrentData.UsedUpgradeHorsesHistory));
			this.DenarHint.HintText = GameTexts.FindText("str_gold", null);
		}

		// Token: 0x0600037B RID: 891 RVA: 0x00017470 File Offset: 0x00015670
		private void InitializeStaticInformation()
		{
			if (this.PartyScreenLogic.RightOwnerParty != null)
			{
				this.MainPartyNameLbl = this.PartyScreenLogic.RightOwnerParty.Name.ToString();
			}
			else
			{
				this.MainPartyNameLbl = ((!TextObject.IsNullOrEmpty(this.PartyScreenLogic.RightPartyName)) ? this.PartyScreenLogic.RightPartyName.ToString() : string.Empty);
			}
			MBTextManager.SetTextVariable("PARTY_NAME", MobileParty.MainParty.Name, false);
			if (this.PartyScreenLogic.LeftOwnerParty != null)
			{
				this.OtherPartyNameLbl = this.PartyScreenLogic.LeftOwnerParty.Name.ToString();
			}
			else
			{
				this.OtherPartyNameLbl = ((!TextObject.IsNullOrEmpty(this.PartyScreenLogic.LeftPartyName)) ? this.PartyScreenLogic.LeftPartyName.ToString() : GameTexts.FindText("str_dismiss", null).ToString());
			}
			if (this.PartyScreenLogic.Header == null || string.IsNullOrEmpty(this.PartyScreenLogic.Header.ToString()))
			{
				this.HeaderLbl = GameTexts.FindText("str_party", null).ToString();
				return;
			}
			this.HeaderLbl = this.PartyScreenLogic.Header.ToString();
		}

		// Token: 0x0600037C RID: 892 RVA: 0x000175A6 File Offset: 0x000157A6
		public void SetSelectedCharacter(PartyCharacterVM troop)
		{
			this.CurrentCharacter = troop;
			this.CurrentCharacter.UpdateRecruitable();
		}

		// Token: 0x0600037D RID: 893 RVA: 0x000175BC File Offset: 0x000157BC
		public void ExecuteSelectCharacterTuple(PartyCharacterVM troop)
		{
			if (troop == null || troop.IsSelected)
			{
				using (IEnumerator<PartyCharacterVM> enumerator = this.GetAllCharacters(true).GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						PartyCharacterVM partyCharacterVM = enumerator.Current;
						partyCharacterVM.IsSelected = false;
					}
					goto IL_BD;
				}
			}
			foreach (PartyCharacterVM partyCharacterVM2 in this.GetAllCharacters(true))
			{
				partyCharacterVM2.IsSelected = partyCharacterVM2.Character.Equals(troop.Character) && partyCharacterVM2.IsPrisoner == troop.IsPrisoner;
				if (partyCharacterVM2.IsSelected)
				{
					this.ScrollCharacterId = partyCharacterVM2.Character.StringId;
					this.ScrollToCharacter = true;
					this.IsScrollTargetPrisoner = partyCharacterVM2.IsPrisoner;
				}
			}
			IL_BD:
			if (troop != null)
			{
				this.SetSelectedCharacter(troop);
			}
		}

		// Token: 0x0600037E RID: 894 RVA: 0x000176AC File Offset: 0x000158AC
		public void ExecuteClearSelectedCharacterTuple()
		{
			this.ExecuteSelectCharacterTuple(null);
		}

		// Token: 0x0600037F RID: 895 RVA: 0x000176B5 File Offset: 0x000158B5
		private IEnumerable<PartyCharacterVM> GetAllCharacters(bool includePrisoners)
		{
			foreach (PartyCharacterVM partyCharacterVM in this.OtherPartyTroops)
			{
				yield return partyCharacterVM;
			}
			IEnumerator<PartyCharacterVM> enumerator = null;
			foreach (PartyCharacterVM partyCharacterVM2 in this.MainPartyTroops)
			{
				yield return partyCharacterVM2;
			}
			enumerator = null;
			if (includePrisoners)
			{
				foreach (PartyCharacterVM partyCharacterVM3 in this.GetPrisonerCharacters())
				{
					yield return partyCharacterVM3;
				}
				enumerator = null;
			}
			yield break;
			yield break;
		}

		// Token: 0x06000380 RID: 896 RVA: 0x000176CC File Offset: 0x000158CC
		private IEnumerable<PartyCharacterVM> GetPrisonerCharacters()
		{
			foreach (PartyCharacterVM partyCharacterVM in this.OtherPartyPrisoners)
			{
				yield return partyCharacterVM;
			}
			IEnumerator<PartyCharacterVM> enumerator = null;
			foreach (PartyCharacterVM partyCharacterVM2 in this.MainPartyPrisoners)
			{
				yield return partyCharacterVM2;
			}
			enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x06000381 RID: 897 RVA: 0x000176DC File Offset: 0x000158DC
		private void ProcessCharacterLock(PartyCharacterVM troop, bool isLocked)
		{
			List<string> list = (troop.IsPrisoner ? this._lockedPrisonerIDs : this._lockedTroopIDs);
			if (isLocked && !list.Contains(troop.StringId))
			{
				list.Add(troop.StringId);
				return;
			}
			if (!isLocked && list.Contains(troop.StringId))
			{
				list.Remove(troop.StringId);
			}
		}

		// Token: 0x06000382 RID: 898 RVA: 0x0001773C File Offset: 0x0001593C
		private PartyCompositionVM GetCompositionForList(MBBindingList<PartyCharacterVM> list)
		{
			if (list == this.MainPartyTroops)
			{
				return this.MainPartyComposition;
			}
			if (list == this.OtherPartyTroops)
			{
				return this.OtherPartyComposition;
			}
			return null;
		}

		// Token: 0x06000383 RID: 899 RVA: 0x0001775F File Offset: 0x0001595F
		private void SaveSortState()
		{
			this._viewDataTracker.SetPartySortType((int)this.PartyScreenLogic.ActiveMainPartySortType);
			this._viewDataTracker.SetIsPartySortAscending(this.PartyScreenLogic.IsMainPartySortAscending);
		}

		// Token: 0x06000384 RID: 900 RVA: 0x0001778D File Offset: 0x0001598D
		private void SaveCharacterLockStates()
		{
			this._viewDataTracker.SetPartyTroopLocks(this._lockedTroopIDs);
			this._viewDataTracker.SetPartyPrisonerLocks(this._lockedPrisonerIDs);
		}

		// Token: 0x06000385 RID: 901 RVA: 0x000177B1 File Offset: 0x000159B1
		private bool IsTroopLocked(TroopRosterElement troop, bool isPrisoner)
		{
			if (!isPrisoner)
			{
				return this._lockedTroopIDs.Contains(troop.Character.StringId);
			}
			return this._lockedPrisonerIDs.Contains(troop.Character.StringId);
		}

		// Token: 0x06000386 RID: 902 RVA: 0x000177E3 File Offset: 0x000159E3
		private void UpdateCurrentCharacterFormationClass(SelectorVM<SelectorItemVM> s)
		{
			Campaign.Current.SetPlayerFormationPreference(this.CurrentCharacter.Character, (FormationClass)s.SelectedIndex);
		}

		// Token: 0x06000387 RID: 903 RVA: 0x00017800 File Offset: 0x00015A00
		private void InitializePartyList(MBBindingList<PartyCharacterVM> partyList, TroopRoster currentTroopRoster, PartyScreenLogic.TroopType type, int side)
		{
			partyList.Clear();
			MBList<TroopRosterElement> troopRoster = currentTroopRoster.GetTroopRoster();
			for (int i = 0; i < troopRoster.Count; i++)
			{
				TroopRosterElement troopRosterElement = troopRoster[i];
				if (troopRosterElement.Character == null)
				{
					Debug.FailedAssert("Invalid TroopRosterElement found in InitializePartyList!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Party\\PartyVM.cs", "InitializePartyList", 497);
				}
				else
				{
					PartyCharacterVM partyCharacterVM = new PartyCharacterVM(this.PartyScreenLogic, this, currentTroopRoster, currentTroopRoster.FindIndexOfTroop(troopRosterElement.Character), type, (PartyScreenLogic.PartyRosterSide)side, this.PartyScreenLogic.IsTroopTransferable(type, troopRosterElement.Character, side));
					partyList.Add(partyCharacterVM);
					partyCharacterVM.ThrowOnPropertyChanged();
					partyCharacterVM.IsLocked = partyCharacterVM.Side == PartyScreenLogic.PartyRosterSide.Right && this.IsTroopLocked(partyCharacterVM.Troop, partyCharacterVM.IsPrisoner);
				}
			}
		}

		// Token: 0x06000388 RID: 904 RVA: 0x000178C0 File Offset: 0x00015AC0
		public void ExecuteTransferWithParameters(PartyCharacterVM party, int index, string targetTag)
		{
			PartyScreenLogic.PartyRosterSide side = party.Side;
			PartyScreenLogic.PartyRosterSide partyRosterSide = (targetTag.StartsWith("MainParty") ? PartyScreenLogic.PartyRosterSide.Right : PartyScreenLogic.PartyRosterSide.Left);
			if (targetTag == "MainParty")
			{
				index = -1;
			}
			else if (targetTag.EndsWith("Prisoners") != party.IsPrisoner)
			{
				index = -1;
			}
			if (side != partyRosterSide && party.IsTroopTransferrable)
			{
				this.OnTransferTroop(party, index, party.Number, party.Side);
				this.ExecuteRemoveZeroCounts();
				return;
			}
			if (side == partyRosterSide)
			{
				this.OnShiftTroop(party, index);
			}
		}

		// Token: 0x06000389 RID: 905 RVA: 0x00017944 File Offset: 0x00015B44
		private void OnTransferTroop(PartyCharacterVM troop, int newIndex, int transferAmount, PartyScreenLogic.PartyRosterSide fromSide)
		{
			if (troop.Side == PartyScreenLogic.PartyRosterSide.None || fromSide == PartyScreenLogic.PartyRosterSide.None)
			{
				return;
			}
			PartyScreenLogic.PartyRosterSide side = troop.Side;
			this.SetSelectedCharacter(troop);
			PartyScreenLogic.PartyCommand partyCommand = new PartyScreenLogic.PartyCommand();
			if (newIndex == -1)
			{
				newIndex = this.PartyScreenLogic.GetIndexToInsertTroop(PartyScreenLogic.PartyRosterSide.Right - troop.Side, troop.Type, troop.Troop);
			}
			else if (fromSide == PartyScreenLogic.PartyRosterSide.Left)
			{
				this.MainPartySortController.SelectSortType(PartyScreenLogic.TroopSortType.Custom);
			}
			else if (fromSide == PartyScreenLogic.PartyRosterSide.Right)
			{
				this.OtherPartySortController.SelectSortType(PartyScreenLogic.TroopSortType.Custom);
			}
			if (transferAmount > 0)
			{
				int numberOfHealthyTroopNumberForSide = this.GetNumberOfHealthyTroopNumberForSide(troop.Troop.Character, fromSide, troop.Type);
				int numberOfWoundedTroopNumberForSide = this.GetNumberOfWoundedTroopNumberForSide(troop.Troop.Character, fromSide, troop.Type);
				if ((this.PartyScreenLogic.TransferHealthiesGetWoundedsFirst && fromSide == PartyScreenLogic.PartyRosterSide.Right) || (!this.PartyScreenLogic.TransferHealthiesGetWoundedsFirst && fromSide == PartyScreenLogic.PartyRosterSide.Left))
				{
					int num = ((transferAmount <= numberOfHealthyTroopNumberForSide) ? 0 : (transferAmount - numberOfHealthyTroopNumberForSide));
					num = (int)MathF.Clamp((float)num, 0f, (float)numberOfWoundedTroopNumberForSide);
					partyCommand.FillForTransferTroop(fromSide, troop.Type, troop.Character, transferAmount, num, newIndex);
				}
				else
				{
					partyCommand.FillForTransferTroop(fromSide, troop.Type, troop.Character, transferAmount, (numberOfWoundedTroopNumberForSide >= transferAmount) ? transferAmount : numberOfWoundedTroopNumberForSide, newIndex);
				}
				this.PartyScreenLogic.AddCommand(partyCommand);
			}
		}

		// Token: 0x0600038A RID: 906 RVA: 0x00017A82 File Offset: 0x00015C82
		private void OnFocusCharacter(PartyCharacterVM character)
		{
			this.CurrentFocusedCharacter = character;
		}

		// Token: 0x0600038B RID: 907 RVA: 0x00017A8B File Offset: 0x00015C8B
		private int GetNumberOfWoundedTroopNumberForSide(CharacterObject character, PartyScreenLogic.PartyRosterSide fromSide, PartyScreenLogic.TroopType troopType)
		{
			PartyCharacterVM partyCharacterVM = this.FindCharacterVM(character, fromSide, troopType);
			if (partyCharacterVM == null)
			{
				return 0;
			}
			return partyCharacterVM.WoundedCount;
		}

		// Token: 0x0600038C RID: 908 RVA: 0x00017AA4 File Offset: 0x00015CA4
		private int GetNumberOfHealthyTroopNumberForSide(CharacterObject character, PartyScreenLogic.PartyRosterSide fromSide, PartyScreenLogic.TroopType troopType)
		{
			PartyCharacterVM partyCharacterVM = this.FindCharacterVM(character, fromSide, troopType);
			int? num = ((partyCharacterVM != null) ? new int?(partyCharacterVM.Troop.Number) : null) - ((partyCharacterVM != null) ? new int?(partyCharacterVM.Troop.WoundedNumber) : null);
			if (num == null)
			{
				return 0;
			}
			return num.GetValueOrDefault();
		}

		// Token: 0x0600038D RID: 909 RVA: 0x00017B44 File Offset: 0x00015D44
		private void OnSortTroops(PartyScreenLogic.PartyRosterSide side, PartyScreenLogic.TroopSortType sortType, bool isAscending)
		{
			PartyScreenLogic.TroopSortType activeSortTypeForSide = this.PartyScreenLogic.GetActiveSortTypeForSide(side);
			bool isAscendingSortForSide = this.PartyScreenLogic.GetIsAscendingSortForSide(side);
			if (activeSortTypeForSide != sortType || isAscendingSortForSide != isAscending)
			{
				PartyScreenLogic.PartyCommand partyCommand = new PartyScreenLogic.PartyCommand();
				partyCommand.FillForSortTroops(side, sortType, isAscending);
				this.PartyScreenLogic.AddCommand(partyCommand);
			}
		}

		// Token: 0x0600038E RID: 910 RVA: 0x00017B8C File Offset: 0x00015D8C
		private PartyCharacterVM FindCharacterVM(CharacterObject character, PartyScreenLogic.PartyRosterSide side, PartyScreenLogic.TroopType troopType)
		{
			MBBindingList<PartyCharacterVM> mbbindingList = null;
			if (side == PartyScreenLogic.PartyRosterSide.Left)
			{
				mbbindingList = ((troopType == PartyScreenLogic.TroopType.Member) ? this.OtherPartyTroops : this.OtherPartyPrisoners);
			}
			else if (side == PartyScreenLogic.PartyRosterSide.Right)
			{
				mbbindingList = ((troopType == PartyScreenLogic.TroopType.Member) ? this.MainPartyTroops : this.MainPartyPrisoners);
			}
			if (mbbindingList == null)
			{
				return null;
			}
			return mbbindingList.FirstOrDefault((PartyCharacterVM x) => x.Troop.Character == character);
		}

		// Token: 0x0600038F RID: 911 RVA: 0x00017BF0 File Offset: 0x00015DF0
		private void UpdateAllTradeDatasOfCharacter(CharacterObject character)
		{
			MBBindingList<PartyCharacterVM> otherPartyPrisoners = this.OtherPartyPrisoners;
			if (otherPartyPrisoners != null)
			{
				PartyCharacterVM partyCharacterVM = otherPartyPrisoners.FirstOrDefault((PartyCharacterVM x) => x.Character == character);
				if (partyCharacterVM != null)
				{
					partyCharacterVM.UpdateTradeData();
				}
			}
			MBBindingList<PartyCharacterVM> otherPartyTroops = this.OtherPartyTroops;
			if (otherPartyTroops != null)
			{
				PartyCharacterVM partyCharacterVM2 = otherPartyTroops.FirstOrDefault((PartyCharacterVM x) => x.Character == character);
				if (partyCharacterVM2 != null)
				{
					partyCharacterVM2.UpdateTradeData();
				}
			}
			MBBindingList<PartyCharacterVM> mainPartyPrisoners = this.MainPartyPrisoners;
			if (mainPartyPrisoners != null)
			{
				PartyCharacterVM partyCharacterVM3 = mainPartyPrisoners.FirstOrDefault((PartyCharacterVM x) => x.Character == character);
				if (partyCharacterVM3 != null)
				{
					partyCharacterVM3.UpdateTradeData();
				}
			}
			MBBindingList<PartyCharacterVM> mainPartyTroops = this.MainPartyTroops;
			if (mainPartyTroops == null)
			{
				return;
			}
			PartyCharacterVM partyCharacterVM4 = mainPartyTroops.FirstOrDefault((PartyCharacterVM x) => x.Character == character);
			if (partyCharacterVM4 == null)
			{
				return;
			}
			partyCharacterVM4.UpdateTradeData();
		}

		// Token: 0x06000390 RID: 912 RVA: 0x00017CA8 File Offset: 0x00015EA8
		private void OnShiftTroop(PartyCharacterVM troop, int newIndex)
		{
			if (troop.Side == PartyScreenLogic.PartyRosterSide.None)
			{
				return;
			}
			this.SetSelectedCharacter(troop);
			PartyScreenLogic.PartyCommand partyCommand = new PartyScreenLogic.PartyCommand();
			partyCommand.FillForShiftTroop(troop.Side, troop.Type, troop.Character, newIndex);
			this.PartyScreenLogic.AddCommand(partyCommand);
		}

		// Token: 0x06000391 RID: 913 RVA: 0x00017CF4 File Offset: 0x00015EF4
		private void Update(PartyScreenLogic.PartyCommand command)
		{
			switch (command.Code)
			{
			case PartyScreenLogic.PartyCommandCode.TransferTroop:
			case PartyScreenLogic.PartyCommandCode.TransferPartyLeaderTroop:
			case PartyScreenLogic.PartyCommandCode.TransferTroopToLeaderSlot:
				this.TransferTroop(command);
				break;
			case PartyScreenLogic.PartyCommandCode.UpgradeTroop:
				this.UpgradeTroop(command);
				this.RefreshTroopsUpgradeable();
				this.UpgradePopUp.OnTroopUpgraded();
				break;
			case PartyScreenLogic.PartyCommandCode.ShiftTroop:
				this.ShiftTroop(command);
				break;
			case PartyScreenLogic.PartyCommandCode.RecruitTroop:
			{
				PartyCharacterVM currentCharacter = this.CurrentCharacter;
				this.RecruitTroop(command);
				this.RecruitPopUp.OnTroopRecruited(currentCharacter);
				break;
			}
			case PartyScreenLogic.PartyCommandCode.ExecuteTroop:
				this.ExecuteTroop(command);
				break;
			case PartyScreenLogic.PartyCommandCode.TransferAllTroops:
				this.TransferAllTroops(command);
				break;
			case PartyScreenLogic.PartyCommandCode.SortTroops:
				this.SortTroops(command);
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
			this.RefreshTopInformation();
			this.UpdateTroopManagerPopUpCounts();
			this.RefreshPrisonersRecruitable();
			this.IsDoneDisabled = !this.PartyScreenLogic.IsDoneActive();
			this.DoneHint.HintText = new TextObject("{=!}" + this.PartyScreenLogic.DoneReasonString, null);
			this.IsCancelDisabled = !this.PartyScreenLogic.IsCancelActive();
		}

		// Token: 0x06000392 RID: 914 RVA: 0x00017E00 File Offset: 0x00016000
		private MBBindingList<PartyCharacterVM> GetPartyCharacterVMList(PartyScreenLogic.PartyRosterSide rosterSide, PartyScreenLogic.TroopType type)
		{
			MBBindingList<PartyCharacterVM> result = null;
			if (type == PartyScreenLogic.TroopType.Member)
			{
				if (rosterSide == PartyScreenLogic.PartyRosterSide.Left)
				{
					result = this.OtherPartyTroops;
				}
				else if (rosterSide == PartyScreenLogic.PartyRosterSide.Right)
				{
					result = this.MainPartyTroops;
				}
			}
			else if (type == PartyScreenLogic.TroopType.Prisoner)
			{
				if (rosterSide == PartyScreenLogic.PartyRosterSide.Left)
				{
					result = this.OtherPartyPrisoners;
				}
				else if (rosterSide == PartyScreenLogic.PartyRosterSide.Right)
				{
					result = this.MainPartyPrisoners;
				}
			}
			return result;
		}

		// Token: 0x06000393 RID: 915 RVA: 0x00017E48 File Offset: 0x00016048
		private void AfterReset(PartyScreenLogic partyScreenLogic, bool fromCancel)
		{
			if (!fromCancel)
			{
				this.InitializeTroopLists();
				this.RefreshPartyInformation();
				this.OnPartyGoldChanged();
				this.OnPartyMoraleChanged();
				this.OnPartyHorseChanged();
				this.OnPartyInfluenceChanged();
				this.UpdateTroopManagerPopUpCounts();
				this.MainPartyComposition.RefreshCounts(this.MainPartyTroops);
				this.OtherPartyComposition.RefreshCounts(this.OtherPartyTroops);
				this.IsDoneDisabled = !partyScreenLogic.IsDoneActive();
				this.DoneHint.HintText = new TextObject("{=!}" + this.PartyScreenLogic.DoneReasonString, null);
				this.IsCancelDisabled = !partyScreenLogic.IsCancelActive();
			}
		}

		// Token: 0x06000394 RID: 916 RVA: 0x00017EEC File Offset: 0x000160EC
		private void TransferTroop(PartyScreenLogic.PartyCommand command)
		{
			PartyScreenLogic.PartyRosterSide partyRosterSide = ((command.RosterSide == PartyScreenLogic.PartyRosterSide.Left) ? PartyScreenLogic.PartyRosterSide.Right : PartyScreenLogic.PartyRosterSide.Left);
			MBBindingList<PartyCharacterVM> partyCharacterVMList = this.GetPartyCharacterVMList(command.RosterSide, command.Type);
			MBBindingList<PartyCharacterVM> partyCharacterVMList2 = this.GetPartyCharacterVMList(partyRosterSide, command.Type);
			TroopRoster troopRoster = null;
			TroopRoster troopRoster2 = null;
			int index = 0;
			int index2 = 0;
			PartyScreenLogic.TroopType type = command.Type;
			if (type != PartyScreenLogic.TroopType.Member)
			{
				if (type == PartyScreenLogic.TroopType.Prisoner)
				{
					troopRoster = this.PartyScreenLogic.PrisonerRosters[(int)partyRosterSide];
					index = this.PartyScreenLogic.PrisonerRosters[(int)partyRosterSide].FindIndexOfTroop(this.CurrentCharacter.Character);
					troopRoster2 = this.PartyScreenLogic.PrisonerRosters[(int)command.RosterSide];
					index2 = this.PartyScreenLogic.PrisonerRosters[(int)command.RosterSide].FindIndexOfTroop(this.CurrentCharacter.Character);
				}
			}
			else
			{
				troopRoster = this.PartyScreenLogic.MemberRosters[(int)partyRosterSide];
				index = this.PartyScreenLogic.MemberRosters[(int)partyRosterSide].FindIndexOfTroop(this.CurrentCharacter.Character);
				troopRoster2 = this.PartyScreenLogic.MemberRosters[(int)command.RosterSide];
				index2 = this.PartyScreenLogic.MemberRosters[(int)command.RosterSide].FindIndexOfTroop(this.CurrentCharacter.Character);
			}
			PartyCharacterVM partyCharacterVM = partyCharacterVMList.FirstOrDefault((PartyCharacterVM q) => q.Character == this.CurrentCharacter.Character);
			if (troopRoster2.FindIndexOfTroop(this.CurrentCharacter.Character) != -1 && partyCharacterVM != null)
			{
				partyCharacterVM.Troop = troopRoster2.GetElementCopyAtIndex(index2);
				partyCharacterVM.ThrowOnPropertyChanged();
				partyCharacterVM.UpdateTradeData();
			}
			int num = -1;
			for (int i = 0; i < partyCharacterVMList2.Count; i++)
			{
				if (partyCharacterVMList2[i].Character == command.Character)
				{
					num = i;
					break;
				}
			}
			if (num >= 0)
			{
				PartyCharacterVM partyCharacterVM2 = partyCharacterVMList2[num];
				partyCharacterVM2.Troop = troopRoster.GetElementCopyAtIndex(index);
				partyCharacterVM2.ThrowOnPropertyChanged();
				if (!partyCharacterVMList.Contains(this.CurrentCharacter))
				{
					this.SetSelectedCharacter(partyCharacterVM2);
				}
				partyCharacterVM2.UpdateTradeData();
				if (partyCharacterVM2.IsSelected)
				{
					this.ScrollCharacterId = partyCharacterVM2.Character.StringId;
					this.IsScrollTargetPrisoner = partyCharacterVM2.IsPrisoner;
					this.ScrollToCharacter = true;
				}
				int num2 = command.Index;
				if (num2 != -1)
				{
					if (num2 == partyCharacterVMList2.Count)
					{
						num2 = partyCharacterVMList2.Count - 1;
					}
					partyCharacterVMList2.RemoveAt(num);
					partyCharacterVMList2.Insert(num2, partyCharacterVM2);
				}
			}
			else
			{
				PartyCharacterVM partyCharacterVM3 = new PartyCharacterVM(this.PartyScreenLogic, this, troopRoster, index, command.Type, partyRosterSide, this.PartyScreenLogic.IsTroopTransferable(command.Type, troopRoster.GetCharacterAtIndex(index), (int)partyRosterSide));
				if (command.Index != -1)
				{
					partyCharacterVMList2.Insert(command.Index, partyCharacterVM3);
				}
				else
				{
					partyCharacterVMList2.Add(partyCharacterVM3);
				}
				if (!partyCharacterVMList.Contains(this.CurrentCharacter))
				{
					this.SetSelectedCharacter(partyCharacterVM3);
				}
				partyCharacterVM3.IsLocked = partyCharacterVM3.Side == PartyScreenLogic.PartyRosterSide.Right && this.IsTroopLocked(partyCharacterVM3.Troop, partyCharacterVM3.IsPrisoner);
				partyCharacterVM3.IsSelected = partyCharacterVM != null && partyCharacterVM.IsSelected;
				if (partyCharacterVM3.IsSelected)
				{
					this.ScrollCharacterId = partyCharacterVM3.Character.StringId;
					this.IsScrollTargetPrisoner = partyCharacterVM3.IsPrisoner;
					this.ScrollToCharacter = true;
				}
			}
			this.CurrentCharacter = this.FindCharacterVM(command.Character, partyRosterSide, command.Type);
			PartyCompositionVM compositionForList = this.GetCompositionForList(partyCharacterVMList);
			if (compositionForList != null)
			{
				compositionForList.OnTroopRemoved(command.Character.DefaultFormationClass, command.TotalNumber);
			}
			PartyCompositionVM compositionForList2 = this.GetCompositionForList(partyCharacterVMList2);
			if (compositionForList2 != null)
			{
				compositionForList2.OnTroopAdded(command.Character.DefaultFormationClass, command.TotalNumber);
			}
			this.CurrentCharacter.UpdateTradeData();
			this.CurrentCharacter.OnTransferred();
			this.CurrentCharacter.ThrowOnPropertyChanged();
			this.RefreshTopInformation();
			this.RefreshPartyInformation();
			Game.Current.EventManager.TriggerEvent<PlayerMoveTroopEvent>(new PlayerMoveTroopEvent(command.Character, command.RosterSide, (command.RosterSide + 1) % (PartyScreenLogic.PartyRosterSide)2, command.TotalNumber, command.Type == PartyScreenLogic.TroopType.Prisoner));
		}

		// Token: 0x06000395 RID: 917 RVA: 0x000182E0 File Offset: 0x000164E0
		private void ShiftTroop(PartyScreenLogic.PartyCommand command)
		{
			MBBindingList<PartyCharacterVM> partyCharacterVMList = this.GetPartyCharacterVMList(command.RosterSide, command.Type);
			if (command.Index < 0)
			{
				return;
			}
			PartyCharacterVM currentCharacter = this.CurrentCharacter;
			int num = partyCharacterVMList.IndexOf(this.CurrentCharacter);
			int num2 = -1;
			partyCharacterVMList.Remove(this.CurrentCharacter);
			if (partyCharacterVMList.Count < command.Index)
			{
				partyCharacterVMList.Add(currentCharacter);
			}
			else
			{
				num2 = ((num < command.Index) ? (command.Index - 1) : command.Index);
				partyCharacterVMList.Insert(num2, currentCharacter);
			}
			this.SetSelectedCharacter(currentCharacter);
			if (num != num2)
			{
				bool isAscendingSortForSide = this.PartyScreenLogic.GetIsAscendingSortForSide(command.RosterSide);
				this.OnSortTroops(command.RosterSide, PartyScreenLogic.TroopSortType.Custom, isAscendingSortForSide);
			}
			this.CurrentCharacter.ThrowOnPropertyChanged();
			this.RefreshTopInformation();
			this.RefreshPartyInformation();
		}

		// Token: 0x06000396 RID: 918 RVA: 0x000183AA File Offset: 0x000165AA
		public void OnUpgradePopUpClosed(bool isCancelled)
		{
			if (!isCancelled)
			{
				this.UpdateTroopManagerPopUpCounts();
			}
			Game.Current.EventManager.TriggerEvent<PlayerToggledUpgradePopupEvent>(new PlayerToggledUpgradePopupEvent(false));
			PartyRecruitTroopVM recruitPopUp = this.RecruitPopUp;
			this.IsAnyPopUpOpen = recruitPopUp != null && recruitPopUp.IsOpen;
		}

		// Token: 0x06000397 RID: 919 RVA: 0x000183E2 File Offset: 0x000165E2
		public void OnRecruitPopUpClosed(bool isCancelled)
		{
			if (!isCancelled)
			{
				this.UpdateTroopManagerPopUpCounts();
			}
			PartyUpgradeTroopVM upgradePopUp = this.UpgradePopUp;
			this.IsAnyPopUpOpen = upgradePopUp != null && upgradePopUp.IsOpen;
		}

		// Token: 0x06000398 RID: 920 RVA: 0x00018408 File Offset: 0x00016608
		private void UpdateTroopManagerPopUpCounts()
		{
			if (this.UpgradePopUp.IsOpen || this.RecruitPopUp.IsOpen)
			{
				return;
			}
			this.RecruitableTroopCount = 0;
			this.UpgradableTroopCount = 0;
			this.MainPartyPrisoners.ApplyActionOnAllItems(delegate(PartyCharacterVM x)
			{
				this.RecruitableTroopCount += x.NumOfRecruitablePrisoners;
			});
			this.MainPartyTroops.ApplyActionOnAllItems(delegate(PartyCharacterVM x)
			{
				this.UpgradableTroopCount += x.NumOfUpgradeableTroops;
			});
			this.IsRecruitPopUpDisabled = !this.ArePrisonersRelevantOnCurrentMode || this.RecruitableTroopCount == 0 || this.PartyScreenLogic.IsTroopUpgradesDisabled;
			this.IsUpgradePopUpDisabled = !this.AreMembersRelevantOnCurrentMode || this.UpgradableTroopCount == 0 || this.PartyScreenLogic.IsTroopUpgradesDisabled;
			this.RecruitPopUp.UpdateOpenButtonHint(this.IsRecruitPopUpDisabled, !this.ArePrisonersRelevantOnCurrentMode, this.PartyScreenLogic.IsTroopUpgradesDisabled);
			this.UpgradePopUp.UpdateOpenButtonHint(this.IsUpgradePopUpDisabled, !this.AreMembersRelevantOnCurrentMode, this.PartyScreenLogic.IsTroopUpgradesDisabled);
		}

		// Token: 0x06000399 RID: 921 RVA: 0x00018500 File Offset: 0x00016700
		private void UpgradeTroop(PartyScreenLogic.PartyCommand command)
		{
			int index = this.PartyScreenLogic.MemberRosters[(int)command.RosterSide].FindIndexOfTroop(command.Character.UpgradeTargets[command.UpgradeTarget]);
			PartyCharacterVM newCharacter = new PartyCharacterVM(this.PartyScreenLogic, this, this.PartyScreenLogic.MemberRosters[(int)command.RosterSide], index, command.Type, command.RosterSide, this.PartyScreenLogic.IsTroopTransferable(command.Type, this.PartyScreenLogic.MemberRosters[(int)command.RosterSide].GetCharacterAtIndex(index), (int)command.RosterSide));
			newCharacter.IsLocked = this.IsTroopLocked(newCharacter.Troop, false);
			MBBindingList<PartyCharacterVM> partyCharacterVMList = this.GetPartyCharacterVMList(command.RosterSide, command.Type);
			if (partyCharacterVMList.Contains(newCharacter))
			{
				PartyCharacterVM partyCharacterVM = partyCharacterVMList.First((PartyCharacterVM character) => character.Equals(newCharacter));
				partyCharacterVM.Troop = newCharacter.Troop;
				partyCharacterVM.ThrowOnPropertyChanged();
			}
			else
			{
				if (command.Index != -1)
				{
					partyCharacterVMList.Insert(command.Index, newCharacter);
				}
				else
				{
					partyCharacterVMList.Add(newCharacter);
				}
				newCharacter.ThrowOnPropertyChanged();
			}
			int num = -1;
			if (command.Type == PartyScreenLogic.TroopType.Member)
			{
				num = this.PartyScreenLogic.MemberRosters[(int)this.CurrentCharacter.Side].FindIndexOfTroop(this.CurrentCharacter.Character);
				if (num > 0)
				{
					this._currentCharacter.Troop = this.PartyScreenLogic.MemberRosters[(int)this.CurrentCharacter.Side].GetElementCopyAtIndex(num);
				}
			}
			else if (command.Type == PartyScreenLogic.TroopType.Prisoner)
			{
				num = this.PartyScreenLogic.MemberRosters[(int)this.CurrentCharacter.Side].FindIndexOfTroop(this.CurrentCharacter.Character);
				if (num > 0)
				{
					this._currentCharacter.Troop = this.PartyScreenLogic.PrisonerRosters[(int)this.CurrentCharacter.Side].GetElementCopyAtIndex(num);
				}
			}
			PartyCharacterVM currentCharacter = this.CurrentCharacter;
			if (num < 0)
			{
				this.UpgradePopUp.OnRanOutTroop(this.CurrentCharacter);
				partyCharacterVMList.Remove(this.CurrentCharacter);
				this.CurrentCharacter = newCharacter;
				MBInformationManager.HideInformations();
			}
			else
			{
				this.CurrentCharacter.InitializeUpgrades();
				this.CurrentCharacter.ThrowOnPropertyChanged();
			}
			PartyCompositionVM compositionForList = this.GetCompositionForList(partyCharacterVMList);
			if (compositionForList != null)
			{
				compositionForList.OnTroopRemoved(command.Character.DefaultFormationClass, command.TotalNumber);
			}
			PartyCompositionVM compositionForList2 = this.GetCompositionForList(partyCharacterVMList);
			if (compositionForList2 != null)
			{
				compositionForList2.OnTroopAdded(newCharacter.Character.DefaultFormationClass, command.TotalNumber);
			}
			this.UpdateAllTradeDatasOfCharacter((currentCharacter != null) ? currentCharacter.Character : null);
			PartyCharacterVM newCharacter2 = newCharacter;
			this.UpdateAllTradeDatasOfCharacter((newCharacter2 != null) ? newCharacter2.Character : null);
			Game.Current.EventManager.TriggerEvent<PlayerRequestUpgradeTroopEvent>(new PlayerRequestUpgradeTroopEvent(command.Character, command.Character.UpgradeTargets[command.UpgradeTarget], command.TotalNumber));
			this.RefreshTopInformation();
		}

		// Token: 0x0600039A RID: 922 RVA: 0x000187FC File Offset: 0x000169FC
		private void RecruitTroop(PartyScreenLogic.PartyCommand command)
		{
			int index = this.PartyScreenLogic.MemberRosters[(int)command.RosterSide].FindIndexOfTroop(command.Character);
			PartyCharacterVM newCharacter = new PartyCharacterVM(this.PartyScreenLogic, this, this.PartyScreenLogic.MemberRosters[(int)command.RosterSide], index, PartyScreenLogic.TroopType.Member, command.RosterSide, this.PartyScreenLogic.IsTroopTransferable(command.Type, this.PartyScreenLogic.MemberRosters[(int)command.RosterSide].GetCharacterAtIndex(index), (int)command.RosterSide));
			newCharacter.IsLocked = this.IsTroopLocked(newCharacter.Troop, false);
			MBBindingList<PartyCharacterVM> partyCharacterVMList = this.GetPartyCharacterVMList(command.RosterSide, PartyScreenLogic.TroopType.Member);
			MBBindingList<PartyCharacterVM> partyCharacterVMList2 = this.GetPartyCharacterVMList(command.RosterSide, PartyScreenLogic.TroopType.Prisoner);
			if (partyCharacterVMList.Contains(newCharacter))
			{
				PartyCharacterVM partyCharacterVM = partyCharacterVMList.First((PartyCharacterVM character) => character.Equals(newCharacter));
				partyCharacterVM.Troop = newCharacter.Troop;
				partyCharacterVM.ThrowOnPropertyChanged();
			}
			else
			{
				if (command.Index != -1)
				{
					partyCharacterVMList.Insert(command.Index, newCharacter);
				}
				else
				{
					partyCharacterVMList.Add(newCharacter);
				}
				newCharacter.ThrowOnPropertyChanged();
			}
			int num = -1;
			if (command.Type == PartyScreenLogic.TroopType.Prisoner)
			{
				num = this.PartyScreenLogic.PrisonerRosters[(int)this.CurrentCharacter.Side].FindIndexOfTroop(this.CurrentCharacter.Character);
				if (num >= 0)
				{
					this._currentCharacter.Troop = this.PartyScreenLogic.PrisonerRosters[(int)this.CurrentCharacter.Side].GetElementCopyAtIndex(num);
				}
			}
			else
			{
				Debug.FailedAssert("Players can only recruit prisoners", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Party\\PartyVM.cs", "RecruitTroop", 1105);
			}
			if (num < 0)
			{
				partyCharacterVMList2.Remove(this.CurrentCharacter);
				this.CurrentCharacter = newCharacter;
				MBInformationManager.HideInformations();
			}
			else
			{
				this.CurrentCharacter.InitializeUpgrades();
				this.CurrentCharacter.ThrowOnPropertyChanged();
			}
			PartyCompositionVM compositionForList = this.GetCompositionForList(partyCharacterVMList);
			if (compositionForList != null)
			{
				compositionForList.OnTroopAdded(command.Character.DefaultFormationClass, command.TotalNumber);
			}
			PartyCharacterVM currentCharacter = this.CurrentCharacter;
			if (currentCharacter != null)
			{
				currentCharacter.UpdateTradeData();
			}
			this.RefreshTopInformation();
			this.RefreshPartyInformation();
		}

		// Token: 0x0600039B RID: 923 RVA: 0x00018A28 File Offset: 0x00016C28
		private void ExecuteTroop(PartyScreenLogic.PartyCommand command)
		{
			this.PartyScreenLogic.MemberRosters[(int)command.RosterSide].FindIndexOfTroop(command.Character);
			MBBindingList<PartyCharacterVM> partyCharacterVMList = this.GetPartyCharacterVMList(command.RosterSide, PartyScreenLogic.TroopType.Member);
			MBBindingList<PartyCharacterVM> partyCharacterVMList2 = this.GetPartyCharacterVMList(command.RosterSide, PartyScreenLogic.TroopType.Prisoner);
			int num = -1;
			if (command.Type == PartyScreenLogic.TroopType.Prisoner)
			{
				num = this.PartyScreenLogic.PrisonerRosters[(int)this.CurrentCharacter.Side].FindIndexOfTroop(this.CurrentCharacter.Character);
				if (num >= 0)
				{
					this._currentCharacter.Troop = this.PartyScreenLogic.PrisonerRosters[(int)this.CurrentCharacter.Side].GetElementCopyAtIndex(num);
				}
			}
			else
			{
				Debug.FailedAssert("Players can only execute prisoners", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Party\\PartyVM.cs", "ExecuteTroop", 1145);
			}
			if (num < 0)
			{
				partyCharacterVMList2.Remove(this.CurrentCharacter);
				this.CurrentCharacter = partyCharacterVMList2.FirstOrDefault<PartyCharacterVM>() ?? partyCharacterVMList.FirstOrDefault<PartyCharacterVM>();
				MBInformationManager.HideInformations();
			}
			else
			{
				Debug.FailedAssert("The prisoner should have been removed from the prisoner roster after execution", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Party\\PartyVM.cs", "ExecuteTroop", 1156);
			}
			this.RefreshTopInformation();
			this.RefreshPartyInformation();
		}

		// Token: 0x0600039C RID: 924 RVA: 0x00018B3C File Offset: 0x00016D3C
		private void TransferAllTroops(PartyScreenLogic.PartyCommand command)
		{
			TroopRoster troopRoster = null;
			TroopRoster troopRoster2 = null;
			MBBindingList<PartyCharacterVM> mbbindingList = null;
			MBBindingList<PartyCharacterVM> mbbindingList2 = null;
			if (command.Type == PartyScreenLogic.TroopType.Member)
			{
				troopRoster = this.PartyScreenLogic.GetRoster(PartyScreenLogic.PartyRosterSide.Left, PartyScreenLogic.TroopType.Member);
				troopRoster2 = this.PartyScreenLogic.GetRoster(PartyScreenLogic.PartyRosterSide.Right, PartyScreenLogic.TroopType.Member);
				mbbindingList = this.OtherPartyTroops;
				mbbindingList2 = this.MainPartyTroops;
			}
			if (command.Type == PartyScreenLogic.TroopType.Prisoner)
			{
				troopRoster = this.PartyScreenLogic.GetRoster(PartyScreenLogic.PartyRosterSide.Left, PartyScreenLogic.TroopType.Prisoner);
				troopRoster2 = this.PartyScreenLogic.GetRoster(PartyScreenLogic.PartyRosterSide.Right, PartyScreenLogic.TroopType.Prisoner);
				mbbindingList = this.OtherPartyPrisoners;
				mbbindingList2 = this.MainPartyPrisoners;
			}
			mbbindingList.Clear();
			mbbindingList2.Clear();
			int side = 0;
			int side2 = 1;
			for (int i = 0; i < troopRoster.Count; i++)
			{
				CharacterObject characterAtIndex = troopRoster.GetCharacterAtIndex(i);
				bool isTroopTransferrable = this.PartyScreenLogic.IsTroopTransferable(command.Type, characterAtIndex, side);
				mbbindingList.Add(new PartyCharacterVM(this.PartyScreenLogic, this, troopRoster, i, command.Type, PartyScreenLogic.PartyRosterSide.Left, isTroopTransferrable));
			}
			for (int j = 0; j < troopRoster2.Count; j++)
			{
				CharacterObject characterAtIndex2 = troopRoster2.GetCharacterAtIndex(j);
				bool isTroopTransferrable2 = this.PartyScreenLogic.IsTroopTransferable(command.Type, characterAtIndex2, side2);
				mbbindingList2.Add(new PartyCharacterVM(this.PartyScreenLogic, this, troopRoster2, j, command.Type, PartyScreenLogic.PartyRosterSide.Right, isTroopTransferrable2));
			}
			this.OtherPartyComposition.RefreshCounts(this.OtherPartyTroops);
			this.MainPartyComposition.RefreshCounts(this.MainPartyTroops);
			this.RefreshTopInformation();
			this.RefreshPartyInformation();
		}

		// Token: 0x0600039D RID: 925 RVA: 0x00018CA0 File Offset: 0x00016EA0
		private void SortTroops(PartyScreenLogic.PartyCommand command)
		{
			if (command.SortType != PartyScreenLogic.TroopSortType.Custom)
			{
				PartyScreenLogic.TroopSortType activeSortTypeForSide = this.PartyScreenLogic.GetActiveSortTypeForSide(command.RosterSide);
				PartyVM.TroopVMComparer comparer = new PartyVM.TroopVMComparer(this.PartyScreenLogic.GetComparer(activeSortTypeForSide));
				if (command.RosterSide == PartyScreenLogic.PartyRosterSide.Left)
				{
					this.OtherPartyTroops.Sort(comparer);
					this.OtherPartyPrisoners.Sort(comparer);
				}
				else if (command.RosterSide == PartyScreenLogic.PartyRosterSide.Right)
				{
					this.MainPartyTroops.Sort(comparer);
					this.MainPartyPrisoners.Sort(comparer);
				}
			}
			if (command.RosterSide == PartyScreenLogic.PartyRosterSide.Left)
			{
				this.OtherPartySortController.IsAscending = command.IsSortAscending;
				this.OtherPartySortController.SelectSortType(command.SortType);
				return;
			}
			if (command.RosterSide == PartyScreenLogic.PartyRosterSide.Right)
			{
				this.MainPartySortController.IsAscending = command.IsSortAscending;
				this.MainPartySortController.SelectSortType(command.SortType);
			}
		}

		// Token: 0x0600039E RID: 926 RVA: 0x00018D72 File Offset: 0x00016F72
		public void ExecuteTransferAllMainTroops()
		{
			this.TransferAllCharacters(PartyScreenLogic.PartyRosterSide.Right, PartyScreenLogic.TroopType.Member);
			this.ExecuteRemoveZeroCounts();
		}

		// Token: 0x0600039F RID: 927 RVA: 0x00018D82 File Offset: 0x00016F82
		public void ExecuteTransferAllOtherTroops()
		{
			this.TransferAllCharacters(PartyScreenLogic.PartyRosterSide.Left, PartyScreenLogic.TroopType.Member);
			this.ExecuteRemoveZeroCounts();
		}

		// Token: 0x060003A0 RID: 928 RVA: 0x00018D92 File Offset: 0x00016F92
		public void ExecuteTransferAllMainPrisoners()
		{
			this.TransferAllCharacters(PartyScreenLogic.PartyRosterSide.Right, PartyScreenLogic.TroopType.Prisoner);
			this.ExecuteRemoveZeroCounts();
		}

		// Token: 0x060003A1 RID: 929 RVA: 0x00018DA2 File Offset: 0x00016FA2
		public void ExecuteTransferAllOtherPrisoners()
		{
			this.TransferAllCharacters(PartyScreenLogic.PartyRosterSide.Left, PartyScreenLogic.TroopType.Prisoner);
			this.ExecuteRemoveZeroCounts();
		}

		// Token: 0x060003A2 RID: 930 RVA: 0x00018DB2 File Offset: 0x00016FB2
		public void ExecuteOpenUpgradePopUp()
		{
			this.IsAnyPopUpOpen = true;
			this.UpgradePopUp.OpenPopUp();
			Game.Current.EventManager.TriggerEvent<PlayerToggledUpgradePopupEvent>(new PlayerToggledUpgradePopupEvent(true));
		}

		// Token: 0x060003A3 RID: 931 RVA: 0x00018DDB File Offset: 0x00016FDB
		public void ExecuteOpenRecruitPopUp()
		{
			this.IsAnyPopUpOpen = true;
			this.RecruitPopUp.OpenPopUp();
		}

		// Token: 0x060003A4 RID: 932 RVA: 0x00018DF0 File Offset: 0x00016FF0
		public void ExecuteUpgrade(PartyCharacterVM troop, int upgradeTargetType, int maxUpgradeCount)
		{
			this.CurrentCharacter = troop;
			if (this.CurrentCharacter.Side == PartyScreenLogic.PartyRosterSide.Right && this.CurrentCharacter.Type == PartyScreenLogic.TroopType.Member)
			{
				int number = 1;
				if (this.IsEntireStackModifierActive)
				{
					number = maxUpgradeCount;
				}
				else if (this.IsFiveStackModifierActive)
				{
					number = MathF.Min(maxUpgradeCount, 5);
				}
				PartyScreenLogic.PartyCommand partyCommand = new PartyScreenLogic.PartyCommand();
				int indexToInsertTroop = this.PartyScreenLogic.GetIndexToInsertTroop(this.CurrentCharacter.Side, this.CurrentCharacter.Type, this.CurrentCharacter.Troop);
				partyCommand.FillForUpgradeTroop(this.CurrentCharacter.Side, this.CurrentCharacter.Type, this.CurrentCharacter.Character, number, upgradeTargetType, indexToInsertTroop);
				this.PartyScreenLogic.AddCommand(partyCommand);
			}
		}

		// Token: 0x060003A5 RID: 933 RVA: 0x00018EB0 File Offset: 0x000170B0
		public void ExecuteRecruit(PartyCharacterVM character, bool recruitAll = false)
		{
			this.CurrentCharacter = character;
			if (this.PartyScreenLogic.IsPrisonerRecruitable(this.CurrentCharacter.Type, this.CurrentCharacter.Character, this.CurrentCharacter.Side))
			{
				int number = 1;
				if (this.IsEntireStackModifierActive || recruitAll)
				{
					number = this.CurrentCharacter.NumOfRecruitablePrisoners;
				}
				else if (this.IsFiveStackModifierActive)
				{
					number = MathF.Min(this.CurrentCharacter.NumOfRecruitablePrisoners, 5);
				}
				int indexToInsertTroop = this.PartyScreenLogic.GetIndexToInsertTroop(character.Side, character.Type, character.Troop);
				PartyScreenLogic.PartyCommand partyCommand = new PartyScreenLogic.PartyCommand();
				partyCommand.FillForRecruitTroop(this.CurrentCharacter.Side, this.CurrentCharacter.Type, this.CurrentCharacter.Character, number, indexToInsertTroop);
				this.PartyScreenLogic.AddCommand(partyCommand);
				this.CurrentCharacter.UpdateRecruitable();
				this.CurrentCharacter.UpdateTalkable();
			}
		}

		// Token: 0x060003A6 RID: 934 RVA: 0x00018F98 File Offset: 0x00017198
		public void ExecuteExecution()
		{
			if (this.PartyScreenLogic.IsExecutable(this.CurrentCharacter.Type, this.CurrentCharacter.Character, this.CurrentCharacter.Side))
			{
				PartyScreenLogic.PartyCommand partyCommand = new PartyScreenLogic.PartyCommand();
				partyCommand.FillForExecuteTroop(this.CurrentCharacter.Side, this.CurrentCharacter.Type, this.CurrentCharacter.Character);
				this.PartyScreenLogic.AddCommand(partyCommand);
			}
		}

		// Token: 0x060003A7 RID: 935 RVA: 0x0001900C File Offset: 0x0001720C
		public void ExecuteRemoveZeroCounts()
		{
			this.PartyScreenLogic.RemoveZeroCounts();
			List<PartyCharacterVM> list = this.OtherPartyTroops.ToList<PartyCharacterVM>();
			for (int i = list.Count - 1; i >= 0; i--)
			{
				if (list[i].Number == 0 && this.OtherPartyTroops.Count > i)
				{
					list[i].IsSelected = false;
					this.OtherPartyTroops.RemoveAt(i);
				}
			}
			List<PartyCharacterVM> list2 = this.OtherPartyPrisoners.ToList<PartyCharacterVM>();
			for (int j = list2.Count - 1; j >= 0; j--)
			{
				if (list2[j].Number == 0 && this.OtherPartyPrisoners.Count > j)
				{
					list2[j].IsSelected = false;
					this.OtherPartyPrisoners.RemoveAt(j);
				}
			}
			List<PartyCharacterVM> list3 = this.MainPartyTroops.ToList<PartyCharacterVM>();
			for (int k = list3.Count - 1; k >= 0; k--)
			{
				if (list3[k].Number == 0 && this.MainPartyTroops.Count > k)
				{
					list3[k].IsSelected = false;
					this.MainPartyTroops.RemoveAt(k);
				}
			}
			List<PartyCharacterVM> list4 = this.MainPartyPrisoners.ToList<PartyCharacterVM>();
			for (int l = list4.Count - 1; l >= 0; l--)
			{
				if (list4[l].Number == 0 && this.MainPartyPrisoners.Count > l)
				{
					list4[l].IsSelected = false;
					this.MainPartyPrisoners.RemoveAt(l);
				}
			}
		}

		// Token: 0x060003A8 RID: 936 RVA: 0x00019194 File Offset: 0x00017394
		private void TransferAllCharacters(PartyScreenLogic.PartyRosterSide rosterSide, PartyScreenLogic.TroopType type)
		{
			PartyScreenLogic.PartyCommand partyCommand = new PartyScreenLogic.PartyCommand();
			partyCommand.FillForTransferAllTroops(rosterSide, type);
			this.PartyScreenLogic.AddCommand(partyCommand);
		}

		// Token: 0x060003A9 RID: 937 RVA: 0x000191BC File Offset: 0x000173BC
		private void RefreshCurrentCharacterInformation()
		{
			HeroViewModel heroViewModel = new HeroViewModel(CharacterViewModel.StanceTypes.None);
			bool flag = this.CurrentCharacter.Character == CharacterObject.PlayerCharacter;
			this.CurrentCharacterWageLbl = "";
			if (this.CurrentCharacter.Type == PartyScreenLogic.TroopType.Member && !flag)
			{
				this.CurrentCharacterWageLbl = this.CurrentCharacter.Character.TroopWage.ToString();
			}
			this.CurrentCharacterLevelLbl = "-";
			if (this.CurrentCharacter.Type == PartyScreenLogic.TroopType.Member || this.CurrentCharacter.Type == PartyScreenLogic.TroopType.Prisoner)
			{
				this.CurrentCharacterLevelLbl = this.CurrentCharacter.Character.Level.ToString();
			}
			this.CurrentCharacter.InitializeUpgrades();
			if (this.CurrentCharacter.Character != null)
			{
				if (this.CurrentCharacter.Character.IsHero)
				{
					heroViewModel.FillFrom(this.CurrentCharacter.Character.HeroObject, -1, false, false);
				}
				else
				{
					string bannerCode = "";
					if (!this.CurrentCharacter.IsPrisoner)
					{
						if (this.CurrentCharacter.Side == PartyScreenLogic.PartyRosterSide.Left)
						{
							bannerCode = ((this.PartyScreenLogic.LeftOwnerParty != null && this.PartyScreenLogic.LeftOwnerParty.Banner != null) ? this.PartyScreenLogic.LeftOwnerParty.Banner.BannerCode : "");
						}
						else
						{
							bannerCode = ((this.PartyScreenLogic.RightOwnerParty != null && this.PartyScreenLogic.RightOwnerParty.Banner != null) ? this.PartyScreenLogic.RightOwnerParty.Banner.BannerCode : "");
						}
					}
					heroViewModel.FillFrom(this.CurrentCharacter.Character, this.CurrentCharacter.Character.StringId.GetDeterministicHashCode(), bannerCode);
				}
			}
			heroViewModel.SetEquipment(this.CurrentCharacter.Character.Equipment);
			if (!this.CurrentCharacter.IsPrisoner)
			{
				if (this.CurrentCharacter.Side == PartyScreenLogic.PartyRosterSide.Right && this.PartyScreenLogic.RightOwnerParty != null && this.PartyScreenLogic.RightOwnerParty.MapFaction != null)
				{
					CharacterViewModel characterViewModel = heroViewModel;
					PartyBase rightOwnerParty = this.PartyScreenLogic.RightOwnerParty;
					uint? num;
					if (rightOwnerParty == null)
					{
						num = null;
					}
					else
					{
						IFaction mapFaction = rightOwnerParty.MapFaction;
						num = ((mapFaction != null) ? new uint?(mapFaction.Color) : null);
					}
					characterViewModel.ArmorColor1 = num ?? 0U;
					CharacterViewModel characterViewModel2 = heroViewModel;
					PartyBase rightOwnerParty2 = this.PartyScreenLogic.RightOwnerParty;
					uint? num2;
					if (rightOwnerParty2 == null)
					{
						num2 = null;
					}
					else
					{
						IFaction mapFaction2 = rightOwnerParty2.MapFaction;
						num2 = ((mapFaction2 != null) ? new uint?(mapFaction2.Color2) : null);
					}
					characterViewModel2.ArmorColor2 = num2 ?? 0U;
				}
				else if (this.CurrentCharacter.Side == PartyScreenLogic.PartyRosterSide.Left && this.PartyScreenLogic.LeftOwnerParty != null && this.PartyScreenLogic.LeftOwnerParty.MapFaction != null)
				{
					CharacterViewModel characterViewModel3 = heroViewModel;
					PartyBase leftOwnerParty = this.PartyScreenLogic.LeftOwnerParty;
					uint? num3;
					if (leftOwnerParty == null)
					{
						num3 = null;
					}
					else
					{
						IFaction mapFaction3 = leftOwnerParty.MapFaction;
						num3 = ((mapFaction3 != null) ? new uint?(mapFaction3.Color) : null);
					}
					characterViewModel3.ArmorColor1 = num3 ?? 0U;
					CharacterViewModel characterViewModel4 = heroViewModel;
					PartyBase leftOwnerParty2 = this.PartyScreenLogic.LeftOwnerParty;
					uint? num4;
					if (leftOwnerParty2 == null)
					{
						num4 = null;
					}
					else
					{
						IFaction mapFaction4 = leftOwnerParty2.MapFaction;
						num4 = ((mapFaction4 != null) ? new uint?(mapFaction4.Color2) : null);
					}
					characterViewModel4.ArmorColor2 = num4 ?? 0U;
				}
			}
			this.IsCurrentCharacterFormationEnabled = !this.CurrentCharacter.IsMainHero && !this.CurrentCharacter.IsPrisoner && this.CurrentCharacter.Side > PartyScreenLogic.PartyRosterSide.Left;
			this.IsCurrentCharacterWageEnabled = !this.CurrentCharacter.IsMainHero && !this.CurrentCharacter.IsPrisoner;
			this.CurrentCharacterTier = CampaignUIHelper.GetCharacterTierData(this.CurrentCharacter.Character, true);
			this.SelectedCharacter = heroViewModel;
			this.CurrentCharacter.UpdateTalkable();
		}

		// Token: 0x060003AA RID: 938 RVA: 0x000195DC File Offset: 0x000177DC
		private void RefreshPartyInformation()
		{
			this.OtherPartyTroopsLbl = PartyVM.PopulatePartyListLabel(this.OtherPartyTroops, this.PartyScreenLogic.LeftPartyMembersSizeLimit);
			this.OtherPartyPrisonersLbl = PartyVM.PopulatePartyListLabel(this.OtherPartyPrisoners, this.PartyScreenLogic.LeftPartyPrisonersSizeLimit);
			this.MainPartyTroopsLbl = PartyVM.PopulatePartyListLabel(this.MainPartyTroops, this.PartyScreenLogic.RightPartyMembersSizeLimit);
			this.MainPartyPrisonersLbl = PartyVM.PopulatePartyListLabel(this.MainPartyPrisoners, this.PartyScreenLogic.RightPartyPrisonersSizeLimit);
			if (this.ShowQuestProgress)
			{
				this.QuestProgressCurrentCount = this.PartyScreenLogic.GetCurrentQuestCurrentCount(this.ArePrisonersRelevantOnCurrentMode, this.AreMembersRelevantOnCurrentMode);
			}
			this.IsMainTroopsLimitWarningEnabled = this.PartyScreenLogic.RightPartyMembersSizeLimit < this.PartyScreenLogic.MemberRosters[1].TotalManCount && this.AreMembersRelevantOnCurrentMode;
			this.IsOtherTroopsLimitWarningEnabled = (this._currentMode == PartyScreenHelper.PartyScreenMode.TroopsManage || this._currentMode == PartyScreenHelper.PartyScreenMode.QuestTroopManage) && this.PartyScreenLogic.LeftPartyMembersSizeLimit < this.PartyScreenLogic.MemberRosters[0].TotalManCount && this.ArePrisonersRelevantOnCurrentMode;
			this.IsMainPrisonersLimitWarningEnabled = this.PartyScreenLogic.RightPartyPrisonersSizeLimit < this.PartyScreenLogic.PrisonerRosters[1].TotalManCount && this.ArePrisonersRelevantOnCurrentMode;
			PartyVM.UpdateAnyTransferableTroops(this.MainPartyTroops, delegate(bool result)
			{
				this.IsMainTroopsHaveTransferableTroops = result;
			}, this.DismissAllTroopsInputKey);
			PartyVM.UpdateAnyTransferableTroops(this.MainPartyPrisoners, delegate(bool result)
			{
				this.IsMainPrisonersHaveTransferableTroops = result;
			}, this.DismissAllPrisonersInputKey);
			PartyVM.UpdateAnyTransferableTroops(this.OtherPartyTroops, delegate(bool result)
			{
				this.IsOtherTroopsHaveTransferableTroops = result;
			}, this.TakeAllTroopsInputKey);
			PartyVM.UpdateAnyTransferableTroops(this.OtherPartyPrisoners, delegate(bool result)
			{
				this.IsOtherPrisonersHaveTransferableTroops = result;
			}, this.TakeAllPrisonersInputKey);
		}

		// Token: 0x060003AB RID: 939 RVA: 0x00019790 File Offset: 0x00017990
		private void RefreshPrisonersRecruitable()
		{
			foreach (PartyCharacterVM partyCharacterVM in this.MainPartyPrisoners)
			{
				partyCharacterVM.UpdateRecruitable();
			}
		}

		// Token: 0x060003AC RID: 940 RVA: 0x000197DC File Offset: 0x000179DC
		private void RefreshTroopsUpgradeable()
		{
			foreach (PartyCharacterVM partyCharacterVM in this.MainPartyTroops)
			{
				partyCharacterVM.InitializeUpgrades();
			}
		}

		// Token: 0x060003AD RID: 941 RVA: 0x00019828 File Offset: 0x00017A28
		private static void UpdateAnyTransferableTroops(MBBindingList<PartyCharacterVM> partyList, Action<bool> setTransferableBoolean, InputKeyItemVM keyItem)
		{
			bool flag = false;
			for (int i = 0; i < partyList.Count; i++)
			{
				PartyCharacterVM partyCharacterVM = partyList[i];
				if (partyCharacterVM.Troop.Number > 0 && partyCharacterVM.IsTroopTransferrable)
				{
					flag = true;
					break;
				}
			}
			setTransferableBoolean(flag);
			bool? forcedVisibility = null;
			if (!flag)
			{
				forcedVisibility = new bool?(false);
			}
			if (keyItem != null)
			{
				keyItem.SetForcedVisibility(forcedVisibility);
			}
		}

		// Token: 0x060003AE RID: 942 RVA: 0x00019894 File Offset: 0x00017A94
		private static string PopulatePartyListLabel(MBBindingList<PartyCharacterVM> partyList, int limit = 0)
		{
			int num = partyList.Sum((PartyCharacterVM item) => MathF.Max(0, item.Number - item.WoundedCount));
			int num2 = partyList.Sum(delegate(PartyCharacterVM item)
			{
				if (item.Number < item.WoundedCount)
				{
					return 0;
				}
				return item.WoundedCount;
			});
			MBTextManager.SetTextVariable("COUNT", num);
			MBTextManager.SetTextVariable("WEAK_COUNT", num2);
			if (limit != 0)
			{
				MBTextManager.SetTextVariable("MAX_COUNT", limit);
				if (num2 > 0)
				{
					MBTextManager.SetTextVariable("PARTY_LIST_TAG", "", false);
					MBTextManager.SetTextVariable("WEAK_COUNT", num2);
					MBTextManager.SetTextVariable("TOTAL_COUNT", num + num2);
					return GameTexts.FindText("str_party_list_label_with_weak", null).ToString();
				}
				MBTextManager.SetTextVariable("PARTY_LIST_TAG", "", false);
				return GameTexts.FindText("str_party_list_label", null).ToString();
			}
			else
			{
				if (num2 > 0)
				{
					return GameTexts.FindText("str_party_list_label_with_weak_without_max", null).ToString();
				}
				return num.ToString();
			}
		}

		// Token: 0x060003AF RID: 943 RVA: 0x0001998C File Offset: 0x00017B8C
		public void ExecuteTalk()
		{
			if (!this.PartyScreenLogic.IsThereAnyChanges())
			{
				this.ExecuteOpenConversation();
				return;
			}
			if (this.PartyScreenLogic.IsDoneActive())
			{
				this.ExecuteRemoveZeroCounts();
				InformationManager.ShowInquiry(new InquiryData(new TextObject("{=pF0SqQxL}Apply Changes?", null).ToString(), new TextObject("{=6DuCoCc2}You need to confirm your changes in order to engage in a conversation.", null).ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), delegate()
				{
					if (this.PartyScreenLogic.DoneLogic(false))
					{
						this.ExecuteOpenConversation();
						return;
					}
					InformationManager.ShowInquiry(new InquiryData(new TextObject("{=1l4kpBDK}Failed to Apply Changes", null).ToString(), new TextObject("{=sFseX1Ka}Could not apply changes.", null).ToString(), true, false, GameTexts.FindText("str_ok", null).ToString(), string.Empty, null, null, "", 0f, null, null, null), false, false);
				}, null, "", 0f, null, null, null), false, false);
				return;
			}
			InformationManager.ShowInquiry(new InquiryData(new TextObject("{=kMAQndom}Reset Changes?", null).ToString(), new TextObject("{=XgkFpSdq}Cannot apply changes. You need reset your changes in order to engage in a conversation.", null).ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), delegate()
			{
				this.ExecuteReset();
				this.ExecuteOpenConversation();
			}, null, "", 0f, null, null, null), false, false);
		}

		// Token: 0x060003B0 RID: 944 RVA: 0x00019A94 File Offset: 0x00017C94
		private void ExecuteOpenConversation()
		{
			if (this.CurrentCharacter.Side == PartyScreenLogic.PartyRosterSide.Right && this.CurrentCharacter.Character != CharacterObject.PlayerCharacter)
			{
				LocationComplex locationComplex = LocationComplex.Current;
				Location location;
				if (locationComplex == null)
				{
					location = null;
				}
				else
				{
					LocationComplex locationComplex2 = LocationComplex.Current;
					location = locationComplex.GetLocationOfCharacter((locationComplex2 != null) ? locationComplex2.GetFirstLocationCharacterOfCharacter(this.CurrentCharacter.Character) : null);
				}
				Location location2 = location;
				if (location2 == null)
				{
					CampaignMission.OpenConversationMission(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false, false), new ConversationCharacterData(this.CurrentCharacter.Character, PartyBase.MainParty, false, false, false, this.CurrentCharacter.IsPrisoner, false, false), "", "", false);
				}
				else
				{
					PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(location2, null, this.CurrentCharacter.Character, null);
				}
				this.IsInConversation = true;
			}
		}

		// Token: 0x060003B1 RID: 945 RVA: 0x00019B68 File Offset: 0x00017D68
		public void ExecuteDone()
		{
			if (this.PartyScreenLogic.IsDoneActive())
			{
				this.ExecuteRemoveZeroCounts();
				if (this.PartyScreenLogic.IsThereAnyChanges() && (this.IsMainPrisonersLimitWarningEnabled || this.IsMainTroopsLimitWarningEnabled || this.IsOtherTroopsLimitWarningEnabled))
				{
					GameTexts.SetVariable("newline", "\n");
					string text = string.Empty;
					if (this.IsMainTroopsLimitWarningEnabled)
					{
						text = GameTexts.FindText("str_party_over_limit_troops", null).ToString();
					}
					else if (this.IsMainPrisonersLimitWarningEnabled)
					{
						text = GameTexts.FindText("str_party_over_limit_prisoners", null).ToString();
					}
					else if (this.IsOtherTroopsLimitWarningEnabled)
					{
						text = GameTexts.FindText("str_other_party_over_limit_troops", null).ToString();
					}
					InformationManager.ShowInquiry(new InquiryData(new TextObject("{=uJro3Bua}Over Limit", null).ToString(), text, true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), new Action(this.CloseScreenInternal), null, "", 0f, null, null, null), false, false);
					return;
				}
				if (this._currentMode == PartyScreenHelper.PartyScreenMode.Loot && ((this.IsOtherPrisonersHaveTransferableTroops && this.CanRightPartyTakeMorePrisoners) || (this.IsOtherTroopsHaveTransferableTroops && this.CanRightPartyTakeMoreTroops)))
				{
					InformationManager.ShowInquiry(new InquiryData("", GameTexts.FindText("str_leaving_troops_behind", null).ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), new Action(this.CloseScreenInternal), null, "", 0f, null, null, null), false, false);
					return;
				}
				this.CloseScreenInternal();
			}
		}

		// Token: 0x060003B2 RID: 946 RVA: 0x00019D01 File Offset: 0x00017F01
		private void CloseScreenInternal()
		{
			this.SaveSortState();
			this.SaveCharacterLockStates();
			PartyScreenHelper.CloseScreen(false, false);
		}

		// Token: 0x060003B3 RID: 947 RVA: 0x00019D16 File Offset: 0x00017F16
		public void ExecuteReset()
		{
			this.PartyScreenLogic.Reset(false);
			this.CurrentFocusedCharacter = null;
			this.CurrentFocusedUpgrade = null;
		}

		// Token: 0x060003B4 RID: 948 RVA: 0x00019D32 File Offset: 0x00017F32
		public void ExecuteResetAndCancel()
		{
			this.ExecuteReset();
			PartyScreenHelper.CloseScreen(false, true);
		}

		// Token: 0x060003B5 RID: 949 RVA: 0x00019D44 File Offset: 0x00017F44
		public void ExecuteCancel()
		{
			if (this.PartyScreenLogic.IsCancelActive())
			{
				if (this._currentMode == PartyScreenHelper.PartyScreenMode.Loot)
				{
					if (this.PartyScreenLogic.IsThereAnyChanges())
					{
						InformationManager.ShowInquiry(new InquiryData("", GameTexts.FindText("str_cancelling_changes", null).ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), new Action(this.ExecuteResetAndCancel), null, "", 0f, null, null, null), false, false);
						return;
					}
					if ((this.IsOtherPrisonersHaveTransferableTroops && this.CanRightPartyTakeMorePrisoners) || (this.IsOtherTroopsHaveTransferableTroops && this.CanRightPartyTakeMoreTroops))
					{
						InformationManager.ShowInquiry(new InquiryData("", GameTexts.FindText("str_leaving_troops_behind", null).ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), new Action(this.ExecuteResetAndCancel), null, "", 0f, null, null, null), false, false);
						return;
					}
					this.ExecuteResetAndCancel();
					return;
				}
				else
				{
					this.ExecuteResetAndCancel();
				}
			}
		}

		// Token: 0x060003B6 RID: 950 RVA: 0x00019E64 File Offset: 0x00018064
		[Conditional("DEBUG")]
		private void EnsureLogicRostersAreInSyncWithVMLists()
		{
			List<TroopRoster> list = new List<TroopRoster>
			{
				this.PartyScreenLogic.GetRoster(PartyScreenLogic.PartyRosterSide.Left, PartyScreenLogic.TroopType.Member),
				this.PartyScreenLogic.GetRoster(PartyScreenLogic.PartyRosterSide.Left, PartyScreenLogic.TroopType.Prisoner),
				this.PartyScreenLogic.GetRoster(PartyScreenLogic.PartyRosterSide.Right, PartyScreenLogic.TroopType.Member),
				this.PartyScreenLogic.GetRoster(PartyScreenLogic.PartyRosterSide.Right, PartyScreenLogic.TroopType.Prisoner)
			};
			List<MBBindingList<PartyCharacterVM>> list2 = new List<MBBindingList<PartyCharacterVM>>
			{
				this.GetPartyCharacterVMList(PartyScreenLogic.PartyRosterSide.Left, PartyScreenLogic.TroopType.Member),
				this.GetPartyCharacterVMList(PartyScreenLogic.PartyRosterSide.Left, PartyScreenLogic.TroopType.Prisoner),
				this.GetPartyCharacterVMList(PartyScreenLogic.PartyRosterSide.Right, PartyScreenLogic.TroopType.Member),
				this.GetPartyCharacterVMList(PartyScreenLogic.PartyRosterSide.Right, PartyScreenLogic.TroopType.Prisoner)
			};
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].Count != list2[i].Count)
				{
					Debug.FailedAssert("Logic and VM list counts do not match", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Party\\PartyVM.cs", "EnsureLogicRostersAreInSyncWithVMLists", 1817);
				}
				else
				{
					for (int j = 0; j < list[i].Count; j++)
					{
						if (list[i].GetCharacterAtIndex(j).StringId != list2[i][j].Character.StringId)
						{
							Debug.FailedAssert("Logic and VM rosters do not match", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Party\\PartyVM.cs", "EnsureLogicRostersAreInSyncWithVMLists", 1825);
							return;
						}
					}
				}
			}
		}

		// Token: 0x060003B7 RID: 951 RVA: 0x00019FB0 File Offset: 0x000181B0
		public override void OnFinalize()
		{
			base.OnFinalize();
			this.IsAnyPopUpOpen = false;
			this._selectedCharacter.OnFinalize();
			this._selectedCharacter = null;
			Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
			this.CancelInputKey.OnFinalize();
			this.DoneInputKey.OnFinalize();
			this.ResetInputKey.OnFinalize();
			this.TakeAllTroopsInputKey.OnFinalize();
			this.DismissAllTroopsInputKey.OnFinalize();
			this.TakeAllPrisonersInputKey.OnFinalize();
			this.DismissAllPrisonersInputKey.OnFinalize();
			InputKeyItemVM openUpgradePanelInputKey = this.OpenUpgradePanelInputKey;
			if (openUpgradePanelInputKey != null)
			{
				openUpgradePanelInputKey.OnFinalize();
			}
			InputKeyItemVM openRecruitPanelInputKey = this.OpenRecruitPanelInputKey;
			if (openRecruitPanelInputKey != null)
			{
				openRecruitPanelInputKey.OnFinalize();
			}
			PartyTradeVM.RemoveZeroCounts -= this.ExecuteRemoveZeroCounts;
			PartyCharacterVM.ProcessCharacterLock = null;
			PartyCharacterVM.SetSelected = null;
			PartyCharacterVM.OnShift = null;
			PartyCharacterVM.OnFocus = null;
			PartyCharacterVM.OnTransfer = null;
			this.UpgradePopUp.OnFinalize();
			this.RecruitPopUp.OnFinalize();
		}

		// Token: 0x170000EA RID: 234
		// (get) Token: 0x060003B8 RID: 952 RVA: 0x0001A0AB File Offset: 0x000182AB
		// (set) Token: 0x060003B9 RID: 953 RVA: 0x0001A0B3 File Offset: 0x000182B3
		[DataSourceProperty]
		public PartySortControllerVM OtherPartySortController
		{
			get
			{
				return this._otherPartySortController;
			}
			set
			{
				if (value != this._otherPartySortController)
				{
					this._otherPartySortController = value;
					base.OnPropertyChangedWithValue<PartySortControllerVM>(value, "OtherPartySortController");
				}
			}
		}

		// Token: 0x170000EB RID: 235
		// (get) Token: 0x060003BA RID: 954 RVA: 0x0001A0D1 File Offset: 0x000182D1
		// (set) Token: 0x060003BB RID: 955 RVA: 0x0001A0D9 File Offset: 0x000182D9
		[DataSourceProperty]
		public PartySortControllerVM MainPartySortController
		{
			get
			{
				return this._mainPartySortController;
			}
			set
			{
				if (value != this._mainPartySortController)
				{
					this._mainPartySortController = value;
					base.OnPropertyChangedWithValue<PartySortControllerVM>(value, "MainPartySortController");
				}
			}
		}

		// Token: 0x170000EC RID: 236
		// (get) Token: 0x060003BC RID: 956 RVA: 0x0001A0F7 File Offset: 0x000182F7
		// (set) Token: 0x060003BD RID: 957 RVA: 0x0001A0FF File Offset: 0x000182FF
		[DataSourceProperty]
		public PartyCompositionVM OtherPartyComposition
		{
			get
			{
				return this._otherPartyComposition;
			}
			set
			{
				if (value != this._otherPartyComposition)
				{
					this._otherPartyComposition = value;
					base.OnPropertyChangedWithValue<PartyCompositionVM>(value, "OtherPartyComposition");
				}
			}
		}

		// Token: 0x170000ED RID: 237
		// (get) Token: 0x060003BE RID: 958 RVA: 0x0001A11D File Offset: 0x0001831D
		// (set) Token: 0x060003BF RID: 959 RVA: 0x0001A125 File Offset: 0x00018325
		[DataSourceProperty]
		public PartyCompositionVM MainPartyComposition
		{
			get
			{
				return this._mainPartyComposition;
			}
			set
			{
				if (value != this._mainPartyComposition)
				{
					this._mainPartyComposition = value;
					base.OnPropertyChangedWithValue<PartyCompositionVM>(value, "MainPartyComposition");
				}
			}
		}

		// Token: 0x170000EE RID: 238
		// (get) Token: 0x060003C0 RID: 960 RVA: 0x0001A143 File Offset: 0x00018343
		// (set) Token: 0x060003C1 RID: 961 RVA: 0x0001A14B File Offset: 0x0001834B
		[DataSourceProperty]
		public PartyCharacterVM CurrentFocusedCharacter
		{
			get
			{
				return this._currentFocusedCharacter;
			}
			set
			{
				if (value != this._currentFocusedCharacter)
				{
					this._currentFocusedCharacter = value;
					base.OnPropertyChangedWithValue<PartyCharacterVM>(value, "CurrentFocusedCharacter");
				}
			}
		}

		// Token: 0x170000EF RID: 239
		// (get) Token: 0x060003C2 RID: 962 RVA: 0x0001A169 File Offset: 0x00018369
		// (set) Token: 0x060003C3 RID: 963 RVA: 0x0001A171 File Offset: 0x00018371
		[DataSourceProperty]
		public UpgradeTargetVM CurrentFocusedUpgrade
		{
			get
			{
				return this._currentFocusedUpgrade;
			}
			set
			{
				if (value != this._currentFocusedUpgrade)
				{
					this._currentFocusedUpgrade = value;
					base.OnPropertyChangedWithValue<UpgradeTargetVM>(value, "CurrentFocusedUpgrade");
				}
			}
		}

		// Token: 0x170000F0 RID: 240
		// (get) Token: 0x060003C4 RID: 964 RVA: 0x0001A18F File Offset: 0x0001838F
		// (set) Token: 0x060003C5 RID: 965 RVA: 0x0001A197 File Offset: 0x00018397
		[DataSourceProperty]
		public string HeaderLbl
		{
			get
			{
				return this._headerLbl;
			}
			set
			{
				if (value != this._headerLbl)
				{
					this._headerLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "HeaderLbl");
				}
			}
		}

		// Token: 0x170000F1 RID: 241
		// (get) Token: 0x060003C6 RID: 966 RVA: 0x0001A1BA File Offset: 0x000183BA
		// (set) Token: 0x060003C7 RID: 967 RVA: 0x0001A1C2 File Offset: 0x000183C2
		[DataSourceProperty]
		public string OtherPartyNameLbl
		{
			get
			{
				return this._otherPartyNameLbl;
			}
			set
			{
				if (value != this._otherPartyNameLbl)
				{
					this._otherPartyNameLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "OtherPartyNameLbl");
				}
			}
		}

		// Token: 0x170000F2 RID: 242
		// (get) Token: 0x060003C8 RID: 968 RVA: 0x0001A1E5 File Offset: 0x000183E5
		// (set) Token: 0x060003C9 RID: 969 RVA: 0x0001A1ED File Offset: 0x000183ED
		[DataSourceProperty]
		public MBBindingList<PartyCharacterVM> OtherPartyTroops
		{
			get
			{
				return this._otherPartyTroops;
			}
			set
			{
				if (value != this._otherPartyTroops)
				{
					this._otherPartyTroops = value;
					base.OnPropertyChangedWithValue<MBBindingList<PartyCharacterVM>>(value, "OtherPartyTroops");
				}
			}
		}

		// Token: 0x170000F3 RID: 243
		// (get) Token: 0x060003CA RID: 970 RVA: 0x0001A20B File Offset: 0x0001840B
		// (set) Token: 0x060003CB RID: 971 RVA: 0x0001A213 File Offset: 0x00018413
		[DataSourceProperty]
		public MBBindingList<PartyCharacterVM> OtherPartyPrisoners
		{
			get
			{
				return this._otherPartyPrisoners;
			}
			set
			{
				if (value != this._otherPartyPrisoners)
				{
					this._otherPartyPrisoners = value;
					base.OnPropertyChangedWithValue<MBBindingList<PartyCharacterVM>>(value, "OtherPartyPrisoners");
				}
			}
		}

		// Token: 0x170000F4 RID: 244
		// (get) Token: 0x060003CC RID: 972 RVA: 0x0001A231 File Offset: 0x00018431
		// (set) Token: 0x060003CD RID: 973 RVA: 0x0001A239 File Offset: 0x00018439
		[DataSourceProperty]
		public MBBindingList<PartyCharacterVM> MainPartyTroops
		{
			get
			{
				return this._mainPartyTroops;
			}
			set
			{
				if (value != this._mainPartyTroops)
				{
					this._mainPartyTroops = value;
					base.OnPropertyChangedWithValue<MBBindingList<PartyCharacterVM>>(value, "MainPartyTroops");
				}
			}
		}

		// Token: 0x170000F5 RID: 245
		// (get) Token: 0x060003CE RID: 974 RVA: 0x0001A257 File Offset: 0x00018457
		// (set) Token: 0x060003CF RID: 975 RVA: 0x0001A25F File Offset: 0x0001845F
		[DataSourceProperty]
		public MBBindingList<PartyCharacterVM> MainPartyPrisoners
		{
			get
			{
				return this._mainPartyPrisoners;
			}
			set
			{
				if (value != this._mainPartyPrisoners)
				{
					this._mainPartyPrisoners = value;
					base.OnPropertyChangedWithValue<MBBindingList<PartyCharacterVM>>(value, "MainPartyPrisoners");
				}
			}
		}

		// Token: 0x170000F6 RID: 246
		// (get) Token: 0x060003D0 RID: 976 RVA: 0x0001A27D File Offset: 0x0001847D
		// (set) Token: 0x060003D1 RID: 977 RVA: 0x0001A285 File Offset: 0x00018485
		[DataSourceProperty]
		public PartyUpgradeTroopVM UpgradePopUp
		{
			get
			{
				return this._upgradePopUp;
			}
			set
			{
				if (value != this._upgradePopUp)
				{
					this._upgradePopUp = value;
					base.OnPropertyChangedWithValue<PartyUpgradeTroopVM>(value, "UpgradePopUp");
				}
			}
		}

		// Token: 0x170000F7 RID: 247
		// (get) Token: 0x060003D2 RID: 978 RVA: 0x0001A2A3 File Offset: 0x000184A3
		// (set) Token: 0x060003D3 RID: 979 RVA: 0x0001A2AB File Offset: 0x000184AB
		[DataSourceProperty]
		public PartyRecruitTroopVM RecruitPopUp
		{
			get
			{
				return this._recruitPopUp;
			}
			set
			{
				if (value != this._recruitPopUp)
				{
					this._recruitPopUp = value;
					base.OnPropertyChangedWithValue<PartyRecruitTroopVM>(value, "RecruitPopUp");
				}
			}
		}

		// Token: 0x170000F8 RID: 248
		// (get) Token: 0x060003D4 RID: 980 RVA: 0x0001A2C9 File Offset: 0x000184C9
		// (set) Token: 0x060003D5 RID: 981 RVA: 0x0001A2D1 File Offset: 0x000184D1
		[DataSourceProperty]
		public HeroViewModel SelectedCharacter
		{
			get
			{
				return this._selectedCharacter;
			}
			set
			{
				if (value != this._selectedCharacter)
				{
					this._selectedCharacter = value;
					base.OnPropertyChangedWithValue<HeroViewModel>(value, "SelectedCharacter");
				}
			}
		}

		// Token: 0x170000F9 RID: 249
		// (get) Token: 0x060003D6 RID: 982 RVA: 0x0001A2EF File Offset: 0x000184EF
		// (set) Token: 0x060003D7 RID: 983 RVA: 0x0001A2F7 File Offset: 0x000184F7
		[DataSourceProperty]
		public string CurrentCharacterLevelLbl
		{
			get
			{
				return this._currentCharacterLevelLbl;
			}
			set
			{
				if (value != this._currentCharacterLevelLbl)
				{
					this._currentCharacterLevelLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentCharacterLevelLbl");
				}
			}
		}

		// Token: 0x170000FA RID: 250
		// (get) Token: 0x060003D8 RID: 984 RVA: 0x0001A31A File Offset: 0x0001851A
		// (set) Token: 0x060003D9 RID: 985 RVA: 0x0001A322 File Offset: 0x00018522
		[DataSourceProperty]
		public string CurrentCharacterWageLbl
		{
			get
			{
				return this._currentCharacterWageLbl;
			}
			set
			{
				if (value != this._currentCharacterWageLbl)
				{
					this._currentCharacterWageLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentCharacterWageLbl");
				}
			}
		}

		// Token: 0x170000FB RID: 251
		// (get) Token: 0x060003DA RID: 986 RVA: 0x0001A345 File Offset: 0x00018545
		// (set) Token: 0x060003DB RID: 987 RVA: 0x0001A34D File Offset: 0x0001854D
		[DataSourceProperty]
		public BasicTooltipViewModel TransferAllOtherTroopsHint
		{
			get
			{
				return this._transferAllOtherTroopsHint;
			}
			set
			{
				if (value != this._transferAllOtherTroopsHint)
				{
					this._transferAllOtherTroopsHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "TransferAllOtherTroopsHint");
				}
			}
		}

		// Token: 0x170000FC RID: 252
		// (get) Token: 0x060003DC RID: 988 RVA: 0x0001A36B File Offset: 0x0001856B
		// (set) Token: 0x060003DD RID: 989 RVA: 0x0001A373 File Offset: 0x00018573
		[DataSourceProperty]
		public BasicTooltipViewModel TransferAllOtherPrisonersHint
		{
			get
			{
				return this._transferAllOtherPrisonersHint;
			}
			set
			{
				if (value != this._transferAllOtherPrisonersHint)
				{
					this._transferAllOtherPrisonersHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "TransferAllOtherPrisonersHint");
				}
			}
		}

		// Token: 0x170000FD RID: 253
		// (get) Token: 0x060003DE RID: 990 RVA: 0x0001A391 File Offset: 0x00018591
		// (set) Token: 0x060003DF RID: 991 RVA: 0x0001A399 File Offset: 0x00018599
		[DataSourceProperty]
		public BasicTooltipViewModel TransferAllMainTroopsHint
		{
			get
			{
				return this._transferAllMainTroopsHint;
			}
			set
			{
				if (value != this._transferAllMainTroopsHint)
				{
					this._transferAllMainTroopsHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "TransferAllMainTroopsHint");
				}
			}
		}

		// Token: 0x170000FE RID: 254
		// (get) Token: 0x060003E0 RID: 992 RVA: 0x0001A3B7 File Offset: 0x000185B7
		// (set) Token: 0x060003E1 RID: 993 RVA: 0x0001A3BF File Offset: 0x000185BF
		[DataSourceProperty]
		public BasicTooltipViewModel TransferAllMainPrisonersHint
		{
			get
			{
				return this._transferAllMainPrisonersHint;
			}
			set
			{
				if (value != this._transferAllMainPrisonersHint)
				{
					this._transferAllMainPrisonersHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "TransferAllMainPrisonersHint");
				}
			}
		}

		// Token: 0x170000FF RID: 255
		// (get) Token: 0x060003E2 RID: 994 RVA: 0x0001A3DD File Offset: 0x000185DD
		// (set) Token: 0x060003E3 RID: 995 RVA: 0x0001A3E5 File Offset: 0x000185E5
		[DataSourceProperty]
		public StringItemWithHintVM CurrentCharacterTier
		{
			get
			{
				return this._currentCharacterTier;
			}
			set
			{
				if (value != this._currentCharacterTier)
				{
					this._currentCharacterTier = value;
					base.OnPropertyChangedWithValue<StringItemWithHintVM>(value, "CurrentCharacterTier");
				}
			}
		}

		// Token: 0x17000100 RID: 256
		// (get) Token: 0x060003E4 RID: 996 RVA: 0x0001A403 File Offset: 0x00018603
		// (set) Token: 0x060003E5 RID: 997 RVA: 0x0001A40B File Offset: 0x0001860B
		[DataSourceProperty]
		public HintViewModel ResetHint
		{
			get
			{
				return this._resetHint;
			}
			set
			{
				if (value != this._resetHint)
				{
					this._resetHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ResetHint");
				}
			}
		}

		// Token: 0x17000101 RID: 257
		// (get) Token: 0x060003E6 RID: 998 RVA: 0x0001A429 File Offset: 0x00018629
		// (set) Token: 0x060003E7 RID: 999 RVA: 0x0001A431 File Offset: 0x00018631
		[DataSourceProperty]
		public HintViewModel DoneHint
		{
			get
			{
				return this._doneHint;
			}
			set
			{
				if (value != this._doneHint)
				{
					this._doneHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "DoneHint");
				}
			}
		}

		// Token: 0x17000102 RID: 258
		// (get) Token: 0x060003E8 RID: 1000 RVA: 0x0001A44F File Offset: 0x0001864F
		// (set) Token: 0x060003E9 RID: 1001 RVA: 0x0001A457 File Offset: 0x00018657
		[DataSourceProperty]
		public string OtherPartyAccompanyingLbl
		{
			get
			{
				return this._otherPartyAccompanyingLbl;
			}
			set
			{
				if (value != this._otherPartyAccompanyingLbl)
				{
					this._otherPartyAccompanyingLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "OtherPartyAccompanyingLbl");
				}
			}
		}

		// Token: 0x17000103 RID: 259
		// (get) Token: 0x060003EA RID: 1002 RVA: 0x0001A47A File Offset: 0x0001867A
		// (set) Token: 0x060003EB RID: 1003 RVA: 0x0001A482 File Offset: 0x00018682
		[DataSourceProperty]
		public HintViewModel MoraleHint
		{
			get
			{
				return this._moraleHint;
			}
			set
			{
				if (value != this._moraleHint)
				{
					this._moraleHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "MoraleHint");
				}
			}
		}

		// Token: 0x17000104 RID: 260
		// (get) Token: 0x060003EC RID: 1004 RVA: 0x0001A4A0 File Offset: 0x000186A0
		// (set) Token: 0x060003ED RID: 1005 RVA: 0x0001A4A8 File Offset: 0x000186A8
		[DataSourceProperty]
		public HintViewModel TotalWageHint
		{
			get
			{
				return this._totalWageHint;
			}
			set
			{
				if (value != this._totalWageHint)
				{
					this._totalWageHint = value;
					base.OnPropertyChanged("Upgrade2Hint");
				}
			}
		}

		// Token: 0x17000105 RID: 261
		// (get) Token: 0x060003EE RID: 1006 RVA: 0x0001A4C5 File Offset: 0x000186C5
		// (set) Token: 0x060003EF RID: 1007 RVA: 0x0001A4CD File Offset: 0x000186CD
		[DataSourceProperty]
		public BasicTooltipViewModel SpeedHint
		{
			get
			{
				return this._speedHint;
			}
			set
			{
				if (value != this._speedHint)
				{
					this._speedHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "SpeedHint");
				}
			}
		}

		// Token: 0x17000106 RID: 262
		// (get) Token: 0x060003F0 RID: 1008 RVA: 0x0001A4EB File Offset: 0x000186EB
		// (set) Token: 0x060003F1 RID: 1009 RVA: 0x0001A4F3 File Offset: 0x000186F3
		[DataSourceProperty]
		public BasicTooltipViewModel MainPartyTroopSizeLimitHint
		{
			get
			{
				return this._mainPartyTroopSizeLimitHint;
			}
			set
			{
				if (value != this._mainPartyTroopSizeLimitHint)
				{
					this._mainPartyTroopSizeLimitHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "MainPartyTroopSizeLimitHint");
				}
			}
		}

		// Token: 0x17000107 RID: 263
		// (get) Token: 0x060003F2 RID: 1010 RVA: 0x0001A511 File Offset: 0x00018711
		// (set) Token: 0x060003F3 RID: 1011 RVA: 0x0001A519 File Offset: 0x00018719
		[DataSourceProperty]
		public BasicTooltipViewModel MainPartyPrisonerSizeLimitHint
		{
			get
			{
				return this._mainPartyPrisonerSizeLimitHint;
			}
			set
			{
				if (value != this._mainPartyPrisonerSizeLimitHint)
				{
					this._mainPartyPrisonerSizeLimitHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "MainPartyPrisonerSizeLimitHint");
				}
			}
		}

		// Token: 0x17000108 RID: 264
		// (get) Token: 0x060003F4 RID: 1012 RVA: 0x0001A537 File Offset: 0x00018737
		// (set) Token: 0x060003F5 RID: 1013 RVA: 0x0001A53F File Offset: 0x0001873F
		[DataSourceProperty]
		public BasicTooltipViewModel OtherPartyTroopSizeLimitHint
		{
			get
			{
				return this._otherPartyTroopSizeLimitHint;
			}
			set
			{
				if (value != this._otherPartyTroopSizeLimitHint)
				{
					this._otherPartyTroopSizeLimitHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "OtherPartyTroopSizeLimitHint");
				}
			}
		}

		// Token: 0x17000109 RID: 265
		// (get) Token: 0x060003F6 RID: 1014 RVA: 0x0001A55D File Offset: 0x0001875D
		// (set) Token: 0x060003F7 RID: 1015 RVA: 0x0001A565 File Offset: 0x00018765
		[DataSourceProperty]
		public BasicTooltipViewModel OtherPartyPrisonerSizeLimitHint
		{
			get
			{
				return this._otherPartyPrisonerSizeLimitHint;
			}
			set
			{
				if (value != this._otherPartyPrisonerSizeLimitHint)
				{
					this._otherPartyPrisonerSizeLimitHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "OtherPartyPrisonerSizeLimitHint");
				}
			}
		}

		// Token: 0x1700010A RID: 266
		// (get) Token: 0x060003F8 RID: 1016 RVA: 0x0001A583 File Offset: 0x00018783
		// (set) Token: 0x060003F9 RID: 1017 RVA: 0x0001A58B File Offset: 0x0001878B
		[DataSourceProperty]
		public BasicTooltipViewModel UsedHorsesHint
		{
			get
			{
				return this._usedHorsesHint;
			}
			set
			{
				if (value != this._usedHorsesHint)
				{
					this._usedHorsesHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "UsedHorsesHint");
				}
			}
		}

		// Token: 0x1700010B RID: 267
		// (get) Token: 0x060003FA RID: 1018 RVA: 0x0001A5A9 File Offset: 0x000187A9
		// (set) Token: 0x060003FB RID: 1019 RVA: 0x0001A5B1 File Offset: 0x000187B1
		[DataSourceProperty]
		public HintViewModel DenarHint
		{
			get
			{
				return this._denarHint;
			}
			set
			{
				if (value != this._denarHint)
				{
					this._denarHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "DenarHint");
				}
			}
		}

		// Token: 0x1700010C RID: 268
		// (get) Token: 0x060003FC RID: 1020 RVA: 0x0001A5CF File Offset: 0x000187CF
		// (set) Token: 0x060003FD RID: 1021 RVA: 0x0001A5D7 File Offset: 0x000187D7
		[DataSourceProperty]
		public HintViewModel LevelHint
		{
			get
			{
				return this._levelHint;
			}
			set
			{
				if (value != this._levelHint)
				{
					this._levelHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "LevelHint");
				}
			}
		}

		// Token: 0x1700010D RID: 269
		// (get) Token: 0x060003FE RID: 1022 RVA: 0x0001A5F5 File Offset: 0x000187F5
		// (set) Token: 0x060003FF RID: 1023 RVA: 0x0001A5FD File Offset: 0x000187FD
		[DataSourceProperty]
		public HintViewModel WageHint
		{
			get
			{
				return this._wageHint;
			}
			set
			{
				if (value != this._wageHint)
				{
					this._wageHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "WageHint");
				}
			}
		}

		// Token: 0x1700010E RID: 270
		// (get) Token: 0x06000400 RID: 1024 RVA: 0x0001A61B File Offset: 0x0001881B
		// (set) Token: 0x06000401 RID: 1025 RVA: 0x0001A623 File Offset: 0x00018823
		[DataSourceProperty]
		public string TitleLbl
		{
			get
			{
				return this._titleLbl;
			}
			set
			{
				if (value != this._titleLbl)
				{
					this._titleLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "TitleLbl");
				}
			}
		}

		// Token: 0x1700010F RID: 271
		// (get) Token: 0x06000402 RID: 1026 RVA: 0x0001A646 File Offset: 0x00018846
		// (set) Token: 0x06000403 RID: 1027 RVA: 0x0001A64E File Offset: 0x0001884E
		[DataSourceProperty]
		public string MainPartyNameLbl
		{
			get
			{
				return this._mainPartyNameLbl;
			}
			set
			{
				if (value != this._mainPartyNameLbl)
				{
					this._mainPartyNameLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "MainPartyNameLbl");
				}
			}
		}

		// Token: 0x17000110 RID: 272
		// (get) Token: 0x06000404 RID: 1028 RVA: 0x0001A671 File Offset: 0x00018871
		// (set) Token: 0x06000405 RID: 1029 RVA: 0x0001A679 File Offset: 0x00018879
		[DataSourceProperty]
		public HintViewModel FormationHint
		{
			get
			{
				return this._formationHint;
			}
			set
			{
				if (value != this._formationHint)
				{
					this._formationHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "FormationHint");
				}
			}
		}

		// Token: 0x17000111 RID: 273
		// (get) Token: 0x06000406 RID: 1030 RVA: 0x0001A697 File Offset: 0x00018897
		// (set) Token: 0x06000407 RID: 1031 RVA: 0x0001A69F File Offset: 0x0001889F
		[DataSourceProperty]
		public string TalkLbl
		{
			get
			{
				return this._talkLbl;
			}
			set
			{
				if (value != this._talkLbl)
				{
					this._talkLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "TalkLbl");
				}
			}
		}

		// Token: 0x17000112 RID: 274
		// (get) Token: 0x06000408 RID: 1032 RVA: 0x0001A6C2 File Offset: 0x000188C2
		// (set) Token: 0x06000409 RID: 1033 RVA: 0x0001A6CA File Offset: 0x000188CA
		[DataSourceProperty]
		public string InfoLbl
		{
			get
			{
				return this._infoLbl;
			}
			set
			{
				if (value != this._infoLbl)
				{
					this._infoLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "InfoLbl");
				}
			}
		}

		// Token: 0x17000113 RID: 275
		// (get) Token: 0x0600040A RID: 1034 RVA: 0x0001A6ED File Offset: 0x000188ED
		// (set) Token: 0x0600040B RID: 1035 RVA: 0x0001A6F5 File Offset: 0x000188F5
		[DataSourceProperty]
		public string CancelLbl
		{
			get
			{
				return this._cancelLbl;
			}
			set
			{
				if (value != this._cancelLbl)
				{
					this._cancelLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "CancelLbl");
				}
			}
		}

		// Token: 0x17000114 RID: 276
		// (get) Token: 0x0600040C RID: 1036 RVA: 0x0001A718 File Offset: 0x00018918
		// (set) Token: 0x0600040D RID: 1037 RVA: 0x0001A720 File Offset: 0x00018920
		[DataSourceProperty]
		public string DoneLbl
		{
			get
			{
				return this._doneLbl;
			}
			set
			{
				if (value != this._doneLbl)
				{
					this._doneLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "DoneLbl");
				}
			}
		}

		// Token: 0x17000115 RID: 277
		// (get) Token: 0x0600040E RID: 1038 RVA: 0x0001A743 File Offset: 0x00018943
		// (set) Token: 0x0600040F RID: 1039 RVA: 0x0001A74B File Offset: 0x0001894B
		[DataSourceProperty]
		public string TroopsLabel
		{
			get
			{
				return this._troopsLbl;
			}
			set
			{
				if (value != this._troopsLbl)
				{
					this._troopsLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "TroopsLabel");
				}
			}
		}

		// Token: 0x17000116 RID: 278
		// (get) Token: 0x06000410 RID: 1040 RVA: 0x0001A76E File Offset: 0x0001896E
		// (set) Token: 0x06000411 RID: 1041 RVA: 0x0001A776 File Offset: 0x00018976
		[DataSourceProperty]
		public string PrisonersLabel
		{
			get
			{
				return this._prisonersLabel;
			}
			set
			{
				if (value != this._prisonersLabel)
				{
					this._prisonersLabel = value;
					base.OnPropertyChangedWithValue<string>(value, "PrisonersLabel");
				}
			}
		}

		// Token: 0x17000117 RID: 279
		// (get) Token: 0x06000412 RID: 1042 RVA: 0x0001A799 File Offset: 0x00018999
		// (set) Token: 0x06000413 RID: 1043 RVA: 0x0001A7A1 File Offset: 0x000189A1
		[DataSourceProperty]
		public string MainPartyTotalGoldLbl
		{
			get
			{
				return this._mainPartyTotalGoldLbl;
			}
			set
			{
				if (value != this._mainPartyTotalGoldLbl)
				{
					this._mainPartyTotalGoldLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "MainPartyTotalGoldLbl");
				}
			}
		}

		// Token: 0x17000118 RID: 280
		// (get) Token: 0x06000414 RID: 1044 RVA: 0x0001A7C4 File Offset: 0x000189C4
		// (set) Token: 0x06000415 RID: 1045 RVA: 0x0001A7CC File Offset: 0x000189CC
		[DataSourceProperty]
		public string MainPartyTotalMoraleLbl
		{
			get
			{
				return this._mainPartyTotalMoraleLbl;
			}
			set
			{
				if (value != this._mainPartyTotalMoraleLbl)
				{
					this._mainPartyTotalMoraleLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "MainPartyTotalMoraleLbl");
				}
			}
		}

		// Token: 0x17000119 RID: 281
		// (get) Token: 0x06000416 RID: 1046 RVA: 0x0001A7EF File Offset: 0x000189EF
		// (set) Token: 0x06000417 RID: 1047 RVA: 0x0001A7F7 File Offset: 0x000189F7
		[DataSourceProperty]
		public string MainPartyTotalSpeedLbl
		{
			get
			{
				return this._mainPartyTotalSpeedLbl;
			}
			set
			{
				if (value != this._mainPartyTotalSpeedLbl)
				{
					this._mainPartyTotalSpeedLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "MainPartyTotalSpeedLbl");
				}
			}
		}

		// Token: 0x1700011A RID: 282
		// (get) Token: 0x06000418 RID: 1048 RVA: 0x0001A81A File Offset: 0x00018A1A
		// (set) Token: 0x06000419 RID: 1049 RVA: 0x0001A822 File Offset: 0x00018A22
		[DataSourceProperty]
		public string MainPartyTotalWeeklyCostLbl
		{
			get
			{
				return this._mainPartyTotalWeeklyCostLbl;
			}
			set
			{
				if (value != this._mainPartyTotalWeeklyCostLbl)
				{
					this._mainPartyTotalWeeklyCostLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "MainPartyTotalWeeklyCostLbl");
				}
			}
		}

		// Token: 0x1700011B RID: 283
		// (get) Token: 0x0600041A RID: 1050 RVA: 0x0001A845 File Offset: 0x00018A45
		// (set) Token: 0x0600041B RID: 1051 RVA: 0x0001A84D File Offset: 0x00018A4D
		[DataSourceProperty]
		public bool IsCurrentCharacterFormationEnabled
		{
			get
			{
				return this._isCurrentCharacterFormationEnabled;
			}
			set
			{
				if (value != this._isCurrentCharacterFormationEnabled)
				{
					this._isCurrentCharacterFormationEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsCurrentCharacterFormationEnabled");
				}
			}
		}

		// Token: 0x1700011C RID: 284
		// (get) Token: 0x0600041C RID: 1052 RVA: 0x0001A86B File Offset: 0x00018A6B
		// (set) Token: 0x0600041D RID: 1053 RVA: 0x0001A873 File Offset: 0x00018A73
		[DataSourceProperty]
		public bool IsCurrentCharacterWageEnabled
		{
			get
			{
				return this._isCurrentCharacterWageEnabled;
			}
			set
			{
				if (value != this._isCurrentCharacterWageEnabled)
				{
					this._isCurrentCharacterWageEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsCurrentCharacterWageEnabled");
				}
			}
		}

		// Token: 0x1700011D RID: 285
		// (get) Token: 0x0600041E RID: 1054 RVA: 0x0001A891 File Offset: 0x00018A91
		// (set) Token: 0x0600041F RID: 1055 RVA: 0x0001A899 File Offset: 0x00018A99
		[DataSourceProperty]
		public bool CanChooseRoles
		{
			get
			{
				return this._canChooseRoles;
			}
			set
			{
				if (value != this._canChooseRoles)
				{
					this._canChooseRoles = value;
					base.OnPropertyChangedWithValue(value, "CanChooseRoles");
				}
			}
		}

		// Token: 0x1700011E RID: 286
		// (get) Token: 0x06000420 RID: 1056 RVA: 0x0001A8B7 File Offset: 0x00018AB7
		// (set) Token: 0x06000421 RID: 1057 RVA: 0x0001A8BF File Offset: 0x00018ABF
		[DataSourceProperty]
		public string OtherPartyTroopsLbl
		{
			get
			{
				return this._otherPartyTroopsLbl;
			}
			set
			{
				if (value != this._otherPartyTroopsLbl)
				{
					this._otherPartyTroopsLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "OtherPartyTroopsLbl");
				}
			}
		}

		// Token: 0x1700011F RID: 287
		// (get) Token: 0x06000422 RID: 1058 RVA: 0x0001A8E2 File Offset: 0x00018AE2
		// (set) Token: 0x06000423 RID: 1059 RVA: 0x0001A8EA File Offset: 0x00018AEA
		[DataSourceProperty]
		public string OtherPartyPrisonersLbl
		{
			get
			{
				return this._otherPartyPrisonersLbl;
			}
			set
			{
				if (value != this._otherPartyPrisonersLbl)
				{
					this._otherPartyPrisonersLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "OtherPartyPrisonersLbl");
				}
			}
		}

		// Token: 0x17000120 RID: 288
		// (get) Token: 0x06000424 RID: 1060 RVA: 0x0001A90D File Offset: 0x00018B0D
		// (set) Token: 0x06000425 RID: 1061 RVA: 0x0001A915 File Offset: 0x00018B15
		[DataSourceProperty]
		public string MainPartyTroopsLbl
		{
			get
			{
				return this._mainPartyTroopsLbl;
			}
			set
			{
				if (value != this._mainPartyTroopsLbl)
				{
					this._mainPartyTroopsLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "MainPartyTroopsLbl");
				}
			}
		}

		// Token: 0x17000121 RID: 289
		// (get) Token: 0x06000426 RID: 1062 RVA: 0x0001A938 File Offset: 0x00018B38
		// (set) Token: 0x06000427 RID: 1063 RVA: 0x0001A940 File Offset: 0x00018B40
		[DataSourceProperty]
		public string MainPartyPrisonersLbl
		{
			get
			{
				return this._mainPartyPrisonersLbl;
			}
			set
			{
				if (value != this._mainPartyPrisonersLbl)
				{
					this._mainPartyPrisonersLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "MainPartyPrisonersLbl");
				}
			}
		}

		// Token: 0x17000122 RID: 290
		// (get) Token: 0x06000428 RID: 1064 RVA: 0x0001A963 File Offset: 0x00018B63
		// (set) Token: 0x06000429 RID: 1065 RVA: 0x0001A96B File Offset: 0x00018B6B
		[DataSourceProperty]
		public bool ShowQuestProgress
		{
			get
			{
				return this._showQuestProgress;
			}
			set
			{
				if (value != this._showQuestProgress)
				{
					this._showQuestProgress = value;
					base.OnPropertyChangedWithValue(value, "ShowQuestProgress");
				}
			}
		}

		// Token: 0x17000123 RID: 291
		// (get) Token: 0x0600042A RID: 1066 RVA: 0x0001A989 File Offset: 0x00018B89
		// (set) Token: 0x0600042B RID: 1067 RVA: 0x0001A991 File Offset: 0x00018B91
		[DataSourceProperty]
		public int QuestProgressRequiredCount
		{
			get
			{
				return this._questProgressRequiredCount;
			}
			set
			{
				if (value != this._questProgressRequiredCount)
				{
					this._questProgressRequiredCount = value;
					base.OnPropertyChangedWithValue(value, "QuestProgressRequiredCount");
				}
			}
		}

		// Token: 0x17000124 RID: 292
		// (get) Token: 0x0600042C RID: 1068 RVA: 0x0001A9AF File Offset: 0x00018BAF
		// (set) Token: 0x0600042D RID: 1069 RVA: 0x0001A9B7 File Offset: 0x00018BB7
		[DataSourceProperty]
		public int QuestProgressCurrentCount
		{
			get
			{
				return this._questProgressCurrentCount;
			}
			set
			{
				if (value != this._questProgressCurrentCount)
				{
					this._questProgressCurrentCount = value;
					base.OnPropertyChangedWithValue(value, "QuestProgressCurrentCount");
				}
			}
		}

		// Token: 0x17000125 RID: 293
		// (get) Token: 0x0600042E RID: 1070 RVA: 0x0001A9D5 File Offset: 0x00018BD5
		// (set) Token: 0x0600042F RID: 1071 RVA: 0x0001A9DD File Offset: 0x00018BDD
		[DataSourceProperty]
		public int UpgradableTroopCount
		{
			get
			{
				return this._upgradableTroopCount;
			}
			set
			{
				if (value != this._upgradableTroopCount)
				{
					this._upgradableTroopCount = value;
					base.OnPropertyChangedWithValue(value, "UpgradableTroopCount");
				}
			}
		}

		// Token: 0x17000126 RID: 294
		// (get) Token: 0x06000430 RID: 1072 RVA: 0x0001A9FB File Offset: 0x00018BFB
		// (set) Token: 0x06000431 RID: 1073 RVA: 0x0001AA03 File Offset: 0x00018C03
		[DataSourceProperty]
		public int RecruitableTroopCount
		{
			get
			{
				return this._recruitableTroopCount;
			}
			set
			{
				if (value != this._recruitableTroopCount)
				{
					this._recruitableTroopCount = value;
					base.OnPropertyChangedWithValue(value, "RecruitableTroopCount");
				}
			}
		}

		// Token: 0x17000127 RID: 295
		// (get) Token: 0x06000432 RID: 1074 RVA: 0x0001AA21 File Offset: 0x00018C21
		// (set) Token: 0x06000433 RID: 1075 RVA: 0x0001AA29 File Offset: 0x00018C29
		[DataSourceProperty]
		public bool IsDoneDisabled
		{
			get
			{
				return this._isDoneDisabled;
			}
			set
			{
				if (value != this._isDoneDisabled)
				{
					this._isDoneDisabled = value;
					base.OnPropertyChangedWithValue(value, "IsDoneDisabled");
				}
			}
		}

		// Token: 0x17000128 RID: 296
		// (get) Token: 0x06000434 RID: 1076 RVA: 0x0001AA47 File Offset: 0x00018C47
		// (set) Token: 0x06000435 RID: 1077 RVA: 0x0001AA4F File Offset: 0x00018C4F
		[DataSourceProperty]
		public bool IsUpgradePopUpDisabled
		{
			get
			{
				return this._isUpgradePopUpDisabled;
			}
			set
			{
				if (value != this._isUpgradePopUpDisabled)
				{
					this._isUpgradePopUpDisabled = value;
					base.OnPropertyChangedWithValue(value, "IsUpgradePopUpDisabled");
				}
			}
		}

		// Token: 0x17000129 RID: 297
		// (get) Token: 0x06000436 RID: 1078 RVA: 0x0001AA6D File Offset: 0x00018C6D
		// (set) Token: 0x06000437 RID: 1079 RVA: 0x0001AA75 File Offset: 0x00018C75
		[DataSourceProperty]
		public bool IsRecruitPopUpDisabled
		{
			get
			{
				return this._isRecruitPopUpDisabled;
			}
			set
			{
				if (value != this._isRecruitPopUpDisabled)
				{
					this._isRecruitPopUpDisabled = value;
					base.OnPropertyChangedWithValue(value, "IsRecruitPopUpDisabled");
				}
			}
		}

		// Token: 0x1700012A RID: 298
		// (get) Token: 0x06000438 RID: 1080 RVA: 0x0001AA93 File Offset: 0x00018C93
		// (set) Token: 0x06000439 RID: 1081 RVA: 0x0001AA9B File Offset: 0x00018C9B
		[DataSourceProperty]
		public bool IsMainPrisonersLimitWarningEnabled
		{
			get
			{
				return this._isMainPrisonersLimitWarningEnabled;
			}
			set
			{
				if (value != this._isMainPrisonersLimitWarningEnabled)
				{
					this._isMainPrisonersLimitWarningEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsMainPrisonersLimitWarningEnabled");
				}
			}
		}

		// Token: 0x1700012B RID: 299
		// (get) Token: 0x0600043A RID: 1082 RVA: 0x0001AAB9 File Offset: 0x00018CB9
		// (set) Token: 0x0600043B RID: 1083 RVA: 0x0001AAC1 File Offset: 0x00018CC1
		[DataSourceProperty]
		public bool IsMainTroopsLimitWarningEnabled
		{
			get
			{
				return this._isMainTroopsLimitWarningEnabled;
			}
			set
			{
				if (value != this._isMainTroopsLimitWarningEnabled)
				{
					this._isMainTroopsLimitWarningEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsMainTroopsLimitWarningEnabled");
				}
			}
		}

		// Token: 0x1700012C RID: 300
		// (get) Token: 0x0600043C RID: 1084 RVA: 0x0001AADF File Offset: 0x00018CDF
		// (set) Token: 0x0600043D RID: 1085 RVA: 0x0001AAE7 File Offset: 0x00018CE7
		[DataSourceProperty]
		public bool IsOtherPrisonersLimitWarningEnabled
		{
			get
			{
				return this._isOtherPrisonersLimitWarningEnabled;
			}
			set
			{
				if (value != this._isOtherPrisonersLimitWarningEnabled)
				{
					this._isOtherPrisonersLimitWarningEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsOtherPrisonersLimitWarningEnabled");
				}
			}
		}

		// Token: 0x1700012D RID: 301
		// (get) Token: 0x0600043E RID: 1086 RVA: 0x0001AB05 File Offset: 0x00018D05
		// (set) Token: 0x0600043F RID: 1087 RVA: 0x0001AB0D File Offset: 0x00018D0D
		[DataSourceProperty]
		public bool IsUpgradePopupButtonHighlightEnabled
		{
			get
			{
				return this._isUpgradePopupButtonHighlightEnabled;
			}
			set
			{
				if (value != this._isUpgradePopupButtonHighlightEnabled)
				{
					this._isUpgradePopupButtonHighlightEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsUpgradePopupButtonHighlightEnabled");
				}
			}
		}

		// Token: 0x1700012E RID: 302
		// (get) Token: 0x06000440 RID: 1088 RVA: 0x0001AB2B File Offset: 0x00018D2B
		// (set) Token: 0x06000441 RID: 1089 RVA: 0x0001AB33 File Offset: 0x00018D33
		[DataSourceProperty]
		public bool IsOtherTroopsLimitWarningEnabled
		{
			get
			{
				return this._isOtherTroopsLimitWarningEnabled;
			}
			set
			{
				if (value != this._isOtherTroopsLimitWarningEnabled)
				{
					this._isOtherTroopsLimitWarningEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsOtherTroopsLimitWarningEnabled");
				}
			}
		}

		// Token: 0x1700012F RID: 303
		// (get) Token: 0x06000442 RID: 1090 RVA: 0x0001AB51 File Offset: 0x00018D51
		// (set) Token: 0x06000443 RID: 1091 RVA: 0x0001AB59 File Offset: 0x00018D59
		[DataSourceProperty]
		public bool IsMainTroopsHaveTransferableTroops
		{
			get
			{
				return this._isMainTroopsHaveTransferableTroops;
			}
			set
			{
				if (value != this._isMainTroopsHaveTransferableTroops)
				{
					this._isMainTroopsHaveTransferableTroops = value;
					base.OnPropertyChangedWithValue(value, "IsMainTroopsHaveTransferableTroops");
				}
			}
		}

		// Token: 0x17000130 RID: 304
		// (get) Token: 0x06000444 RID: 1092 RVA: 0x0001AB77 File Offset: 0x00018D77
		// (set) Token: 0x06000445 RID: 1093 RVA: 0x0001AB7F File Offset: 0x00018D7F
		[DataSourceProperty]
		public bool IsMainPrisonersHaveTransferableTroops
		{
			get
			{
				return this._isMainPrisonersHaveTransferableTroops;
			}
			set
			{
				if (value != this._isMainPrisonersHaveTransferableTroops)
				{
					this._isMainPrisonersHaveTransferableTroops = value;
					base.OnPropertyChangedWithValue(value, "IsMainPrisonersHaveTransferableTroops");
				}
			}
		}

		// Token: 0x17000131 RID: 305
		// (get) Token: 0x06000446 RID: 1094 RVA: 0x0001AB9D File Offset: 0x00018D9D
		// (set) Token: 0x06000447 RID: 1095 RVA: 0x0001ABA5 File Offset: 0x00018DA5
		[DataSourceProperty]
		public bool IsOtherTroopsHaveTransferableTroops
		{
			get
			{
				return this._isOtherTroopsHaveTransferableTroops;
			}
			set
			{
				if (value != this._isOtherTroopsHaveTransferableTroops)
				{
					this._isOtherTroopsHaveTransferableTroops = value;
					base.OnPropertyChangedWithValue(value, "IsOtherTroopsHaveTransferableTroops");
				}
			}
		}

		// Token: 0x17000132 RID: 306
		// (get) Token: 0x06000448 RID: 1096 RVA: 0x0001ABC3 File Offset: 0x00018DC3
		// (set) Token: 0x06000449 RID: 1097 RVA: 0x0001ABCB File Offset: 0x00018DCB
		[DataSourceProperty]
		public bool IsOtherPrisonersHaveTransferableTroops
		{
			get
			{
				return this._isOtherPrisonersHaveTransferableTroops;
			}
			set
			{
				if (value != this._isOtherPrisonersHaveTransferableTroops)
				{
					this._isOtherPrisonersHaveTransferableTroops = value;
					base.OnPropertyChangedWithValue(value, "IsOtherPrisonersHaveTransferableTroops");
				}
			}
		}

		// Token: 0x17000133 RID: 307
		// (get) Token: 0x0600044A RID: 1098 RVA: 0x0001ABE9 File Offset: 0x00018DE9
		// (set) Token: 0x0600044B RID: 1099 RVA: 0x0001ABF1 File Offset: 0x00018DF1
		[DataSourceProperty]
		public bool IsCancelDisabled
		{
			get
			{
				return this._isCancelDisabled;
			}
			set
			{
				if (value != this._isCancelDisabled)
				{
					this._isCancelDisabled = value;
					base.OnPropertyChangedWithValue(value, "IsCancelDisabled");
				}
			}
		}

		// Token: 0x17000134 RID: 308
		// (get) Token: 0x0600044C RID: 1100 RVA: 0x0001AC0F File Offset: 0x00018E0F
		// (set) Token: 0x0600044D RID: 1101 RVA: 0x0001AC17 File Offset: 0x00018E17
		[DataSourceProperty]
		public bool AreMembersRelevantOnCurrentMode
		{
			get
			{
				return this._areMembersRelevantOnCurrentMode;
			}
			set
			{
				if (value != this._areMembersRelevantOnCurrentMode)
				{
					this._areMembersRelevantOnCurrentMode = value;
					base.OnPropertyChangedWithValue(value, "AreMembersRelevantOnCurrentMode");
				}
			}
		}

		// Token: 0x17000135 RID: 309
		// (get) Token: 0x0600044E RID: 1102 RVA: 0x0001AC35 File Offset: 0x00018E35
		// (set) Token: 0x0600044F RID: 1103 RVA: 0x0001AC3D File Offset: 0x00018E3D
		[DataSourceProperty]
		public bool ArePrisonersRelevantOnCurrentMode
		{
			get
			{
				return this._arePrisonersRelevantOnCurrentMode;
			}
			set
			{
				if (value != this._arePrisonersRelevantOnCurrentMode)
				{
					this._arePrisonersRelevantOnCurrentMode = value;
					base.OnPropertyChangedWithValue(value, "ArePrisonersRelevantOnCurrentMode");
				}
			}
		}

		// Token: 0x17000136 RID: 310
		// (get) Token: 0x06000450 RID: 1104 RVA: 0x0001AC5B File Offset: 0x00018E5B
		// (set) Token: 0x06000451 RID: 1105 RVA: 0x0001AC63 File Offset: 0x00018E63
		[DataSourceProperty]
		public string GoldChangeText
		{
			get
			{
				return this._goldChangeText;
			}
			set
			{
				if (value != this._goldChangeText)
				{
					this._goldChangeText = value;
					base.OnPropertyChangedWithValue<string>(value, "GoldChangeText");
				}
			}
		}

		// Token: 0x17000137 RID: 311
		// (get) Token: 0x06000452 RID: 1106 RVA: 0x0001AC86 File Offset: 0x00018E86
		// (set) Token: 0x06000453 RID: 1107 RVA: 0x0001AC8E File Offset: 0x00018E8E
		[DataSourceProperty]
		public string MoraleChangeText
		{
			get
			{
				return this._moraleChangeText;
			}
			set
			{
				if (value != this._moraleChangeText)
				{
					this._moraleChangeText = value;
					base.OnPropertyChangedWithValue<string>(value, "MoraleChangeText");
				}
			}
		}

		// Token: 0x17000138 RID: 312
		// (get) Token: 0x06000454 RID: 1108 RVA: 0x0001ACB1 File Offset: 0x00018EB1
		// (set) Token: 0x06000455 RID: 1109 RVA: 0x0001ACB9 File Offset: 0x00018EB9
		[DataSourceProperty]
		public string HorseChangeText
		{
			get
			{
				return this._horseChangeText;
			}
			set
			{
				if (value != this._horseChangeText)
				{
					this._horseChangeText = value;
					base.OnPropertyChangedWithValue<string>(value, "HorseChangeText");
				}
			}
		}

		// Token: 0x17000139 RID: 313
		// (get) Token: 0x06000456 RID: 1110 RVA: 0x0001ACDC File Offset: 0x00018EDC
		// (set) Token: 0x06000457 RID: 1111 RVA: 0x0001ACE4 File Offset: 0x00018EE4
		[DataSourceProperty]
		public string InfluenceChangeText
		{
			get
			{
				return this._influenceChangeText;
			}
			set
			{
				if (value != this._influenceChangeText)
				{
					this._influenceChangeText = value;
					base.OnPropertyChangedWithValue<string>(value, "InfluenceChangeText");
				}
			}
		}

		// Token: 0x1700013A RID: 314
		// (get) Token: 0x06000458 RID: 1112 RVA: 0x0001AD07 File Offset: 0x00018F07
		// (set) Token: 0x06000459 RID: 1113 RVA: 0x0001AD0F File Offset: 0x00018F0F
		[DataSourceProperty]
		public bool IsAnyPopUpOpen
		{
			get
			{
				return this._isAnyPopUpOpen;
			}
			set
			{
				if (value != this._isAnyPopUpOpen)
				{
					this._isAnyPopUpOpen = value;
					base.OnPropertyChangedWithValue(value, "IsAnyPopUpOpen");
				}
			}
		}

		// Token: 0x1700013B RID: 315
		// (get) Token: 0x0600045A RID: 1114 RVA: 0x0001AD2D File Offset: 0x00018F2D
		// (set) Token: 0x0600045B RID: 1115 RVA: 0x0001AD35 File Offset: 0x00018F35
		[DataSourceProperty]
		public bool ScrollToCharacter
		{
			get
			{
				return this._scrollToCharacter;
			}
			set
			{
				if (value != this._scrollToCharacter)
				{
					this._scrollToCharacter = value;
					base.OnPropertyChangedWithValue(value, "ScrollToCharacter");
				}
			}
		}

		// Token: 0x1700013C RID: 316
		// (get) Token: 0x0600045C RID: 1116 RVA: 0x0001AD53 File Offset: 0x00018F53
		// (set) Token: 0x0600045D RID: 1117 RVA: 0x0001AD5B File Offset: 0x00018F5B
		[DataSourceProperty]
		public bool IsScrollTargetPrisoner
		{
			get
			{
				return this._isScrollTargetPrisoner;
			}
			set
			{
				if (value != this._isScrollTargetPrisoner)
				{
					this._isScrollTargetPrisoner = value;
					base.OnPropertyChangedWithValue(value, "IsScrollTargetPrisoner");
				}
			}
		}

		// Token: 0x1700013D RID: 317
		// (get) Token: 0x0600045E RID: 1118 RVA: 0x0001AD79 File Offset: 0x00018F79
		// (set) Token: 0x0600045F RID: 1119 RVA: 0x0001AD81 File Offset: 0x00018F81
		[DataSourceProperty]
		public string ScrollCharacterId
		{
			get
			{
				return this._scrollCharacterId;
			}
			set
			{
				if (value != this._scrollCharacterId)
				{
					this._scrollCharacterId = value;
					base.OnPropertyChangedWithValue<string>(value, "ScrollCharacterId");
				}
			}
		}

		// Token: 0x06000460 RID: 1120 RVA: 0x0001ADA4 File Offset: 0x00018FA4
		private TextObject GetTransferAllOtherTroopsKeyText()
		{
			if (this.TakeAllTroopsInputKey == null || this._getKeyTextFromKeyId == null)
			{
				return TextObject.GetEmpty();
			}
			return this._getKeyTextFromKeyId(this.TakeAllTroopsInputKey.KeyID);
		}

		// Token: 0x06000461 RID: 1121 RVA: 0x0001ADD2 File Offset: 0x00018FD2
		private TextObject GetTransferAllMainTroopsKeyText()
		{
			if (this.DismissAllTroopsInputKey == null || this._getKeyTextFromKeyId == null)
			{
				return TextObject.GetEmpty();
			}
			return this._getKeyTextFromKeyId(this.DismissAllTroopsInputKey.KeyID);
		}

		// Token: 0x06000462 RID: 1122 RVA: 0x0001AE00 File Offset: 0x00019000
		private TextObject GetTransferAllOtherPrisonersKeyText()
		{
			if (this.TakeAllPrisonersInputKey == null || this._getKeyTextFromKeyId == null)
			{
				return TextObject.GetEmpty();
			}
			return this._getKeyTextFromKeyId(this.TakeAllPrisonersInputKey.KeyID);
		}

		// Token: 0x06000463 RID: 1123 RVA: 0x0001AE2E File Offset: 0x0001902E
		private TextObject GetTransferAllMainPrisonersKeyText()
		{
			if (this.DismissAllPrisonersInputKey == null || this._getKeyTextFromKeyId == null)
			{
				return TextObject.GetEmpty();
			}
			return this._getKeyTextFromKeyId(this.DismissAllPrisonersInputKey.KeyID);
		}

		// Token: 0x06000464 RID: 1124 RVA: 0x0001AE5C File Offset: 0x0001905C
		public void SetResetInputKey(HotKey hotkey)
		{
			this.ResetInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, true);
		}

		// Token: 0x06000465 RID: 1125 RVA: 0x0001AE6B File Offset: 0x0001906B
		public void SetCancelInputKey(HotKey hotKey)
		{
			this.CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
			this.UpgradePopUp.SetCancelInputKey(hotKey);
			this.RecruitPopUp.SetCancelInputKey(hotKey);
		}

		// Token: 0x06000466 RID: 1126 RVA: 0x0001AE92 File Offset: 0x00019092
		public void SetDoneInputKey(HotKey hotKey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
			this.UpgradePopUp.SetDoneInputKey(hotKey);
			this.RecruitPopUp.SetDoneInputKey(hotKey);
		}

		// Token: 0x06000467 RID: 1127 RVA: 0x0001AEBC File Offset: 0x000190BC
		public void SetTakeAllTroopsInputKey(HotKey hotKey)
		{
			this.TakeAllTroopsInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
			this.TransferAllOtherTroopsHint = new BasicTooltipViewModel(delegate()
			{
				GameTexts.SetVariable("TEXT", new TextObject("{=9WrJP0hD}Transfer All Troops", null));
				GameTexts.SetVariable("HOTKEY", this.GetTransferAllOtherTroopsKeyText());
				return GameTexts.FindText("str_hotkey_with_hint", null).ToString();
			});
			PartyVM.UpdateAnyTransferableTroops(this.OtherPartyTroops, delegate(bool result)
			{
				this.IsOtherTroopsHaveTransferableTroops = result;
			}, this.TakeAllTroopsInputKey);
		}

		// Token: 0x06000468 RID: 1128 RVA: 0x0001AF0C File Offset: 0x0001910C
		public void SetDismissAllTroopsInputKey(HotKey hotKey)
		{
			this.DismissAllTroopsInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
			this.TransferAllMainTroopsHint = new BasicTooltipViewModel(delegate()
			{
				GameTexts.SetVariable("TEXT", new TextObject("{=9WrJP0hD}Transfer All Troops", null));
				GameTexts.SetVariable("HOTKEY", this.GetTransferAllMainTroopsKeyText());
				return GameTexts.FindText("str_hotkey_with_hint", null).ToString();
			});
			PartyVM.UpdateAnyTransferableTroops(this.MainPartyTroops, delegate(bool result)
			{
				this.IsMainTroopsHaveTransferableTroops = result;
			}, this.DismissAllTroopsInputKey);
		}

		// Token: 0x06000469 RID: 1129 RVA: 0x0001AF5C File Offset: 0x0001915C
		public void SetTakeAllPrisonersInputKey(HotKey hotKey)
		{
			this.TakeAllPrisonersInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
			this.TransferAllOtherPrisonersHint = new BasicTooltipViewModel(delegate()
			{
				GameTexts.SetVariable("TEXT", new TextObject("{=qgK86eSo}Transfer All Prisoners", null));
				GameTexts.SetVariable("HOTKEY", this.GetTransferAllOtherPrisonersKeyText());
				return GameTexts.FindText("str_hotkey_with_hint", null).ToString();
			});
			PartyVM.UpdateAnyTransferableTroops(this.OtherPartyPrisoners, delegate(bool result)
			{
				this.IsOtherPrisonersHaveTransferableTroops = result;
			}, this.TakeAllPrisonersInputKey);
		}

		// Token: 0x0600046A RID: 1130 RVA: 0x0001AFAC File Offset: 0x000191AC
		public void SetDismissAllPrisonersInputKey(HotKey hotKey)
		{
			this.DismissAllPrisonersInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
			this.TransferAllMainPrisonersHint = new BasicTooltipViewModel(delegate()
			{
				GameTexts.SetVariable("TEXT", new TextObject("{=qgK86eSo}Transfer All Prisoners", null));
				GameTexts.SetVariable("HOTKEY", this.GetTransferAllMainPrisonersKeyText());
				return GameTexts.FindText("str_hotkey_with_hint", null).ToString();
			});
			PartyVM.UpdateAnyTransferableTroops(this.MainPartyPrisoners, delegate(bool result)
			{
				this.IsMainPrisonersHaveTransferableTroops = result;
			}, this.DismissAllPrisonersInputKey);
		}

		// Token: 0x0600046B RID: 1131 RVA: 0x0001AFFA File Offset: 0x000191FA
		public void SetOpenUpgradePanelInputKey(HotKey hotKey)
		{
			this.OpenUpgradePanelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x0600046C RID: 1132 RVA: 0x0001B009 File Offset: 0x00019209
		public void SetOpenRecruitPanelInputKey(HotKey hotKey)
		{
			this.OpenRecruitPanelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x0600046D RID: 1133 RVA: 0x0001B018 File Offset: 0x00019218
		public void SetGetKeyTextFromKeyIDFunc(Func<string, TextObject> getKeyTextFromKeyId)
		{
			this._getKeyTextFromKeyId = getKeyTextFromKeyId;
		}

		// Token: 0x1700013E RID: 318
		// (get) Token: 0x0600046E RID: 1134 RVA: 0x0001B021 File Offset: 0x00019221
		// (set) Token: 0x0600046F RID: 1135 RVA: 0x0001B029 File Offset: 0x00019229
		[DataSourceProperty]
		public InputKeyItemVM ResetInputKey
		{
			get
			{
				return this._resetInputKey;
			}
			set
			{
				if (value != this._resetInputKey)
				{
					this._resetInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "ResetInputKey");
				}
			}
		}

		// Token: 0x1700013F RID: 319
		// (get) Token: 0x06000470 RID: 1136 RVA: 0x0001B047 File Offset: 0x00019247
		// (set) Token: 0x06000471 RID: 1137 RVA: 0x0001B04F File Offset: 0x0001924F
		[DataSourceProperty]
		public InputKeyItemVM CancelInputKey
		{
			get
			{
				return this._cancelInputKey;
			}
			set
			{
				if (value != this._cancelInputKey)
				{
					this._cancelInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "CancelInputKey");
				}
			}
		}

		// Token: 0x17000140 RID: 320
		// (get) Token: 0x06000472 RID: 1138 RVA: 0x0001B06D File Offset: 0x0001926D
		// (set) Token: 0x06000473 RID: 1139 RVA: 0x0001B075 File Offset: 0x00019275
		[DataSourceProperty]
		public InputKeyItemVM DoneInputKey
		{
			get
			{
				return this._doneInputKey;
			}
			set
			{
				if (value != this._doneInputKey)
				{
					this._doneInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "DoneInputKey");
				}
			}
		}

		// Token: 0x17000141 RID: 321
		// (get) Token: 0x06000474 RID: 1140 RVA: 0x0001B093 File Offset: 0x00019293
		// (set) Token: 0x06000475 RID: 1141 RVA: 0x0001B09B File Offset: 0x0001929B
		[DataSourceProperty]
		public InputKeyItemVM TakeAllTroopsInputKey
		{
			get
			{
				return this._takeAllTroopsInputKey;
			}
			set
			{
				if (value != this._takeAllTroopsInputKey)
				{
					this._takeAllTroopsInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "TakeAllTroopsInputKey");
				}
			}
		}

		// Token: 0x17000142 RID: 322
		// (get) Token: 0x06000476 RID: 1142 RVA: 0x0001B0B9 File Offset: 0x000192B9
		// (set) Token: 0x06000477 RID: 1143 RVA: 0x0001B0C1 File Offset: 0x000192C1
		[DataSourceProperty]
		public InputKeyItemVM DismissAllTroopsInputKey
		{
			get
			{
				return this._dismissAllTroopsInputKey;
			}
			set
			{
				if (value != this._dismissAllTroopsInputKey)
				{
					this._dismissAllTroopsInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "DismissAllTroopsInputKey");
				}
			}
		}

		// Token: 0x17000143 RID: 323
		// (get) Token: 0x06000478 RID: 1144 RVA: 0x0001B0DF File Offset: 0x000192DF
		// (set) Token: 0x06000479 RID: 1145 RVA: 0x0001B0E7 File Offset: 0x000192E7
		[DataSourceProperty]
		public InputKeyItemVM TakeAllPrisonersInputKey
		{
			get
			{
				return this._takeAllPrisonersInputKey;
			}
			set
			{
				if (value != this._takeAllPrisonersInputKey)
				{
					this._takeAllPrisonersInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "TakeAllPrisonersInputKey");
				}
			}
		}

		// Token: 0x0600047A RID: 1146 RVA: 0x0001B108 File Offset: 0x00019308
		private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent obj)
		{
			if (obj.NewNotificationElementID != this._latestTutorialElementID)
			{
				if (this._latestTutorialElementID != null)
				{
					if (this._isUpgradePopupButtonHighlightApplied)
					{
						this.IsUpgradePopupButtonHighlightEnabled = false;
						this._isUpgradePopupButtonHighlightApplied = false;
					}
					if (this._isUpgradeButtonHighlightApplied)
					{
						this.SetUpgradeButtonsHighlightState(false);
						this._isUpgradeButtonHighlightApplied = false;
					}
					if (this._isRecruitButtonHighlightApplied)
					{
						this.SetRecruitButtonsHighlightState(false);
						this._isRecruitButtonHighlightApplied = false;
					}
					if (this._isTransferButtonHighlightApplied)
					{
						this.SetTransferButtonHighlightState(false, null);
						this._isTransferButtonHighlightApplied = false;
					}
				}
				this._latestTutorialElementID = obj.NewNotificationElementID;
				if (this._latestTutorialElementID != null)
				{
					if (!this._isUpgradePopupButtonHighlightApplied && this._latestTutorialElementID == this._upgradePopupButtonID)
					{
						this.IsUpgradePopupButtonHighlightEnabled = true;
						this._isUpgradePopupButtonHighlightApplied = true;
					}
					if (this._latestTutorialElementID == this._upgradeButtonID)
					{
						this.SetUpgradeButtonsHighlightState(true);
						this._isUpgradeButtonHighlightApplied = true;
					}
					if (!this._isRecruitButtonHighlightApplied && this._latestTutorialElementID == this._recruitButtonID)
					{
						this.SetRecruitButtonsHighlightState(true);
						this._isRecruitButtonHighlightApplied = true;
					}
					if (!this._isTransferButtonHighlightApplied && this._latestTutorialElementID == this._transferButtonOnlyOtherPrisonersID)
					{
						this.SetTransferButtonHighlightState(true, (PartyCharacterVM x) => x.Side == PartyScreenLogic.PartyRosterSide.Left && x.IsPrisoner && x.IsTroopTransferrable);
						this._isTransferButtonHighlightApplied = true;
					}
				}
			}
		}

		// Token: 0x17000144 RID: 324
		// (get) Token: 0x0600047B RID: 1147 RVA: 0x0001B25E File Offset: 0x0001945E
		// (set) Token: 0x0600047C RID: 1148 RVA: 0x0001B266 File Offset: 0x00019466
		[DataSourceProperty]
		public InputKeyItemVM DismissAllPrisonersInputKey
		{
			get
			{
				return this._dismissAllPrisonersInputKey;
			}
			set
			{
				if (value != this._dismissAllPrisonersInputKey)
				{
					this._dismissAllPrisonersInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "DismissAllPrisonersInputKey");
				}
			}
		}

		// Token: 0x0600047D RID: 1149 RVA: 0x0001B284 File Offset: 0x00019484
		private void SetUpgradeButtonsHighlightState(bool state)
		{
			MBBindingList<PartyCharacterVM> mainPartyTroops = this.MainPartyTroops;
			if (mainPartyTroops == null)
			{
				return;
			}
			mainPartyTroops.ApplyActionOnAllItems(delegate(PartyCharacterVM x)
			{
				x.SetIsUpgradeButtonHighlighted(state);
			});
		}

		// Token: 0x17000145 RID: 325
		// (get) Token: 0x0600047E RID: 1150 RVA: 0x0001B2BA File Offset: 0x000194BA
		// (set) Token: 0x0600047F RID: 1151 RVA: 0x0001B2C2 File Offset: 0x000194C2
		[DataSourceProperty]
		public InputKeyItemVM OpenUpgradePanelInputKey
		{
			get
			{
				return this._openUpgradePanelInputKey;
			}
			set
			{
				if (value != this._openUpgradePanelInputKey)
				{
					this._openUpgradePanelInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "OpenUpgradePanelInputKey");
				}
			}
		}

		// Token: 0x06000480 RID: 1152 RVA: 0x0001B2E0 File Offset: 0x000194E0
		private void SetRecruitButtonsHighlightState(bool state)
		{
			foreach (PartyCharacterVM partyCharacterVM in this.MainPartyTroops)
			{
				partyCharacterVM.IsRecruitButtonsHiglighted = state;
			}
		}

		// Token: 0x17000146 RID: 326
		// (get) Token: 0x06000481 RID: 1153 RVA: 0x0001B32C File Offset: 0x0001952C
		// (set) Token: 0x06000482 RID: 1154 RVA: 0x0001B334 File Offset: 0x00019534
		[DataSourceProperty]
		public InputKeyItemVM OpenRecruitPanelInputKey
		{
			get
			{
				return this._openRecruitPanelInputKey;
			}
			set
			{
				if (value != this._openRecruitPanelInputKey)
				{
					this._openRecruitPanelInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "OpenRecruitPanelInputKey");
				}
			}
		}

		// Token: 0x06000483 RID: 1155 RVA: 0x0001B354 File Offset: 0x00019554
		private void SetTransferButtonHighlightState(bool state, Func<PartyCharacterVM, bool> predicate)
		{
			foreach (PartyCharacterVM partyCharacterVM in this.MainPartyTroops)
			{
				if (predicate == null || predicate(partyCharacterVM))
				{
					partyCharacterVM.IsTransferButtonHiglighted = state;
				}
			}
			foreach (PartyCharacterVM partyCharacterVM2 in this.MainPartyPrisoners)
			{
				if (predicate == null || predicate(partyCharacterVM2))
				{
					partyCharacterVM2.IsTransferButtonHiglighted = state;
				}
			}
			foreach (PartyCharacterVM partyCharacterVM3 in this.OtherPartyTroops)
			{
				if (predicate == null || predicate(partyCharacterVM3))
				{
					partyCharacterVM3.IsTransferButtonHiglighted = state;
				}
			}
			foreach (PartyCharacterVM partyCharacterVM4 in this.OtherPartyPrisoners)
			{
				if (predicate == null || predicate(partyCharacterVM4))
				{
					partyCharacterVM4.IsTransferButtonHiglighted = state;
				}
			}
		}

		// Token: 0x0400018B RID: 395
		private readonly PartyScreenHelper.PartyScreenMode _currentMode;

		// Token: 0x0400018C RID: 396
		private readonly IViewDataTracker _viewDataTracker;

		// Token: 0x0400018D RID: 397
		public bool IsFiveStackModifierActive;

		// Token: 0x0400018E RID: 398
		public bool IsEntireStackModifierActive;

		// Token: 0x0400018F RID: 399
		private PartyCharacterVM _currentCharacter;

		// Token: 0x04000190 RID: 400
		private List<string> _lockedTroopIDs;

		// Token: 0x04000191 RID: 401
		private List<string> _lockedPrisonerIDs;

		// Token: 0x04000192 RID: 402
		private Func<string, TextObject> _getKeyTextFromKeyId;

		// Token: 0x04000193 RID: 403
		public bool IsInConversation;

		// Token: 0x04000194 RID: 404
		private List<Tuple<string, TextObject>> _formationNames;

		// Token: 0x04000195 RID: 405
		private PartySortControllerVM _otherPartySortController;

		// Token: 0x04000196 RID: 406
		private PartySortControllerVM _mainPartySortController;

		// Token: 0x04000197 RID: 407
		private PartyCompositionVM _otherPartyComposition;

		// Token: 0x04000198 RID: 408
		private PartyCompositionVM _mainPartyComposition;

		// Token: 0x04000199 RID: 409
		private PartyCharacterVM _currentFocusedCharacter;

		// Token: 0x0400019A RID: 410
		private UpgradeTargetVM _currentFocusedUpgrade;

		// Token: 0x0400019B RID: 411
		private HeroViewModel _selectedCharacter;

		// Token: 0x0400019C RID: 412
		private MBBindingList<PartyCharacterVM> _otherPartyTroops;

		// Token: 0x0400019D RID: 413
		private MBBindingList<PartyCharacterVM> _otherPartyPrisoners;

		// Token: 0x0400019E RID: 414
		private MBBindingList<PartyCharacterVM> _mainPartyTroops;

		// Token: 0x0400019F RID: 415
		private MBBindingList<PartyCharacterVM> _mainPartyPrisoners;

		// Token: 0x040001A0 RID: 416
		private PartyUpgradeTroopVM _upgradePopUp;

		// Token: 0x040001A1 RID: 417
		private PartyRecruitTroopVM _recruitPopUp;

		// Token: 0x040001A2 RID: 418
		private string _titleLbl;

		// Token: 0x040001A3 RID: 419
		private string _mainPartyNameLbl;

		// Token: 0x040001A4 RID: 420
		private string _otherPartyNameLbl;

		// Token: 0x040001A5 RID: 421
		private string _headerLbl;

		// Token: 0x040001A6 RID: 422
		private string _otherPartyAccompanyingLbl;

		// Token: 0x040001A7 RID: 423
		private string _talkLbl;

		// Token: 0x040001A8 RID: 424
		private string _infoLbl;

		// Token: 0x040001A9 RID: 425
		private string _cancelLbl;

		// Token: 0x040001AA RID: 426
		private string _doneLbl;

		// Token: 0x040001AB RID: 427
		private string _troopsLbl;

		// Token: 0x040001AC RID: 428
		private string _prisonersLabel;

		// Token: 0x040001AD RID: 429
		private string _mainPartyTotalGoldLbl;

		// Token: 0x040001AE RID: 430
		private string _mainPartyTotalMoraleLbl;

		// Token: 0x040001AF RID: 431
		private string _mainPartyTotalSpeedLbl;

		// Token: 0x040001B0 RID: 432
		private string _mainPartyTotalWeeklyCostLbl;

		// Token: 0x040001B1 RID: 433
		private string _currentCharacterWageLbl;

		// Token: 0x040001B2 RID: 434
		private string _currentCharacterLevelLbl;

		// Token: 0x040001B3 RID: 435
		private BasicTooltipViewModel _transferAllMainTroopsHint;

		// Token: 0x040001B4 RID: 436
		private BasicTooltipViewModel _transferAllMainPrisonersHint;

		// Token: 0x040001B5 RID: 437
		private BasicTooltipViewModel _transferAllOtherTroopsHint;

		// Token: 0x040001B6 RID: 438
		private BasicTooltipViewModel _transferAllOtherPrisonersHint;

		// Token: 0x040001B7 RID: 439
		private HintViewModel _moraleHint;

		// Token: 0x040001B8 RID: 440
		private HintViewModel _doneHint;

		// Token: 0x040001B9 RID: 441
		private BasicTooltipViewModel _speedHint;

		// Token: 0x040001BA RID: 442
		private BasicTooltipViewModel _mainPartyTroopSizeLimitHint;

		// Token: 0x040001BB RID: 443
		private BasicTooltipViewModel _mainPartyPrisonerSizeLimitHint;

		// Token: 0x040001BC RID: 444
		private BasicTooltipViewModel _otherPartyTroopSizeLimitHint;

		// Token: 0x040001BD RID: 445
		private BasicTooltipViewModel _otherPartyPrisonerSizeLimitHint;

		// Token: 0x040001BE RID: 446
		private BasicTooltipViewModel _usedHorsesHint;

		// Token: 0x040001BF RID: 447
		private HintViewModel _denarHint;

		// Token: 0x040001C0 RID: 448
		private HintViewModel _totalWageHint;

		// Token: 0x040001C1 RID: 449
		private HintViewModel _levelHint;

		// Token: 0x040001C2 RID: 450
		private HintViewModel _wageHint;

		// Token: 0x040001C3 RID: 451
		private HintViewModel _formationHint;

		// Token: 0x040001C4 RID: 452
		private HintViewModel _resetHint;

		// Token: 0x040001C5 RID: 453
		private StringItemWithHintVM _currentCharacterTier;

		// Token: 0x040001C6 RID: 454
		private bool _isCurrentCharacterFormationEnabled;

		// Token: 0x040001C7 RID: 455
		private bool _isCurrentCharacterWageEnabled;

		// Token: 0x040001C8 RID: 456
		private bool _arePrisonersRelevantOnCurrentMode;

		// Token: 0x040001C9 RID: 457
		private bool _areMembersRelevantOnCurrentMode;

		// Token: 0x040001CA RID: 458
		private bool _canChooseRoles;

		// Token: 0x040001CB RID: 459
		private string _otherPartyTroopsLbl;

		// Token: 0x040001CC RID: 460
		private string _otherPartyPrisonersLbl;

		// Token: 0x040001CD RID: 461
		private string _mainPartyTroopsLbl;

		// Token: 0x040001CE RID: 462
		private string _mainPartyPrisonersLbl;

		// Token: 0x040001CF RID: 463
		private string _goldChangeText;

		// Token: 0x040001D0 RID: 464
		private string _moraleChangeText;

		// Token: 0x040001D1 RID: 465
		private string _horseChangeText;

		// Token: 0x040001D2 RID: 466
		private string _influenceChangeText;

		// Token: 0x040001D3 RID: 467
		private bool _isMainTroopsLimitWarningEnabled;

		// Token: 0x040001D4 RID: 468
		private bool _isMainPrisonersLimitWarningEnabled;

		// Token: 0x040001D5 RID: 469
		private bool _isOtherTroopsLimitWarningEnabled;

		// Token: 0x040001D6 RID: 470
		private bool _isOtherPrisonersLimitWarningEnabled;

		// Token: 0x040001D7 RID: 471
		private bool _isMainTroopsHaveTransferableTroops;

		// Token: 0x040001D8 RID: 472
		private bool _isMainPrisonersHaveTransferableTroops;

		// Token: 0x040001D9 RID: 473
		private bool _isOtherTroopsHaveTransferableTroops;

		// Token: 0x040001DA RID: 474
		private bool _isOtherPrisonersHaveTransferableTroops;

		// Token: 0x040001DB RID: 475
		private bool _showQuestProgress;

		// Token: 0x040001DC RID: 476
		private bool _isUpgradePopupButtonHighlightEnabled;

		// Token: 0x040001DD RID: 477
		private int _questProgressRequiredCount;

		// Token: 0x040001DE RID: 478
		private int _questProgressCurrentCount;

		// Token: 0x040001DF RID: 479
		private int _upgradableTroopCount;

		// Token: 0x040001E0 RID: 480
		private int _recruitableTroopCount;

		// Token: 0x040001E1 RID: 481
		private bool _isDoneDisabled;

		// Token: 0x040001E2 RID: 482
		private bool _isCancelDisabled;

		// Token: 0x040001E3 RID: 483
		private bool _isUpgradePopUpDisabled;

		// Token: 0x040001E4 RID: 484
		private bool _isAnyPopUpOpen;

		// Token: 0x040001E5 RID: 485
		private bool _isRecruitPopUpDisabled;

		// Token: 0x040001E6 RID: 486
		private bool _scrollToCharacter;

		// Token: 0x040001E7 RID: 487
		private bool _isScrollTargetPrisoner;

		// Token: 0x040001E8 RID: 488
		private string _scrollCharacterId;

		// Token: 0x040001E9 RID: 489
		private InputKeyItemVM _resetInputKey;

		// Token: 0x040001EA RID: 490
		private InputKeyItemVM _cancelInputKey;

		// Token: 0x040001EB RID: 491
		private InputKeyItemVM _doneInputKey;

		// Token: 0x040001EC RID: 492
		private InputKeyItemVM _takeAllTroopsInputKey;

		// Token: 0x040001ED RID: 493
		private InputKeyItemVM _dismissAllTroopsInputKey;

		// Token: 0x040001EE RID: 494
		private InputKeyItemVM _takeAllPrisonersInputKey;

		// Token: 0x040001EF RID: 495
		private InputKeyItemVM _dismissAllPrisonersInputKey;

		// Token: 0x040001F0 RID: 496
		private InputKeyItemVM _openUpgradePanelInputKey;

		// Token: 0x040001F1 RID: 497
		private InputKeyItemVM _openRecruitPanelInputKey;

		// Token: 0x040001F2 RID: 498
		private readonly string _upgradePopupButtonID = "UpgradePopupButton";

		// Token: 0x040001F3 RID: 499
		private readonly string _upgradeButtonID = "UpgradeButton";

		// Token: 0x040001F4 RID: 500
		private readonly string _recruitButtonID = "RecruitButton";

		// Token: 0x040001F5 RID: 501
		private readonly string _transferButtonOnlyOtherPrisonersID = "TransferButtonOnlyOtherPrisoners";

		// Token: 0x040001F6 RID: 502
		private bool _isUpgradePopupButtonHighlightApplied;

		// Token: 0x040001F7 RID: 503
		private bool _isUpgradeButtonHighlightApplied;

		// Token: 0x040001F8 RID: 504
		private bool _isRecruitButtonHighlightApplied;

		// Token: 0x040001F9 RID: 505
		private bool _isTransferButtonHighlightApplied;

		// Token: 0x040001FA RID: 506
		private string _latestTutorialElementID;

		// Token: 0x02000197 RID: 407
		private class TroopVMComparer : IComparer<PartyCharacterVM>
		{
			// Token: 0x060022AE RID: 8878 RVA: 0x0007CDFD File Offset: 0x0007AFFD
			public TroopVMComparer(PartyScreenLogic.TroopComparer originalTroopComparer)
			{
				this._originalTroopComparer = originalTroopComparer;
			}

			// Token: 0x060022AF RID: 8879 RVA: 0x0007CE0C File Offset: 0x0007B00C
			public int Compare(PartyCharacterVM x, PartyCharacterVM y)
			{
				return this._originalTroopComparer.Compare(x.Troop, y.Troop);
			}

			// Token: 0x04001085 RID: 4229
			private readonly PartyScreenLogic.TroopComparer _originalTroopComparer;
		}
	}
}
