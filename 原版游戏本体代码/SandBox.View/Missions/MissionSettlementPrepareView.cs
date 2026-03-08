using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

namespace SandBox.View.Missions
{
	// Token: 0x02000020 RID: 32
	[DefaultView]
	public class MissionSettlementPrepareView : MissionView
	{
		// Token: 0x060000CF RID: 207 RVA: 0x00009F2E File Offset: 0x0000812E
		public override void AfterStart()
		{
			base.AfterStart();
			this.SetOwnerBanner();
		}

		// Token: 0x060000D0 RID: 208 RVA: 0x00009F3C File Offset: 0x0000813C
		private void SetOwnerBanner()
		{
			Campaign campaign = Campaign.Current;
			if (campaign != null && campaign.GameMode == CampaignGameMode.Campaign)
			{
				Settlement currentSettlement = Settlement.CurrentSettlement;
				bool flag;
				if (currentSettlement == null)
				{
					flag = null != null;
				}
				else
				{
					Clan ownerClan = currentSettlement.OwnerClan;
					flag = ((ownerClan != null) ? ownerClan.Banner : null) != null;
				}
				if (flag && base.Mission.Scene != null)
				{
					foreach (GameEntity gameEntity in base.Mission.Scene.FindEntitiesWithTag("bd_banner_b"))
					{
						Action<Texture> setAction = delegate(Texture tex)
						{
							Material material = Mesh.GetFromResource("bd_banner_b").GetMaterial();
							uint num = (uint)material.GetShader().GetMaterialShaderFlagMask("use_tableau_blending", true);
							ulong shaderFlags = material.GetShaderFlags();
							material.SetShaderFlags(shaderFlags | (ulong)num);
							material.SetTexture(Material.MBTextureType.DiffuseMap2, tex);
						};
						Banner banner = Settlement.CurrentSettlement.OwnerClan.Banner;
						BannerDebugInfo bannerDebugInfo = BannerDebugInfo.CreateManual(base.GetType().Name);
						banner.GetTableauTextureLarge(bannerDebugInfo, setAction);
					}
				}
			}
		}

		// Token: 0x0400007D RID: 125
		public const string BannerTagId = "bd_banner_b";
	}
}
