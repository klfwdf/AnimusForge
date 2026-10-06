using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.FaceGenerator;

public class FaceGenVM : ViewModel
{
	public enum FaceGenTabs
	{
		None = -1,
		Body,
		Face,
		Eyes,
		Nose,
		Mouth,
		Hair,
		Taint,
		NumOfFaceGenTabs
	}

	public enum Presets
	{
		Gender = -1,
		FacePresets = -2,
		FaceType = -3,
		EyePresets = -4,
		HairBeardPreset = -5,
		HairType = -6,
		BeardType = -7,
		TaintPresets = -8,
		SoundPresets = -9,
		TaintType = -10,
		Age = -11,
		EyeColor = -12,
		HairAndBeardColor = -13,
		TeethType = -14,
		EyebrowType = -15,
		Scale = -16,
		Weight = -17,
		Build = -18,
		Pitch = -19,
		Race = -20
	}

	public struct GenderBasedSelectedValue
	{
		public int Hair;

		public int Beard;

		public int FaceTexture;

		public int MouthTexture;

		public int Tattoo;

		public int SoundPreset;

		public int EyebrowTexture;

		public void Reset()
		{
			Hair = -1;
			Beard = -1;
			FaceTexture = -1;
			MouthTexture = -1;
			Tattoo = -1;
			SoundPreset = -1;
			EyebrowTexture = -1;
		}
	}

	private const float MultiplayerHeightSliderMinValue = 0.25f;

	private const float MultiplayerHeightSliderMaxValue = 0.75f;

	private readonly IFaceGeneratorHandler _faceGeneratorScreen;

	private bool _characterRefreshEnabled = true;

	private bool _initialValuesSet;

	private readonly BodyGenerator _bodyGenerator;

	private readonly TextObject _affirmitiveText;

	private readonly TextObject _negativeText;

	private FaceGenerationParams _faceGenerationParams = FaceGenerationParams.Create();

	private List<UndoRedoKey> _undoCommands;

	private List<UndoRedoKey> _redoCommands;

	private Dictionary<string, float> _initialValues;

	private List<bool> _isVoiceTypeUsableForOnlyNpc;

	private MBReadOnlyList<bool> _tabAvailabilities;

	private Action<float> _onHeightChanged;

	private Action _onAgeChanged;

	private int _initialRace = -1;

	private int _initialGender = -1;

	private BodyMeshMaturityType _latestMaturityType;

	private bool _isRandomizing;

	private readonly Action<int> _goToIndex;

	private GenderBasedSelectedValue[] genderBasedSelectedValues;

	private readonly Dictionary<FaceGenTabs, MBBindingList<FaceGenPropertyVM>> _tabProperties;

	private List<uint> _skinColors;

	private List<uint> _hairColors;

	private List<uint> _tattooColors;

	private readonly bool _showDebugValues;

	private readonly bool _openedFromMultiplayer;

	private bool _enforceConstraints;

	private IFaceGeneratorCustomFilter _filter;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _doneInputKey;

	private InputKeyItemVM _previousTabInputKey;

	private InputKeyItemVM _nextTabInputKey;

	private MBBindingList<InputKeyItemVM> _cameraControlKeys;

	private bool _isBodyEnabled;

	private bool _isFaceEnabled;

	private bool _isEyesEnabled;

	private bool _isNoseEnabled;

	private bool _isMouthEnabled;

	private bool _isHairEnabled;

	private bool _isTaintEnabled;

	private string _cancelBtnLbl;

	private string _doneBtnLbl;

	private int _initialSelectedTaintType;

	private int _initialSelectedHairType;

	private int _initialSelectedBeardType;

	private float _initialSelectedSkinColor;

	private float _initialSelectedHairColor;

	private float _initialSelectedTaintColor;

	private string _flipHairLbl;

	private string _skinColorLbl;

	private string _raceLbl;

	private string _genderLbl;

	private FaceGenPropertyVM _heightSlider;

	private HintViewModel _bodyHint;

	private HintViewModel _faceHint;

	private HintViewModel _eyesHint;

	private HintViewModel _noseHint;

	private HintViewModel _hairHint;

	private HintViewModel _taintHint;

	private HintViewModel _mouthHint;

	private HintViewModel _redoHint;

	private HintViewModel _undoHint;

	private HintViewModel _randomizeHint;

	private HintViewModel _randomizeAllHint;

	private HintViewModel _resetHint;

	private HintViewModel _resetAllHint;

	private HintViewModel _clothHint;

	private int hairNum;

	private int beardNum;

	private int faceTextureNum;

	private int mouthTextureNum;

	private int eyebrowTextureNum;

	private int faceTattooNum;

	private int _newSoundPresetSize;

	private float _scale = 1f;

	private int _tab = -1;

	private int _selectedRace = -1;

	private int _selectedGender = -1;

	private bool _canChangeGender;

	private bool _canChangeRace;

	private bool _isDressed;

	private bool _characterGamepadControlsEnabled;

	private bool _isUndoEnabled;

	private bool _isRedoEnabled;

	private MBBindingList<FaceGenPropertyVM> _bodyProperties;

	private MBBindingList<FaceGenPropertyVM> _faceProperties;

	private MBBindingList<FaceGenPropertyVM> _eyesProperties;

	private MBBindingList<FaceGenPropertyVM> _noseProperties;

	private MBBindingList<FaceGenPropertyVM> _mouthProperties;

	private MBBindingList<FaceGenPropertyVM> _hairProperties;

	private MBBindingList<FaceGenPropertyVM> _taintProperties;

	private MBBindingList<FacegenListItemVM> _taintTypes;

	private MBBindingList<FacegenListItemVM> _beardTypes;

	private MBBindingList<FacegenListItemVM> _hairTypes;

	private FaceGenPropertyVM _soundPreset;

	private FaceGenPropertyVM _faceTypes;

	private FaceGenPropertyVM _teethTypes;

	private FaceGenPropertyVM _eyebrowTypes;

	private SelectorVM<SelectorItemVM> _skinColorSelector;

	private SelectorVM<SelectorItemVM> _hairColorSelector;

	private SelectorVM<SelectorItemVM> _tattooColorSelector;

	private SelectorVM<SelectorItemVM> _raceSelector;

	private FacegenListItemVM _selectedTaintType;

	private FacegenListItemVM _selectedBeardType;

	private FacegenListItemVM _selectedHairType;

	private string _title = "";

	private int _totalStageCount = -1;

	private int _currentStageIndex = -1;

	private int _furthestIndex = -1;

	private bool _isAgeAvailable
	{
		get
		{
			if (!_openedFromMultiplayer)
			{
				return _showDebugValues;
			}
			return true;
		}
	}

	private bool _isWeightAvailable
	{
		get
		{
			if (_openedFromMultiplayer)
			{
				return _showDebugValues;
			}
			return true;
		}
	}

	private bool _isBuildAvailable
	{
		get
		{
			if (_openedFromMultiplayer)
			{
				return _showDebugValues;
			}
			return true;
		}
	}

	private bool _isRaceAvailable
	{
		get
		{
			if (TaleWorlds.Core.FaceGen.GetRaceCount() <= 1 || _openedFromMultiplayer)
			{
				return _showDebugValues;
			}
			return true;
		}
	}

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
	public InputKeyItemVM PreviousTabInputKey
	{
		get
		{
			return _previousTabInputKey;
		}
		set
		{
			if (value != _previousTabInputKey)
			{
				_previousTabInputKey = value;
				OnPropertyChangedWithValue(value, "PreviousTabInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM NextTabInputKey
	{
		get
		{
			return _nextTabInputKey;
		}
		set
		{
			if (value != _nextTabInputKey)
			{
				_nextTabInputKey = value;
				OnPropertyChangedWithValue(value, "NextTabInputKey");
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
	public bool AreAllTabsEnabled
	{
		get
		{
			if (IsBodyEnabled && IsFaceEnabled && IsEyesEnabled && IsNoseEnabled && IsMouthEnabled && IsHairEnabled)
			{
				return IsTaintEnabled;
			}
			return false;
		}
	}

	[DataSourceProperty]
	public bool IsBodyEnabled
	{
		get
		{
			return _isBodyEnabled;
		}
		set
		{
			if (value != _isBodyEnabled)
			{
				_isBodyEnabled = value;
				OnPropertyChangedWithValue(value, "IsBodyEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsFaceEnabled
	{
		get
		{
			return _isFaceEnabled;
		}
		set
		{
			if (value != _isFaceEnabled)
			{
				_isFaceEnabled = value;
				OnPropertyChangedWithValue(value, "IsFaceEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEyesEnabled
	{
		get
		{
			return _isEyesEnabled;
		}
		set
		{
			if (value != _isEyesEnabled)
			{
				_isEyesEnabled = value;
				OnPropertyChangedWithValue(value, "IsEyesEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsNoseEnabled
	{
		get
		{
			return _isNoseEnabled;
		}
		set
		{
			if (value != _isNoseEnabled)
			{
				_isNoseEnabled = value;
				OnPropertyChangedWithValue(value, "IsNoseEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMouthEnabled
	{
		get
		{
			return _isMouthEnabled;
		}
		set
		{
			if (value != _isMouthEnabled)
			{
				_isMouthEnabled = value;
				OnPropertyChangedWithValue(value, "IsMouthEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsHairEnabled
	{
		get
		{
			return _isHairEnabled;
		}
		set
		{
			if (value != _isHairEnabled)
			{
				_isHairEnabled = value;
				OnPropertyChangedWithValue(value, "IsHairEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTaintEnabled
	{
		get
		{
			return _isTaintEnabled;
		}
		set
		{
			if (value != _isTaintEnabled)
			{
				_isTaintEnabled = value;
				OnPropertyChangedWithValue(value, "IsTaintEnabled");
			}
		}
	}

	[DataSourceProperty]
	public string FlipHairLbl
	{
		get
		{
			return _flipHairLbl;
		}
		set
		{
			if (value != _flipHairLbl)
			{
				_flipHairLbl = value;
				OnPropertyChangedWithValue(value, "FlipHairLbl");
			}
		}
	}

	[DataSourceProperty]
	public string SkinColorLbl
	{
		get
		{
			return _skinColorLbl;
		}
		set
		{
			if (value != _skinColorLbl)
			{
				_skinColorLbl = value;
				OnPropertyChangedWithValue(value, "SkinColorLbl");
			}
		}
	}

	[DataSourceProperty]
	public string RaceLbl
	{
		get
		{
			return _raceLbl;
		}
		set
		{
			if (value != _raceLbl)
			{
				_raceLbl = value;
				OnPropertyChangedWithValue(value, "RaceLbl");
			}
		}
	}

	[DataSourceProperty]
	public string GenderLbl
	{
		get
		{
			return _genderLbl;
		}
		set
		{
			if (value != _genderLbl)
			{
				_genderLbl = value;
				OnPropertyChangedWithValue(value, "GenderLbl");
			}
		}
	}

	[DataSourceProperty]
	public string CancelBtnLbl
	{
		get
		{
			return _cancelBtnLbl;
		}
		set
		{
			if (value != _cancelBtnLbl)
			{
				_cancelBtnLbl = value;
				OnPropertyChangedWithValue(value, "CancelBtnLbl");
			}
		}
	}

	[DataSourceProperty]
	public string DoneBtnLbl
	{
		get
		{
			return _doneBtnLbl;
		}
		set
		{
			if (value != _doneBtnLbl)
			{
				_doneBtnLbl = value;
				OnPropertyChangedWithValue(value, "DoneBtnLbl");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel BodyHint
	{
		get
		{
			return _bodyHint;
		}
		set
		{
			if (value != _bodyHint)
			{
				_bodyHint = value;
				OnPropertyChangedWithValue(value, "BodyHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel FaceHint
	{
		get
		{
			return _faceHint;
		}
		set
		{
			if (value != _faceHint)
			{
				_faceHint = value;
				OnPropertyChangedWithValue(value, "FaceHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel EyesHint
	{
		get
		{
			return _eyesHint;
		}
		set
		{
			if (value != _eyesHint)
			{
				_eyesHint = value;
				OnPropertyChangedWithValue(value, "EyesHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel NoseHint
	{
		get
		{
			return _noseHint;
		}
		set
		{
			if (value != _noseHint)
			{
				_noseHint = value;
				OnPropertyChangedWithValue(value, "NoseHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel HairHint
	{
		get
		{
			return _hairHint;
		}
		set
		{
			if (value != _hairHint)
			{
				_hairHint = value;
				OnPropertyChangedWithValue(value, "HairHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel TaintHint
	{
		get
		{
			return _taintHint;
		}
		set
		{
			if (value != _taintHint)
			{
				_taintHint = value;
				OnPropertyChangedWithValue(value, "TaintHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel MouthHint
	{
		get
		{
			return _mouthHint;
		}
		set
		{
			if (value != _mouthHint)
			{
				_mouthHint = value;
				OnPropertyChangedWithValue(value, "MouthHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel RedoHint
	{
		get
		{
			return _redoHint;
		}
		set
		{
			if (value != _redoHint)
			{
				_redoHint = value;
				OnPropertyChangedWithValue(value, "RedoHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel UndoHint
	{
		get
		{
			return _undoHint;
		}
		set
		{
			if (value != _undoHint)
			{
				_undoHint = value;
				OnPropertyChangedWithValue(value, "UndoHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel RandomizeHint
	{
		get
		{
			return _randomizeHint;
		}
		set
		{
			if (value != _randomizeHint)
			{
				_randomizeHint = value;
				OnPropertyChangedWithValue(value, "RandomizeHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel RandomizeAllHint
	{
		get
		{
			return _randomizeAllHint;
		}
		set
		{
			if (value != _randomizeAllHint)
			{
				_randomizeAllHint = value;
				OnPropertyChangedWithValue(value, "RandomizeAllHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ResetHint
	{
		get
		{
			return _resetHint;
		}
		set
		{
			if (value != _resetHint)
			{
				_resetHint = value;
				OnPropertyChangedWithValue(value, "ResetHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ResetAllHint
	{
		get
		{
			return _resetAllHint;
		}
		set
		{
			if (value != _resetAllHint)
			{
				_resetAllHint = value;
				OnPropertyChangedWithValue(value, "ResetAllHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ClothHint
	{
		get
		{
			return _clothHint;
		}
		set
		{
			if (value != _clothHint)
			{
				_clothHint = value;
				OnPropertyChangedWithValue(value, "ClothHint");
			}
		}
	}

	[DataSourceProperty]
	public int HairNum
	{
		get
		{
			return hairNum;
		}
		set
		{
			if (value != hairNum)
			{
				hairNum = value;
				OnPropertyChangedWithValue(value, "HairNum");
			}
		}
	}

	[DataSourceProperty]
	public SelectorVM<SelectorItemVM> SkinColorSelector
	{
		get
		{
			return _skinColorSelector;
		}
		set
		{
			if (value != _skinColorSelector)
			{
				_skinColorSelector = value;
				OnPropertyChangedWithValue(value, "SkinColorSelector");
			}
		}
	}

	[DataSourceProperty]
	public SelectorVM<SelectorItemVM> HairColorSelector
	{
		get
		{
			return _hairColorSelector;
		}
		set
		{
			if (value != _hairColorSelector)
			{
				_hairColorSelector = value;
				OnPropertyChangedWithValue(value, "HairColorSelector");
			}
		}
	}

	[DataSourceProperty]
	public SelectorVM<SelectorItemVM> TattooColorSelector
	{
		get
		{
			return _tattooColorSelector;
		}
		set
		{
			if (value != _tattooColorSelector)
			{
				_tattooColorSelector = value;
				OnPropertyChangedWithValue(value, "TattooColorSelector");
			}
		}
	}

	[DataSourceProperty]
	public SelectorVM<SelectorItemVM> RaceSelector
	{
		get
		{
			return _raceSelector;
		}
		set
		{
			if (value != _raceSelector)
			{
				_raceSelector = value;
				OnPropertyChangedWithValue(value, "RaceSelector");
			}
		}
	}

	[DataSourceProperty]
	public int Tab
	{
		get
		{
			return _tab;
		}
		set
		{
			if (_tab != value)
			{
				_tab = value;
				OnPropertyChangedWithValue(value, "Tab");
				TryValidateCurrentTab();
			}
			switch (value)
			{
			case 0:
				_faceGeneratorScreen.ChangeToBodyCamera();
				break;
			case 2:
				_faceGeneratorScreen.ChangeToEyeCamera();
				break;
			case 3:
				_faceGeneratorScreen.ChangeToNoseCamera();
				break;
			case 4:
				_faceGeneratorScreen.ChangeToMouthCamera();
				break;
			case 1:
			case 6:
				_faceGeneratorScreen.ChangeToFaceCamera();
				break;
			case 5:
				_faceGeneratorScreen.ChangeToHairCamera();
				break;
			}
			UpdateTitle();
		}
	}

	[DataSourceProperty]
	public int SelectedGender
	{
		get
		{
			return _selectedGender;
		}
		set
		{
			if (_initialGender == -1 && !TryGetInitialValue("SelectedGender", ref _initialGender))
			{
				SetOrAddInitialValue("SelectedGender", value);
			}
			if (_selectedGender != value)
			{
				AddCommand();
				_selectedGender = value;
				UpdateRaceAndGenderBasedResources();
				Refresh(TaleWorlds.Core.FaceGen.UpdateDeformKeys);
				OnPropertyChangedWithValue(value, "SelectedGender");
				OnPropertyChanged("IsFemale");
			}
		}
	}

	[DataSourceProperty]
	public bool IsFemale => SelectedGender != 0;

	[DataSourceProperty]
	public MBBindingList<FaceGenPropertyVM> BodyProperties
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
	public bool CanChangeGender
	{
		get
		{
			return _canChangeGender;
		}
		set
		{
			if (value != _canChangeGender)
			{
				_canChangeGender = value;
				OnPropertyChangedWithValue(value, "CanChangeGender");
			}
		}
	}

	[DataSourceProperty]
	public bool CanChangeRace
	{
		get
		{
			return _canChangeRace;
		}
		set
		{
			if (value != _canChangeRace)
			{
				_canChangeRace = value;
				OnPropertyChangedWithValue(value, "CanChangeRace");
			}
		}
	}

	[DataSourceProperty]
	public bool IsUndoEnabled
	{
		get
		{
			return _isUndoEnabled;
		}
		set
		{
			if (value != _isUndoEnabled)
			{
				_isUndoEnabled = value;
				OnPropertyChangedWithValue(value, "IsUndoEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRedoEnabled
	{
		get
		{
			return _isRedoEnabled;
		}
		set
		{
			if (value != _isRedoEnabled)
			{
				_isRedoEnabled = value;
				OnPropertyChangedWithValue(value, "IsRedoEnabled");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<FaceGenPropertyVM> FaceProperties
	{
		get
		{
			return _faceProperties;
		}
		set
		{
			if (value != _faceProperties)
			{
				_faceProperties = value;
				OnPropertyChangedWithValue(value, "FaceProperties");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<FaceGenPropertyVM> EyesProperties
	{
		get
		{
			return _eyesProperties;
		}
		set
		{
			if (value != _eyesProperties)
			{
				_eyesProperties = value;
				OnPropertyChangedWithValue(value, "EyesProperties");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<FaceGenPropertyVM> NoseProperties
	{
		get
		{
			return _noseProperties;
		}
		set
		{
			if (value != _noseProperties)
			{
				_noseProperties = value;
				OnPropertyChangedWithValue(value, "NoseProperties");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<FaceGenPropertyVM> MouthProperties
	{
		get
		{
			return _mouthProperties;
		}
		set
		{
			if (value != _mouthProperties)
			{
				_mouthProperties = value;
				OnPropertyChangedWithValue(value, "MouthProperties");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<FaceGenPropertyVM> HairProperties
	{
		get
		{
			return _hairProperties;
		}
		set
		{
			if (value != _hairProperties)
			{
				_hairProperties = value;
				OnPropertyChangedWithValue(value, "HairProperties");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<FaceGenPropertyVM> TaintProperties
	{
		get
		{
			return _taintProperties;
		}
		set
		{
			if (value != _taintProperties)
			{
				_taintProperties = value;
				OnPropertyChangedWithValue(value, "TaintProperties");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<FacegenListItemVM> TaintTypes
	{
		get
		{
			return _taintTypes;
		}
		set
		{
			if (value != _taintTypes)
			{
				_taintTypes = value;
				OnPropertyChangedWithValue(value, "TaintTypes");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<FacegenListItemVM> BeardTypes
	{
		get
		{
			return _beardTypes;
		}
		set
		{
			if (value != _beardTypes)
			{
				_beardTypes = value;
				OnPropertyChangedWithValue(value, "BeardTypes");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<FacegenListItemVM> HairTypes
	{
		get
		{
			return _hairTypes;
		}
		set
		{
			if (value != _hairTypes)
			{
				_hairTypes = value;
				OnPropertyChangedWithValue(value, "HairTypes");
			}
		}
	}

	[DataSourceProperty]
	public FaceGenPropertyVM SoundPreset
	{
		get
		{
			return _soundPreset;
		}
		set
		{
			if (value != _soundPreset)
			{
				_soundPreset = value;
				OnPropertyChangedWithValue(value, "SoundPreset");
			}
		}
	}

	[DataSourceProperty]
	public FaceGenPropertyVM EyebrowTypes
	{
		get
		{
			return _eyebrowTypes;
		}
		set
		{
			if (value != _eyebrowTypes)
			{
				_eyebrowTypes = value;
				OnPropertyChangedWithValue(value, "EyebrowTypes");
			}
		}
	}

	[DataSourceProperty]
	public FaceGenPropertyVM TeethTypes
	{
		get
		{
			return _teethTypes;
		}
		set
		{
			if (value != _teethTypes)
			{
				_teethTypes = value;
				OnPropertyChangedWithValue(value, "TeethTypes");
			}
		}
	}

	[DataSourceProperty]
	public bool FlipHairCb
	{
		get
		{
			return _faceGenerationParams.IsHairFlipped;
		}
		set
		{
			if (value != _faceGenerationParams.IsHairFlipped)
			{
				_faceGenerationParams.IsHairFlipped = value;
				OnPropertyChangedWithValue(value, "FlipHairCb");
				UpdateFace();
			}
		}
	}

	[DataSourceProperty]
	public bool IsDressed
	{
		get
		{
			return _isDressed;
		}
		set
		{
			if (value != _isDressed)
			{
				_isDressed = value;
				OnPropertyChangedWithValue(value, "IsDressed");
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

	[DataSourceProperty]
	public FaceGenPropertyVM FaceTypes
	{
		get
		{
			return _faceTypes;
		}
		set
		{
			if (value != _faceTypes)
			{
				_faceTypes = value;
				OnPropertyChangedWithValue(value, "FaceTypes");
			}
		}
	}

	[DataSourceProperty]
	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			if (value != _title)
			{
				_title = value;
				OnPropertyChangedWithValue(value, "Title");
			}
		}
	}

	[DataSourceProperty]
	public int TotalStageCount
	{
		get
		{
			return _totalStageCount;
		}
		set
		{
			if (value != _totalStageCount)
			{
				_totalStageCount = value;
				OnPropertyChangedWithValue(value, "TotalStageCount");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentStageIndex
	{
		get
		{
			return _currentStageIndex;
		}
		set
		{
			if (value != _currentStageIndex)
			{
				_currentStageIndex = value;
				OnPropertyChangedWithValue(value, "CurrentStageIndex");
			}
		}
	}

	[DataSourceProperty]
	public int FurthestIndex
	{
		get
		{
			return _furthestIndex;
		}
		set
		{
			if (value != _furthestIndex)
			{
				_furthestIndex = value;
				OnPropertyChangedWithValue(value, "FurthestIndex");
			}
		}
	}

	public void SetFaceGenerationParams(FaceGenerationParams faceGenerationParams)
	{
		_faceGenerationParams = faceGenerationParams;
	}

	public FaceGenVM(BodyGenerator bodyGenerator, IFaceGeneratorHandler faceGeneratorScreen, Action<float> onHeightChanged, Action onAgeChanged, TextObject affirmitiveText, TextObject negativeText, int currentStageIndex, int totalStagesCount, int furthestIndex, Action<int> goToIndex, bool canChangeGender, bool openedFromMultiplayer, IFaceGeneratorCustomFilter filter)
	{
		_bodyGenerator = bodyGenerator;
		_faceGeneratorScreen = faceGeneratorScreen;
		_showDebugValues = TaleWorlds.Core.FaceGen.ShowDebugValues;
		_affirmitiveText = affirmitiveText;
		_negativeText = negativeText;
		_openedFromMultiplayer = openedFromMultiplayer;
		_filter = filter;
		CanChangeGender = canChangeGender || _showDebugValues;
		_onHeightChanged = onHeightChanged;
		_onAgeChanged = onAgeChanged;
		_goToIndex = goToIndex;
		TotalStageCount = totalStagesCount;
		CurrentStageIndex = currentStageIndex;
		FurthestIndex = furthestIndex;
		CameraControlKeys = new MBBindingList<InputKeyItemVM>();
		BodyProperties = new MBBindingList<FaceGenPropertyVM>();
		FaceProperties = new MBBindingList<FaceGenPropertyVM>();
		EyesProperties = new MBBindingList<FaceGenPropertyVM>();
		NoseProperties = new MBBindingList<FaceGenPropertyVM>();
		MouthProperties = new MBBindingList<FaceGenPropertyVM>();
		HairProperties = new MBBindingList<FaceGenPropertyVM>();
		TaintProperties = new MBBindingList<FaceGenPropertyVM>();
		_tabProperties = new Dictionary<FaceGenTabs, MBBindingList<FaceGenPropertyVM>>
		{
			{
				FaceGenTabs.Body,
				BodyProperties
			},
			{
				FaceGenTabs.Face,
				FaceProperties
			},
			{
				FaceGenTabs.Eyes,
				EyesProperties
			},
			{
				FaceGenTabs.Nose,
				NoseProperties
			},
			{
				FaceGenTabs.Mouth,
				MouthProperties
			},
			{
				FaceGenTabs.Hair,
				HairProperties
			},
			{
				FaceGenTabs.Taint,
				TaintProperties
			}
		};
		TaintTypes = new MBBindingList<FacegenListItemVM>();
		BeardTypes = new MBBindingList<FacegenListItemVM>();
		HairTypes = new MBBindingList<FacegenListItemVM>();
		IsDressed = false;
		genderBasedSelectedValues = new GenderBasedSelectedValue[2];
		genderBasedSelectedValues[0].Reset();
		genderBasedSelectedValues[1].Reset();
		_undoCommands = new List<UndoRedoKey>(100);
		_redoCommands = new List<UndoRedoKey>(100);
		IsUndoEnabled = false;
		IsRedoEnabled = false;
		_initialValues = new Dictionary<string, float>();
		CanChangeRace = _isRaceAvailable;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		BodyHint = new HintViewModel(GameTexts.FindText("str_body"));
		FaceHint = new HintViewModel(GameTexts.FindText("str_face"));
		EyesHint = new HintViewModel(GameTexts.FindText("str_eyes"));
		NoseHint = new HintViewModel(GameTexts.FindText("str_nose"));
		HairHint = new HintViewModel(GameTexts.FindText("str_hair"));
		TaintHint = new HintViewModel(GameTexts.FindText("str_face_gen_markings"));
		MouthHint = new HintViewModel(GameTexts.FindText("str_mouth"));
		RedoHint = new HintViewModel(GameTexts.FindText("str_redo"));
		UndoHint = new HintViewModel(GameTexts.FindText("str_undo"));
		RandomizeHint = new HintViewModel(GameTexts.FindText("str_randomize"));
		RandomizeAllHint = new HintViewModel(GameTexts.FindText("str_randomize_all"));
		ResetHint = new HintViewModel(GameTexts.FindText("str_reset"));
		ResetAllHint = new HintViewModel(GameTexts.FindText("str_reset_all"));
		ClothHint = new HintViewModel(GameTexts.FindText("str_face_gen_change_cloth"));
		FlipHairLbl = new TextObject("{=74PKmRWJ}Flip Hair").ToString();
		SkinColorLbl = GameTexts.FindText("sf_facegen_skin_color").ToString();
		GenderLbl = GameTexts.FindText("sf_facegen_gender").ToString();
		RaceLbl = GameTexts.FindText("sf_facegen_race").ToString();
		Title = GameTexts.FindText("sf_facegen_title").ToString();
		DoneBtnLbl = _affirmitiveText.ToString();
		CancelBtnLbl = _negativeText.ToString();
		_selectedTaintType?.RefreshValues();
		_selectedBeardType?.RefreshValues();
		_selectedHairType?.RefreshValues();
		_bodyProperties.ApplyActionOnAllItems(delegate(FaceGenPropertyVM x)
		{
			x.RefreshValues();
		});
		_faceProperties.ApplyActionOnAllItems(delegate(FaceGenPropertyVM x)
		{
			x.RefreshValues();
		});
		_eyesProperties.ApplyActionOnAllItems(delegate(FaceGenPropertyVM x)
		{
			x.RefreshValues();
		});
		_noseProperties.ApplyActionOnAllItems(delegate(FaceGenPropertyVM x)
		{
			x.RefreshValues();
		});
		_mouthProperties.ApplyActionOnAllItems(delegate(FaceGenPropertyVM x)
		{
			x.RefreshValues();
		});
		_hairProperties.ApplyActionOnAllItems(delegate(FaceGenPropertyVM x)
		{
			x.RefreshValues();
		});
		_taintProperties.ApplyActionOnAllItems(delegate(FaceGenPropertyVM x)
		{
			x.RefreshValues();
		});
		_taintTypes.ApplyActionOnAllItems(delegate(FacegenListItemVM x)
		{
			x.RefreshValues();
		});
		_beardTypes.ApplyActionOnAllItems(delegate(FacegenListItemVM x)
		{
			x.RefreshValues();
		});
		_hairTypes.ApplyActionOnAllItems(delegate(FacegenListItemVM x)
		{
			x.RefreshValues();
		});
		_soundPreset?.RefreshValues();
		_faceTypes?.RefreshValues();
		_teethTypes?.RefreshValues();
		_eyebrowTypes?.RefreshValues();
		_skinColorSelector?.RefreshValues();
		_hairColorSelector?.RefreshValues();
		_tattooColorSelector?.RefreshValues();
		_raceSelector?.RefreshValues();
	}

	public void InitializeHistory(FaceGenHistory faceGenHistory)
	{
		if (faceGenHistory != null)
		{
			if (faceGenHistory.UndoCommands != null)
			{
				_undoCommands = faceGenHistory.UndoCommands;
			}
			if (faceGenHistory.RedoCommands != null)
			{
				_redoCommands = faceGenHistory.RedoCommands;
			}
			if (faceGenHistory.InitialValues != null)
			{
				_initialValues = faceGenHistory.InitialValues;
			}
		}
		IsUndoEnabled = _undoCommands.Count > 0;
		IsRedoEnabled = _redoCommands.Count > 0;
	}

	private void FilterCategories()
	{
		FaceGeneratorStage[] availableStages = _filter.GetAvailableStages();
		IsBodyEnabled = availableStages.Contains(FaceGeneratorStage.Body);
		IsFaceEnabled = availableStages.Contains(FaceGeneratorStage.Face);
		IsEyesEnabled = availableStages.Contains(FaceGeneratorStage.Eyes);
		IsNoseEnabled = availableStages.Contains(FaceGeneratorStage.Nose);
		IsMouthEnabled = availableStages.Contains(FaceGeneratorStage.Mouth);
		IsHairEnabled = availableStages.Contains(FaceGeneratorStage.Hair);
		IsTaintEnabled = availableStages.Contains(FaceGeneratorStage.Taint);
		Tab = (int)availableStages.FirstOrDefault();
	}

	private void SetColorCodes()
	{
		_skinColors = MBBodyProperties.GetSkinColorGradientPoints(_selectedRace, SelectedGender, (int)_bodyGenerator.Character.Age);
		_hairColors = MBBodyProperties.GetHairColorGradientPoints(_selectedRace, SelectedGender, (int)_bodyGenerator.Character.Age);
		_tattooColors = MBBodyProperties.GetTatooColorGradientPoints(_selectedRace, SelectedGender, (int)_bodyGenerator.Character.Age);
		SkinColorSelector = new SelectorVM<SelectorItemVM>(_skinColors.Select(delegate(uint t)
		{
			t %= 4278190080u;
			return "#" + Convert.ToString(t, 16).PadLeft(6, '0').ToUpper() + "FF";
		}).ToList(), TaleWorlds.Library.MathF.Round(_faceGenerationParams.CurrentSkinColorOffset * (float)(_skinColors.Count - 1)), OnSelectSkinColor);
		HairColorSelector = new SelectorVM<SelectorItemVM>(_hairColors.Select(delegate(uint t)
		{
			t %= 4278190080u;
			return "#" + Convert.ToString(t, 16).PadLeft(6, '0').ToUpper() + "FF";
		}).ToList(), TaleWorlds.Library.MathF.Round(_faceGenerationParams.CurrentHairColorOffset * (float)(_hairColors.Count - 1)), OnSelectHairColor);
		TattooColorSelector = new SelectorVM<SelectorItemVM>(_tattooColors.Select(delegate(uint t)
		{
			t %= 4278190080u;
			return "#" + Convert.ToString(t, 16).PadLeft(6, '0').ToUpper() + "FF";
		}).ToList(), TaleWorlds.Library.MathF.Round(_faceGenerationParams.CurrentFaceTattooColorOffset1 * (float)(_tattooColors.Count - 1)), OnSelectTattooColor);
	}

	private void OnSelectSkinColor(SelectorVM<SelectorItemVM> s)
	{
		AddCommand();
		_faceGenerationParams.CurrentSkinColorOffset = (float)s.SelectedIndex / (float)(_skinColors.Count - 1);
		UpdateFace();
	}

	private void OnSelectTattooColor(SelectorVM<SelectorItemVM> s)
	{
		AddCommand();
		_faceGenerationParams.CurrentFaceTattooColorOffset1 = (float)s.SelectedIndex / (float)(_tattooColors.Count - 1);
		UpdateFace();
	}

	private void OnSelectHairColor(SelectorVM<SelectorItemVM> s)
	{
		AddCommand();
		_faceGenerationParams.CurrentHairColorOffset = (float)s.SelectedIndex / (float)(_hairColors.Count - 1);
		UpdateFace();
	}

	private void OnSelectRace(SelectorVM<SelectorItemVM> s)
	{
		AddCommand();
		_selectedRace = s.SelectedIndex;
		if (_initialRace == -1 && !TryGetInitialValue("SelectedRace", ref _initialRace))
		{
			SetOrAddInitialValue("SelectedRace", _selectedRace);
		}
		UpdateRaceAndGenderBasedResources();
		Refresh(clearProperties: true);
	}

	private void OnHeightChanged()
	{
		_onHeightChanged?.Invoke(_heightSlider?.Value ?? 0f);
	}

	private void OnAgeChanged()
	{
		_onAgeChanged?.Invoke();
	}

	private void SetTabAvailabilities()
	{
		_tabAvailabilities = new MBList<bool> { IsBodyEnabled, IsFaceEnabled, IsEyesEnabled, IsNoseEnabled, IsMouthEnabled, IsHairEnabled, IsTaintEnabled };
	}

	public void OnTabClicked(int index)
	{
		Tab = index;
	}

	public void SelectPreviousTab()
	{
		int num = ((Tab <= 0) ? 6 : (Tab - 1));
		while (!_tabAvailabilities[num] && num != Tab)
		{
			num = ((num <= 0) ? 6 : (num - 1));
		}
		Tab = num;
	}

	public void SelectNextTab()
	{
		int num = (Tab + 1) % 7;
		while (!_tabAvailabilities[num] && num != Tab)
		{
			num = (num + 1) % 7;
		}
		Tab = num;
	}

	public void Refresh(bool clearProperties)
	{
		if (!_characterRefreshEnabled)
		{
			return;
		}
		_characterRefreshEnabled = false;
		OnPropertyChanged("FlipHairCb");
		_selectedRace = _faceGenerationParams.CurrentRace;
		SelectedGender = _faceGenerationParams.CurrentGender;
		SetColorCodes();
		int num = 0;
		MBBodyProperties.GetParamsMax(_selectedRace, SelectedGender, (int)_faceGenerationParams.CurrentAge, ref num, ref beardNum, ref faceTextureNum, ref mouthTextureNum, ref faceTattooNum, ref _newSoundPresetSize, ref eyebrowTextureNum, ref _scale);
		HairNum = num;
		MBBodyProperties.GetZeroProbabilities(_selectedRace, SelectedGender, _faceGenerationParams.CurrentAge, ref _faceGenerationParams.TattooZeroProbability);
		if (clearProperties)
		{
			foreach (KeyValuePair<FaceGenTabs, MBBindingList<FaceGenPropertyVM>> tabProperty in _tabProperties)
			{
				tabProperty.Value.Clear();
			}
		}
		int keyTimePoint = 0;
		int keyTimePoint2 = 0;
		int keyTimePoint3 = 0;
		int keyTimePoint4 = 0;
		if (clearProperties)
		{
			int faceGenInstancesLength = MBBodyProperties.GetFaceGenInstancesLength(_faceGenerationParams.CurrentRace, _faceGenerationParams.CurrentGender, (int)_faceGenerationParams.CurrentAge);
			for (int i = 0; i < faceGenInstancesLength; i++)
			{
				DeformKeyData deformKeyData = MBBodyProperties.GetDeformKeyData(i, _faceGenerationParams.CurrentRace, _faceGenerationParams.CurrentGender, (int)_faceGenerationParams.CurrentAge);
				TextObject textObject = new TextObject("{=bsiRNJtk}{NAME}:");
				textObject.SetTextVariable("NAME", GameTexts.FindText("str_facegen_skin", deformKeyData.Id));
				if (GameTexts.FindText("str_facegen_skin", deformKeyData.Id).ToString().Contains("exist"))
				{
					Debug.FailedAssert(deformKeyData.Id + " id name doesn't exist", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\FaceGenerator\\FaceGenVM.cs", "Refresh", 455);
				}
				if (deformKeyData.Id == "weight")
				{
					keyTimePoint2 = deformKeyData.KeyTimePoint;
					continue;
				}
				if (deformKeyData.Id == "build")
				{
					keyTimePoint4 = deformKeyData.KeyTimePoint;
					continue;
				}
				if (deformKeyData.Id == "height")
				{
					keyTimePoint = deformKeyData.KeyTimePoint;
					continue;
				}
				if (deformKeyData.Id == "age")
				{
					keyTimePoint3 = deformKeyData.KeyTimePoint;
					continue;
				}
				float num2 = _faceGenerationParams.KeyWeights[i];
				float initialValue = num2;
				if (!TryGetInitialValue(textObject.ToString(), ref initialValue))
				{
					SetOrAddInitialValue(textObject.ToString(), initialValue);
				}
				FaceGenPropertyVM item = new FaceGenPropertyVM(i, 0.0, 1.0, textObject, deformKeyData.KeyTimePoint, deformKeyData.GroupId, num2, initialValue, UpdateFace, AddCommand, ResetSliderPrevValues, isEnabled: true, isDiscrete: false, addCommandOnValueChange: false);
				if (deformKeyData.GroupId > -1 && deformKeyData.GroupId < 7)
				{
					_tabProperties[(FaceGenTabs)deformKeyData.GroupId].Add(item);
				}
			}
		}
		if (_filter != null)
		{
			FilterCategories();
		}
		else if (_tab == -1)
		{
			IsBodyEnabled = true;
			IsFaceEnabled = true;
			IsEyesEnabled = true;
			IsNoseEnabled = true;
			IsMouthEnabled = true;
			IsHairEnabled = true;
			IsTaintEnabled = true;
			Tab = 0;
		}
		SetTabAvailabilities();
		if (clearProperties)
		{
			TextObject textObject2 = new TextObject("{=G6hYIR5k}Voice Pitch:");
			float voicePitch = _faceGenerationParams.VoicePitch;
			float initialValue2 = voicePitch;
			if (!TryGetInitialValue(textObject2.ToString(), ref initialValue2))
			{
				SetOrAddInitialValue(textObject2.ToString(), initialValue2);
			}
			FaceGenPropertyVM item = new FaceGenPropertyVM(-19, 0.0, 1.0, textObject2, -19, 0, voicePitch, initialValue2, UpdateFace, AddCommand, ResetSliderPrevValues, isEnabled: true, isDiscrete: false, addCommandOnValueChange: false);
			_tabProperties[FaceGenTabs.Body].Add(item);
			TextObject textObject3 = new TextObject("{=cLJdeUWz}Height:");
			float num3 = ((_heightSlider == null) ? _faceGenerationParams.HeightMultiplier : _heightSlider.Value);
			float initialValue3 = num3;
			if (!TryGetInitialValue(textObject3.ToString(), ref initialValue3))
			{
				SetOrAddInitialValue(textObject3.ToString(), initialValue3);
			}
			_heightSlider = new FaceGenPropertyVM(-16, _openedFromMultiplayer ? 0.25f : 0f, _openedFromMultiplayer ? 0.75f : 1f, textObject3, keyTimePoint, 0, num3, initialValue3, UpdateFace, AddCommand, ResetSliderPrevValues, isEnabled: true, isDiscrete: false, addCommandOnValueChange: false);
			_tabProperties[FaceGenTabs.Body].Add(_heightSlider);
			UpdateVoiceIndiciesFromCurrentParameters();
			if (_isAgeAvailable)
			{
				double min = (_openedFromMultiplayer ? 25 : 3);
				TextObject textObject4 = new TextObject("{=H1emUb6k}Age:");
				float currentAge = _faceGenerationParams.CurrentAge;
				float initialValue4 = currentAge;
				if (!TryGetInitialValue(textObject4.ToString(), ref initialValue4))
				{
					SetOrAddInitialValue(textObject4.ToString(), initialValue4);
				}
				item = new FaceGenPropertyVM(-11, min, 128.0, textObject4, keyTimePoint3, 0, currentAge, initialValue4, UpdateFace, AddCommand, ResetSliderPrevValues, isEnabled: true, isDiscrete: false, addCommandOnValueChange: false);
				_tabProperties[FaceGenTabs.Body].Add(item);
			}
			if (_isWeightAvailable)
			{
				TextObject textObject5 = new TextObject("{=zBld61ck}Weight:");
				float currentWeight = _faceGenerationParams.CurrentWeight;
				float initialValue5 = currentWeight;
				if (!TryGetInitialValue(textObject5.ToString(), ref initialValue5))
				{
					SetOrAddInitialValue(textObject5.ToString(), initialValue5);
				}
				item = new FaceGenPropertyVM(-17, 0.0, 1.0, textObject5, keyTimePoint2, 0, currentWeight, initialValue5, UpdateFace, AddCommand, ResetSliderPrevValues, isEnabled: true, isDiscrete: false, addCommandOnValueChange: false);
				_tabProperties[FaceGenTabs.Body].Add(item);
			}
			if (_isBuildAvailable)
			{
				TextObject textObject6 = new TextObject("{=EUAKPHek}Build:");
				float currentBuild = _faceGenerationParams.CurrentBuild;
				float initialValue6 = currentBuild;
				if (!TryGetInitialValue(textObject6.ToString(), ref initialValue6))
				{
					SetOrAddInitialValue(textObject6.ToString(), initialValue6);
				}
				item = new FaceGenPropertyVM(-18, 0.0, 1.0, textObject6, keyTimePoint4, 0, currentBuild, initialValue6, UpdateFace, AddCommand, ResetSliderPrevValues, isEnabled: true, isDiscrete: false, addCommandOnValueChange: false);
				_tabProperties[FaceGenTabs.Body].Add(item);
			}
			TextObject textObject7 = new TextObject("{=qXxpITdc}Eye Color:");
			float currentEyeColorOffset = _faceGenerationParams.CurrentEyeColorOffset;
			float initialValue7 = currentEyeColorOffset;
			if (!TryGetInitialValue(textObject7.ToString(), ref initialValue7))
			{
				SetOrAddInitialValue(textObject7.ToString(), initialValue7);
			}
			item = new FaceGenPropertyVM(-12, 0.0, 1.0, textObject7, -12, 2, currentEyeColorOffset, initialValue7, UpdateFace, AddCommand, ResetSliderPrevValues, isEnabled: true, isDiscrete: false, addCommandOnValueChange: false);
			_tabProperties[FaceGenTabs.Eyes].Add(item);
			RaceSelector = new SelectorVM<SelectorItemVM>(TaleWorlds.Core.FaceGen.GetRaceNames(), _selectedRace, OnSelectRace);
		}
		UpdateRaceAndGenderBasedResources();
		if (!_initialValuesSet)
		{
			_initialSelectedTaintType = _faceGenerationParams.CurrentFaceTattoo;
			if (!TryGetInitialValue("SelectedTaintType", ref _initialSelectedTaintType))
			{
				SetOrAddInitialValue("SelectedTaintType", _initialSelectedTaintType);
			}
			_initialSelectedBeardType = _faceGenerationParams.CurrentBeard;
			if (!TryGetInitialValue("SelectedBeardType", ref _initialSelectedBeardType))
			{
				SetOrAddInitialValue("SelectedBeardType", _initialSelectedBeardType);
			}
			_initialSelectedHairType = _faceGenerationParams.CurrentHair;
			if (!TryGetInitialValue("SelectedHairType", ref _initialSelectedHairType))
			{
				SetOrAddInitialValue("SelectedHairType", _initialSelectedHairType);
			}
			_initialSelectedHairColor = _faceGenerationParams.CurrentHairColorOffset;
			if (!TryGetInitialValue("SelectedHairColor", ref _initialSelectedHairColor))
			{
				SetOrAddInitialValue("SelectedHairColor", _initialSelectedHairColor);
			}
			_initialSelectedSkinColor = _faceGenerationParams.CurrentSkinColorOffset;
			if (!TryGetInitialValue("SelectedSkinColor", ref _initialSelectedSkinColor))
			{
				SetOrAddInitialValue("SelectedSkinColor", _initialSelectedSkinColor);
			}
			_initialSelectedTaintColor = _faceGenerationParams.CurrentFaceTattooColorOffset1;
			if (!TryGetInitialValue("SelectedTaintColor", ref _initialSelectedTaintColor))
			{
				SetOrAddInitialValue("SelectedTaintColor", _initialSelectedTaintColor);
			}
			_initialRace = _selectedRace;
			if (!TryGetInitialValue("SelectedRace", ref _initialRace))
			{
				SetOrAddInitialValue("SelectedRace", _initialRace);
			}
			_initialGender = SelectedGender;
			if (!TryGetInitialValue("SelectedGender", ref _initialGender))
			{
				SetOrAddInitialValue("SelectedGender", _initialGender);
			}
			_initialValuesSet = true;
		}
		_characterRefreshEnabled = true;
		UpdateFace();
	}

	private bool TryGetInitialValue(string propertyName, ref float initialValue)
	{
		if (_initialValues.TryGetValue(propertyName, out var value))
		{
			initialValue = value;
			return true;
		}
		return false;
	}

	private bool TryGetInitialValue(string propertyName, ref int initialValue)
	{
		if (_initialValues.TryGetValue(propertyName, out var value))
		{
			initialValue = (int)value;
			return true;
		}
		return false;
	}

	private void SetOrAddInitialValue(string propertyName, float initialValue)
	{
		if (_initialValues.ContainsKey(propertyName))
		{
			_initialValues[propertyName] = initialValue;
		}
		else
		{
			_initialValues.Add(propertyName, initialValue);
		}
	}

	private void UpdateRaceAndGenderBasedResources()
	{
		int num = 0;
		MBBodyProperties.GetParamsMax(_selectedRace, SelectedGender, (int)_faceGenerationParams.CurrentAge, ref num, ref beardNum, ref faceTextureNum, ref mouthTextureNum, ref faceTattooNum, ref _newSoundPresetSize, ref eyebrowTextureNum, ref _scale);
		HairNum = num;
		int[] source = Enumerable.Range(0, num).ToArray();
		int[] source2 = Enumerable.Range(0, beardNum).ToArray();
		if (_filter != null)
		{
			source = _filter.GetHaircutIndices(_bodyGenerator.Character);
			source2 = _filter.GetFacialHairIndices(_bodyGenerator.Character);
		}
		BeardTypes.Clear();
		for (int i = 0; i < beardNum; i++)
		{
			if (source2.Contains(i) || i == _faceGenerationParams.CurrentBeard)
			{
				FacegenListItemVM item = new FacegenListItemVM("FaceGen\\Beard\\img" + i, i, SetSelectedBeardType);
				BeardTypes.Add(item);
			}
		}
		string text = ((_selectedGender == 1) ? "Female" : "Male");
		HairTypes.Clear();
		for (int j = 0; j < num; j++)
		{
			if (source.Contains(j) || j == _faceGenerationParams.CurrentHair)
			{
				FacegenListItemVM item2 = new FacegenListItemVM("FaceGen\\Hair\\" + text + "\\img" + j, j, SetSelectedHairType);
				HairTypes.Add(item2);
			}
		}
		TaintTypes.Clear();
		for (int k = 0; k < faceTattooNum; k++)
		{
			FacegenListItemVM item3 = new FacegenListItemVM("FaceGen\\Tattoo\\" + text + "\\img" + k, k, SetSelectedTattooType);
			TaintTypes.Add(item3);
		}
		UpdateFace(-20, _selectedRace, calledFromInit: true);
		UpdateFace(-1, _selectedGender, calledFromInit: true);
		if (BeardTypes.Count > 0)
		{
			SetSelectedBeardType(_faceGenerationParams.CurrentBeard, addCommand: false);
		}
		SetSelectedHairType(_faceGenerationParams.CurrentHair, addCommand: false);
		if (TaintTypes.Count > 0)
		{
			SetSelectedTattooType(_faceGenerationParams.CurrentFaceTattoo, addCommand: false);
		}
		UpdateVoiceIndiciesFromCurrentParameters();
		_faceGenerationParams.CurrentFaceTexture = MBMath.ClampInt(_faceGenerationParams.CurrentFaceTexture, 0, faceTextureNum - 1);
		TextObject textObject = new TextObject("{=DmaP2qaR}Skin Type");
		int currentFaceTexture = _faceGenerationParams.CurrentFaceTexture;
		int initialValue = currentFaceTexture;
		if (!TryGetInitialValue(textObject.ToString(), ref initialValue))
		{
			SetOrAddInitialValue(textObject.ToString(), initialValue);
		}
		FaceTypes = new FaceGenPropertyVM(-3, 0.0, faceTextureNum - 1, textObject, -3, 1, currentFaceTexture, initialValue, UpdateFace, AddCommand, ResetSliderPrevValues);
		_faceGenerationParams.CurrentMouthTexture = MBMath.ClampInt(_faceGenerationParams.CurrentMouthTexture, 0, mouthTextureNum - 1);
		TextObject name = new TextObject("{=l2CNxPXG}Teeth Type");
		int currentMouthTexture = _faceGenerationParams.CurrentMouthTexture;
		int num2 = currentMouthTexture;
		TeethTypes = new FaceGenPropertyVM(-14, 0.0, mouthTextureNum - 1, name, -14, 4, currentMouthTexture, num2, UpdateFace, AddCommand, ResetSliderPrevValues);
		_faceGenerationParams.CurrentEyebrow = MBMath.ClampInt(_faceGenerationParams.CurrentEyebrow, 0, eyebrowTextureNum - 1);
		TextObject name2 = new TextObject("{=bIcFZT6L}Eyebrow Type");
		int currentEyebrow = _faceGenerationParams.CurrentEyebrow;
		int num3 = currentEyebrow;
		EyebrowTypes = new FaceGenPropertyVM(-15, 0.0, eyebrowTextureNum - 1, name2, -15, 4, currentEyebrow, num3, UpdateFace, AddCommand, ResetSliderPrevValues);
	}

	private void UpdateVoiceIndiciesFromCurrentParameters()
	{
		_isVoiceTypeUsableForOnlyNpc = MBBodyProperties.GetVoiceTypeUsableForPlayerData(_faceGenerationParams.CurrentRace, SelectedGender, (int)_faceGenerationParams.CurrentAge, _newSoundPresetSize);
		int num = 0;
		for (int i = 0; i < _isVoiceTypeUsableForOnlyNpc.Count; i++)
		{
			if (!_isVoiceTypeUsableForOnlyNpc[i])
			{
				num++;
			}
		}
		TextObject name = new TextObject("{=macpKFaG}Voice");
		int voiceUIIndex = GetVoiceUIIndex();
		int num2 = voiceUIIndex;
		SoundPreset = new FaceGenPropertyVM(-9, 0.0, num - 1, name, -9, 0, voiceUIIndex, num2, UpdateFace, AddCommand, ResetSliderPrevValues);
		Debug.Print("Called GetVoiceTypeUsableForPlayerData");
	}

	private void UpdateFace()
	{
		if (_characterRefreshEnabled)
		{
			_bodyGenerator.RefreshFace(_faceGenerationParams, IsDressed);
			_faceGeneratorScreen.RefreshCharacterEntity();
		}
		SaveGenderBasedSelectedValues();
	}

	private void UpdateFace(int keyNo, float value, bool calledFromInit, bool isNeedRefresh = true)
	{
		if (_enforceConstraints)
		{
			return;
		}
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		if (keyNo > -1)
		{
			if (keyNo < _faceGenerationParams.KeyWeights.Length)
			{
				_faceGenerationParams.KeyWeights[keyNo] = value;
				_enforceConstraints = MBBodyProperties.EnforceConstraints(ref _faceGenerationParams);
				flag3 = _enforceConstraints && !calledFromInit;
			}
		}
		else
		{
			switch ((Presets)keyNo)
			{
			case Presets.Race:
				RestoreRaceGenderBasedSelectedValues();
				_faceGenerationParams.SetRaceGenderAndAdjustParams((int)value, SelectedGender, (int)_faceGenerationParams.CurrentAge);
				break;
			case Presets.Gender:
				RestoreRaceGenderBasedSelectedValues();
				_faceGenerationParams.SetRaceGenderAndAdjustParams(_faceGenerationParams.CurrentRace, (int)value, (int)_faceGenerationParams.CurrentAge);
				break;
			case Presets.FaceType:
				_faceGenerationParams.CurrentFaceTexture = (int)value;
				break;
			case Presets.HairType:
				_faceGenerationParams.CurrentHair = (int)value;
				break;
			case Presets.BeardType:
				_faceGenerationParams.CurrentBeard = (int)value;
				break;
			case Presets.TaintType:
				_faceGenerationParams.CurrentFaceTattoo = (int)value;
				break;
			case Presets.EyeColor:
				_faceGenerationParams.CurrentEyeColorOffset = value;
				break;
			case Presets.Pitch:
				_faceGenerationParams.VoicePitch = value;
				break;
			case Presets.SoundPresets:
				_faceGenerationParams.CurrentVoice = GetVoiceRealIndex((int)value);
				break;
			case Presets.Age:
			{
				_faceGenerationParams.CurrentAge = value;
				_enforceConstraints = MBBodyProperties.EnforceConstraints(ref _faceGenerationParams);
				flag3 = _enforceConstraints && !calledFromInit;
				flag = true;
				flag2 = true;
				BodyMeshMaturityType maturityTypeWithAge = TaleWorlds.Core.FaceGen.GetMaturityTypeWithAge(_faceGenerationParams.CurrentAge);
				if (_latestMaturityType != maturityTypeWithAge)
				{
					UpdateVoiceIndiciesFromCurrentParameters();
					_latestMaturityType = maturityTypeWithAge;
				}
				break;
			}
			case Presets.TeethType:
				_faceGenerationParams.CurrentMouthTexture = (int)value;
				break;
			case Presets.EyebrowType:
				_faceGenerationParams.CurrentEyebrow = (int)value;
				break;
			case Presets.Scale:
				_faceGenerationParams.HeightMultiplier = (_openedFromMultiplayer ? TaleWorlds.Library.MathF.Clamp(value, 0.25f, 0.75f) : TaleWorlds.Library.MathF.Clamp(value, 0f, 1f));
				_enforceConstraints = MBBodyProperties.EnforceConstraints(ref _faceGenerationParams);
				flag3 = _enforceConstraints && !calledFromInit;
				flag2 = true;
				break;
			case Presets.Weight:
				_faceGenerationParams.CurrentWeight = value;
				_enforceConstraints = MBBodyProperties.EnforceConstraints(ref _faceGenerationParams);
				flag3 = _enforceConstraints && !calledFromInit;
				break;
			case Presets.Build:
				_faceGenerationParams.CurrentBuild = value;
				_enforceConstraints = MBBodyProperties.EnforceConstraints(ref _faceGenerationParams);
				flag3 = _enforceConstraints && !calledFromInit;
				break;
			default:
				MBDebug.ShowWarning("Unknown preset!");
				break;
			}
		}
		if (flag3)
		{
			UpdateFacegen();
		}
		if (isNeedRefresh)
		{
			UpdateFace();
		}
		else
		{
			SaveGenderBasedSelectedValues();
		}
		if (!calledFromInit && !_isRandomizing && keyNo < 0)
		{
			switch ((Presets)keyNo)
			{
			case Presets.SoundPresets:
				_faceGeneratorScreen.MakeVoiceDelayed();
				break;
			case Presets.TeethType:
				_faceGeneratorScreen.SetFacialAnimation("facegen_teeth", loop: false);
				break;
			}
		}
		_enforceConstraints = false;
		if (flag)
		{
			OnAgeChanged();
		}
		if (flag2)
		{
			OnHeightChanged();
		}
	}

	private void RestoreRaceGenderBasedSelectedValues()
	{
		if (genderBasedSelectedValues[SelectedGender].FaceTexture > -1)
		{
			_faceGenerationParams.CurrentFaceTexture = genderBasedSelectedValues[SelectedGender].FaceTexture;
		}
		if (genderBasedSelectedValues[SelectedGender].Hair > -1)
		{
			_faceGenerationParams.CurrentHair = genderBasedSelectedValues[SelectedGender].Hair;
		}
		if (genderBasedSelectedValues[SelectedGender].Beard > -1)
		{
			_faceGenerationParams.CurrentBeard = genderBasedSelectedValues[SelectedGender].Beard;
		}
		if (genderBasedSelectedValues[SelectedGender].Tattoo > -1)
		{
			_faceGenerationParams.CurrentFaceTattoo = genderBasedSelectedValues[SelectedGender].Tattoo;
		}
		if (genderBasedSelectedValues[SelectedGender].SoundPreset > -1)
		{
			_faceGenerationParams.CurrentVoice = genderBasedSelectedValues[SelectedGender].SoundPreset;
		}
		if (genderBasedSelectedValues[SelectedGender].MouthTexture > -1)
		{
			_faceGenerationParams.CurrentMouthTexture = genderBasedSelectedValues[SelectedGender].MouthTexture;
		}
		if (genderBasedSelectedValues[SelectedGender].EyebrowTexture > -1)
		{
			_faceGenerationParams.CurrentEyebrow = genderBasedSelectedValues[SelectedGender].EyebrowTexture;
		}
	}

	private void SaveGenderBasedSelectedValues()
	{
		genderBasedSelectedValues[SelectedGender].FaceTexture = _faceGenerationParams.CurrentFaceTexture;
		genderBasedSelectedValues[SelectedGender].Hair = _faceGenerationParams.CurrentHair;
		genderBasedSelectedValues[SelectedGender].Beard = _faceGenerationParams.CurrentBeard;
		genderBasedSelectedValues[SelectedGender].Tattoo = _faceGenerationParams.CurrentFaceTattoo;
		genderBasedSelectedValues[SelectedGender].SoundPreset = _faceGenerationParams.CurrentVoice;
		genderBasedSelectedValues[SelectedGender].MouthTexture = _faceGenerationParams.CurrentMouthTexture;
		genderBasedSelectedValues[SelectedGender].EyebrowTexture = _faceGenerationParams.CurrentEyebrow;
	}

	private int GetVoiceUIIndex()
	{
		int num = 0;
		for (int i = 0; i < _faceGenerationParams.CurrentVoice; i++)
		{
			if (!_isVoiceTypeUsableForOnlyNpc[i])
			{
				num++;
			}
		}
		return num;
	}

	private int GetVoiceRealIndex(int UIValue)
	{
		int num = 0;
		for (int i = 0; i < _newSoundPresetSize; i++)
		{
			if (!_isVoiceTypeUsableForOnlyNpc[i])
			{
				if (num == UIValue)
				{
					return i;
				}
				num++;
			}
		}
		Debug.FailedAssert("Cannot calculate voice index", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\FaceGenerator\\FaceGenVM.cs", "GetVoiceRealIndex", 1081);
		return -1;
	}

	public void ExecuteHearCurrentVoiceSample()
	{
		_faceGeneratorScreen.MakeVoice();
	}

	public void ExecuteReset()
	{
		string titleText = GameTexts.FindText("str_reset").ToString();
		string text = new TextObject("{=hiKTvBgF}Are you sure want to reset changes done in this tab? Your changes will be lost.").ToString();
		InformationManager.ShowInquiry(new InquiryData(titleText, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), Reset, null));
	}

	private void Reset()
	{
		TryValidateCurrentTab();
		AddCommand();
		_characterRefreshEnabled = false;
		bool flag = _initialRace != RaceSelector.SelectedIndex;
		switch ((FaceGenTabs)Tab)
		{
		case FaceGenTabs.Body:
			SelectedGender = _initialGender;
			RaceSelector.SelectedIndex = _initialRace;
			SoundPreset.Reset();
			SkinColorSelector.SelectedIndex = TaleWorlds.Library.MathF.Round(_initialSelectedSkinColor * (float)(_skinColors.Count - 1));
			break;
		case FaceGenTabs.Eyes:
			EyebrowTypes.Reset();
			break;
		case FaceGenTabs.Mouth:
			TeethTypes.Reset();
			break;
		case FaceGenTabs.Face:
			FaceTypes.Reset();
			break;
		case FaceGenTabs.Hair:
			SetSelectedBeardType(_initialSelectedBeardType, addCommand: false);
			SetSelectedHairType(_initialSelectedHairType, addCommand: false);
			HairColorSelector.SelectedIndex = TaleWorlds.Library.MathF.Round(_initialSelectedHairColor * (float)(_hairColors.Count - 1));
			break;
		case FaceGenTabs.Taint:
			SetSelectedTattooType(_initialSelectedTaintType, addCommand: false);
			TattooColorSelector.SelectedIndex = TaleWorlds.Library.MathF.Round(_initialSelectedTaintColor * (float)(_tattooColors.Count - 1));
			break;
		}
		if (Tab < 0 || Tab >= 7)
		{
			Debug.FailedAssert("Calling Reset on invalid tab!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\FaceGenerator\\FaceGenVM.cs", "Reset", 1141);
		}
		else
		{
			foreach (FaceGenPropertyVM item in _tabProperties[(FaceGenTabs)Tab])
			{
				if (item.TabID == Tab)
				{
					item.Reset();
				}
			}
		}
		_characterRefreshEnabled = true;
		if (flag)
		{
			Refresh(clearProperties: true);
		}
		UpdateFace();
	}

	private void ResetAll()
	{
		AddCommand();
		_characterRefreshEnabled = false;
		bool flag = _initialRace != RaceSelector.SelectedIndex;
		SelectedGender = _initialGender;
		RaceSelector.SelectedIndex = _initialRace;
		SkinColorSelector.SelectedIndex = TaleWorlds.Library.MathF.Round(_initialSelectedSkinColor * (float)(_skinColors.Count - 1));
		HairColorSelector.SelectedIndex = TaleWorlds.Library.MathF.Round(_initialSelectedHairColor * (float)(_hairColors.Count - 1));
		TattooColorSelector.SelectedIndex = TaleWorlds.Library.MathF.Round(_initialSelectedTaintColor * (float)(_tattooColors.Count - 1));
		SetSelectedBeardType(_initialSelectedBeardType, addCommand: false);
		SetSelectedHairType(_initialSelectedHairType, addCommand: false);
		SetSelectedTattooType(_initialSelectedTaintType, addCommand: false);
		FaceTypes.Reset();
		SoundPreset.Reset();
		TeethTypes.Reset();
		EyebrowTypes.Reset();
		foreach (KeyValuePair<FaceGenTabs, MBBindingList<FaceGenPropertyVM>> tabProperty in _tabProperties)
		{
			foreach (FaceGenPropertyVM item in tabProperty.Value)
			{
				item.Reset();
			}
		}
		_characterRefreshEnabled = true;
		if (flag)
		{
			Refresh(TaleWorlds.Core.FaceGen.UpdateDeformKeys);
		}
		UpdateFace();
	}

	public void ExecuteResetAll()
	{
		string titleText = GameTexts.FindText("str_reset_all").ToString();
		string text = new TextObject("{=1hnq3Kb1}Are you sure want to reset all properties? Your changes will be lost.").ToString();
		InformationManager.ShowInquiry(new InquiryData(titleText, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), ResetAll, null));
	}

	public void ExecuteRandomize()
	{
		TryValidateCurrentTab();
		AddCommand();
		_characterRefreshEnabled = false;
		_isRandomizing = true;
		if (Tab < 0 || Tab >= 7)
		{
			Debug.FailedAssert("Calling ExecuteRandomize on invalid tab!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\FaceGenerator\\FaceGenVM.cs", "ExecuteRandomize", 1219);
		}
		else
		{
			foreach (FaceGenPropertyVM item in _tabProperties[(FaceGenTabs)Tab])
			{
				item.Randomize();
			}
		}
		switch ((FaceGenTabs)Tab)
		{
		case FaceGenTabs.Body:
			SkinColorSelector.SelectedIndex = MBRandom.RandomInt(_skinColors.Count);
			break;
		case FaceGenTabs.Eyes:
			EyebrowTypes.Value = MBRandom.RandomInt((int)EyebrowTypes.Max + 1);
			break;
		case FaceGenTabs.Face:
			FaceTypes.Value = MBRandom.RandomInt((int)FaceTypes.Max + 1);
			break;
		case FaceGenTabs.Hair:
			SetSelectedBeardType(BeardTypes[MBRandom.RandomInt(BeardTypes.Count)], addCommand: false);
			SetSelectedHairType(HairTypes[MBRandom.RandomInt(HairTypes.Count)], addCommand: false);
			HairColorSelector.SelectedIndex = MBRandom.RandomInt(_hairColors.Count);
			break;
		case FaceGenTabs.Taint:
			SetSelectedTattooType(TaintTypes[MBRandom.RandomInt(TaintTypes.Count)], addCommand: false);
			TattooColorSelector.SelectedIndex = MBRandom.RandomInt(_tattooColors.Count);
			break;
		case FaceGenTabs.Mouth:
			TeethTypes.Value = MBRandom.RandomInt((int)TeethTypes.Max + 1);
			break;
		}
		_characterRefreshEnabled = true;
		_isRandomizing = false;
		UpdateFace();
	}

	public void ExecuteRandomizeAll()
	{
		AddCommand();
		_characterRefreshEnabled = false;
		_isRandomizing = true;
		foreach (KeyValuePair<FaceGenTabs, MBBindingList<FaceGenPropertyVM>> tabProperty in _tabProperties)
		{
			foreach (FaceGenPropertyVM item in tabProperty.Value)
			{
				item.Randomize();
			}
		}
		FaceTypes.Value = MBRandom.RandomInt((int)FaceTypes.Max + 1);
		if (BeardTypes.Count > 0)
		{
			SetSelectedBeardType(BeardTypes[MBRandom.RandomInt(BeardTypes.Count)], addCommand: false);
		}
		if (HairTypes.Count > 0)
		{
			SetSelectedHairType(HairTypes[MBRandom.RandomInt(HairTypes.Count)], addCommand: false);
		}
		EyebrowTypes.Value = MBRandom.RandomInt((int)EyebrowTypes.Max + 1);
		TeethTypes.Value = MBRandom.RandomInt((int)TeethTypes.Max + 1);
		if (TaintTypes.Count > 0)
		{
			if (MBRandom.RandomFloat < _faceGenerationParams.TattooZeroProbability)
			{
				SetSelectedTattooType(TaintTypes[0], addCommand: false);
			}
			else
			{
				SetSelectedTattooType(TaintTypes[MBRandom.RandomInt(1, TaintTypes.Count)], addCommand: false);
			}
		}
		TattooColorSelector.SelectedIndex = MBRandom.RandomInt(_tattooColors.Count);
		HairColorSelector.SelectedIndex = MBRandom.RandomInt(_hairColors.Count);
		SkinColorSelector.SelectedIndex = MBRandom.RandomInt(_skinColors.Count);
		_characterRefreshEnabled = true;
		UpdateFace();
		_isRandomizing = false;
	}

	public void ExecuteCancel()
	{
		_faceGeneratorScreen.Cancel();
	}

	public void ExecuteDone()
	{
		_faceGeneratorScreen.Done();
	}

	public void ExecuteRedo()
	{
		if (_redoCommands.Count > 0)
		{
			int index = _redoCommands.Count - 1;
			BodyProperties bodyProperties = _redoCommands[index].BodyProperties;
			int gender = _redoCommands[index].Gender;
			int race = _redoCommands[index].Race;
			_redoCommands.RemoveAt(index);
			_undoCommands.Add(new UndoRedoKey(_faceGenerationParams.CurrentGender, _faceGenerationParams.CurrentRace, _bodyGenerator.CurrentBodyProperties));
			IsRedoEnabled = _redoCommands.Count > 0;
			IsUndoEnabled = _undoCommands.Count > 0;
			_characterRefreshEnabled = false;
			SetBodyProperties(bodyProperties, ignoreDebugValues: false, race, gender);
			_characterRefreshEnabled = true;
		}
	}

	public void ExecuteUndo()
	{
		if (_undoCommands.Count > 0)
		{
			int index = _undoCommands.Count - 1;
			BodyProperties bodyProperties = _undoCommands[index].BodyProperties;
			int gender = _undoCommands[index].Gender;
			int race = _undoCommands[index].Race;
			_undoCommands.RemoveAt(index);
			_redoCommands.Add(new UndoRedoKey(_faceGenerationParams.CurrentGender, _faceGenerationParams.CurrentRace, _bodyGenerator.CurrentBodyProperties));
			IsRedoEnabled = _redoCommands.Count > 0;
			IsUndoEnabled = _undoCommands.Count > 0;
			_characterRefreshEnabled = false;
			SetBodyProperties(bodyProperties, ignoreDebugValues: false, race, gender);
			_characterRefreshEnabled = true;
		}
	}

	public void ExecuteChangeClothing()
	{
		if (IsDressed)
		{
			_faceGeneratorScreen.UndressCharacterEntity();
			IsDressed = false;
		}
		else
		{
			_faceGeneratorScreen.DressCharacterEntity();
			IsDressed = true;
		}
	}

	public void AddCommand()
	{
		if (!_characterRefreshEnabled)
		{
			return;
		}
		UndoRedoKey item = new UndoRedoKey(_faceGenerationParams.CurrentGender, _faceGenerationParams.CurrentRace, _bodyGenerator.CurrentBodyProperties);
		if (_undoCommands.Count > 0)
		{
			UndoRedoKey undoRedoKey = _undoCommands[_undoCommands.Count - 1];
			if (undoRedoKey.Gender == item.Gender && undoRedoKey.Race == item.Race && undoRedoKey.BodyProperties.Equals(item.BodyProperties))
			{
				return;
			}
		}
		if (_undoCommands.Count + 1 == _undoCommands.Capacity)
		{
			_undoCommands.RemoveAt(0);
		}
		_undoCommands.Add(item);
		_redoCommands.Clear();
		IsRedoEnabled = _redoCommands.Count > 0;
		IsUndoEnabled = _undoCommands.Count > 0;
	}

	private void UpdateTitle()
	{
	}

	private void ExecuteGoToIndex(int index)
	{
		_goToIndex(index);
	}

	public void SetBodyProperties(BodyProperties bodyProperties, bool ignoreDebugValues, int race = 0, int gender = -1, bool recordChange = false)
	{
		_characterRefreshEnabled = false;
		bool flag = false;
		if (gender == -1)
		{
			_faceGenerationParams.CurrentGender = _selectedGender;
		}
		else
		{
			_faceGenerationParams.CurrentGender = gender;
		}
		if (_isRaceAvailable)
		{
			flag = _faceGenerationParams.CurrentRace != race;
			_faceGenerationParams.CurrentRace = race;
		}
		if (_openedFromMultiplayer)
		{
			bodyProperties = bodyProperties.ClampForMultiplayer();
		}
		float age = (_isAgeAvailable ? bodyProperties.Age : _bodyGenerator.CurrentBodyProperties.Age);
		float weight = (_isWeightAvailable ? bodyProperties.Weight : _bodyGenerator.CurrentBodyProperties.Weight);
		float build = (_isWeightAvailable ? bodyProperties.Build : _bodyGenerator.CurrentBodyProperties.Build);
		bodyProperties = new BodyProperties(new DynamicBodyProperties(age, weight, build), bodyProperties.StaticProperties);
		_bodyGenerator.CurrentBodyProperties = bodyProperties;
		MBBodyProperties.GetParamsFromKey(ref _faceGenerationParams, bodyProperties, IsDressed && _bodyGenerator.Character.Equipment.EarsAreHidden, IsDressed && _bodyGenerator.Character.Equipment.MouthIsHidden);
		if (flag)
		{
			_characterRefreshEnabled = true;
			Refresh(clearProperties: true);
		}
		else
		{
			UpdateFacegen();
			_characterRefreshEnabled = true;
			UpdateFace();
		}
		if (recordChange)
		{
			_characterRefreshEnabled = true;
			AddCommand();
		}
	}

	private void ResetSliderPrevValues()
	{
		foreach (MBBindingList<FaceGenPropertyVM> value in _tabProperties.Values)
		{
			foreach (FaceGenPropertyVM item in value)
			{
				item.PrevValue = -1.0;
			}
		}
	}

	public void UpdateFacegen()
	{
		foreach (MBBindingList<FaceGenPropertyVM> value in _tabProperties.Values)
		{
			foreach (FaceGenPropertyVM item in value)
			{
				if (item.KeyNo < 0)
				{
					switch ((Presets)item.KeyNo)
					{
					case Presets.EyeColor:
						item.Value = _faceGenerationParams.CurrentEyeColorOffset;
						break;
					case Presets.Age:
						item.Value = _faceGenerationParams.CurrentAge;
						break;
					case Presets.Scale:
						item.Value = (_openedFromMultiplayer ? TaleWorlds.Library.MathF.Clamp(_faceGenerationParams.HeightMultiplier, 0.25f, 0.75f) : TaleWorlds.Library.MathF.Clamp(_faceGenerationParams.HeightMultiplier, 0f, 1f));
						break;
					case Presets.Build:
						item.Value = _faceGenerationParams.CurrentBuild;
						break;
					case Presets.Weight:
						item.Value = _faceGenerationParams.CurrentWeight;
						break;
					case Presets.Pitch:
						item.Value = _faceGenerationParams.VoicePitch;
						break;
					}
				}
				else
				{
					item.Value = _faceGenerationParams.KeyWeights[item.KeyNo];
				}
				item.PrevValue = -1.0;
			}
		}
		SelectedGender = _faceGenerationParams.CurrentGender;
		SoundPreset.Value = GetVoiceUIIndex();
		FaceTypes.Value = _faceGenerationParams.CurrentFaceTexture;
		EyebrowTypes.Value = _faceGenerationParams.CurrentEyebrow;
		TeethTypes.Value = _faceGenerationParams.CurrentMouthTexture;
		SetSelectedTattooType(_faceGenerationParams.CurrentFaceTattoo, addCommand: false);
		SetSelectedBeardType(_faceGenerationParams.CurrentBeard, addCommand: false);
		SetSelectedHairType(_faceGenerationParams.CurrentHair, addCommand: false);
		SkinColorSelector.SelectedIndex = TaleWorlds.Library.MathF.Round(_faceGenerationParams.CurrentSkinColorOffset * (float)(_skinColors.Count - 1));
		HairColorSelector.SelectedIndex = TaleWorlds.Library.MathF.Round(_faceGenerationParams.CurrentHairColorOffset * (float)(_hairColors.Count - 1));
		TattooColorSelector.SelectedIndex = TaleWorlds.Library.MathF.Round(_faceGenerationParams.CurrentFaceTattooColorOffset1 * (float)(_tattooColors.Count - 1));
	}

	private void SetSelectedHairType(FacegenListItemVM item, bool addCommand)
	{
		if (_selectedHairType != null)
		{
			_selectedHairType.IsSelected = false;
		}
		_selectedHairType = item;
		_selectedHairType.IsSelected = true;
		_faceGenerationParams.CurrentHair = item.Index;
		if (addCommand)
		{
			AddCommand();
			UpdateFace(-6, item.Index, calledFromInit: false);
		}
	}

	private void SetSelectedHairType(int index, bool addCommand)
	{
		foreach (FacegenListItemVM hairType in HairTypes)
		{
			if (hairType.Index == index)
			{
				SetSelectedHairType(hairType, addCommand);
				break;
			}
		}
	}

	private void SetSelectedTattooType(FacegenListItemVM item, bool addCommand)
	{
		if (_selectedTaintType != null)
		{
			_selectedTaintType.IsSelected = false;
		}
		_selectedTaintType = item;
		_selectedTaintType.IsSelected = true;
		_faceGenerationParams.CurrentFaceTattoo = item.Index;
		if (addCommand)
		{
			AddCommand();
			UpdateFace(-10, item.Index, calledFromInit: false);
		}
	}

	private void SetSelectedTattooType(int index, bool addCommand)
	{
		foreach (FacegenListItemVM taintType in TaintTypes)
		{
			if (taintType.Index == index)
			{
				SetSelectedTattooType(taintType, addCommand);
				break;
			}
		}
	}

	private void SetSelectedBeardType(FacegenListItemVM item, bool addCommand)
	{
		if (_selectedBeardType != null)
		{
			_selectedBeardType.IsSelected = false;
		}
		_selectedBeardType = item;
		_selectedBeardType.IsSelected = true;
		_faceGenerationParams.CurrentBeard = item.Index;
		if (addCommand)
		{
			AddCommand();
			UpdateFace(-7, item.Index, calledFromInit: false);
		}
	}

	private void SetSelectedBeardType(int index, bool addCommand)
	{
		if (SelectedGender == 1)
		{
			SetSelectedBeardType(BeardTypes.FirstOrDefault(), addCommand);
			return;
		}
		foreach (FacegenListItemVM beardType in BeardTypes)
		{
			if (beardType.Index == index)
			{
				SetSelectedBeardType(beardType, addCommand);
				break;
			}
		}
	}

	private void TryValidateCurrentTab()
	{
		if (Tab <= -1 || Tab >= 7)
		{
			Debug.FailedAssert($"Invalid tab: {Tab}", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\FaceGenerator\\FaceGenVM.cs", "TryValidateCurrentTab", 1680);
			Debug.Print($"Invalid tab: {Tab}");
			Tab = _tabAvailabilities.IndexOf(item: true);
			if (Tab <= -1 || Tab >= 7)
			{
				Debug.FailedAssert("No valid tabs are available!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\FaceGenerator\\FaceGenVM.cs", "TryValidateCurrentTab", 1687);
				Debug.Print("No valid tabs are available!");
			}
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CancelInputKey?.OnFinalize();
		DoneInputKey?.OnFinalize();
		PreviousTabInputKey?.OnFinalize();
		NextTabInputKey?.OnFinalize();
		for (int i = 0; i < CameraControlKeys.Count; i++)
		{
			CameraControlKeys[i].OnFinalize();
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

	public void SetPreviousTabInputKey(HotKey hotKey)
	{
		PreviousTabInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetNextTabInputKey(HotKey hotKey)
	{
		NextTabInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
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

	public void AddCameraControlInputKey(GameAxisKey gameAxisKey)
	{
		TextObject forcedName = Module.CurrentModule.GlobalTextManager.FindText("str_key_name", typeof(FaceGenHotkeyCategory).Name + "_" + gameAxisKey.Id);
		InputKeyItemVM item = InputKeyItemVM.CreateFromForcedID(gameAxisKey.AxisKey.ToString(), forcedName, isConsoleOnly: true);
		CameraControlKeys.Add(item);
	}
}
