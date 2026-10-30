using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace AnimusForge;

/// <summary>
/// Instantiates camp props once per camp mission. Every prop kind has an ordered
/// candidate list (names verified in the installed Native prefab XML; 1.3 not live-tested);
/// the first candidate that instantiates is cached for the rest of the mission and a
/// kind whose candidates all fail is skipped and logged once.
/// Tents and fences get physics so the player cannot walk through them; small
/// props stay physics-free so they never trap agents.
/// </summary>
internal static class FieldCampProps
{
	private const string LogSource = "FieldCamp";

	private static readonly string[] NeutralTents = { "tent_vlandia_a", "tent_vlandia_b", "village_tent_c" };
	private static readonly Dictionary<string, string[]> CultureTents = new Dictionary<string, string[]>(StringComparer.Ordinal)
	{
		["khuzait"] = new[] { "khuzait_tent_a", "khuzait_tent_b" },
		["aserai"] = new[] { "tent_aserai_a", "tent_aserai_b", "tent_aserai_c", "tent_aserai_d" },
		["battania"] = new[] { "tents_pict_a", "tents_pict_b", "tents_pict_c", "battania_street_tent_a" },
		["sturgia"] = new[] { "sturgia_village_tent_a", "sturgia_village_tent_b" },
		["vlandia"] = new[] { "tent_vlandia_a", "tent_vlandia_b", "tent_vlandia_c" }
	};

	private static readonly string[] Fires = { "burning_campfire", "campfire", "fire_stones_bonfire" };
	private static readonly string[] SmallFires = { "campfire", "burning_campfire" };
	private static readonly string[] Flags = { "flag_pole_big", "flagpole_a" };
	private static readonly string[] Supplies = { "bd_sack_heap_a", "bd_barrel_heap_a", "crate_a", "bd_sack_heap_b", "crate_b", "bd_barrel_a", "crate_c", "bd_barrel_heap_b", "bd_sack_a" };
	private static readonly string[] Fences = { "fence_old_a1", "fence_old_b1", "fence_a" };
	private static readonly string[] Beds = { "bed_straw_a", "bed_straw_b", "bed_straw_c", "bd_bed_khuzait_lord_d" };
	private static readonly string[] Racks = { "weapon_rack_a", "weapon_rack_vlandia_a", "weapon_rack_sturgia_a", "weapon_heap_sword_a", "weapon_heap_axe_a" };
	private static readonly string[] Bedrolls = { "bed_ground_a", "bed_ground_b", "bed_ground_c", "bed_ground_d", "bed_ground_e" };
	private static readonly string[] Spits = { "bd_roasting_spit_a", "bd_roasting_spit_b" };
	private static readonly string[] Cauldrons = { "bd_cauldron_sturgia", "foods_cauldron_stew", "cooking_pot" };
	private static readonly string[] Benches = { "log_bench_default_a_unsittable", "log_bench_pine_a_unsittable", "log_bench_beech_a_unsittable" };
	private static readonly string[] Woodpiles = { "bd_wood_heap_a", "bd_stacked_wood", "bd_log_and_axe" };
	private static readonly string[] Clutter = { "crate_a", "bd_barrel_a", "bd_sack_a", "bd_bucket_a", "bd_chest_a", "weapon_heap_bow_a" };
	private static readonly string[] Carts = { "bd_cart_a", "bd_cart_b", "bd_cart_heap_a" };
	private static readonly string[] Chests = { "bd_chest_a", "chest_empire_a" };
	private static readonly string[] Torches = { "torch_outdoors_a_burning", "torch_long_a" };
	private static readonly string[] Targets = { "archery_practice_target", "bd_target_board" };
	private static readonly string[] TrainingRacks = { "weapon_rack_a", "weapon_heap_sword_a" };
	private static readonly string[] HorseBarriers = { "horse_barrier_a" };
	private static readonly string[] Troughs = { "bd_trough_a" };
	private static readonly string[] HayCarts = { "bd_hay_cart_a", "bd_cart_a" };
	private static readonly string[] Standards = { "flagpole_a", "flag_pole_big" };

	/// <param name="scale">Camp-local distances are multiplied by this to fit small scenes; prop sizes are unchanged.</param>
	internal static int Place(Scene scene, FieldCampLayout layout, MatrixFrame campFrame, float scale)
	{
		var resolved = new Dictionary<string, string>(StringComparer.Ordinal);
		var failed = new HashSet<string>(StringComparer.Ordinal);
		int placed = 0;
		for (int i = 0; i < layout.Props.Count; i++)
		{
			FieldCampLayout.Prop prop = layout.Props[i];
			string[] pool = PoolFor(prop, out bool physics);
			string poolKey = prop.Kind + ":" + (prop.Kind == FieldCampLayout.PropKind.Tent ? prop.CultureId : (prop.Variant > 0 && prop.Kind == FieldCampLayout.PropKind.Fire ? "small" : ""));
			if (failed.Contains(poolKey))
			{
				continue;
			}
			Vec3 local = new Vec3(prop.Position.x * scale, prop.Position.y * scale, 0f);
			Vec3 world = campFrame.TransformToParent(in local);
			world.z = FieldCampMissionLogic.GroundHeight(scene, world);
			MatrixFrame frame = MatrixFrame.Identity;
			frame.rotation = campFrame.rotation;
			frame.rotation.RotateAboutUp(prop.Yaw);
			frame.origin = world;

			GameEntity entity = null;
			if (resolved.TryGetValue(poolKey, out string known))
			{
				entity = TrySpawn(scene, VariantOf(pool, known, prop.Variant), frame, physics) ?? TrySpawn(scene, known, frame, physics);
			}
			else
			{
				for (int c = 0; c < pool.Length && entity == null; c++)
				{
					entity = TrySpawn(scene, pool[c], frame, physics);
					if (entity != null)
					{
						resolved[poolKey] = pool[c];
					}
				}
				if (entity == null && prop.Kind == FieldCampLayout.PropKind.Tent && !ReferenceEquals(pool, NeutralTents))
				{
					for (int c = 0; c < NeutralTents.Length && entity == null; c++)
					{
						entity = TrySpawn(scene, NeutralTents[c], frame, physics);
						if (entity != null)
						{
							resolved[poolKey] = NeutralTents[c];
						}
					}
				}
			}
			if (entity == null)
			{
				failed.Add(poolKey);
				Logger.Log(LogSource, "no prefab available for " + poolKey + "; skipped");
				continue;
			}
			placed++;
		}
		return placed;
	}

	private static string[] PoolFor(FieldCampLayout.Prop prop, out bool physics)
	{
		physics = false;
		switch (prop.Kind)
		{
			case FieldCampLayout.PropKind.Tent:
				physics = true;
				return CultureTents.TryGetValue(prop.CultureId ?? "", out string[] tents) ? tents : NeutralTents;
			case FieldCampLayout.PropKind.Fire:
				return prop.Variant > 0 ? SmallFires : Fires;
			case FieldCampLayout.PropKind.Flag:
				return Flags;
			case FieldCampLayout.PropKind.Supply:
				return Supplies;
			case FieldCampLayout.PropKind.Fence:
				physics = true;
				return Fences;
			case FieldCampLayout.PropKind.Bed:
				return Beds;
			case FieldCampLayout.PropKind.Bedroll:
				return Bedrolls;
			case FieldCampLayout.PropKind.Spit:
				return Spits;
			case FieldCampLayout.PropKind.Cauldron:
				return Cauldrons;
			case FieldCampLayout.PropKind.Bench:
				return Benches;
			case FieldCampLayout.PropKind.Woodpile:
				return Woodpiles;
			case FieldCampLayout.PropKind.Clutter:
				return Clutter;
			case FieldCampLayout.PropKind.Cart:
				physics = true;
				return Carts;
			case FieldCampLayout.PropKind.Chest:
				return Chests;
			case FieldCampLayout.PropKind.Torch:
				return Torches;
			case FieldCampLayout.PropKind.Target:
				return Targets;
			case FieldCampLayout.PropKind.TrainingRack:
				return TrainingRacks;
			case FieldCampLayout.PropKind.HorseBarrier:
				physics = true;
				return HorseBarriers;
			case FieldCampLayout.PropKind.Trough:
				return Troughs;
			case FieldCampLayout.PropKind.HayCart:
				physics = true;
				return HayCarts;
			case FieldCampLayout.PropKind.Standard:
				return Standards;
			default:
				return Racks;
		}
	}

	/// <summary>Rotates through same-pool prefabs that come after the known-good one.</summary>
	private static string VariantOf(string[] pool, string known, int variant)
	{
		int start = Array.IndexOf(pool, known);
		if (start < 0 || pool.Length <= 1)
		{
			return known;
		}
		return pool[start + Math.Abs(variant) % (pool.Length - start)];
	}

	private static GameEntity TrySpawn(Scene scene, string prefab, MatrixFrame frame, bool physics)
	{
		try
		{
			GameEntity entity = GameEntity.Instantiate(scene, prefab, false, physics);
			if (entity == null)
			{
				return null;
			}
			entity.SetGlobalFrame(in frame);
			return entity;
		}
		catch
		{
			return null;
		}
	}
}
