using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Conversation.MissionLogics;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

namespace SandBox.View.Missions
{
	// Token: 0x02000016 RID: 22
	public class MissionConversationPrepareView : MissionView
	{
		// Token: 0x06000094 RID: 148 RVA: 0x00005AE8 File Offset: 0x00003CE8
		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			this._conversationMissionLogic = base.Mission.GetMissionBehavior<ConversationMissionLogic>();
		}

		// Token: 0x06000095 RID: 149 RVA: 0x00005B04 File Offset: 0x00003D04
		public override void AfterStart()
		{
			base.AfterStart();
			if (this._conversationMissionLogic != null)
			{
				GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("banner_with_faction_color");
				if (gameEntity != null)
				{
					if (this._conversationMissionLogic.OtherSideConversationData.Character.IsHero)
					{
						PartyBase party = this._conversationMissionLogic.OtherSideConversationData.Party;
						Banner banner;
						if ((banner = ((party != null) ? party.Banner : null)) == null)
						{
							PartyBase party2 = this._conversationMissionLogic.PlayerConversationData.Party;
							banner = ((party2 != null) ? party2.Banner : null);
						}
						Banner banner2 = banner;
						if (banner2 != null)
						{
							this.SetOwnerBanner(gameEntity, banner2);
							return;
						}
					}
					else
					{
						gameEntity.Remove(112);
					}
				}
			}
		}

		// Token: 0x06000096 RID: 150 RVA: 0x00005BAC File Offset: 0x00003DAC
		private void SetOwnerBanner(GameEntity bannerEntity, Banner ownerBanner)
		{
			BannerDebugInfo bannerDebugInfo = BannerDebugInfo.CreateManual(base.GetType().Name);
			ownerBanner.GetTableauTextureLarge(bannerDebugInfo, delegate(Texture tex)
			{
				this.OnTextureRendered(tex, bannerEntity);
			});
		}

		// Token: 0x06000097 RID: 151 RVA: 0x00005BF4 File Offset: 0x00003DF4
		private void OnTextureRendered(Texture tex, GameEntity bannerEntity)
		{
			List<Mesh> list = bannerEntity.GetAllMeshesWithTag("banner_with_faction_color").ToList<Mesh>();
			if (list.IsEmpty<Mesh>())
			{
				list.Add(bannerEntity.GetFirstMesh());
			}
			foreach (Mesh mesh in list)
			{
				Material material = mesh.GetMaterial().CreateCopy();
				material.SetTexture(Material.MBTextureType.DiffuseMap2, tex);
				uint num = (uint)material.GetShader().GetMaterialShaderFlagMask("use_tableau_blending", true);
				ulong shaderFlags = material.GetShaderFlags();
				material.SetShaderFlags(shaderFlags | (ulong)num);
				mesh.SetMaterial(material);
			}
		}

		// Token: 0x0400002F RID: 47
		public const string BannerTagId = "banner_with_faction_color";

		// Token: 0x04000030 RID: 48
		private ConversationMissionLogic _conversationMissionLogic;
	}
}
