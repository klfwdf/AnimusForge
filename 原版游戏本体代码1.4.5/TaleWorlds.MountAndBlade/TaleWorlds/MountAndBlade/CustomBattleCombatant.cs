using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade;

public class CustomBattleCombatant : IBattleCombatant
{
	private List<BasicCharacterObject> _characters;

	private BasicCharacterObject _general;

	public TextObject Name { get; private set; }

	public BattleSideEnum Side { get; set; }

	public BasicCharacterObject General => _general;

	public BasicCultureObject BasicCulture { get; private set; }

	public Tuple<uint, uint> PrimaryColorPair => new Tuple<uint, uint>(Banner.GetPrimaryColor(), Banner.GetFirstIconColor());

	public Tuple<uint, uint> AlternativeColorPair => new Tuple<uint, uint>(Banner.GetFirstIconColor(), Banner.GetPrimaryColor());

	public Banner Banner { get; private set; }

	public IEnumerable<BasicCharacterObject> Characters => _characters.AsReadOnly();

	public int CountOfCharacters => _characters.Count();

	public int NumberOfAllMembers { get; private set; }

	public int NumberOfHealthyMembers => _characters.Count;

	public int GetTacticsSkillAmount()
	{
		if (_characters.Count > 0)
		{
			return _characters.Max((BasicCharacterObject h) => h.GetSkillValue(DefaultSkills.Tactics));
		}
		return 0;
	}

	public CustomBattleCombatant(TextObject name, BasicCultureObject culture, Banner banner)
	{
		Name = name;
		BasicCulture = culture;
		Banner = banner;
		_characters = new List<BasicCharacterObject>();
		_general = null;
	}

	public void AddCharacter(BasicCharacterObject characterObject, int number)
	{
		for (int i = 0; i < number; i++)
		{
			_characters.Add(characterObject);
		}
		NumberOfAllMembers += number;
	}

	public void SetGeneral(BasicCharacterObject generalCharacter)
	{
		_general = generalCharacter;
	}

	public bool IsUnderPlayersCommand(BattleSideEnum playerSide)
	{
		if (Side != playerSide)
		{
			return false;
		}
		return General.IsPlayerCharacter;
	}
}
