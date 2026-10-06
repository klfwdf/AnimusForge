using System.Collections.Generic;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class MultiplayerSceneValidator : ScriptComponentBehavior
{
	public SimpleButton SelectFaultyEntities;

	protected internal override void OnEditorVariableChanged(string variableName)
	{
		base.OnEditorVariableChanged(variableName);
		if (variableName == "SelectFaultyEntities")
		{
			SelectInvalidEntities();
		}
	}

	protected internal override void OnSceneSave(string saveFolder)
	{
		base.OnSceneSave(saveFolder);
		foreach (GameEntity invalidEntity in GetInvalidEntities())
		{
			_ = invalidEntity;
		}
	}

	private List<GameEntity> GetInvalidEntities()
	{
		List<GameEntity> list = new List<GameEntity>();
		List<GameEntity> entities = new List<GameEntity>();
		base.Scene.GetEntities(ref entities);
		foreach (GameEntity item in entities)
		{
			foreach (ScriptComponentBehavior scriptComponent in item.GetScriptComponents())
			{
				if (scriptComponent != null && (scriptComponent.GetType().IsSubclassOf(typeof(MissionObject)) || (scriptComponent.GetType() == typeof(MissionObject) && scriptComponent.IsOnlyVisual())))
				{
					list.Add(item);
					break;
				}
			}
		}
		return list;
	}

	private void SelectInvalidEntities()
	{
		base.GameEntity.DeselectEntityOnEditor();
		foreach (GameEntity invalidEntity in GetInvalidEntities())
		{
			invalidEntity.SelectEntityOnEditor();
		}
	}
}
