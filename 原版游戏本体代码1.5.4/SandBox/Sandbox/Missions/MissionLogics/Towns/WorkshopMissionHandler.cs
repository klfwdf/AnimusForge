using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Objects.AreaMarkers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics.Towns;

public class WorkshopMissionHandler : MissionLogic
{
	private Settlement _settlement;

	private string[] _propKinds = new string[6] { "a", "b", "c", "d", "e", "f" };

	private Dictionary<int, Dictionary<string, List<MatrixFrame>>> _propFrames;

	private List<GameEntity> _listOfCurrentProps;

	private List<WorkshopAreaMarker> _areaMarkers;

	private List<Tuple<Workshop, GameEntity>> _workshopSignEntities;

	public IEnumerable<Tuple<Workshop, GameEntity>> WorkshopSignEntities => _workshopSignEntities.AsEnumerable();

	public WorkshopMissionHandler(Settlement settlement)
	{
		_settlement = settlement;
	}

	public override void OnBehaviorInitialize()
	{
		_workshopSignEntities = new List<Tuple<Workshop, GameEntity>>();
		_listOfCurrentProps = new List<GameEntity>();
		_propFrames = new Dictionary<int, Dictionary<string, List<MatrixFrame>>>();
		_areaMarkers = new List<WorkshopAreaMarker>();
	}

	public override void EarlyStart()
	{
		for (int i = 0; i < _settlement.Town.Workshops.Length; i++)
		{
			if (!_settlement.Town.Workshops[i].WorkshopType.IsHidden)
			{
				_propFrames.Add(i, new Dictionary<string, List<MatrixFrame>>());
				string[] propKinds = _propKinds;
				foreach (string key in propKinds)
				{
					_propFrames[i].Add(key, new List<MatrixFrame>());
				}
			}
		}
		List<WorkshopAreaMarker> list = base.Mission.ActiveMissionObjects.FindAllWithType<WorkshopAreaMarker>().ToList();
		_areaMarkers = list.FindAll((WorkshopAreaMarker x) => x.GameEntity.HasTag("workshop_area_marker"));
		foreach (WorkshopAreaMarker areaMarker in _areaMarkers)
		{
			_ = areaMarker;
		}
		foreach (GameEntity item in base.Mission.Scene.FindEntitiesWithTag("shop_prop").ToList())
		{
			WorkshopAreaMarker workshopAreaMarker = FindWorkshop(item);
			if (workshopAreaMarker == null || !_propFrames.ContainsKey(workshopAreaMarker.AreaIndex) || (_settlement.Town.Workshops[workshopAreaMarker.AreaIndex] != null && _settlement.Town.Workshops[workshopAreaMarker.AreaIndex].WorkshopType.IsHidden))
			{
				continue;
			}
			string[] propKinds = _propKinds;
			foreach (string text in propKinds)
			{
				if (item.HasTag(text))
				{
					_propFrames[workshopAreaMarker.AreaIndex][text].Add(item.GetGlobalFrame());
					_listOfCurrentProps.Add(item);
					break;
				}
			}
		}
		SetBenches();
	}

	public override void AfterStart()
	{
		InitShopSigns();
	}

	private WorkshopAreaMarker FindWorkshop(GameEntity prop)
	{
		foreach (WorkshopAreaMarker areaMarker in _areaMarkers)
		{
			if (areaMarker.IsPositionInRange(prop.GlobalPosition))
			{
				return areaMarker;
			}
		}
		return null;
	}

	private void SetBenches()
	{
		foreach (GameEntity listOfCurrentProp in _listOfCurrentProps)
		{
			listOfCurrentProp.SetVisibilityExcludeParents(visible: false);
			listOfCurrentProp.GetFirstScriptOfType<MissionObject>()?.SetDisabled(isParentObject: true);
			foreach (GameEntity child in listOfCurrentProp.GetChildren())
			{
				child.GetFirstScriptOfType<UsableMachine>()?.SetDisabled(isParentObject: true);
			}
		}
		_listOfCurrentProps.Clear();
		foreach (KeyValuePair<int, Dictionary<string, List<MatrixFrame>>> propFrame in _propFrames)
		{
			int key = propFrame.Key;
			foreach (KeyValuePair<string, List<MatrixFrame>> item in propFrame.Value)
			{
				List<string> prefabNames = GetPrefabNames(key, item.Key);
				if (prefabNames.Count != 0)
				{
					for (int i = 0; i < item.Value.Count; i++)
					{
						MatrixFrame frame = item.Value[i];
						_listOfCurrentProps.Add(GameEntity.Instantiate(base.Mission.Scene, prefabNames[i % prefabNames.Count], frame));
					}
				}
			}
		}
	}

	private void InitShopSigns()
	{
		if (Campaign.Current.GameMode != CampaignGameMode.Campaign || _settlement == null || !_settlement.IsTown)
		{
			return;
		}
		List<GameEntity> list = base.Mission.Scene.FindEntitiesWithTag("shop_sign").ToList();
		foreach (WorkshopAreaMarker item2 in base.Mission.ActiveMissionObjects.FindAllWithType<WorkshopAreaMarker>().ToList())
		{
			Workshop workshop = _settlement.Town.Workshops[item2.AreaIndex];
			if (!_workshopSignEntities.All((Tuple<Workshop, GameEntity> x) => x.Item1 != workshop))
			{
				continue;
			}
			for (int num = 0; num < list.Count; num++)
			{
				GameEntity gameEntity = list[num];
				if (item2.IsPositionInRange(gameEntity.GlobalPosition))
				{
					_workshopSignEntities.Add(new Tuple<Workshop, GameEntity>(workshop, gameEntity));
					list.RemoveAt(num);
					break;
				}
			}
		}
		foreach (Tuple<Workshop, GameEntity> workshopSignEntity in _workshopSignEntities)
		{
			GameEntity item = workshopSignEntity.Item2;
			WorkshopType workshopType = workshopSignEntity.Item1.WorkshopType;
			item.ClearComponents();
			MetaMesh copy = MetaMesh.GetCopy((workshopType != null) ? workshopType.SignMeshName : "shop_sign_merchantavailable");
			item.AddMultiMesh(copy);
		}
	}

	private List<string> GetPrefabNames(int areaIndex, string propKind)
	{
		List<string> list = new List<string>();
		Workshop workshop = _settlement.Town.Workshops[areaIndex];
		if (workshop.WorkshopType != null)
		{
			if (propKind == _propKinds[0])
			{
				list.Add(workshop.WorkshopType.PropMeshName1);
			}
			else if (propKind == _propKinds[1])
			{
				list.Add(workshop.WorkshopType.PropMeshName2);
			}
			else if (propKind == _propKinds[2])
			{
				list.AddRange(workshop.WorkshopType.PropMeshName3List);
			}
			else if (propKind == _propKinds[3])
			{
				list.Add(workshop.WorkshopType.PropMeshName4);
			}
			else if (propKind == _propKinds[4])
			{
				list.Add(workshop.WorkshopType.PropMeshName5);
			}
			else if (propKind == _propKinds[5])
			{
				list.Add(workshop.WorkshopType.PropMeshName6);
			}
		}
		return list;
	}
}
