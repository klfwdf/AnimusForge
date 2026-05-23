using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Map.DistanceCache;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;

namespace SandBox.View.Map;

public class SettlementPositionScript : ScriptComponentBehavior
{
	private sealed class SettlementRecord : ISettlementDataHolder
	{
		public readonly string SettlementId;

		public readonly XmlNode Node;

		public readonly Vec2 Position;

		public readonly Vec2 GatePosition;

		public readonly bool HasGate;

		public readonly Vec2 PortPosition;

		public readonly bool HasPort;

		public readonly bool IsFortification;

		public string StringId => SettlementId;

		CampaignVec2 ISettlementDataHolder.GatePosition => new CampaignVec2(GatePosition, isOnLand: true);

		CampaignVec2 ISettlementDataHolder.PortPosition => new CampaignVec2(PortPosition, isOnLand: false);

		bool ISettlementDataHolder.IsFortification => IsFortification;

		bool ISettlementDataHolder.HasPort => HasPort;

		public SettlementRecord(string settlementId, Vec2 position, Vec2 gatePosition, XmlNode node, bool hasGate, Vec2 portPosition, bool hasPort, bool isFortification)
		{
			SettlementId = settlementId;
			Position = position;
			GatePosition = gatePosition;
			Node = node;
			HasGate = hasGate;
			PortPosition = portPosition;
			HasPort = hasPort;
			IsFortification = isFortification;
		}
	}

	private sealed class SettlementPositionScriptNavigationCache : NavigationCache<SettlementRecord>
	{
		private readonly Scene Scene;

		private readonly List<SettlementRecord> _settlementRecords;

		private readonly int[] _excludedFaceIds;

		private readonly int _regionSwitchCostTo0;

		private readonly int _regionSwitchCostTo1;

		public SettlementPositionScriptNavigationCache(List<SettlementRecord> settlementRecords, Scene scene, MapDistanceModel mapDistanceModel, PartyNavigationModel partyNavigationModel, MobileParty.NavigationType navigationType)
			: base(navigationType)
		{
			Scene = scene;
			_settlementRecords = settlementRecords;
			_excludedFaceIds = partyNavigationModel.GetInvalidTerrainTypesForNavigationType(base._navigationType);
			_regionSwitchCostTo0 = mapDistanceModel.RegionSwitchCostFromLandToSea;
			_regionSwitchCostTo1 = mapDistanceModel.RegionSwitchCostFromSeaToLand;
		}

		protected override NavigationCacheElement<SettlementRecord> GetCacheElement(SettlementRecord settlement, bool isPortUsed)
		{
			return new NavigationCacheElement<SettlementRecord>(settlement, isPortUsed);
		}

		protected override SettlementRecord GetCacheElement(string settlementId)
		{
			return _settlementRecords.Single((SettlementRecord x) => x.SettlementId == settlementId);
		}

		public override void GetSceneXmlCrcValues(out uint sceneXmlCrc, out uint sceneNavigationMeshCrc)
		{
			sceneXmlCrc = Scene.GetSceneXMLCRC();
			sceneNavigationMeshCrc = Scene.GetNavigationMeshCRC();
		}

		protected override int GetNavMeshFaceCount()
		{
			return Scene.GetNavMeshFaceCount();
		}

		protected override Vec2 GetNavMeshFaceCenterPosition(int faceIndex)
		{
			Vec3 centerPosition = Vec3.Zero;
			Scene.GetNavMeshCenterPosition(faceIndex, ref centerPosition);
			return centerPosition.AsVec2;
		}

		protected override PathFaceRecord GetFaceRecordAtIndex(int faceIndex)
		{
			return Scene.GetNavMeshPathFaceRecord(faceIndex);
		}

		protected override int[] GetExcludedFaceIds()
		{
			return _excludedFaceIds;
		}

		protected override int GetRegionSwitchCostTo0()
		{
			return _regionSwitchCostTo0;
		}

		protected override int GetRegionSwitchCostTo1()
		{
			return _regionSwitchCostTo1;
		}

		protected override IEnumerable<SettlementRecord> GetClosestSettlementsToPositionInCache(Vec2 checkPosition, List<SettlementRecord> settlements)
		{
			if (base._navigationType == MobileParty.NavigationType.Naval)
			{
				return from x in settlements
					where x.HasPort
					orderby checkPosition.DistanceSquared(x.PortPosition)
					select x;
			}
			if (base._navigationType == MobileParty.NavigationType.Default)
			{
				return settlements.OrderBy((SettlementRecord x) => checkPosition.DistanceSquared(x.GatePosition));
			}
			return settlements.OrderBy((SettlementRecord x) => (!x.HasPort) ? checkPosition.DistanceSquared(x.GatePosition) : TaleWorlds.Library.MathF.Min(checkPosition.DistanceSquared(x.GatePosition), checkPosition.DistanceSquared(x.PortPosition)));
		}

		protected override float GetRealPathDistanceFromPositionToSettlement(Vec2 checkPosition, PathFaceRecord currentFaceRecord, float maxDistanceToLookForPathDetection, SettlementRecord currentSettlementToLook, out bool isPort)
		{
			float result = float.MaxValue;
			isPort = false;
			PathFaceRecord record = PathFaceRecord.NullFaceRecord;
			switch (base._navigationType)
			{
			case MobileParty.NavigationType.Default:
			{
				Scene.GetNavMeshFaceIndex(ref record, currentSettlementToLook.GatePosition, isRegion1: true, checkIfDisabled: false, ignoreHeight: true);
				if (Scene.GetPathDistanceBetweenAIFaces(currentFaceRecord.FaceIndex, record.FaceIndex, checkPosition, currentSettlementToLook.GatePosition, 0.3f, maxDistanceToLookForPathDetection, out var distance4, _excludedFaceIds, _regionSwitchCostTo0, _regionSwitchCostTo1))
				{
					result = distance4;
				}
				break;
			}
			case MobileParty.NavigationType.Naval:
			{
				Scene.GetNavMeshFaceIndex(ref record, currentSettlementToLook.PortPosition, isRegion1: false, checkIfDisabled: false, ignoreHeight: true);
				if (Scene.GetPathDistanceBetweenAIFaces(currentFaceRecord.FaceIndex, record.FaceIndex, checkPosition, currentSettlementToLook.PortPosition, 0.3f, maxDistanceToLookForPathDetection, out var distance3, _excludedFaceIds, _regionSwitchCostTo0, _regionSwitchCostTo1))
				{
					result = distance3;
					isPort = true;
				}
				break;
			}
			case MobileParty.NavigationType.All:
			{
				Scene.GetNavMeshFaceIndex(ref record, currentSettlementToLook.GatePosition, isRegion1: true, checkIfDisabled: false, ignoreHeight: true);
				if (Scene.GetPathDistanceBetweenAIFaces(currentFaceRecord.FaceIndex, record.FaceIndex, checkPosition, currentSettlementToLook.GatePosition, 0.3f, maxDistanceToLookForPathDetection, out var distance, _excludedFaceIds, _regionSwitchCostTo0, _regionSwitchCostTo1))
				{
					result = distance;
				}
				if (currentSettlementToLook.HasPort)
				{
					Scene.GetNavMeshFaceIndex(ref record, currentSettlementToLook.PortPosition, isRegion1: false, checkIfDisabled: false, ignoreHeight: true);
					if (Scene.GetPathDistanceBetweenAIFaces(currentFaceRecord.FaceIndex, record.FaceIndex, checkPosition, currentSettlementToLook.PortPosition, 0.3f, maxDistanceToLookForPathDetection, out var distance2, _excludedFaceIds, _regionSwitchCostTo0, _regionSwitchCostTo1) && distance2 < distance)
					{
						result = distance2;
						isPort = true;
					}
				}
				break;
			}
			}
			return result;
		}

		protected override float GetRealDistanceAndLandRatioBetweenSettlements(NavigationCacheElement<SettlementRecord> settlement1, NavigationCacheElement<SettlementRecord> settlement2, out float landRatio)
		{
			Vec2 vec = (settlement1.IsPortUsed ? settlement1.PortPosition.ToVec2() : settlement1.GatePosition.ToVec2());
			Vec2 vec2 = (settlement2.IsPortUsed ? settlement2.PortPosition.ToVec2() : settlement2.GatePosition.ToVec2());
			PathFaceRecord record = PathFaceRecord.NullFaceRecord;
			Scene.GetNavMeshFaceIndex(ref record, vec, !settlement1.IsPortUsed, checkIfDisabled: false, ignoreHeight: true);
			PathFaceRecord record2 = PathFaceRecord.NullFaceRecord;
			Scene.GetNavMeshFaceIndex(ref record2, vec2, !settlement2.IsPortUsed, checkIfDisabled: false, ignoreHeight: true);
			landRatio = 1f;
			if (base._navigationType == MobileParty.NavigationType.Naval)
			{
				landRatio = 0f;
			}
			else if (base._navigationType == MobileParty.NavigationType.All)
			{
				NavigationPath path = new NavigationPath();
				Scene.GetPathBetweenAIFaces(record.FaceIndex, record2.FaceIndex, vec, vec2, 0.3f, path, _excludedFaceIds, 1f, _regionSwitchCostTo0, _regionSwitchCostTo1);
				landRatio = GetLandRatioOfPath(path, vec);
			}
			Scene.GetPathDistanceBetweenAIFaces(record.FaceIndex, record2.FaceIndex, vec, vec2, 0.3f, float.PositiveInfinity, out var distance, _excludedFaceIds, _regionSwitchCostTo0, _regionSwitchCostTo1);
			return distance;
		}

		protected override void GetFaceRecordForPoint(Vec2 position, out bool isOnRegion1)
		{
			isOnRegion1 = true;
			PathFaceRecord record = PathFaceRecord.NullFaceRecord;
			Scene.GetNavMeshFaceIndex(ref record, position, isOnRegion1, checkIfDisabled: false, ignoreHeight: true);
			if (!record.IsValid())
			{
				isOnRegion1 = false;
				Scene.GetNavMeshFaceIndex(ref record, position, isOnRegion1, checkIfDisabled: false, ignoreHeight: true);
			}
			if (!record.IsValid())
			{
				Debug.Print($"{position} has no region data.", 0, Debug.DebugColor.Red);
			}
		}

		protected override bool CheckBeingNeighbor(List<SettlementRecord> settlementsToConsider, SettlementRecord settlement1, SettlementRecord settlement2, bool useGate1, bool useGate2, out float distance)
		{
			Vec2 vec = (useGate1 ? settlement1.GatePosition : settlement1.PortPosition);
			Vec2 vec2 = (useGate2 ? settlement2.GatePosition : settlement2.PortPosition);
			PathFaceRecord record = PathFaceRecord.NullFaceRecord;
			Scene.GetNavMeshFaceIndex(ref record, vec, useGate1, checkIfDisabled: false, ignoreHeight: true);
			PathFaceRecord record2 = PathFaceRecord.NullFaceRecord;
			Scene.GetNavMeshFaceIndex(ref record2, vec2, useGate2, checkIfDisabled: false, ignoreHeight: true);
			if (!record.IsValid() || !record2.IsValid())
			{
				Debug.FailedAssert("Settlement navFace index should not be -1, check here", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\SettlementPositionScript.cs", "CheckBeingNeighbor", 392);
			}
			NavigationPath navigationPath = new NavigationPath();
			float num = (((float)(_regionSwitchCostTo0 + _regionSwitchCostTo1) > 0f) ? 2f : 0f);
			if (num > 0f)
			{
				Scene.GetPathBetweenAIFaces(record.FaceIndex, record2.FaceIndex, vec, vec2, 0.3f, navigationPath, _excludedFaceIds, num, _regionSwitchCostTo0, _regionSwitchCostTo1);
			}
			else
			{
				Scene.GetPathBetweenAIFaces(record.FaceIndex, record2.FaceIndex, vec, vec2, 0.3f, navigationPath, _excludedFaceIds, 0f);
			}
			bool flag = navigationPath.Size > 0 || record.FaceIndex == record2.FaceIndex;
			bool flag2 = useGate1;
			if (!Scene.GetPathDistanceBetweenAIFaces(record.FaceIndex, record2.FaceIndex, vec, vec2, 0.3f, 1784684f, out distance, GetExcludedFaceIds(), _regionSwitchCostTo0, _regionSwitchCostTo1))
			{
				distance = 1784684f;
			}
			for (int i = 0; i < navigationPath.Size && flag; i++)
			{
				Vec2 vec3 = navigationPath[i] - ((i == 0) ? vec : navigationPath[i - 1]);
				float num2 = vec3.Length / 1f;
				vec3.Normalize();
				for (int j = 0; (float)j < num2; j++)
				{
					Vec2 vec4 = ((i == 0) ? vec : navigationPath[i - 1]) + vec3 * 1f * j;
					if (!(vec4 != vec) || !(vec4 != vec2))
					{
						continue;
					}
					PathFaceRecord record3 = PathFaceRecord.NullFaceRecord;
					Scene.GetNavMeshFaceIndex(ref record3, vec4, flag2, checkIfDisabled: false, ignoreHeight: true);
					if (record3.FaceIndex == -1)
					{
						flag2 = !flag2;
						Scene.GetNavMeshFaceIndex(ref record3, vec4, flag2, checkIfDisabled: false, ignoreHeight: true);
					}
					bool isPort;
					float realPathDistanceFromPositionToSettlement = GetRealPathDistanceFromPositionToSettlement(vec4, record3, distance, settlement1, out isPort);
					float realPathDistanceFromPositionToSettlement2 = GetRealPathDistanceFromPositionToSettlement(vec4, record3, distance, settlement2, out isPort);
					float num3 = ((realPathDistanceFromPositionToSettlement < realPathDistanceFromPositionToSettlement2) ? realPathDistanceFromPositionToSettlement : realPathDistanceFromPositionToSettlement2);
					if (record3.FaceIndex != -1)
					{
						SettlementRecord closestSettlementToPosition = GetClosestSettlementToPosition(vec4, record3, _excludedFaceIds, settlementsToConsider, _regionSwitchCostTo0, _regionSwitchCostTo1, num3 * 0.8f, out isPort);
						if (closestSettlementToPosition != null && closestSettlementToPosition != settlement1 && closestSettlementToPosition != settlement2)
						{
							flag = false;
							break;
						}
					}
				}
			}
			return flag;
		}

		protected override List<SettlementRecord> GetAllRegisteredSettlements()
		{
			return _settlementRecords;
		}
	}

	private const string SandBoxModuleId = "Sandbox";

	private const string NavalDLCModuleId = "NavalDLC";

	private const string NavalPartyNavigationModelName = "NavalPartyNavigationModel";

	private const string NavalMapDistanceModelName = "NavalDLCMapDistanceModel";

	private bool _mapIsSandBox;

	private bool _mapIsNavalDLC;

	[EditableScriptComponentVariable(true, "")]
	private string _partyNavigationModelOverriddenClassName;

	[EditableScriptComponentVariable(true, "")]
	private string _distanceModelOverridenClassName;

	private PartyNavigationModel _partyNavigationModel;

	private MapDistanceModel _mapDistanceModel;

	public SimpleButton CheckPositions;

	public SimpleButton SavePositions;

	public SimpleButton ComputeAndSaveSettlementDistanceCache;

	private string SettlementsXmlPath
	{
		get
		{
			string text = base.Scene.GetModulePath();
			if (text.Contains("$BASE"))
			{
				text = text.Remove(0, 6);
				text = BasePath.Name + text;
			}
			return text + "ModuleData/settlements.xml";
		}
	}

	protected override void OnInit()
	{
		try
		{
			InitializeCachedVariables();
			bool useNavalNavigation = false;
			if (GetMapIsNavalDLC() || (!GetMapIsSandBox() && ModuleHelper.IsModuleActive("NavalDLC")))
			{
				useNavalNavigation = true;
			}
			RegisterNavigationCachesOnGameLoad(useNavalNavigation);
		}
		catch (Exception ex)
		{
			Debug.Print("Error when reading distance cache " + ex.Message);
			Debug.Print("SettlementsDistanceCacheFilePath could not be read!. Campaign starting performance will be affected very badly, cache will be initialized now.");
			Debug.FailedAssert("SettlementsDistanceCacheFilePath could not be read!. Campaign starting performance will be affected very badly, cache will be initialized now.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\SettlementPositionScript.cs", "OnInit", 536);
		}
	}

	private void RegisterNavigationCachesOnGameLoad(bool useNavalNavigation)
	{
		SandBoxNavigationCache cacheToRegister = ReadNavigationCacheForNavigationTypeOnGameLoad(MobileParty.NavigationType.Default);
		_mapDistanceModel.RegisterDistanceCache(MobileParty.NavigationType.Default, cacheToRegister);
		if (useNavalNavigation)
		{
			SandBoxNavigationCache cacheToRegister2 = ReadNavigationCacheForNavigationTypeOnGameLoad(MobileParty.NavigationType.Naval);
			SandBoxNavigationCache cacheToRegister3 = ReadNavigationCacheForNavigationTypeOnGameLoad(MobileParty.NavigationType.All);
			_mapDistanceModel.RegisterDistanceCache(MobileParty.NavigationType.Naval, cacheToRegister2);
			_mapDistanceModel.RegisterDistanceCache(MobileParty.NavigationType.All, cacheToRegister3);
		}
	}

	private SandBoxNavigationCache ReadNavigationCacheForNavigationTypeOnGameLoad(MobileParty.NavigationType navigationCapability)
	{
		string text = string.Empty;
		foreach (ModuleInfo activeModule in ModuleHelper.GetActiveModules())
		{
			if (activeModule.IsActive && GetSettlementsDistanceCacheFileForCapability(activeModule.Id, navigationCapability, out var filePath))
			{
				text = filePath;
			}
		}
		SandBoxNavigationCache sandBoxNavigationCache;
		if (!string.IsNullOrEmpty(text))
		{
			sandBoxNavigationCache = ReadNavigationCacheOnGameLoad(text, navigationCapability);
		}
		else
		{
			Debug.FailedAssert($"Navigation type with id {navigationCapability} file is not found, this should not be happening, will generate cache (this will take some time)", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\SettlementPositionScript.cs", "ReadNavigationCacheForNavigationTypeOnGameLoad", 576);
			sandBoxNavigationCache = new SandBoxNavigationCache(navigationCapability);
			sandBoxNavigationCache.GenerateCacheData();
		}
		return sandBoxNavigationCache;
	}

	private SandBoxNavigationCache ReadNavigationCacheOnGameLoad(string path, MobileParty.NavigationType navigationCapability)
	{
		SandBoxNavigationCache sandBoxNavigationCache = new SandBoxNavigationCache(navigationCapability);
		sandBoxNavigationCache.Deserialize(path);
		return sandBoxNavigationCache;
	}

	protected override void OnEditorInit()
	{
		base.OnEditorInit();
		_partyNavigationModelOverriddenClassName = "";
		_distanceModelOverridenClassName = "";
		InitializeCachedVariables();
	}

	protected override void OnEditorVariableChanged(string variableName)
	{
		base.OnEditorVariableChanged(variableName);
		if (variableName == "SavePositions")
		{
			SaveSettlementPositions();
		}
		if (variableName == "ComputeAndSaveSettlementDistanceCache")
		{
			SaveSettlementDistanceCacheEditor();
		}
		if (variableName == "CheckPositions")
		{
			CheckSettlementPositions();
		}
		if (variableName == "_partyNavigationModelOverriddenClassName" || variableName == "_distanceModelOverridenClassName")
		{
			InitializeCachedVariables();
		}
	}

	protected override void OnSceneSave(string saveFolder)
	{
		base.OnSceneSave(saveFolder);
		SaveSettlementPositions();
	}

	private void CheckSettlementPositions()
	{
		XmlDocument xmlDocument = LoadXmlFile(SettlementsXmlPath);
		base.GameEntity.RemoveAllChildren();
		PartyNavigationModel partyNavigationModel = GetPartyNavigationModel();
		bool[] regionMapping = SandBoxHelpers.MapSceneHelper.GetRegionMapping(partyNavigationModel);
		base.GameEntity.Scene.SetNavMeshRegionMap(regionMapping);
		List<int> list = partyNavigationModel.GetInvalidTerrainTypesForNavigationType(MobileParty.NavigationType.Default).ToList();
		list.Add(0);
		List<int> list2 = null;
		foreach (XmlNode item2 in xmlDocument.DocumentElement.SelectNodes("Settlement"))
		{
			string value = item2.Attributes["id"].Value;
			GameEntity campaignEntityWithName = base.Scene.GetCampaignEntityWithName(value);
			if (!(campaignEntityWithName != null))
			{
				continue;
			}
			Vec3 origin = campaignEntityWithName.GetGlobalFrame().origin;
			Vec3 vec = default(Vec3);
			Vec3 pos = default(Vec3);
			List<GameEntity> children = new List<GameEntity>();
			campaignEntityWithName.GetChildrenRecursive(ref children);
			bool flag = false;
			bool flag2 = false;
			foreach (GameEntity item3 in children)
			{
				if (item3.HasTag("main_map_city_gate"))
				{
					vec = item3.GetGlobalFrame().origin;
					flag = true;
				}
				if (item3.HasTag("main_map_city_port"))
				{
					pos = item3.GetGlobalFrame().origin;
					flag2 = true;
				}
			}
			Vec3 pos2 = origin;
			if (flag)
			{
				pos2 = vec;
			}
			PathFaceRecord record = PathFaceRecord.NullFaceRecord;
			base.GameEntity.Scene.GetNavMeshFaceIndex(ref record, pos2.AsVec2, isRegion1: true, checkIfDisabled: true);
			int item = 0;
			if (record.IsValid())
			{
				item = record.FaceGroupIndex;
			}
			if (list.Contains(item))
			{
				Debug.Print($"There is gate position problem with settlement {campaignEntityWithName.Name} at position:  {pos2.AsVec2}");
				MBEditor.ZoomToPosition(pos2);
				break;
			}
			if (flag2)
			{
				if (list2 == null)
				{
					list2 = partyNavigationModel.GetInvalidTerrainTypesForNavigationType(MobileParty.NavigationType.Naval).ToList();
					list2.Add(0);
				}
				record = PathFaceRecord.NullFaceRecord;
				base.GameEntity.Scene.GetNavMeshFaceIndex(ref record, pos.AsVec2, isRegion1: false, checkIfDisabled: true);
				item = 0;
				if (record.IsValid())
				{
					item = record.FaceGroupIndex;
				}
				if (list2.Contains(item))
				{
					Debug.Print($"There is port position problem with settlement {campaignEntityWithName.Name} at position:  {pos.AsVec2}");
					MBEditor.ZoomToPosition(pos);
					break;
				}
			}
		}
	}

	private void InitializeCachedVariables()
	{
		_mapIsNavalDLC = string.Equals("NavalDLC", GetMapModuleId(), StringComparison.CurrentCultureIgnoreCase);
		_mapIsSandBox = string.Equals("Sandbox", GetMapModuleId(), StringComparison.CurrentCultureIgnoreCase);
		_partyNavigationModel = GetPartyNavigationModel();
		_mapDistanceModel = GetMapDistanceModel();
	}

	protected override bool IsOnlyVisual()
	{
		return true;
	}

	private bool GetMapIsNavalDLC()
	{
		return _mapIsNavalDLC;
	}

	private bool GetMapIsSandBox()
	{
		return _mapIsSandBox;
	}

	private string GetMapModuleId()
	{
		return base.Scene.GetModulePath().Trim().TrimEnd(new char[1] { '/' })
			.Split(new char[1] { '/' })
			.Last();
	}

	private PartyNavigationModel GetPartyNavigationModel()
	{
		if (Campaign.Current != null)
		{
			return Campaign.Current.Models.PartyNavigationModel;
		}
		if (string.IsNullOrEmpty(_partyNavigationModelOverriddenClassName))
		{
			if (GetMapIsSandBox())
			{
				_partyNavigationModelOverriddenClassName = "DefaultPartyNavigationModel";
				return CreateBaseNavigationModel(naval: false);
			}
			if (GetMapIsNavalDLC())
			{
				if (!ModuleHelper.IsModuleActive("NavalDLC"))
				{
					throw new ApplicationException("NavalDlc map changes can not be made without NavalDlc module!");
				}
				_partyNavigationModelOverriddenClassName = "NavalPartyNavigationModel";
				return CreateBaseNavigationModel(naval: true);
			}
			if (ModuleHelper.IsModuleActive("NavalDLC"))
			{
				_partyNavigationModelOverriddenClassName = "NavalPartyNavigationModel";
				return CreateBaseNavigationModel(naval: true);
			}
			_partyNavigationModelOverriddenClassName = "DefaultPartyNavigationModel";
			return CreateBaseNavigationModel(naval: false);
		}
		if (FindClass(_partyNavigationModelOverriddenClassName) == null)
		{
			Debug.FailedAssert("Cant find custom navigation model", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\SettlementPositionScript.cs", "GetPartyNavigationModel", 826);
			return CreateBaseNavigationModel(GetMapIsNavalDLC());
		}
		return CreateCustomNavigationModel(_partyNavigationModelOverriddenClassName, !GetMapIsSandBox() && ModuleHelper.IsModuleActive("NavalDLC"));
	}

	private MapDistanceModel GetMapDistanceModel()
	{
		if (Campaign.Current != null)
		{
			return Campaign.Current.Models.MapDistanceModel;
		}
		if (string.IsNullOrEmpty(_distanceModelOverridenClassName))
		{
			if (GetMapIsSandBox())
			{
				_distanceModelOverridenClassName = "DefaultMapDistanceModel";
				return CreateBaseDistanceModel(naval: false);
			}
			if (GetMapIsNavalDLC())
			{
				if (!ModuleHelper.IsModuleActive("NavalDLC"))
				{
					throw new ApplicationException("NavalDlc map changes can not be made without NavalDlc module!");
				}
				_distanceModelOverridenClassName = "NavalDLCMapDistanceModel";
				return CreateBaseDistanceModel(naval: true);
			}
			if (ModuleHelper.IsModuleActive("NavalDLC"))
			{
				_distanceModelOverridenClassName = "NavalDLCMapDistanceModel";
				return CreateBaseDistanceModel(naval: true);
			}
			_distanceModelOverridenClassName = "DefaultMapDistanceModel";
			return CreateBaseDistanceModel(naval: false);
		}
		if (FindClass(_distanceModelOverridenClassName) == null)
		{
			Debug.FailedAssert("Cant find custom navigation model", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\SettlementPositionScript.cs", "GetMapDistanceModel", 882);
			return CreateBaseDistanceModel(GetMapIsNavalDLC());
		}
		return CreateCustomMapDistanceModel(_distanceModelOverridenClassName, !GetMapIsSandBox() && ModuleHelper.IsModuleActive("NavalDLC"));
	}

	private static PartyNavigationModel CreateCustomNavigationModel(string name, bool naval)
	{
		if (name == "DefaultPartyNavigationModel")
		{
			return CreateBaseNavigationModel(naval: false);
		}
		Type type = FindClass(name);
		if (type == null)
		{
			Debug.FailedAssert("Cant find custom navigation model", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\SettlementPositionScript.cs", "CreateCustomNavigationModel", 903);
			return CreateBaseNavigationModel(naval);
		}
		if (type.GetConstructor(new Type[1] { typeof(PartyNavigationModel) }) != null)
		{
			return (PartyNavigationModel)Activator.CreateInstance(type, CreateBaseNavigationModel(naval));
		}
		return (PartyNavigationModel)Activator.CreateInstance(type);
	}

	private static MapDistanceModel CreateCustomMapDistanceModel(string name, bool naval)
	{
		if (name == "DefaultMapDistanceModel")
		{
			return CreateBaseDistanceModel(naval: false);
		}
		Type type = FindClass(name);
		if (type == null)
		{
			Debug.FailedAssert("Cant find custom navigation model", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\SettlementPositionScript.cs", "CreateCustomMapDistanceModel", 930);
			return CreateBaseDistanceModel(naval);
		}
		return (MapDistanceModel)Activator.CreateInstance(type);
	}

	private static Type FindClass(string name)
	{
		Type result = null;
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		for (int i = 0; i < assemblies.Length; i++)
		{
			foreach (Type item in assemblies[i].GetTypesSafe())
			{
				if (item.Name == name)
				{
					result = item;
					break;
				}
			}
		}
		return result;
	}

	private static PartyNavigationModel CreateBaseNavigationModel(bool naval)
	{
		if (naval)
		{
			Type type = FindClass("NavalPartyNavigationModel");
			if (type == null)
			{
				throw new ArgumentException("Cant find naval navigation model");
			}
			return (PartyNavigationModel)Activator.CreateInstance(type, CreateBaseNavigationModel(naval: false));
		}
		return new DefaultPartyNavigationModel();
	}

	private static MapDistanceModel CreateBaseDistanceModel(bool naval)
	{
		if (naval)
		{
			Type type = FindClass("NavalDLCMapDistanceModel");
			if (type == null)
			{
				throw new ArgumentException("Cant find naval navigation model");
			}
			return (MapDistanceModel)Activator.CreateInstance(type);
		}
		return new DefaultMapDistanceModel();
	}

	private static MapDistanceModel CreateBaseDistanceModel()
	{
		return new DefaultMapDistanceModel();
	}

	private bool GetSettlementsDistanceCacheFileForCapability(string moduleId, MobileParty.NavigationType navigationType, out string filePath)
	{
		string text = ModuleHelper.GetModuleFullPath(moduleId) + "ModuleData/DistanceCaches";
		string text2 = navigationType.ToString();
		filePath = text + "/settlements_distance_cache_" + text2 + ".bin";
		bool num = File.Exists(filePath);
		if (num)
		{
			Debug.Print($"Found distance cache at: {moduleId}, {text}, {navigationType}");
		}
		return num;
	}

	private List<SettlementRecord> LoadSettlementData(XmlDocument settlementDocument)
	{
		List<SettlementRecord> list = new List<SettlementRecord>();
		base.GameEntity.RemoveAllChildren();
		foreach (XmlNode item in settlementDocument.DocumentElement.SelectNodes("Settlement"))
		{
			_ = item.Attributes["name"].Value;
			string value = item.Attributes["id"].Value;
			GameEntity campaignEntityWithName = base.Scene.GetCampaignEntityWithName(value);
			if (campaignEntityWithName == null)
			{
				continue;
			}
			Vec2 asVec = campaignEntityWithName.GetGlobalFrame().origin.AsVec2;
			Vec2 vec = default(Vec2);
			List<GameEntity> children = new List<GameEntity>();
			campaignEntityWithName.GetChildrenRecursive(ref children);
			bool flag = false;
			bool hasPort = false;
			Vec2 portPosition = default(Vec2);
			foreach (GameEntity item2 in children)
			{
				if (item2.HasTag("main_map_city_gate"))
				{
					vec = item2.GetGlobalFrame().origin.AsVec2;
					flag = true;
				}
				if (item2.HasTag("main_map_city_port"))
				{
					portPosition = item2.GetGlobalFrame().origin.AsVec2;
					hasPort = true;
				}
				if (item2.HasTag("main_map_village_dropoff"))
				{
					portPosition = item2.GetGlobalFrame().origin.AsVec2;
					hasPort = true;
				}
			}
			bool isFortification = false;
			foreach (XmlNode childNode in item.ChildNodes)
			{
				if (!childNode.Name.Equals("Components"))
				{
					continue;
				}
				foreach (XmlNode childNode2 in childNode.ChildNodes)
				{
					if (childNode2.Name.Equals("Town"))
					{
						if (childNode2.Attributes["is_castle"] != null)
						{
							bool.Parse(childNode2.Attributes["is_castle"].Value);
						}
						else
							_ = 0;
						isFortification = true;
						break;
					}
				}
				break;
			}
			list.Add(new SettlementRecord(value, asVec, flag ? vec : asVec, item, flag, portPosition, hasPort, isFortification));
		}
		return list;
	}

	private XmlDocument LoadXmlFile(string path)
	{
		Debug.Print("opening " + path);
		XmlDocument xmlDocument = new XmlDocument();
		StreamReader streamReader = new StreamReader(path);
		string xml = streamReader.ReadToEnd();
		xmlDocument.LoadXml(xml);
		streamReader.Close();
		return xmlDocument;
	}

	private void SaveSettlementPositions()
	{
		XmlDocument xmlDocument = LoadXmlFile(SettlementsXmlPath);
		foreach (SettlementRecord item in LoadSettlementData(xmlDocument))
		{
			_ = item.Node.Attributes["name"].Value;
			if (item.Node.Attributes["posX"] == null)
			{
				XmlAttribute node = xmlDocument.CreateAttribute("posX");
				item.Node.Attributes.Append(node);
			}
			item.Node.Attributes["posX"].Value = item.Position.X.ToString();
			if (item.Node.Attributes["posY"] == null)
			{
				XmlAttribute node2 = xmlDocument.CreateAttribute("posY");
				item.Node.Attributes.Append(node2);
			}
			item.Node.Attributes["posY"].Value = item.Position.Y.ToString();
			if (item.HasGate)
			{
				if (item.Node.Attributes["gate_posX"] == null)
				{
					XmlAttribute node3 = xmlDocument.CreateAttribute("gate_posX");
					item.Node.Attributes.Append(node3);
				}
				item.Node.Attributes["gate_posX"].Value = item.GatePosition.X.ToString();
				if (item.Node.Attributes["gate_posY"] == null)
				{
					XmlAttribute node4 = xmlDocument.CreateAttribute("gate_posY");
					item.Node.Attributes.Append(node4);
				}
				item.Node.Attributes["gate_posY"].Value = item.GatePosition.Y.ToString();
			}
			if (item.HasPort)
			{
				if (item.Node.Attributes["port_posX"] == null)
				{
					XmlAttribute node5 = xmlDocument.CreateAttribute("port_posX");
					item.Node.Attributes.Append(node5);
				}
				item.Node.Attributes["port_posX"].Value = item.PortPosition.X.ToString();
				if (item.Node.Attributes["port_posY"] == null)
				{
					XmlAttribute node6 = xmlDocument.CreateAttribute("port_posY");
					item.Node.Attributes.Append(node6);
				}
				item.Node.Attributes["port_posY"].Value = item.PortPosition.Y.ToString();
			}
		}
		xmlDocument.Save(SettlementsXmlPath);
	}

	private void SaveSettlementDistanceCacheEditor()
	{
		bool[] regionMapping = SandBoxHelpers.MapSceneHelper.GetRegionMapping(_partyNavigationModel);
		base.Scene.SetNavMeshRegionMap(regionMapping);
		List<MobileParty.NavigationType> list = new List<MobileParty.NavigationType> { MobileParty.NavigationType.Default };
		if (GetMapIsNavalDLC() || (!GetMapIsSandBox() && ModuleHelper.IsModuleActive("NavalDLC")))
		{
			list.Add(MobileParty.NavigationType.Naval);
			list.Add(MobileParty.NavigationType.All);
		}
		foreach (MobileParty.NavigationType item in list)
		{
			int[] invalidTerrainTypesForNavigationType = _partyNavigationModel.GetInvalidTerrainTypesForNavigationType(item);
			try
			{
				XmlDocument settlementDocument = LoadXmlFile(SettlementsXmlPath);
				List<SettlementRecord> settlementRecords = LoadSettlementData(settlementDocument);
				int[] array = invalidTerrainTypesForNavigationType;
				foreach (int faceGroupId in array)
				{
					base.Scene.SetAbilityOfFacesWithId(faceGroupId, isEnabled: false);
				}
				SettlementPositionScriptNavigationCache settlementPositionScriptNavigationCache = new SettlementPositionScriptNavigationCache(settlementRecords, base.Scene, _mapDistanceModel, _partyNavigationModel, item);
				settlementPositionScriptNavigationCache.GenerateCacheData();
				GetSettlementsDistanceCacheFileForCapability(GetMapModuleId(), item, out var filePath);
				settlementPositionScriptNavigationCache.Serialize(filePath);
			}
			catch
			{
			}
			finally
			{
				int[] array = invalidTerrainTypesForNavigationType;
				foreach (int faceGroupId2 in array)
				{
					base.Scene.SetAbilityOfFacesWithId(faceGroupId2, isEnabled: true);
				}
			}
		}
	}
}
