using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Source.Objects;

public class SceneLeveler : ScriptComponentBehavior
{
	public string SourceSelectionSetName = "";

	public string TargetSelectionSetName = "";

	public SimpleButton CreateLevel1;

	public SimpleButton CreateLevel2;

	public SimpleButton CreateLevel3;

	public SimpleButton DeleteLevel1;

	public SimpleButton DeleteLevel2;

	public SimpleButton DeleteLevel3;

	public SimpleButton SelectEntitiesWithoutLevel;

	protected internal override void OnEditorVariableChanged(string variableName)
	{
		switch (variableName)
		{
		case "CreateLevel1":
			OnLevelizeButtonPressed(1);
			break;
		case "CreateLevel2":
			OnLevelizeButtonPressed(2);
			break;
		case "CreateLevel3":
			OnLevelizeButtonPressed(3);
			break;
		case "DeleteLevel1":
			OnDeleteButtonPressed(1);
			break;
		case "DeleteLevel2":
			OnDeleteButtonPressed(2);
			break;
		case "DeleteLevel3":
			OnDeleteButtonPressed(3);
			break;
		case "SelectEntitiesWithoutLevel":
			OnSelectEntitiesWithoutLevelButtonPressed();
			break;
		}
	}

	private void OnLevelizeButtonPressed(int level)
	{
		if (SourceSelectionSetName.IsEmpty())
		{
			MessageManager.DisplayMessage("ApplyToSelectionSet is empty!");
			return;
		}
		if (TargetSelectionSetName.IsEmpty())
		{
			MessageManager.DisplayMessage("NewSelectionSetName is empty!");
			return;
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		GameEntity.UpgradeLevelMask levelMask = GetLevelMask(level);
		List<GameEntity> list = CollectEntitiesWithLevel();
		List<GameEntity> list2 = new List<GameEntity>();
		foreach (GameEntity item in list)
		{
			string text = FindPossiblePrefabName(item);
			if (text.IsEmpty())
			{
				num++;
				continue;
			}
			GameEntity.UpgradeLevelMask upgradeLevelMask = item.GetUpgradeLevelMask();
			if ((upgradeLevelMask & levelMask) != TaleWorlds.Engine.GameEntity.UpgradeLevelMask.None)
			{
				num2++;
				list2.Add(item);
				continue;
			}
			string prefabName = ConvertPrefabName(text, levelMask);
			GameEntity gameEntity = TaleWorlds.Engine.GameEntity.Instantiate(base.Scene, prefabName, item.GetGlobalFrame());
			if (gameEntity == null)
			{
				num3++;
				continue;
			}
			num4++;
			GameEntity.UpgradeLevelMask upgradeLevelMask2 = upgradeLevelMask & ~TaleWorlds.Engine.GameEntity.UpgradeLevelMask.Level1 & ~TaleWorlds.Engine.GameEntity.UpgradeLevelMask.Level2 & ~TaleWorlds.Engine.GameEntity.UpgradeLevelMask.Level3;
			upgradeLevelMask2 |= levelMask;
			gameEntity.SetUpgradeLevelMask(upgradeLevelMask2);
			CopyScriptParameters(gameEntity, item);
			list2.Add(gameEntity);
		}
		Debug.Print("Created Entities : " + num4 + "\nAlready Visible In Desired Level : " + num2 + "\nWithout Prefab For Level : " + num3 + "\nWithout Prefab Info : " + num, 0, Debug.DebugColor.Magenta);
		Utilities.CreateSelectionInEditor(list2, TargetSelectionSetName);
	}

	private void CopyScriptParameters(GameEntity entity, GameEntity copyFromEntity)
	{
		if (copyFromEntity.HasScriptComponent("WallSegment") && !entity.HasScriptComponent("WallSegment"))
		{
			entity.CopyScriptComponentFromAnotherEntity(copyFromEntity, "WallSegment");
		}
		if (copyFromEntity.HasScriptComponent("mesh_bender") && !entity.HasScriptComponent("mesh_bender"))
		{
			entity.CopyScriptComponentFromAnotherEntity(copyFromEntity, "mesh_bender");
		}
		for (int i = 0; i < entity.ChildCount && i < copyFromEntity.ChildCount; i++)
		{
			CopyScriptParameters(entity.GetChild(i), copyFromEntity.GetChild(i));
		}
	}

	private GameEntity.UpgradeLevelMask GetLevelMask(int level)
	{
		return level switch
		{
			2 => TaleWorlds.Engine.GameEntity.UpgradeLevelMask.Level2, 
			1 => TaleWorlds.Engine.GameEntity.UpgradeLevelMask.Level1, 
			_ => TaleWorlds.Engine.GameEntity.UpgradeLevelMask.Level3, 
		};
	}

	private string GetLevelSubString(GameEntity.UpgradeLevelMask levelMask)
	{
		return levelMask switch
		{
			TaleWorlds.Engine.GameEntity.UpgradeLevelMask.Level1 => "_l1", 
			TaleWorlds.Engine.GameEntity.UpgradeLevelMask.Level2 => "_l2", 
			TaleWorlds.Engine.GameEntity.UpgradeLevelMask.Level3 => "_l3", 
			_ => "", 
		};
	}

	private string ConvertPrefabName(string prefabName, GameEntity.UpgradeLevelMask newLevelMask)
	{
		string text = prefabName;
		string levelSubString = GetLevelSubString(newLevelMask);
		if (newLevelMask != TaleWorlds.Engine.GameEntity.UpgradeLevelMask.Level1)
		{
			text = text.Replace(GetLevelSubString(TaleWorlds.Engine.GameEntity.UpgradeLevelMask.Level1), levelSubString);
		}
		if (newLevelMask != TaleWorlds.Engine.GameEntity.UpgradeLevelMask.Level2)
		{
			text = text.Replace(GetLevelSubString(TaleWorlds.Engine.GameEntity.UpgradeLevelMask.Level2), levelSubString);
		}
		if (newLevelMask != TaleWorlds.Engine.GameEntity.UpgradeLevelMask.Level3)
		{
			text = text.Replace(GetLevelSubString(TaleWorlds.Engine.GameEntity.UpgradeLevelMask.Level3), levelSubString);
		}
		if (text.Equals(prefabName))
		{
			return "";
		}
		return text;
	}

	private string FindPossiblePrefabName(GameEntity gameEntity)
	{
		string prefabName = gameEntity.GetPrefabName();
		if (prefabName.IsEmpty())
		{
			return gameEntity.GetOldPrefabName();
		}
		return prefabName;
	}

	private void OnDeleteButtonPressed(int level)
	{
		if (SourceSelectionSetName.IsEmpty())
		{
			MessageManager.DisplayMessage("ApplyToSelectionSet is empty!");
			return;
		}
		List<GameEntity> list = CollectEntitiesWithLevel();
		GameEntity.UpgradeLevelMask levelMask = GetLevelMask(level);
		List<GameEntity> list2 = new List<GameEntity>();
		int num = 0;
		int num2 = 0;
		foreach (GameEntity item in list)
		{
			GameEntity.UpgradeLevelMask upgradeLevelMask = item.GetUpgradeLevelMask();
			if (upgradeLevelMask == levelMask)
			{
				list2.Add(item);
				num++;
			}
			else if ((upgradeLevelMask & levelMask) != TaleWorlds.Engine.GameEntity.UpgradeLevelMask.None)
			{
				item.SetUpgradeLevelMask(upgradeLevelMask & ~levelMask);
				num2++;
			}
		}
		Utilities.DeleteEntitiesInEditorScene(list2);
		TextObject textObject = new TextObject("{=!}Deleted entity count : {DELETED_ENTRY_COUNT}");
		TextObject textObject2 = new TextObject("{=!}Removed level mask count : {REMOVED_LEVEL_MASK}");
		textObject.SetTextVariable("DELETED_ENTRY_COUNT", num);
		textObject2.SetTextVariable("REMOVED_LEVEL_MASK", num2);
		MessageManager.DisplayMessage(textObject.ToString());
		MessageManager.DisplayMessage(textObject2.ToString());
	}

	private void OnSelectEntitiesWithoutLevelButtonPressed()
	{
		List<GameEntity> entities = new List<GameEntity>();
		base.Scene.GetEntities(ref entities);
		List<GameEntity> list = entities.FindAll((GameEntity x) => x.GetUpgradeLevelMask() == TaleWorlds.Engine.GameEntity.UpgradeLevelMask.None);
		TextObject textObject = new TextObject("{=!}Selected entity count : {SELECTED_ENTITIES}");
		textObject.SetTextVariable("SELECTED_ENTITIES", list.Count);
		MessageManager.DisplayMessage(textObject.ToString());
		if (list.Count > 0)
		{
			Utilities.SelectEntities(list);
		}
	}

	private List<GameEntity> CollectEntitiesWithLevel()
	{
		List<GameEntity> gameEntities = new List<GameEntity>();
		Utilities.GetEntitiesOfSelectionSet(SourceSelectionSetName, ref gameEntities);
		for (int num = gameEntities.Count - 1; num >= 0; num--)
		{
			if ((gameEntities[num].GetUpgradeLevelMask() & (TaleWorlds.Engine.GameEntity.UpgradeLevelMask.Level1 | TaleWorlds.Engine.GameEntity.UpgradeLevelMask.Level2 | TaleWorlds.Engine.GameEntity.UpgradeLevelMask.Level3)) == 0)
			{
				gameEntities.RemoveAt(num);
			}
		}
		return gameEntities;
	}
}
