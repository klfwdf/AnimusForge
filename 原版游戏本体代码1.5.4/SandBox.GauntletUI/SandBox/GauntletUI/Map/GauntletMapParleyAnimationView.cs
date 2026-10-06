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

namespace SandBox.GauntletUI.Map;

[OverrideView(typeof(MapParleyAnimationView))]
public class GauntletMapParleyAnimationView : MapParleyAnimationView
{
	private readonly PartyBase _parleyedParty;

	private CampaignTimeControlMode _previousTimeControlMode;

	private const float _animationDuration = 1f;

	private float _remainingAnimationDuration;

	private readonly IParleyCampaignBehavior _behavior;

	private GameEntity _playerBannerEntity;

	private GameEntity _targetBannerEntity;

	private Vec3 _bannerTargetPosition;

	private MapEntityVisual<PartyBase> _mainPartyVisual;

	private MapEntityVisual<PartyBase> _parleyedPartyVisual;

	public GauntletMapParleyAnimationView(PartyBase parleyedParty)
	{
		_parleyedParty = parleyedParty;
		_behavior = Campaign.Current.GetCampaignBehavior<IParleyCampaignBehavior>();
		foreach (EntityVisualManagerBase<PartyBase> component in SandBoxViewSubModule.SandBoxViewVisualManager.GetComponents<EntityVisualManagerBase<PartyBase>>())
		{
			MapEntityVisual<PartyBase> visualOfEntity = component.GetVisualOfEntity(PartyBase.MainParty);
			MapEntityVisual<PartyBase> visualOfEntity2 = component.GetVisualOfEntity(_parleyedParty);
			if (visualOfEntity != null)
			{
				_mainPartyVisual = visualOfEntity;
			}
			if (visualOfEntity2 != null)
			{
				_parleyedPartyVisual = visualOfEntity2;
			}
		}
	}

	protected override void CreateLayout()
	{
		base.CreateLayout();
		_remainingAnimationDuration = 1f;
		CreateBanners();
		MBInformationManager.AddQuickInformation(new TextObject("{=LZbHWkCB}Parleying with {PARTY_NAME}").SetTextVariable("PARTY_NAME", _parleyedParty.Name), -750);
		_previousTimeControlMode = Campaign.Current.TimeControlMode;
		Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
		Campaign.Current.SetTimeControlModeLock(isLocked: true);
	}

	private void CreateBanners()
	{
		_playerBannerEntity = CreateAnimationBannerEntity(PartyBase.MainParty, _mainPartyVisual);
		_targetBannerEntity = CreateAnimationBannerEntity(_parleyedParty, _parleyedPartyVisual);
		if (_parleyedParty.IsSettlement)
		{
			_bannerTargetPosition = _targetBannerEntity.GetFrame().origin;
		}
		else
		{
			_bannerTargetPosition = Vec3.Lerp(_playerBannerEntity.GetFrame().origin, _targetBannerEntity.GetFrame().origin, 0.5f);
		}
		RotateBannersTowardsEachother(_playerBannerEntity, _targetBannerEntity, _bannerTargetPosition);
		float num = 0.7f;
		Vec3 scaleVector = new Vec3(num, num, num);
		ScaleBanner(_playerBannerEntity, scaleVector);
		ScaleBanner(_targetBannerEntity, scaleVector);
	}

	private GameEntity CreateAnimationBannerEntity(PartyBase party, MapEntityVisual<PartyBase> partyVisual)
	{
		GameEntity gameEntity = GameEntity.CreateEmpty(base.MapScreen.MapScene, isModifiableFromEditor: false);
		MetaMesh copy = MetaMesh.GetCopy("map_banner");
		gameEntity.AddMultiMesh(copy);
		MatrixFrame frame = MatrixFrame.Identity;
		frame.origin = partyVisual.GetVisualPosition();
		gameEntity.SetFrame(ref frame);
		return gameEntity;
	}

	private void RotateBannersTowardsEachother(GameEntity playerBanner, GameEntity targetBanner, Vec3 bannerTargetPosition)
	{
		MatrixFrame frame = playerBanner.GetFrame();
		MatrixFrame frame2 = targetBanner.GetFrame();
		Vec3 f = bannerTargetPosition - frame.origin;
		frame.rotation.f = f;
		frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		frame.rotation.RotateAboutUp(System.MathF.PI);
		frame2.rotation = frame.rotation;
		frame2.rotation.RotateAboutUp(System.MathF.PI);
		playerBanner.SetFrame(ref frame);
		targetBanner.SetFrame(ref frame2);
	}

	private void ScaleBanner(GameEntity bannerEntity, Vec3 scaleVector)
	{
		MatrixFrame frame = bannerEntity.GetFrame();
		frame.Scale(in scaleVector);
		bannerEntity.SetFrame(ref frame);
	}

	private void DestroyAnimationBannerEntities()
	{
		_playerBannerEntity?.Remove(0);
		_targetBannerEntity?.Remove(0);
		_playerBannerEntity = null;
		_targetBannerEntity = null;
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		Tick(dt);
	}

	protected override void OnIdleTick(float dt)
	{
		base.OnIdleTick(dt);
		Tick(dt);
	}

	private void Tick(float dt)
	{
		if (_remainingAnimationDuration <= 0f)
		{
			base.MapScreen.RemoveMapView(this);
			_behavior?.StartParley(_parleyedParty);
			return;
		}
		float alpha = TaleWorlds.Library.MathF.Clamp((1f - _remainingAnimationDuration) / 1f, 0f, 1f);
		Vec3 visualPosition = _mainPartyVisual.GetVisualPosition();
		Vec3 visualPosition2 = _parleyedPartyVisual.GetVisualPosition();
		MatrixFrame frame = _playerBannerEntity.GetFrame();
		MatrixFrame frame2 = _targetBannerEntity.GetFrame();
		frame.origin = Vec3.Lerp(visualPosition, _bannerTargetPosition, alpha);
		frame2.origin = Vec3.Lerp(visualPosition2, _bannerTargetPosition, alpha);
		_playerBannerEntity.SetFrame(ref frame);
		_targetBannerEntity.SetFrame(ref frame2);
		_remainingAnimationDuration -= dt;
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		DestroyAnimationBannerEntities();
		Campaign.Current.SetTimeControlModeLock(isLocked: false);
		Campaign.Current.TimeControlMode = _previousTimeControlMode;
	}
}
