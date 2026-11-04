using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

/// <summary>
/// Command tent, the six flank zones, the prisoner pen and troop sampling (field camp v2).
/// Flank zones are 14 m wide strips just inside the palisade: left = training (front),
/// kitchen (middle), supply depot (back); right = horse lines, smithy, infirmary.
/// </summary>
internal sealed partial class FieldCampLayout
{
	/// <summary>Point in a flank zone: u = 0 at the inner edge (toward the road), 1 at the palisade.</summary>
	private Vec2 Flank(float sx, float u, float y)
	{
		return new Vec2(sx * (_zoneInner + u * ZoneWidth), y);
	}

	private static Vec2 Inward(float sx) => new Vec2(-sx, 0f);

	private static Vec2 Outward(float sx) => new Vec2(sx, 0f);

	private void Work(Vec2 position, Vec2 facing, WorkKind work)
	{
		WorkSlots.Add(new Slot { Kind = SlotKind.Work, Position = position, Facing = facing, Work = work });
	}

	/// <summary>The player's tent at the end of the road, with banners, a war table and a chest.</summary>
	private void PlaceCommand(string home)
	{
			int tentAnchor = Props.Count + 1;
			Props.Add(new Prop { Kind = PropKind.Tent, CultureId = home, Position = new Vec2(0f, -2.5f), Yaw = 0f, Variant = 99, Scale = 1.6f });
		Props.Add(new Prop { Kind = PropKind.Flag, CultureId = home, Position = new Vec2(-3f, 6.6f) });
		Props.Add(new Prop { Kind = PropKind.Standard, CultureId = home, Position = new Vec2(-4.4f, 1.8f) });
		Props.Add(new Prop { Kind = PropKind.Standard, CultureId = home, Position = new Vec2(4.4f, 1.8f) });
			Props.Add(new Prop { Kind = PropKind.Table, CultureId = home, Position = new Vec2(0f, -0.6f), TentAnchor = tentAnchor });
			Props.Add(new Prop { Kind = PropKind.Bed, CultureId = home, Position = new Vec2(-0.58f, -0.05f), TentAnchor = tentAnchor });
			Props.Add(new Prop { Kind = PropKind.Chest, CultureId = home, Position = new Vec2(0.58f, -0.45f), TentAnchor = tentAnchor });
			Props.Add(new Prop { Kind = PropKind.Rack, CultureId = home, Position = new Vec2(0.58f, 0.25f), TentAnchor = tentAnchor });
		PlayerStart = new Vec2(0f, 12f);
		PlayerFacing = new Vec2(0f, -1f);
		StandbyOrigin = new Vec2(-8.8f, 8.6f);
	}

	/// <summary>Left front: archery butts along the palisade, archers facing them, practice racks.</summary>
	private void PlaceTraining(string home)
	{
		const float sx = -1f;
		for (int i = 0; i < 4; i++)
		{
			float y = FrontZoneBottom + 3.5f + i * 4f;
			Props.Add(new Prop { Kind = PropKind.Target, Position = Flank(sx, 0.92f, y), Yaw = YawOf(Inward(sx)), Variant = i });
			Work(Flank(sx, 0.18f, y), Outward(sx), WorkKind.Archer);
		}
		Props.Add(new Prop { Kind = PropKind.TrainingRack, CultureId = home, Position = Flank(sx, 0.35f, Front - 2f), Yaw = 0f });
		Props.Add(new Prop { Kind = PropKind.WeaponHeap, Position = Flank(sx, 0.6f, Front - 2f), Yaw = 0.3f, Variant = 0 });
		Work(Flank(sx, 0.5f, FrontZoneBottom + 1.2f), new Vec2(1f, 0f), WorkKind.Gossip);
		Work(Flank(sx, 0.5f, FrontZoneBottom + 1.2f) + new Vec2(1.1f, 0f), new Vec2(-1f, 0f), WorkKind.Gossip);
		// Spent arrows and spare bows below the shooting lanes; a man gathers them up.
		Vec2 arrows = Flank(sx, 0.78f, FrontZoneBottom + 1.2f);
		Props.Add(new Prop { Kind = PropKind.WeaponHeap, Position = arrows, Yaw = 0.4f, Variant = 2 });
		Work(arrows + Inward(sx) * 1.5f, Outward(sx), WorkKind.Pickup);
		Props.Add(new Prop { Kind = PropKind.Clutter, CultureId = home, Position = Flank(sx, 0.15f, Front - 2f), Yaw = 0.3f, Variant = 1 });
	}

	/// <summary>Right front: two tether lines with real horses, a trough and a hay cart.</summary>
	private void PlaceHorseLines(string home)
	{
		const float sx = 1f;
		for (int line = 0; line < 2; line++)
		{
			float y = FrontZoneBottom + 4f + line * 7f;
			Props.Add(new Prop { Kind = PropKind.HorseBarrier, Position = Flank(sx, 0.3f, y), Yaw = 0f, Variant = line * 2 });
			Props.Add(new Prop { Kind = PropKind.HorseBarrier, Position = Flank(sx, 0.62f, y), Yaw = 0f, Variant = line * 2 + 1 });
			for (int i = 0; i < 4; i++)
			{
				// Whole bodies clear the rail, and adjacent 1.6 m clearance discs do not overlap.
					HorseSlots.Add(new Slot { Kind = SlotKind.Horse, Position = Flank(sx, 0.15f + i * 0.24f, y + 3.6f), Facing = new Vec2(0f, -1f) });
			}
		}
		Props.Add(new Prop { Kind = PropKind.Trough, Position = Flank(sx, 0.45f, Front - 4.5f), Yaw = MathF.PI * 0.5f });
		Props.Add(new Prop { Kind = PropKind.HayCart, Position = Flank(sx, 0.85f, FrontZoneBottom + 1.5f), Yaw = 0f });
		Props.Add(new Prop { Kind = PropKind.Woodpile, CultureId = home, Position = Flank(sx, 0.15f, Front - 2.5f), Yaw = 0f, Variant = 2 });
		Work(Flank(sx, 0.08f, FrontZoneBottom + 5.6f), Outward(sx), WorkKind.Groom);
		Work(Flank(sx, 0.9f, FrontZoneBottom + 12.6f), Inward(sx), WorkKind.Groom);
		// Fodder work: pitching hay at the cart, feed sacks beside it, buckets and firewood by the gate end.
		Vec2 hay = Flank(sx, 0.85f, FrontZoneBottom + 1.5f);
		Work(hay + Inward(sx) * 1.9f, Outward(sx), WorkKind.Shovel);
		Props.Add(new Prop { Kind = PropKind.Supply, Position = Flank(sx, 0.5f, FrontZoneBottom + 1.2f), Yaw = 0.7f, Variant = 0 });
		Props.Add(new Prop { Kind = PropKind.Clutter, CultureId = home, Position = Flank(sx, 0.62f, Front - 2.5f), Yaw = 0.2f, Variant = 3 });
		Vec2 logs = Flank(sx, 0.15f, Front - 2.5f);
		Work(logs + Inward(sx) * 1.5f, Outward(sx), WorkKind.Chop);
	}

	/// <summary>Left middle: cooking fires, work tables, provisions and eating benches.</summary>
	private void PlaceKitchen(string home)
	{
		const float sx = -1f;
		Vec2 stew = Flank(sx, 0.4f, 1.5f);
		Vec2 roast = Flank(sx, 0.4f, -4f);
		Props.Add(new Prop { Kind = PropKind.Fire, Position = stew });
		Props.Add(new Prop { Kind = PropKind.Cauldron, CultureId = home, Position = stew, Variant = 1 });
		Props.Add(new Prop { Kind = PropKind.Fire, Position = roast });
		Props.Add(new Prop { Kind = PropKind.Spit, Position = roast, Yaw = MathF.PI * 0.5f, Variant = 1 });
		Work(stew + new Vec2(1.4f, 0f), new Vec2(-1f, 0f), WorkKind.Cook);
		Work(roast + new Vec2(1.4f, 0f), new Vec2(-1f, 0f), WorkKind.Cook);
		for (int i = 0; i < 2; i++)
		{
			Vec2 table = Flank(sx, 0.86f, 2.5f - i * 4.5f);
			Props.Add(new Prop { Kind = PropKind.Table, CultureId = home, Position = table, Yaw = YawOf(Inward(sx)), Variant = 1 + i });
			Work(table + new Vec2(1.3f, 0f), new Vec2(-1f, 0f), WorkKind.Cook);
		}
		for (int i = 0; i < 4; i++)
		{
			Props.Add(new Prop { Kind = PropKind.Supply, Position = Flank(sx, 0.78f + (i % 2) * 0.11f, -7f + (i / 2) * 1.6f), Yaw = i * 0.6f, Variant = i });
		}
		Vec2 kitchenLogs = Flank(sx, 0.1f, -7.2f);
		Props.Add(new Prop { Kind = PropKind.Woodpile, CultureId = home, Position = kitchenLogs, Yaw = 0f, Variant = 0 });
		Work(kitchenLogs + new Vec2(0f, 1.6f), new Vec2(0f, -1f), WorkKind.Chop);
		// Baskets and a water bucket between the work tables.
		Props.Add(new Prop { Kind = PropKind.Clutter, CultureId = home, Position = Flank(sx, 0.7f, 0.2f), Yaw = 0.4f, Variant = 5 });
		Props.Add(new Prop { Kind = PropKind.Clutter, CultureId = home, Position = Flank(sx, 0.92f, 0.2f), Yaw = 1.1f, Variant = 3 });
		for (int b = 0; b < 2; b++)
		{
			float by = 5.2f + b * 1.6f;
			Vec2 bench = Flank(sx, 0.45f, by);
			Props.Add(new Prop { Kind = PropKind.Bench, CultureId = home, Position = bench, Yaw = 0f, Variant = 10 + b });
			for (int seat = -1; seat <= 1; seat += 2)
			{
				BenchSlots.Add(new Slot { Kind = SlotKind.Bench, Position = bench + new Vec2(seat * 0.6f, 0f), Facing = new Vec2(0f, b == 0 ? 1f : -1f), Work = WorkKind.Eat });
			}
		}
	}

	/// <summary>Right middle: anvil and forge fire, weapon racks along the palisade, a cart under repair.</summary>
	private void PlaceSmithy(string home)
	{
		const float sx = 1f;
		Vec2 anvil = Flank(sx, 0.35f, 1.5f);
		Props.Add(new Prop { Kind = PropKind.Anvil, Position = anvil, Yaw = 0f });
		Props.Add(new Prop { Kind = PropKind.Fire, Position = anvil + new Vec2(2.6f, 0f) });
		Work(anvil + new Vec2(0f, -0.9f), new Vec2(0f, 1f), WorkKind.Smith);
		for (int i = 0; i < 2; i++)
		{
			Props.Add(new Prop { Kind = PropKind.Rack, CultureId = home, Position = Flank(sx, 0.93f, 4.5f - i * 4f), Yaw = YawOf(Inward(sx)), Variant = 1 + i });
		}
		Props.Add(new Prop { Kind = PropKind.WeaponHeap, Position = Flank(sx, 0.75f, 6.5f), Yaw = 0.2f, Variant = 1 });
		Vec2 cart = Flank(sx, 0.4f, -5.5f);
		Props.Add(new Prop { Kind = PropKind.Cart, Position = cart, Yaw = 0f, Variant = 2 });
		Work(cart + new Vec2(-1.7f, 0f), new Vec2(1f, 0f), WorkKind.Repair);
		Props.Add(new Prop { Kind = PropKind.Clutter, CultureId = home, Position = Flank(sx, 0.12f, -1.5f), Yaw = 0.5f, Variant = 7 });
		Vec2 smithyLogs = Flank(sx, 0.85f, -6.5f);
		Props.Add(new Prop { Kind = PropKind.Woodpile, CultureId = home, Position = smithyLogs, Yaw = 0f, Variant = 1 });
		Work(smithyLogs + Inward(sx) * 1.6f, Outward(sx), WorkKind.Chop);
		// A workbench for fitting straps and hafts.
		Vec2 bench = Flank(sx, 0.62f, -1.5f);
		Props.Add(new Prop { Kind = PropKind.Table, CultureId = home, Position = bench, Yaw = YawOf(Inward(sx)), Variant = 2 });
		Work(bench + Inward(sx) * 1.2f, Outward(sx), WorkKind.Table);
		Props.Add(new Prop { Kind = PropKind.Clutter, CultureId = home, Position = Flank(sx, 0.62f, 3.5f), Yaw = 0.8f, Variant = 1 });
		CompanionSlots["engineer"] = new Slot { Kind = SlotKind.Companion, Position = anvil + new Vec2(-2f, 1.2f), Facing = new Vec2(1f, 0f) };
	}

	/// <summary>Left back: piles of provisions sized by the food stock, wagons, the quartermaster's chest.</summary>
	private void PlaceSupply(string home, int piles)
	{
		const float sx = -1f;
		for (int i = 0; i < piles; i++)
		{
			Props.Add(new Prop { Kind = PropKind.Supply, Position = Flank(sx, 0.62f + (i % 3) * 0.13f, BackZoneTop - 2.5f - (i / 3) * 1.9f), Yaw = i * 0.5f, Variant = i });
		}
		for (int i = 0; i < 2; i++)
		{
			Vec2 cart = Flank(sx, 0.22f, BackZoneTop - 4f - i * 6.5f);
			Props.Add(new Prop { Kind = PropKind.Cart, Position = cart, Yaw = 0f, Variant = i });
			if (i == 1)
			{
				Work(cart + new Vec2(1.7f, 0f), new Vec2(-1f, 0f), WorkKind.Repair);
			}
		}
		Vec2 ledger = Flank(sx, 0.72f, BackZoneTop - 10f);
		Props.Add(new Prop { Kind = PropKind.Chest, CultureId = home, Position = ledger, Yaw = YawOf(Inward(sx)) });
		// The clerk tallies stores at the chest; a porter stacks loose sacks by the piles.
		Work(ledger + Inward(sx) * 1.1f, Outward(sx), WorkKind.Write);
		Props.Add(new Prop { Kind = PropKind.Supply, Position = Flank(sx, 0.45f, BackZoneTop - 7.5f), Yaw = 0.9f, Variant = 3 });
		Work(Flank(sx, 0.45f, BackZoneTop - 6.2f), new Vec2(0f, -1f), WorkKind.Pickup);
		Vec2 supplyLogs = Flank(sx, 0.75f, BackZoneBottom + 3f);
		Props.Add(new Prop { Kind = PropKind.Woodpile, CultureId = home, Position = supplyLogs, Yaw = 0f, Variant = 3 });
		Work(supplyLogs + new Vec2(0f, 1.6f), new Vec2(0f, -1f), WorkKind.Chop);
		Props.Add(new Prop { Kind = PropKind.Clutter, CultureId = home, Position = Flank(sx, 0.5f, BackZoneBottom + 6f), Yaw = 0.9f, Variant = 8 });
		CompanionSlots["quartermaster"] = new Slot { Kind = SlotKind.Companion, Position = Flank(sx, 0.45f, BackZoneTop - 1.2f), Facing = Outward(sx) };
	}

	/// <summary>Right back: the surgeon's tent, beds with lying wounded, and ground seats for the rest.</summary>
	private void PlaceInfirmary(string home, int totalWounded)
	{
		const float sx = 1f;
		Props.Add(new Prop { Kind = PropKind.Tent, CultureId = home, Position = Flank(sx, 0.72f, BackZoneTop - 4f), Yaw = YawOf(Inward(sx)), Variant = 98, Scale = 1.3f });
		int beds = totalWounded > 0 ? 6 : 2;
		for (int i = 0; i < beds; i++)
		{
			Vec2 bed = Flank(sx, 0.18f + (i % 3) * 0.17f, BackZoneTop - 11f - (i / 3) * 3.2f);
			Props.Add(new Prop { Kind = PropKind.Bed, Position = bed, Yaw = 0f, Variant = i });
			WoundedSlots.Add(new Slot { Kind = SlotKind.Wounded, Position = bed, Facing = new Vec2(0f, 1f), Work = WorkKind.Lie });
		}
		for (int i = 0; i < 6; i++)
		{
			WoundedSlots.Add(new Slot { Kind = SlotKind.Wounded, Position = Flank(sx, 0.2f + (i % 3) * 0.17f, BackZoneBottom + 2.5f + (i / 3) * 1.8f), Facing = new Vec2(0f, 1f), Work = WorkKind.Sit });
		}
		Vec2 dressing = Flank(sx, 0.88f, BackZoneTop - 12f);
		Props.Add(new Prop { Kind = PropKind.Table, CultureId = home, Position = dressing, Yaw = YawOf(Inward(sx)), Variant = 4 });
		Props.Add(new Prop { Kind = PropKind.Chest, CultureId = home, Position = Flank(sx, 0.88f, BackZoneTop - 14.5f), Yaw = YawOf(Inward(sx)) });
		// An orderly washes bandages at the dressing table; water buckets stand beside it.
		Work(dressing + Inward(sx) * 1.2f, Outward(sx), WorkKind.Wash);
		Props.Add(new Prop { Kind = PropKind.Clutter, CultureId = home, Position = Flank(sx, 0.88f, BackZoneBottom + 4.5f), Yaw = 0.3f, Variant = 3 });
		CompanionSlots["surgeon"] = new Slot { Kind = SlotKind.Companion, Position = Flank(sx, 0.08f, BackZoneTop - 9.5f), Facing = Outward(sx) };
	}

	/// <summary>Fenced pen behind the tent streets; the gap faces the road so a slaughter order can reach it.</summary>
	private void PlacePen(string home, int prisonerSlots)
	{
		const float half = 3.5f;
		const float gap = 1.1f;
		Vec2 c = new Vec2(0f, _penY);
		int rows = (prisonerSlots + 2) / 3;
		float back = half - Math.Max(7f, (rows - 1) * 1.6f + 3.8f);
		AddWall(home, c + new Vec2(-half, back), c + new Vec2(half, back));
		AddWall(home, c + new Vec2(-half, back), c + new Vec2(-half, half));
		AddWall(home, c + new Vec2(half, back), c + new Vec2(half, half));
		AddWall(home, c + new Vec2(-half, half), c + new Vec2(-gap, half));
		AddWall(home, c + new Vec2(gap, half), c + new Vec2(half, half));
		for (int i = 0; i < prisonerSlots; i++)
		{
			PrisonerSlots.Add(new Slot { Kind = SlotKind.Prisoner, Position = c + new Vec2(-1.6f + (i % 3) * 1.6f, half - 1.9f - (i / 3) * 1.6f), Facing = new Vec2(0f, 1f) });
		}
		Props.Add(new Prop { Kind = PropKind.Torch, CultureId = home, Position = c + new Vec2(-half - 1f, half + 1f) });
		SentrySlots.Add(new Slot { Kind = SlotKind.Sentry, Position = c + new Vec2(half + 1.2f, half + 1.2f), Facing = new Vec2(-1f, -1f).Normalized() });
	}

	/// <summary>
	/// Duel ring: a fenced octagon with a gap toward the road, behind the left tent street,
	/// between the last tent row and the back wall. Torches at two corners.
	/// </summary>
	private void PlaceRing(string home)
	{
		RingRadius = 5f;
		RingCenter = new Vec2(-(FirstColX + ColPitch * 0.5f), _penY);
		const int sides = 8;
		for (int i = 0; i < sides; i++)
		{
			if (i == 0)
			{
				continue; // the entrance faces +x (toward the road)
			}
			float a0 = (i - 0.5f) * (2f * MathF.PI / sides);
			float a1 = (i + 0.5f) * (2f * MathF.PI / sides);
			AddWall(home, RingCenter + new Vec2(MathF.Cos(a0), MathF.Sin(a0)) * RingRadius, RingCenter + new Vec2(MathF.Cos(a1), MathF.Sin(a1)) * RingRadius);
		}
		Props.Add(new Prop { Kind = PropKind.Torch, CultureId = home, Position = RingCenter + new Vec2(RingRadius + 1f, 2.5f) });
		Props.Add(new Prop { Kind = PropKind.Torch, CultureId = home, Position = RingCenter + new Vec2(RingRadius + 1f, -2.5f) });
		Props.Add(new Prop { Kind = PropKind.WeaponHeap, Position = RingCenter + new Vec2(-RingRadius - 1.5f, 0f), Yaw = 0.3f, Variant = 2 });
		for (int i = 0; i < 4; i++)
		{
			// Spectators lean on the ring fence.
			float a = MathF.PI * (0.35f + i * 0.43f);
			Work(RingCenter + new Vec2(MathF.Cos(a), MathF.Sin(a)) * (RingRadius + 0.8f), new Vec2(-MathF.Cos(a), -MathF.Sin(a)), WorkKind.Lean);
		}
	}

	// Logical gathering position only. The player creates all execution facilities in the B-key builder.
	private void PlaceExecutionGround(string home, bool hasPrisoners)
	{
		HasExecutionGround = hasPrisoners;
		if (!hasPrisoners)
		{
			return;
		}
		ExecutionCenter = new Vec2(FirstColX + ColPitch * 0.5f, _penY);
		ExecutionFacing = new Vec2(-1f, 0f);

	}

	private static bool HasPrisoners(IReadOnlyList<MobileParty> parties)
	{
		TroopRoster prisoners = parties.Count > 0 ? parties[0]?.PrisonRoster : null;
		return prisoners != null && prisoners.TotalManCount > 0;
	}

	/// <summary>Patrol points along the road, the tent lanes and the zone edges (never on a seat or a fire).</summary>
	private void PlaceWander()
	{
		// The road: once in front of the command tent, then every 6 m behind it down to the pen.
		WanderSlots.Add(new Slot { Kind = SlotKind.Wander, Position = new Vec2(3f, 7.5f) });
		int step = 0;
		for (float y = -8.5f; y >= _penY + 5.5f; y -= 6f, step++)
		{
			WanderSlots.Add(new Slot { Kind = SlotKind.Wander, Position = new Vec2(step % 2 == 0 ? 1.5f : -1.5f, y) });
		}
		for (int side = -1; side <= 1; side += 2)
		{
			for (int s = 0; s < _streets; s++)
			{
				float lane = side * (FirstColX + s * StreetPitch + ColPitch * 0.5f);
				WanderSlots.Add(new Slot { Kind = SlotKind.Wander, Position = new Vec2(lane, FirstRowY + 3.5f) });
				if (s > 0)
				{
					// The first street's far end holds the duel ring (left) and execution ground (right).
					WanderSlots.Add(new Slot { Kind = SlotKind.Wander, Position = new Vec2(lane, FirstRowY - RowPitch * (_rows - 1) - 3.5f) });
				}
			}
			WanderSlots.Add(new Slot { Kind = SlotKind.Wander, Position = new Vec2(side * (_zoneInner - 0.5f), MiddleZoneTop + 0.8f) });
			WanderSlots.Add(new Slot { Kind = SlotKind.Wander, Position = new Vec2(side * (_zoneInner - 0.5f), BackZoneTop + 1f) });
			WanderSlots.Add(new Slot { Kind = SlotKind.Wander, Position = new Vec2(side * (ParadeHalfWidth - 1f), FrontZoneBottom + 1f) });
		}
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
			days = parties.Count > 0 ? parties[0]?.GetNumDaysForFoodToLast() ?? 0 : 0;
		}
		catch
		{
		}
		return ClampInt((int)(days / 2f) + 1, 1, 9);
	}

	private static int ClampInt(int value, int min, int max) => value < min ? min : (value > max ? max : value);

	/// <summary>Camp yaw that turns a prop's forward (+y) to <paramref name="direction"/>.</summary>
	private static float YawOf(Vec2 direction) => direction.RotationInRadians;

	private static string CultureOf(MobileParty party)
	{
		return (party?.LeaderHero?.Culture ?? party?.ActualClan?.Culture)?.StringId ?? "";
	}
}
