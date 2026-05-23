using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection;

public class CharacterViewModel : ViewModel
{
	public enum StanceTypes
	{
		None,
		EmphasizeFace,
		SideView,
		CelebrateVictory,
		OnMount
	}

	public static Action<CharacterViewModel> OnCustomAnimationFinished;

	private bool _isManuallyStoppingAnimation;

	protected Equipment _equipment;

	private string _mountCreationKey = "";

	private string _bodyProperties = "";

	private string _equipmentCode;

	private string _idleAction;

	private string _idleFaceAnim;

	private string _charStringId;

	private string _customAnimation;

	protected string _bannerCode;

	private bool _hasMount;

	private bool _isFemale;

	private bool _isHidden;

	private bool _isPlayingCustomAnimations;

	private bool _shouldLoopCustomAnimation;

	private float _customAnimationProgressRatio;

	private float _customAnimationWaitDuration;

	private int _race;

	private int _stanceIndex;

	private uint _armorColor1;

	private uint _armorColor2;

	private int _leftHandWieldedEquipmentIndex;

	private int _rightHandWieldedEquipmentIndex;

	[DataSourceProperty]
	public string BannerCodeText
	{
		get
		{
			return _bannerCode;
		}
		set
		{
			if (value != _bannerCode)
			{
				_bannerCode = value;
				OnPropertyChangedWithValue(value, "BannerCodeText");
			}
		}
	}

	[DataSourceProperty]
	public string BodyProperties
	{
		get
		{
			return _bodyProperties;
		}
		set
		{
			if (value != _bodyProperties)
			{
				_bodyProperties = value;
				OnPropertyChangedWithValue(value, "BodyProperties");
			}
		}
	}

	[DataSourceProperty]
	public string MountCreationKey
	{
		get
		{
			return _mountCreationKey;
		}
		set
		{
			if (value != _mountCreationKey)
			{
				_mountCreationKey = value;
				OnPropertyChangedWithValue(value, "MountCreationKey");
			}
		}
	}

	[DataSourceProperty]
	public string CharStringId
	{
		get
		{
			return _charStringId;
		}
		set
		{
			if (value != _charStringId)
			{
				_charStringId = value;
				OnPropertyChangedWithValue(value, "CharStringId");
			}
		}
	}

	[DataSourceProperty]
	public string CustomAnimation
	{
		get
		{
			return _customAnimation;
		}
		set
		{
			if (value != _customAnimation)
			{
				_customAnimation = value;
				OnPropertyChangedWithValue(value, "CustomAnimation");
			}
		}
	}

	[DataSourceProperty]
	public int StanceIndex
	{
		get
		{
			return _stanceIndex;
		}
		private set
		{
			if (value != _stanceIndex)
			{
				_stanceIndex = value;
				OnPropertyChangedWithValue(value, "StanceIndex");
			}
		}
	}

	[DataSourceProperty]
	public bool IsFemale
	{
		get
		{
			return _isFemale;
		}
		set
		{
			if (value != _isFemale)
			{
				_isFemale = value;
				OnPropertyChangedWithValue(value, "IsFemale");
			}
		}
	}

	[DataSourceProperty]
	public bool IsHidden
	{
		get
		{
			return _isHidden;
		}
		set
		{
			if (value != _isHidden)
			{
				_isHidden = value;
				OnPropertyChangedWithValue(value, "IsHidden");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPlayingCustomAnimations
	{
		get
		{
			return _isPlayingCustomAnimations;
		}
		set
		{
			if (value != _isPlayingCustomAnimations)
			{
				_isPlayingCustomAnimations = value;
				OnPropertyChangedWithValue(value, "IsPlayingCustomAnimations");
				if (!_isPlayingCustomAnimations && !_isManuallyStoppingAnimation && !ShouldLoopCustomAnimation)
				{
					OnCustomAnimationFinished?.Invoke(this);
				}
			}
		}
	}

	[DataSourceProperty]
	public bool ShouldLoopCustomAnimation
	{
		get
		{
			return _shouldLoopCustomAnimation;
		}
		set
		{
			if (value != _shouldLoopCustomAnimation)
			{
				_shouldLoopCustomAnimation = value;
				OnPropertyChangedWithValue(value, "ShouldLoopCustomAnimation");
			}
		}
	}

	[DataSourceProperty]
	public float CustomAnimationProgressRatio
	{
		get
		{
			return _customAnimationProgressRatio;
		}
		set
		{
			if (value != _customAnimationProgressRatio)
			{
				_customAnimationProgressRatio = value;
				OnPropertyChangedWithValue(value, "CustomAnimationProgressRatio");
			}
		}
	}

	[DataSourceProperty]
	public float CustomAnimationWaitDuration
	{
		get
		{
			return _customAnimationWaitDuration;
		}
		set
		{
			if (value != _customAnimationWaitDuration)
			{
				_customAnimationWaitDuration = value;
				OnPropertyChangedWithValue(value, "CustomAnimationWaitDuration");
			}
		}
	}

	[DataSourceProperty]
	public int Race
	{
		get
		{
			return _race;
		}
		set
		{
			if (value != _race)
			{
				_race = value;
				OnPropertyChangedWithValue(value, "Race");
			}
		}
	}

	[DataSourceProperty]
	public bool HasMount
	{
		get
		{
			return _hasMount;
		}
		set
		{
			if (value != _hasMount)
			{
				_hasMount = value;
				OnPropertyChangedWithValue(value, "HasMount");
			}
		}
	}

	[DataSourceProperty]
	public string EquipmentCode
	{
		get
		{
			return _equipmentCode;
		}
		set
		{
			if (value != _equipmentCode)
			{
				_equipmentCode = value;
				OnPropertyChangedWithValue(value, "EquipmentCode");
			}
		}
	}

	[DataSourceProperty]
	public string IdleAction
	{
		get
		{
			return _idleAction;
		}
		set
		{
			if (value != _idleAction)
			{
				_idleAction = value;
				OnPropertyChangedWithValue(value, "IdleAction");
			}
		}
	}

	[DataSourceProperty]
	public string IdleFaceAnim
	{
		get
		{
			return _idleFaceAnim;
		}
		set
		{
			if (value != _idleFaceAnim)
			{
				_idleFaceAnim = value;
				OnPropertyChangedWithValue(value, "IdleFaceAnim");
			}
		}
	}

	[DataSourceProperty]
	public uint ArmorColor1
	{
		get
		{
			return _armorColor1;
		}
		set
		{
			if (value != _armorColor1)
			{
				_armorColor1 = value;
				OnPropertyChangedWithValue(value, "ArmorColor1");
			}
		}
	}

	[DataSourceProperty]
	public uint ArmorColor2
	{
		get
		{
			return _armorColor2;
		}
		set
		{
			if (value != _armorColor2)
			{
				_armorColor2 = value;
				OnPropertyChangedWithValue(value, "ArmorColor2");
			}
		}
	}

	[DataSourceProperty]
	public int LeftHandWieldedEquipmentIndex
	{
		get
		{
			return _leftHandWieldedEquipmentIndex;
		}
		set
		{
			if (value != _leftHandWieldedEquipmentIndex)
			{
				_leftHandWieldedEquipmentIndex = value;
				OnPropertyChangedWithValue(value, "LeftHandWieldedEquipmentIndex");
			}
		}
	}

	[DataSourceProperty]
	public int RightHandWieldedEquipmentIndex
	{
		get
		{
			return _rightHandWieldedEquipmentIndex;
		}
		set
		{
			if (value != _rightHandWieldedEquipmentIndex)
			{
				_rightHandWieldedEquipmentIndex = value;
				OnPropertyChangedWithValue(value, "RightHandWieldedEquipmentIndex");
			}
		}
	}

	public CharacterViewModel()
	{
	}

	public CharacterViewModel(StanceTypes stance = StanceTypes.None)
	{
		_equipment = new Equipment(Equipment.EquipmentType.Battle);
		EquipmentCode = _equipment.CalculateEquipmentCode();
		StanceIndex = (int)stance;
	}

	public void SetEquipment(EquipmentIndex index, EquipmentElement item)
	{
		_equipment[(int)index] = item;
		EquipmentCode = _equipment.CalculateEquipmentCode();
		HasMount = _equipment?[10].Item != null;
	}

	public virtual void SetEquipment(Equipment equipment)
	{
		_equipment = equipment?.Clone();
		HasMount = _equipment?[10].Item != null;
		EquipmentCode = _equipment?.CalculateEquipmentCode();
		if (!string.IsNullOrEmpty(CharStringId))
		{
			MountCreationKey = TaleWorlds.Core.MountCreationKey.GetRandomMountKeyString(equipment?[10].Item, Common.GetDJB2(CharStringId));
		}
	}

	public void FillFrom(BasicCharacterObject character, int seed = -1, string bannerCode = null)
	{
		if (FaceGen.GetMaturityTypeWithAge(character.Age) > BodyMeshMaturityType.Child)
		{
			if (character.Culture != null)
			{
				ArmorColor1 = character.Culture.Color;
				ArmorColor2 = character.Culture.Color2;
			}
			CharStringId = character.StringId;
			IsFemale = character.IsFemale;
			Race = character.Race;
			BodyProperties = character.GetBodyProperties(character.Equipment, seed).ToString();
			MountCreationKey = TaleWorlds.Core.MountCreationKey.GetRandomMountKeyString(character.Equipment?[10].Item, Common.GetDJB2(character.StringId));
			_equipment = character.Equipment?.Clone();
			HasMount = _equipment?[10].Item != null;
			EquipmentCode = _equipment?.CalculateEquipmentCode();
			BannerCodeText = bannerCode;
		}
	}

	public void FillFrom(CharacterViewModel characterViewModel, int seed = -1)
	{
		ArmorColor1 = characterViewModel.ArmorColor1;
		ArmorColor2 = characterViewModel.ArmorColor2;
		CharStringId = characterViewModel.CharStringId;
		IsFemale = characterViewModel.IsFemale;
		Race = characterViewModel.Race;
		BodyProperties = characterViewModel.BodyProperties;
		MountCreationKey = characterViewModel.MountCreationKey;
		_equipment = characterViewModel._equipment.Clone();
		HasMount = _equipment?[10].Item != null;
		EquipmentCode = _equipment?.CalculateEquipmentCode();
		BannerCodeText = characterViewModel.BannerCodeText;
	}

	public void ExecuteEquipWeaponAtIndex(EquipmentIndex index, bool isLeftHand)
	{
		if (_equipment?[index].Item?.WeaponComponent != null)
		{
			if (isLeftHand)
			{
				LeftHandWieldedEquipmentIndex = (int)index;
			}
			else
			{
				RightHandWieldedEquipmentIndex = (int)index;
			}
		}
	}

	public void ExecuteStartCustomAnimation(string animation, bool loop = false, float loopInterval = 0f)
	{
		ExecuteStopCustomAnimation();
		CustomAnimation = animation;
		ShouldLoopCustomAnimation = loop;
		CustomAnimationWaitDuration = loopInterval;
		IsPlayingCustomAnimations = true;
	}

	public void ExecuteStopCustomAnimation()
	{
		_isManuallyStoppingAnimation = true;
		CustomAnimation = null;
		ShouldLoopCustomAnimation = false;
		CustomAnimationWaitDuration = 0f;
		if (IsPlayingCustomAnimations)
		{
			OnCustomAnimationFinished?.Invoke(this);
		}
		IsPlayingCustomAnimations = false;
		_isManuallyStoppingAnimation = false;
	}
}
