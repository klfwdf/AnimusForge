using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.ShipActuators;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

namespace NavalDLC.View.MissionViews;

public class NavalMissionPrepareView : MissionView
{
	private NavalShipsLogic _navalShipsLogic;

	private string BannerTag => "banner_with_faction_color";

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_navalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		_navalShipsLogic.ShipSpawnedEvent += OnShipSpawned;
		_navalShipsLogic.ShipCapturedEvent += StartBannerChangeAnimationForShip;
	}

	public void OnShipSpawned(MissionShip missionShip)
	{
		foreach (GameEntity bannerEntity in missionShip.BannerEntities)
		{
			SetOwnerBanner(bannerEntity, missionShip.Banner);
		}
		foreach (GameEntity sailMeshEntity in missionShip.SailMeshEntities)
		{
			var (sailColor, sailColor2) = missionShip.SailColors;
			SetSailColors(sailMeshEntity, sailColor, sailColor2);
		}
	}

	private void SetSailColors(GameEntity sailEntity, uint sailColor1, uint sailColor2)
	{
		if (sailEntity.Skeleton != null)
		{
			foreach (Mesh allMesh in sailEntity.Skeleton.GetAllMeshes())
			{
				if (allMesh.HasTag("faction_color"))
				{
					allMesh.Color = sailColor1;
					allMesh.Color2 = sailColor2;
				}
			}
		}
		foreach (Mesh item in sailEntity.WeakEntity.GetAllMeshesWithTag("faction_color"))
		{
			item.Color = sailColor1;
			item.Color2 = sailColor2;
		}
	}

	private void SetOwnerBanner(GameEntity bannerEntity, Banner ownerBanner)
	{
		ownerBanner.GetTableauTextureLarge(BannerDebugInfo.CreateManual(GetType().Name), delegate(Texture tex)
		{
			OnTextureRendered(tex, bannerEntity);
		});
	}

	private void OnTextureRendered(Texture tex, GameEntity bannerEntity)
	{
		List<Mesh> list = bannerEntity.GetAllMeshesWithTag(BannerTag).ToList();
		if (list.IsEmpty())
		{
			list.Add(bannerEntity.GetFirstMesh());
		}
		foreach (Mesh item in list)
		{
			if (item != null)
			{
				Material material = item.GetMaterial().CreateCopy();
				material.SetTexture(Material.MBTextureType.DiffuseMap2, tex);
				uint num = (uint)material.GetShader().GetMaterialShaderFlagMask("use_tableau_blending");
				ulong shaderFlags = material.GetShaderFlags();
				material.SetShaderFlags(shaderFlags | num);
				item.SetMaterial(material);
			}
		}
	}

	public override void OnRemoveBehavior()
	{
		base.OnRemoveBehavior();
		_navalShipsLogic.ShipSpawnedEvent -= OnShipSpawned;
		_navalShipsLogic.ShipCapturedEvent -= StartBannerChangeAnimationForShip;
	}

	public void StartBannerChangeAnimationForShip(MissionShip ship, MissionShip ship2, Formation formation, Formation formation2)
	{
		ship.Banner.GetTableauTextureLarge(BannerDebugInfo.CreateManual(GetType().Name), delegate(Texture tex)
		{
			OnCaptureBannerTextureRendered(tex, ship);
		});
	}

	private void OnCaptureBannerTextureRendered(Texture newTexture, MissionShip ship)
	{
		foreach (MissionSail sail in ship.Sails)
		{
			sail.StartShipCaptureAnimation(newTexture);
		}
	}
}
