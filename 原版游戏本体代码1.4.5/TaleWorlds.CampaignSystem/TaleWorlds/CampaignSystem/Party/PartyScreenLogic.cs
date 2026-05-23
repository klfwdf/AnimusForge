using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.Party;

public class PartyScreenLogic
{
	public enum TroopSortType
	{
		Invalid = -1,
		Custom,
		Type,
		Name,
		Count,
		Tier
	}

	public enum PartyRosterSide : byte
	{
		None = 99,
		Right = 1,
		Left = 0
	}

	[Flags]
	public enum TroopType
	{
		Member = 1,
		Prisoner = 2,
		None = 3
	}

	public enum PartyCommandCode
	{
		TransferTroop,
		UpgradeTroop,
		TransferPartyLeaderTroop,
		TransferTroopToLeaderSlot,
		ShiftTroop,
		RecruitTroop,
		ExecuteTroop,
		TransferAllTroops,
		SortTroops
	}

	public enum TransferState
	{
		NotTransferable,
		Transferable,
		TransferableWithTrade
	}

	public delegate void PresentationUpdate(PartyCommand command);

	public delegate void PartyGoldDelegate();

	public delegate void PartyMoraleDelegate();

	public delegate void PartyInfluenceDelegate();

	public delegate void PartyHorseDelegate();

	public delegate void AfterResetDelegate(PartyScreenLogic partyScreenLogic, bool fromCancel);

	public class PartyCommand : ISerializableObject
	{
		public PartyCommandCode Code { get; private set; }

		public PartyRosterSide RosterSide { get; private set; }

		public CharacterObject Character { get; private set; }

		public int TotalNumber { get; private set; }

		public int WoundedNumber { get; private set; }

		public int Index { get; private set; }

		public int UpgradeTarget { get; private set; }

		public TroopType Type { get; private set; }

		public TroopSortType SortType { get; private set; }

		public bool IsSortAscending { get; private set; }

		public void FillForTransferTroop(PartyRosterSide fromSide, TroopType type, CharacterObject character, int totalNumber, int woundedNumber, int targetIndex)
		{
			Code = PartyCommandCode.TransferTroop;
			RosterSide = fromSide;
			TotalNumber = totalNumber;
			WoundedNumber = woundedNumber;
			Character = character;
			Type = type;
			Index = targetIndex;
		}

		public void FillForShiftTroop(PartyRosterSide side, TroopType type, CharacterObject character, int targetIndex)
		{
			Code = PartyCommandCode.ShiftTroop;
			RosterSide = side;
			Character = character;
			Type = type;
			Index = targetIndex;
		}

		public void FillForTransferTroopToLeaderSlot(PartyRosterSide side, TroopType type, CharacterObject character, int totalNumber, int woundedNumber, int targetIndex)
		{
			Code = PartyCommandCode.TransferTroopToLeaderSlot;
			RosterSide = side;
			TotalNumber = totalNumber;
			WoundedNumber = woundedNumber;
			Character = character;
			Type = type;
			Index = targetIndex;
		}

		public void FillForTransferPartyLeaderTroop(PartyRosterSide side, TroopType type, CharacterObject character, int totalNumber)
		{
			Code = PartyCommandCode.TransferPartyLeaderTroop;
			RosterSide = side;
			TotalNumber = totalNumber;
			Character = character;
			Type = type;
		}

		public void FillForUpgradeTroop(PartyRosterSide side, TroopType type, CharacterObject character, int number, int upgradeTargetType, int index)
		{
			Code = PartyCommandCode.UpgradeTroop;
			RosterSide = side;
			TotalNumber = number;
			Character = character;
			UpgradeTarget = upgradeTargetType;
			Type = type;
			Index = index;
		}

		public void FillForRecruitTroop(PartyRosterSide side, TroopType type, CharacterObject character, int number, int index)
		{
			Code = PartyCommandCode.RecruitTroop;
			RosterSide = side;
			Character = character;
			Type = type;
			TotalNumber = number;
			Index = index;
		}

		public void FillForExecuteTroop(PartyRosterSide side, TroopType type, CharacterObject character)
		{
			Code = PartyCommandCode.ExecuteTroop;
			RosterSide = side;
			Character = character;
			Type = type;
		}

		public void FillForTransferAllTroops(PartyRosterSide side, TroopType type)
		{
			Code = PartyCommandCode.TransferAllTroops;
			RosterSide = side;
			Type = type;
		}

		public void FillForSortTroops(PartyRosterSide side, TroopSortType sortType, bool isAscending)
		{
			RosterSide = side;
			Code = PartyCommandCode.SortTroops;
			SortType = sortType;
			IsSortAscending = isAscending;
		}

		void ISerializableObject.SerializeTo(IWriter writer)
		{
			writer.WriteByte((byte)Code);
			writer.WriteByte((byte)RosterSide);
			writer.WriteUInt(Character.Id.InternalValue);
			writer.WriteInt(TotalNumber);
			writer.WriteInt(WoundedNumber);
			writer.WriteInt(UpgradeTarget);
			writer.WriteByte((byte)Type);
		}

		void ISerializableObject.DeserializeFrom(IReader reader)
		{
			Code = (PartyCommandCode)reader.ReadByte();
			RosterSide = (PartyRosterSide)reader.ReadByte();
			MBGUID objectId = new MBGUID(reader.ReadUInt());
			Character = (CharacterObject)MBObjectManager.Instance.GetObject(objectId);
			TotalNumber = reader.ReadInt();
			WoundedNumber = reader.ReadInt();
			UpgradeTarget = reader.ReadInt();
			Type = (TroopType)reader.ReadByte();
		}
	}

	public abstract class TroopComparer : IComparer<TroopRosterElement>
	{
		private bool _isAscending;

		public void SetIsAscending(bool isAscending)
		{
			_isAscending = isAscending;
		}

		private int GetHeroComparisonResult(TroopRosterElement x, TroopRosterElement y)
		{
			if (x.Character.HeroObject != null)
			{
				if (x.Character.HeroObject == Hero.MainHero)
				{
					return -2;
				}
				if (y.Character.HeroObject == null)
				{
					return -1;
				}
			}
			return 0;
		}

		public int Compare(TroopRosterElement x, TroopRosterElement y)
		{
			int num = (_isAscending ? 1 : (-1));
			int heroComparisonResult = GetHeroComparisonResult(x, y);
			if (heroComparisonResult != 0)
			{
				return heroComparisonResult;
			}
			heroComparisonResult = GetHeroComparisonResult(y, x);
			if (heroComparisonResult != 0)
			{
				return heroComparisonResult * -1;
			}
			return CompareTroops(x, y) * num;
		}

		protected abstract int CompareTroops(TroopRosterElement x, TroopRosterElement y);
	}

	private class TroopDefaultComparer : TroopComparer
	{
		protected override int CompareTroops(TroopRosterElement x, TroopRosterElement y)
		{
			return 0;
		}
	}

	private class TroopTypeComparer : TroopComparer
	{
		protected override int CompareTroops(TroopRosterElement x, TroopRosterElement y)
		{
			int defaultFormationClass = (int)x.Character.DefaultFormationClass;
			int defaultFormationClass2 = (int)y.Character.DefaultFormationClass;
			return defaultFormationClass.CompareTo(defaultFormationClass2);
		}
	}

	private class TroopNameComparer : TroopComparer
	{
		protected override int CompareTroops(TroopRosterElement x, TroopRosterElement y)
		{
			return x.Character.Name.ToString().CompareTo(y.Character.Name.ToString());
		}
	}

	private class TroopCountComparer : TroopComparer
	{
		protected override int CompareTroops(TroopRosterElement x, TroopRosterElement y)
		{
			return x.Number.CompareTo(y.Number);
		}
	}

	private class TroopTierComparer : TroopComparer
	{
		protected override int CompareTroops(TroopRosterElement x, TroopRosterElement y)
		{
			return x.Character.Tier.CompareTo(y.Character.Tier);
		}
	}

	public PartyPresentationDoneButtonDelegate PartyPresentationDoneButtonDelegate;

	public PartyPresentationDoneButtonConditionDelegate PartyPresentationDoneButtonConditionDelegate;

	public PartyPresentationCancelButtonActivateDelegate PartyPresentationCancelButtonActivateDelegate;

	public PartyPresentationCancelButtonDelegate PartyPresentationCancelButtonDelegate;

	public PresentationUpdate UpdateDelegate;

	public IsTroopTransferableDelegate IsTroopTransferableDelegate;

	public CanTalkToHeroDelegate CanTalkToHeroDelegate;

	private TroopSortType _activeOtherPartySortType;

	private TroopSortType _activeMainPartySortType;

	private bool _isOtherPartySortAscending;

	private bool _isMainPartySortAscending;

	public TroopRoster[] MemberRosters;

	public TroopRoster[] PrisonerRosters;

	public bool IsConsumablesChanges;

	private PartyScreenHelper.PartyScreenMode _partyScreenMode;

	private readonly Dictionary<TroopSortType, TroopComparer> _defaultComparers;

	private readonly PartyScreenData _initialData;

	private PartyScreenData _savedData;

	private Game _game;

	public TroopSortType ActiveOtherPartySortType
	{
		get
		{
			return _activeOtherPartySortType;
		}
		set
		{
			_activeOtherPartySortType = value;
		}
	}

	public TroopSortType ActiveMainPartySortType
	{
		get
		{
			return _activeMainPartySortType;
		}
		set
		{
			_activeMainPartySortType = value;
		}
	}

	public bool IsOtherPartySortAscending
	{
		get
		{
			return _isOtherPartySortAscending;
		}
		set
		{
			_isOtherPartySortAscending = value;
		}
	}

	public bool IsMainPartySortAscending
	{
		get
		{
			return _isMainPartySortAscending;
		}
		set
		{
			_isMainPartySortAscending = value;
		}
	}

	public TransferState MemberTransferState { get; private set; }

	public TransferState PrisonerTransferState { get; private set; }

	public TransferState AccompanyingTransferState { get; private set; }

	public TextObject LeftPartyName { get; private set; }

	public TextObject RightPartyName { get; private set; }

	public TextObject Header { get; private set; }

	public int LeftPartyMembersSizeLimit { get; private set; }

	public int LeftPartyPrisonersSizeLimit { get; private set; }

	public int RightPartyMembersSizeLimit { get; private set; }

	public int RightPartyPrisonersSizeLimit { get; private set; }

	public bool DoNotApplyGoldTransactions { get; private set; }

	public bool ShowProgressBar { get; private set; }

	public string DoneReasonString { get; private set; }

	public bool IsTroopUpgradesDisabled { get; private set; }

	public CharacterObject RightPartyLeader { get; private set; }

	public CharacterObject LeftPartyLeader { get; private set; }

	public PartyBase LeftOwnerParty { get; private set; }

	public PartyBase RightOwnerParty { get; private set; }

	public PartyScreenData CurrentData { get; private set; }

	public bool TransferHealthiesGetWoundedsFirst { get; private set; }

	public int QuestModeWageDaysMultiplier { get; private set; }

	public Game Game
	{
		get
		{
			return _game;
		}
		set
		{
			_game = value;
		}
	}

	public event PartyGoldDelegate PartyGoldChange;

	public event PartyMoraleDelegate PartyMoraleChange;

	public event PartyInfluenceDelegate PartyInfluenceChange;

	public event PartyHorseDelegate PartyHorseChange;

	public event PresentationUpdate Update;

	public event PartyScreenClosedDelegate PartyScreenClosedEvent;

	public event AfterResetDelegate AfterReset;

	public PartyScreenLogic()
	{
		_game = Game.Current;
		MemberRosters = new TroopRoster[2];
		PrisonerRosters = new TroopRoster[2];
		CurrentData = new PartyScreenData();
		_initialData = new PartyScreenData();
		_defaultComparers = new Dictionary<TroopSortType, TroopComparer>
		{
			{
				TroopSortType.Custom,
				new TroopDefaultComparer()
			},
			{
				TroopSortType.Type,
				new TroopTypeComparer()
			},
			{
				TroopSortType.Name,
				new TroopNameComparer()
			},
			{
				TroopSortType.Count,
				new TroopCountComparer()
			},
			{
				TroopSortType.Tier,
				new TroopTierComparer()
			}
		};
		IsTroopUpgradesDisabled = false;
	}

	public void Initialize(PartyScreenLogicInitializationData initializationData)
	{
		MemberRosters[1] = initializationData.RightMemberRoster;
		PrisonerRosters[1] = initializationData.RightPrisonerRoster;
		MemberRosters[0] = initializationData.LeftMemberRoster;
		PrisonerRosters[0] = initializationData.LeftPrisonerRoster;
		RightPartyLeader = initializationData.RightLeaderHero?.CharacterObject;
		LeftPartyLeader = initializationData.LeftLeaderHero?.CharacterObject;
		RightOwnerParty = initializationData.RightOwnerParty;
		LeftOwnerParty = initializationData.LeftOwnerParty;
		RightPartyName = initializationData.RightPartyName;
		RightPartyMembersSizeLimit = initializationData.RightPartyMembersSizeLimit;
		RightPartyPrisonersSizeLimit = initializationData.RightPartyPrisonersSizeLimit;
		LeftPartyName = initializationData.LeftPartyName;
		LeftPartyMembersSizeLimit = initializationData.LeftPartyMembersSizeLimit;
		LeftPartyPrisonersSizeLimit = initializationData.LeftPartyPrisonersSizeLimit;
		Header = initializationData.Header;
		QuestModeWageDaysMultiplier = initializationData.QuestModeWageDaysMultiplier;
		TransferHealthiesGetWoundedsFirst = initializationData.TransferHealthiesGetWoundedsFirst;
		SetPartyGoldChangeAmount(0);
		SetHorseChangeAmount(0);
		SetInfluenceChangeAmount(0, 0, 0);
		SetMoraleChangeAmount(0);
		CurrentData.BindRostersFrom(MemberRosters[1], PrisonerRosters[1], MemberRosters[0], PrisonerRosters[0], RightOwnerParty, LeftOwnerParty);
		_initialData.InitializeCopyFrom(initializationData.RightOwnerParty, initializationData.LeftOwnerParty);
		_initialData.CopyFromPartyAndRoster(MemberRosters[1], PrisonerRosters[1], MemberRosters[0], PrisonerRosters[0], RightOwnerParty);
		if (initializationData.PartyPresentationDoneButtonDelegate == null)
		{
			TaleWorlds.Library.Debug.FailedAssert("Done handler is given null for party screen!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Party\\PartyScreenLogic.cs", "Initialize", 242);
			initializationData.PartyPresentationDoneButtonDelegate = DefaultDoneHandler;
		}
		PartyPresentationDoneButtonDelegate = initializationData.PartyPresentationDoneButtonDelegate;
		PartyPresentationDoneButtonConditionDelegate = initializationData.PartyPresentationDoneButtonConditionDelegate;
		PartyPresentationCancelButtonActivateDelegate = initializationData.PartyPresentationCancelButtonActivateDelegate;
		PartyPresentationCancelButtonDelegate = initializationData.PartyPresentationCancelButtonDelegate;
		IsTroopUpgradesDisabled = initializationData.IsTroopUpgradesDisabled || initializationData.RightOwnerParty == null;
		MemberTransferState = initializationData.MemberTransferState;
		PrisonerTransferState = initializationData.PrisonerTransferState;
		AccompanyingTransferState = initializationData.AccompanyingTransferState;
		IsTroopTransferableDelegate = initializationData.TroopTransferableDelegate;
		CanTalkToHeroDelegate = initializationData.CanTalkToTroopDelegate;
		PartyPresentationCancelButtonActivateDelegate = initializationData.PartyPresentationCancelButtonActivateDelegate;
		PartyPresentationCancelButtonDelegate = initializationData.PartyPresentationCancelButtonDelegate;
		this.PartyScreenClosedEvent = initializationData.PartyScreenClosedDelegate;
		DoNotApplyGoldTransactions = initializationData.DoNotApplyGoldTransactions;
		ShowProgressBar = initializationData.ShowProgressBar;
		if (_partyScreenMode == PartyScreenHelper.PartyScreenMode.QuestTroopManage)
		{
			int partyGoldChangeAmount = -MemberRosters[0].Sum((TroopRosterElement t) => t.Character.TroopWage * t.Number * QuestModeWageDaysMultiplier);
			_initialData.PartyGoldChangeAmount = partyGoldChangeAmount;
			SetPartyGoldChangeAmount(partyGoldChangeAmount);
		}
	}

	private void SetPartyGoldChangeAmount(int newTotalAmount)
	{
		CurrentData.PartyGoldChangeAmount = newTotalAmount;
		this.PartyGoldChange?.Invoke();
	}

	private void SetMoraleChangeAmount(int newAmount)
	{
		CurrentData.PartyMoraleChangeAmount = newAmount;
		this.PartyMoraleChange?.Invoke();
	}

	private void SetHorseChangeAmount(int newAmount)
	{
		CurrentData.PartyHorseChangeAmount = newAmount;
		this.PartyHorseChange?.Invoke();
	}

	private void SetInfluenceChangeAmount(int heroInfluence, int troopInfluence, int prisonerInfluence)
	{
		CurrentData.PartyInfluenceChangeAmount = (heroInfluence, troopInfluence, prisonerInfluence);
		this.PartyInfluenceChange?.Invoke();
	}

	private void ProcessCommand(PartyCommand command)
	{
		switch (command.Code)
		{
		case PartyCommandCode.TransferTroop:
			TransferTroop(command, invokeUpdate: true);
			break;
		case PartyCommandCode.TransferTroopToLeaderSlot:
			TransferTroopToLeaderSlot(command);
			break;
		case PartyCommandCode.UpgradeTroop:
			UpgradeTroop(command);
			break;
		case PartyCommandCode.TransferPartyLeaderTroop:
			TransferPartyLeaderTroop(command);
			break;
		case PartyCommandCode.ShiftTroop:
			ShiftTroop(command);
			break;
		case PartyCommandCode.RecruitTroop:
			RecruitPrisoner(command);
			break;
		case PartyCommandCode.ExecuteTroop:
			ExecuteTroop(command);
			break;
		case PartyCommandCode.TransferAllTroops:
			TransferAllTroops(command);
			break;
		case PartyCommandCode.SortTroops:
			SortTroops(command);
			break;
		}
	}

	public void AddCommand(PartyCommand command)
	{
		ProcessCommand(command);
	}

	public bool ValidateCommand(PartyCommand command)
	{
		if (command.Code == PartyCommandCode.TransferTroop || command.Code == PartyCommandCode.TransferTroopToLeaderSlot)
		{
			CharacterObject character = command.Character;
			if (character != CharacterObject.PlayerCharacter)
			{
				int num;
				if (command.Type == TroopType.Member)
				{
					num = MemberRosters[(uint)command.RosterSide].FindIndexOfTroop(character);
					bool num2 = num != -1 && MemberRosters[(uint)command.RosterSide].GetElementNumber(num) >= command.TotalNumber;
					bool flag = command.RosterSide != PartyRosterSide.Left || command.Index != 0;
					return num2 && flag;
				}
				num = PrisonerRosters[(uint)command.RosterSide].FindIndexOfTroop(character);
				if (num != -1)
				{
					return PrisonerRosters[(uint)command.RosterSide].GetElementNumber(num) >= command.TotalNumber;
				}
				return false;
			}
			return false;
		}
		if (command.Code == PartyCommandCode.ShiftTroop)
		{
			CharacterObject character2 = command.Character;
			if (character2 != LeftPartyLeader && character2 != RightPartyLeader && ((command.RosterSide == PartyRosterSide.Left && (LeftPartyLeader == null || command.Index != 0)) || (command.RosterSide == PartyRosterSide.Right && (RightPartyLeader == null || command.Index != 0))))
			{
				int num3;
				if (command.Type == TroopType.Member)
				{
					num3 = MemberRosters[(uint)command.RosterSide].FindIndexOfTroop(character2);
					if (num3 != -1)
					{
						return num3 != command.Index;
					}
					return false;
				}
				num3 = PrisonerRosters[(uint)command.RosterSide].FindIndexOfTroop(character2);
				if (num3 != -1)
				{
					return num3 != command.Index;
				}
				return false;
			}
			return false;
		}
		if (command.Code == PartyCommandCode.TransferPartyLeaderTroop)
		{
			_ = command.Character;
			_ = _game.PlayerTroop;
			return false;
		}
		if (command.Code == PartyCommandCode.UpgradeTroop)
		{
			CharacterObject character3 = command.Character;
			int num4 = MemberRosters[(uint)command.RosterSide].FindIndexOfTroop(character3);
			if (num4 != -1 && MemberRosters[(uint)command.RosterSide].GetElementNumber(num4) >= command.TotalNumber && character3.UpgradeTargets.Length != 0)
			{
				if (command.UpgradeTarget < character3.UpgradeTargets.Length)
				{
					CharacterObject characterObject = character3.UpgradeTargets[command.UpgradeTarget];
					int upgradeXpCost = character3.GetUpgradeXpCost(PartyBase.MainParty, command.UpgradeTarget);
					int upgradeGoldCost = character3.GetUpgradeGoldCost(PartyBase.MainParty, command.UpgradeTarget);
					if (MemberRosters[(uint)command.RosterSide].GetElementXp(num4) >= upgradeXpCost * command.TotalNumber)
					{
						if (((command.RosterSide == PartyRosterSide.Left) ? LeftPartyLeader : RightPartyLeader)?.HeroObject.Gold + CurrentData.PartyGoldChangeAmount >= upgradeGoldCost * command.TotalNumber)
						{
							if (characterObject.UpgradeRequiresItemFromCategory == null)
							{
								return true;
							}
							foreach (ItemRosterElement item in RightOwnerParty.ItemRoster)
							{
								if (item.EquipmentElement.Item.ItemCategory == characterObject.UpgradeRequiresItemFromCategory)
								{
									return true;
								}
							}
							MBTextManager.SetTextVariable("REQUIRED_ITEM", characterObject.UpgradeRequiresItemFromCategory.GetName());
							MBInformationManager.AddQuickInformation(GameTexts.FindText("str_item_needed_for_upgrade"));
							return false;
						}
						MBTextManager.SetTextVariable("VALUE", upgradeGoldCost);
						MBInformationManager.AddQuickInformation(GameTexts.FindText("str_gold_needed_for_upgrade"));
						return false;
					}
					MBInformationManager.AddQuickInformation(new TextObject("{=m1bIfPf1}Character does not have enough experience for upgrade."));
					return false;
				}
				MBInformationManager.AddQuickInformation(new TextObject("{=kaQ7DsW3}Character does not have upgrade target."));
				return false;
			}
			return false;
		}
		if (command.Code == PartyCommandCode.RecruitTroop)
		{
			return IsPrisonerRecruitable(command.Type, command.Character, command.RosterSide);
		}
		if (command.Code == PartyCommandCode.ExecuteTroop)
		{
			return IsExecutable(command.Type, command.Character, command.RosterSide);
		}
		if (command.Code == PartyCommandCode.TransferAllTroops)
		{
			return GetRoster(command.RosterSide, command.Type).Count != 0;
		}
		if (command.Code == PartyCommandCode.SortTroops)
		{
			if (GetActiveSortTypeForSide(command.RosterSide) == command.SortType)
			{
				return GetIsAscendingSortForSide(command.RosterSide) != command.IsSortAscending;
			}
			return true;
		}
		throw new MBUnknownTypeException("Unknown command type in ValidateCommand.");
	}

	private void OnReset(bool fromCancel)
	{
		this.AfterReset?.Invoke(this, fromCancel);
	}

	protected void TransferTroopToLeaderSlot(PartyCommand command)
	{
		bool flag = false;
		if (ValidateCommand(command))
		{
			CharacterObject character = command.Character;
			if (command.Type == TroopType.Member)
			{
				int index = MemberRosters[(uint)command.RosterSide].FindIndexOfTroop(character);
				TroopRosterElement elementCopyAtIndex = MemberRosters[(uint)command.RosterSide].GetElementCopyAtIndex(index);
				int num = command.TotalNumber * (elementCopyAtIndex.Xp / elementCopyAtIndex.Number);
				MemberRosters[(uint)command.RosterSide].AddToCounts(character, -command.TotalNumber, insertAtFront: false, -command.WoundedNumber, 0, removeDepleted: true, index);
				MemberRosters[(uint)(1 - command.RosterSide)].AddToCounts(character, command.TotalNumber, insertAtFront: false, command.WoundedNumber, 0, removeDepleted: true, 0);
				if (elementCopyAtIndex.Number != command.TotalNumber)
				{
					MemberRosters[(uint)command.RosterSide].AddXpToTroop(character, -num);
				}
				MemberRosters[(uint)(1 - command.RosterSide)].AddXpToTroop(character, num);
			}
			flag = true;
		}
		if (flag)
		{
			UpdateDelegate?.Invoke(command);
			this.Update?.Invoke(command);
		}
	}

	protected void TransferTroop(PartyCommand command, bool invokeUpdate)
	{
		bool flag = false;
		if (ValidateCommand(command))
		{
			CharacterObject troop = command.Character;
			if (command.Type == TroopType.Member)
			{
				TroopRoster troopRoster = MemberRosters[(uint)command.RosterSide];
				TroopRoster troopRoster2 = MemberRosters[(uint)(1 - command.RosterSide)];
				int index = troopRoster.FindIndexOfTroop(troop);
				TroopRosterElement elementCopyAtIndex = troopRoster.GetElementCopyAtIndex(index);
				int num = ((troop.UpgradeTargets.Length != 0) ? troop.UpgradeTargets.Max((CharacterObject x) => Campaign.Current.Models.PartyTroopUpgradeModel.GetXpCostForUpgrade(PartyBase.MainParty, troop, x)) : 0);
				int num2 = 0;
				if (command.RosterSide == PartyRosterSide.Right)
				{
					int num3 = (elementCopyAtIndex.Number - command.TotalNumber) * num;
					num2 = ((elementCopyAtIndex.Xp >= num3 && num3 >= 0) ? (elementCopyAtIndex.Xp - num3) : 0);
				}
				else
				{
					int num4 = command.TotalNumber * num;
					num2 = ((elementCopyAtIndex.Xp > num4 && num4 >= 0) ? num4 : elementCopyAtIndex.Xp);
					troopRoster.AddXpToTroop(troop, -num2);
				}
				troopRoster.AddToCounts(troop, -command.TotalNumber, insertAtFront: false, -command.WoundedNumber, 0, removeDepleted: false);
				int num5 = command.Index;
				if (num5 == troopRoster2.Count && troopRoster2.Contains(troop))
				{
					num5 = troopRoster2.Count - 1;
				}
				troopRoster2.AddToCounts(troop, command.TotalNumber, insertAtFront: false, command.WoundedNumber, 0, removeDepleted: false, num5);
				troopRoster2.AddXpToTroop(troop, num2);
			}
			else
			{
				TroopRoster troopRoster3 = PrisonerRosters[(uint)command.RosterSide];
				TroopRoster troopRoster4 = PrisonerRosters[(uint)(1 - command.RosterSide)];
				int index2 = troopRoster3.FindIndexOfTroop(troop);
				TroopRosterElement elementCopyAtIndex2 = troopRoster3.GetElementCopyAtIndex(index2);
				int num6 = 0;
				int conformityNeededToRecruitPrisoner = Campaign.Current.Models.PrisonerRecruitmentCalculationModel.GetConformityNeededToRecruitPrisoner(elementCopyAtIndex2.Character);
				if (command.RosterSide == PartyRosterSide.Right)
				{
					UpdatePrisonerTransferHistory(troop, -command.TotalNumber);
					int num7 = (elementCopyAtIndex2.Number - command.TotalNumber) * conformityNeededToRecruitPrisoner;
					num6 = ((elementCopyAtIndex2.Xp >= num7 && num7 >= 0) ? (elementCopyAtIndex2.Xp - num7) : 0);
				}
				else
				{
					UpdatePrisonerTransferHistory(troop, command.TotalNumber);
					int num8 = command.TotalNumber * conformityNeededToRecruitPrisoner;
					num6 = ((elementCopyAtIndex2.Xp > num8 && num8 >= 0) ? num8 : elementCopyAtIndex2.Xp);
					troopRoster3.AddXpToTroop(troop, -num6);
				}
				troopRoster3.AddToCounts(troop, -command.TotalNumber, insertAtFront: false, -command.WoundedNumber, 0, removeDepleted: false);
				int num9 = command.Index;
				if (num9 == troopRoster4.Count && troopRoster4.Contains(troop))
				{
					num9 = troopRoster4.Count - 1;
				}
				troopRoster4.AddToCounts(troop, command.TotalNumber, insertAtFront: false, command.WoundedNumber, 0, removeDepleted: false, num9);
				troopRoster4.AddXpToTroop(troop, num6);
				if (CurrentData.RightRecruitableData.ContainsKey(troop))
				{
					CurrentData.RightRecruitableData[troop] = TaleWorlds.Library.MathF.Max(TaleWorlds.Library.MathF.Min(CurrentData.RightRecruitableData[troop], PrisonerRosters[1].GetElementNumber(troop)), Campaign.Current.Models.PrisonerRecruitmentCalculationModel.CalculateRecruitableNumber(PartyBase.MainParty, troop));
				}
			}
			flag = true;
		}
		if (!flag)
		{
			return;
		}
		if (PrisonerTransferState == TransferState.TransferableWithTrade && command.Type == TroopType.Prisoner)
		{
			int num10 = ((command.RosterSide == PartyRosterSide.Right) ? 1 : (-1));
			SetPartyGoldChangeAmount(CurrentData.PartyGoldChangeAmount + Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(command.Character, Hero.MainHero) * command.TotalNumber * num10);
		}
		if (_partyScreenMode == PartyScreenHelper.PartyScreenMode.QuestTroopManage)
		{
			int num11 = ((command.RosterSide != PartyRosterSide.Right) ? 1 : (-1));
			SetPartyGoldChangeAmount(CurrentData.PartyGoldChangeAmount + command.Character.TroopWage * command.TotalNumber * QuestModeWageDaysMultiplier * num11);
		}
		PartyState activePartyState = PartyScreenHelper.GetActivePartyState();
		if (activePartyState != null && activePartyState.IsDonating)
		{
			Settlement currentSettlement = Hero.MainHero.CurrentSettlement;
			float num12 = 0f;
			float num13 = 0f;
			float num14 = 0f;
			foreach (TroopTradeDifference item in CurrentData.GetTroopTradeDifferencesFromTo(_initialData, PartyRosterSide.Left))
			{
				int differenceCount = item.DifferenceCount;
				if (differenceCount > 0)
				{
					if (!item.IsPrisoner)
					{
						num13 += (float)differenceCount * Campaign.Current.Models.PrisonerDonationModel.CalculateInfluenceGainAfterTroopDonation(PartyBase.MainParty, item.Troop, currentSettlement);
					}
					else if (item.Troop.IsHero)
					{
						num12 += Campaign.Current.Models.PrisonerDonationModel.CalculateInfluenceGainAfterPrisonerDonation(PartyBase.MainParty, item.Troop, currentSettlement);
					}
					else
					{
						num14 += (float)differenceCount * Campaign.Current.Models.PrisonerDonationModel.CalculateInfluenceGainAfterPrisonerDonation(PartyBase.MainParty, item.Troop, currentSettlement);
					}
				}
			}
			SetInfluenceChangeAmount((int)num12, (int)num13, (int)num14);
		}
		if (invokeUpdate)
		{
			UpdateDelegate?.Invoke(command);
			this.Update?.Invoke(command);
		}
	}

	protected void ShiftTroop(PartyCommand command)
	{
		bool flag = false;
		if (ValidateCommand(command))
		{
			CharacterObject character = command.Character;
			if (command.Type == TroopType.Member)
			{
				int num = MemberRosters[(uint)command.RosterSide].FindIndexOfTroop(character);
				int targetIndex = ((num < command.Index) ? (command.Index - 1) : command.Index);
				MemberRosters[(uint)command.RosterSide].ShiftTroopToIndex(num, targetIndex);
			}
			else
			{
				int num2 = PrisonerRosters[(uint)command.RosterSide].FindIndexOfTroop(character);
				PrisonerRosters[(uint)command.RosterSide].GetElementCopyAtIndex(num2);
				int targetIndex2 = ((num2 < command.Index) ? (command.Index - 1) : command.Index);
				PrisonerRosters[(uint)command.RosterSide].ShiftTroopToIndex(num2, targetIndex2);
			}
			flag = true;
		}
		if (flag)
		{
			UpdateDelegate?.Invoke(command);
			this.Update?.Invoke(command);
		}
	}

	protected void TransferPartyLeaderTroop(PartyCommand command)
	{
		if (ValidateCommand(command))
		{
			if (command.RosterSide == PartyRosterSide.Left)
			{
				_ = LeftOwnerParty;
			}
			else
				_ = RightOwnerParty;
		}
	}

	protected void UpgradeTroop(PartyCommand command)
	{
		if (!ValidateCommand(command))
		{
			return;
		}
		CharacterObject character = command.Character;
		CharacterObject characterObject = character.UpgradeTargets[command.UpgradeTarget];
		TroopRoster roster = GetRoster(command.RosterSide, command.Type);
		int index = roster.FindIndexOfTroop(character);
		int num = character.GetUpgradeXpCost(PartyBase.MainParty, command.UpgradeTarget) * command.TotalNumber;
		roster.SetElementXp(index, roster.GetElementXp(index) - num);
		List<(EquipmentElement, int)> usedHorses = null;
		SetPartyGoldChangeAmount(CurrentData.PartyGoldChangeAmount - character.GetUpgradeGoldCost(PartyBase.MainParty, command.UpgradeTarget) * command.TotalNumber);
		if (characterObject.UpgradeRequiresItemFromCategory != null)
		{
			usedHorses = RemoveItemFromItemRoster(characterObject.UpgradeRequiresItemFromCategory, command.TotalNumber);
		}
		int num2 = 0;
		foreach (TroopRosterElement item in roster.GetTroopRoster())
		{
			if (item.Character == character && command.TotalNumber > item.Number - item.WoundedNumber)
			{
				num2 = command.TotalNumber - (item.Number - item.WoundedNumber);
			}
		}
		roster.AddToCounts(character, -command.TotalNumber, insertAtFront: false, -num2);
		roster.AddToCounts(characterObject, command.TotalNumber, insertAtFront: false, num2, 0, removeDepleted: true, command.Index);
		AddUpgradeToHistory(character, characterObject, command.TotalNumber);
		AddUsedHorsesToHistory(usedHorses);
		UpdateDelegate?.Invoke(command);
	}

	protected void RecruitPrisoner(PartyCommand command)
	{
		bool flag = false;
		if (ValidateCommand(command))
		{
			CharacterObject character = command.Character;
			TroopRoster troopRoster = PrisonerRosters[(uint)command.RosterSide];
			int num = TaleWorlds.Library.MathF.Min(CurrentData.RightRecruitableData[character], command.TotalNumber);
			if (num > 0)
			{
				CurrentData.RightRecruitableData[character] -= num;
				int conformityNeededToRecruitPrisoner = Campaign.Current.Models.PrisonerRecruitmentCalculationModel.GetConformityNeededToRecruitPrisoner(character);
				troopRoster.AddXpToTroop(character, -conformityNeededToRecruitPrisoner * num);
				troopRoster.AddToCounts(character, -num);
				MemberRosters[(uint)command.RosterSide].AddToCounts(command.Character, num, insertAtFront: false, 0, 0, removeDepleted: true, command.Index);
				AddRecruitToHistory(character, num);
				flag = true;
			}
			else
			{
				flag = false;
			}
		}
		if (flag)
		{
			UpdateDelegate?.Invoke(command);
			this.Update?.Invoke(command);
		}
	}

	protected void ExecuteTroop(PartyCommand command)
	{
		bool flag = false;
		if (ValidateCommand(command))
		{
			CharacterObject character = command.Character;
			PrisonerRosters[(uint)command.RosterSide].AddToCounts(character, -1);
			KillCharacterAction.ApplyByExecution(character.HeroObject, Hero.MainHero);
			flag = true;
		}
		if (flag)
		{
			UpdateDelegate?.Invoke(command);
			this.Update?.Invoke(command);
			if (command.RosterSide == PartyRosterSide.Left)
			{
				_initialData.LeftPrisonerRoster.AddToCounts(command.Character, -1);
			}
			else if (PartyRosterSide.Right == command.RosterSide)
			{
				_initialData.RightPrisonerRoster.AddToCounts(command.Character, -1);
			}
		}
	}

	protected void TransferAllTroops(PartyCommand command)
	{
		if (!ValidateCommand(command))
		{
			return;
		}
		PartyRosterSide side = 1 - command.RosterSide;
		TroopRoster roster = GetRoster(command.RosterSide, command.Type);
		List<TroopRosterElement> listFromRoster = GetListFromRoster(roster);
		int num = -1;
		if (command.RosterSide == PartyRosterSide.Right)
		{
			num = ((command.Type != TroopType.Prisoner) ? (LeftPartyMembersSizeLimit - MemberRosters[0].TotalManCount) : (LeftPartyPrisonersSizeLimit - PrisonerRosters[0].TotalManCount));
		}
		else if (command.RosterSide == PartyRosterSide.Left)
		{
			num = ((command.Type != TroopType.Prisoner) ? (RightPartyMembersSizeLimit - MemberRosters[1].TotalManCount) : (RightPartyPrisonersSizeLimit - PrisonerRosters[1].TotalManCount));
		}
		if (num <= 0)
		{
			num = listFromRoster.Sum((TroopRosterElement x) => x.Number);
		}
		IEnumerable<string> source = ((command.Type == TroopType.Member) ? Campaign.Current.GetCampaignBehavior<IViewDataTracker>().GetPartyTroopLocks() : Campaign.Current.GetCampaignBehavior<IViewDataTracker>().GetPartyPrisonerLocks());
		for (int num2 = 0; num2 < listFromRoster.Count; num2++)
		{
			if (num <= 0)
			{
				break;
			}
			TroopRosterElement troopRosterElement = listFromRoster[num2];
			if ((command.RosterSide != PartyRosterSide.Right || !source.Contains(troopRosterElement.Character.StringId)) && IsTroopTransferable(command.Type, troopRosterElement.Character, (int)command.RosterSide))
			{
				PartyCommand partyCommand = new PartyCommand();
				int num3 = MBMath.ClampInt(troopRosterElement.Number, 0, num);
				partyCommand.FillForTransferTroop(command.RosterSide, command.Type, troopRosterElement.Character, num3, troopRosterElement.WoundedNumber, -1);
				TransferTroop(partyCommand, invokeUpdate: false);
				num -= num3;
			}
		}
		TroopSortType activeSortTypeForSide = GetActiveSortTypeForSide(side);
		if (activeSortTypeForSide != TroopSortType.Custom)
		{
			TroopRoster roster2 = GetRoster(side, TroopType.Member);
			TroopRoster roster3 = GetRoster(side, TroopType.Prisoner);
			SortRoster(roster2, activeSortTypeForSide);
			SortRoster(roster3, activeSortTypeForSide);
		}
		UpdateDelegate?.Invoke(command);
		this.Update?.Invoke(command);
	}

	protected void SortTroops(PartyCommand command)
	{
		if (ValidateCommand(command))
		{
			SetActiveSortTypeForSide(command.RosterSide, command.SortType);
			SetIsAscendingForSide(command.RosterSide, command.IsSortAscending);
			UpdateComparersAscendingOrder(command.IsSortAscending);
			if (command.SortType != TroopSortType.Custom)
			{
				TroopRoster roster = GetRoster(command.RosterSide, TroopType.Member);
				TroopRoster roster2 = GetRoster(command.RosterSide, TroopType.Prisoner);
				SortRoster(roster, command.SortType);
				SortRoster(roster2, command.SortType);
			}
			UpdateDelegate?.Invoke(command);
			this.Update?.Invoke(command);
		}
	}

	public int GetIndexToInsertTroop(PartyRosterSide side, TroopType type, TroopRosterElement troop)
	{
		TroopSortType activeSortTypeForSide = GetActiveSortTypeForSide(side);
		if (activeSortTypeForSide != TroopSortType.Custom)
		{
			return -1;
		}
		TroopComparer comparer = GetComparer(activeSortTypeForSide);
		TroopRoster roster = GetRoster(side, type);
		for (int i = 0; i < roster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = roster.GetElementCopyAtIndex(i);
			if (!elementCopyAtIndex.Character.IsHero)
			{
				if (elementCopyAtIndex.Character.StringId == troop.Character.StringId)
				{
					return -1;
				}
				if (comparer.Compare(elementCopyAtIndex, troop) < 0)
				{
					return i;
				}
			}
		}
		return -1;
	}

	public TroopSortType GetActiveSortTypeForSide(PartyRosterSide side)
	{
		return side switch
		{
			PartyRosterSide.Left => ActiveOtherPartySortType, 
			PartyRosterSide.Right => ActiveMainPartySortType, 
			_ => TroopSortType.Invalid, 
		};
	}

	private void SetActiveSortTypeForSide(PartyRosterSide side, TroopSortType sortType)
	{
		switch (side)
		{
		case PartyRosterSide.Left:
			ActiveOtherPartySortType = sortType;
			break;
		case PartyRosterSide.Right:
			ActiveMainPartySortType = sortType;
			break;
		}
	}

	public bool GetIsAscendingSortForSide(PartyRosterSide side)
	{
		return side switch
		{
			PartyRosterSide.Left => IsOtherPartySortAscending, 
			PartyRosterSide.Right => IsMainPartySortAscending, 
			_ => false, 
		};
	}

	private void SetIsAscendingForSide(PartyRosterSide side, bool isAscending)
	{
		switch (side)
		{
		case PartyRosterSide.Left:
			IsOtherPartySortAscending = isAscending;
			break;
		case PartyRosterSide.Right:
			IsMainPartySortAscending = isAscending;
			break;
		}
	}

	private List<TroopRosterElement> GetListFromRoster(TroopRoster roster)
	{
		List<TroopRosterElement> list = new List<TroopRosterElement>();
		for (int i = 0; i < roster.Count; i++)
		{
			list.Add(roster.GetElementCopyAtIndex(i));
		}
		return list;
	}

	private void SyncRosterWithList(TroopRoster roster, List<TroopRosterElement> list)
	{
		for (int i = 0; i < list.Count; i++)
		{
			int firstIndex = roster.FindIndexOfTroop(list[i].Character);
			roster.SwapTroopsAtIndices(firstIndex, i);
		}
	}

	[Conditional("DEBUG")]
	private void EnsureRosterIsSyncedWithList(TroopRoster roster, List<TroopRosterElement> list)
	{
		if (roster.Count != list.Count)
		{
			TaleWorlds.Library.Debug.FailedAssert("Roster count is not synced with the list count", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Party\\PartyScreenLogic.cs", "EnsureRosterIsSyncedWithList", 1081);
			return;
		}
		for (int i = 0; i < roster.Count; i++)
		{
			if (roster.GetCharacterAtIndex(i).StringId != list[i].Character.StringId)
			{
				TaleWorlds.Library.Debug.FailedAssert("Roster is not synced with the list at index: " + i, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Party\\PartyScreenLogic.cs", "EnsureRosterIsSyncedWithList", 1091);
				break;
			}
		}
	}

	private void SortRoster(TroopRoster originalRoster, TroopSortType sortType)
	{
		TroopComparer comparer = _defaultComparers[sortType];
		if (!IsRosterOrdered(originalRoster, comparer))
		{
			List<TroopRosterElement> listFromRoster = GetListFromRoster(originalRoster);
			listFromRoster.Sort(_defaultComparers[sortType]);
			SyncRosterWithList(originalRoster, listFromRoster);
		}
	}

	private bool IsRosterOrdered(TroopRoster roster, TroopComparer comparer)
	{
		for (int i = 1; i < roster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = roster.GetElementCopyAtIndex(i - 1);
			TroopRosterElement elementCopyAtIndex2 = roster.GetElementCopyAtIndex(i);
			if (comparer.Compare(elementCopyAtIndex, elementCopyAtIndex2) >= 1)
			{
				return false;
			}
		}
		return true;
	}

	public bool IsDoneActive()
	{
		bool num = Hero.MainHero.Gold < -CurrentData.PartyGoldChangeAmount && CurrentData.PartyGoldChangeAmount < 0;
		Tuple<bool, TextObject> tuple = PartyPresentationDoneButtonConditionDelegate?.Invoke(MemberRosters[0], PrisonerRosters[0], MemberRosters[1], PrisonerRosters[1], LeftPartyMembersSizeLimit, 0);
		bool flag = PartyPresentationDoneButtonConditionDelegate == null || (tuple?.Item1 ?? false);
		DoneReasonString = null;
		if (num)
		{
			DoneReasonString = GameTexts.FindText("str_inventory_popup_player_not_enough_gold").ToString();
		}
		else
		{
			DoneReasonString = tuple?.Item2?.ToString() ?? string.Empty;
		}
		return !num && flag;
	}

	public bool IsCancelActive()
	{
		if (PartyPresentationCancelButtonActivateDelegate != null)
		{
			return PartyPresentationCancelButtonActivateDelegate();
		}
		return true;
	}

	public bool DoneLogic(bool isForced)
	{
		if (Hero.MainHero.Gold < -CurrentData.PartyGoldChangeAmount && CurrentData.PartyGoldChangeAmount < 0)
		{
			MBInformationManager.AddQuickInformation(GameTexts.FindText("str_inventory_popup_player_not_enough_gold"));
			return false;
		}
		FlattenedTroopRoster flattenedTroopRoster = new FlattenedTroopRoster();
		FlattenedTroopRoster flattenedTroopRoster2 = new FlattenedTroopRoster();
		foreach (Tuple<CharacterObject, int> item in CurrentData.TransferredPrisonersHistory)
		{
			int number = TaleWorlds.Library.MathF.Abs(item.Item2);
			if (item.Item2 < 0)
			{
				flattenedTroopRoster.Add(item.Item1, number);
			}
			else if (item.Item2 > 0)
			{
				flattenedTroopRoster2.Add(item.Item1, number);
			}
		}
		if (Settlement.CurrentSettlement != null && !flattenedTroopRoster2.IsEmpty())
		{
			CampaignEventDispatcher.Instance.OnPrisonersChangeInSettlement(Settlement.CurrentSettlement, flattenedTroopRoster2, null, takenFromDungeon: true);
		}
		bool num = PartyPresentationDoneButtonDelegate(MemberRosters[0], PrisonerRosters[0], MemberRosters[1], PrisonerRosters[1], flattenedTroopRoster2, flattenedTroopRoster, isForced, LeftOwnerParty, RightOwnerParty);
		if (num)
		{
			if (!DoNotApplyGoldTransactions)
			{
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, CurrentData.PartyGoldChangeAmount);
			}
			if (CurrentData.PartyInfluenceChangeAmount.Item2 != 0)
			{
				GainKingdomInfluenceAction.ApplyForLeavingTroopToGarrison(Hero.MainHero, CurrentData.PartyInfluenceChangeAmount.Item2);
			}
			FireCampaignRelatedEvents();
			SetPartyGoldChangeAmount(0);
			SetHorseChangeAmount(0);
			SetInfluenceChangeAmount(0, 0, 0);
			SetMoraleChangeAmount(0);
			CurrentData.UpgradedTroopsHistory = new List<Tuple<CharacterObject, CharacterObject, int>>();
			CurrentData.TransferredPrisonersHistory = new List<Tuple<CharacterObject, int>>();
			CurrentData.RecruitedPrisonersHistory = new List<Tuple<CharacterObject, int>>();
			CurrentData.UsedUpgradeHorsesHistory = new List<Tuple<EquipmentElement, int>>();
			_initialData.CopyFromScreenData(CurrentData);
		}
		return num;
	}

	public void OnPartyScreenClosed(bool fromCancel)
	{
		if (fromCancel)
		{
			PartyPresentationCancelButtonDelegate?.Invoke();
		}
		this.PartyScreenClosedEvent?.Invoke(LeftOwnerParty, MemberRosters[0], PrisonerRosters[0], RightOwnerParty, MemberRosters[1], PrisonerRosters[1], fromCancel);
	}

	private void UpdateComparersAscendingOrder(bool isAscending)
	{
		foreach (KeyValuePair<TroopSortType, TroopComparer> defaultComparer in _defaultComparers)
		{
			defaultComparer.Value.SetIsAscending(isAscending);
		}
	}

	private void FireCampaignRelatedEvents()
	{
		foreach (Tuple<CharacterObject, CharacterObject, int> item in CurrentData.UpgradedTroopsHistory)
		{
			CampaignEventDispatcher.Instance.OnPlayerUpgradedTroops(item.Item1, item.Item2, item.Item3);
		}
		FlattenedTroopRoster flattenedTroopRoster = new FlattenedTroopRoster();
		foreach (Tuple<CharacterObject, int> item2 in CurrentData.RecruitedPrisonersHistory)
		{
			flattenedTroopRoster.Add(item2.Item1, item2.Item2);
		}
		if (!flattenedTroopRoster.IsEmpty())
		{
			CampaignEventDispatcher.Instance.OnMainPartyPrisonerRecruited(flattenedTroopRoster);
		}
	}

	public bool IsTroopTransferable(TroopType troopType, CharacterObject character, int side)
	{
		if (IsTroopRosterTransferable(troopType) && !character.IsNotTransferableInPartyScreen && character != CharacterObject.PlayerCharacter)
		{
			if (IsTroopTransferableDelegate != null)
			{
				return IsTroopTransferableDelegate(character, troopType, (PartyRosterSide)side, LeftOwnerParty);
			}
			return true;
		}
		return false;
	}

	public bool IsTroopRosterTransferable(TroopType troopType)
	{
		switch (troopType)
		{
		case TroopType.Prisoner:
			if (PrisonerTransferState != TransferState.Transferable)
			{
				return PrisonerTransferState == TransferState.TransferableWithTrade;
			}
			return true;
		case TroopType.Member:
			if (MemberTransferState != TransferState.Transferable)
			{
				return MemberTransferState == TransferState.TransferableWithTrade;
			}
			return true;
		default:
			return false;
		}
	}

	public bool IsPrisonerRecruitable(TroopType troopType, CharacterObject character, PartyRosterSide side)
	{
		if (side != PartyRosterSide.Right || troopType != TroopType.Prisoner || character.IsHero)
		{
			return false;
		}
		if (CurrentData.RightRecruitableData.ContainsKey(character))
		{
			return CurrentData.RightRecruitableData[character] > 0;
		}
		return false;
	}

	public string GetRecruitableReasonString(CharacterObject character, bool isRecruitable, int troopCount, out bool showStackModifierText)
	{
		showStackModifierText = false;
		if (isRecruitable)
		{
			showStackModifierText = true;
			if (RightOwnerParty.PartySizeLimit <= MemberRosters[1].TotalManCount)
			{
				return GameTexts.FindText("str_recruit_party_size_limit").ToString();
			}
			return GameTexts.FindText("str_recruit_prisoner").ToString();
		}
		if (character.IsHero)
		{
			return GameTexts.FindText("str_cannot_recruit_hero").ToString();
		}
		return GameTexts.FindText("str_cannot_recruit_prisoner").ToString();
	}

	public bool IsExecutable(TroopType troopType, CharacterObject character, PartyRosterSide side)
	{
		if (troopType == TroopType.Prisoner && side == PartyRosterSide.Right && character.IsHero && character.HeroObject.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && PlayerEncounter.Current == null)
		{
			return FaceGen.GetMaturityTypeWithAge(character.Age) > BodyMeshMaturityType.Tween;
		}
		return false;
	}

	public string GetExecutableReasonString(CharacterObject character, bool isExecutable)
	{
		if (!isExecutable)
		{
			if (!character.IsHero)
			{
				return GameTexts.FindText("str_cannot_execute_nonhero").ToString();
			}
			return GameTexts.FindText("str_cannot_execute_hero").ToString();
		}
		return GameTexts.FindText("str_execute_prisoner").ToString();
	}

	public int GetCurrentQuestCurrentCount(bool includePrisoners, bool includeMembers)
	{
		int num = 0;
		if (includeMembers)
		{
			num += MemberRosters[0].Sum((TroopRosterElement item) => item.Number - item.WoundedNumber);
		}
		if (includePrisoners)
		{
			num += PrisonerRosters[0].Sum((TroopRosterElement item) => item.Number - item.WoundedNumber);
		}
		return num;
	}

	public int GetCurrentQuestRequiredCount()
	{
		return LeftPartyMembersSizeLimit;
	}

	private static bool DefaultDoneHandler(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster, bool isForced, PartyBase leftParty = null, PartyBase rightParty = null)
	{
		return true;
	}

	private void AddUpgradeToHistory(CharacterObject fromTroop, CharacterObject toTroop, int num)
	{
		Tuple<CharacterObject, CharacterObject, int> tuple = CurrentData.UpgradedTroopsHistory.Find((Tuple<CharacterObject, CharacterObject, int> t) => t.Item1 == fromTroop && t.Item2 == toTroop);
		if (tuple != null)
		{
			int item = tuple.Item3;
			CurrentData.UpgradedTroopsHistory.Remove(tuple);
			CurrentData.UpgradedTroopsHistory.Add(new Tuple<CharacterObject, CharacterObject, int>(fromTroop, toTroop, num + item));
		}
		else
		{
			CurrentData.UpgradedTroopsHistory.Add(new Tuple<CharacterObject, CharacterObject, int>(fromTroop, toTroop, num));
		}
	}

	private void AddUsedHorsesToHistory(List<(EquipmentElement, int)> usedHorses)
	{
		if (usedHorses == null)
		{
			return;
		}
		foreach (var usedHorse in usedHorses)
		{
			Tuple<EquipmentElement, int> tuple = CurrentData.UsedUpgradeHorsesHistory.Find((Tuple<EquipmentElement, int> t) => t.Equals(usedHorse.Item1));
			if (tuple != null)
			{
				int item = tuple.Item2;
				CurrentData.UsedUpgradeHorsesHistory.Remove(tuple);
				CurrentData.UsedUpgradeHorsesHistory.Add(new Tuple<EquipmentElement, int>(usedHorse.Item1, item + usedHorse.Item2));
			}
			else
			{
				CurrentData.UsedUpgradeHorsesHistory.Add(new Tuple<EquipmentElement, int>(usedHorse.Item1, usedHorse.Item2));
			}
		}
		SetHorseChangeAmount(CurrentData.PartyHorseChangeAmount += usedHorses.Sum(((EquipmentElement, int) t) => t.Item2));
	}

	private void UpdatePrisonerTransferHistory(CharacterObject troop, int amount)
	{
		Tuple<CharacterObject, int> tuple = CurrentData.TransferredPrisonersHistory.Find((Tuple<CharacterObject, int> t) => t.Item1 == troop);
		if (tuple != null)
		{
			int item = tuple.Item2;
			CurrentData.TransferredPrisonersHistory.Remove(tuple);
			CurrentData.TransferredPrisonersHistory.Add(new Tuple<CharacterObject, int>(troop, amount + item));
		}
		else
		{
			CurrentData.TransferredPrisonersHistory.Add(new Tuple<CharacterObject, int>(troop, amount));
		}
	}

	private void AddRecruitToHistory(CharacterObject troop, int amount)
	{
		Tuple<CharacterObject, int> tuple = CurrentData.RecruitedPrisonersHistory.Find((Tuple<CharacterObject, int> t) => t.Item1 == troop);
		if (tuple != null)
		{
			int item = tuple.Item2;
			CurrentData.RecruitedPrisonersHistory.Remove(tuple);
			CurrentData.RecruitedPrisonersHistory.Add(new Tuple<CharacterObject, int>(troop, amount + item));
		}
		else
		{
			CurrentData.RecruitedPrisonersHistory.Add(new Tuple<CharacterObject, int>(troop, amount));
		}
		int prisonerRecruitmentMoraleEffect = Campaign.Current.Models.PrisonerRecruitmentCalculationModel.GetPrisonerRecruitmentMoraleEffect(RightOwnerParty, troop, amount);
		SetMoraleChangeAmount(CurrentData.PartyMoraleChangeAmount + prisonerRecruitmentMoraleEffect);
	}

	private string GetItemLockStringID(EquipmentElement equipmentElement)
	{
		return equipmentElement.Item.StringId + ((equipmentElement.ItemModifier != null) ? equipmentElement.ItemModifier.StringId : "");
	}

	private List<(EquipmentElement, int)> RemoveItemFromItemRoster(ItemCategory itemCategory, int numOfItemsLeftToRemove = 1)
	{
		List<(EquipmentElement, int)> list = new List<(EquipmentElement, int)>();
		IEnumerable<string> lockedItems = Campaign.Current.GetCampaignBehavior<IViewDataTracker>().GetInventoryLocks();
		foreach (ItemRosterElement item in from x in RightOwnerParty.ItemRoster
			where x.EquipmentElement.Item?.ItemCategory == itemCategory
			orderby x.EquipmentElement.Item.Value
			orderby lockedItems.Contains(GetItemLockStringID(x.EquipmentElement))
			select x)
		{
			int num = TaleWorlds.Library.MathF.Min(numOfItemsLeftToRemove, item.Amount);
			RightOwnerParty.ItemRoster.AddToCounts(item.EquipmentElement, -num);
			numOfItemsLeftToRemove -= num;
			list.Add((item.EquipmentElement, num));
			if (numOfItemsLeftToRemove <= 0)
			{
				break;
			}
		}
		if (numOfItemsLeftToRemove > 0)
		{
			TaleWorlds.Library.Debug.FailedAssert("Couldn't find enough upgrade req items in the inventory.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Party\\PartyScreenLogic.cs", "RemoveItemFromItemRoster", 1509);
		}
		return list;
	}

	public void Reset(bool fromCancel)
	{
		ResetLogic(fromCancel);
	}

	private void ResetLogic(bool fromCancel)
	{
		if (CurrentData != _initialData)
		{
			CurrentData.ResetUsing(_initialData);
			this.AfterReset?.Invoke(this, fromCancel);
		}
	}

	public void SavePartyScreenData()
	{
		_savedData = new PartyScreenData();
		_savedData.InitializeCopyFrom(CurrentData.RightParty, CurrentData.LeftParty);
		_savedData.CopyFromScreenData(CurrentData);
	}

	public void ResetToLastSavedPartyScreenData(bool fromCancel)
	{
		if (CurrentData != _savedData)
		{
			CurrentData.ResetUsing(_savedData);
			this.AfterReset?.Invoke(this, fromCancel);
		}
	}

	public void RemoveZeroCounts()
	{
		for (int i = 0; i < MemberRosters.Length; i++)
		{
			MemberRosters[i].RemoveZeroCounts();
		}
		for (int j = 0; j < PrisonerRosters.Length; j++)
		{
			PrisonerRosters[j].RemoveZeroCounts();
		}
	}

	public int GetTroopRecruitableAmount(CharacterObject troop)
	{
		if (!CurrentData.RightRecruitableData.ContainsKey(troop))
		{
			return 0;
		}
		return CurrentData.RightRecruitableData[troop];
	}

	public TroopRoster GetRoster(PartyRosterSide side, TroopType troopType)
	{
		return troopType switch
		{
			TroopType.Member => MemberRosters[(uint)side], 
			TroopType.Prisoner => PrisonerRosters[(uint)side], 
			_ => null, 
		};
	}

	internal void OnDoneEvent(List<TroopTradeDifference> freshlySellList)
	{
	}

	public bool IsThereAnyChanges()
	{
		return _initialData.IsThereAnyTroopTradeDifferenceBetween(CurrentData);
	}

	public bool HaveRightSideGainedTroops()
	{
		foreach (TroopTradeDifference item in _initialData.GetTroopTradeDifferencesFromTo(CurrentData))
		{
			if (!item.IsPrisoner && item.FromCount < item.ToCount)
			{
				return true;
			}
		}
		return false;
	}

	public TroopComparer GetComparer(TroopSortType sortType)
	{
		return _defaultComparers[sortType];
	}
}
