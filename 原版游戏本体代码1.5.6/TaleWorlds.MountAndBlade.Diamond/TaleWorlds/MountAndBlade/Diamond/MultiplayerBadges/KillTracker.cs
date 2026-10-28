using System.Collections.Generic;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond.MultiplayerBadges;

public class KillTracker : GameBadgeTracker
{
	private readonly string _badgeId;

	private readonly BadgeCondition _condition;

	private readonly Dictionary<(PlayerId, string, string), int> _dataDictionary;

	private readonly string _faction;

	private readonly string _troop;

	public KillTracker(string badgeId, BadgeCondition condition, Dictionary<(PlayerId, string, string), int> dataDictionary)
	{
		_badgeId = badgeId;
		_condition = condition;
		_dataDictionary = dataDictionary;
		_faction = null;
		_troop = null;
		if (condition.Parameters.TryGetValue("faction", out var value))
		{
			_faction = value;
		}
		if (condition.Parameters.TryGetValue("troop", out var value2))
		{
			_troop = value2;
		}
	}

	public override void OnKill(KillData killData)
	{
		if (killData.KillerId.IsValid && killData.VictimId.IsValid && !killData.KillerId.Equals(killData.VictimId) && (_faction == null || _faction == killData.KillerFaction) && (_troop == null || _troop == killData.KillerTroop))
		{
			if (!_dataDictionary.TryGetValue((killData.KillerId, _badgeId, _condition.StringId), out var value))
			{
				value = 0;
			}
			_dataDictionary[(killData.KillerId, _badgeId, _condition.StringId)] = value + 1;
		}
	}
}
