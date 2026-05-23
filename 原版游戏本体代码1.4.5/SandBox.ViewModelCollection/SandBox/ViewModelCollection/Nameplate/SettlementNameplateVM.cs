using System;
using System.Collections.Generic;
using Helpers;
using SandBox.ViewModelCollection.Nameplate.NameplateNotifications.SettlementNotificationTypes;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.Nameplate;

public class SettlementNameplateVM : NameplateVM
{
	public enum Type
	{
		Village,
		Castle,
		Town
	}

	public enum RelationType
	{
		Neutral,
		SameFaction,
		Enemy,
		Ally
	}

	public enum IssueTypes
	{
		None,
		Possible,
		Active
	}

	public enum MainQuestTypes
	{
		None,
		Possible,
		Active
	}

	private readonly Camera _mapCamera;

	private float _latestX;

	private float _latestY;

	private float _latestW;

	private float _heightOffset;

	private bool _latestIsInsideWindow;

	private Banner _latestBanner;

	private int _latestBannerVersionNo;

	private bool _isTrackedManually;

	private readonly GameEntity _entity;

	private Vec3 _worldPos;

	private Vec3 _worldPosWithHeight;

	private IFaction _currentFaction;

	private readonly Action<CampaignVec2> _fastMoveCameraToPosition;

	private readonly bool _isVillage;

	private readonly bool _isCastle;

	private readonly bool _isTown;

	private float _wPosAfterPositionCalculation;

	private string _bindName;

	private string _bindFactionColor;

	private bool _bindIsTracked;

	private BannerImageIdentifierVM _bindBanner;

	private int _bindRelation;

	private float _bindWPos;

	private float _bindDistanceToCamera;

	private int _bindWSign;

	private bool _bindIsInside;

	private Vec2 _bindPosition;

	private bool _bindIsVisibleOnMap;

	private bool _bindIsInRange;

	private bool _bindHasPort;

	private int _bindPortLevel;

	private List<Clan> _rebelliousClans;

	private string _name;

	private int _settlementType = -1;

	private BannerImageIdentifierVM _banner;

	private int _relation;

	private int _wSign;

	private float _wPos;

	private bool _isTracked;

	private bool _isInside;

	private bool _isInRange;

	private bool _hasPort;

	private int _portLevel;

	private int _mapEventVisualType;

	private SettlementNameplateNotificationsVM _settlementNotifications;

	private SettlementNameplatePartyMarkersVM _settlementParties;

	private SettlementNameplateEventsVM _settlementEvents;

	public Settlement Settlement { get; }

	public Type SettlementTypeEnum { get; private set; }

	public SettlementNameplateNotificationsVM SettlementNotifications
	{
		get
		{
			return _settlementNotifications;
		}
		set
		{
			if (value != _settlementNotifications)
			{
				_settlementNotifications = value;
				OnPropertyChangedWithValue(value, "SettlementNotifications");
			}
		}
	}

	public SettlementNameplatePartyMarkersVM SettlementParties
	{
		get
		{
			return _settlementParties;
		}
		set
		{
			if (value != _settlementParties)
			{
				_settlementParties = value;
				OnPropertyChangedWithValue(value, "SettlementParties");
			}
		}
	}

	public SettlementNameplateEventsVM SettlementEvents
	{
		get
		{
			return _settlementEvents;
		}
		set
		{
			if (value != _settlementEvents)
			{
				_settlementEvents = value;
				OnPropertyChangedWithValue(value, "SettlementEvents");
			}
		}
	}

	public int Relation
	{
		get
		{
			return _relation;
		}
		set
		{
			if (value != _relation)
			{
				_relation = value;
				OnPropertyChangedWithValue(value, "Relation");
			}
		}
	}

	public int MapEventVisualType
	{
		get
		{
			return _mapEventVisualType;
		}
		set
		{
			if (value != _mapEventVisualType)
			{
				_mapEventVisualType = value;
				OnPropertyChangedWithValue(value, "MapEventVisualType");
			}
		}
	}

	public int WSign
	{
		get
		{
			return _wSign;
		}
		set
		{
			if (value != _wSign)
			{
				_wSign = value;
				OnPropertyChangedWithValue(value, "WSign");
			}
		}
	}

	public float WPos
	{
		get
		{
			return _wPos;
		}
		set
		{
			if (value != _wPos)
			{
				_wPos = value;
				OnPropertyChangedWithValue(value, "WPos");
			}
		}
	}

	public BannerImageIdentifierVM Banner
	{
		get
		{
			return _banner;
		}
		set
		{
			if (value != _banner)
			{
				_banner = value;
				OnPropertyChangedWithValue(value, "Banner");
			}
		}
	}

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

	public bool IsTracked
	{
		get
		{
			if (!_isTracked)
			{
				return _bindIsTargetedByTutorial;
			}
			return true;
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

	public bool IsInside
	{
		get
		{
			return _isInside;
		}
		set
		{
			if (value != _isInside)
			{
				_isInside = value;
				OnPropertyChangedWithValue(value, "IsInside");
			}
		}
	}

	public bool IsInRange
	{
		get
		{
			return _isInRange;
		}
		set
		{
			if (value != _isInRange)
			{
				_isInRange = value;
				OnPropertyChangedWithValue(value, "IsInRange");
				if (IsInRange)
				{
					SettlementNotifications.RegisterEvents();
					SettlementParties.RegisterEvents();
					SettlementEvents?.RegisterEvents();
				}
				else
				{
					SettlementNotifications.UnloadEvents();
					SettlementParties.UnloadEvents();
					SettlementEvents?.UnloadEvents();
				}
			}
		}
	}

	public bool HasPort
	{
		get
		{
			return _hasPort;
		}
		set
		{
			if (value != _hasPort)
			{
				_hasPort = value;
				OnPropertyChangedWithValue(value, "HasPort");
			}
		}
	}

	public int PortLevel
	{
		get
		{
			return _portLevel;
		}
		set
		{
			if (value != _portLevel)
			{
				_portLevel = value;
				OnPropertyChangedWithValue(value, "PortLevel");
			}
		}
	}

	public int SettlementType
	{
		get
		{
			return _settlementType;
		}
		set
		{
			if (value != _settlementType)
			{
				_settlementType = value;
				OnPropertyChangedWithValue(value, "SettlementType");
			}
		}
	}

	public SettlementNameplateVM(Settlement settlement, GameEntity entity, Camera mapCamera, Action<CampaignVec2> fastMoveCameraToPosition)
	{
		Settlement = settlement;
		_mapCamera = mapCamera;
		_entity = entity;
		_fastMoveCameraToPosition = fastMoveCameraToPosition;
		SettlementNotifications = new SettlementNameplateNotificationsVM(settlement);
		SettlementParties = new SettlementNameplatePartyMarkersVM(settlement);
		SettlementEvents = new SettlementNameplateEventsVM(settlement);
		Name = Settlement.Name.ToString();
		IsTracked = Campaign.Current.VisualTrackerManager.CheckTracked(settlement);
		if (Settlement.IsCastle)
		{
			SettlementTypeEnum = Type.Castle;
			_isCastle = true;
		}
		else if (Settlement.IsVillage)
		{
			SettlementTypeEnum = Type.Village;
			_isVillage = true;
		}
		else if (Settlement.IsTown)
		{
			SettlementTypeEnum = Type.Town;
			_isTown = true;
		}
		else
		{
			SettlementTypeEnum = Type.Village;
			_isTown = true;
		}
		SettlementType = (int)SettlementTypeEnum;
		if (_entity != null)
		{
			_worldPos = _entity.GlobalPosition;
		}
		else
		{
			_worldPos = Settlement.GetPositionAsVec3();
		}
		RefreshDynamicProperties(forceUpdate: false);
		_rebelliousClans = new List<Clan>();
		if (Game.Current != null)
		{
			Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(base.OnTutorialNotificationElementChanged);
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		SettlementNotifications.UnloadEvents();
		SettlementParties.UnloadEvents();
		Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(base.OnTutorialNotificationElementChanged);
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = Settlement.Name.ToString();
	}

	public override void RefreshDynamicProperties(bool forceUpdate)
	{
		base.RefreshDynamicProperties(forceUpdate);
		if ((_bindIsVisibleOnMap && _currentFaction != Settlement.MapFaction) || forceUpdate)
		{
			_bindFactionColor = "#" + Color.UIntToColorString((uint)(((int?)Settlement.MapFaction?.Color) ?? (-1)));
			Banner banner = Settlement.Banner;
			int num = banner?.GetVersionNo() ?? 0;
			if ((_latestBanner != banner && !_latestBanner.IsContentsSameWith(banner)) || _latestBannerVersionNo != num)
			{
				_bindBanner = ((banner != null) ? new BannerImageIdentifierVM(banner, nineGrid: true) : new BannerImageIdentifierVM(null));
				_latestBannerVersionNo = num;
				_latestBanner = banner;
			}
			_currentFaction = Settlement.MapFaction;
		}
		if ((Settlement.Party?.IsVisualDirty ?? false) || forceUpdate)
		{
			_bindName = Settlement.Party.Name.ToString();
		}
		_bindIsTracked = Campaign.Current.VisualTrackerManager.CheckTracked(Settlement);
		if (Settlement.IsHideout)
		{
			_bindIsInRange = (Settlement.SettlementComponent as ISpottable)?.IsSpotted ?? false;
		}
		else
		{
			_bindIsInRange = Settlement.IsInspected;
		}
		if (!(_bindIsVisibleOnMap || forceUpdate))
		{
			return;
		}
		_bindHasPort = Settlement.HasPort;
		_bindPortLevel = 0;
		if (!_bindHasPort)
		{
			return;
		}
		MBList<Building> mBList = Settlement?.Town?.Buildings;
		if (mBList == null)
		{
			return;
		}
		for (int i = 0; i < mBList.Count; i++)
		{
			if (mBList[i].BuildingType.StringId == "building_shipyard")
			{
				_bindPortLevel = mBList[i].CurrentLevel;
			}
		}
	}

	public override void RefreshRelationStatus()
	{
		_bindRelation = 0;
		if (Settlement.OwnerClan != null)
		{
			if (FactionManager.IsAtWarAgainstFaction(Settlement.MapFaction, Hero.MainHero.MapFaction))
			{
				_bindRelation = 2;
			}
			else if (DiplomacyHelper.IsSameFactionAndNotEliminated(Settlement.MapFaction, Hero.MainHero.MapFaction))
			{
				_bindRelation = 1;
			}
			else if (DiplomacyHelper.HasAllianceWithFaction(Settlement.MapFaction, Hero.MainHero.MapFaction))
			{
				_bindRelation = 3;
			}
		}
	}

	public override void RefreshPosition()
	{
		base.RefreshPosition();
		_bindWPos = _wPosAfterPositionCalculation;
		_bindWSign = (int)_bindWPos;
		_bindIsInside = _latestIsInsideWindow;
		if (_bindIsVisibleOnMap)
		{
			_bindPosition = new Vec2(_latestX, _latestY);
		}
		else
		{
			_bindPosition = new Vec2(-1000f, -1000f);
		}
	}

	public override void RefreshTutorialStatus(string newTutorialHighlightElementID)
	{
		base.RefreshTutorialStatus(newTutorialHighlightElementID);
		if (Settlement?.Party?.Id == null)
		{
			Debug.FailedAssert("Settlement party id is null when refreshing tutorial status", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\Nameplate\\SettlementNameplateVM.cs", "RefreshTutorialStatus", 271);
		}
		else
		{
			_bindIsTargetedByTutorial = Settlement.Party.Id == newTutorialHighlightElementID;
		}
	}

	public void OnSiegeEventStartedOnSettlement(SiegeEvent siegeEvent)
	{
		MapEventVisualType = 2;
		if (Settlement.MapFaction == Hero.MainHero.MapFaction && (BannerlordConfig.AutoTrackAttackedSettlements == 0 || (BannerlordConfig.AutoTrackAttackedSettlements == 1 && Settlement.MapFaction.Leader == Hero.MainHero)))
		{
			Track();
		}
	}

	public void OnSiegeEventEndedOnSettlement(SiegeEvent siegeEvent)
	{
		if (Settlement?.Party?.MapEvent != null && !Settlement.Party.MapEvent.IsFinalized)
		{
			OnMapEventStartedOnSettlement(Settlement.Party.MapEvent);
		}
		else
		{
			OnMapEventEndedOnSettlement();
		}
		if (!_isTrackedManually && BannerlordConfig.AutoTrackAttackedSettlements < 2 && Settlement.MapFaction == Hero.MainHero.MapFaction)
		{
			Untrack();
		}
	}

	public void OnMapEventStartedOnSettlement(MapEvent mapEvent)
	{
		MapEventVisualType = (int)SandBoxUIHelper.GetMapEventVisualTypeFromMapEvent(mapEvent);
		if (Settlement.MapFaction == Hero.MainHero.MapFaction && (Settlement.IsUnderRaid || Settlement.IsUnderSiege || Settlement.InRebelliousState) && (BannerlordConfig.AutoTrackAttackedSettlements == 0 || (BannerlordConfig.AutoTrackAttackedSettlements == 1 && Settlement.MapFaction.Leader == Hero.MainHero)))
		{
			Track();
		}
	}

	public void OnMapEventEndedOnSettlement()
	{
		MapEventVisualType = 0;
		if (!_isTrackedManually && BannerlordConfig.AutoTrackAttackedSettlements < 2 && !Settlement.IsUnderSiege && !Settlement.IsUnderRaid && !Settlement.InRebelliousState)
		{
			Untrack();
		}
	}

	public void OnRebelliousClanFormed(Clan clan)
	{
		MapEventVisualType = 4;
		_rebelliousClans.Add(clan);
		if (Settlement.MapFaction == Hero.MainHero.MapFaction && (BannerlordConfig.AutoTrackAttackedSettlements == 0 || (BannerlordConfig.AutoTrackAttackedSettlements == 1 && Settlement.MapFaction.Leader == Hero.MainHero)))
		{
			Track();
		}
	}

	public void OnRebelliousClanDisbanded(Clan clan)
	{
		_rebelliousClans.Remove(clan);
		if (!_rebelliousClans.IsEmpty())
		{
			return;
		}
		if (Settlement.IsUnderSiege)
		{
			MapEventVisualType = 2;
			return;
		}
		MapEventVisualType = 0;
		if (!_isTrackedManually && BannerlordConfig.AutoTrackAttackedSettlements < 2)
		{
			Untrack();
		}
	}

	public void UpdateNameplateMT(Vec3 cameraPosition)
	{
		CalculatePosition(in cameraPosition);
		DetermineIsInsideWindow();
		DetermineIsVisibleOnMap(in cameraPosition);
		RefreshPosition();
		RefreshDynamicProperties(forceUpdate: false);
	}

	private void CalculatePosition(in Vec3 cameraPosition)
	{
		_worldPosWithHeight = _worldPos;
		if (_isVillage)
		{
			_heightOffset = 0.5f + TaleWorlds.Library.MathF.Clamp(cameraPosition.z / 30f, 0f, 1f) * 2.5f;
		}
		else if (_isCastle)
		{
			_heightOffset = 0.5f + TaleWorlds.Library.MathF.Clamp(cameraPosition.z / 30f, 0f, 1f) * 3f;
		}
		else if (_isTown)
		{
			_heightOffset = 0.5f + TaleWorlds.Library.MathF.Clamp(cameraPosition.z / 30f, 0f, 1f) * 6f;
		}
		else
		{
			_heightOffset = 1f;
		}
		_worldPosWithHeight += new Vec3(0f, 0f, _heightOffset);
		if (_worldPosWithHeight.IsValidXYZW && _mapCamera.Position.IsValidXYZW)
		{
			_latestX = 0f;
			_latestY = 0f;
			_latestW = 0f;
			MBWindowManager.WorldToScreenInsideUsableArea(_mapCamera, _worldPosWithHeight, ref _latestX, ref _latestY, ref _latestW);
		}
		bool flag = _latestW < 0f;
		_wPosAfterPositionCalculation = (flag ? (-1f) : 1.1f);
	}

	private void DetermineIsVisibleOnMap(in Vec3 cameraPosition)
	{
		_bindIsVisibleOnMap = IsVisible(in cameraPosition);
	}

	private void DetermineIsInsideWindow()
	{
		_latestIsInsideWindow = IsInsideWindow();
	}

	public void RefreshBindValues()
	{
		base.FactionColor = _bindFactionColor;
		Banner = _bindBanner;
		Relation = _bindRelation;
		WPos = _bindWPos;
		WSign = _bindWSign;
		IsInside = _bindIsInside;
		base.Position = _bindPosition;
		base.IsVisibleOnMap = _bindIsVisibleOnMap;
		IsInRange = _bindIsInRange;
		HasPort = _bindHasPort;
		PortLevel = _bindPortLevel;
		IsTracked = _bindIsTracked;
		base.IsTargetedByTutorial = _bindIsTargetedByTutorial;
		base.DistanceToCamera = _bindDistanceToCamera;
		Name = _bindName;
		if (SettlementNotifications.IsEventsRegistered)
		{
			SettlementNotifications.Tick();
		}
		if (SettlementEvents.IsEventsRegistered)
		{
			SettlementEvents.Tick();
		}
	}

	private bool IsVisible(in Vec3 cameraPosition)
	{
		_bindDistanceToCamera = _worldPos.Distance(cameraPosition);
		if (!IsTracked)
		{
			if (WPos >= 0f && _latestIsInsideWindow)
			{
				if (cameraPosition.z > 400f)
				{
					return Settlement.IsTown;
				}
				if (cameraPosition.z > 200f)
				{
					return Settlement.IsFortification;
				}
				return _bindDistanceToCamera < cameraPosition.z + 100f;
			}
			return false;
		}
		return true;
	}

	private bool IsInsideWindow()
	{
		float num = Screen.RealScreenResolutionWidth * 0.00052083336f;
		if (_latestX <= Screen.RealScreenResolutionWidth + 200f * num && _latestY <= Screen.RealScreenResolutionHeight + 100f * num && _latestX + 200f * num >= 0f)
		{
			return _latestY + 100f * num >= 0f;
		}
		return false;
	}

	public void ExecuteTrack()
	{
		if (IsTracked)
		{
			Untrack();
			_isTrackedManually = false;
		}
		else
		{
			Track();
			_isTrackedManually = true;
		}
	}

	private void Track()
	{
		IsTracked = true;
		if (!Campaign.Current.VisualTrackerManager.CheckTracked(Settlement))
		{
			Campaign.Current.VisualTrackerManager.RegisterObject(Settlement);
		}
	}

	private void Untrack()
	{
		IsTracked = false;
		if (Campaign.Current.VisualTrackerManager.CheckTracked(Settlement))
		{
			Campaign.Current.VisualTrackerManager.RemoveTrackedObject(Settlement);
		}
	}

	public void ExecuteSetCameraPosition()
	{
		_fastMoveCameraToPosition(Settlement.Position);
	}

	public void ExecuteOpenEncyclopedia()
	{
		Campaign.Current.EncyclopediaManager.GoToLink(Settlement.EncyclopediaLink);
	}
}
