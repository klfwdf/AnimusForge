using System;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;

public class CharacterCreationClanNamingStageVM : CharacterCreationStageBaseVM
{
	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _doneInputKey;

	private MBBindingList<InputKeyItemVM> _cameraControlKeys;

	private string _clanName;

	private string _clanNameNotApplicableReason;

	private string _bottomHintText;

	private BannerImageIdentifierVM _clanBanner;

	private bool _characterGamepadControlsEnabled;

	public BasicCharacterObject Character { get; private set; }

	public int ShieldSlotIndex { get; private set; } = 3;

	public ItemRosterElement ShieldRosterElement { get; private set; }

	[DataSourceProperty]
	public InputKeyItemVM CancelInputKey
	{
		get
		{
			return _cancelInputKey;
		}
		set
		{
			if (value != _cancelInputKey)
			{
				_cancelInputKey = value;
				OnPropertyChangedWithValue(value, "CancelInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM DoneInputKey
	{
		get
		{
			return _doneInputKey;
		}
		set
		{
			if (value != _doneInputKey)
			{
				_doneInputKey = value;
				OnPropertyChangedWithValue(value, "DoneInputKey");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<InputKeyItemVM> CameraControlKeys
	{
		get
		{
			return _cameraControlKeys;
		}
		set
		{
			if (value != _cameraControlKeys)
			{
				_cameraControlKeys = value;
				OnPropertyChangedWithValue(value, "CameraControlKeys");
			}
		}
	}

	[DataSourceProperty]
	public string ClanName
	{
		get
		{
			return _clanName;
		}
		set
		{
			if (value != _clanName)
			{
				_clanName = value;
				OnPropertyChangedWithValue(value, "ClanName");
				base.CanAdvance = CanAdvanceToNextStage();
			}
		}
	}

	[DataSourceProperty]
	public string ClanNameNotApplicableReason
	{
		get
		{
			return _clanNameNotApplicableReason;
		}
		set
		{
			if (value != _clanNameNotApplicableReason)
			{
				_clanNameNotApplicableReason = value;
				OnPropertyChangedWithValue(value, "ClanNameNotApplicableReason");
			}
		}
	}

	[DataSourceProperty]
	public string BottomHintText
	{
		get
		{
			return _bottomHintText;
		}
		set
		{
			if (value != _bottomHintText)
			{
				_bottomHintText = value;
				OnPropertyChangedWithValue(value, "BottomHintText");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM ClanBanner
	{
		get
		{
			return _clanBanner;
		}
		set
		{
			if (value != _clanBanner)
			{
				_clanBanner = value;
				OnPropertyChangedWithValue(value, "ClanBanner");
			}
		}
	}

	[DataSourceProperty]
	public bool CharacterGamepadControlsEnabled
	{
		get
		{
			return _characterGamepadControlsEnabled;
		}
		set
		{
			if (value != _characterGamepadControlsEnabled)
			{
				_characterGamepadControlsEnabled = value;
				OnPropertyChangedWithValue(value, "CharacterGamepadControlsEnabled");
			}
		}
	}

	public CharacterCreationClanNamingStageVM(BasicCharacterObject character, CharacterCreationManager characterCreationManager, Action affirmativeAction, TextObject affirmativeActionText, Action negativeAction, TextObject negativeActionText)
		: base(characterCreationManager, affirmativeAction, affirmativeActionText, negativeAction, negativeActionText)
	{
		Character = character;
		ClanName = Hero.MainHero.Clan.Name.ToString();
		ItemObject item = FindShield();
		ShieldRosterElement = new ItemRosterElement(item, 1);
		ClanBanner = new BannerImageIdentifierVM(Hero.MainHero.Clan.Banner, nineGrid: true);
		CameraControlKeys = new MBBindingList<InputKeyItemVM>();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		base.Title = new TextObject("{=wNUcqcJP}Clan Name").ToString();
		base.Description = new TextObject("{=JJiKk4ow}Select your family name: ").ToString();
		BottomHintText = new TextObject("{=dbBAJ8yi}You can change your banner and clan name later on clan screen").ToString();
	}

	public override bool CanAdvanceToNextStage()
	{
		Tuple<bool, string> tuple = FactionHelper.IsClanNameApplicable(ClanName);
		ClanNameNotApplicableReason = tuple.Item2;
		return tuple.Item1;
	}

	public override void OnNextStage()
	{
		_affirmativeAction();
	}

	public override void OnPreviousStage()
	{
		_negativeAction();
	}

	private ItemObject FindShield()
	{
		for (int i = 0; i < 4; i++)
		{
			EquipmentElement equipmentFromSlot = Character.Equipment.GetEquipmentFromSlot((EquipmentIndex)i);
			if (equipmentFromSlot.Item?.PrimaryWeapon != null && equipmentFromSlot.Item.PrimaryWeapon.IsShield && equipmentFromSlot.Item.IsUsingTableau)
			{
				return equipmentFromSlot.Item;
			}
		}
		foreach (ItemObject objectType in Game.Current.ObjectManager.GetObjectTypeList<ItemObject>())
		{
			if (objectType.PrimaryWeapon != null && objectType.PrimaryWeapon.IsShield && objectType.IsUsingTableau)
			{
				return objectType;
			}
		}
		return null;
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CancelInputKey?.OnFinalize();
		DoneInputKey?.OnFinalize();
		foreach (InputKeyItemVM cameraControlKey in CameraControlKeys)
		{
			cameraControlKey.OnFinalize();
		}
	}

	public void SetCancelInputKey(HotKey hotKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void AddCameraControlInputKey(HotKey hotKey)
	{
		InputKeyItemVM item = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
		CameraControlKeys.Add(item);
	}

	public void AddCameraControlInputKey(GameKey gameKey)
	{
		InputKeyItemVM item = InputKeyItemVM.CreateFromGameKey(gameKey, isConsoleOnly: true);
		CameraControlKeys.Add(item);
	}

	public void AddCameraControlInputKey(GameAxisKey gameAxisKey, TextObject keyName)
	{
		InputKeyItemVM item = InputKeyItemVM.CreateFromForcedID(gameAxisKey.AxisKey.ToString(), keyName, isConsoleOnly: true);
		CameraControlKeys.Add(item);
	}
}
