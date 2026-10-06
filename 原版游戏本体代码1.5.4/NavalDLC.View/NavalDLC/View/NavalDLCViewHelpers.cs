using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using NavalDLC.Missions.Objects;
using NavalDLC.View.Map.Visuals;
using SandBox.View.Map;
using SandBox.View.Map.Managers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.View;

public class NavalDLCViewHelpers
{
	public static class ShipVisualHelper
	{
		private const string BannerTag = "banner_with_faction_color";

		private const float AnimationSpeedMultiplier = 0.1f;

		public static GameEntity GetFlagshipEntity(PartyBase party, Scene scene)
		{
			if (party.Ships.Count > 0)
			{
				Ship flagShip = party.FlagShip;
				return GetShipEntityForCampaign(flagShip, scene, flagShip.GetShipVisualSlotInfos());
			}
			float scaleAmount = 0.4f;
			MatrixFrame frame = MatrixFrame.Identity;
			GameEntity gameEntity = GameEntity.CreateEmpty(scene);
			gameEntity.AddMultiMesh(MetaMesh.GetCopy("boat_sail_on"));
			frame.rotation.ApplyScaleLocal(scaleAmount);
			gameEntity.SetFrame(ref frame);
			return gameEntity;
		}

		public static GameEntity GetShipEntity(Ship ship, Scene scene, List<ShipVisualSlotInfo> selectedPieces, bool createPhysics = false)
		{
			MissionShipObject missionShipObject = MBObjectManager.Instance.GetObject<MissionShipObject>(ship.ShipHull.MissionShipObjectId);
			int randomValue = ship.RandomValue;
			float mapVisualScale = ship.ShipHull.MapVisualScale;
			string shipPrefab = missionShipObject?.Prefab;
			ShipHelper.TryGetSailColors(ship, out (uint, uint) sailColors);
			GameEntity gameEntity = VisualShipFactory.CreateVisualShip(shipPrefab, scene, selectedPieces, randomValue, ship.HitPoints / ship.MaxHitPoints, sailColors.Item1, sailColors.Item2, createPhysics, ship.ShipHull.FloatingForceMultiplier, keepFireEntities: false);
			ShipVisual firstScriptOfType = gameEntity.GetFirstScriptOfType<ShipVisual>();
			if (firstScriptOfType != null)
			{
				foreach (ScriptComponentBehavior sailVisual2 in firstScriptOfType.SailVisuals)
				{
					if (sailVisual2 is SailVisual sailVisual && sailVisual.SailTopBannerEntity != null && sailVisual.SailTopBannerEntity.HasTag("banner_with_faction_color"))
					{
						GameEntity sailTopBannerEntity = sailVisual.SailTopBannerEntity;
						ShipHelper.TryGetShipBanner(ship, out var banner);
						SetBanner(sailTopBannerEntity, banner);
					}
				}
			}
			gameEntity?.SetPhysicsState(isEnabled: false, setChildren: true);
			gameEntity.SetBodyFlags(BodyFlags.Moveable | BodyFlags.OnlyCollideWithRaycast);
			MatrixFrame frame = MatrixFrame.Identity;
			frame.rotation.ApplyScaleLocal(mapVisualScale);
			gameEntity.SetFrame(ref frame);
			return gameEntity;
		}

		public static GameEntity GetShipEntityForCampaign(Ship ship, Scene scene, List<ShipVisualSlotInfo> selectedPieces)
		{
			MissionShipObject missionShipObject = MBObjectManager.Instance.GetObject<MissionShipObject>(ship.ShipHull.MissionShipObjectId);
			int randomValue = ship.RandomValue;
			string customSailPatternId = ship.CustomSailPatternId;
			float mapVisualScale = ship.ShipHull.MapVisualScale;
			string shipPrefab = missionShipObject?.Prefab;
			ShipHelper.TryGetSailColors(ship, out (uint, uint) sailColors);
			GameEntity gameEntity = VisualShipFactory.CreateVisualShipForCampaign(shipPrefab, scene, selectedPieces, randomValue, customSailPatternId, sailColors.Item1, sailColors.Item2);
			ShipVisual firstScriptOfType = gameEntity.GetFirstScriptOfType<ShipVisual>();
			if (firstScriptOfType != null)
			{
				foreach (ScriptComponentBehavior sailVisual2 in firstScriptOfType.SailVisuals)
				{
					if (sailVisual2 is SailVisual sailVisual && sailVisual.SailTopBannerEntity != null && sailVisual.SailTopBannerEntity.HasTag("banner_with_faction_color"))
					{
						GameEntity sailTopBannerEntity = sailVisual.SailTopBannerEntity;
						ShipHelper.TryGetShipBanner(ship, out var banner);
						SetBanner(sailTopBannerEntity, banner);
					}
				}
			}
			gameEntity?.SetPhysicsState(isEnabled: false, setChildren: true);
			gameEntity.SetBodyFlags(BodyFlags.Moveable | BodyFlags.OnlyCollideWithRaycast);
			MatrixFrame frame = MatrixFrame.Identity;
			frame.rotation.ApplyScaleLocal(mapVisualScale);
			gameEntity.SetFrame(ref frame);
			return gameEntity;
		}

		public static void CollectSailVisuals(WeakGameEntity shipEntity, List<SailVisual> sailVisuals)
		{
			sailVisuals.Clear();
			ShipVisual firstScriptOfType = shipEntity.GetFirstScriptOfType<ShipVisual>();
			if (firstScriptOfType == null)
			{
				return;
			}
			foreach (ScriptComponentBehavior sailVisual2 in firstScriptOfType.SailVisuals)
			{
				if (sailVisual2 is SailVisual sailVisual)
				{
					sailVisual.SailEnabled = false;
					sailVisual.SetFoldSailStepMultiplier(0.3f);
					sailVisual.SetFoldSailDuration(0.4f);
					sailVisual.SetUnfoldSailDuration(0.2f);
					sailVisual.FoldAnimationEnabled = false;
					sailVisuals.Add(sailVisual);
				}
			}
		}

		public static void FoldSails(List<SailVisual> sailVisuals)
		{
			foreach (SailVisual sailVisual in sailVisuals)
			{
				sailVisual.SailEnabled = false;
			}
		}

		public static void UnfoldSails(List<SailVisual> sailVisuals)
		{
			foreach (SailVisual sailVisual in sailVisuals)
			{
				sailVisual.SailEnabled = true;
			}
		}

		public static void RefreshShipVisuals(WeakGameEntity shipEntity, Ship ship, List<SailVisual> sailVisuals)
		{
			VisualShipFactory.RefreshUpgrades(shipEntity, ship.GetShipVisualSlotInfos());
			ShipHelper.TryGetSailColors(ship, out (uint, uint) sailColors);
			foreach (SailVisual sailVisual in sailVisuals)
			{
				sailVisual.ShipVisual.UpdateParameters(ship.HitPoints / ship.MaxHitPoints, sailColors);
				sailVisual.RefreshSailVisual();
			}
			ShipHelper.TryGetShipBanner(ship, out var banner);
			UpdateBanner(banner, sailVisuals);
			foreach (Mesh item in shipEntity.GetAllMeshesWithTag("faction_color"))
			{
				(item.Color, item.Color2) = sailColors;
			}
		}

		public static void RefreshShipVisuals(GameEntity shipEntity, List<ShipVisualSlotInfo> selectedPieces, uint sailColor1, uint sailColor2, Banner banner, float healthPercent)
		{
			VisualShipFactory.RefreshUpgrades(shipEntity.WeakEntity, selectedPieces);
			ShipVisual firstScriptOfType = shipEntity.GetFirstScriptOfType<ShipVisual>();
			if (firstScriptOfType != null)
			{
				firstScriptOfType.UpdateParameters(healthPercent, (sailColor1, sailColor2));
				foreach (ScriptComponentBehavior sailVisual2 in firstScriptOfType.SailVisuals)
				{
					if (sailVisual2 is SailVisual sailVisual)
					{
						if (sailVisual.SailTopBannerEntity != null && sailVisual.SailTopBannerEntity.HasTag("banner_with_faction_color"))
						{
							SetBanner(sailVisual.SailTopBannerEntity, banner);
						}
						sailVisual.RefreshSailVisual();
					}
				}
			}
			foreach (Mesh item in shipEntity.WeakEntity.GetAllMeshesWithTag("faction_color"))
			{
				item.Color = sailColor1;
				item.Color2 = sailColor2;
			}
		}

		public static ShipHull GetFerryShipHullForSettlement(Settlement settlement)
		{
			string objectName = settlement.Culture.StringId switch
			{
				"empire" => "empire_trade_ship", 
				"aserai" => "central_light_ship", 
				"sturgia" => "northern_trade_ship", 
				"vlandia" => "western_trade_ship", 
				"battania" => "northern_trade_ship", 
				"khuzait" => "eastern_trade_ship", 
				"nord" => "northern_trade_ship", 
				_ => "northern_trade_ship", 
			};
			return MBObjectManager.Instance.GetObject<ShipHull>(objectName);
		}

		private static void UpdateBanner(Banner banner, List<SailVisual> sailVisuals)
		{
			foreach (SailVisual sailVisual in sailVisuals)
			{
				if (sailVisual.SailTopBannerEntity != null && sailVisual.SailTopBannerEntity.HasTag("banner_with_faction_color"))
				{
					SetBanner(sailVisual.SailTopBannerEntity, banner, isUpdated: true);
				}
			}
		}

		private static void SetBanner(GameEntity bannerEntity, Banner banner, bool isUpdated = false)
		{
			banner.GetTableauTextureLarge(BannerDebugInfo.CreateManual("SetBanner"), onTextureRendered);
			void onTextureRendered(Texture tex)
			{
				if (bannerEntity.Scene != null)
				{
					List<Mesh> list = bannerEntity.GetAllMeshesWithTag("banner_with_faction_color").ToList();
					if (list.IsEmpty() && bannerEntity.GetFirstMesh() != null)
					{
						list.Add(bannerEntity.GetFirstMesh());
					}
					foreach (Mesh item in list)
					{
						Material material = item.GetMaterial();
						Material material2 = (isUpdated ? material : material.CreateCopy());
						material2.SetTexture(Material.MBTextureType.DiffuseMap2, tex);
						uint num = (uint)material2.GetShader().GetMaterialShaderFlagMask("use_tableau_blending");
						ulong shaderFlags = material2.GetShaderFlags();
						material2.SetShaderFlags(shaderFlags | num);
						item.SetMaterial(material2);
					}
				}
			}
		}
	}

	public static class BlockadeVisualHelper
	{
		private const float AnimationSpeedMultiplier = 0.1f;

		public static List<Vec3> GetPositionsOnBlockadeArc(Settlement settlement, int numberOfArcs, int numberOfPositions, float angle, float distanceBetweenArcs)
		{
			CampaignVec2 portPosition = settlement.PortPosition;
			Vec2 vec = settlement.PortPosition.ToVec2() - settlement.Position.ToVec2();
			List<Vec3> list = new List<Vec3>();
			Vec2 vec2 = vec.Normalized();
			vec2.RotateCCW((0f - angle) / 2f);
			Vec2 vec3 = vec2;
			for (int i = 1; numberOfArcs >= i; i++)
			{
				if (numberOfPositions <= 0)
				{
					break;
				}
				int num = TaleWorlds.Library.MathF.Min(i, numberOfPositions);
				for (int j = 0; j < num; j++)
				{
					Vec3 item = ((i == 1) ? portPosition : (portPosition + vec3 * (i - 1) * distanceBetweenArcs)).AsVec3();
					vec3.RotateCCW(angle / (float)TaleWorlds.Library.MathF.Max(1, num - 1));
					list.Add(item);
				}
				vec3 = vec2;
				numberOfPositions -= i;
			}
			return list;
		}

		public static void AddBlockadeVisuals(Dictionary<Ship, NavalMobilePartyVisual.BlockadeShipVisual> shipToBlockadeShipVisualCache, PartyBase party, GameEntity strategicEntity)
		{
			int num = 0;
			int num2 = 0;
			SiegeEvent siegeEvent = party.MobileParty.SiegeEvent;
			Settlement besiegedSettlement = siegeEvent.BesiegedSettlement;
			BlockadePositionScript firstScriptOfType = SettlementVisualManager.Current.GetSettlementVisual(besiegedSettlement).StrategicEntity.GetFirstScriptOfType<BlockadePositionScript>();
			IEnumerable<PartyBase> involvedPartiesForEventType = siegeEvent.BesiegerCamp.GetInvolvedPartiesForEventType();
			MobileParty leaderParty = siegeEvent.BesiegerCamp.LeaderParty;
			if (firstScriptOfType == null)
			{
				return;
			}
			if (!shipToBlockadeShipVisualCache.IsEmpty())
			{
				foreach (KeyValuePair<Ship, NavalMobilePartyVisual.BlockadeShipVisual> item in shipToBlockadeShipVisualCache)
				{
					item.Value.ShipEntity.SetVisibilityExcludeParents(visible: false);
				}
			}
			Vec3 center;
			List<List<Vec3>> blockadeArc = firstScriptOfType.GetBlockadeArc(involvedPartiesForEventType.Sum((PartyBase p) => p.Ships.Count), out center);
			int num3 = ((leaderParty.Ships.Count > 0) ? (blockadeArc[0].Count / 2) : (-1));
			foreach (PartyBase item2 in involvedPartiesForEventType)
			{
				if (num == blockadeArc.Count)
				{
					break;
				}
				if (item2.Ships.IsEmpty())
				{
					continue;
				}
				Ship flagShip = item2.FlagShip;
				if (leaderParty.Party == item2)
				{
					if (item2 == party)
					{
						if (!shipToBlockadeShipVisualCache.TryGetValue(flagShip, out var value))
						{
							value = (shipToBlockadeShipVisualCache[flagShip] = CreateBlockadeShipVisual(ShipVisualHelper.GetFlagshipEntity(item2, strategicEntity.Scene)));
						}
						InitializeBlockadeVisual(blockadeArc[0][num3], value.ShipEntity, center);
					}
				}
				else
				{
					if (num2 == num3 && num == 0)
					{
						num2++;
					}
					if (num2 < blockadeArc[num].Count && item2 == party)
					{
						if (!shipToBlockadeShipVisualCache.TryGetValue(flagShip, out var value2))
						{
							value2 = (shipToBlockadeShipVisualCache[flagShip] = CreateBlockadeShipVisual(ShipVisualHelper.GetFlagshipEntity(item2, strategicEntity.Scene)));
						}
						InitializeBlockadeVisual(blockadeArc[num][num2], value2.ShipEntity, center);
					}
					num2++;
				}
				if (num2 >= blockadeArc[num].Count)
				{
					num++;
					num2 = 0;
				}
			}
			if (num >= blockadeArc.Count)
			{
				return;
			}
			foreach (PartyBase item3 in involvedPartiesForEventType)
			{
				if (num == blockadeArc.Count)
				{
					break;
				}
				if (item3.Ships.Count() <= 1)
				{
					continue;
				}
				foreach (Ship item4 in (item3 == party) ? item3.Ships.OrderByDescending((Ship x) => x.FlagshipScore).ToMBList() : item3.Ships)
				{
					if (num == blockadeArc.Count)
					{
						break;
					}
					if (item4 == item3.FlagShip)
					{
						continue;
					}
					if (num2 == num3 && num == 0)
					{
						num2++;
					}
					if (item3 == party)
					{
						if (!shipToBlockadeShipVisualCache.TryGetValue(item4, out var value3))
						{
							value3 = (shipToBlockadeShipVisualCache[item4] = CreateBlockadeShipVisual(ShipVisualHelper.GetShipEntityForCampaign(item4, strategicEntity.Scene, item4.GetShipVisualSlotInfos())));
						}
						InitializeBlockadeVisual(blockadeArc[num][num2], value3.ShipEntity, center);
					}
					num2++;
					if (num2 >= blockadeArc[num].Count)
					{
						num++;
						num2 = 0;
					}
				}
			}
		}

		private static NavalMobilePartyVisual.BlockadeShipVisual CreateBlockadeShipVisual(GameEntity shipEntity)
		{
			return new NavalMobilePartyVisual.BlockadeShipVisual
			{
				ShipEntity = shipEntity,
				RockingPhase = MBRandom.RandomFloatRanged(-System.MathF.PI, System.MathF.PI)
			};
		}

		private static void InitializeBlockadeVisual(Vec3 position, GameEntity shipEntity, Vec3 centerOfArc)
		{
			Vec2 asVec = position.AsVec2;
			Vec2 vec = asVec - centerOfArc.AsVec2;
			MatrixFrame frame = shipEntity.GetFrame();
			position.z = new CampaignVec2(asVec, isOnLand: false).AsVec3().Z;
			frame.origin = position;
			float num = vec.AngleBetween(frame.rotation.f.AsVec2);
			frame.Rotate(System.MathF.PI / 2f - num, in Vec3.Up);
			shipEntity.SetFrame(ref frame);
			shipEntity.SetVisibilityExcludeParents(visible: true);
			ShipVisual firstScriptOfType = shipEntity.GetFirstScriptOfType<ShipVisual>();
			if (firstScriptOfType == null)
			{
				return;
			}
			foreach (ScriptComponentBehavior sailVisual2 in firstScriptOfType.SailVisuals)
			{
				if (sailVisual2 is SailVisual sailVisual)
				{
					sailVisual.SailEnabled = false;
					sailVisual.SetFoldSailStepMultiplier(0.3f);
					sailVisual.SetFoldSailDuration(0.4f);
					sailVisual.SetUnfoldSailDuration(0.2f);
					sailVisual.FoldAnimationEnabled = false;
				}
			}
		}
	}
}
