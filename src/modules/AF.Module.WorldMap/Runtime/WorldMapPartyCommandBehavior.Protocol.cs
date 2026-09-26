using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Helpers;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed partial class WorldMapPartyCommandBehavior : CampaignBehaviorBase
{
	private static bool TryParseTag(string tag, bool validateTargets, out PartyCommandEntry command, out bool stop)
	{
		command = null;
		stop = false;
		if (!WorldMapOrderCoordinator.TryParseToken(tag, out WorldMapOrderToken token))
		{
			return false;
		}
		string[] parts = token.Parts;
		string kind = token.Kind;
		if (token.IsStop)
		{
			stop = true;
			return true;
		}
		if (kind == "MERGE_TO_PLAYER")
		{
			command = new PartyCommandEntry
			{
				Kind = CommandKind.MergeToPlayer.ToString(),
				Days = ParseDays(parts.Length >= 2 ? parts[1] : null)
			};
			return true;
		}
		if (kind == "GO_TO_SETTLEMENT" || kind == "PATROL_SETTLEMENT")
		{
			if (parts.Length < 3 || !string.Equals(parts[1], "settlement", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			string id = parts[2];
			if (!WorldMapOrderCoordinator.IsSafeIdentifier(id))
			{
				return false;
			}
			if (validateTargets && ResolveSettlementById(id) == null)
			{
				return false;
			}
			command = new PartyCommandEntry
			{
				Kind = (kind == "GO_TO_SETTLEMENT") ? CommandKind.GoToSettlement.ToString() : CommandKind.PatrolSettlement.ToString(),
				TargetType = "settlement",
				TargetId = id,
				Days = ParseDays(parts.Length >= 4 ? parts[3] : null)
			};
			return true;
		}
		if (kind == "FOLLOW")
		{
			if (parts.Length < 3)
			{
				return false;
			}
			string targetType = (parts[1] ?? "").Trim().ToLowerInvariant();
			string id = parts[2];
			if (!WorldMapOrderCoordinator.IsSafeIdentifier(id))
			{
				return false;
			}
			if (string.Equals(targetType, "hero", StringComparison.OrdinalIgnoreCase))
			{
				if (validateTargets && ResolveHeroById(id) == null)
				{
					return false;
				}
				command = new PartyCommandEntry
				{
					Kind = CommandKind.FollowHero.ToString(),
					TargetType = "hero",
					TargetId = id,
					Days = ParseDays(parts.Length >= 4 ? parts[3] : null),
					Mode = ""
				};
				return true;
			}
			if (string.Equals(targetType, "party", StringComparison.OrdinalIgnoreCase))
			{
				if (validateTargets && ResolveMobilePartyById(id) == null)
				{
					return false;
				}
				command = new PartyCommandEntry
				{
					Kind = CommandKind.FollowParty.ToString(),
					TargetType = "party",
					TargetId = id,
					Days = ParseDays(parts.Length >= 4 ? parts[3] : null),
					Mode = ""
				};
				return true;
			}
			return false;
		}
		if (kind == "FOLLOW_HERO")
		{
			if (parts.Length < 3 || !string.Equals(parts[1], "hero", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			string id = parts[2];
			if (!WorldMapOrderCoordinator.IsSafeIdentifier(id))
			{
				return false;
			}
			if (validateTargets && ResolveHeroById(id) == null)
			{
				return false;
			}
			command = new PartyCommandEntry
			{
				Kind = CommandKind.FollowHero.ToString(),
				TargetType = "hero",
				TargetId = id,
				Days = ParseDays(parts.Length >= 4 ? parts[3] : null),
				Mode = ""
			};
			return true;
		}
		if (kind == "ATTACK")
		{
			if (parts.Length < 3)
			{
				return false;
			}
			string targetType = (parts[1] ?? "").Trim().ToLowerInvariant();
			string id = parts[2];
			if (!WorldMapOrderCoordinator.IsSafeIdentifier(id))
			{
				return false;
			}
			if (string.Equals(targetType, "hero", StringComparison.OrdinalIgnoreCase))
			{
				if (validateTargets && ResolveHeroById(id) == null)
				{
					return false;
				}
				command = new PartyCommandEntry
				{
					Kind = CommandKind.AttackHero.ToString(),
					TargetType = "hero",
					TargetId = id,
					Days = ParseDays(parts.Length >= 4 ? parts[3] : null, DefaultHeroAttackDays),
					Mode = NormalizeAttackMode(parts.Length >= 5 ? parts[4] : "AI")
				};
				return true;
			}
			if (string.Equals(targetType, "settlement", StringComparison.OrdinalIgnoreCase))
			{
				Settlement settlement = ResolveSettlementById(id);
				if (validateTargets && !IsSupportedAttackSettlement(settlement))
				{
					return false;
				}
				command = new PartyCommandEntry
				{
					Kind = CommandKind.AttackHero.ToString(),
					TargetType = "settlement",
					TargetId = id,
					Days = ParseDays(parts.Length >= 4 ? parts[3] : null, GetDefaultAttackDaysForSettlement(settlement)),
					Mode = NormalizeAttackMode(parts.Length >= 5 ? parts[4] : "AI")
				};
				return true;
			}
			if (string.Equals(targetType, "party", StringComparison.OrdinalIgnoreCase))
			{
				if (validateTargets && ResolveMobilePartyById(id) == null)
				{
					return false;
				}
				command = new PartyCommandEntry
				{
					Kind = CommandKind.AttackParty.ToString(),
					TargetType = "party",
					TargetId = id,
					Days = ParseDays(parts.Length >= 4 ? parts[3] : null, DefaultHeroAttackDays),
					Mode = NormalizeAttackMode(parts.Length >= 5 ? parts[4] : "AI")
				};
				return true;
			}
			return false;
		}
		if (kind == "ATTACK_HERO")
		{
			if (parts.Length < 3)
			{
				return false;
			}
			string targetType = (parts[1] ?? "").Trim().ToLowerInvariant();
			string id = parts[2];
			if (!WorldMapOrderCoordinator.IsSafeIdentifier(id))
			{
				return false;
			}
			if (string.Equals(targetType, "hero", StringComparison.OrdinalIgnoreCase))
			{
				if (validateTargets && ResolveHeroById(id) == null)
				{
					return false;
				}
				command = new PartyCommandEntry
				{
					Kind = CommandKind.AttackHero.ToString(),
					TargetType = "hero",
					TargetId = id,
					Days = ParseDays(parts.Length >= 4 ? parts[3] : null, DefaultHeroAttackDays),
					Mode = NormalizeAttackMode(parts.Length >= 5 ? parts[4] : "AI")
				};
				return true;
			}
			if (string.Equals(targetType, "settlement", StringComparison.OrdinalIgnoreCase))
			{
				Settlement settlement = ResolveSettlementById(id);
				if (validateTargets && !IsSupportedAttackSettlement(settlement))
				{
					return false;
				}
				command = new PartyCommandEntry
				{
					Kind = CommandKind.AttackHero.ToString(),
					TargetType = "settlement",
					TargetId = id,
					Days = ParseDays(parts.Length >= 4 ? parts[3] : null, GetDefaultAttackDaysForSettlement(settlement)),
					Mode = NormalizeAttackMode(parts.Length >= 5 ? parts[4] : "AI")
				};
				return true;
			}
			if (string.Equals(targetType, "party", StringComparison.OrdinalIgnoreCase))
			{
				if (validateTargets && ResolveMobilePartyById(id) == null)
				{
					return false;
				}
				command = new PartyCommandEntry
				{
					Kind = CommandKind.AttackParty.ToString(),
					TargetType = "party",
					TargetId = id,
					Days = ParseDays(parts.Length >= 4 ? parts[3] : null, DefaultHeroAttackDays),
					Mode = NormalizeAttackMode(parts.Length >= 5 ? parts[4] : "AI")
				};
				return true;
			}
		}
		return false;
	}

	private static string BuildTag(PartyCommandEntry command)
	{
		if (command == null)
		{
			return "";
		}
		if (IsKind(command, CommandKind.GoToSettlement))
		{
			return "[ACTION:WORLDMAP_ORDER:GO_TO_SETTLEMENT:settlement:" + command.TargetId + ":" + Math.Max(1, command.Days) + "]";
		}
		if (IsKind(command, CommandKind.PatrolSettlement))
		{
			return "[ACTION:WORLDMAP_ORDER:PATROL_SETTLEMENT:settlement:" + command.TargetId + ":" + Math.Max(1, command.Days) + "]";
		}
		if (IsKind(command, CommandKind.FollowHero))
		{
			return "[ACTION:WORLDMAP_ORDER:FOLLOW:hero:" + command.TargetId + ":" + Math.Max(1, command.Days) + "]";
		}
		if (IsKind(command, CommandKind.FollowParty))
		{
			return "[ACTION:WORLDMAP_ORDER:FOLLOW:party:" + command.TargetId + ":" + Math.Max(1, command.Days) + "]";
		}
		if (IsKind(command, CommandKind.AttackHero))
		{
			string targetType = IsSettlementTarget(command) ? "settlement" : "hero";
			return "[ACTION:WORLDMAP_ORDER:ATTACK:" + targetType + ":" + command.TargetId + ":" + Math.Max(1, command.Days) + ":" + NormalizeAttackMode(command.Mode) + "]";
		}
		if (IsKind(command, CommandKind.AttackParty))
		{
			return "[ACTION:WORLDMAP_ORDER:ATTACK:party:" + command.TargetId + ":" + Math.Max(1, command.Days) + ":" + NormalizeAttackMode(command.Mode) + "]";
		}
		if (IsKind(command, CommandKind.MergeToPlayer))
		{
			return "[ACTION:WORLDMAP_ORDER:MERGE_TO_PLAYER:" + Math.Max(1, command.Days) + "]";
		}
		return "";
	}

	private static int ParseDays(string token)
	{
		return ParseDays(token, 1);
	}

	private static int ParseDays(string token, int defaultDays)
	{
		if (!int.TryParse((token ?? "").Trim(), out int result) || result <= 0)
		{
			return Math.Max(1, defaultDays);
		}
		return result;
	}

	private static string NormalizeAttackMode(string token)
	{
		string text = (token ?? "").Trim();
		if (string.Equals(text, LegacyAttackModeRebellionForce, StringComparison.OrdinalIgnoreCase))
		{
			return AttackModeForce;
		}
		if (string.Equals(text, AttackModeForce, StringComparison.OrdinalIgnoreCase))
		{
			return AttackModeForce;
		}
		return AttackModeAi;
	}

	private static bool IsForceAttackMode(string mode)
	{
		string normalized = NormalizeAttackMode(mode);
		return string.Equals(normalized, AttackModeForce, StringComparison.OrdinalIgnoreCase);
	}
}
