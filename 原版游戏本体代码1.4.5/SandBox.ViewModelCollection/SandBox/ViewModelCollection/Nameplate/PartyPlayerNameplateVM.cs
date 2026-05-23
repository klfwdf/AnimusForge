using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.Nameplate;

public class PartyPlayerNameplateVM : PartyNameplateVM
{
	private float _latestMainHeroAge = -1f;

	private bool _isPartyHeroVisualDirty;

	private Action _resetCamera;

	private CharacterImageIdentifierVM _mainHeroVisualBind;

	private bool _isPrisonerBind;

	private bool _isMainParty;

	private bool _isPrisoner;

	private CharacterImageIdentifierVM _mainHeroVisual;

	[DataSourceProperty]
	public bool IsMainParty
	{
		get
		{
			return _isMainParty;
		}
		set
		{
			if (value != _isMainParty)
			{
				_isMainParty = value;
				OnPropertyChangedWithValue(value, "IsMainParty");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPrisoner
	{
		get
		{
			return _isPrisoner;
		}
		set
		{
			if (value != _isPrisoner)
			{
				_isPrisoner = value;
				OnPropertyChangedWithValue(value, "IsPrisoner");
			}
		}
	}

	[DataSourceProperty]
	public CharacterImageIdentifierVM MainHeroVisual
	{
		get
		{
			return _mainHeroVisual;
		}
		set
		{
			if (value != _mainHeroVisual)
			{
				_mainHeroVisual = value;
				OnPropertyChangedWithValue(value, "MainHeroVisual");
			}
		}
	}

	public PartyPlayerNameplateVM()
	{
		IsMainParty = true;
	}

	public void InitializePlayerNameplate(Action resetCamera)
	{
		_isPartyHeroVisualDirty = true;
		_resetCamera = resetCamera;
		_isPrisonerBind = IsMainParty && base.Party.LeaderHero == null && (Hero.MainHero?.IsAlive ?? false);
		MainHeroVisual = new CharacterImageIdentifierVM(CampaignUIHelper.GetCharacterCode(Hero.MainHero.CharacterObject));
	}

	public override void Clear()
	{
		base.Clear();
		base.IsInSettlement = true;
		base.IsVisibleOnMap = false;
		MainHeroVisual = null;
	}

	public override void RefreshDynamicProperties(bool forceUpdate)
	{
		base.RefreshDynamicProperties(forceUpdate);
		if ((IsMainParty && TaleWorlds.Library.MathF.Abs(Hero.MainHero.Age - _latestMainHeroAge) >= 1f) || forceUpdate)
		{
			_latestMainHeroAge = Hero.MainHero.Age;
			_isPartyHeroVisualDirty = true;
		}
		if (_isPartyHeroVisualDirty || forceUpdate)
		{
			_mainHeroVisualBind = new CharacterImageIdentifierVM(SandBoxUIHelper.GetCharacterCode(Hero.MainHero.CharacterObject));
			_isPartyHeroVisualDirty = false;
		}
		_isPrisonerBind = IsMainParty && base.Party.LeaderHero == null && (Hero.MainHero?.IsAlive ?? false);
	}

	public override void RefreshBinding()
	{
		base.RefreshBinding();
		IsPrisoner = _isPrisonerBind;
	}

	public override void RefreshPosition()
	{
		Vec3 vec = (base.Party.Position + base.Party.EventPositionAdder).AsVec3();
		Vec3 worldSpacePosition = vec + new Vec3(0f, 0f, 0.8f);
		_latestX = 0f;
		_latestY = 0f;
		_latestW = 0f;
		MBWindowManager.WorldToScreenInsideUsableArea(_mapCamera, vec, ref _latestX, ref _latestY, ref _latestW);
		_partyPositionBind = new Vec2(_latestX, _latestY);
		_isHighBind = _mapCamera.Position.Distance(vec) >= 110f;
		_isBehindBind = _latestW < 0f;
		MBWindowManager.WorldToScreenInsideUsableArea(_mapCamera, worldSpacePosition, ref _latestX, ref _latestY, ref _latestW);
		_headPositionBind = new Vec2(_latestX, _latestY);
		base.DistanceToCamera = vec.Distance(_mapCamera.Position);
	}

	public void ExecuteSetCameraPosition()
	{
		_resetCamera();
	}
}
