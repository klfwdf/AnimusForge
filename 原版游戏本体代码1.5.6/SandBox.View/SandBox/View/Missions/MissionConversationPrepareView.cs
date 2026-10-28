using System.Collections.Generic;
using System.Linq;
using SandBox.Conversation.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

namespace SandBox.View.Missions;

public class MissionConversationPrepareView : MissionView
{
	public const string BannerTagId = "banner_with_faction_color";

	private ConversationMissionLogic _conversationMissionLogic;

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_conversationMissionLogic = base.Mission.GetMissionBehavior<ConversationMissionLogic>();
	}

	public override void AfterStart()
	{
		base.AfterStart();
		if (_conversationMissionLogic == null)
		{
			return;
		}
		GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("banner_with_faction_color");
		if (!(gameEntity != null))
		{
			return;
		}
		if (_conversationMissionLogic.OtherSideConversationData.Character.IsHero)
		{
			Banner banner = _conversationMissionLogic.OtherSideConversationData.Party?.Banner ?? _conversationMissionLogic.PlayerConversationData.Party?.Banner;
			if (banner != null)
			{
				SetOwnerBanner(gameEntity, banner);
			}
		}
		else
		{
			gameEntity.Remove(112);
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
		List<Mesh> list = bannerEntity.GetAllMeshesWithTag("banner_with_faction_color").ToList();
		if (list.IsEmpty())
		{
			list.Add(bannerEntity.GetFirstMesh());
		}
		foreach (Mesh item in list)
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
