using TaleWorlds.CampaignSystem.ViewModelCollection.Quests;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.Missions.NameMarker;

public abstract class MissionNameMarkerTargetBaseVM : ViewModel
{
	private MBBindingList<QuestMarkerVM> _quests;

	private Vec2 _screenPosition;

	private int _distance;

	private string _name;

	private string _iconType = string.Empty;

	private string _nameType = string.Empty;

	private bool _isEnabled;

	private bool _isTracked;

	private bool _isQuestMainStory;

	private bool _isEnemy;

	private bool _isFriendly;

	private bool _isPersistent;

	[DataSourceProperty]
	public MBBindingList<QuestMarkerVM> Quests
	{
		get
		{
			return _quests;
		}
		set
		{
			if (value != _quests)
			{
				_quests = value;
				OnPropertyChangedWithValue(value, "Quests");
			}
		}
	}

	[DataSourceProperty]
	public Vec2 ScreenPosition
	{
		get
		{
			return _screenPosition;
		}
		set
		{
			if (value.x != _screenPosition.x || value.y != _screenPosition.y)
			{
				_screenPosition = value;
				OnPropertyChangedWithValue(value, "ScreenPosition");
			}
		}
	}

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public string IconType
	{
		get
		{
			return _iconType;
		}
		set
		{
			if (value != _iconType)
			{
				_iconType = value;
				OnPropertyChangedWithValue(value, "IconType");
			}
		}
	}

	[DataSourceProperty]
	public string NameType
	{
		get
		{
			return _nameType;
		}
		set
		{
			if (value != _nameType)
			{
				_nameType = value;
				OnPropertyChangedWithValue(value, "NameType");
			}
		}
	}

	[DataSourceProperty]
	public int Distance
	{
		get
		{
			return _distance;
		}
		set
		{
			if (value != _distance)
			{
				_distance = value;
				OnPropertyChangedWithValue(value, "Distance");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTracked
	{
		get
		{
			return _isTracked;
		}
		set
		{
			if (value != _isTracked)
			{
				_isTracked = value;
				OnPropertyChangedWithValue(value, "IsTracked");
			}
		}
	}

	[DataSourceProperty]
	public bool IsQuestMainStory
	{
		get
		{
			return _isQuestMainStory;
		}
		set
		{
			if (value != _isQuestMainStory)
			{
				_isQuestMainStory = value;
				OnPropertyChangedWithValue(value, "IsQuestMainStory");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEnemy
	{
		get
		{
			return _isEnemy;
		}
		set
		{
			if (value != _isEnemy)
			{
				_isEnemy = value;
				OnPropertyChangedWithValue(value, "IsEnemy");
			}
		}
	}

	[DataSourceProperty]
	public bool IsFriendly
	{
		get
		{
			return _isFriendly;
		}
		set
		{
			if (value != _isFriendly)
			{
				_isFriendly = value;
				OnPropertyChangedWithValue(value, "IsFriendly");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPersistent
	{
		get
		{
			return _isPersistent;
		}
		set
		{
			if (value != _isPersistent)
			{
				_isPersistent = value;
				OnPropertyChangedWithValue(value, "IsPersistent");
				if (IsPersistent)
				{
					SetEnabledState(enabled: true);
				}
				else if (!IsEnabled)
				{
					SetEnabledState(enabled: false);
				}
			}
		}
	}

	public MissionNameMarkerTargetBaseVM()
	{
		Quests = new MBBindingList<QuestMarkerVM>();
	}

	public abstract void UpdatePosition(Camera missionCamera);

	public abstract bool Equals(MissionNameMarkerTargetBaseVM other);

	protected abstract TextObject GetName();

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = GetName().ToString();
	}

	protected void UpdatePositionWith(Camera missionCamera, Vec3 worldPosition)
	{
		float screenX = -100f;
		float screenY = -100f;
		float w = 0f;
		MBWindowManager.WorldToScreenInsideUsableArea(missionCamera, worldPosition, ref screenX, ref screenY, ref w);
		if (w > 0f)
		{
			ScreenPosition = new Vec2(screenX, screenY);
			Distance = (int)(worldPosition - missionCamera.Position).Length;
		}
		else
		{
			Distance = -1;
			ScreenPosition = new Vec2(-500f, -500f);
		}
	}

	public void SetEnabledState(bool enabled)
	{
		IsEnabled = IsPersistent || enabled;
	}
}
