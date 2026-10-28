using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Encounters;

public class LocationEncounter
{
	public bool IsInsideOfASettlement;

	public Settlement Settlement { get; }

	public List<AccompanyingCharacter> CharactersAccompanyingPlayer { get; private set; }

	protected LocationEncounter(Settlement settlement)
	{
		Settlement = settlement;
		CharactersAccompanyingPlayer = new List<AccompanyingCharacter>();
	}

	public void AddAccompanyingCharacter(LocationCharacter locationCharacter, bool isFollowing = false)
	{
		if (!CharactersAccompanyingPlayer.Any((AccompanyingCharacter x) => x.LocationCharacter.Character == locationCharacter.Character))
		{
			AccompanyingCharacter item = new AccompanyingCharacter(locationCharacter, isFollowing);
			CharactersAccompanyingPlayer.Add(item);
		}
	}

	public AccompanyingCharacter GetAccompanyingCharacter(LocationCharacter locationCharacter)
	{
		return CharactersAccompanyingPlayer.Find((AccompanyingCharacter x) => x.LocationCharacter == locationCharacter);
	}

	public AccompanyingCharacter GetAccompanyingCharacter(CharacterObject character)
	{
		return CharactersAccompanyingPlayer.Find((AccompanyingCharacter x) => x.LocationCharacter?.Character == character);
	}

	public void RemoveAccompanyingCharacter(LocationCharacter locationCharacter)
	{
		if (CharactersAccompanyingPlayer.Any((AccompanyingCharacter x) => x.LocationCharacter == locationCharacter))
		{
			AccompanyingCharacter item = CharactersAccompanyingPlayer.Find((AccompanyingCharacter x) => x.LocationCharacter == locationCharacter);
			CharactersAccompanyingPlayer.Remove(item);
		}
	}

	public void RemoveAccompanyingCharacter(Hero hero)
	{
		for (int num = CharactersAccompanyingPlayer.Count - 1; num >= 0; num--)
		{
			if (CharactersAccompanyingPlayer[num].LocationCharacter.Character.IsHero && CharactersAccompanyingPlayer[num].LocationCharacter.Character.HeroObject == hero)
			{
				CharactersAccompanyingPlayer.Remove(CharactersAccompanyingPlayer[num]);
				break;
			}
		}
	}

	public void RemoveAllAccompanyingCharacters()
	{
		CharactersAccompanyingPlayer.Clear();
	}

	public void OnCharacterLocationChanged(LocationCharacter locationCharacter, Location fromLocation, Location toLocation)
	{
		if ((fromLocation == CampaignMission.Current.Location && toLocation == null) || (fromLocation == null && toLocation == CampaignMission.Current.Location))
		{
			CampaignMission.Current.OnCharacterLocationChanged(locationCharacter, fromLocation, toLocation);
		}
	}

	public virtual bool IsWorkshopLocation(Location location)
	{
		return false;
	}

	public virtual bool IsTavern(Location location)
	{
		return false;
	}

	public virtual IMission CreateAndOpenMissionController(Location nextLocation, Location previousLocation = null, CharacterObject talkToChar = null, string playerSpecialSpawnTag = null)
	{
		return null;
	}
}
