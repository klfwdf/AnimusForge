using System.Collections.Generic;
using System.Linq;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;

namespace SandBox.Objects.AreaMarkers;

public class CommonAreaMarker : AreaMarker
{
	public string Type = "";

	public List<MatrixFrame> HiddenSpawnFrames { get; private set; }

	public override string Tag => GetAlley()?.Tag;

	protected override void OnInit()
	{
		HiddenSpawnFrames = new List<MatrixFrame>();
	}

	public override List<UsableMachine> GetUsableMachinesInRange(string excludeTag = null)
	{
		List<UsableMachine> usableMachinesInRange = base.GetUsableMachinesInRange();
		for (int num = usableMachinesInRange.Count - 1; num >= 0; num--)
		{
			UsableMachine usableMachine = usableMachinesInRange[num];
			string[] tags = usableMachine.GameEntity.Tags;
			if (usableMachine.GameEntity.HasScriptOfType<Passage>() || (!tags.Contains("npc_common") && !tags.Contains("npc_common_limited") && !tags.Contains("sp_guard") && !tags.Contains("sp_guard_unarmed") && !tags.Contains("sp_notable")))
			{
				usableMachinesInRange.RemoveAt(num);
			}
		}
		List<GameEntity> list = Mission.Current.Scene.FindEntitiesWithTag("sp_common_hidden").ToList();
		GameEntity gameEntity = null;
		float num2 = float.MaxValue;
		float num3 = AreaRadius * AreaRadius;
		for (int num4 = list.Count - 1; num4 >= 0; num4--)
		{
			float num5 = list[num4].GlobalPosition.DistanceSquared(base.GameEntity.GlobalPosition);
			if (num5 < num2)
			{
				gameEntity = list[num4];
				num2 = num5;
			}
			if (num5 < num3)
			{
				HiddenSpawnFrames.Add(list[num4].GetGlobalFrame());
			}
		}
		if (HiddenSpawnFrames.IsEmpty() && gameEntity != null)
		{
			HiddenSpawnFrames.Add(gameEntity.GetGlobalFrame());
		}
		return usableMachinesInRange;
	}

	public Alley GetAlley()
	{
		Alley result = null;
		Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
		if (settlement != null && settlement?.Alleys != null && AreaIndex > 0 && AreaIndex <= settlement.Alleys.Count)
		{
			result = settlement.Alleys[AreaIndex - 1];
		}
		return result;
	}

	public override TextObject GetName()
	{
		return GetAlley()?.Name;
	}
}
