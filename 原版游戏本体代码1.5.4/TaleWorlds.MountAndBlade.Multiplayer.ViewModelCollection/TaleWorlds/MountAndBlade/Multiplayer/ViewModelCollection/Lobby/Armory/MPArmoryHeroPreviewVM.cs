using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Armory;

public class MPArmoryHeroPreviewVM : ViewModel
{
	private BasicCharacterObject _character;

	private Equipment _orgEquipmentWithoutPerks;

	private CharacterViewModel _heroVisual;

	private string _className;

	[DataSourceProperty]
	public CharacterViewModel HeroVisual
	{
		get
		{
			return _heroVisual;
		}
		set
		{
			if (value != _heroVisual)
			{
				_heroVisual = value;
				OnPropertyChangedWithValue(value, "HeroVisual");
			}
		}
	}

	[DataSourceProperty]
	public string ClassName
	{
		get
		{
			return _className;
		}
		set
		{
			if (value != _className)
			{
				_className = value;
				OnPropertyChangedWithValue(value, "ClassName");
			}
		}
	}

	public MPArmoryHeroPreviewVM()
	{
		HeroVisual = new CharacterViewModel();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ClassName = _character?.Name.ToString() ?? "";
	}

	public void SetCharacter(BasicCharacterObject character, DynamicBodyProperties dynamicBodyProperties, int race, bool isFemale)
	{
		_character = character;
		HeroVisual.FillFrom(character);
		HeroVisual.BodyProperties = new BodyProperties(dynamicBodyProperties, character.BodyPropertyRange.BodyPropertyMin.StaticProperties).ToString();
		HeroVisual.IsFemale = isFemale;
		HeroVisual.Race = race;
		ClassName = character.Name.ToString();
	}

	public void SetCharacterClass(BasicCharacterObject classCharacter)
	{
		_character = classCharacter;
		_orgEquipmentWithoutPerks = classCharacter.Equipment;
		HeroVisual.SetEquipment(_orgEquipmentWithoutPerks);
		HeroVisual.ArmorColor1 = classCharacter.Culture.Color;
		HeroVisual.ArmorColor2 = classCharacter.Culture.Color2;
		if (NetworkMain.GameClient.PlayerData != null)
		{
			string sigil = NetworkMain.GameClient.PlayerData.Sigil;
			if (NetworkMain.GameClient.PlayerData.IsUsingClanSigil && NetworkMain.GameClient.ClanInfo != null)
			{
				sigil = NetworkMain.GameClient.ClanInfo.Sigil;
			}
			Banner banner = new Banner(sigil, classCharacter.Culture.BackgroundColor1, classCharacter.Culture.ForegroundColor1);
			HeroVisual.BannerCodeText = banner.BannerCode;
		}
		ClassName = classCharacter.Name.ToString();
	}

	public void SetCharacterPerks(List<IReadOnlyPerkObject> selectedPerks)
	{
		Equipment equipment = _orgEquipmentWithoutPerks.Clone();
		MPArmoryVM.ApplyPerkEffectsToEquipment(ref equipment, selectedPerks);
		HeroVisual.SetEquipment(equipment);
	}
}
