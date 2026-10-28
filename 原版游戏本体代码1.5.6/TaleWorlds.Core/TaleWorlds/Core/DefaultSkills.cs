using TaleWorlds.Localization;

namespace TaleWorlds.Core;

public class DefaultSkills
{
	private SkillObject _skillEngineering;

	private SkillObject _skillMedicine;

	private SkillObject _skillLeadership;

	private SkillObject _skillSteward;

	private SkillObject _skillTrade;

	private SkillObject _skillCharm;

	private SkillObject _skillRoguery;

	private SkillObject _skillScouting;

	private SkillObject _skillTactics;

	private SkillObject _skillCrafting;

	private SkillObject _skillAthletics;

	private SkillObject _skillRiding;

	private SkillObject _skillThrowing;

	private SkillObject _skillCrossbow;

	private SkillObject _skillBow;

	private SkillObject _skillPolearm;

	private SkillObject _skillTwoHanded;

	private SkillObject _skillOneHanded;

	private static DefaultSkills Instance => Game.Current.DefaultSkills;

	public static SkillObject OneHanded => Instance._skillOneHanded;

	public static SkillObject TwoHanded => Instance._skillTwoHanded;

	public static SkillObject Polearm => Instance._skillPolearm;

	public static SkillObject Bow => Instance._skillBow;

	public static SkillObject Crossbow => Instance._skillCrossbow;

	public static SkillObject Throwing => Instance._skillThrowing;

	public static SkillObject Riding => Instance._skillRiding;

	public static SkillObject Athletics => Instance._skillAthletics;

	public static SkillObject Crafting => Instance._skillCrafting;

	public static SkillObject Tactics => Instance._skillTactics;

	public static SkillObject Scouting => Instance._skillScouting;

	public static SkillObject Roguery => Instance._skillRoguery;

	public static SkillObject Charm => Instance._skillCharm;

	public static SkillObject Leadership => Instance._skillLeadership;

	public static SkillObject Trade => Instance._skillTrade;

	public static SkillObject Steward => Instance._skillSteward;

	public static SkillObject Medicine => Instance._skillMedicine;

	public static SkillObject Engineering => Instance._skillEngineering;

	private SkillObject Create(string stringId)
	{
		return Game.Current.ObjectManager.RegisterPresumedObject(new SkillObject(stringId));
	}

	private void InitializeAll()
	{
		_skillOneHanded.Initialize(new TextObject("{=PiHpR4QL}One Handed"), new TextObject("{=yEkSSqIm}Mastery of fighting with one-handed weapons either with a shield or without."), new CharacterAttribute[1] { DefaultCharacterAttributes.Vigor });
		_skillTwoHanded.Initialize(new TextObject("{=t78atYqH}Two Handed"), new TextObject("{=eoLbkhsY}Mastery of fighting with two-handed weapons of average length such as bigger axes and swords."), new CharacterAttribute[1] { DefaultCharacterAttributes.Vigor });
		_skillPolearm.Initialize(new TextObject("{=haax8kMa}Polearm"), new TextObject("{=iKmXX7i3}Mastery of the spear, lance, staff and other polearms, both one-handed and two-handed."), new CharacterAttribute[1] { DefaultCharacterAttributes.Vigor });
		_skillBow.Initialize(new TextObject("{=5rj7xQE4}Bow"), new TextObject("{=FLf5J3su}Familarity with bows and physical conditioning to shoot with them effectively."), new CharacterAttribute[1] { DefaultCharacterAttributes.Control });
		_skillCrossbow.Initialize(new TextObject("{=TTWL7RLe}Crossbow"), new TextObject("{=haV3nLYA}Knowledge of operating and maintaining crossbows."), new CharacterAttribute[1] { DefaultCharacterAttributes.Control });
		_skillThrowing.Initialize(new TextObject("{=2wclahIJ}Throwing"), new TextObject("{=NwTpATW5}Mastery of throwing projectiles accurately and with power."), new CharacterAttribute[1] { DefaultCharacterAttributes.Control });
		_skillRiding.Initialize(new TextObject("{=p9i3zRm9}Riding"), new TextObject("{=H9Zamrao}The ability to control a horse, to keep your balance when it moves suddenly or unexpectedly, as well as general knowledge of horses, including their care and breeding."), new CharacterAttribute[1] { DefaultCharacterAttributes.Endurance });
		_skillAthletics.Initialize(new TextObject("{=skZS2UlW}Athletics"), new TextObject("{=bVD9j0wI}Physical fitness, speed and balance."), new CharacterAttribute[1] { DefaultCharacterAttributes.Endurance });
		_skillCrafting.Initialize(new TextObject("{=smithingskill}Smithing"), new TextObject("{=xWbkjccP}The knowledge of how to forge metal, match handle to blade, turn poles, sew scales, and other skills useful in the assembly of weapons and armor"), new CharacterAttribute[1] { DefaultCharacterAttributes.Endurance });
		_skillScouting.Initialize(new TextObject("{=LJ6Krlbr}Scouting"), new TextObject("{=kmBxaJZd}Knowledge of how to scan the wilderness for life. You can follow tracks, spot movement in the undergrowth, and spot an enemy across the valley from a flash of light on spearpoints or a dustcloud."), new CharacterAttribute[1] { DefaultCharacterAttributes.Cunning });
		_skillTactics.Initialize(new TextObject("{=m8o51fc7}Tactics"), new TextObject("{=FQOFDrAu}Your judgment of how troops will perform in contact. This allows you to make a good prediction of when an unorthodox tactic will work, and when it won't."), new CharacterAttribute[1] { DefaultCharacterAttributes.Cunning });
		_skillRoguery.Initialize(new TextObject("{=V0ZMJ0PX}Roguery"), new TextObject("{=81YLbLok}Experience with the darker side of human life. You can tell when a guard wants a bribe, you know how to intimidate someone, and have a good sense of what you can and can't get away with."), new CharacterAttribute[1] { DefaultCharacterAttributes.Cunning });
		_skillCharm.Initialize(new TextObject("{=EGeY1gfs}Charm"), new TextObject("{=VajIVjkc}The ability to make a person like and trust you. You can make a good guess at people's motivations and the kinds of arguments to which they'll respond."), new CharacterAttribute[1] { DefaultCharacterAttributes.Social });
		_skillLeadership.Initialize(new TextObject("{=HsLfmEmb}Leadership"), new TextObject("{=97EmbcHQ}The ability to inspire. You can fill individuals with confidence and stir up enthusiasm and courage in larger groups."), new CharacterAttribute[1] { DefaultCharacterAttributes.Social });
		_skillTrade.Initialize(new TextObject("{=GmcgoiGy}Trade"), new TextObject("{=lsJMCkZy}Familiarity with the most common goods in the marketplace and their prices, as well as the ability to spot defective goods or tell if you've been shortchanged in quantity"), new CharacterAttribute[1] { DefaultCharacterAttributes.Social });
		_skillSteward.Initialize(new TextObject("{=stewardskill}Steward"), new TextObject("{=2K0iVRkW}Ability to organize a group and manage logistics. This helps you to run an estate or administer a town, and can increase the size of a party that you lead or in which you serve as quartermaster."), new CharacterAttribute[1] { DefaultCharacterAttributes.Intelligence });
		_skillMedicine.Initialize(new TextObject("{=JKH59XNp}Medicine"), new TextObject("{=igg5sEh3}Knowledge of how to staunch bleeding, to set broken bones, to remove embedded weapons and clean wounds to prevent infection, and to apply poultices to relieve pain and soothe inflammation."), new CharacterAttribute[1] { DefaultCharacterAttributes.Intelligence });
		_skillEngineering.Initialize(new TextObject("{=engineeringskill}Engineering"), new TextObject("{=hbaMnpVR}Knowledge of how to make things that can withstand powerful forces without collapsing. Useful for building both structures and the devices that knock them down."), new CharacterAttribute[1] { DefaultCharacterAttributes.Intelligence });
	}

	public DefaultSkills()
	{
		RegisterAll();
	}

	private void RegisterAll()
	{
		_skillOneHanded = Create("OneHanded");
		_skillTwoHanded = Create("TwoHanded");
		_skillPolearm = Create("Polearm");
		_skillBow = Create("Bow");
		_skillCrossbow = Create("Crossbow");
		_skillThrowing = Create("Throwing");
		_skillRiding = Create("Riding");
		_skillAthletics = Create("Athletics");
		_skillCrafting = Create("Crafting");
		_skillTactics = Create("Tactics");
		_skillScouting = Create("Scouting");
		_skillRoguery = Create("Roguery");
		_skillCharm = Create("Charm");
		_skillTrade = Create("Trade");
		_skillLeadership = Create("Leadership");
		_skillSteward = Create("Steward");
		_skillMedicine = Create("Medicine");
		_skillEngineering = Create("Engineering");
		InitializeAll();
	}
}
