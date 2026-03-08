using System;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Party
{
	// Token: 0x02000027 RID: 39
	public class PartyCharacterVM : ViewModel
	{
		// Token: 0x17000095 RID: 149
		// (get) Token: 0x0600028E RID: 654 RVA: 0x0001418D File Offset: 0x0001238D
		// (set) Token: 0x0600028F RID: 655 RVA: 0x00014195 File Offset: 0x00012395
		public TroopRoster Troops { get; private set; }

		// Token: 0x17000096 RID: 150
		// (get) Token: 0x06000290 RID: 656 RVA: 0x0001419E File Offset: 0x0001239E
		// (set) Token: 0x06000291 RID: 657 RVA: 0x000141A6 File Offset: 0x000123A6
		public string StringId { get; private set; }

		// Token: 0x17000097 RID: 151
		// (get) Token: 0x06000292 RID: 658 RVA: 0x000141AF File Offset: 0x000123AF
		// (set) Token: 0x06000293 RID: 659 RVA: 0x000141B8 File Offset: 0x000123B8
		public TroopRosterElement Troop
		{
			get
			{
				return this._troop;
			}
			set
			{
				this._troop = value;
				this.Character = value.Character;
				this.TroopID = this.Character.StringId;
				this.CheckTransferAmountDefaultValue();
				this.TroopXPTooltip = new BasicTooltipViewModel(() => CampaignUIHelper.GetTroopXPTooltip(value));
				this.TroopConformityTooltip = new BasicTooltipViewModel(() => CampaignUIHelper.GetTroopConformityTooltip(value));
			}
		}

		// Token: 0x17000098 RID: 152
		// (get) Token: 0x06000294 RID: 660 RVA: 0x00014234 File Offset: 0x00012434
		// (set) Token: 0x06000295 RID: 661 RVA: 0x0001423C File Offset: 0x0001243C
		public CharacterObject Character
		{
			get
			{
				return this._character;
			}
			set
			{
				if (this._character != value)
				{
					this._character = value;
					CharacterCode characterCode = this.GetCharacterCode(value, this.Type, this.Side);
					this.Code = new CharacterImageIdentifierVM(characterCode);
					CharacterObject[] upgradeTargets = this._character.UpgradeTargets;
					if (upgradeTargets != null && upgradeTargets.Length != 0)
					{
						this.Upgrades = new MBBindingList<UpgradeTargetVM>();
						for (int i = 0; i < this._character.UpgradeTargets.Length; i++)
						{
							CharacterCode characterCode2 = this.GetCharacterCode(this._character.UpgradeTargets[i], this.Type, this.Side);
							this.Upgrades.Add(new UpgradeTargetVM(i, value, characterCode2, new Action<int, int>(this.Upgrade), new Action<UpgradeTargetVM>(this.FocusUpgrade)));
						}
					}
				}
				this.CheckTransferAmountDefaultValue();
			}
		}

		// Token: 0x06000296 RID: 662 RVA: 0x00014308 File Offset: 0x00012508
		public PartyCharacterVM(PartyScreenLogic partyScreenLogic, PartyVM partyVm, TroopRoster troops, int index, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, bool isTroopTransferrable)
		{
			this.Upgrades = new MBBindingList<UpgradeTargetVM>();
			this._partyScreenLogic = partyScreenLogic;
			this._partyVm = partyVm;
			this.Troops = troops;
			this.Side = side;
			this.Type = type;
			this.Troop = troops.GetElementCopyAtIndex(index);
			this.Index = index;
			this.IsHero = this.Troop.Character.IsHero;
			this.IsMainHero = Hero.MainHero.CharacterObject == this.Troop.Character;
			this.IsPrisoner = this.Type == PartyScreenLogic.TroopType.Prisoner;
			this.TierIconData = CampaignUIHelper.GetCharacterTierData(this.Troop.Character, true);
			this.TypeIconData = CampaignUIHelper.GetCharacterTypeData(this.Troop.Character, false);
			this.StringId = CampaignUIHelper.GetTroopLockStringID(this.Troop);
			this._initIsTroopTransferable = isTroopTransferrable;
			this.IsTroopTransferrable = this._initIsTroopTransferable;
			this.TradeData = new PartyTradeVM(partyScreenLogic, this.Troop, this.Side, this.IsTroopTransferrable, this.IsPrisoner, new Action<int, bool>(this.OnTradeApplyTransaction));
			this.IsPrisonerOfPlayer = this.IsPrisoner && this.Side == PartyScreenLogic.PartyRosterSide.Right;
			this.IsHeroPrisonerOfPlayer = this.IsPrisonerOfPlayer && this.Character.IsHero;
			this.IsExecutable = this._partyScreenLogic.IsExecutable(this.Type, this.Character, this.Side);
			this.IsUpgradableTroop = this.Side == PartyScreenLogic.PartyRosterSide.Right && !this.IsHero && !this.IsPrisoner && this.Character.UpgradeTargets.Length != 0;
			this.InitializeUpgrades();
			this.ThrowOnPropertyChanged();
			this.CheckTransferAmountDefaultValue();
			this.UpdateRecruitable();
			this.RefreshValues();
			this.SetMoraleCost();
			this.UpdateTalkable();
			this.TransferHint = new BasicTooltipViewModel(() => this.GetTransferHint());
			this.RecruitPrisonerHint = new BasicTooltipViewModel(() => this.GetRecruitHint());
			this.ExecutePrisonerHint = new BasicTooltipViewModel(() => this._partyScreenLogic.GetExecutableReasonString(this.Troop.Character, this.IsExecutable));
			this.HeroHealthHint = (this.Troop.Character.IsHero ? new BasicTooltipViewModel(() => CampaignUIHelper.GetHeroHealthTooltip(this.Troop.Character.HeroObject)) : null);
		}

		// Token: 0x06000297 RID: 663 RVA: 0x0001455C File Offset: 0x0001275C
		public void UpdateTalkable()
		{
			bool flag = this.Side == PartyScreenLogic.PartyRosterSide.Right;
			bool flag2 = this.Troop.Character != CharacterObject.PlayerCharacter;
			bool isHero = this.Troop.Character.IsHero;
			this.IsTalkableCharacter = flag2 && flag && isHero;
			if (this.TalkHint == null)
			{
				this.TalkHint = new HintViewModel();
			}
			if (this.IsTalkableCharacter)
			{
				this._partyCharacterTalkPermission = null;
				Game.Current.EventManager.TriggerEvent<PartyScreenCharacterTalkPermissionEvent>(new PartyScreenCharacterTalkPermissionEvent(this.Character.HeroObject, new Action<bool, TextObject>(this.OnPartyCharacterTalkPermissionResult)));
				if (this._partyCharacterTalkPermission != null && !this._partyCharacterTalkPermission.Item1)
				{
					this.CanTalk = false;
					this.TalkHint.HintText = this._partyCharacterTalkPermission.Item2;
					if (this.TalkHint.HintText.IsEmpty())
					{
						this.TalkHint.HintText = new TextObject("{=epQYhd1A}Cannot talk to hero right now", null);
						return;
					}
				}
				else
				{
					CanTalkToHeroDelegate canTalkToHeroDelegate = this._partyVm.PartyScreenLogic.CanTalkToHeroDelegate;
					this.CanTalk = (canTalkToHeroDelegate == null || canTalkToHeroDelegate(this.Character.HeroObject, this.Type, this.Side, this._partyScreenLogic.LeftOwnerParty, out this.TalkHint.HintText)) && CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out this.TalkHint.HintText);
					if (this.CanTalk)
					{
						this.TalkHint.HintText = GameTexts.FindText("str_talk_button", null);
						return;
					}
					if (this.TalkHint.HintText.IsEmpty())
					{
						this.TalkHint.HintText = new TextObject("{=epQYhd1A}Cannot talk to hero right now", null);
						return;
					}
				}
			}
			else
			{
				this.TalkHint.HintText = TextObject.GetEmpty();
				this.CanTalk = false;
			}
		}

		// Token: 0x06000298 RID: 664 RVA: 0x00014718 File Offset: 0x00012918
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Name = this.Troop.Character.Name.ToString();
			this.LockHint = new HintViewModel(GameTexts.FindText("str_inventory_lock", null), null);
			MBBindingList<UpgradeTargetVM> upgrades = this.Upgrades;
			if (upgrades != null)
			{
				upgrades.ApplyActionOnAllItems(delegate(UpgradeTargetVM x)
				{
					x.RefreshValues();
				});
			}
			PartyTradeVM tradeData = this.TradeData;
			if (tradeData == null)
			{
				return;
			}
			tradeData.RefreshValues();
		}

		// Token: 0x06000299 RID: 665 RVA: 0x0001479D File Offset: 0x0001299D
		private void OnPartyCharacterTalkPermissionResult(bool isAvailable, TextObject reasonStr)
		{
			this._partyCharacterTalkPermission = new Tuple<bool, TextObject>(isAvailable, reasonStr);
		}

		// Token: 0x0600029A RID: 666 RVA: 0x000147AC File Offset: 0x000129AC
		private string GetTransferHint()
		{
			string text = GameTexts.FindText("str_transfer", null).ToString();
			string stackModifierString = CampaignUIHelper.GetStackModifierString(GameTexts.FindText("str_entire_stack_shortcut_transfer_troops", null), GameTexts.FindText("str_five_stack_shortcut_transfer_troops", null), this.Troop.Number >= 5);
			if (string.IsNullOrEmpty(stackModifierString))
			{
				return text;
			}
			return GameTexts.FindText("str_string_newline_string", null).SetTextVariable("STR1", text).SetTextVariable("STR2", stackModifierString)
				.ToString();
		}

		// Token: 0x0600029B RID: 667 RVA: 0x0001482C File Offset: 0x00012A2C
		private string GetRecruitHint()
		{
			bool flag;
			string recruitableReasonString = this._partyScreenLogic.GetRecruitableReasonString(this.Troop.Character, this.IsTroopRecruitable, this.Troop.Number, out flag);
			string stackModifierString = CampaignUIHelper.GetStackModifierString(GameTexts.FindText("str_entire_stack_shortcut_recruit_units", null), GameTexts.FindText("str_five_stack_shortcut_recruit_units", null), this.Troop.Number >= 5);
			if (string.IsNullOrEmpty(stackModifierString) || !flag)
			{
				return recruitableReasonString;
			}
			return GameTexts.FindText("str_string_newline_string", null).SetTextVariable("STR1", recruitableReasonString).SetTextVariable("STR2", stackModifierString)
				.ToString();
		}

		// Token: 0x0600029C RID: 668 RVA: 0x000148CC File Offset: 0x00012ACC
		private void CheckTransferAmountDefaultValue()
		{
			if (this.TransferAmount == 0 && this.Troop.Character != null && this.Troop.Number > 0)
			{
				this.TransferAmount = 1;
			}
		}

		// Token: 0x0600029D RID: 669 RVA: 0x00014906 File Offset: 0x00012B06
		public void ExecuteSetSelected()
		{
			if (this.Character != null)
			{
				PartyCharacterVM.SetSelected(this);
			}
		}

		// Token: 0x0600029E RID: 670 RVA: 0x0001491B File Offset: 0x00012B1B
		public void ExecuteTalk()
		{
			PartyVM partyVm = this._partyVm;
			if (partyVm == null)
			{
				return;
			}
			partyVm.ExecuteTalk();
		}

		// Token: 0x0600029F RID: 671 RVA: 0x0001492D File Offset: 0x00012B2D
		public void UpdateTradeData()
		{
			PartyTradeVM tradeData = this.TradeData;
			if (tradeData == null)
			{
				return;
			}
			tradeData.UpdateTroopData(this.Troop, this.Side, true);
		}

		// Token: 0x060002A0 RID: 672 RVA: 0x0001494C File Offset: 0x00012B4C
		public void UpdateRecruitable()
		{
			this.MaxConformity = this.Troop.Character.ConformityNeededToRecruitPrisoner;
			int elementXp = PartyBase.MainParty.PrisonRoster.GetElementXp(this.Troop.Character);
			this.CurrentConformity = ((elementXp >= this.Troop.Number * this.MaxConformity) ? this.MaxConformity : (elementXp % this.MaxConformity));
			this.IsRecruitablePrisoner = !this._character.IsHero && this.Type == PartyScreenLogic.TroopType.Prisoner;
			this.IsTroopRecruitable = this._partyScreenLogic.IsPrisonerRecruitable(this.Type, this.Character, this.Side) && !this._partyScreenLogic.IsTroopUpgradesDisabled;
			this.NumOfRecruitablePrisoners = this._partyScreenLogic.GetTroopRecruitableAmount(this.Character);
			GameTexts.SetVariable("LEFT", this.NumOfRecruitablePrisoners);
			GameTexts.SetVariable("RIGHT", this.Troop.Number);
			this.StrNumOfRecruitableTroop = GameTexts.FindText("str_LEFT_over_RIGHT", null).ToString();
		}

		// Token: 0x060002A1 RID: 673 RVA: 0x00014A64 File Offset: 0x00012C64
		private void OnTradeApplyTransaction(int amount, bool isIncreasing)
		{
			this.TransferAmount = amount;
			PartyScreenLogic.PartyRosterSide side = (isIncreasing ? PartyScreenLogic.PartyRosterSide.Left : PartyScreenLogic.PartyRosterSide.Right);
			this.ApplyTransfer(this.TransferAmount, side);
			this.IsExecutable = this._partyScreenLogic.IsExecutable(this.Type, this.Character, this.Side) && this.Troop.Number > 0;
		}

		// Token: 0x060002A2 RID: 674 RVA: 0x00014AC8 File Offset: 0x00012CC8
		public void InitializeUpgrades()
		{
			if (this.IsUpgradableTroop)
			{
				for (int i = 0; i < this.Character.UpgradeTargets.Length; i++)
				{
					CharacterObject characterObject = this.Character.UpgradeTargets[i];
					int level = characterObject.Level;
					int upgradeGoldCost = this.Character.GetUpgradeGoldCost(PartyBase.MainParty, i);
					if (!this.Character.Culture.IsBandit)
					{
						int level2 = this.Character.Level;
					}
					else
					{
						int level3 = this.Character.Level;
					}
					PerkObject requiredPerk;
					bool flag = Campaign.Current.Models.PartyTroopUpgradeModel.DoesPartyHaveRequiredPerksForUpgrade(PartyBase.MainParty, this.Character, characterObject, out requiredPerk);
					int b = (flag ? this.Troop.Number : 0);
					bool flag2 = true;
					int numOfCategoryItemPartyHas = this.GetNumOfCategoryItemPartyHas(this._partyScreenLogic.RightOwnerParty.ItemRoster, characterObject.UpgradeRequiresItemFromCategory);
					if (characterObject.UpgradeRequiresItemFromCategory != null)
					{
						flag2 = numOfCategoryItemPartyHas > 0;
					}
					bool flag3 = Hero.MainHero.Gold + this._partyScreenLogic.CurrentData.PartyGoldChangeAmount >= upgradeGoldCost;
					bool flag4 = level >= this.Character.Level && this.Troop.Xp >= this.Character.GetUpgradeXpCost(PartyBase.MainParty, i);
					bool flag5 = !flag2 || !flag3;
					int a = this.Troop.Number;
					if (upgradeGoldCost > 0)
					{
						a = (int)MathF.Clamp((float)MathF.Floor((float)(Hero.MainHero.Gold + this._partyScreenLogic.CurrentData.PartyGoldChangeAmount) / (float)upgradeGoldCost), 0f, (float)this.Troop.Number);
					}
					int b2 = ((characterObject.UpgradeRequiresItemFromCategory != null) ? numOfCategoryItemPartyHas : this.Troop.Number);
					int num = (flag4 ? ((int)MathF.Clamp((float)MathF.Floor((float)this.Troop.Xp / (float)this.Character.GetUpgradeXpCost(PartyBase.MainParty, i)), 0f, (float)this.Troop.Number)) : 0);
					int num2 = MathF.Min(MathF.Min(a, b2), MathF.Min(num, b));
					if (this.Character.Culture.IsBandit)
					{
						flag5 = flag5 || !Campaign.Current.Models.PartyTroopUpgradeModel.CanPartyUpgradeTroopToTarget(PartyBase.MainParty, this.Character, characterObject);
						num2 = ((!flag4) ? 0 : num2);
					}
					flag4 = flag4 && !this._partyVm.PartyScreenLogic.IsTroopUpgradesDisabled;
					string upgradeHint = CampaignUIHelper.GetUpgradeHint(i, numOfCategoryItemPartyHas, num2, upgradeGoldCost, flag, requiredPerk, this.Character, this.Troop, this._partyScreenLogic.CurrentData.PartyGoldChangeAmount, this._partyVm.PartyScreenLogic.IsTroopUpgradesDisabled);
					this.Upgrades[i].Refresh(num2, flag4, flag5, flag2, flag, upgradeHint, this.Character.IsMariner);
					if (i == 0)
					{
						this.UpgradeCostText = upgradeGoldCost.ToString();
						this.HasEnoughGold = flag3;
						this.NumOfReadyToUpgradeTroops = num;
						this.MaxXP = this.Character.GetUpgradeXpCost(PartyBase.MainParty, i);
						this.CurrentXP = ((this.Troop.Xp >= this.Troop.Number * this.MaxXP) ? this.MaxXP : (this.Troop.Xp % this.MaxXP));
					}
				}
				this.AnyUpgradeHasRequirement = this.Upgrades.Any((UpgradeTargetVM x) => x.Requirements.HasItemRequirement || x.Requirements.HasPerkRequirement);
			}
			int num3 = 0;
			foreach (UpgradeTargetVM upgradeTargetVM in this.Upgrades)
			{
				if (upgradeTargetVM.AvailableUpgrades > num3)
				{
					num3 = upgradeTargetVM.AvailableUpgrades;
				}
			}
			this.NumOfUpgradeableTroops = num3;
			this.IsTroopUpgradable = this.NumOfUpgradeableTroops > 0 && !this._partyVm.PartyScreenLogic.IsTroopUpgradesDisabled;
			GameTexts.SetVariable("LEFT", this.NumOfReadyToUpgradeTroops);
			GameTexts.SetVariable("RIGHT", this.Troop.Number);
			this.StrNumOfUpgradableTroop = GameTexts.FindText("str_LEFT_over_RIGHT", null).ToString();
			base.OnPropertyChanged("AmountOfUpgrades");
		}

		// Token: 0x060002A3 RID: 675 RVA: 0x00014F4C File Offset: 0x0001314C
		public void OnTransferred()
		{
			if (this.Side != PartyScreenLogic.PartyRosterSide.Left || this.IsPrisoner)
			{
				this.InitializeUpgrades();
				return;
			}
			PartyCharacterVM partyCharacterVM = this._partyVm.MainPartyTroops.FirstOrDefault((PartyCharacterVM x) => x.Character == this.Character);
			if (partyCharacterVM == null)
			{
				return;
			}
			partyCharacterVM.InitializeUpgrades();
		}

		// Token: 0x060002A4 RID: 676 RVA: 0x00014F8C File Offset: 0x0001318C
		public void ThrowOnPropertyChanged()
		{
			base.OnPropertyChanged("Name");
			base.OnPropertyChanged("Number");
			base.OnPropertyChanged("WoundedCount");
			base.OnPropertyChanged("IsTroopTransferrable");
			base.OnPropertyChanged("MaxCount");
			base.OnPropertyChanged("AmountOfUpgrades");
			base.OnPropertyChanged("Level");
			base.OnPropertyChanged("PartyIndex");
			base.OnPropertyChanged("Index");
			base.OnPropertyChanged("TroopNum");
			base.OnPropertyChanged("TransferString");
		}

		// Token: 0x060002A5 RID: 677 RVA: 0x00015014 File Offset: 0x00013214
		public override bool Equals(object obj)
		{
			PartyCharacterVM partyCharacterVM;
			return obj != null && (partyCharacterVM = obj as PartyCharacterVM) != null && ((partyCharacterVM.Character == null && this.Code == null) || partyCharacterVM.Character == this.Character);
		}

		// Token: 0x060002A6 RID: 678 RVA: 0x00015052 File Offset: 0x00013252
		private void ApplyTransfer(int transferAmount, PartyScreenLogic.PartyRosterSide side)
		{
			PartyCharacterVM.OnTransfer(this, -1, transferAmount, side);
			this.ThrowOnPropertyChanged();
			this.UpdateTalkable();
		}

		// Token: 0x060002A7 RID: 679 RVA: 0x0001506E File Offset: 0x0001326E
		private void ExecuteTransfer()
		{
			this.ApplyTransfer(this.TransferAmount, this.Side);
		}

		// Token: 0x060002A8 RID: 680 RVA: 0x00015084 File Offset: 0x00013284
		private void ExecuteTransferAll()
		{
			this.ApplyTransfer(this.Troop.Number, this.Side);
		}

		// Token: 0x060002A9 RID: 681 RVA: 0x000150AB File Offset: 0x000132AB
		public void ExecuteSetFocused()
		{
			Action<PartyCharacterVM> onFocus = PartyCharacterVM.OnFocus;
			if (onFocus == null)
			{
				return;
			}
			onFocus(this);
		}

		// Token: 0x060002AA RID: 682 RVA: 0x000150BD File Offset: 0x000132BD
		public void ExecuteSetUnfocused()
		{
			Action<PartyCharacterVM> onFocus = PartyCharacterVM.OnFocus;
			if (onFocus == null)
			{
				return;
			}
			onFocus(null);
		}

		// Token: 0x060002AB RID: 683 RVA: 0x000150D0 File Offset: 0x000132D0
		public void ExecuteTransferSingle()
		{
			int transferAmount = 1;
			if (this._partyVm.IsEntireStackModifierActive)
			{
				transferAmount = this.Troop.Number;
			}
			else if (this._partyVm.IsFiveStackModifierActive)
			{
				transferAmount = MathF.Min(5, this.Troop.Number);
			}
			this.ApplyTransfer(transferAmount, this.Side);
			this._partyVm.ExecuteRemoveZeroCounts();
		}

		// Token: 0x060002AC RID: 684 RVA: 0x00015137 File Offset: 0x00013337
		public void ExecuteResetTrade()
		{
			this.TradeData.ExecuteReset();
		}

		// Token: 0x060002AD RID: 685 RVA: 0x00015144 File Offset: 0x00013344
		public void Upgrade(int upgradeIndex, int maxUpgradeCount)
		{
			PartyVM partyVm = this._partyVm;
			if (partyVm == null)
			{
				return;
			}
			partyVm.ExecuteUpgrade(this, upgradeIndex, maxUpgradeCount);
		}

		// Token: 0x060002AE RID: 686 RVA: 0x00015159 File Offset: 0x00013359
		public void FocusUpgrade(UpgradeTargetVM upgrade)
		{
			this._partyVm.CurrentFocusedUpgrade = upgrade;
		}

		// Token: 0x060002AF RID: 687 RVA: 0x00015167 File Offset: 0x00013367
		public void RecruitAll()
		{
			if (this.IsTroopRecruitable)
			{
				this._partyVm.ExecuteRecruit(this, true);
			}
		}

		// Token: 0x060002B0 RID: 688 RVA: 0x0001517E File Offset: 0x0001337E
		public void ExecuteRecruitTroop()
		{
			if (this.IsTroopRecruitable)
			{
				this._partyVm.ExecuteRecruit(this, false);
			}
		}

		// Token: 0x060002B1 RID: 689 RVA: 0x00015198 File Offset: 0x00013398
		public void ExecuteExecuteTroop()
		{
			if (this.IsExecutable)
			{
				if (FaceGen.GetMaturityTypeWithAge(this.Character.HeroObject.BodyProperties.Age) <= BodyMeshMaturityType.Tween)
				{
					return;
				}
				MBInformationManager.ShowSceneNotification(HeroExecutionSceneNotificationData.CreateForPlayerExecutingHero(this.Character.HeroObject, delegate
				{
					this._partyVm.ExecuteExecution();
				}, SceneNotificationData.RelevantContextType.Any, true));
			}
		}

		// Token: 0x060002B2 RID: 690 RVA: 0x000151F4 File Offset: 0x000133F4
		public void ExecuteOpenTroopEncyclopedia()
		{
			if (!this.Troop.Character.IsHero)
			{
				if (Campaign.Current.EncyclopediaManager.GetPageOf(typeof(CharacterObject)).IsValidEncyclopediaItem(this.Troop.Character))
				{
					Campaign.Current.EncyclopediaManager.GoToLink(this.Troop.Character.EncyclopediaLink);
					return;
				}
			}
			else if (Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Hero)).IsValidEncyclopediaItem(this.Troop.Character.HeroObject))
			{
				Campaign.Current.EncyclopediaManager.GoToLink(this.Troop.Character.HeroObject.EncyclopediaLink);
			}
		}

		// Token: 0x060002B3 RID: 691 RVA: 0x000152B4 File Offset: 0x000134B4
		private CharacterCode GetCharacterCode(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side)
		{
			IFaction faction = null;
			if (type != PartyScreenLogic.TroopType.Prisoner)
			{
				if (side == PartyScreenLogic.PartyRosterSide.Left && this._partyScreenLogic.LeftOwnerParty != null)
				{
					faction = this._partyScreenLogic.LeftOwnerParty.MapFaction;
				}
				else if (this.Side == PartyScreenLogic.PartyRosterSide.Right && this._partyScreenLogic.RightOwnerParty != null)
				{
					faction = this._partyScreenLogic.RightOwnerParty.MapFaction;
				}
			}
			uint color = Color.White.ToUnsignedInteger();
			uint color2 = Color.White.ToUnsignedInteger();
			if (faction != null)
			{
				color = faction.Color;
				color2 = faction.Color2;
			}
			else if (character.Culture != null)
			{
				color = character.Culture.Color;
				color2 = character.Culture.Color2;
			}
			Equipment equipment = character.Equipment;
			string equipmentCode = ((equipment != null) ? equipment.CalculateEquipmentCode() : null);
			BodyProperties bodyProperties = character.GetBodyProperties(character.Equipment, -1);
			return CharacterCode.CreateFrom(equipmentCode, bodyProperties, character.IsFemale, character.IsHero, color, color2, character.DefaultFormationClass, character.Race);
		}

		// Token: 0x060002B4 RID: 692 RVA: 0x000153A4 File Offset: 0x000135A4
		private void SetMoraleCost()
		{
			if (this.IsTroopRecruitable)
			{
				this.RecruitMoraleCostText = Campaign.Current.Models.PrisonerRecruitmentCalculationModel.GetPrisonerRecruitmentMoraleEffect(this._partyScreenLogic.RightOwnerParty, this.Character, 1).ToString();
			}
		}

		// Token: 0x060002B5 RID: 693 RVA: 0x000153F0 File Offset: 0x000135F0
		public void SetIsUpgradeButtonHighlighted(bool isHighlighted)
		{
			MBBindingList<UpgradeTargetVM> upgrades = this.Upgrades;
			if (upgrades == null)
			{
				return;
			}
			upgrades.ApplyActionOnAllItems(delegate(UpgradeTargetVM x)
			{
				x.IsHighlighted = isHighlighted;
			});
		}

		// Token: 0x060002B6 RID: 694 RVA: 0x00015428 File Offset: 0x00013628
		public int GetNumOfCategoryItemPartyHas(ItemRoster items, ItemCategory itemCategory)
		{
			int num = 0;
			foreach (ItemRosterElement itemRosterElement in items)
			{
				if (itemRosterElement.EquipmentElement.Item.ItemCategory == itemCategory)
				{
					num += itemRosterElement.Amount;
				}
			}
			return num;
		}

		// Token: 0x060002B7 RID: 695 RVA: 0x00015490 File Offset: 0x00013690
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x17000099 RID: 153
		// (get) Token: 0x060002B8 RID: 696 RVA: 0x00015498 File Offset: 0x00013698
		// (set) Token: 0x060002B9 RID: 697 RVA: 0x000154A0 File Offset: 0x000136A0
		[DataSourceProperty]
		public bool IsFormationEnabled
		{
			get
			{
				return this._isFormationEnabled;
			}
			set
			{
				if (this._isFormationEnabled != value)
				{
					this._isFormationEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsFormationEnabled");
				}
			}
		}

		// Token: 0x1700009A RID: 154
		// (get) Token: 0x060002BA RID: 698 RVA: 0x000154C0 File Offset: 0x000136C0
		[DataSourceProperty]
		public string TransferString
		{
			get
			{
				return this.TransferAmount.ToString() + "/" + this.Number.ToString();
			}
		}

		// Token: 0x1700009B RID: 155
		// (get) Token: 0x060002BB RID: 699 RVA: 0x000154F3 File Offset: 0x000136F3
		// (set) Token: 0x060002BC RID: 700 RVA: 0x000154FB File Offset: 0x000136FB
		[DataSourceProperty]
		public bool IsTroopUpgradable
		{
			get
			{
				return this._isTroopUpgradable;
			}
			set
			{
				if (value != this._isTroopUpgradable)
				{
					this._isTroopUpgradable = value;
					base.OnPropertyChangedWithValue(value, "IsTroopUpgradable");
				}
			}
		}

		// Token: 0x1700009C RID: 156
		// (get) Token: 0x060002BD RID: 701 RVA: 0x00015519 File Offset: 0x00013719
		// (set) Token: 0x060002BE RID: 702 RVA: 0x00015521 File Offset: 0x00013721
		[DataSourceProperty]
		public bool IsTroopRecruitable
		{
			get
			{
				return this._isTroopRecruitable;
			}
			set
			{
				if (value != this._isTroopRecruitable)
				{
					this._isTroopRecruitable = value;
					base.OnPropertyChangedWithValue(value, "IsTroopRecruitable");
				}
			}
		}

		// Token: 0x1700009D RID: 157
		// (get) Token: 0x060002BF RID: 703 RVA: 0x0001553F File Offset: 0x0001373F
		// (set) Token: 0x060002C0 RID: 704 RVA: 0x00015547 File Offset: 0x00013747
		[DataSourceProperty]
		public bool IsRecruitablePrisoner
		{
			get
			{
				return this._isRecruitablePrisoner;
			}
			set
			{
				if (value != this._isRecruitablePrisoner)
				{
					this._isRecruitablePrisoner = value;
					base.OnPropertyChangedWithValue(value, "IsRecruitablePrisoner");
				}
			}
		}

		// Token: 0x1700009E RID: 158
		// (get) Token: 0x060002C1 RID: 705 RVA: 0x00015565 File Offset: 0x00013765
		// (set) Token: 0x060002C2 RID: 706 RVA: 0x0001556D File Offset: 0x0001376D
		[DataSourceProperty]
		public bool IsUpgradableTroop
		{
			get
			{
				return this._isUpgradableTroop;
			}
			set
			{
				if (value != this._isUpgradableTroop)
				{
					this._isUpgradableTroop = value;
					base.OnPropertyChangedWithValue(value, "IsUpgradableTroop");
				}
			}
		}

		// Token: 0x1700009F RID: 159
		// (get) Token: 0x060002C3 RID: 707 RVA: 0x0001558B File Offset: 0x0001378B
		// (set) Token: 0x060002C4 RID: 708 RVA: 0x00015593 File Offset: 0x00013793
		[DataSourceProperty]
		public bool IsExecutable
		{
			get
			{
				return this._isExecutable;
			}
			set
			{
				if (value != this._isExecutable)
				{
					this._isExecutable = value;
					base.OnPropertyChangedWithValue(value, "IsExecutable");
				}
			}
		}

		// Token: 0x170000A0 RID: 160
		// (get) Token: 0x060002C5 RID: 709 RVA: 0x000155B1 File Offset: 0x000137B1
		// (set) Token: 0x060002C6 RID: 710 RVA: 0x000155B9 File Offset: 0x000137B9
		[DataSourceProperty]
		public int NumOfReadyToUpgradeTroops
		{
			get
			{
				return this._numOfReadyToUpgradeTroops;
			}
			set
			{
				if (value != this._numOfReadyToUpgradeTroops)
				{
					this._numOfReadyToUpgradeTroops = value;
					base.OnPropertyChangedWithValue(value, "NumOfReadyToUpgradeTroops");
				}
			}
		}

		// Token: 0x170000A1 RID: 161
		// (get) Token: 0x060002C7 RID: 711 RVA: 0x000155D7 File Offset: 0x000137D7
		// (set) Token: 0x060002C8 RID: 712 RVA: 0x000155DF File Offset: 0x000137DF
		[DataSourceProperty]
		public int NumOfUpgradeableTroops
		{
			get
			{
				return this._numOfUpgradeableTroops;
			}
			set
			{
				if (value != this._numOfUpgradeableTroops)
				{
					this._numOfUpgradeableTroops = value;
					base.OnPropertyChangedWithValue(value, "NumOfUpgradeableTroops");
				}
			}
		}

		// Token: 0x170000A2 RID: 162
		// (get) Token: 0x060002C9 RID: 713 RVA: 0x000155FD File Offset: 0x000137FD
		// (set) Token: 0x060002CA RID: 714 RVA: 0x00015605 File Offset: 0x00013805
		[DataSourceProperty]
		public int NumOfRecruitablePrisoners
		{
			get
			{
				return this._numOfRecruitablePrisoners;
			}
			set
			{
				if (value != this._numOfRecruitablePrisoners)
				{
					this._numOfRecruitablePrisoners = value;
					base.OnPropertyChangedWithValue(value, "NumOfRecruitablePrisoners");
				}
			}
		}

		// Token: 0x170000A3 RID: 163
		// (get) Token: 0x060002CB RID: 715 RVA: 0x00015623 File Offset: 0x00013823
		// (set) Token: 0x060002CC RID: 716 RVA: 0x0001562B File Offset: 0x0001382B
		[DataSourceProperty]
		public int MaxXP
		{
			get
			{
				return this._maxXP;
			}
			set
			{
				if (value != this._maxXP)
				{
					this._maxXP = value;
					base.OnPropertyChangedWithValue(value, "MaxXP");
				}
			}
		}

		// Token: 0x170000A4 RID: 164
		// (get) Token: 0x060002CD RID: 717 RVA: 0x00015649 File Offset: 0x00013849
		// (set) Token: 0x060002CE RID: 718 RVA: 0x00015651 File Offset: 0x00013851
		[DataSourceProperty]
		public int CurrentXP
		{
			get
			{
				return this._currentXP;
			}
			set
			{
				if (value != this._currentXP)
				{
					this._currentXP = value;
					base.OnPropertyChangedWithValue(value, "CurrentXP");
				}
			}
		}

		// Token: 0x170000A5 RID: 165
		// (get) Token: 0x060002CF RID: 719 RVA: 0x0001566F File Offset: 0x0001386F
		// (set) Token: 0x060002D0 RID: 720 RVA: 0x00015677 File Offset: 0x00013877
		[DataSourceProperty]
		public int CurrentConformity
		{
			get
			{
				return this._currentConformity;
			}
			set
			{
				if (value != this._currentConformity)
				{
					this._currentConformity = value;
					base.OnPropertyChangedWithValue(value, "CurrentConformity");
				}
			}
		}

		// Token: 0x170000A6 RID: 166
		// (get) Token: 0x060002D1 RID: 721 RVA: 0x00015695 File Offset: 0x00013895
		// (set) Token: 0x060002D2 RID: 722 RVA: 0x0001569D File Offset: 0x0001389D
		[DataSourceProperty]
		public int MaxConformity
		{
			get
			{
				return this._maxConformity;
			}
			set
			{
				if (value != this._maxConformity)
				{
					this._maxConformity = value;
					base.OnPropertyChangedWithValue(value, "MaxConformity");
				}
			}
		}

		// Token: 0x170000A7 RID: 167
		// (get) Token: 0x060002D3 RID: 723 RVA: 0x000156BB File Offset: 0x000138BB
		// (set) Token: 0x060002D4 RID: 724 RVA: 0x000156C3 File Offset: 0x000138C3
		[DataSourceProperty]
		public BasicTooltipViewModel TroopXPTooltip
		{
			get
			{
				return this._troopXPTooltip;
			}
			set
			{
				if (value != this._troopXPTooltip)
				{
					this._troopXPTooltip = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "TroopXPTooltip");
				}
			}
		}

		// Token: 0x170000A8 RID: 168
		// (get) Token: 0x060002D5 RID: 725 RVA: 0x000156E1 File Offset: 0x000138E1
		// (set) Token: 0x060002D6 RID: 726 RVA: 0x000156E9 File Offset: 0x000138E9
		[DataSourceProperty]
		public BasicTooltipViewModel TroopConformityTooltip
		{
			get
			{
				return this._troopConformityTooltip;
			}
			set
			{
				if (value != this._troopConformityTooltip)
				{
					this._troopConformityTooltip = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "TroopConformityTooltip");
				}
			}
		}

		// Token: 0x170000A9 RID: 169
		// (get) Token: 0x060002D7 RID: 727 RVA: 0x00015707 File Offset: 0x00013907
		// (set) Token: 0x060002D8 RID: 728 RVA: 0x0001570F File Offset: 0x0001390F
		[DataSourceProperty]
		public BasicTooltipViewModel TransferHint
		{
			get
			{
				return this._transferHint;
			}
			set
			{
				if (value != this._transferHint)
				{
					this._transferHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "TransferHint");
				}
			}
		}

		// Token: 0x170000AA RID: 170
		// (get) Token: 0x060002D9 RID: 729 RVA: 0x0001572D File Offset: 0x0001392D
		// (set) Token: 0x060002DA RID: 730 RVA: 0x00015735 File Offset: 0x00013935
		[DataSourceProperty]
		public bool IsRecruitButtonsHiglighted
		{
			get
			{
				return this._isRecruitButtonsHiglighted;
			}
			set
			{
				if (value != this._isRecruitButtonsHiglighted)
				{
					this._isRecruitButtonsHiglighted = value;
					base.OnPropertyChangedWithValue(value, "IsRecruitButtonsHiglighted");
				}
			}
		}

		// Token: 0x170000AB RID: 171
		// (get) Token: 0x060002DB RID: 731 RVA: 0x00015753 File Offset: 0x00013953
		// (set) Token: 0x060002DC RID: 732 RVA: 0x0001575B File Offset: 0x0001395B
		[DataSourceProperty]
		public bool IsTransferButtonHiglighted
		{
			get
			{
				return this._isTransferButtonHiglighted;
			}
			set
			{
				if (value != this._isTransferButtonHiglighted)
				{
					this._isTransferButtonHiglighted = value;
					base.OnPropertyChangedWithValue(value, "IsTransferButtonHiglighted");
				}
			}
		}

		// Token: 0x170000AC RID: 172
		// (get) Token: 0x060002DD RID: 733 RVA: 0x00015779 File Offset: 0x00013979
		// (set) Token: 0x060002DE RID: 734 RVA: 0x00015781 File Offset: 0x00013981
		[DataSourceProperty]
		public string StrNumOfUpgradableTroop
		{
			get
			{
				return this._strNumOfUpgradableTroop;
			}
			set
			{
				if (value != this._strNumOfUpgradableTroop)
				{
					this._strNumOfUpgradableTroop = value;
					base.OnPropertyChangedWithValue<string>(value, "StrNumOfUpgradableTroop");
				}
			}
		}

		// Token: 0x170000AD RID: 173
		// (get) Token: 0x060002DF RID: 735 RVA: 0x000157A4 File Offset: 0x000139A4
		// (set) Token: 0x060002E0 RID: 736 RVA: 0x000157AC File Offset: 0x000139AC
		[DataSourceProperty]
		public string StrNumOfRecruitableTroop
		{
			get
			{
				return this._strNumOfRecruitableTroop;
			}
			set
			{
				if (value != this._strNumOfRecruitableTroop)
				{
					this._strNumOfRecruitableTroop = value;
					base.OnPropertyChangedWithValue<string>(value, "StrNumOfRecruitableTroop");
				}
			}
		}

		// Token: 0x170000AE RID: 174
		// (get) Token: 0x060002E1 RID: 737 RVA: 0x000157CF File Offset: 0x000139CF
		// (set) Token: 0x060002E2 RID: 738 RVA: 0x000157D7 File Offset: 0x000139D7
		[DataSourceProperty]
		public string TroopID
		{
			get
			{
				return this._troopID;
			}
			set
			{
				if (value != this._troopID)
				{
					this._troopID = value;
					base.OnPropertyChangedWithValue<string>(value, "TroopID");
				}
			}
		}

		// Token: 0x170000AF RID: 175
		// (get) Token: 0x060002E3 RID: 739 RVA: 0x000157FA File Offset: 0x000139FA
		// (set) Token: 0x060002E4 RID: 740 RVA: 0x00015802 File Offset: 0x00013A02
		[DataSourceProperty]
		public string UpgradeCostText
		{
			get
			{
				return this._upgradeCostText;
			}
			set
			{
				if (value != this._upgradeCostText)
				{
					this._upgradeCostText = value;
					base.OnPropertyChangedWithValue<string>(value, "UpgradeCostText");
				}
			}
		}

		// Token: 0x170000B0 RID: 176
		// (get) Token: 0x060002E5 RID: 741 RVA: 0x00015825 File Offset: 0x00013A25
		// (set) Token: 0x060002E6 RID: 742 RVA: 0x0001582D File Offset: 0x00013A2D
		[DataSourceProperty]
		public string RecruitMoraleCostText
		{
			get
			{
				return this._recruitMoraleCostText;
			}
			set
			{
				if (value != this._recruitMoraleCostText)
				{
					this._recruitMoraleCostText = value;
					base.OnPropertyChangedWithValue<string>(value, "RecruitMoraleCostText");
				}
			}
		}

		// Token: 0x170000B1 RID: 177
		// (get) Token: 0x060002E7 RID: 743 RVA: 0x00015850 File Offset: 0x00013A50
		// (set) Token: 0x060002E8 RID: 744 RVA: 0x00015858 File Offset: 0x00013A58
		[DataSourceProperty]
		public int Index
		{
			get
			{
				return this._index;
			}
			set
			{
				if (this._index != value)
				{
					this._index = value;
					base.OnPropertyChangedWithValue(value, "Index");
				}
			}
		}

		// Token: 0x170000B2 RID: 178
		// (get) Token: 0x060002E9 RID: 745 RVA: 0x00015876 File Offset: 0x00013A76
		// (set) Token: 0x060002EA RID: 746 RVA: 0x0001587E File Offset: 0x00013A7E
		[DataSourceProperty]
		public int TransferAmount
		{
			get
			{
				return this._transferAmount;
			}
			set
			{
				if (value <= 0)
				{
					value = 1;
				}
				if (this._transferAmount != value)
				{
					this._transferAmount = value;
					base.OnPropertyChangedWithValue(value, "TransferAmount");
					base.OnPropertyChanged("TransferString");
				}
			}
		}

		// Token: 0x170000B3 RID: 179
		// (get) Token: 0x060002EB RID: 747 RVA: 0x000158AE File Offset: 0x00013AAE
		// (set) Token: 0x060002EC RID: 748 RVA: 0x000158B6 File Offset: 0x00013AB6
		[DataSourceProperty]
		public bool IsTroopTransferrable
		{
			get
			{
				return this._isTroopTransferrable;
			}
			set
			{
				if (this.Character != CharacterObject.PlayerCharacter)
				{
					this._isTroopTransferrable = value;
					base.OnPropertyChangedWithValue(value, "IsTroopTransferrable");
				}
			}
		}

		// Token: 0x170000B4 RID: 180
		// (get) Token: 0x060002ED RID: 749 RVA: 0x000158D8 File Offset: 0x00013AD8
		// (set) Token: 0x060002EE RID: 750 RVA: 0x000158E0 File Offset: 0x00013AE0
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

		// Token: 0x170000B5 RID: 181
		// (get) Token: 0x060002EF RID: 751 RVA: 0x00015904 File Offset: 0x00013B04
		[DataSourceProperty]
		public string TroopNum
		{
			get
			{
				if (this.Character != null && this.Character.IsHero)
				{
					return "1";
				}
				if (this.Troop.Character == null)
				{
					return "-1";
				}
				int num = this.Troop.Number - this.Troop.WoundedNumber;
				string text = GameTexts.FindText("str_party_nameplate_wounded_abbr", null).ToString();
				if (num != this.Troop.Number && this.Type != PartyScreenLogic.TroopType.Prisoner)
				{
					return string.Concat(new object[]
					{
						num,
						"+",
						this.Troop.WoundedNumber,
						text
					});
				}
				return this.Troop.Number.ToString();
			}
		}

		// Token: 0x170000B6 RID: 182
		// (get) Token: 0x060002F0 RID: 752 RVA: 0x000159D8 File Offset: 0x00013BD8
		[DataSourceProperty]
		public bool IsHeroWounded
		{
			get
			{
				CharacterObject character = this.Character;
				return character != null && character.IsHero && this.Character.HeroObject.IsWounded;
			}
		}

		// Token: 0x170000B7 RID: 183
		// (get) Token: 0x060002F1 RID: 753 RVA: 0x00015A00 File Offset: 0x00013C00
		[DataSourceProperty]
		public int HeroHealth
		{
			get
			{
				CharacterObject character = this.Character;
				if (character != null && character.IsHero)
				{
					return MathF.Ceiling((float)this.Character.HeroObject.HitPoints * 100f / (float)this.Character.MaxHitPoints());
				}
				return 0;
			}
		}

		// Token: 0x170000B8 RID: 184
		// (get) Token: 0x060002F2 RID: 754 RVA: 0x00015A4C File Offset: 0x00013C4C
		[DataSourceProperty]
		public int Number
		{
			get
			{
				this.IsTroopTransferrable = this._initIsTroopTransferable && this.Troop.Number > 0;
				return this.Troop.Number;
			}
		}

		// Token: 0x170000B9 RID: 185
		// (get) Token: 0x060002F3 RID: 755 RVA: 0x00015A8C File Offset: 0x00013C8C
		[DataSourceProperty]
		public int WoundedCount
		{
			get
			{
				if (this.Troop.Character == null)
				{
					return 0;
				}
				return this.Troop.WoundedNumber;
			}
		}

		// Token: 0x170000BA RID: 186
		// (get) Token: 0x060002F4 RID: 756 RVA: 0x00015AB6 File Offset: 0x00013CB6
		// (set) Token: 0x060002F5 RID: 757 RVA: 0x00015ABE File Offset: 0x00013CBE
		[DataSourceProperty]
		public BasicTooltipViewModel RecruitPrisonerHint
		{
			get
			{
				return this._recruitPrisonerHint;
			}
			set
			{
				if (value != this._recruitPrisonerHint)
				{
					this._recruitPrisonerHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "RecruitPrisonerHint");
				}
			}
		}

		// Token: 0x170000BB RID: 187
		// (get) Token: 0x060002F6 RID: 758 RVA: 0x00015ADC File Offset: 0x00013CDC
		// (set) Token: 0x060002F7 RID: 759 RVA: 0x00015AE4 File Offset: 0x00013CE4
		[DataSourceProperty]
		public CharacterImageIdentifierVM Code
		{
			get
			{
				return this._code;
			}
			set
			{
				if (value != this._code)
				{
					this._code = value;
					base.OnPropertyChangedWithValue<CharacterImageIdentifierVM>(value, "Code");
				}
			}
		}

		// Token: 0x170000BC RID: 188
		// (get) Token: 0x060002F8 RID: 760 RVA: 0x00015B02 File Offset: 0x00013D02
		// (set) Token: 0x060002F9 RID: 761 RVA: 0x00015B0A File Offset: 0x00013D0A
		[DataSourceProperty]
		public BasicTooltipViewModel ExecutePrisonerHint
		{
			get
			{
				return this._executePrisonerHint;
			}
			set
			{
				if (value != this._executePrisonerHint)
				{
					this._executePrisonerHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "ExecutePrisonerHint");
				}
			}
		}

		// Token: 0x170000BD RID: 189
		// (get) Token: 0x060002FA RID: 762 RVA: 0x00015B28 File Offset: 0x00013D28
		// (set) Token: 0x060002FB RID: 763 RVA: 0x00015B30 File Offset: 0x00013D30
		[DataSourceProperty]
		public MBBindingList<UpgradeTargetVM> Upgrades
		{
			get
			{
				return this._upgrades;
			}
			set
			{
				if (value != this._upgrades)
				{
					this._upgrades = value;
					base.OnPropertyChangedWithValue<MBBindingList<UpgradeTargetVM>>(value, "Upgrades");
				}
			}
		}

		// Token: 0x170000BE RID: 190
		// (get) Token: 0x060002FC RID: 764 RVA: 0x00015B4E File Offset: 0x00013D4E
		// (set) Token: 0x060002FD RID: 765 RVA: 0x00015B56 File Offset: 0x00013D56
		[DataSourceProperty]
		public BasicTooltipViewModel HeroHealthHint
		{
			get
			{
				return this._heroHealthHint;
			}
			set
			{
				if (value != this._heroHealthHint)
				{
					this._heroHealthHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "HeroHealthHint");
				}
			}
		}

		// Token: 0x170000BF RID: 191
		// (get) Token: 0x060002FE RID: 766 RVA: 0x00015B74 File Offset: 0x00013D74
		// (set) Token: 0x060002FF RID: 767 RVA: 0x00015B7C File Offset: 0x00013D7C
		[DataSourceProperty]
		public bool IsHero
		{
			get
			{
				return this._isHero;
			}
			set
			{
				if (value != this._isHero)
				{
					this._isHero = value;
					base.OnPropertyChangedWithValue(value, "IsHero");
				}
			}
		}

		// Token: 0x170000C0 RID: 192
		// (get) Token: 0x06000300 RID: 768 RVA: 0x00015B9A File Offset: 0x00013D9A
		// (set) Token: 0x06000301 RID: 769 RVA: 0x00015BA2 File Offset: 0x00013DA2
		[DataSourceProperty]
		public bool IsMainHero
		{
			get
			{
				return this._isMainHero;
			}
			set
			{
				if (value != this._isMainHero)
				{
					this._isMainHero = value;
					base.OnPropertyChangedWithValue(value, "IsMainHero");
				}
			}
		}

		// Token: 0x170000C1 RID: 193
		// (get) Token: 0x06000302 RID: 770 RVA: 0x00015BC0 File Offset: 0x00013DC0
		// (set) Token: 0x06000303 RID: 771 RVA: 0x00015BC8 File Offset: 0x00013DC8
		[DataSourceProperty]
		public bool IsPrisoner
		{
			get
			{
				return this._isPrisoner;
			}
			set
			{
				if (value != this._isPrisoner)
				{
					this._isPrisoner = value;
					base.OnPropertyChangedWithValue(value, "IsPrisoner");
				}
			}
		}

		// Token: 0x170000C2 RID: 194
		// (get) Token: 0x06000304 RID: 772 RVA: 0x00015BE6 File Offset: 0x00013DE6
		// (set) Token: 0x06000305 RID: 773 RVA: 0x00015BEE File Offset: 0x00013DEE
		[DataSourceProperty]
		public bool IsPrisonerOfPlayer
		{
			get
			{
				return this._isPrisonerOfPlayer;
			}
			set
			{
				if (value != this._isPrisonerOfPlayer)
				{
					this._isPrisonerOfPlayer = value;
					base.OnPropertyChangedWithValue(value, "IsPrisonerOfPlayer");
				}
			}
		}

		// Token: 0x170000C3 RID: 195
		// (get) Token: 0x06000306 RID: 774 RVA: 0x00015C0C File Offset: 0x00013E0C
		// (set) Token: 0x06000307 RID: 775 RVA: 0x00015C14 File Offset: 0x00013E14
		[DataSourceProperty]
		public bool IsHeroPrisonerOfPlayer
		{
			get
			{
				return this._isHeroPrisonerOfPlayer;
			}
			set
			{
				if (value != this._isHeroPrisonerOfPlayer)
				{
					this._isHeroPrisonerOfPlayer = value;
					base.OnPropertyChangedWithValue(value, "IsHeroPrisonerOfPlayer");
				}
			}
		}

		// Token: 0x170000C4 RID: 196
		// (get) Token: 0x06000308 RID: 776 RVA: 0x00015C32 File Offset: 0x00013E32
		// (set) Token: 0x06000309 RID: 777 RVA: 0x00015C3A File Offset: 0x00013E3A
		[DataSourceProperty]
		public bool AnyUpgradeHasRequirement
		{
			get
			{
				return this._anyUpgradeHasRequirement;
			}
			set
			{
				if (value != this._anyUpgradeHasRequirement)
				{
					this._anyUpgradeHasRequirement = value;
					base.OnPropertyChangedWithValue(value, "AnyUpgradeHasRequirement");
				}
			}
		}

		// Token: 0x170000C5 RID: 197
		// (get) Token: 0x0600030A RID: 778 RVA: 0x00015C58 File Offset: 0x00013E58
		// (set) Token: 0x0600030B RID: 779 RVA: 0x00015C60 File Offset: 0x00013E60
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

		// Token: 0x170000C6 RID: 198
		// (get) Token: 0x0600030C RID: 780 RVA: 0x00015C7E File Offset: 0x00013E7E
		// (set) Token: 0x0600030D RID: 781 RVA: 0x00015C86 File Offset: 0x00013E86
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

		// Token: 0x170000C7 RID: 199
		// (get) Token: 0x0600030E RID: 782 RVA: 0x00015CA4 File Offset: 0x00013EA4
		// (set) Token: 0x0600030F RID: 783 RVA: 0x00015CAC File Offset: 0x00013EAC
		[DataSourceProperty]
		public bool HasEnoughGold
		{
			get
			{
				return this._hasEnoughGold;
			}
			set
			{
				if (value != this._hasEnoughGold)
				{
					this._hasEnoughGold = value;
					base.OnPropertyChangedWithValue(value, "HasEnoughGold");
				}
			}
		}

		// Token: 0x170000C8 RID: 200
		// (get) Token: 0x06000310 RID: 784 RVA: 0x00015CCA File Offset: 0x00013ECA
		// (set) Token: 0x06000311 RID: 785 RVA: 0x00015CD2 File Offset: 0x00013ED2
		[DataSourceProperty]
		public bool IsTalkableCharacter
		{
			get
			{
				return this._isTalkableCharacter;
			}
			set
			{
				if (value != this._isTalkableCharacter)
				{
					this._isTalkableCharacter = value;
					base.OnPropertyChangedWithValue(value, "IsTalkableCharacter");
				}
			}
		}

		// Token: 0x170000C9 RID: 201
		// (get) Token: 0x06000312 RID: 786 RVA: 0x00015CF0 File Offset: 0x00013EF0
		// (set) Token: 0x06000313 RID: 787 RVA: 0x00015CF8 File Offset: 0x00013EF8
		[DataSourceProperty]
		public bool CanTalk
		{
			get
			{
				return this._canTalk;
			}
			set
			{
				if (value != this._canTalk)
				{
					this._canTalk = value;
					base.OnPropertyChangedWithValue(value, "CanTalk");
				}
			}
		}

		// Token: 0x170000CA RID: 202
		// (get) Token: 0x06000314 RID: 788 RVA: 0x00015D16 File Offset: 0x00013F16
		// (set) Token: 0x06000315 RID: 789 RVA: 0x00015D1E File Offset: 0x00013F1E
		[DataSourceProperty]
		public HintViewModel TalkHint
		{
			get
			{
				return this._talkHint;
			}
			set
			{
				if (value != this._talkHint)
				{
					this._talkHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "TalkHint");
				}
			}
		}

		// Token: 0x170000CB RID: 203
		// (get) Token: 0x06000316 RID: 790 RVA: 0x00015D3C File Offset: 0x00013F3C
		// (set) Token: 0x06000317 RID: 791 RVA: 0x00015D44 File Offset: 0x00013F44
		[DataSourceProperty]
		public PartyTradeVM TradeData
		{
			get
			{
				return this._tradeData;
			}
			set
			{
				if (value != this._tradeData)
				{
					this._tradeData = value;
					base.OnPropertyChangedWithValue<PartyTradeVM>(value, "TradeData");
				}
			}
		}

		// Token: 0x170000CC RID: 204
		// (get) Token: 0x06000318 RID: 792 RVA: 0x00015D62 File Offset: 0x00013F62
		// (set) Token: 0x06000319 RID: 793 RVA: 0x00015D6A File Offset: 0x00013F6A
		[DataSourceProperty]
		public bool IsLocked
		{
			get
			{
				return this._isLocked;
			}
			set
			{
				if (value != this._isLocked)
				{
					this._isLocked = value;
					base.OnPropertyChangedWithValue(value, "IsLocked");
					Action<PartyCharacterVM, bool> processCharacterLock = PartyCharacterVM.ProcessCharacterLock;
					if (processCharacterLock == null)
					{
						return;
					}
					processCharacterLock(this, value);
				}
			}
		}

		// Token: 0x170000CD RID: 205
		// (get) Token: 0x0600031A RID: 794 RVA: 0x00015D99 File Offset: 0x00013F99
		// (set) Token: 0x0600031B RID: 795 RVA: 0x00015DA1 File Offset: 0x00013FA1
		[DataSourceProperty]
		public HintViewModel LockHint
		{
			get
			{
				return this._lockHint;
			}
			set
			{
				if (value != this._lockHint)
				{
					this._lockHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "LockHint");
				}
			}
		}

		// Token: 0x170000CE RID: 206
		// (get) Token: 0x0600031C RID: 796 RVA: 0x00015DBF File Offset: 0x00013FBF
		// (set) Token: 0x0600031D RID: 797 RVA: 0x00015DC7 File Offset: 0x00013FC7
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

		// Token: 0x0400012A RID: 298
		public static bool IsShiftingDisabled;

		// Token: 0x0400012B RID: 299
		public static Action<PartyCharacterVM, bool> ProcessCharacterLock;

		// Token: 0x0400012C RID: 300
		public static Action<PartyCharacterVM> SetSelected;

		// Token: 0x0400012D RID: 301
		public static Action<PartyCharacterVM, int, int, PartyScreenLogic.PartyRosterSide> OnTransfer;

		// Token: 0x0400012E RID: 302
		public static Action<PartyCharacterVM> OnShift;

		// Token: 0x0400012F RID: 303
		public static Action<PartyCharacterVM> OnFocus;

		// Token: 0x04000130 RID: 304
		public readonly PartyScreenLogic.PartyRosterSide Side;

		// Token: 0x04000131 RID: 305
		public readonly PartyScreenLogic.TroopType Type;

		// Token: 0x04000132 RID: 306
		protected readonly PartyVM _partyVm;

		// Token: 0x04000133 RID: 307
		protected readonly PartyScreenLogic _partyScreenLogic;

		// Token: 0x04000134 RID: 308
		protected readonly bool _initIsTroopTransferable;

		// Token: 0x04000135 RID: 309
		private Tuple<bool, TextObject> _partyCharacterTalkPermission;

		// Token: 0x04000138 RID: 312
		private TroopRosterElement _troop;

		// Token: 0x04000139 RID: 313
		private CharacterObject _character;

		// Token: 0x0400013A RID: 314
		private string _name;

		// Token: 0x0400013B RID: 315
		private string _strNumOfUpgradableTroop;

		// Token: 0x0400013C RID: 316
		private string _strNumOfRecruitableTroop;

		// Token: 0x0400013D RID: 317
		private string _troopID;

		// Token: 0x0400013E RID: 318
		private string _upgradeCostText;

		// Token: 0x0400013F RID: 319
		private string _recruitMoraleCostText;

		// Token: 0x04000140 RID: 320
		private MBBindingList<UpgradeTargetVM> _upgrades;

		// Token: 0x04000141 RID: 321
		private CharacterImageIdentifierVM _code;

		// Token: 0x04000142 RID: 322
		private BasicTooltipViewModel _transferHint;

		// Token: 0x04000143 RID: 323
		private BasicTooltipViewModel _recruitPrisonerHint;

		// Token: 0x04000144 RID: 324
		private BasicTooltipViewModel _executePrisonerHint;

		// Token: 0x04000145 RID: 325
		private BasicTooltipViewModel _heroHealthHint;

		// Token: 0x04000146 RID: 326
		private HintViewModel _talkHint;

		// Token: 0x04000147 RID: 327
		private int _transferAmount = 1;

		// Token: 0x04000148 RID: 328
		private int _index = -2;

		// Token: 0x04000149 RID: 329
		private int _numOfReadyToUpgradeTroops;

		// Token: 0x0400014A RID: 330
		private int _numOfUpgradeableTroops;

		// Token: 0x0400014B RID: 331
		private int _numOfRecruitablePrisoners;

		// Token: 0x0400014C RID: 332
		private int _maxXP;

		// Token: 0x0400014D RID: 333
		private int _currentXP;

		// Token: 0x0400014E RID: 334
		private int _maxConformity;

		// Token: 0x0400014F RID: 335
		private int _currentConformity;

		// Token: 0x04000150 RID: 336
		private BasicTooltipViewModel _troopXPTooltip;

		// Token: 0x04000151 RID: 337
		private BasicTooltipViewModel _troopConformityTooltip;

		// Token: 0x04000152 RID: 338
		private bool _isHero;

		// Token: 0x04000153 RID: 339
		private bool _isMainHero;

		// Token: 0x04000154 RID: 340
		private bool _isPrisoner;

		// Token: 0x04000155 RID: 341
		private bool _isPrisonerOfPlayer;

		// Token: 0x04000156 RID: 342
		private bool _isRecruitablePrisoner;

		// Token: 0x04000157 RID: 343
		private bool _isUpgradableTroop;

		// Token: 0x04000158 RID: 344
		private bool _isTroopTransferrable;

		// Token: 0x04000159 RID: 345
		private bool _isHeroPrisonerOfPlayer;

		// Token: 0x0400015A RID: 346
		private bool _isTroopUpgradable;

		// Token: 0x0400015B RID: 347
		private StringItemWithHintVM _tierIconData;

		// Token: 0x0400015C RID: 348
		private bool _hasEnoughGold;

		// Token: 0x0400015D RID: 349
		private bool _anyUpgradeHasRequirement;

		// Token: 0x0400015E RID: 350
		private StringItemWithHintVM _typeIconData;

		// Token: 0x0400015F RID: 351
		private bool _isRecruitButtonsHiglighted;

		// Token: 0x04000160 RID: 352
		private bool _isTransferButtonHiglighted;

		// Token: 0x04000161 RID: 353
		private bool _isFormationEnabled;

		// Token: 0x04000162 RID: 354
		private PartyTradeVM _tradeData;

		// Token: 0x04000163 RID: 355
		private bool _isTroopRecruitable;

		// Token: 0x04000164 RID: 356
		private bool _isExecutable;

		// Token: 0x04000165 RID: 357
		private bool _isLocked;

		// Token: 0x04000166 RID: 358
		private HintViewModel _lockHint;

		// Token: 0x04000167 RID: 359
		private bool _isTalkableCharacter;

		// Token: 0x04000168 RID: 360
		private bool _canTalk;

		// Token: 0x04000169 RID: 361
		private bool _isSelected;
	}
}
