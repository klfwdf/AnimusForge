using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View;

public class ItemVisualizer : ScriptComponentBehavior
{
	private MBGameManager _editorGameManager;

	private bool isFinished;

	protected override void OnEditorInit()
	{
		base.OnEditorInit();
		if (Game.Current == null)
		{
			_editorGameManager = new EditorGameManager();
		}
	}

	protected override void OnEditorTick(float dt)
	{
		if (!isFinished && _editorGameManager != null)
		{
			isFinished = !_editorGameManager.DoLoadingForGameManager();
			if (isFinished)
			{
				SpawnItems();
			}
		}
	}

	private void SpawnItems()
	{
		Scene scene = base.GameEntity.Scene;
		MBReadOnlyList<ItemObject> objectTypeList = Game.Current.ObjectManager.GetObjectTypeList<ItemObject>();
		Vec3 localPosition = new Vec3(100f, 100f)
		{
			z = 2f
		};
		float num = 0f;
		float num2 = 200f;
		foreach (ItemObject item in (IEnumerable<ItemObject>)objectTypeList)
		{
			if (item.MultiMeshName.Length <= 0)
			{
				continue;
			}
			MetaMesh copy = MetaMesh.GetCopy(item.MultiMeshName, showErrors: true, mayReturnNull: true);
			if (copy != null)
			{
				GameEntity gameEntity = TaleWorlds.Engine.GameEntity.CreateEmpty(scene);
				gameEntity.EntityFlags |= EntityFlags.DontSaveToScene;
				gameEntity.AddMultiMesh(copy);
				gameEntity.Name = item.Name.ToString();
				gameEntity.RecomputeBoundingBox();
				float boundingBoxRadius = gameEntity.GetBoundingBoxRadius();
				if (boundingBoxRadius > num)
				{
					num = boundingBoxRadius;
				}
				localPosition.x += boundingBoxRadius;
				if (localPosition.x > num2)
				{
					localPosition.x = 100f;
					localPosition.y += num * 3f;
					num = 0f;
				}
				gameEntity.SetLocalPosition(localPosition);
				localPosition.x += boundingBoxRadius;
				if (localPosition.x > num2)
				{
					localPosition.x = 100f;
					localPosition.y += num * 3f;
					num = 0f;
				}
			}
		}
	}
}
