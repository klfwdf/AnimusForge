using System;
using TaleWorlds.CampaignSystem.ViewModelCollection.Quests;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.Tracker;

public abstract class MapTrackerItemVM<T> : MapTrackerItemVM where T : ITrackableCampaignObject
{
	public new T TrackedObject => (T)base.TrackedObject;

	protected MapTrackerItemVM(T trackableObject)
		: base(trackableObject)
	{
		base.IsTracked = Campaign.Current.VisualTrackerManager.CheckTracked(TrackedObject);
	}

	protected sealed override void OnUpdateProperties()
	{
		_nameBind = TrackedObject.GetName().ToString();
		Banner banner = TrackedObject.GetBanner();
		_factionVisualBind = new BannerImageIdentifierVM(banner, nineGrid: true);
		_isVisibleOnMapBind = IsVisibleOnMap();
		_canToggleTrackBind = GetCanToggleTrack();
		_questsBind = GetRelatedQuests();
	}

	protected sealed override void OnUpdatePosition(float screenX, float screenY, float screenW)
	{
		_latestX = screenX;
		_latestY = screenY;
		_latestW = screenW;
		_partyPositionBind = new Vec2(_latestX, _latestY);
		_isBehindBind = _latestW < 0f;
	}

	protected sealed override void OnToggleTrack()
	{
		if (GetCanToggleTrack())
		{
			if (base.IsTracked)
			{
				Untrack();
			}
			else
			{
				Track();
			}
		}
	}

	protected sealed override void OnGoToPosition()
	{
		MapTrackerItemVM.OnFastMoveCameraToPosition?.Invoke(new CampaignVec2(TrackedObject.GetPosition().AsVec2, isOnLand: true));
	}

	protected sealed override void OnRefreshBinding()
	{
		base.Name = _nameBind;
		base.IsEnabled = _isVisibleOnMapBind;
		base.IsBehind = _isBehindBind;
		base.FactionVisual = _factionVisualBind;
		base.CanToggleTrack = _canToggleTrackBind;
		if (base.IsEnabled)
		{
			base.PartyPosition = _partyPositionBind;
		}
		if (_previousQuestsBind == _questsBind)
		{
			return;
		}
		base.Quests.Clear();
		CampaignUIHelper.IssueQuestFlags[] issueQuestFlagsValues = CampaignUIHelper.IssueQuestFlagsValues;
		foreach (CampaignUIHelper.IssueQuestFlags issueQuestFlags in issueQuestFlagsValues)
		{
			if (issueQuestFlags != CampaignUIHelper.IssueQuestFlags.None && (_questsBind & issueQuestFlags) != CampaignUIHelper.IssueQuestFlags.None)
			{
				base.Quests.Add(new QuestMarkerVM(issueQuestFlags));
			}
		}
		_previousQuestsBind = _questsBind;
	}

	private void Track()
	{
		base.IsTracked = true;
		if (!Campaign.Current.VisualTrackerManager.CheckTracked(TrackedObject))
		{
			Campaign.Current.VisualTrackerManager.RegisterObject(TrackedObject);
		}
	}

	private void Untrack()
	{
		base.IsTracked = false;
		if (Campaign.Current.VisualTrackerManager.CheckTracked(TrackedObject))
		{
			Campaign.Current.VisualTrackerManager.RemoveTrackedObject(TrackedObject);
		}
	}
}
public abstract class MapTrackerItemVM : ViewModel
{
	public readonly ITrackableCampaignObject TrackedObject;

	protected float _latestX;

	protected float _latestY;

	protected float _latestW;

	protected CampaignUIHelper.IssueQuestFlags _previousQuestsBind;

	protected CampaignUIHelper.IssueQuestFlags _questsBind;

	protected bool _isVisibleOnMapBind;

	protected bool _isBehindBind;

	protected bool _canToggleTrackBind;

	protected string _nameBind;

	protected Vec2 _partyPositionBind;

	protected BannerImageIdentifierVM _factionVisualBind;

	public static Action<CampaignVec2> OnFastMoveCameraToPosition;

	private bool _isTracked;

	private bool _canToggleTrack;

	private bool _isEnabled;

	private bool _isBehind;

	private string _name;

	private string _trackerType;

	private Vec2 _partyPosition;

	private BannerImageIdentifierVM _factionVisual;

	private MBBindingList<QuestMarkerVM> _quests;

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
	public bool CanToggleTrack
	{
		get
		{
			return _canToggleTrack;
		}
		set
		{
			if (value != _canToggleTrack)
			{
				_canToggleTrack = value;
				OnPropertyChangedWithValue(value, "CanToggleTrack");
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
	public bool IsBehind
	{
		get
		{
			return _isBehind;
		}
		set
		{
			if (value != _isBehind)
			{
				_isBehind = value;
				OnPropertyChangedWithValue(value, "IsBehind");
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
	public string TrackerType
	{
		get
		{
			return _trackerType;
		}
		set
		{
			if (value != _trackerType)
			{
				_trackerType = value;
				OnPropertyChangedWithValue(value, "TrackerType");
			}
		}
	}

	[DataSourceProperty]
	public Vec2 PartyPosition
	{
		get
		{
			return _partyPosition;
		}
		set
		{
			if (value != _partyPosition)
			{
				_partyPosition = value;
				OnPropertyChangedWithValue(value, "PartyPosition");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM FactionVisual
	{
		get
		{
			return _factionVisual;
		}
		set
		{
			if (value != _factionVisual)
			{
				_factionVisual = value;
				OnPropertyChangedWithValue(value, "FactionVisual");
			}
		}
	}

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

	public MapTrackerItemVM(ITrackableCampaignObject trackedObject)
	{
		TrackedObject = trackedObject;
		Quests = new MBBindingList<QuestMarkerVM>();
		UpdateProperties();
	}

	protected abstract void OnShowTooltip();

	protected abstract void OnUpdateProperties();

	protected abstract void OnUpdatePosition(float screenX, float screenY, float screenW);

	protected abstract void OnToggleTrack();

	protected abstract void OnGoToPosition();

	protected abstract void OnRefreshBinding();

	protected abstract bool IsVisibleOnMap();

	protected abstract bool GetCanToggleTrack();

	protected abstract string GetTrackerType();

	protected abstract CampaignUIHelper.IssueQuestFlags GetRelatedQuests();

	public void UpdateProperties()
	{
		OnUpdateProperties();
	}

	public void UpdatePosition(float screenX, float screenY, float screenW)
	{
		OnUpdatePosition(screenX, screenY, screenW);
	}

	public void ExecuteToggleTrack()
	{
		OnToggleTrack();
	}

	public void ExecuteGoToPosition()
	{
		OnGoToPosition();
	}

	public void ExecuteShowTooltip()
	{
		OnShowTooltip();
	}

	public void ExecuteHideTooltip()
	{
		MBInformationManager.HideInformations();
	}

	public void RefreshBinding()
	{
		OnRefreshBinding();
		TrackerType = GetTrackerType();
	}
}
