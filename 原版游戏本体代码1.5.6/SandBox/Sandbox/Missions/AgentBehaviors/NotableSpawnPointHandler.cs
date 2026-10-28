using System.Collections.Generic;
using System.Linq;
using SandBox.Objects.AreaMarkers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors;

public class NotableSpawnPointHandler : MissionLogic
{
	private int _merchantNotableCount;

	private int _gangLeaderNotableCount;

	private int _preacherNotableCount;

	private int _artisanNotableCount;

	private int _ruralNotableCount;

	private GameEntity _currentMerchantSetGameEntity;

	private GameEntity _currentPreacherSetGameEntity;

	private GameEntity _currentGangLeaderSetGameEntity;

	private GameEntity _currentArtisanSetGameEntity;

	private GameEntity _currentRuralNotableSetGameEntity;

	private List<Hero> _workshopAssignedHeroes;

	public override void OnBehaviorInitialize()
	{
		List<GameEntity> list = Mission.Current.Scene.FindEntitiesWithTag("sp_notables_parent").ToList();
		Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
		_workshopAssignedHeroes = new List<Hero>();
		foreach (Hero notable in settlement.Notables)
		{
			if (notable.IsGangLeader)
			{
				_gangLeaderNotableCount++;
			}
			else if (notable.IsPreacher)
			{
				_preacherNotableCount++;
			}
			else if (notable.IsArtisan)
			{
				_artisanNotableCount++;
			}
			else if (notable.IsRuralNotable || notable.IsHeadman)
			{
				_ruralNotableCount++;
			}
			else if (notable.IsMerchant)
			{
				_merchantNotableCount++;
			}
		}
		foreach (GameEntity item in list.ToList())
		{
			foreach (GameEntity child in item.GetChildren())
			{
				FindAndSetChild(child);
			}
			foreach (WorkshopAreaMarker item2 in (from x in base.Mission.ActiveMissionObjects.FindAllWithType<WorkshopAreaMarker>().ToList()
				orderby x.AreaIndex
				select x).ToList())
			{
				if (item2.IsPositionInRange(item.GlobalPosition) && item2.GetWorkshop().Owner.OwnedWorkshops.First((Workshop x) => !x.WorkshopType.IsHidden).Tag == item2.Tag)
				{
					ActivateParentSetInsideWorkshop(item2);
					list.Remove(item);
					break;
				}
			}
		}
		foreach (GameEntity item3 in list)
		{
			foreach (GameEntity child2 in item3.GetChildren())
			{
				FindAndSetChild(child2);
			}
			ActivateParentSetOutsideWorkshop();
		}
	}

	private void FindAndSetChild(GameEntity childGameEntity)
	{
		if (childGameEntity.HasTag("merchant_notary_talking_set"))
		{
			_currentMerchantSetGameEntity = childGameEntity;
		}
		else if (childGameEntity.HasTag("preacher_notary_talking_set"))
		{
			_currentPreacherSetGameEntity = childGameEntity;
		}
		else if (childGameEntity.HasTag("gangleader_sitting_and_talking_with_guards_set"))
		{
			_currentGangLeaderSetGameEntity = childGameEntity;
		}
		else if (childGameEntity.HasTag("sp_artisan_notary_talking_set"))
		{
			_currentArtisanSetGameEntity = childGameEntity;
		}
		else if (childGameEntity.HasTag("sp_ruralnotable_notary_talking_set"))
		{
			_currentRuralNotableSetGameEntity = childGameEntity;
		}
	}

	private void ActivateParentSetInsideWorkshop(WorkshopAreaMarker areaMarker)
	{
		Hero owner = areaMarker.GetWorkshop().Owner;
		if (!_workshopAssignedHeroes.Contains(owner))
		{
			_workshopAssignedHeroes.Add(owner);
			if (owner.IsMerchant)
			{
				DeactivateAllExcept(_currentMerchantSetGameEntity);
				_merchantNotableCount--;
			}
			else if (owner.IsArtisan)
			{
				DeactivateAllExcept(_currentArtisanSetGameEntity);
				_artisanNotableCount--;
			}
			else if (owner.IsGangLeader)
			{
				DeactivateAllExcept(_currentGangLeaderSetGameEntity);
				_gangLeaderNotableCount--;
			}
			else if (owner.IsPreacher)
			{
				DeactivateAllExcept(_currentPreacherSetGameEntity);
				_preacherNotableCount--;
			}
			else if (owner.IsRuralNotable)
			{
				DeactivateAllExcept(_currentRuralNotableSetGameEntity);
				_ruralNotableCount--;
			}
		}
		else
		{
			DeactivateAll();
		}
	}

	private void ActivateParentSetOutsideWorkshop()
	{
		if (_gangLeaderNotableCount > 0)
		{
			DeactivateAllExcept(_currentGangLeaderSetGameEntity);
			_gangLeaderNotableCount--;
		}
		else if (_merchantNotableCount > 0)
		{
			DeactivateAllExcept(_currentMerchantSetGameEntity);
			_merchantNotableCount--;
		}
		else if (_preacherNotableCount > 0)
		{
			DeactivateAllExcept(_currentPreacherSetGameEntity);
			_preacherNotableCount--;
		}
		else if (_artisanNotableCount > 0)
		{
			DeactivateAllExcept(_currentArtisanSetGameEntity);
			_artisanNotableCount--;
		}
		else if (_ruralNotableCount > 0)
		{
			DeactivateAllExcept(_currentRuralNotableSetGameEntity);
			_ruralNotableCount--;
		}
		else
		{
			DeactivateAll();
		}
	}

	private void DeactivateAll()
	{
		MakeInvisibleAndDeactivate(_currentGangLeaderSetGameEntity);
		MakeInvisibleAndDeactivate(_currentMerchantSetGameEntity);
		MakeInvisibleAndDeactivate(_currentPreacherSetGameEntity);
		MakeInvisibleAndDeactivate(_currentArtisanSetGameEntity);
		MakeInvisibleAndDeactivate(_currentRuralNotableSetGameEntity);
	}

	private void DeactivateAllExcept(GameEntity gameEntity)
	{
		if (gameEntity != _currentMerchantSetGameEntity)
		{
			MakeInvisibleAndDeactivate(_currentMerchantSetGameEntity);
		}
		if (gameEntity != _currentGangLeaderSetGameEntity)
		{
			MakeInvisibleAndDeactivate(_currentGangLeaderSetGameEntity);
		}
		if (gameEntity != _currentPreacherSetGameEntity)
		{
			MakeInvisibleAndDeactivate(_currentPreacherSetGameEntity);
		}
		if (gameEntity != _currentArtisanSetGameEntity)
		{
			MakeInvisibleAndDeactivate(_currentArtisanSetGameEntity);
		}
		if (gameEntity != _currentRuralNotableSetGameEntity)
		{
			MakeInvisibleAndDeactivate(_currentRuralNotableSetGameEntity);
		}
	}

	private void MakeInvisibleAndDeactivate(GameEntity gameEntity)
	{
		gameEntity.SetVisibilityExcludeParents(visible: false);
		gameEntity.GetFirstScriptOfType<UsableMachine>()?.Deactivate();
		foreach (GameEntity child in gameEntity.GetChildren())
		{
			MakeInvisibleAndDeactivate(child);
		}
	}
}
