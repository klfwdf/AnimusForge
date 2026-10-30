using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace AnimusForge;

/// <summary>
/// Instantiates camp props once per camp mission. Every prop kind has an ordered
/// candidate list (names verified in the 1.4.8 Native prefabs; 1.3 not live-tested);
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
	private static readonly string[] Supplies = { "bd_sack_heap_a", "bd_barrel_heap_a", "crate_a", "bd_cart_a", "crate_b", "bd_barrel_a" };
	private static readonly string[] Fences = { "fence_old_a1", "fence_a" };
	private static readonly string[] Beds = { "bd_bed_khuzait_lord_d", "_to_carry_bed_convolute_a" };
	private static readonly string[] Racks = { "weapon_rack_a" };

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
