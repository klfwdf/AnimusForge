using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class HideoutSpawnPointGroup : SynchedMissionObject
{
	private const int NumberOfDefaultFormations = 4;

	public BattleSideEnum Side;

	public int PhaseNumber;

	private GameEntity[] _spawnPoints;

	protected internal override void OnInit()
	{
		base.OnInit();
		_spawnPoints = new GameEntity[4];
		string spawnPointTagAffix = Side.ToString().ToLower() + "_";
		string[] array = new string[4];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = spawnPointTagAffix + ((FormationClass)i).GetName().ToLower();
		}
		foreach (WeakGameEntity item in from ce in base.GameEntity.GetChildren()
			where ce.Tags.Any((string t) => t.StartsWith(spawnPointTagAffix))
			select ce)
		{
			for (int num = 0; num < array.Length; num++)
			{
				if (item.HasTag(array[num]))
				{
					_spawnPoints[num] = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(item);
					break;
				}
			}
		}
	}

	public MatrixFrame[] GetSpawnPointFrames()
	{
		MatrixFrame[] array = new MatrixFrame[_spawnPoints.Length];
		for (int i = 0; i < _spawnPoints.Length; i++)
		{
			array[i] = ((_spawnPoints[i] != null) ? _spawnPoints[i].GetGlobalFrame() : MatrixFrame.Identity);
		}
		return array;
	}

	public void RemoveWithAllChildren()
	{
		base.GameEntity.RemoveAllChildren();
		base.GameEntity.Remove(83);
	}
}
