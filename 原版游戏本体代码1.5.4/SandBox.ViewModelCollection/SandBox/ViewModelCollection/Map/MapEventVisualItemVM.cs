using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.Map;

public class MapEventVisualItemVM : ViewModel
{
	private Camera _mapCamera;

	private bool _isAVisibleEvent;

	private CampaignVec2 _mapEventPositionCache;

	private const float CameraDistanceCutoff = 200f;

	private Vec2 _bindPosition;

	private bool _bindIsVisibleOnMap;

	private float _latestX;

	private float _latestY;

	private float _latestW;

	private Vec2 _position;

	private int _eventType;

	private bool _isVisibleOnMap;

	public MapEvent MapEvent { get; private set; }

	public Vec2 Position
	{
		get
		{
			return _position;
		}
		set
		{
			if (_position != value)
			{
				_position = value;
				OnPropertyChangedWithValue(value, "Position");
			}
		}
	}

	public int EventType
	{
		get
		{
			return _eventType;
		}
		set
		{
			if (_eventType != value)
			{
				_eventType = value;
				OnPropertyChangedWithValue(value, "EventType");
			}
		}
	}

	public bool IsVisibleOnMap
	{
		get
		{
			return _isVisibleOnMap;
		}
		set
		{
			if (_isVisibleOnMap != value)
			{
				_isVisibleOnMap = value;
				OnPropertyChangedWithValue(value, "IsVisibleOnMap");
			}
		}
	}

	public MapEventVisualItemVM(Camera mapCamera, MapEvent mapEvent)
	{
		_mapCamera = mapCamera;
		MapEvent = mapEvent;
		_mapEventPositionCache = mapEvent.Position;
	}

	public void UpdateProperties()
	{
		EventType = (int)SandBoxUIHelper.GetMapEventVisualTypeFromMapEvent(MapEvent);
		_isAVisibleEvent = MapEvent.IsVisible;
	}

	public void ParallelUpdatePosition()
	{
		_latestX = 0f;
		_latestY = 0f;
		_latestW = 0f;
		if (_mapEventPositionCache != MapEvent.Position)
		{
			_mapEventPositionCache = MapEvent.Position;
		}
		MBWindowManager.WorldToScreenInsideUsableArea(_mapCamera, _mapEventPositionCache.AsVec3() + new Vec3(0f, 0f, 1.5f), ref _latestX, ref _latestY, ref _latestW);
		_bindPosition = new Vec2(_latestX, _latestY);
	}

	public void DetermineIsVisibleOnMap()
	{
		_bindIsVisibleOnMap = _latestW > 0f && _mapCamera.Position.z < 200f && _isAVisibleEvent;
	}

	public void UpdateBindingProperties()
	{
		Position = _bindPosition;
		IsVisibleOnMap = _bindIsVisibleOnMap;
	}
}
