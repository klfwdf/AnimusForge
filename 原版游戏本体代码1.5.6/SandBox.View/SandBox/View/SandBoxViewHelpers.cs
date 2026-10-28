using System;
using Helpers;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

namespace SandBox.View;

public class SandBoxViewHelpers
{
	public static class BannerVisualHelper
	{
		public static MetaMesh GetBanner(Banner banner, string bannerMeshName)
		{
			MetaMesh copy = MetaMesh.GetCopy(bannerMeshName);
			for (int i = 0; i < copy.MeshCount; i++)
			{
				Mesh meshAtIndex = copy.GetMeshAtIndex(i);
				if (meshAtIndex.HasTag("dont_use_tableau"))
				{
					continue;
				}
				Material material = meshAtIndex.GetMaterial();
				Material tableauMaterial = null;
				Tuple<Material, Banner> key = new Tuple<Material, Banner>(material, banner);
				if (MapScreen.Instance.CharacterBannerMaterialCache.ContainsKey(key))
				{
					tableauMaterial = MapScreen.Instance.CharacterBannerMaterialCache[key];
				}
				else
				{
					tableauMaterial = material.CreateCopy();
					Action<Texture> setAction = delegate(Texture tex)
					{
						tableauMaterial.SetTexture(Material.MBTextureType.DiffuseMap2, tex);
						uint num = (uint)tableauMaterial.GetShader().GetMaterialShaderFlagMask("use_tableau_blending");
						ulong shaderFlags = tableauMaterial.GetShaderFlags();
						tableauMaterial.SetShaderFlags(shaderFlags | num);
					};
					banner.GetTableauTextureLarge(BannerDebugInfo.CreateManual("GetBanner"), setAction);
					MapScreen.Instance.CharacterBannerMaterialCache[key] = tableauMaterial;
				}
				meshAtIndex.SetMaterial(tableauMaterial);
			}
			return copy;
		}
	}

	public static class MobilePartyVisualHelper
	{
		private const float PartyScale = 0.3f;

		public static void GetMeleeWeaponToWield(PartyBase party, out int wieldedItemIndex)
		{
			wieldedItemIndex = -1;
			CharacterObject visualPartyLeader = PartyBaseHelper.GetVisualPartyLeader(party);
			if (visualPartyLeader == null)
			{
				return;
			}
			for (int i = 0; i < 5; i++)
			{
				if (visualPartyLeader.Equipment[i].Item != null && visualPartyLeader.Equipment[i].Item.PrimaryWeapon.IsMeleeWeapon)
				{
					wieldedItemIndex = i;
					break;
				}
			}
		}

		public static AgentVisuals GetHumanAgentPartyVisual(Scene mapScene, MatrixFrame frame, PartyBase party, uint contourColor, ActionIndexCache leaderAction, ref bool clearBannerEntityCache, ref (string, GameEntity) cachedBannerEntity, out float animationDuration)
		{
			uint clothColor = (uint)(((int?)party.MapFaction?.Color) ?? (-3357781));
			uint clothColor2 = (uint)(((int?)party.MapFaction?.Color2) ?? (-3357781));
			CharacterObject visualPartyLeader = PartyBaseHelper.GetVisualPartyLeader(party);
			string text = null;
			if (party.LeaderHero?.ClanBanner != null)
			{
				text = party.LeaderHero.ClanBanner.BannerCode;
			}
			Equipment equipment = visualPartyLeader.Equipment.Clone();
			bool flag = !string.IsNullOrEmpty(text) && (((visualPartyLeader.IsPlayerCharacter || visualPartyLeader.HeroObject.Clan == Clan.PlayerClan) && Clan.PlayerClan.Tier >= Campaign.Current.Models.ClanTierModel.BannerEligibleTier) || (!visualPartyLeader.IsPlayerCharacter && (!visualPartyLeader.IsHero || (visualPartyLeader.IsHero && visualPartyLeader.HeroObject.Clan != Clan.PlayerClan))));
			GetMeleeWeaponToWield(party, out var wieldedItemIndex);
			int leftWieldedItemIndex = 4;
			if (flag)
			{
				ItemObject item = Game.Current.ObjectManager.GetObject<ItemObject>("campaign_banner_small");
				equipment[EquipmentIndex.ExtraWeaponSlot] = new EquipmentElement(item);
			}
			Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(visualPartyLeader.Race);
			MBActionSet actionSetWithSuffix = MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, visualPartyLeader.IsFemale, flag ? "_map_with_banner" : "_map");
			AgentVisualsData agentVisualsData = new AgentVisualsData().UseMorphAnims(useMorphAnims: true).Equipment(equipment).BodyProperties(visualPartyLeader.GetBodyProperties(visualPartyLeader.Equipment))
				.SkeletonType(visualPartyLeader.IsFemale ? SkeletonType.Female : SkeletonType.Male)
				.Scale(0.3f)
				.Frame(frame)
				.ActionSet(actionSetWithSuffix)
				.Scene(mapScene)
				.Monster(baseMonsterFromRace)
				.PrepareImmediately(prepareImmediately: false)
				.RightWieldedItemIndex(wieldedItemIndex)
				.HasClippingPlane(hasClippingPlane: true)
				.UseScaledWeapons(useScaledWeapons: true)
				.ClothColor1(clothColor)
				.ClothColor2(clothColor2)
				.CharacterObjectStringId(visualPartyLeader.StringId)
				.AddColorRandomness(!visualPartyLeader.IsHero)
				.Race(visualPartyLeader.Race);
			if (flag)
			{
				Banner banner = new Banner(text);
				agentVisualsData.Banner(banner).LeftWieldedItemIndex(leftWieldedItemIndex);
				if (cachedBannerEntity.Item1 == text + "campaign_banner_small")
				{
					agentVisualsData.CachedWeaponEntity(EquipmentIndex.ExtraWeaponSlot, cachedBannerEntity.Item2);
				}
			}
			animationDuration = ((leaderAction != ActionIndexCache.act_none) ? MBActionSet.GetActionAnimationDuration(actionSetWithSuffix, in leaderAction) : 1f);
			AgentVisuals agentVisuals = AgentVisuals.Create(agentVisualsData, "PartyIcon " + visualPartyLeader.Name, isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: false);
			if (agentVisuals != null)
			{
				if (flag)
				{
					GameEntity entity = agentVisuals.GetEntity();
					GameEntity child = entity.GetChild(entity.ChildCount - 1);
					if (child.GetComponentCount(GameEntity.ComponentType.ClothSimulator) > 0)
					{
						clearBannerEntityCache = false;
						cachedBannerEntity = (text + "campaign_banner_small", child);
					}
				}
				agentVisuals.GetWeakEntity().SetContourColor(contourColor, alwaysVisible: false);
			}
			return agentVisuals;
		}
	}
}
