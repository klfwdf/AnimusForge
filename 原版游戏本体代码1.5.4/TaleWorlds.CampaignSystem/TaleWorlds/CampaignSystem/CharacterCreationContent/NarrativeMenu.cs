using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CharacterCreationContent;

public sealed class NarrativeMenu
{
	public delegate List<NarrativeMenuCharacterArgs> GetNarrativeMenuCharacterArgsDelegate(CultureObject culture, string occupationType, CharacterCreationManager characterCreationManager);

	public readonly string StringId;

	public readonly string InputMenuId;

	public readonly string OutputMenuId;

	public readonly TextObject Title;

	public readonly TextObject Description;

	private readonly List<NarrativeMenuCharacter> _characters;

	private readonly MBList<NarrativeMenuOption> _characterCreationMenuOptions;

	public readonly GetNarrativeMenuCharacterArgsDelegate GetNarrativeMenuCharacterArgs;

	public List<NarrativeMenuCharacter> Characters => _characters;

	public MBReadOnlyList<NarrativeMenuOption> CharacterCreationMenuOptions => _characterCreationMenuOptions;

	public NarrativeMenu(string stringId, string inputMenuId, string outputMenuId, TextObject title, TextObject description, List<NarrativeMenuCharacter> characters, GetNarrativeMenuCharacterArgsDelegate getNarrativeMenuCharacterArgs)
	{
		StringId = stringId;
		InputMenuId = inputMenuId;
		OutputMenuId = outputMenuId;
		Title = title;
		Description = description;
		_characters = characters;
		GetNarrativeMenuCharacterArgs = getNarrativeMenuCharacterArgs;
		_characterCreationMenuOptions = new MBList<NarrativeMenuOption>();
	}

	public void AddNarrativeMenuOption(NarrativeMenuOption narrativeMenuOption)
	{
		_characterCreationMenuOptions.Add(narrativeMenuOption);
	}

	public void RemoveNarrativeMenuOption(NarrativeMenuOption narrativeMenuOption)
	{
		_characterCreationMenuOptions.Remove(narrativeMenuOption);
	}
}
