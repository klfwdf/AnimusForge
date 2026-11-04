using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

/// <summary>
/// Pure camp layout in camp-local metres (origin = camp centre, +y = toward the gate).
/// Built once when the camp mission starts; never touched per frame.
/// Plan (field camp v2): a palisade with a gate in the front wall, the parade ground
/// inside the gate, a road from the gate to the player's command tent, tent streets on
/// both sides of the road (two facing tent columns per street with fires and benches in
/// the lane), training ground / kitchen / supply depot on the left flank, horse lines /
/// smithy / infirmary on the right flank and the prisoner pen behind the streets.
/// The number of streets and rows follows the tent count, so the camp grows with the army.
/// </summary>
internal sealed partial class FieldCampLayout
{
	internal enum PropKind
	{
		Tent,
		Fire,
		Flag,
		Supply,
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
		Standard,
		/// <summary>A straight palisade run from Position to End, filled with fence pieces.</summary>
		Wall,
		Table,
		Anvil,
		WeaponHeap,
		Bed,
		ExecutionBlock,
			Gallows,
			Bag,
		/// <summary>Stone ring under a fire (walk-through).</summary>
		Hearth,
		/// <summary>Fur or carpet on the ground (walk-through).</summary>
		Rug,
		/// <summary>Burnt-out logs of an old fire (walk-through).</summary>
		Ash,
		/// <summary>Bowls, plates and cups on the ground or a table (walk-through).</summary>
		Dishes,
		/// <summary>Bread, ham, vegetables and cutting boards; normally stacked on a table (walk-through).</summary>
		Food,
		/// <summary>Rope coils, hides, cart wheels and poles (solid).</summary>
		Gear,
		/// <summary>Broken carts and cart parts (solid).</summary>
		Wreck
	}

	internal struct Prop
	{
		internal PropKind Kind;
		internal string CultureId;
		internal Vec2 Position;
		internal Vec2 End;
		internal float Yaw;
		internal int Variant;
		/// <summary>Uniform prefab scale; 0 means 1.</summary>
			internal float Scale;
			/// <summary>One-based index of the tent prop; Position/Yaw are in its doorway-local frame.</summary>
			internal int TentAnchor;
		/// <summary>One-based index of an earlier standalone prop (a table); this prop rests on its measured top.</summary>
		internal int StackOn;
	}

	internal enum SlotKind
	{
		Fire,
		Sentry,
		Wander,
		Wounded,
		Prisoner,
		Companion,
		Lord,
		Bench,
		Work,
		Horse
	}

	/// <summary>What a soldier does at a bench or work slot (selects the animation pool).</summary>
	internal enum WorkKind
	{
		None,
		Sit,
		Eat,
		Cook,
		Smith,
		Repair,
		Archer,
		Groom,
		Lean,
		Gossip,
		Lie,
		Chop,
		Shovel,
		Wash,
		Write,
		Pickup,
		Table
	}

	internal struct Slot
	{
		internal SlotKind Kind;
		internal Vec2 Position;
		internal Vec2 Facing;
		internal WorkKind Work;
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
	internal readonly List<Slot> BenchSlots = new List<Slot>();
	internal readonly List<Slot> WorkSlots = new List<Slot>();
	internal readonly List<Slot> SentrySlots = new List<Slot>();
	internal readonly List<Slot> WanderSlots = new List<Slot>();
	internal readonly List<Slot> WoundedSlots = new List<Slot>();
	internal readonly List<Slot> PrisonerSlots = new List<Slot>();
	internal readonly List<Slot> HorseSlots = new List<Slot>();
	internal readonly Dictionary<string, Slot> CompanionSlots = new Dictionary<string, Slot>(StringComparer.Ordinal);
	/// <summary>Army lord posts in front of the first tent of each lord's party (keyed by party).</summary>
	internal readonly Dictionary<MobileParty, Slot> LordSlots = new Dictionary<MobileParty, Slot>();
	internal readonly List<TroopGroup> Groups = new List<TroopGroup>();

	internal Vec2 PlayerStart;
	internal Vec2 PlayerFacing;
	/// <summary>Front rank centre of the assembled formation; ranks grow toward -y.</summary>
	internal Vec2 ParadeCenter;
	/// <summary>Half the width the assembled formation may take before it reaches the flank zones.</summary>
	internal float ParadeHalfWidth;
	/// <summary>Rows in front of the command tent for extra companions and overflow lords.</summary>
	internal Vec2 StandbyOrigin;
	internal float Radius;
	/// <summary>Palisade extents: x in [-HalfX, HalfX], y in [Back, Front].</summary>
	internal float HalfX;
	internal float Front;
	internal float Back;
	/// <summary>Fenced duel ring behind the left tent streets; duelists start on opposite sides.</summary>
	internal Vec2 RingCenter;
	internal float RingRadius;
	/// <summary>Execution ground behind the right tent streets (only laid out when the party has prisoners).</summary>
	internal bool HasExecutionGround;
	internal Vec2 ExecutionCenter;
	/// <summary>Camp-local direction the condemned face (toward the road and the watching soldiers).</summary>
	internal Vec2 ExecutionFacing;

	private const float FirstColX = 8f;
	private const float ColPitch = 9f;
	private const float StreetPitch = 18f;
	private const float FirstRowY = -12f;
	private const float RowPitch = 8.5f;
	private const float TentHalf = 2.25f;
	private const int MaxRowsPerStreet = 5;
	private const float ZoneGap = 1.5f;
	private const float ZoneWidth = 14f;
	private const float FrontWallY = 30f;
	private const float GateWidth = 6f;
	private const float FrontZoneBottom = 9.5f;
	private const float MiddleZoneTop = 8f;
	private const float MiddleZoneBottom = -9f;
	private const float BackZoneTop = -11f;
	private const float BackZoneBottom = -32f;

	private int _streets;
	private int _rows;
	private float _zoneInner;
	private float _zoneOuter;
	private float _penY;

	internal static FieldCampLayout Build(IReadOnlyList<MobileParty> parties, int agentCap, int tentCap)
	{
		var layout = new FieldCampLayout();
		int totalMen = 0;
		int totalWounded = 0;
		for (int i = 0; i < parties.Count; i++)
		{
			totalMen += parties[i]?.MemberRoster?.TotalManCount ?? 0;
			totalWounded += parties[i]?.MemberRoster?.TotalWounded ?? 0;
		}
		int tents = ClampInt((totalMen + 7) / 8, 2, Math.Max(2, tentCap));
		string home = CultureOf(parties.Count > 0 ? parties[0] : null);
		// Noble captives are actual residents, not a capped sample. Reserve their
		// pen before the perimeter, site solver and ground cache consume the layout.
		int noblePrisoners = 0;
		TroopRoster prisonRoster = PartyBase.MainParty?.PrisonRoster;
		if (prisonRoster != null)
			for (int i = 0; i < prisonRoster.Count; i++)
			{
				TroopRosterElement prisoner = prisonRoster.GetElementCopyAtIndex(i);
				if (prisoner.Number > 0 && prisoner.Character?.IsHero == true && prisoner.Character.HeroObject?.IsAlive == true)
					noblePrisoners++;
			}
		int prisonerSlots = Math.Max(9, noblePrisoners);
		layout.PlaceTentStreets(parties, totalMen, tents);
		int prisonerRows = (prisonerSlots + 2) / 3;
		float penDepth = Math.Max(7f, (prisonerRows - 1) * 1.6f + 3.8f);
		layout.Back = Math.Min(layout.Back, layout._penY + 3.5f - penDepth - 1.5f);
		layout.PlaceFrame(home);
		layout.PlaceCommand(home);
		layout.PlaceTraining(home);
		layout.PlaceHorseLines(home);
		layout.PlaceKitchen(home);
		layout.PlaceSmithy(home);
		layout.PlaceSupply(home, SupplyPiles(parties));
		layout.PlaceInfirmary(home, totalWounded);
		layout.PlacePen(home, prisonerSlots);
		layout.PlaceRing(home);
		layout.PlaceExecutionGround(home, HasPrisoners(parties));
		layout.PlaceWander();
		layout.Radius = Math.Max(layout.HalfX, Math.Max(layout.Front, -layout.Back)) + 3f;
		layout.BuildGroups(parties, agentCap);
		return layout;
	}

	/// <summary>
	/// Tents fill the left side first, then the right; each party's tents stay together and
	/// its first tent carries the party standard and (for army lords) the lord's post.
	/// </summary>
	private void PlaceTentStreets(IReadOnlyList<MobileParty> parties, int totalMen, int tents)
	{
		int left = (tents + 1) / 2;
		int right = tents - left;
		_streets = Math.Max(1, (int)Math.Ceiling(left / (float)(2 * MaxRowsPerStreet)));
		_rows = Math.Max(1, (int)Math.Ceiling(left / (float)(2 * _streets)));
		float lastRowY = FirstRowY - RowPitch * (_rows - 1);
		_zoneInner = FirstColX + StreetPitch * (_streets - 1) + ColPitch + TentHalf + ZoneGap;
		_zoneOuter = _zoneInner + ZoneWidth;
		HalfX = _zoneOuter + 1f;
		Front = FrontWallY;
		_penY = lastRowY - TentHalf - 5.5f;
		Back = Math.Min(_penY - 5f, BackZoneBottom - 2f);

		int partyIndex = 0;
		int tentsForParty = 0;
		int budget = TentBudget(parties, 0, totalMen, tents);
		int placed = 0;
		int fireIndex = 0;
		for (int side = 0; side < 2; side++)
		{
			float sx = side == 0 ? -1f : 1f;
			int sideTents = side == 0 ? left : right;
			int n = 0;
			for (int s = 0; s < _streets && n < sideTents; s++)
			{
				float inner = sx * (FirstColX + s * StreetPitch);
				float outer = sx * (FirstColX + s * StreetPitch + ColPitch);
				float lane = (inner + outer) * 0.5f;
				int streetStart = n;
				string streetCulture = "";
				for (int k = 0; k < _rows && n < sideTents; k++)
				{
					float y = FirstRowY - k * RowPitch;
					for (int c = 0; c < 2 && n < sideTents; c++, n++)
					{
						while (tentsForParty >= budget && partyIndex < parties.Count - 1)
						{
							partyIndex++;
							tentsForParty = 0;
							budget = TentBudget(parties, partyIndex, totalMen, tents);
						}
						MobileParty party = parties[partyIndex];
						string culture = CultureOf(party);
						streetCulture = streetCulture.Length == 0 ? culture : streetCulture;
						float x = c == 0 ? inner : outer;
						Vec2 toLane = new Vec2(lane > x ? 1f : -1f, 0f);
						Vec2 pos = new Vec2(x, y);
							int tentAnchor = Props.Count + 1;
							Props.Add(new Prop { Kind = PropKind.Tent, CultureId = culture, Position = pos, Yaw = YawOf(toLane), Variant = placed });
							// The host fits these to the measured interior; keep the entrance/centre aisle clear.
							Props.Add(new Prop { Kind = PropKind.Bedroll, CultureId = culture, Position = new Vec2(-0.55f, -0.15f), TentAnchor = tentAnchor, Variant = placed });
							Props.Add(new Prop { Kind = PropKind.Chest, CultureId = culture, Position = new Vec2(0.58f, -0.5f), TentAnchor = tentAnchor });
							Props.Add(new Prop { Kind = PropKind.Bag, CultureId = culture, Position = new Vec2(0.58f, 0.3f), TentAnchor = tentAnchor, Variant = 0 });
							// One man beside each tent mouth, 1.6 m off the door axis so the entrance aisle stays clear:
							// most rest on the ground, every third mends kit.
							Vec2 mouth = pos + toLane * 3.1f + new Vec2(0f, 1.6f);
							bool mending = placed % 3 == 0;
							WorkSlots.Add(new Slot { Kind = SlotKind.Work, Position = mouth, Facing = toLane,
								Work = mending ? WorkKind.Repair : WorkKind.Sit });
							// The mended kit 0.9 m toward the lane: >=1.1 m from the street-fire seats, and
							// facing tents never both mend, so the lane keeps a walkway of about 1.1 m.
							if (mending) Props.Add(new Prop { Kind = PropKind.Clutter, CultureId = culture, Position = mouth + toLane * 0.9f, Yaw = 0.4f * placed, Variant = placed });
							// The resting man sits on a fur or carpet (walk-through).
							else Props.Add(new Prop { Kind = PropKind.Rug, Position = mouth, Yaw = YawOf(toLane), Variant = placed });
							if (tentsForParty == 0)
							{
								Props.Add(new Prop { Kind = PropKind.Standard, CultureId = culture, Position = pos + toLane * 1.8f + new Vec2(0f, -3.1f) });
							if (partyIndex > 0)
							{
								LordSlots[party] = new Slot { Kind = SlotKind.Lord, Position = pos + toLane * 3f, Facing = toLane };
							}
						}
						tentsForParty++;
						placed++;
					}
				}
				int rowsUsed = (n - streetStart + 1) / 2;
				int fires = Math.Max(1, rowsUsed - 1);
				for (int g = 0; g < fires; g++)
				{
					float gy = rowsUsed <= 1 ? FirstRowY + RowPitch * 0.5f : FirstRowY - (g + 0.5f) * RowPitch;
					PlaceStreetFire(lane, gy, inner, outer, streetCulture, fireIndex++);
				}
			}
		}
	}

	/// <summary>A fire in the lane between two tent rows: something cooking, a bench on each side, bedrolls by the tents.</summary>
	private static readonly float[] FireRingAngles = { 45f, 90f, 135f, 225f, 270f, 315f };

	private void PlaceStreetFire(float lane, float gy, float inner, float outer, string culture, int index)
	{
		Vec2 fire = new Vec2(lane, gy);
		Props.Add(new Prop { Kind = PropKind.Hearth, Position = fire, Yaw = 0.9f * index, Variant = index });
		Props.Add(new Prop { Kind = PropKind.Fire, Position = fire });
		// Bowls and cups on the ground in front of each bench, inside the 1.6 m seat ring (walk-through).
		Props.Add(new Prop { Kind = PropKind.Dishes, Position = fire + new Vec2(1.4f, 0.2f), Yaw = 0.5f * index, Variant = index * 2 });
		Props.Add(new Prop { Kind = PropKind.Dishes, Position = fire + new Vec2(-1.4f, -0.2f), Yaw = -0.5f * index, Variant = index * 2 + 1 });
		Props.Add(new Prop { Kind = index % 2 == 0 ? PropKind.Spit : PropKind.Cauldron, CultureId = culture, Position = fire, Variant = index });
		for (int b = -1; b <= 1; b += 2)
		{
			float bx = lane + b * 2.6f;
			Props.Add(new Prop { Kind = PropKind.Bench, CultureId = culture, Position = new Vec2(bx, gy), Yaw = MathF.PI * 0.5f, Variant = index * 2 + (b + 1) / 2 });
			for (int seat = -1; seat <= 1; seat += 2)
			{
				BenchSlots.Add(new Slot { Kind = SlotKind.Bench, Position = new Vec2(bx - b * 0.3f, gy + seat * 0.55f), Facing = new Vec2(-b, 0f), Work = WorkKind.Sit });
			}
		}
		// A ring of ground seats 1.6 m out, every 45 degrees except toward the two benches (0/180):
		// neighbours are 1.2 m apart. Work=Sit is required, or spawn turns the resident into a wanderer.
		for (int k = 0; k < FireRingAngles.Length; k++)
		{
			float a = FireRingAngles[k] * MathF.PI / 180f;
			Vec2 offset = new Vec2(MathF.Cos(a), MathF.Sin(a)) * 1.6f;
			FireSlots.Add(new Slot { Kind = SlotKind.Fire, Position = fire + offset, Facing = (-offset).Normalized(), Work = WorkKind.Sit });
		}
		// Firewood or kit up the lane: 1.4 m beyond the 90-degree ring seat, >=3.1 m from the tent-mouth stations.
		Props.Add(new Prop { Kind = index % 2 == 0 ? PropKind.Woodpile : PropKind.Clutter, CultureId = culture,
			Position = fire + new Vec2(0f, 3f), Yaw = 0.3f * index, Variant = index });		float[] columns = { inner, outer };
		for (int c = 0; c < columns.Length; c++)
		{
			Props.Add(new Prop { Kind = PropKind.Bedroll, Position = new Vec2(columns[c] - 0.6f, gy), Yaw = 0.05f, Variant = index * 4 + c * 2 });
			Props.Add(new Prop { Kind = PropKind.Bedroll, Position = new Vec2(columns[c] + 0.6f, gy), Yaw = -0.05f, Variant = index * 4 + c * 2 + 1 });
		}
	}

	/// <summary>Palisade, gate, corner posts, lookouts, road torches and the parade ground.</summary>
	private void PlaceFrame(string home)
	{
		float g = GateWidth * 0.5f;
		AddWall(home, new Vec2(-HalfX, Front), new Vec2(-g, Front));
		AddWall(home, new Vec2(g, Front), new Vec2(HalfX, Front));
		AddWall(home, new Vec2(-HalfX, Back), new Vec2(HalfX, Back));
		AddWall(home, new Vec2(-HalfX, Back), new Vec2(-HalfX, Front));
		AddWall(home, new Vec2(HalfX, Back), new Vec2(HalfX, Front));
		for (int side = -1; side <= 1; side += 2)
		{
			float sx = side;
			Props.Add(new Prop { Kind = PropKind.Torch, CultureId = home, Position = new Vec2(sx * (g + 0.7f), Front - 0.8f) });
			Props.Add(new Prop { Kind = PropKind.Standard, CultureId = home, Position = new Vec2(sx * (g + 1.9f), Front - 1.2f) });
			SentrySlots.Add(new Slot { Kind = SlotKind.Sentry, Position = new Vec2(sx * (g + 1.6f), Front - 2.6f), Facing = new Vec2(0f, 1f) });
			Props.Add(new Prop { Kind = PropKind.Torch, CultureId = home, Position = new Vec2(sx * (HalfX - 2f), Front - 2f) });
			SentrySlots.Add(new Slot { Kind = SlotKind.Sentry, Position = new Vec2(sx * (HalfX - 3f), Front - 3f), Facing = new Vec2(sx, 1f).Normalized() });
			Props.Add(new Prop { Kind = PropKind.Torch, CultureId = home, Position = new Vec2(sx * (HalfX - 2f), Back + 2f) });
			SentrySlots.Add(new Slot { Kind = SlotKind.Sentry, Position = new Vec2(sx * (HalfX - 3f), Back + 3f), Facing = new Vec2(sx, -1f).Normalized() });
			// Lookouts leaning on the palisade where the flank zones meet, and on the back wall.
			WorkSlots.Add(new Slot { Kind = SlotKind.Work, Position = new Vec2(sx * (HalfX - 0.9f), MiddleZoneTop + 0.8f), Facing = new Vec2(sx, 0f), Work = WorkKind.Lean });
			WorkSlots.Add(new Slot { Kind = SlotKind.Work, Position = new Vec2(sx * (HalfX - 0.9f), MiddleZoneBottom - 1f), Facing = new Vec2(sx, 0f), Work = WorkKind.Lean });
			WorkSlots.Add(new Slot { Kind = SlotKind.Work, Position = new Vec2(sx * (_zoneInner - 2f), Back + 0.9f), Facing = new Vec2(0f, -1f), Work = WorkKind.Lean });
			Props.Add(new Prop { Kind = PropKind.Torch, CultureId = home, Position = new Vec2(sx * 11f, 10.5f) });
			Props.Add(new Prop { Kind = PropKind.Torch, CultureId = home, Position = new Vec2(sx * 4.6f, FirstRowY + 3.2f) });
		}
		CompanionSlots["scout"] = new Slot { Kind = SlotKind.Companion, Position = new Vec2(0f, Front - 1.5f), Facing = new Vec2(0f, 1f) };
		ParadeCenter = new Vec2(0f, Front - 3.5f);
		ParadeHalfWidth = _zoneInner - 1.5f;
	}

	private void AddWall(string culture, Vec2 from, Vec2 to)
	{
		Props.Add(new Prop { Kind = PropKind.Wall, CultureId = culture, Position = from, End = to });
	}
}
