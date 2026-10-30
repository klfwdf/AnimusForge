using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace AnimusForge;

/// <summary>
/// Instantiates camp props once per camp mission. Every prop kind has an ordered
/// candidate list, optionally per culture (names verified in the installed Native prefab
/// XML; 1.3 not live-tested). The first candidate that instantiates is cached for the
/// rest of the mission and a pool whose candidates all fail is skipped and logged once.
/// Tents, palisade, carts and horse barriers get physics so nobody walks through them;
/// small props stay physics-free so they never trap agents.
/// Cost: one engine instantiate per prop (about 250-450 for a full camp), once.
/// </summary>
internal static class FieldCampProps
{
	private const string LogSource = "FieldCamp";
	private const float FencePiece = 2.9f;

	/// <summary>Measured fence piece length and long axis per prefab, cached for the mission.</summary>
	private struct PieceSize
	{
		internal float Length;
		internal bool AlongY;
	}

	private static readonly Dictionary<FieldCampLayout.PropKind, string[]> Neutral = new Dictionary<FieldCampLayout.PropKind, string[]>
	{
		[FieldCampLayout.PropKind.Tent] = new[] { "tent_vlandia_a", "tent_vlandia_b", "village_tent_c" },
		[FieldCampLayout.PropKind.Fire] = new[] { "burning_campfire", "campfire" },
		[FieldCampLayout.PropKind.Flag] = new[] { "flag_pole_big", "flagpole_a" },
		[FieldCampLayout.PropKind.Standard] = new[] { "flagpole_a", "flag_pole_big" },
		[FieldCampLayout.PropKind.Supply] = new[] { "bd_sack_heap_a", "bd_barrel_heap_a", "crate_a", "bd_sack_heap_b", "crate_b", "bd_barrel_a", "crate_c", "bd_barrel_heap_b", "bd_sack_a" },
		[FieldCampLayout.PropKind.Wall] = new[] { "fence_old_a1", "fence_old_a2", "fence_old_b1", "fence_a" },
		[FieldCampLayout.PropKind.Bed] = new[] { "bed_straw_a", "bed_straw_b", "bed_straw_c", "bed_straw_d" },
		[FieldCampLayout.PropKind.Rack] = new[] { "weapon_rack_a", "weapon_rack_vlandia_a", "weapon_rack_sturgia_a" },
		[FieldCampLayout.PropKind.WeaponHeap] = new[] { "weapon_heap_sword_a", "weapon_heap_axe_a", "weapon_heap_bow_a", "weapon_heap_crossbow_a" },
		[FieldCampLayout.PropKind.Bedroll] = new[] { "bed_ground_a", "bed_ground_b", "bed_ground_c", "bed_ground_d", "bed_ground_e" },
		[FieldCampLayout.PropKind.Spit] = new[] { "bd_roasting_spit_a", "bd_roasting_spit_b" },
		[FieldCampLayout.PropKind.Cauldron] = new[] { "bd_cauldron_sturgia", "foods_cauldron_stew", "cooking_pot" },
		[FieldCampLayout.PropKind.Bench] = new[] { "log_bench_default_a_unsittable", "log_bench_pine_a_unsittable", "log_bench_beech_a_unsittable" },
		[FieldCampLayout.PropKind.Woodpile] = new[] { "bd_wood_heap_a", "bd_stacked_wood", "bd_log_and_axe", "bd_wood_heap_b" },
		[FieldCampLayout.PropKind.Clutter] = new[] { "crate_a", "bd_barrel_a", "bd_sack_a", "bd_bucket_a", "bd_chest_a", "foods_basket_a", "bd_basket_heap_a" },
		[FieldCampLayout.PropKind.Cart] = new[] { "bd_cart_a", "bd_cart_b", "bd_cart_heap_a" },
		[FieldCampLayout.PropKind.Chest] = new[] { "bd_chest_a", "chest_empire_a" },
		[FieldCampLayout.PropKind.Torch] = new[] { "torch_outdoors_a_burning", "torch_long_a" },
		[FieldCampLayout.PropKind.Target] = new[] { "archery_practice_target", "bd_target_board" },
		[FieldCampLayout.PropKind.TrainingRack] = new[] { "weapon_rack_a", "weapon_heap_sword_a" },
		[FieldCampLayout.PropKind.HorseBarrier] = new[] { "horse_barrier_a" },
		[FieldCampLayout.PropKind.Trough] = new[] { "bd_trough_a" },
		[FieldCampLayout.PropKind.HayCart] = new[] { "bd_hay_cart_a", "bd_cart_a" },
		[FieldCampLayout.PropKind.Table] = new[] { "bd_table_kitchen_a", "bd_table_a", "bd_table_trunk_a" },
		[FieldCampLayout.PropKind.Anvil] = new[] { "bd_anvil", "bd_smithy_anvil" }
	};

	/// <summary>Culture overrides; any kind not listed falls back to <see cref="Neutral"/>.</summary>
	private static readonly Dictionary<string, Dictionary<FieldCampLayout.PropKind, string[]>> ByCulture = new Dictionary<string, Dictionary<FieldCampLayout.PropKind, string[]>>(StringComparer.Ordinal)
	{
		["khuzait"] = new Dictionary<FieldCampLayout.PropKind, string[]>
		{
			[FieldCampLayout.PropKind.Tent] = new[] { "khuzait_tent_a", "khuzait_tent_b" },
			[FieldCampLayout.PropKind.Torch] = new[] { "bd_torch_khuzait_lord_a", "torch_outdoors_a_burning" },
			[FieldCampLayout.PropKind.Table] = new[] { "bd_table_khuzait_lord_a", "bd_table_trunk_a" }
		},
		["aserai"] = new Dictionary<FieldCampLayout.PropKind, string[]>
		{
			[FieldCampLayout.PropKind.Tent] = new[] { "tent_aserai_a", "tent_aserai_b", "tent_aserai_c", "tent_aserai_d" },
			[FieldCampLayout.PropKind.Wall] = new[] { "fence_adobe_a", "fence_adobe_b", "fence_old_a1" },
			[FieldCampLayout.PropKind.Rack] = new[] { "weapon_rack_aserai_a", "weapon_rack_a" },
			[FieldCampLayout.PropKind.Clutter] = new[] { "bd_basket_aserai_a", "bd_basket_aserai_b", "bd_basket_heap_aserai_a", "bd_barrel_a", "bd_sack_b", "bd_aserai_pot_cupper_a" },
			[FieldCampLayout.PropKind.Table] = new[] { "bd_table_aserai_tavern_a", "bd_table_kitchen_a" }
		},
		["battania"] = new Dictionary<FieldCampLayout.PropKind, string[]>
		{
			[FieldCampLayout.PropKind.Tent] = new[] { "tents_pict_a", "tents_pict_b", "tents_pict_c", "tents_pict_d" },
			[FieldCampLayout.PropKind.Wall] = new[] { "barn_fence_a", "barn_fence_b", "fence_old_a1" },
			[FieldCampLayout.PropKind.Cauldron] = new[] { "bd_battania_pot_on_trunk_a", "bd_cauldron_sturgia" },
			[FieldCampLayout.PropKind.Woodpile] = new[] { "bd_wood_heap_b", "bd_wood_heap_c", "bd_stacked_wood" }
		},
		["sturgia"] = new Dictionary<FieldCampLayout.PropKind, string[]>
		{
			[FieldCampLayout.PropKind.Tent] = new[] { "sturgia_village_tent_a", "sturgia_village_tent_b" },
			[FieldCampLayout.PropKind.Rack] = new[] { "weapon_rack_sturgia_a", "weapon_rack_a" },
			[FieldCampLayout.PropKind.Bench] = new[] { "bd_sturgia_bench_a_unsittable", "log_bench_pine_a_unsittable" },
			[FieldCampLayout.PropKind.Chest] = new[] { "bd_chest_sturgia_a", "bd_chest_a" },
			[FieldCampLayout.PropKind.Cauldron] = new[] { "bd_cauldron_sturgia", "bd_sturgia_pot_a" }
		},
		["vlandia"] = new Dictionary<FieldCampLayout.PropKind, string[]>
		{
			[FieldCampLayout.PropKind.Tent] = new[] { "tent_vlandia_a", "tent_vlandia_b", "tent_vlandia_c" },
			[FieldCampLayout.PropKind.Rack] = new[] { "weapon_rack_vlandia_a", "weapon_rack_a" },
			[FieldCampLayout.PropKind.Bench] = new[] { "vlandia_bench_a_unsittable", "log_bench_default_a_unsittable" }
		},
		["empire"] = new Dictionary<FieldCampLayout.PropKind, string[]>
		{
			[FieldCampLayout.PropKind.Tent] = new[] { "tent_vlandia_a", "tent_vlandia_b", "village_tent_c" },
			[FieldCampLayout.PropKind.Wall] = new[] { "fence_empire_a", "fence_empire_b", "fence_old_a1" },
			[FieldCampLayout.PropKind.Chest] = new[] { "chest_empire_a", "bd_chest_a" }
		}
	};

	private static readonly HashSet<FieldCampLayout.PropKind> Solid = new HashSet<FieldCampLayout.PropKind>
	{
		FieldCampLayout.PropKind.Tent,
		FieldCampLayout.PropKind.Wall,
		FieldCampLayout.PropKind.Cart,
		FieldCampLayout.PropKind.HayCart,
		FieldCampLayout.PropKind.HorseBarrier
	};

	/// <param name="scale">Camp-local distances are multiplied by this to fit small scenes; prop sizes are unchanged.</param>
	/// <returns>Number of entities created.</returns>
	internal static int Place(Scene scene, FieldCampLayout layout, MatrixFrame campFrame, float scale)
	{
		var resolved = new Dictionary<string, string>(StringComparer.Ordinal);
		var failed = new HashSet<string>(StringComparer.Ordinal);
		var sizes = new Dictionary<string, PieceSize>(StringComparer.Ordinal);
		int placed = 0;
		for (int i = 0; i < layout.Props.Count; i++)
		{
			FieldCampLayout.Prop prop = layout.Props[i];
			string[] pool = PoolFor(prop, out string poolKey);
			if (failed.Contains(poolKey))
			{
				continue;
			}
			if (prop.Kind == FieldCampLayout.PropKind.Wall)
			{
				placed += PlaceWall(scene, prop, pool, poolKey, campFrame, scale, resolved, failed, sizes);
				continue;
			}
			MatrixFrame frame = FrameFor(scene, campFrame, prop.Position * scale, prop.Yaw, prop.Scale);
			if (Spawn(scene, pool, poolKey, prop.Variant, frame, Solid.Contains(prop.Kind), resolved, failed) != null)
			{
				placed++;
			}
		}
		return placed;
	}

	/// <summary>
	/// Fence pieces end to end along the run. The first piece of each prefab is measured
	/// (local bounding box) so the spacing and the long axis match the real model.
	/// </summary>
	private static int PlaceWall(Scene scene, FieldCampLayout.Prop prop, string[] pool, string poolKey, MatrixFrame campFrame, float scale, Dictionary<string, string> resolved, HashSet<string> failed, Dictionary<string, PieceSize> sizes)
	{
		Vec2 from = prop.Position * scale;
		Vec2 to = prop.End * scale;
		Vec2 run = to - from;
		float length = run.Length;
		if (length < 0.5f)
		{
			return 0;
		}
		Vec2 dir = run * (1f / length);
		GameEntity first = Spawn(scene, pool, poolKey, 0, FrameFor(scene, campFrame, from, 0f, 0f), true, resolved, failed);
		if (first == null || !resolved.TryGetValue(poolKey, out string prefab))
		{
			return 0;
		}
		if (!sizes.TryGetValue(prefab, out PieceSize size))
		{
			size = Measure(first);
			sizes[prefab] = size;
			Logger.Log(LogSource, "fence piece " + prefab + " length=" + size.Length.ToString("0.00") + " axis=" + (size.AlongY ? "y" : "x"));
		}
		// Yaw turns local +y onto the run; a piece whose long side is local x needs another -90 degrees.
		float yaw = dir.RotationInRadians - (size.AlongY ? 0f : MathF.PI * 0.5f);
		int pieces = Math.Max(1, (int)Math.Ceiling(length / size.Length - 0.15f));
		int placed = 0;
		for (int i = 0; i < pieces; i++)
		{
			MatrixFrame frame = FrameFor(scene, campFrame, from + dir * (length * (i + 0.5f) / pieces), yaw, 0f);
			GameEntity entity = i == 0 ? first : TrySpawn(scene, prefab, frame, true);
			if (entity == null)
			{
				continue;
			}
			if (i == 0)
			{
				entity.SetGlobalFrame(in frame);
			}
			placed++;
		}
		return placed;
	}

	private static PieceSize Measure(GameEntity entity)
	{
		try
		{
			BoundingBox box = entity.GetLocalBoundingBox();
			float x = box.max.x - box.min.x;
			float y = box.max.y - box.min.y;
			float longest = Math.Max(x, y);
			if (longest > 0.5f && longest < 20f)
			{
				return new PieceSize { Length = longest * 0.97f, AlongY = y > x };
			}
		}
		catch
		{
		}
		return new PieceSize { Length = FencePiece, AlongY = false };
	}

	private static MatrixFrame FrameFor(Scene scene, MatrixFrame campFrame, Vec2 local, float yaw, float uniformScale)
	{
		Vec3 world = campFrame.TransformToParent(new Vec3(local.x, local.y, 0f));
		world.z = FieldCampMissionLogic.GroundHeight(scene, world);
		MatrixFrame frame = MatrixFrame.Identity;
		frame.rotation = campFrame.rotation;
		frame.rotation.RotateAboutUp(yaw);
		if (uniformScale > 0f && Math.Abs(uniformScale - 1f) > 0.01f)
		{
			frame.rotation.ApplyScaleLocal(uniformScale);
		}
		frame.origin = world;
		return frame;
	}

	private static GameEntity Spawn(Scene scene, string[] pool, string poolKey, int variant, MatrixFrame frame, bool physics, Dictionary<string, string> resolved, HashSet<string> failed)
	{
		GameEntity entity = null;
		if (resolved.TryGetValue(poolKey, out string known))
		{
			entity = TrySpawn(scene, VariantOf(pool, known, variant), frame, physics) ?? TrySpawn(scene, known, frame, physics);
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
		}
		if (entity == null && !resolved.ContainsKey(poolKey))
		{
			failed.Add(poolKey);
			Logger.Log(LogSource, "no prefab available for " + poolKey + "; skipped");
		}
		return entity;
	}

	private static string[] PoolFor(FieldCampLayout.Prop prop, out string poolKey)
	{
		string culture = prop.CultureId ?? "";
		if (ByCulture.TryGetValue(culture, out Dictionary<FieldCampLayout.PropKind, string[]> pools) && pools.TryGetValue(prop.Kind, out string[] pool))
		{
			poolKey = prop.Kind + ":" + culture;
			return pool;
		}
		poolKey = prop.Kind.ToString();
		return Neutral.TryGetValue(prop.Kind, out pool) ? pool : Neutral[FieldCampLayout.PropKind.Supply];
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
