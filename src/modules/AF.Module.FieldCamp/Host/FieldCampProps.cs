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
/// Every prop is created with its final frame, so the engine builds its collision in
/// place (setting the frame after a physics-less spawn left props without collision),
/// and is then made a stationary, enabled body (see <see cref="MakeSolid"/>).
/// Only bedrolls, torches, fires and flat clutter stay walk-through.
/// Height comes from terrain only. Tent furniture shares one grounded doorway frame;
/// bounded initialization probes reject slopes/closed doors and fit the actual furniture.
/// Candidate trials and measurements run only during placement, before the ground cache.
/// </summary>
internal static class FieldCampProps
{
	private const string LogSource = "FieldCamp";
    internal struct FenceObstacle
    {
        internal Vec3 From, To;
        internal float HalfWidth;
    }
    internal static readonly List<GameEntity> ArcheryTargets = new List<GameEntity>();
    internal static readonly List<FenceObstacle> FenceObstacles = new List<FenceObstacle>();
    internal static int PlacedTentCount { get; private set; }
    internal static int RequestedTentCount { get; private set; }
    internal static void ResetPlacementCounts()
    {
        PlacedTentCount = RequestedTentCount = 0;
        FenceObstacles.Clear();
        ArcheryTargets.Clear();
    }
		private const float FencePiece = 2.9f;
		private const BodyFlags ClearanceExcludeFlags = BodyFlags.CommonCollisionExcludeFlagsForAgent & ~BodyFlags.DoNotCollideWithRaycast;

	/// <summary>Measured fence piece length and long axis per prefab, cached for the mission.</summary>
	private struct PieceSize
	{
		internal float Length;
        internal float HalfWidth;
		internal bool AlongY;
        internal Vec3 PivotToGroundCentre;
	}

    // Native cooked-food prefabs, not inventory items or a new gameplay resource. Bounds are
    // cached with the other setup measurements; no per-frame food spawning/updates.
    private static readonly string[] RoastFoods = { "foods_chicken_roast", "foods_Cooked mutton", "foods_Cooked ribs" };

	private static readonly Dictionary<FieldCampLayout.PropKind, string[]> Neutral = new Dictionary<FieldCampLayout.PropKind, string[]>
	{
		[FieldCampLayout.PropKind.Tent] = new[] { "tent_vlandia_a", "tent_vlandia_b", "village_tent_c" },
		[FieldCampLayout.PropKind.Fire] = new[] { "burning_campfire", "campfire" },
		[FieldCampLayout.PropKind.Flag] = new[] { "flag_pole_big", "flagpole_a" },
		[FieldCampLayout.PropKind.Standard] = new[] { "flagpole_a", "flag_pole_big" },
		[FieldCampLayout.PropKind.Supply] = new[] { "bd_sack_heap_a", "bd_barrel_heap_a", "crate_a", "bd_sack_heap_b", "crate_b", "bd_barrel_a", "crate_c", "bd_barrel_heap_b", "bd_sack_a", "bd_sack_heap_c", "bd_barrel_e", "bd_barrel_d", "bd_sack_c", "bd_barrel_f" },
		[FieldCampLayout.PropKind.Wall] = new[] { "fence_old_a1", "fence_old_a2", "fence_old_b1", "fence_a" },
		[FieldCampLayout.PropKind.Bed] = new[] { "bed_straw_a", "bed_straw_b", "bed_straw_c", "bed_straw_d" },
		[FieldCampLayout.PropKind.Rack] = new[] { "weapon_rack_a", "weapon_rack_vlandia_a", "weapon_rack_sturgia_a" },
		[FieldCampLayout.PropKind.WeaponHeap] = new[] { "weapon_heap_sword_a", "weapon_heap_axe_a", "weapon_heap_bow_a", "weapon_heap_crossbow_a" },
		[FieldCampLayout.PropKind.Bedroll] = new[] { "bed_ground_a", "bed_ground_b", "bed_ground_c", "bed_ground_d", "bed_ground_e", "bed_convolute_g", "bed_convolute_e" },
		[FieldCampLayout.PropKind.Spit] = new[] { "bd_roasting_spit_a", "bd_roasting_spit_b" },
		[FieldCampLayout.PropKind.Cauldron] = new[] { "bd_cauldron_sturgia", "foods_cauldron_stew", "cooking_pot" },
		[FieldCampLayout.PropKind.Bench] = new[] { "log_bench_default_a_unsittable", "log_bench_pine_a_unsittable", "log_bench_beech_a_unsittable" },
		[FieldCampLayout.PropKind.Woodpile] = new[] { "bd_wood_heap_a", "bd_stacked_wood", "bd_log_and_axe", "bd_wood_heap_b", "bd_wood_heap_e", "bd_wood_heap_f" },
		[FieldCampLayout.PropKind.Clutter] = new[] { "crate_a", "bd_barrel_a", "bd_sack_a", "bd_bucket_a", "bd_chest_a", "foods_basket_a", "bd_basket_heap_a", "bd_bucket_b", "bd_barrel_c", "bd_empty_sack", "bd_chest_c", "kitchen_cauldron" },
		[FieldCampLayout.PropKind.Cart] = new[] { "bd_cart_a", "bd_cart_b", "bd_cart_heap_a" },
		[FieldCampLayout.PropKind.Chest] = new[] { "bd_chest_a", "chest_empire_a" },
		[FieldCampLayout.PropKind.Torch] = new[] { "torch_outdoors_a_burning", "torch_long_a" },
		// pot_archery_target_breakable is excluded: its DestructableComponent initializes only in OnInit,
		// which camp spawns (callScriptCallbacks=false) never run, so a hit would touch null state.
		[FieldCampLayout.PropKind.Target] = new[] { "archery_practice_target", "bd_target_board" },
		[FieldCampLayout.PropKind.TrainingRack] = new[] { "weapon_rack_a", "weapon_heap_sword_a" },
		[FieldCampLayout.PropKind.HorseBarrier] = new[] { "horse_barrier_a" },
		[FieldCampLayout.PropKind.Trough] = new[] { "bd_trough_a" },
		[FieldCampLayout.PropKind.HayCart] = new[] { "bd_hay_cart_a", "bd_cart_a" },
		[FieldCampLayout.PropKind.Table] = new[] { "bd_table_kitchen_a", "bd_table_a", "bd_table_trunk_a", "bd_table_b", "bd_table_c" },
		[FieldCampLayout.PropKind.Anvil] = new[] { "bd_anvil", "bd_smithy_anvil" },
		[FieldCampLayout.PropKind.ExecutionBlock] = new[] { "bd_log_and_axe", "bd_wood_heap_a" },
		[FieldCampLayout.PropKind.Gallows] = new[] { "gallows" },
		[FieldCampLayout.PropKind.Bag] = new[] { "bd_sack_a", "bd_sack_b" },
		// Native prefabs used by the SandBox hideout scenes (checked 2026-11-04 in Native/Prefabs).
		[FieldCampLayout.PropKind.Hearth] = new[] { "fire_stones_steppe", "fire_stones_battania", "fire_stones_desert" },
		[FieldCampLayout.PropKind.Rug] = new[] { "mat_fur_b", "mat_carpet_h", "mat_carpet_i", "mat_carpet_e" },
		[FieldCampLayout.PropKind.Ash] = new[] { "burned_wood_b", "burned_wood_e" },
		[FieldCampLayout.PropKind.Dishes] = new[] { "kitchen_bowl_c", "kitchen_bowl_stew_b", "kitchen_plate_a", "mead_cup_wooden_a", "kitchen_bowl_b_four", "mead_cup_wooden_c", "kitchen_mug_a" },
		[FieldCampLayout.PropKind.Food] = new[] { "kitchen_cutting_food_bread", "foods_cured_ham_leg", "foods_bread_piece", "kitchen_cutting_food_fish", "foods_carrots_a", "kitchen_chopping_board_a", "foods_cabbage_a", "kitchen_basket_a" },
		[FieldCampLayout.PropKind.Gear] = new[] { "bd_rope_c", "merchandise_hides_b", "bd_cart_wheel_a", "bd_rope_a", "bd_cart_wheel_b", "bd_rope_b" },
		[FieldCampLayout.PropKind.Wreck] = new[] { "bd_cartbroken_a", "bd_cartbroken_b", "bd_cart_heap_b", "bd_cartbroken_c", "bd_cart_heap_c", "bd_cartbroken_d", "bd_cart_heap_d" }
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

	private static readonly HashSet<FieldCampLayout.PropKind> WalkThrough = new HashSet<FieldCampLayout.PropKind>
	{
		FieldCampLayout.PropKind.Bedroll,
		FieldCampLayout.PropKind.Fire,
		FieldCampLayout.PropKind.Torch,
		FieldCampLayout.PropKind.Spit,
		FieldCampLayout.PropKind.Cauldron,
		FieldCampLayout.PropKind.Bed,
		FieldCampLayout.PropKind.Hearth,
		FieldCampLayout.PropKind.Rug,
		FieldCampLayout.PropKind.Ash,
		FieldCampLayout.PropKind.Dishes,
		FieldCampLayout.PropKind.Food
	};

	/// <param name="scale">Must be 1: spacing is never compressed independently of solid prop footprints.</param>
	/// <returns>Number of entities created.</returns>
	internal static int Place(Scene scene, FieldCampLayout layout, MatrixFrame campFrame, float scale, bool allowUnevenGround = false)
	{
        ResetPlacementCounts();
        for (int i = 0; i < layout.Props.Count; i++)
            if (layout.Props[i].Kind == FieldCampLayout.PropKind.Tent) RequestedTentCount++;
		_solidRequested = 0;
		_solidWithBody = 0;
		int placed = PlaceAll(scene, layout, campFrame, scale, allowUnevenGround);
		Logger.Log(LogSource, "solid props with body=" + _solidWithBody + "/" + _solidRequested);
		return placed;
	}

	/// <summary>Main-thread set-up counters for the solid-prop log line (one camp at a time).</summary>
	private static int _solidRequested;
	private static int _solidWithBody;

		private sealed class TentInterior
		{
			internal MatrixFrame Frame;
			internal float HalfWidth;
			internal float HalfDepth;
			internal float RoofHeight;
			internal float DoorDepth;
			internal readonly HashSet<UIntPtr> Entities = new HashSet<UIntPtr>();
			internal readonly List<BoundingBox> Furniture = new List<BoundingBox>();
			internal readonly List<GameEntity> FurnitureEntities = new List<GameEntity>();
		}

		private static int PlaceAll(Scene scene, FieldCampLayout layout, MatrixFrame campFrame, float scale, bool allowUnevenGround)
		{
			if (Math.Abs(scale - 1f) > 0.001f)
			{
				throw new InvalidOperationException("Camp spacing cannot be compressed independently of prop footprints.");
			}
			var resolved = new Dictionary<string, string>(StringComparer.Ordinal);
			var failed = new HashSet<string>(StringComparer.Ordinal);
			var sizes = new Dictionary<string, PieceSize>(StringComparer.Ordinal);
			var tents = new Dictionary<int, TentInterior>();
			var furnitureSizes = new Dictionary<string, BoundingBox>(StringComparer.Ordinal);
			var doorwayTurns = new Dictionary<string, int>(StringComparer.Ordinal);
			var furnishingPlans = new Dictionary<int, List<FieldCampLayout.Prop>>();
			var tableTops = new Dictionary<int, TableTop>();
			for (int i = 0; i < layout.Props.Count; i++)
			{
				FieldCampLayout.Prop item = layout.Props[i];
				if (item.TentAnchor <= 0)
				{
					continue;
				}
				if (!furnishingPlans.TryGetValue(item.TentAnchor, out List<FieldCampLayout.Prop> plan))
				{
					plan = new List<FieldCampLayout.Prop>(4);
					furnishingPlans.Add(item.TentAnchor, plan);
				}
				plan.Add(item);
			}
			int placed = 0;
            // Standalone props first: every tent trial sees the final nearby physical clutter.
            for (int pass = 0; pass < 2; pass++)
			for (int i = 0; i < layout.Props.Count; i++)
			{
				FieldCampLayout.Prop prop = layout.Props[i];
                if ((prop.Kind == FieldCampLayout.PropKind.Tent) != (pass == 1)) continue;
                if (prop.Kind == FieldCampLayout.PropKind.ExecutionBlock || prop.Kind == FieldCampLayout.PropKind.Gallows) continue;
				string[] pool = PoolFor(prop, out string poolKey);
				if (prop.TentAnchor > 0)
				{
					continue; // committed transactionally with its tent, including candidate fallback
				}
				if (failed.Contains(poolKey))
				{
					continue;
				}
				if (prop.Kind == FieldCampLayout.PropKind.Wall)
				{
					placed += PlaceWall(scene, prop, pool, poolKey, campFrame, scale, resolved, failed, sizes);
					continue;
				}
				if (prop.Kind == FieldCampLayout.PropKind.Tent)
				{
					furnishingPlans.TryGetValue(i + 1, out List<FieldCampLayout.Prop> plan);
					if (!PlaceTent(scene, prop, pool, campFrame, plan, furnitureSizes, doorwayTurns, tents.Values, allowUnevenGround, out TentInterior tentInterior))
					{
						Logger.Log(LogSource, "[WARN] tent rejected after real size/ground/door trials pool=" + poolKey + " slot=" + i);
                        continue;
					}
					tents.Add(i + 1, tentInterior);
                    PlacedTentCount++;
					placed += 1 + tentInterior.FurnitureEntities.Count;
					continue;
				}
				MatrixFrame frame = FrameFor(scene, campFrame, prop.Position * scale, prop.Yaw, prop.Scale);
				if (prop.StackOn > 0)
				{
					// Rests on the measured top of an earlier table, from its mesh centre (artist pivots are
					// not centred) along its measured long side, clamped inside the rim; skipped when that
					// table failed or measured as no table.
					if (!tableTops.TryGetValue(prop.StackOn, out TableTop top)) continue;
					FieldCampLayout.Prop table = layout.Props[prop.StackOn - 1];
					Vec2 layoutOffset = prop.Position - table.Position;
					// Signed distance along the table's yaw axis, as written by FieldCampLayout.SetTable.
					float offset = (layoutOffset.x * MathF.Cos(table.Yaw) + layoutOffset.y * MathF.Sin(table.Yaw)) * scale;
					offset = Math.Max(-top.Reach, Math.Min(top.Reach, offset));
					frame.origin = top.Centre + top.Along * offset;
				}
				GameEntity entity = Spawn(scene, pool, poolKey, prop.Variant, frame, !WalkThrough.Contains(prop.Kind), resolved, failed);
				if (entity != null)
				{
                    placed++;
                    if (prop.Kind == FieldCampLayout.PropKind.Target)
                    {
                        GameEntity aim = entity;
                        for (int child = 0; child < entity.ChildCount; child++)
                        {
                            GameEntity part = entity.GetChild(child);
                            if (part.Name == "bullseye") { aim = part; break; }
                        }
                        ArcheryTargets.Add(aim);
                    }
                    if (prop.Kind == FieldCampLayout.PropKind.Table) RecordTableTop(entity, frame, i + 1, tableTops);
                    if (prop.Kind == FieldCampLayout.PropKind.Spit && PlaceRoastFood(scene, entity, frame, prop.Variant, furnitureSizes)) placed++;
				}
			}
			return placed;
		}

        // Called only while choosing a site. Measurements (including unavailable
        // prefabs) are cached across candidates; no props survive these size probes.
        internal static bool CanSupportTents(Scene scene, FieldCampLayout layout,
            MatrixFrame campFrame, Dictionary<string, BoundingBox> measured)
        {
            string[] fallback = Neutral[FieldCampLayout.PropKind.Tent];
            for (int i = 0; i < layout.Props.Count; i++)
            {
                FieldCampLayout.Prop prop = layout.Props[i];
                if (prop.Kind != FieldCampLayout.PropKind.Tent) continue;
                string[] pool = PoolFor(prop, out _);
                bool supported = false;
                for (int c = 0; c < pool.Length + fallback.Length && !supported; c++)
                {
                    string prefab = c < pool.Length ? pool[c] : fallback[c - pool.Length];
                    if (!TryMeasureTent(scene, prefab, measured, out BoundingBox box)) continue;
                    float rawX = (box.max.x - box.min.x) * 0.5f;
                    float rawY = (box.max.y - box.min.y) * 0.5f;
                    // Opposite turns have identical support footprints.
                    for (int turn = 0; turn < 2 && !supported; turn++)
                    {
                        float width = turn == 0 ? rawX : rawY;
                        float depth = turn == 0 ? rawY : rawX;
                        bool large = prop.Variant >= 98;
                        if (!FieldCampPlacementRules.TryTentScale(width, depth,
                            prop.Scale > 0f ? prop.Scale : 1f, large ? 7f : 3f,
                            large ? 7f : 4f, out float fittedScale)) continue;
                        MatrixFrame door = FrameFor(scene, campFrame, prop.Position, prop.Yaw, 1f);
                        supported = TryFitTentGround(scene, ref door, width * fittedScale, depth * fittedScale);
                    }
                }
                if (!supported) return false;
            }
            return true;
        }

        private static bool TryMeasureTent(Scene scene, string prefab,
            Dictionary<string, BoundingBox> measured, out BoundingBox box)
        {
            if (!measured.TryGetValue(prefab, out box))
            {
                GameEntity probe = null;
                try
                {
                    if (GameEntity.PrefabExists(prefab))
                    {
                        probe = GameEntity.Instantiate(scene, prefab, false, false);
                        if (probe != null) box = probe.GetLocalBoundingBox();
                    }
                }
                catch (Exception ex)
                {
                    box = default(BoundingBox);
                    Logger.Log(LogSource, "[WARN] tent measurement failed prefab=" + prefab + " error=" + ex.Message);
                }
                finally
                {
                    probe?.Remove(80);
                    measured[prefab] = box;
                }
            }
            return box.min.IsValid && box.max.IsValid && box.max.z > box.min.z;
        }

		/// <summary>
		/// Four bounded orientation trials per available prefab, at initialization only. Bounding boxes
		/// are measured from the actual candidate, never inferred from a culture's name. A closed
		/// candidate is rejected, not made walk-through. The doorway-local frame is shared by all furniture.
		/// </summary>
        private static bool PlaceTent(Scene scene, FieldCampLayout.Prop prop, string[] pool, MatrixFrame campFrame,
            List<FieldCampLayout.Prop> furnishingPlan, Dictionary<string, BoundingBox> measured,
            Dictionary<string, int> doorwayTurns, IEnumerable<TentInterior> acceptedTents, bool allowUnevenGround, out TentInterior interior)
        {
            interior = null;
            string[] fallback = Neutral[FieldCampLayout.PropKind.Tent];
            // Prefer every normal prefab/turn before trying a rough-ground fit.
            for (int groundPass = 0; groundPass < (allowUnevenGround ? 2 : 1); groundPass++)
            for (int c = 0; c < pool.Length + fallback.Length; c++)
            {
                string prefab = c < pool.Length ? pool[c] : fallback[c-pool.Length];
                if (!TryMeasureTent(scene, prefab, measured, out BoundingBox box)) continue;
                doorwayTurns.TryGetValue(prefab, out int firstTurn);
                for (int attempt = 0; attempt < 4; attempt++)
                {
                    int turn = (firstTurn + attempt) % 4;
                    float rawX = (box.max.x-box.min.x)*0.5f, rawY = (box.max.y-box.min.y)*0.5f;
                    float width = turn % 2 == 0 ? rawX : rawY, depth = turn % 2 == 0 ? rawY : rawX;
                    bool large = prop.Variant >= 98;
                    if (!FieldCampPlacementRules.TryTentScale(width, depth, prop.Scale > 0f ? prop.Scale : 1f,
                        large ? 7f : 3f, large ? 7f : 4f, out float fittedScale)) continue;
                    width *= fittedScale; depth *= fittedScale;
                    MatrixFrame door;
                    try
                    {
                        door = FrameFor(scene, campFrame, prop.Position, prop.Yaw, 1f);
                        if (!TryFitTentGround(scene, ref door, width, depth, groundPass > 0)) continue;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogSource, "[WARN] tent support trial failed prefab=" + prefab + " error=" + ex.Message);
                        continue;
                    }
                    MatrixFrame frame = door;
                    frame.rotation.RotateAboutUp(-turn * MathF.PI * 0.5f);
                    frame.rotation.ApplyScaleLocal(fittedScale);
                    frame.origin = door.origin - frame.rotation.TransformToParent(new Vec3(
                        (box.min.x+box.max.x)*0.5f, (box.min.y+box.max.y)*0.5f, box.min.z));
                    GameEntity tent = TrySpawn(scene, prefab, frame, true);
                    if (tent == null) continue;
                    bool accepted = false;
                    TentInterior trial = null;
                    try
                    {
                        if (!DoorIsClear(scene, door, depth)) continue;
                        trial = new TentInterior { Frame=door, HalfWidth=width*0.68f, HalfDepth=depth*0.68f,
                            RoofHeight=(box.max.z-box.min.z)*fittedScale, DoorDepth=depth };
                        CaptureTentEntities(tent, trial.Entities);
                        int skipped = 0;
                        for (int f=0; furnishingPlan != null && f<furnishingPlan.Count; f++)
                        {
                            var furnishing=furnishingPlan[f];
                            if (!PlaceTentFurniture(scene, furnishing, PoolFor(furnishing, out _), trial, measured)) skipped++;
                        }
                        if (!DoorIsClear(scene, door, depth))
                        {
                            for (int f=0; f<trial.FurnitureEntities.Count; f++) trial.FurnitureEntities[f].Remove(80);
                            trial.FurnitureEntities.Clear();
                            trial.Furniture.Clear();
                            if (!DoorIsClear(scene, door, depth)) continue;
                        }
                        // The new physical tent/furniture may not obstruct an earlier accepted doorway.
                        bool blocksEntrance = false;
                        foreach (TentInterior previous in acceptedTents)
                        {
                            float reach = Math.Max(width, depth) * 1.5f + previous.DoorDepth + 2f;
                            if (previous.Frame.origin.AsVec2.DistanceSquared(door.origin.AsVec2) <= reach * reach &&
                                !DoorIsClear(scene, previous.Frame, previous.DoorDepth))
                            { blocksEntrance = true; break; }
                        }
                        if (blocksEntrance) continue;
                        interior=trial;doorwayTurns[prefab]=turn;accepted=true;
                        Logger.Log(LogSource, "tent placed prefab="+prefab+" scale="+fittedScale.ToString("0.00")+" doorwayTurn="+turn+" unevenGround="+(groundPass > 0)+" skippedFurniture="+skipped);
                        return true;
                    }
                    catch (Exception ex)
                    { Logger.Log(LogSource, "[WARN] tent placement trial failed prefab=" + prefab + " turn=" + turn + " error=" + ex.Message); }
                    finally
                    {
                        if (!accepted)
                        {
                            if (trial != null) for (int f=0; f<trial.FurnitureEntities.Count; f++) trial.FurnitureEntities[f].Remove(80);
                            tent.Remove(80);
                        }
                    }
                }
            }
            return false;
        }

		private static void CaptureTentEntities(GameEntity root, HashSet<UIntPtr> entities)
		{
			var pending = new Stack<GameEntity>();
			pending.Push(root);
			while (pending.Count > 0)
			{
				GameEntity entity = pending.Pop();
				if (!entities.Add(entity.Pointer))
				{
					continue;
				}
				if (entities.Count + pending.Count + entity.ChildCount > 256)
				{
					throw new InvalidOperationException("Tent hierarchy exceeds its bounded placement budget.");
				}
				for (int i = 0; i < entity.ChildCount; i++)
				{
					pending.Push(entity.GetChild(i));
				}
			}
		}

        private static bool TryFitTentGround(Scene scene, ref MatrixFrame door, float halfX, float halfY, bool allowUnevenGround = false)
        {
            float x = halfX*0.68f, y=halfY*0.68f;
            float centre=scene.GetTerrainHeight(door.origin.AsVec2,true);
            float left=scene.GetTerrainHeight(door.TransformToParent(new Vec3(-x,0f,0f)).AsVec2,true);
            float right=scene.GetTerrainHeight(door.TransformToParent(new Vec3(x,0f,0f)).AsVec2,true);
            float back=scene.GetTerrainHeight(door.TransformToParent(new Vec3(0f,-y,0f)).AsVec2,true);
            float front=scene.GetTerrainHeight(door.TransformToParent(new Vec3(0f,y,0f)).AsVec2,true);
            float sx=(right-left)/(2f*x), sy=(front-back)/(2f*y);
            if (!FieldCampPlacementRules.Finite(centre) || !FieldCampPlacementRules.Finite(sx) ||
                !FieldCampPlacementRules.Finite(sy) || (!allowUnevenGround && sx*sx+sy*sy>0.0225f)) return false;
            Vec3 side=door.rotation.s;side.z=sx;side.Normalize();
            Vec3 forward=door.rotation.f;forward.z=sy;
            Vec3 up=Vec3.CrossProduct(side,forward);up.Normalize();
            forward=Vec3.CrossProduct(up,side);forward.Normalize();
            door.rotation.s=side;door.rotation.f=forward;door.rotation.u=up;door.origin.z=centre+0.02f;
            for (int ix=-1;ix<=1;ix++) for(int iy=-1;iy<=1;iy++)
            {
                Vec3 point=door.TransformToParent(new Vec3(ix*x,iy*y,0f));
                float actual=scene.GetTerrainHeight(point.AsVec2,true);
                if (!FieldCampPlacementRules.Finite(actual) || (!allowUnevenGround && Math.Abs(actual-point.z)>0.20f) ||
                    scene.GetWaterLevelAtPosition(point.AsVec2,true,true)>actual-0.3f) return false;
            }
            return true;
        }

		private static bool DoorIsClear(Scene scene, MatrixFrame door, float depth)
		{
			// Probe the full entrance aisle at agent height; a central support pole must not block it.
			for (int lane = -1; lane <= 1; lane++)
			{
				for (int level = 0; level < 2; level++)
				{
					float z = level == 0 ? 0.45f : 1.45f;
					Vec3 from = door.TransformToParent(new Vec3(lane * 0.35f, depth * 0.3f, z));
					Vec3 to = door.TransformToParent(new Vec3(lane * 0.35f, depth + 0.8f, z));
					if (scene.RayCastForClosestEntityOrTerrain(from, to, out _, out _, out _, 0.16f, ClearanceExcludeFlags))
					{
						return false;
					}
				}
			}
			return true;
		}

		private static bool PlaceTentFurniture(Scene scene, FieldCampLayout.Prop prop, string[] pool, TentInterior interior, Dictionary<string, BoundingBox> measured)
		{
			// Keep at least a 1.3 m centre/entrance aisle; positions are normalized interior offsets.
			Vec3 position = new Vec3(prop.Position.x * interior.HalfWidth, prop.Position.y * interior.HalfDepth, 0f);
			for (int c = 0; c < pool.Length; c++)
			{
				string prefab = pool[(c + Math.Abs(prop.Variant) % pool.Length) % pool.Length];
				if (!GameEntity.PrefabExists(prefab))
				{
					continue;
				}
				if (!measured.TryGetValue(prefab, out BoundingBox box))
				{
					GameEntity probe = null;
					try
					{
						probe = GameEntity.Instantiate(scene, prefab, false, false);
						if (probe == null)
						{
							continue;
						}
						box = probe.GetLocalBoundingBox();
						measured[prefab] = box;
					}
					catch
					{
						continue;
					}
					finally
					{
						probe?.Remove(80);
					}
				}
				if (!box.min.IsValid || !box.max.IsValid)
				{
					continue;
				}
				float halfX = (box.max.x - box.min.x) * 0.5f;
				float halfY = (box.max.y - box.min.y) * 0.5f;
				if (!(halfX > 0f && halfY > 0f))
				{
					continue;
				}
				// Place the mesh's centre, not an arbitrary artist pivot, at the interior offset.
				Vec3 centre = position;
				if (prop.Position.x < -0.1f)
				{
					centre.x = Math.Min(centre.x, -0.65f - halfX);
				}
				else if (prop.Position.x > 0.1f)
				{
					centre.x = Math.Max(centre.x, 0.65f + halfX);
				}
				else
				{
					centre.y = Math.Min(centre.y, -0.5f - halfY);
				}
				Vec3 min = centre - new Vec3(halfX, halfY, 0f);
				Vec3 max = centre + new Vec3(halfX, halfY, 0f);
				if (min.x < -interior.HalfWidth || max.x > interior.HalfWidth || min.y < -interior.HalfDepth || max.y > interior.HalfDepth)
				{
					continue;
				}
				bool overlap = false;
				for (int i = 0; i < interior.Furniture.Count; i++)
				{
					BoundingBox used = interior.Furniture[i];
					if (min.x < used.max.x + 0.08f && max.x > used.min.x - 0.08f && min.y < used.max.y + 0.08f && max.y > used.min.y - 0.08f)
					{
						overlap = true;
						break;
					}
				}
				if (overlap || !FurnitureFitsTent(scene, interior, min, max, box.max.z - box.min.z))
				{
					continue;
				}
				MatrixFrame frame = interior.Frame;
				frame.origin = interior.Frame.TransformToParent(new Vec3(centre.x - (box.min.x + box.max.x) * 0.5f,
					centre.y - (box.min.y + box.max.y) * 0.5f, -box.min.z + 0.02f));
				GameEntity furnitureEntity = TrySpawn(scene, prefab, frame, !WalkThrough.Contains(prop.Kind));
				if (furnitureEntity != null)
				{
					interior.FurnitureEntities.Add(furnitureEntity);
					interior.Furniture.Add(new BoundingBox { min = min, max = max });
					return true;
				}
			}
			return false;
		}

		private static bool FurnitureFitsTent(Scene scene, TentInterior interior, Vec3 min, Vec3 max, float height)
		{
			// A box can include guy ropes and empty space. Prove roof cover above the actual
			// furniture corners, and an unobstructed horizontal cross-section below the roof.
			if (!(height > 0f) || height + 0.2f >= interior.RoofHeight)
			{
				return false;
			}
			for (int x = 0; x < 2; x++)
			{
				for (int y = 0; y < 2; y++)
				{
					Vec3 corner = interior.Frame.TransformToParent(new Vec3(x == 0 ? min.x : max.x, y == 0 ? min.y : max.y, 0f));
					Vec3 top = corner + new Vec3(0f, 0f, interior.RoofHeight + 0.5f);
					Vec3 bottom = corner + new Vec3(0f, 0f, height + 0.12f);
					if (!scene.RayCastForClosestEntityOrTerrain(top, bottom, out _, out _, out WeakGameEntity roof, 0.02f,
						ClearanceExcludeFlags) || !roof.IsValid || !interior.Entities.Contains(roof.Pointer))
					{
						return false;
					}
				}
			}
			for (int axis = 0; axis < 2; axis++)
			{
				Vec3 a = axis == 0 ? new Vec3(min.x, (min.y + max.y) * 0.5f, Math.Max(0.25f, height * 0.5f))
					: new Vec3((min.x + max.x) * 0.5f, min.y, Math.Max(0.25f, height * 0.5f));
				Vec3 b = axis == 0 ? new Vec3(max.x, a.y, a.z) : new Vec3(a.x, max.y, a.z);
				if (scene.RayCastForClosestEntityOrTerrain(interior.Frame.TransformToParent(a), interior.Frame.TransformToParent(b), out _, out _, out _, 0.06f,
					ClearanceExcludeFlags))
				{
					return false;
				}
			}
			return true;
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
        // Build a basis from the actual endpoints instead of guessing the prefab yaw.
        // Compensate the local origin as many fence prefabs are anchored at one end.
        int pieces = Math.Max(1, (int)Math.Ceiling(length / size.Length));
        first.Remove(80);
        int placed = 0;
        for (int i = 0; i < pieces; i++)
        {
            Vec2 p0 = from + dir * (length * i / pieces);
            Vec2 p1 = from + dir * (length * (i + 1) / pieces);
            Vec3 start = campFrame.TransformToParent(new Vec3(p0.x, p0.y, 0f));
            Vec3 end = campFrame.TransformToParent(new Vec3(p1.x, p1.y, 0f));
            start.z = scene.GetTerrainHeight(start.AsVec2, true);
            end.z = scene.GetTerrainHeight(end.AsVec2, true);
            Vec3 delta = end - start;
            float stretch = delta.Length / size.Length;
            Vec3 along = delta.NormalizedCopy();
            Vec3 up = new Vec3(0f, 0f, 1f);
            MatrixFrame frame = MatrixFrame.Identity;
            if (size.AlongY)
            {
                frame.rotation.s = Vec3.CrossProduct(along, up).NormalizedCopy();
                frame.rotation.u = Vec3.CrossProduct(frame.rotation.s, along).NormalizedCopy();
                frame.rotation.f = along * stretch;
            }
            else
            {
                frame.rotation.f = Vec3.CrossProduct(up, along).NormalizedCopy();
                frame.rotation.u = Vec3.CrossProduct(along, frame.rotation.f).NormalizedCopy();
                frame.rotation.s = along * stretch;
            }
            frame.origin = (start + end) * 0.5f - frame.rotation.TransformToParent(size.PivotToGroundCentre);
            if (TrySpawn(scene, prefab, frame, true) != null)
            {
                placed++;
                FenceObstacles.Add(new FenceObstacle { From = start, To = end, HalfWidth = size.HalfWidth });
            }
        }
		return placed;
	}

    /// <summary>One hanging roast per successful spit, fitted to both meshes, setup only.</summary>
    private static bool PlaceRoastFood(Scene scene, GameEntity spit, MatrixFrame spitFrame, int variant,
        Dictionary<string, BoundingBox> measured)
    {
        try
        {
            BoundingBox rack = spit.GetLocalBoundingBox();
            if (!rack.min.IsValid || !rack.max.IsValid) return false;
            float rackX = rack.max.x - rack.min.x, rackY = rack.max.y - rack.min.y;
            float height = rack.max.z - rack.min.z, span = Math.Max(rackX, rackY);
            if (height <= 0.2f || span <= 0.3f) return false;
            // Centre of the upper crossbar region, not terrain height or an artist's arbitrary pivot.
            Vec3 rod = new Vec3((rack.min.x + rack.max.x) * 0.5f,
                (rack.min.y + rack.max.y) * 0.5f, rack.max.z - height * 0.15f);
            for (int i = 0; i < RoastFoods.Length; i++)
            {
                string prefab = RoastFoods[(variant % RoastFoods.Length + RoastFoods.Length + i) % RoastFoods.Length];
                if (!TryMeasureTent(scene, prefab, measured, out BoundingBox food)) continue;
                float foodX = food.max.x - food.min.x, foodY = food.max.y - food.min.y;
                float foodSpan = Math.Max(foodX, foodY);
                if (foodSpan <= 0.01f) continue;
                float fit = Math.Min(1.25f, Math.Min(0.7f, span * 0.4f) / foodSpan);
                MatrixFrame frame = spitFrame;
                if ((rackY > rackX) != (foodY > foodX)) frame.rotation.RotateAboutUp(MathF.PI * 0.5f);
                frame.rotation.ApplyScaleLocal(fit);
                Vec3 centre = (food.min + food.max) * 0.5f;
                frame.origin = spitFrame.TransformToParent(rod) - frame.rotation.TransformToParent(centre);
                // Decorative food must not add another body over the fire or change camp routing.
                if (TrySpawn(scene, prefab, frame, false) != null) return true;
            }
            Logger.Log(LogSource, "roast food unavailable spit=" + spit.Name);
        }
        catch (Exception ex)
        {
            Logger.Log(LogSource, "roast food placement failed spit=" + spit.Name + " error=" + ex.Message);
        }
        return false;
    }

	/// <summary>World height of a placed table's top; set-up only, one bounding-box read per table.</summary>
	private struct TableTop
	{
		internal Vec3 Centre;
		/// <summary>World direction of the measured long side.</summary>
		internal Vec3 Along;
		/// <summary>Half the long side less a 0.2 m rim, so items never overhang.</summary>
		internal float Reach;
	}

	private static void RecordTableTop(GameEntity table, MatrixFrame frame, int anchor, Dictionary<int, TableTop> tops)
	{
		try
		{
			BoundingBox box = table.GetLocalBoundingBox();
			float height = box.max.z - box.min.z;
			// A 0.4-1.3 m top is a real table; anything else (a trunk, a failed measure) carries nothing.
			if (!box.min.IsValid || !box.max.IsValid || height < 0.4f || height > 1.3f) return;
			float x = box.max.x - box.min.x, y = box.max.y - box.min.y;
			Vec3 along = frame.rotation.TransformToParent(x >= y ? new Vec3(1f, 0f, 0f) : new Vec3(0f, 1f, 0f));
			along.z = 0f;
			if (along.LengthSquared < 0.0001f) return;
			tops[anchor] = new TableTop
			{
				Centre = frame.TransformToParent(new Vec3((box.min.x + box.max.x) * 0.5f, (box.min.y + box.max.y) * 0.5f, box.max.z)),
				Along = along.NormalizedCopy(),
				Reach = Math.Max(0f, Math.Max(x, y) * 0.5f - 0.2f)
			};
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "table top measure failed " + table.Name + ": " + ex.Message);
		}
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
				return new PieceSize { Length = longest, AlongY = y > x, HalfWidth = Math.Max(0.1f, Math.Min(x, y) * 0.5f), PivotToGroundCentre = new Vec3((box.min.x + box.max.x) * 0.5f, (box.min.y + box.max.y) * 0.5f, box.min.z) };
			}
		}
		catch
		{
		}
		return new PieceSize { Length = FencePiece, AlongY = false, HalfWidth = 0.3f };
	}

	private static MatrixFrame FrameFor(Scene scene, MatrixFrame campFrame, Vec2 local, float yaw, float uniformScale)
	{
		Vec3 world = campFrame.TransformToParent(new Vec3(local.x, local.y, 0f));
		// Centre terrain, not the five-probe minimum (which buried the uphill side and
			// split tent/furniture levels). Large tents are checked over their measured footprint.
			world.z = scene.GetTerrainHeight(world.AsVec2, true);
			if (float.IsNaN(world.z) || float.IsInfinity(world.z))
			{
				throw new InvalidOperationException("Invalid camp prop terrain height.");
			}
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

	/// <summary>Terrain height (no props, no trees) at the lowest of the centre and four footprint corners.</summary>
	internal static float LowestTerrain(Scene scene, Vec2 p, float half)
	{
		try
		{
			float low = scene.GetTerrainHeight(p, true);
			if (half > 0.05f)
			{
				low = Math.Min(low, scene.GetTerrainHeight(p + new Vec2(half, half), true));
				low = Math.Min(low, scene.GetTerrainHeight(p + new Vec2(-half, half), true));
				low = Math.Min(low, scene.GetTerrainHeight(p + new Vec2(half, -half), true));
				low = Math.Min(low, scene.GetTerrainHeight(p + new Vec2(-half, -half), true));
			}
			return low;
		}
		catch
		{
			return 0f;
		}
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
			GameEntity entity = null;
			try
			{
				if (!physics)
				{
					entity = GameEntity.Instantiate(scene, prefab, false, false);
					entity?.SetGlobalFrame(in frame);
					return entity;
				}
				// Created at its final frame with physics, as vanilla workshop props are.
				entity = GameEntity.Instantiate(scene, prefab, frame, false);
				MakeSolid(entity);
				return entity;
			}
			catch
			{
				entity?.Remove(80);
				return null;
			}
		}

	/// <summary>
	/// A runtime prefab instance does not reliably end up with an active static body, so
	/// tents and props were walk-through. Same steps as the Vengeance gallows ramps, which
	/// do block agents: stationary mobility, clear Dynamic/Moveable, preserving authored Disabled on the
	/// tree, then enable physics on the whole tree. Once per prop at set-up.
	/// </summary>
	internal static void MakeSolid(GameEntity entity)
	{
		if (entity == null)
		{
			return;
		}
		_solidRequested++;
		try
		{
			var pending = new Stack<GameEntity>();
			var visited = new HashSet<UIntPtr>();
			pending.Push(entity);
			bool hasBody = false;
			while (pending.Count > 0)
			{
				GameEntity part = pending.Pop();
				if (!visited.Add(part.Pointer))
				{
					continue;
				}
				if (visited.Count + pending.Count + part.ChildCount > 256)
				{
					throw new InvalidOperationException("Camp prop hierarchy exceeds its physics setup budget.");
				}
				for (int i = 0; i < part.ChildCount; i++)
				{
					pending.Push(part.GetChild(i));
				}
				// Preserve disabled helper/variant bodies; never recursively enable an authored
				// invisible door blocker when enabling its visible parent.
				bool enabled = (part.BodyFlag & BodyFlags.Disabled) == 0;
				part.SetMobility(GameEntity.Mobility.Stationary);
				part.RemoveBodyFlags(BodyFlags.Dynamic | BodyFlags.Moveable, applyToChildren: false);
				part.SetPhysicsState(isEnabled: enabled, setChildren: false);
				hasBody |= enabled && part.HasBody();
			}
			if (hasBody)
			{
				_solidWithBody++;
			}
		}
		catch (Exception ex)
		{
				if (_solidRequested - _solidWithBody <= 3)
				{
					Logger.Log(LogSource, "solid prop setup failed " + entity.Name + ": " + ex.Message);
				}
				throw;
		}
	}
}
