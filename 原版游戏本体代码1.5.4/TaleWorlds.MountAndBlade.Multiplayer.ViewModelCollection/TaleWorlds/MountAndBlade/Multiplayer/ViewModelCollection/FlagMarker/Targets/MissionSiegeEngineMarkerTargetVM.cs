using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.FlagMarker.Targets;

public class MissionSiegeEngineMarkerTargetVM : MissionMarkerTargetVM
{
	private readonly GameEntity _siegeEngine;

	public readonly BattleSideEnum Side;

	private string _siegeEngineID;

	public override Vec3 WorldPosition
	{
		get
		{
			if (!(_siegeEngine != null))
			{
				return Vec3.One;
			}
			return _siegeEngine.GlobalPosition;
		}
	}

	protected override float HeightOffset => 2.5f;

	[DataSourceProperty]
	public string SiegeEngineID
	{
		get
		{
			return _siegeEngineID;
		}
		set
		{
			if (value != _siegeEngineID)
			{
				_siegeEngineID = value;
				OnPropertyChangedWithValue(value, "SiegeEngineID");
			}
		}
	}

	public MissionSiegeEngineMarkerTargetVM(SiegeWeapon siegeEngine)
		: base(MissionMarkerType.SiegeEngine)
	{
		_siegeEngine = GameEntity.CreateFromWeakEntity(siegeEngine.GameEntity);
		Side = siegeEngine.Side;
		SiegeEngineID = siegeEngine.GetSiegeEngineType().StringId;
		RefreshColor((Side == BattleSideEnum.Attacker) ? Mission.Current.AttackerTeam.Color : Mission.Current.DefenderTeam.Color, (Side == BattleSideEnum.Attacker) ? Mission.Current.AttackerTeam.Color2 : Mission.Current.DefenderTeam.Color2);
	}
}
