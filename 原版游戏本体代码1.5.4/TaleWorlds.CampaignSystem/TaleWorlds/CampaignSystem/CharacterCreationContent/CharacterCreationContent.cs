using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CharacterCreationContent;

public sealed class CharacterCreationContent
{
	public delegate bool TryGetEquipmentIdDelegate(string occupationId, out string equipmentId);

	public int FocusToAdd = 1;

	public int SkillLevelToAdd = 10;

	public int AttributeLevelToAdd = 1;

	public int StartingAge = 20;

	private readonly Dictionary<CultureObject, KeyValuePair<int, int>> _characterCreationCultures = new Dictionary<CultureObject, KeyValuePair<int, int>>();

	private readonly List<TryGetEquipmentIdDelegate> _tryGetEquipmentIdDelegates = new List<TryGetEquipmentIdDelegate>();

	public string SelectedTitleType { get; set; }

	public string SelectedParentOccupation { get; private set; }

	public string DefaultSelectedTitleType { get; set; }

	public TextObject ReviewPageDescription { get; private set; }

	public string MainCharacterName { get; private set; }

	public CultureObject SelectedCulture { get; private set; }

	public Banner SelectedBanner { get; private set; }

	public CharacterCreationContent()
	{
		SetMainHeroInitialStats();
	}

	public void AddCharacterCreationCulture(CultureObject culture, int focusToAddByCulture, int skillLevelToAddByCulture)
	{
		if (!_characterCreationCultures.ContainsKey(culture))
		{
			_characterCreationCultures.Add(culture, new KeyValuePair<int, int>(focusToAddByCulture, skillLevelToAddByCulture));
		}
		else
		{
			_characterCreationCultures[culture] = new KeyValuePair<int, int>(focusToAddByCulture, skillLevelToAddByCulture);
		}
	}

	public int GetFocusToAddByCulture(CultureObject culture)
	{
		return _characterCreationCultures[culture].Key;
	}

	public int GetSkillLevelToAddByCulture(CultureObject culture)
	{
		return _characterCreationCultures[culture].Value;
	}

	public void ChangeReviewPageDescription(TextObject reviewPageDescription)
	{
		ReviewPageDescription = reviewPageDescription;
	}

	public void SetMainCharacterName(string name)
	{
		MainCharacterName = name;
	}

	public void SetParentOccupation(string occupationType)
	{
		SelectedParentOccupation = occupationType;
	}

	public void ApplySkillAndAttributeEffects(List<SkillObject> skills, int focusToAdd, int skillLevelToAdd, CharacterAttribute attribute, int attributeLevelToAdd, List<TraitObject> traits = null, int traitLevelToAdd = 0, int renownToAdd = 0, int goldToAdd = 0, int unspentFocusPoints = 0, int unspentAttributePoints = 0)
	{
		foreach (SkillObject skill in skills)
		{
			Hero.MainHero.HeroDeveloper.AddFocus(skill, focusToAdd, checkUnspentFocusPoints: false);
			if (Hero.MainHero.GetSkillValue(skill) == 1)
			{
				Hero.MainHero.HeroDeveloper.ChangeSkillLevel(skill, skillLevelToAdd - 1, shouldNotify: false);
			}
			else
			{
				Hero.MainHero.HeroDeveloper.ChangeSkillLevel(skill, skillLevelToAdd, shouldNotify: false);
			}
		}
		Hero.MainHero.HeroDeveloper.UnspentFocusPoints += unspentFocusPoints;
		Hero.MainHero.HeroDeveloper.UnspentAttributePoints += unspentAttributePoints;
		if (attribute != null)
		{
			Hero.MainHero.HeroDeveloper.AddAttribute(attribute, attributeLevelToAdd, checkUnspentPoints: false);
		}
		if (traits != null && traitLevelToAdd > 0 && traits.Count > 0)
		{
			foreach (TraitObject trait in traits)
			{
				Hero.MainHero.SetTraitLevel(trait, Hero.MainHero.GetTraitLevel(trait) + traitLevelToAdd);
			}
		}
		if (renownToAdd > 0)
		{
			GainRenownAction.Apply(Hero.MainHero, renownToAdd, doNotNotify: true);
		}
		if (goldToAdd > 0)
		{
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, goldToAdd, disableNotification: true);
		}
		Hero.MainHero.HeroDeveloper.ResetTotalXpForPlayerCharacter();
	}

	public void SetMainClanBanner(Banner banner)
	{
		SelectedBanner = banner;
	}

	public void SetSelectedCulture(CultureObject culture, CharacterCreationManager characterCreationManager)
	{
		SelectedCulture = culture;
		characterCreationManager.ResetMenuOptions();
		SelectedTitleType = DefaultSelectedTitleType;
		TextObject textObject = FactionHelper.GenerateClanNameforPlayer();
		Clan.PlayerClan.ChangeClanName(textObject, textObject);
	}

	public void ApplyCulture(CharacterCreationManager characterCreationManager)
	{
		Hero.MainHero.Culture = SelectedCulture;
		Clan.PlayerClan.Culture = SelectedCulture;
		Clan.PlayerClan.ResetPlayerHomeAndFactionMidSettlement();
		Hero.MainHero.BornSettlement = Clan.PlayerClan.HomeSettlement;
	}

	public IEnumerable<CultureObject> GetCultures()
	{
		foreach (KeyValuePair<CultureObject, KeyValuePair<int, int>> characterCreationCulture in _characterCreationCultures)
		{
			yield return characterCreationCulture.Key;
		}
	}

	private void SetMainHeroInitialStats()
	{
		Hero.MainHero.HeroDeveloper.ClearHero();
		Hero.MainHero.HitPoints = 100;
		foreach (SkillObject item in Skills.All)
		{
			Hero.MainHero.HeroDeveloper.InitializeSkillXp(item);
		}
		foreach (CharacterAttribute item2 in Attributes.All)
		{
			Hero.MainHero.HeroDeveloper.AddAttribute(item2, 2, checkUnspentPoints: false);
		}
	}

	public void AddEquipmentToUseGetter(TryGetEquipmentIdDelegate tryGetEquipmentIdDelegate)
	{
		_tryGetEquipmentIdDelegates.Add(tryGetEquipmentIdDelegate);
	}

	public bool TryGetEquipmentToUse(string occupationId, out string equipmentId)
	{
		for (int num = _tryGetEquipmentIdDelegates.Count - 1; num >= 0; num--)
		{
			if (_tryGetEquipmentIdDelegates[num](occupationId, out equipmentId))
			{
				return true;
			}
		}
		equipmentId = null;
		return false;
	}
}
