using System.Collections.Generic;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond.MultiplayerBadges;

public class BadgeOwnerKillTracker : GameBadgeTracker
{
	private readonly string _badgeId;

	private readonly BadgeCondition _condition;

	private readonly List<string> _requiredBadges;

	private readonly Dictionary<(PlayerId, string, string), int> _dataDictionary;

	private readonly Dictionary<PlayerId, bool> _playerBadgeMap;

	public BadgeOwnerKillTracker(string badgeId, BadgeCondition condition, Dictionary<(PlayerId, string, string), int> dataDictionary)
	{
		_badgeId = badgeId;
		_condition = condition;
		_playerBadgeMap = new Dictionary<PlayerId, bool>();
		_dataDictionary = dataDictionary;
		_requiredBadges = new List<string>();
		foreach (KeyValuePair<string, string> parameter in condition.Parameters)
		{
			if (parameter.Key.StartsWith("required_badge."))
			{
				_requiredBadges.Add(parameter.Value);
			}
		}
	}

	public override void OnPlayerJoin(PlayerData playerData)
	{
		_playerBadgeMap[playerData.PlayerId] = _requiredBadges.Contains(playerData.ShownBadgeId);
	}

	public override void OnKill(KillData killData)
	{
		bool value = default(bool);
		if (killData.KillerId.IsValid && killData.VictimId.IsValid && !killData.KillerId.Equals(killData.VictimId) && _playerBadgeMap.TryGetValue(killData.VictimId, out value) && value)
		{
			_playerBadgeMap[killData.KillerId] = true;
			if (!_dataDictionary.TryGetValue((killData.KillerId, _badgeId, _condition.StringId), out var value2))
			{
				value2 = 0;
			}
			_dataDictionary[(killData.KillerId, _badgeId, _condition.StringId)] = value2 + 1;
		}
	}
}
