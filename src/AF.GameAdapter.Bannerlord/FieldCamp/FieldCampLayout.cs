using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

/// <summary>
/// Pure camp layout in camp-local metres (origin = camp centre, +y = forward).
/// Built once when the camp mission starts; never touched per frame.
/// Grid: tents sit in rows with 4 m lanes between them; agent targets are only
/// ever placed in lanes, around fires or on the parade ground.
/// </summary>
internal sealed class FieldCampLayout
{
	internal enum PropKind
	{
		Tent,
		Fire,
		Flag,
		Supply,
		Fence,
		Bed,
		Rack,
		Bedroll,
		Spit,
		Cauldron,
		Bench,
		Woodpile,
		Clutter,
		Cart,
		Chest,
		Torch,
		Target,
		TrainingRack,
		HorseBarrier,
		Trough,
		HayCart,
		Standard
	}

	internal struct Prop
	{
		internal PropKind Kind;
		internal string CultureId;
		internal Vec2 Position;
		internal float Yaw;
		internal int Variant;
	}

	internal enum SlotKind
	{
		Fire,
		Sentry,
		Wander,
		Wounded,
		Prisoner,
		Companion,
		Lord
	}

	internal struct Slot
	{
		internal SlotKind Kind;
		internal Vec2 Position;
		internal Vec2 Facing;
	}

	internal sealed class TroopGroup
	{
		internal CharacterObject Character;
		internal int Healthy;
		internal int Wounded;
		internal int SpawnHealthy;
		internal int SpawnWounded;
		internal MobileParty Party;
	}

	internal readonly List<Prop> Props = new List<Prop>();
	internal readonly List<Slot> FireSlots = new List<Slot>();
	internal readonly List<Slot> SentrySlots = new List<Slot>();
	internal readonly List<Slot> WanderSlots = new List<Slot>();
	internal readonly List<Slot> WoundedSlots = new List<Slot>();
	internal readonly List<Slot> PrisonerSlots = new List<Slot>();
	internal readonly Dictionary<string, Slot> CompanionSlots = new Dictionary<string, Slot>(StringComparer.Ordinal);
	/// <summary>Army lord posts beside the first tent of each lord's party (keyed by party).</summary>
	internal readonly Dictionary<MobileParty, Slot> LordSlots = new Dictionary<MobileParty, Slot>();
	internal readonly List<TroopGroup> Groups = new List<TroopGroup>();

	internal Vec2 PlayerStart;
	internal Vec2 ParadeCenter;
	/// <summary>Rows between the banner and the back rank of the parade, for extra companions and overflow lords.</summary>
	internal Vec2 StandbyOrigin;
	internal float Radius;

	private const float TentSpacing = 9f;
	private const float LaneHalfWidth = 2f;

	internal static FieldCampLayout Build(IReadOnlyList<MobileParty> parties, int agentCap, int tentCap)
	{
		var layout = new FieldCampLayout();
		int totalMen = 0;
		int totalWounded = 0;
		for (int i = 0; i < parties.Count; i++)
		{
			TroopRoster roster = parties[i]?.MemberRoster;
			if (roster == null)
			{
				continue;
			}
			totalMen += roster.TotalManCount;
			totalWounded += roster.TotalWounded;
		}
		int tents = ClampInt((totalMen + 7) / 8, 2, Math.Max(2, tentCap));
		int fires = ClampInt(tents / 3, 1, 12);
		int sentries = ClampInt(totalMen / 40, 2, 8);
		int columns = Math.Max(2, (int)Math.Ceiling(Math.Sqrt(tents * 1.5)));
		int rows = (int)Math.Ceiling(tents / (float)columns);

		// Tents fill a grid behind the parade ground (negative y); parade ground in front.
		float width = (columns - 1) * TentSpacing;
		float depth = (rows - 1) * TentSpacing;
		float baseY = -8f;
		int placed = 0;
		int partyIndex = 0;
		int tentsForParty = 0;
		int tentBudgetForParty = TentBudget(parties, 0, totalMen, tents);
		for (int r = 0; r < rows && placed < tents; r++)
		{
			for (int c = 0; c < columns && placed < tents; c++)
			{
				while (tentsForParty >= tentBudgetForParty && partyIndex < parties.Count - 1)
				{
					partyIndex++;
					tentsForParty = 0;
					tentBudgetForParty = TentBudget(parties, partyIndex, totalMen, tents);
				}
				Vec2 pos = new Vec2(-width * 0.5f + c * TentSpacing, baseY - r * TentSpacing);
				layout.Props.Add(new Prop
				{
					Kind = PropKind.Tent,
					CultureId = CultureOf(parties[partyIndex]),
					Position = pos,
					Yaw = 0f,
					Variant = placed
				});
				if (partyIndex > 0 && tentsForParty == 0)
				{
					layout.LordSlots[parties[partyIndex]] = new Slot { Kind = SlotKind.Lord, Position = pos + new Vec2(0f, 4.5f), Facing = new Vec2(0f, 1f) };
					layout.Props.Add(new Prop { Kind = PropKind.Standard, Position = pos + new Vec2(2.6f, 5f) });
				}
				// Bedrolls in front of every tent and some gear beside every other one.
				layout.Props.Add(new Prop { Kind = PropKind.Bedroll, Position = pos + new Vec2(-1.1f, 3.6f), Yaw = 0.08f, Variant = placed * 2 });
				layout.Props.Add(new Prop { Kind = PropKind.Bedroll, Position = pos + new Vec2(1.1f, 3.6f), Yaw = -0.1f, Variant = placed * 2 + 1 });
				if (placed % 2 == 0)
				{
					layout.Props.Add(new Prop { Kind = PropKind.Clutter, Position = pos + new Vec2(2.9f, -2.6f), Yaw = placed * 0.7f, Variant = placed / 2 });
				}
				tentsForParty++;
				placed++;
			}
		}

		// Fires sit at lane crossings between tent rows so seats never touch tents.
		for (int f = 0; f < fires; f++)
		{
			int c = f % Math.Max(1, columns - 1);
			int r = f / Math.Max(1, columns - 1);
			Vec2 pos = new Vec2(-width * 0.5f + (c + 0.5f) * TentSpacing, baseY - (r + 0.5f) * TentSpacing);
			if (rows == 1)
			{
				pos.y = baseY + TentSpacing * 0.5f;
			}
			layout.Props.Add(new Prop { Kind = PropKind.Fire, Position = pos });
			// Something cooking on every fire and two log benches behind the seats, along the column lane.
			layout.Props.Add(new Prop { Kind = f % 2 == 0 ? PropKind.Spit : PropKind.Cauldron, Position = pos, Yaw = f * 0.9f, Variant = f });
			for (int b = 0; b < 2; b++)
			{
				float a = MathF.PI * (0.5f + b);
				layout.Props.Add(new Prop { Kind = PropKind.Bench, Position = pos + new Vec2(MathF.Cos(a), MathF.Sin(a)) * 2.8f, Yaw = a + MathF.PI * 0.5f, Variant = f * 2 + b });
			}
			for (int s = 0; s < 5; s++)
			{
				float a = s * (2f * MathF.PI / 5f);
				Vec2 offset = new Vec2(MathF.Cos(a), MathF.Sin(a)) * 1.7f;
				layout.FireSlots.Add(new Slot { Kind = SlotKind.Fire, Position = pos + offset, Facing = (-offset).Normalized() });
			}
		}

		float halfX = width * 0.5f + 10f;
		float frontY = 14f;
		float backY = baseY - depth - 8f;
		layout.Radius = Math.Max(halfX, Math.Max(frontY, -backY)) + 6f;

		// Banner and supply area on the left flank; wounded tent and beds on the right flank.
		layout.Props.Add(new Prop { Kind = PropKind.Flag, Position = new Vec2(0f, 2f) });
		Vec2 supply = new Vec2(-halfX + 2f, baseY);
		int supplyPiles = SupplyPiles(parties);
		for (int i = 0; i < supplyPiles; i++)
		{
			layout.Props.Add(new Prop { Kind = PropKind.Supply, Position = supply + new Vec2((i % 3) * 1.8f, -(i / 3) * 1.8f), Yaw = i * 0.6f, Variant = i });
		}
		layout.Props.Add(new Prop { Kind = PropKind.Rack, Position = supply + new Vec2(0f, 3.5f), Yaw = MathF.PI * 0.5f });
		layout.Props.Add(new Prop { Kind = PropKind.Cart, Position = supply + new Vec2(-2.6f, -6.5f), Yaw = MathF.PI * 0.5f, Variant = 0 });
		layout.Props.Add(new Prop { Kind = PropKind.Cart, Position = supply + new Vec2(-2.6f, -10.5f), Yaw = MathF.PI * 0.45f, Variant = 1 });
		layout.Props.Add(new Prop { Kind = PropKind.Chest, Position = supply + new Vec2(4.6f, 0.4f), Yaw = MathF.PI * 0.5f });
		layout.Props.Add(new Prop { Kind = PropKind.Woodpile, Position = supply + new Vec2(-1.8f, 0.8f), Yaw = MathF.PI * 0.5f, Variant = 0 });
		layout.Props.Add(new Prop { Kind = PropKind.Woodpile, Position = supply + new Vec2(-1.8f, -1.6f), Yaw = MathF.PI * 0.5f, Variant = 1 });
		// A weapon rack at the left end of every tent row.
		for (int r = 0; r < rows; r++)
		{
			layout.Props.Add(new Prop { Kind = PropKind.Rack, Position = new Vec2(-width * 0.5f - 4.5f, baseY - r * TentSpacing), Yaw = MathF.PI * 0.5f, Variant = r + 1 });
		}
		layout.CompanionSlots["quartermaster"] = new Slot { Kind = SlotKind.Companion, Position = supply + new Vec2(2.5f, 3f), Facing = new Vec2(1f, 0f) };

		Vec2 infirmary = new Vec2(halfX - 2f, baseY);
		layout.Props.Add(new Prop { Kind = PropKind.Tent, CultureId = CultureOf(parties[0]), Position = infirmary + new Vec2(0f, -4f), Variant = 99 });
		int beds = ClampInt((totalWounded + 9) / 10, totalWounded > 0 ? 1 : 0, 6);
		for (int i = 0; i < beds; i++)
		{
			Vec2 bed = infirmary + new Vec2(-2.5f + (i % 3) * 2.5f, 1.5f + (i / 3) * 2.5f);
			layout.Props.Add(new Prop { Kind = PropKind.Bed, Position = bed, Yaw = MathF.PI * 0.5f, Variant = i });
		}
		for (int i = 0; i < 12; i++)
		{
			Vec2 seat = infirmary + new Vec2(-3.5f + (i % 4) * 2.2f, 1f + (i / 4) * 2.4f);
			layout.WoundedSlots.Add(new Slot { Kind = SlotKind.Wounded, Position = seat, Facing = new Vec2(0f, 1f) });
		}
		layout.CompanionSlots["surgeon"] = new Slot { Kind = SlotKind.Companion, Position = infirmary + new Vec2(-4.5f, 4f), Facing = new Vec2(1f, 0f) };

		// Prisoner pen: fenced square behind the tents.
		Vec2 pen = new Vec2(0f, backY + 2f);
		for (int i = 0; i < 4; i++)
		{
			float a = i * MathF.PI * 0.5f;
			Vec2 side = new Vec2(MathF.Cos(a), MathF.Sin(a)) * 3f;
			layout.Props.Add(new Prop { Kind = PropKind.Fence, Position = pen + side, Yaw = a + MathF.PI * 0.5f, Variant = i });
		}
		for (int i = 0; i < 9; i++)
		{
			layout.PrisonerSlots.Add(new Slot { Kind = SlotKind.Prisoner, Position = pen + new Vec2(-1.6f + (i % 3) * 1.6f, -1.6f + (i / 3) * 1.6f), Facing = new Vec2(0f, 1f) });
		}

		// Sentry posts on the perimeter, each with a fence piece and a small fire.
		for (int i = 0; i < sentries; i++)
		{
			float a = MathF.PI * 0.5f + i * (2f * MathF.PI / sentries);
			Vec2 dir = new Vec2(MathF.Cos(a), MathF.Sin(a));
			Vec2 post = new Vec2(dir.x * (halfX + 3f), dir.y > 0f ? dir.y * (frontY + 6f) : dir.y * (-backY + 3f));
			layout.Props.Add(new Prop { Kind = PropKind.Fence, Position = post + dir * 1.5f, Yaw = a + MathF.PI * 0.5f, Variant = 10 + i });
			layout.Props.Add(new Prop { Kind = PropKind.Fire, Position = post - dir * 1.2f + dir.LeftVec() * 1.2f, Variant = 1 });
			layout.SentrySlots.Add(new Slot { Kind = SlotKind.Sentry, Position = post, Facing = dir });
		}
		layout.CompanionSlots["scout"] = layout.SentrySlots.Count > 0
			? new Slot { Kind = SlotKind.Companion, Position = layout.SentrySlots[0].Position - layout.SentrySlots[0].Facing * 2.5f, Facing = layout.SentrySlots[0].Facing }
			: new Slot { Kind = SlotKind.Companion, Position = new Vec2(0f, frontY), Facing = new Vec2(0f, 1f) };
		layout.CompanionSlots["engineer"] = new Slot { Kind = SlotKind.Companion, Position = new Vec2(-3f, 2f), Facing = new Vec2(0f, 1f) };

		// Torches where the tent lanes open onto the parade ground (a single row has its fires there) and at its corners.
		for (int c = 0; rows > 1 && c < columns - 1; c++)
		{
			layout.Props.Add(new Prop { Kind = PropKind.Torch, Position = new Vec2(-width * 0.5f + (c + 0.5f) * TentSpacing + 1.6f, baseY + 3.6f) });
		}
		for (int i = 0; i < 4; i++)
		{
			layout.Props.Add(new Prop { Kind = PropKind.Torch, Position = new Vec2((i % 2 == 0 ? -1f : 1f) * (halfX - 3f), i < 2 ? 5f : frontY + 6f) });
		}
		// Training ground on the left of the parade: archery butts facing the camp centre and a practice rack.
		for (int i = 0; i < 3; i++)
		{
			layout.Props.Add(new Prop { Kind = PropKind.Target, Position = new Vec2(-halfX + 1f, frontY - 3f + i * 3f), Yaw = -MathF.PI * 0.5f, Variant = i });
		}
		layout.Props.Add(new Prop { Kind = PropKind.TrainingRack, Position = new Vec2(-halfX + 5f, frontY + 6f), Yaw = 0f });
		// Horse lines on the right of the parade.
		layout.Props.Add(new Prop { Kind = PropKind.HorseBarrier, Position = new Vec2(halfX + 1f, frontY - 2f), Yaw = 0f, Variant = 0 });
		layout.Props.Add(new Prop { Kind = PropKind.HorseBarrier, Position = new Vec2(halfX + 1f, frontY + 2.5f), Yaw = 0f, Variant = 1 });
		layout.Props.Add(new Prop { Kind = PropKind.Trough, Position = new Vec2(halfX - 1.2f, frontY + 0.2f), Yaw = MathF.PI * 0.5f });
		layout.Props.Add(new Prop { Kind = PropKind.HayCart, Position = new Vec2(halfX + 1.5f, frontY + 7f), Yaw = 0.3f });

		// Wander targets sit in the column lanes level with the tents, so they never
		// coincide with the fires at the lane crossings.
		for (int c = 0; c < columns - 1; c++)
		{
			for (int r = 0; r < rows; r++)
			{
				layout.WanderSlots.Add(new Slot { Kind = SlotKind.Wander, Position = new Vec2(-width * 0.5f + (c + 0.5f) * TentSpacing, baseY - r * TentSpacing) });
			}
		}
		for (int i = -2; i <= 2; i++)
		{
			layout.WanderSlots.Add(new Slot { Kind = SlotKind.Wander, Position = new Vec2(i * 5f, 7f) });
		}

		layout.ParadeCenter = new Vec2(0f, frontY);
		layout.StandbyOrigin = new Vec2(-9f, 3.2f);
		layout.PlayerStart = new Vec2(0f, frontY + 8f);
		layout.BuildGroups(parties, agentCap);
		return layout;
	}

	private void BuildGroups(IReadOnlyList<MobileParty> parties, int agentCap)
	{
		int totalRegulars = 0;
		for (int p = 0; p < parties.Count; p++)
		{
			TroopRoster roster = parties[p]?.MemberRoster;
			if (roster == null)
			{
				continue;
			}
			for (int i = 0; i < roster.Count; i++)
			{
				TroopRosterElement element = roster.GetElementCopyAtIndex(i);
				if (element.Character == null || element.Character.IsHero || element.Number <= 0)
				{
					continue;
				}
				Groups.Add(new TroopGroup
				{
					Character = element.Character,
					Healthy = element.Number - element.WoundedNumber,
					Wounded = element.WoundedNumber,
					Party = parties[p]
				});
				totalRegulars += element.Number;
			}
		}
		if (totalRegulars <= 0)
		{
			return;
		}
		// Proportional sampling with at least one agent for every troop type that has men.
		int cap = Math.Max(1, agentCap);
		float scale = Math.Min(1f, cap / (float)totalRegulars);
		int used = 0;
		for (int i = 0; i < Groups.Count; i++)
		{
			TroopGroup g = Groups[i];
			int healthy = g.Healthy > 0 ? Math.Max(1, (int)Math.Round(g.Healthy * scale)) : 0;
			int wounded = g.Wounded > 0 ? (int)Math.Round(g.Wounded * scale) : 0;
			g.SpawnHealthy = healthy;
			g.SpawnWounded = wounded;
			used += healthy + wounded;
		}
		for (int i = Groups.Count - 1; i >= 0 && used > cap; i--)
		{
			TroopGroup g = Groups[i];
			int trimWounded = Math.Min(g.SpawnWounded, used - cap);
			g.SpawnWounded -= trimWounded;
			used -= trimWounded;
			int trimHealthy = Math.Min(Math.Max(0, g.SpawnHealthy - 1), used - cap);
			g.SpawnHealthy -= trimHealthy;
			used -= trimHealthy;
		}
	}

	private static int TentBudget(IReadOnlyList<MobileParty> parties, int index, int totalMen, int tents)
	{
		int men = parties[index]?.MemberRoster?.TotalManCount ?? 0;
		return Math.Max(1, (int)Math.Round(tents * (totalMen <= 0 ? 1f : men / (float)totalMen)));
	}

	private static int SupplyPiles(IReadOnlyList<MobileParty> parties)
	{
		float days = 0f;
		try
		{
			days = parties[0]?.GetNumDaysForFoodToLast() ?? 0;
		}
		catch
		{
		}
		return ClampInt((int)(days / 2f) + 1, 1, 9);
	}

	private static int ClampInt(int value, int min, int max) => value < min ? min : (value > max ? max : value);

	private static string CultureOf(MobileParty party)
	{
		return (party?.LeaderHero?.Culture ?? party?.ActualClan?.Culture)?.StringId ?? "";
	}
}
