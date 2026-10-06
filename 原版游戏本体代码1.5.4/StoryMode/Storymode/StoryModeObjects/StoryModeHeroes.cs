using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace StoryMode.StoryModeObjects;

public class StoryModeHeroes
{
	private const string BrotherStringId = "tutorial_npc_brother";

	private const string LittleBrotherStringId = "storymode_little_brother";

	private const string LittleSisterStringId = "storymode_little_sister";

	private const string TacitusStringId = "tutorial_npc_tacitus";

	private const string RadagosStringId = "tutorial_npc_radagos";

	private const string IstianaStringId = "storymode_imperial_mentor_istiana";

	private const string ArzagosStringId = "storymode_imperial_mentor_arzagos";

	private const string GalterStringId = "radagos_henchman";

	private const string MainHeroMotherId = "main_hero_mother";

	private const string MainHeroFatherId = "main_hero_father";

	private Hero _elderBrother;

	private Hero _littleBrother;

	private Hero _littleSister;

	private Hero _tacitus;

	private Hero _radagos;

	private Hero _imperialMentor;

	private Hero _antiImperialMentor;

	private Hero _radagosHenchman;

	private Hero _mainHeroMother;

	private Hero _mainHeroFather;

	public static Hero ElderBrother => StoryModeManager.Current.StoryModeHeroes._elderBrother;

	public static Hero LittleBrother => StoryModeManager.Current.StoryModeHeroes._littleBrother;

	public static Hero LittleSister => StoryModeManager.Current.StoryModeHeroes._littleSister;

	public static Hero Tacitus => StoryModeManager.Current.StoryModeHeroes._tacitus;

	public static Hero Radagos => StoryModeManager.Current.StoryModeHeroes._radagos;

	public static Hero ImperialMentor => StoryModeManager.Current.StoryModeHeroes._imperialMentor;

	public static Hero AntiImperialMentor => StoryModeManager.Current.StoryModeHeroes._antiImperialMentor;

	public static Hero RadagosHenchman => StoryModeManager.Current.StoryModeHeroes._radagosHenchman;

	public static Hero MainHeroMother => StoryModeManager.Current.StoryModeHeroes._mainHeroMother;

	public static Hero MainHeroFather => StoryModeManager.Current.StoryModeHeroes._mainHeroFather;

	internal StoryModeHeroes()
	{
		RegisterAll();
	}

	private void RegisterAll()
	{
		Clan clan = Campaign.Current.CampaignObjectManager.Find<Clan>("player_faction");
		CharacterObject characterObject = Game.Current.ObjectManager.GetObject<CharacterObject>("main_hero_mother");
		CharacterObject characterObject2 = Game.Current.ObjectManager.GetObject<CharacterObject>("main_hero_father");
		if (HeroCreator.CreateBasicHero("main_hero_mother", characterObject, out _mainHeroMother, isAlive: false))
		{
			_mainHeroMother.Clan = clan;
			HeroHelper.GetRandomDeathDayAndBirthDay((int)characterObject.Age, out var birthday, out var deathday);
			_mainHeroMother.SetBirthDay(birthday);
			_mainHeroMother.SetDeathDay(deathday);
		}
		if (HeroCreator.CreateBasicHero("main_hero_father", characterObject2, out _mainHeroFather, isAlive: false))
		{
			_mainHeroFather.Clan = clan;
			HeroHelper.GetRandomDeathDayAndBirthDay((int)characterObject2.Age, out var birthday2, out var deathday2);
			_mainHeroFather.SetBirthDay(birthday2);
			_mainHeroFather.SetDeathDay(deathday2);
		}
		if (HeroCreator.CreateBasicHero("tutorial_npc_brother", MBObjectManager.Instance.GetObject<CharacterObject>("tutorial_npc_brother"), out _elderBrother))
		{
			_elderBrother.Clan = clan;
			TextObject textObject = GameTexts.FindText("str_player_brother_name", characterObject.Culture.StringId);
			_elderBrother.SetName(textObject, textObject);
			_elderBrother.Mother = characterObject.HeroObject;
			_elderBrother.Father = characterObject2.HeroObject;
			_elderBrother.HeroDeveloper.ResetCharacterStats();
		}
		if (HeroCreator.CreateBasicHero("storymode_little_brother", MBObjectManager.Instance.GetObject<CharacterObject>("storymode_little_brother"), out _littleBrother))
		{
			TextObject textObject2 = GameTexts.FindText("str_player_little_brother_name", characterObject.Culture.StringId);
			_littleBrother.SetName(textObject2, textObject2);
			_littleBrother.Mother = characterObject.HeroObject;
			_littleBrother.Father = characterObject2.HeroObject;
		}
		if (HeroCreator.CreateBasicHero("storymode_little_sister", MBObjectManager.Instance.GetObject<CharacterObject>("storymode_little_sister"), out _littleSister))
		{
			TextObject textObject3 = GameTexts.FindText("str_player_little_sister_name", characterObject.Culture.StringId);
			_littleSister.SetName(textObject3, textObject3);
			_littleSister.Mother = characterObject.HeroObject;
			_littleSister.Father = characterObject2.HeroObject;
		}
		HeroCreator.CreateBasicHero("tutorial_npc_tacitus", MBObjectManager.Instance.GetObject<CharacterObject>("tutorial_npc_tacitus"), out _tacitus);
		HeroCreator.CreateBasicHero("tutorial_npc_radagos", MBObjectManager.Instance.GetObject<CharacterObject>("tutorial_npc_radagos"), out _radagos);
		HeroCreator.CreateBasicHero("storymode_imperial_mentor_istiana", MBObjectManager.Instance.GetObject<CharacterObject>("storymode_imperial_mentor_istiana"), out _imperialMentor);
		HeroCreator.CreateBasicHero("storymode_imperial_mentor_arzagos", MBObjectManager.Instance.GetObject<CharacterObject>("storymode_imperial_mentor_arzagos"), out _antiImperialMentor);
		HeroCreator.CreateBasicHero("radagos_henchman", MBObjectManager.Instance.GetObject<CharacterObject>("radagos_henchman"), out _radagosHenchman);
	}
}
