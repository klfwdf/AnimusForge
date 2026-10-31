using System;
using System.Collections.Generic;
using SandBox;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace AnimusForge;

/// <summary>
/// One independent tent cluster per camped party on the campaign map scene.
/// Entities are created once per (scene, party); every frame only compares the
/// cached Scene reference. MapScreen.OnFinalize clears the scene and loading a
/// save creates a new one, so a changed reference triggers a rebuild.
/// </summary>
internal sealed class FieldCampMapVisuals
{
	private const string LogSource = "FieldCamp";
	private const string NeutralTent = "map_icon_siege_camp_tent";
	// Vanilla siege tents use ~1.2 on an unscaled party entity; tuned in game later.
	private const float BaseScale = 1f;
	private const float ClusterRadius = 0.32f;
	private const float AnchorOffset = 0.45f;

	private sealed class Entry
	{
		internal readonly List<GameEntity> Entities = new List<GameEntity>();
	}

	private readonly Dictionary<MobileParty, Entry> _entries = new Dictionary<MobileParty, Entry>();
	private Scene _scene;
	private MapScreen _mapScreen;
	private bool _disabledForScene;

	internal void Ensure(List<MobileParty> parties)
	{
		Scene scene = TryGetMapScene();
		MapScreen mapScreen = MapScreen.Instance;
		if (scene == null || mapScreen == null)
		{
			return;
		}
		if (!ReferenceEquals(scene, _scene) || !ReferenceEquals(mapScreen, _mapScreen))
		{
			// MapScreen.OnFinalize clears the scene and a load replaces it: old entities are gone.
			_entries.Clear();
			_scene = scene;
			_mapScreen = mapScreen;
			_disabledForScene = false;
		}
		if (_disabledForScene || _entries.Count == parties.Count)
		{
			return;
		}
		for (int i = 0; i < parties.Count; i++)
		{
			MobileParty party = parties[i];
			if (party != null && !_entries.ContainsKey(party))
			{
				_entries[party] = Build(scene, party, i);
			}
		}
	}

	internal void Remove(MobileParty party)
	{
		if (party != null && _entries.TryGetValue(party, out Entry entry))
		{
			RemoveEntities(entry);
			_entries.Remove(party);
		}
	}

	/// <summary>Removes entities from a still-live scene.</summary>
	internal void Clear()
	{
		if (_entries.Count == 0)
		{
			return;
		}
		bool sceneLive = _scene != null && ReferenceEquals(TryGetMapScene(), _scene) && ReferenceEquals(MapScreen.Instance, _mapScreen);
		foreach (Entry entry in _entries.Values)
		{
			if (sceneLive)
			{
				RemoveEntities(entry);
			}
		}
		_entries.Clear();
	}

	/// <summary>Forgets entities without touching the engine (save load / new game).</summary>
	internal void Forget()
	{
		_entries.Clear();
		_scene = null;
		_mapScreen = null;
		_disabledForScene = false;
	}

	private Entry Build(Scene scene, MobileParty party, int index)
	{
		var entry = new Entry();
		try
		{
			string prefab = TentPrefabFor(party);
			int tents = TentCountFor(party.MemberRoster?.TotalManCount ?? 0);
			Vec2 center = ResolveCenter(party, index);
			for (int t = 0; t < tents; t++)
			{
				Vec2 pos = center;
				if (tents > 1)
				{
					float angle = t * (2f * MathF.PI / tents) + index * 0.7f;
					pos += new Vec2(MathF.Cos(angle), MathF.Sin(angle)) * ClusterRadius;
				}
				GameEntity entity = Spawn(scene, prefab, pos, t * 1.3f + index);
				if (entity == null && prefab != NeutralTent)
				{
					prefab = NeutralTent;
					entity = Spawn(scene, prefab, pos, t * 1.3f + index);
				}
				if (entity == null)
				{
					_disabledForScene = true;
					Logger.Log(LogSource, "map tent prefab unavailable; map visuals disabled for this scene");
					break;
				}
				entry.Entities.Add(entity);
			}
		}
		catch (Exception ex)
		{
			_disabledForScene = true;
			Logger.Log(LogSource, "map visual build failed: " + ex.Message);
		}
		return entry;
	}

	private static GameEntity Spawn(Scene scene, string prefab, Vec2 pos, float yaw)
	{
		float height = 0f;
		CampaignVec2 point = new CampaignVec2(pos, true);
		Campaign.Current.MapSceneWrapper.GetHeightAtPoint(in point, ref height);
		MatrixFrame frame = MatrixFrame.Identity;
		frame.rotation.RotateAboutUp(yaw);
		frame.rotation.ApplyScaleLocal(BaseScale);
		frame.origin = new Vec3(pos.x, pos.y, height);
		GameEntity entity = GameEntity.Instantiate(scene, prefab, false, false);
		if (entity == null)
		{
			return null;
		}
		entity.SetFrame(ref frame);
		return entity;
	}

	private static Vec2 ResolveCenter(MobileParty party, int index)
	{
		Vec2 origin = party.Position.ToVec2();
		float angle = 0.9f + index * 2.39996f;
		Vec2 candidate = origin + new Vec2(MathF.Cos(angle), MathF.Sin(angle)) * AnchorOffset;
		try
		{
			CampaignVec2 probe = new CampaignVec2(candidate, true);
			if (probe.IsValid())
			{
				return candidate;
			}
		}
		catch
		{
		}
		return origin;
	}

	private static int TentCountFor(int men)
	{
		if (men >= FieldCampSettings.LargeCampMen)
		{
			return 5;
		}
		return men >= FieldCampSettings.MediumCampMen ? 3 : 1;
	}

	private static string TentPrefabFor(MobileParty party)
	{
		string culture = (party.LeaderHero?.Culture ?? party.ActualClan?.Culture)?.StringId ?? "";
		if (culture == "khuzait")
		{
			return "map_icons_khuzait_tent_a";
		}
		if (culture == "battania")
		{
			return "map_icons_battania_tent_a";
		}
		return NeutralTent;
	}

	private static void RemoveEntities(Entry entry)
	{
		for (int i = 0; i < entry.Entities.Count; i++)
		{
			try
			{
				entry.Entities[i]?.Remove(111);
			}
			catch
			{
			}
		}
		entry.Entities.Clear();
	}

	private static Scene TryGetMapScene()
	{
		try
		{
			return (Campaign.Current?.MapSceneWrapper as MapScene)?.Scene;
		}
		catch
		{
			return null;
		}
	}
}
