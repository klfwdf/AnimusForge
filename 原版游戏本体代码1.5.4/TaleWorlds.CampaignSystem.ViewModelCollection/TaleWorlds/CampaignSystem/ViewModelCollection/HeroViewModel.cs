using Helpers;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection;

public class HeroViewModel : CharacterViewModel
{
	private Hero _hero;

	private bool _isDead;

	[DataSourceProperty]
	public bool IsDead
	{
		get
		{
			return _isDead;
		}
		set
		{
			if (value != _isDead)
			{
				_isDead = value;
				OnPropertyChangedWithValue(value, "IsDead");
			}
		}
	}

	public HeroViewModel(StanceTypes stance = StanceTypes.None)
		: base(stance)
	{
	}

	public override void SetEquipment(Equipment equipment)
	{
		_equipment = equipment?.Clone();
		base.HasMount = _equipment?[10].Item != null;
		base.EquipmentCode = _equipment?.CalculateEquipmentCode();
		if (_hero != null)
		{
			base.MountCreationKey = TaleWorlds.Core.MountCreationKey.GetRandomMountKeyString(equipment[10].Item, _hero.CharacterObject.GetMountKeySeed());
		}
	}

	public void FillFrom(Hero hero, int seed = -1, bool useCivilian = false, bool useCharacteristicIdleAction = false)
	{
		base.IsHidden = CampaignUIHelper.IsHeroInformationHidden(hero, out var _);
		if (FaceGen.GetMaturityTypeWithAge(hero.Age) <= BodyMeshMaturityType.Child || base.IsHidden)
		{
			return;
		}
		_hero = hero;
		FillFrom(hero.CharacterObject, seed);
		base.MountCreationKey = TaleWorlds.Core.MountCreationKey.GetRandomMountKeyString(hero.CharacterObject.Equipment[10].Item, hero.CharacterObject.GetMountKeySeed());
		IsDead = hero.IsDead;
		if ((hero.IsNoncombatant && !hero.IsPartyLeader) || useCivilian)
		{
			_equipment = hero.CivilianEquipment?.Clone();
		}
		else
		{
			_equipment = hero.BattleEquipment?.Clone();
		}
		if (useCharacteristicIdleAction)
		{
			if (Campaign.Current.ConversationManager.ConversationAnimationManager.ConversationAnims.TryGetValue(CharacterHelper.GetNonconversationPose(hero.CharacterObject), out var value))
			{
				base.IdleAction = value.IdleAnimLoop;
			}
			base.IdleFaceAnim = CharacterHelper.GetNonconversationFacialIdle(hero.CharacterObject) ?? "";
		}
		base.EquipmentCode = _equipment?.CalculateEquipmentCode();
		base.HasMount = _equipment?[10].Item != null;
		if (hero?.ClanBanner != null)
		{
			base.BannerCodeText = hero.ClanBanner.BannerCode;
		}
		base.ArmorColor1 = hero.MapFaction?.Color ?? 0;
		base.ArmorColor2 = hero.MapFaction?.Color2 ?? 0;
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		_hero = null;
	}
}
