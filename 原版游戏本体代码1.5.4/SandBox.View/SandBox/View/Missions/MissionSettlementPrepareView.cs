using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

namespace SandBox.View.Missions;

[DefaultView]
public class MissionSettlementPrepareView : MissionView
{
	public const string BannerTagId = "bd_banner_b";

	public override void AfterStart()
	{
		base.AfterStart();
		SetOwnerBanner();
	}

	private void SetOwnerBanner()
	{
		Campaign current = Campaign.Current;
		if (current == null || current.GameMode != CampaignGameMode.Campaign)
		{
			return;
		}
		Clan clan = ((Settlement.CurrentSettlement != null) ? Settlement.CurrentSettlement.OwnerClan : Campaign.Current.GetCampaignBehavior<IParleyCampaignBehavior>().GetParleyedParty()?.Owner?.Clan);
		if (clan?.Banner == null || !(base.Mission.Scene != null))
		{
			return;
		}
		foreach (GameEntity item in base.Mission.Scene.FindEntitiesWithTag("bd_banner_b"))
		{
			_ = item;
			Action<Texture> setAction = delegate(Texture tex)
			{
				Material material = Mesh.GetFromResource("bd_banner_b").GetMaterial();
				uint num = (uint)material.GetShader().GetMaterialShaderFlagMask("use_tableau_blending");
				ulong shaderFlags = material.GetShaderFlags();
				material.SetShaderFlags(shaderFlags | num);
				material.SetTexture(Material.MBTextureType.DiffuseMap2, tex);
			};
			clan.Banner.GetTableauTextureLarge(BannerDebugInfo.CreateManual(GetType().Name), setAction);
		}
	}
}
