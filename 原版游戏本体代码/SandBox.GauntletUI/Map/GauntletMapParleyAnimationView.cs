using System;
using SandBox.View;
using SandBox.View.Map;
using SandBox.View.Map.Managers;
using SandBox.View.Map.Visuals;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.View;

namespace SandBox.GauntletUI.Map
{
	// Token: 0x0200003D RID: 61
	[OverrideView(typeof(MapParleyAnimationView))]
	public class GauntletMapParleyAnimationView : MapParleyAnimationView
	{
		// Token: 0x060002D9 RID: 729 RVA: 0x00010CB8 File Offset: 0x0000EEB8
		public GauntletMapParleyAnimationView(PartyBase parleyedParty)
		{
			this._parleyedParty = parleyedParty;
			this._behavior = Campaign.Current.GetCampaignBehavior<IParleyCampaignBehavior>();
			foreach (EntityVisualManagerBase<PartyBase> entityVisualManagerBase in SandBoxViewSubModule.SandBoxViewVisualManager.GetComponents<EntityVisualManagerBase<PartyBase>>())
			{
				MapEntityVisual<PartyBase> visualOfEntity = entityVisualManagerBase.GetVisualOfEntity(PartyBase.MainParty);
				MapEntityVisual<PartyBase> visualOfEntity2 = entityVisualManagerBase.GetVisualOfEntity(this._parleyedParty);
				if (visualOfEntity != null)
				{
					this._mainPartyVisual = visualOfEntity;
				}
				if (visualOfEntity2 != null)
				{
					this._parleyedPartyVisual = visualOfEntity2;
				}
			}
		}

		// Token: 0x060002DA RID: 730 RVA: 0x00010D50 File Offset: 0x0000EF50
		protected override void CreateLayout()
		{
			base.CreateLayout();
			this._remainingAnimationDuration = 1f;
			this.CreateBanners();
			MBInformationManager.AddQuickInformation(new TextObject("{=LZbHWkCB}Parleying with {PARTY_NAME}", null).SetTextVariable("PARTY_NAME", this._parleyedParty.Name), -750, null, null, "");
			this._previousTimeControlMode = Campaign.Current.TimeControlMode;
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			Campaign.Current.SetTimeControlModeLock(true);
		}

		// Token: 0x060002DB RID: 731 RVA: 0x00010DCC File Offset: 0x0000EFCC
		private void CreateBanners()
		{
			this._playerBannerEntity = this.CreateAnimationBannerEntity(PartyBase.MainParty, this._mainPartyVisual);
			this._targetBannerEntity = this.CreateAnimationBannerEntity(this._parleyedParty, this._parleyedPartyVisual);
			if (this._parleyedParty.IsSettlement)
			{
				this._bannerTargetPosition = this._targetBannerEntity.GetFrame().origin;
			}
			else
			{
				this._bannerTargetPosition = Vec3.Lerp(this._playerBannerEntity.GetFrame().origin, this._targetBannerEntity.GetFrame().origin, 0.5f);
			}
			this.RotateBannersTowardsEachother(this._playerBannerEntity, this._targetBannerEntity, this._bannerTargetPosition);
			float num = 0.7f;
			Vec3 scaleVector = new Vec3(num, num, num, -1f);
			this.ScaleBanner(this._playerBannerEntity, scaleVector);
			this.ScaleBanner(this._targetBannerEntity, scaleVector);
		}

		// Token: 0x060002DC RID: 732 RVA: 0x00010EA4 File Offset: 0x0000F0A4
		private GameEntity CreateAnimationBannerEntity(PartyBase party, MapEntityVisual<PartyBase> partyVisual)
		{
			GameEntity gameEntity = GameEntity.CreateEmpty(base.MapScreen.MapScene, false, true, true);
			MetaMesh copy = MetaMesh.GetCopy("map_banner", true, false);
			gameEntity.AddMultiMesh(copy, true);
			MatrixFrame identity = MatrixFrame.Identity;
			identity.origin = partyVisual.GetVisualPosition();
			gameEntity.SetFrame(ref identity, true);
			return gameEntity;
		}

		// Token: 0x060002DD RID: 733 RVA: 0x00010EF8 File Offset: 0x0000F0F8
		private void RotateBannersTowardsEachother(GameEntity playerBanner, GameEntity targetBanner, Vec3 bannerTargetPosition)
		{
			MatrixFrame frame = playerBanner.GetFrame();
			MatrixFrame frame2 = targetBanner.GetFrame();
			Vec3 f = bannerTargetPosition - frame.origin;
			frame.rotation.f = f;
			frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			frame.rotation.RotateAboutUp(3.1415927f);
			frame2.rotation = frame.rotation;
			frame2.rotation.RotateAboutUp(3.1415927f);
			playerBanner.SetFrame(ref frame, true);
			targetBanner.SetFrame(ref frame2, true);
		}

		// Token: 0x060002DE RID: 734 RVA: 0x00010F7C File Offset: 0x0000F17C
		private void ScaleBanner(GameEntity bannerEntity, Vec3 scaleVector)
		{
			MatrixFrame frame = bannerEntity.GetFrame();
			frame.Scale(scaleVector);
			bannerEntity.SetFrame(ref frame, true);
		}

		// Token: 0x060002DF RID: 735 RVA: 0x00010FA2 File Offset: 0x0000F1A2
		private void DestroyAnimationBannerEntities()
		{
			GameEntity playerBannerEntity = this._playerBannerEntity;
			if (playerBannerEntity != null)
			{
				playerBannerEntity.Remove(0);
			}
			GameEntity targetBannerEntity = this._targetBannerEntity;
			if (targetBannerEntity != null)
			{
				targetBannerEntity.Remove(0);
			}
			this._playerBannerEntity = null;
			this._targetBannerEntity = null;
		}

		// Token: 0x060002E0 RID: 736 RVA: 0x00010FD6 File Offset: 0x0000F1D6
		protected override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			this.Tick(dt);
		}

		// Token: 0x060002E1 RID: 737 RVA: 0x00010FE6 File Offset: 0x0000F1E6
		protected override void OnIdleTick(float dt)
		{
			base.OnIdleTick(dt);
			this.Tick(dt);
		}

		// Token: 0x060002E2 RID: 738 RVA: 0x00010FF8 File Offset: 0x0000F1F8
		private void Tick(float dt)
		{
			if (this._remainingAnimationDuration > 0f)
			{
				float alpha = MathF.Clamp((1f - this._remainingAnimationDuration) / 1f, 0f, 1f);
				Vec3 visualPosition = this._mainPartyVisual.GetVisualPosition();
				Vec3 visualPosition2 = this._parleyedPartyVisual.GetVisualPosition();
				MatrixFrame frame = this._playerBannerEntity.GetFrame();
				MatrixFrame frame2 = this._targetBannerEntity.GetFrame();
				frame.origin = Vec3.Lerp(visualPosition, this._bannerTargetPosition, alpha);
				frame2.origin = Vec3.Lerp(visualPosition2, this._bannerTargetPosition, alpha);
				this._playerBannerEntity.SetFrame(ref frame, true);
				this._targetBannerEntity.SetFrame(ref frame2, true);
				this._remainingAnimationDuration -= dt;
				return;
			}
			base.MapScreen.RemoveMapView(this);
			IParleyCampaignBehavior behavior = this._behavior;
			if (behavior == null)
			{
				return;
			}
			behavior.StartParley(this._parleyedParty);
		}

		// Token: 0x060002E3 RID: 739 RVA: 0x000110DA File Offset: 0x0000F2DA
		protected override void OnFinalize()
		{
			base.OnFinalize();
			this.DestroyAnimationBannerEntities();
			Campaign.Current.SetTimeControlModeLock(false);
			Campaign.Current.TimeControlMode = this._previousTimeControlMode;
		}

		// Token: 0x04000114 RID: 276
		private readonly PartyBase _parleyedParty;

		// Token: 0x04000115 RID: 277
		private CampaignTimeControlMode _previousTimeControlMode;

		// Token: 0x04000116 RID: 278
		private const float _animationDuration = 1f;

		// Token: 0x04000117 RID: 279
		private float _remainingAnimationDuration;

		// Token: 0x04000118 RID: 280
		private readonly IParleyCampaignBehavior _behavior;

		// Token: 0x04000119 RID: 281
		private GameEntity _playerBannerEntity;

		// Token: 0x0400011A RID: 282
		private GameEntity _targetBannerEntity;

		// Token: 0x0400011B RID: 283
		private Vec3 _bannerTargetPosition;

		// Token: 0x0400011C RID: 284
		private MapEntityVisual<PartyBase> _mainPartyVisual;

		// Token: 0x0400011D RID: 285
		private MapEntityVisual<PartyBase> _parleyedPartyVisual;
	}
}
