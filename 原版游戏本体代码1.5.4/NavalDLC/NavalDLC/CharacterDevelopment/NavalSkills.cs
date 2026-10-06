using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace NavalDLC.CharacterDevelopment;

public class NavalSkills
{
	private SkillObject _skillMariner;

	private SkillObject _skillBoatswain;

	private SkillObject _skillShipmaster;

	private static NavalSkills Instance => NavalDLCManager.Instance.NavalSkills;

	public static SkillObject Mariner => Instance._skillMariner;

	public static SkillObject Boatswain => Instance._skillBoatswain;

	public static SkillObject Shipmaster => Instance._skillShipmaster;

	private SkillObject Create(string stringId)
	{
		return Game.Current.ObjectManager.RegisterPresumedObject(new SkillObject(stringId));
	}

	private void InitializeAll()
	{
		_skillMariner.Initialize(new TextObject("{=bOhiqquf}Mariner"), new TextObject("{=JSvE81Iw}Enhances your personal combat prowess during naval engagements and bolsters your effectiveness in leading troops and employing tactics in sea battles."), new CharacterAttribute[2]
		{
			DefaultCharacterAttributes.Endurance,
			DefaultCharacterAttributes.Cunning
		});
		_skillBoatswain.Initialize(new TextObject("{=olTmdP9j}Boatswain"), new TextObject("{=SZ0BH8b1}Governs the well-being and discipline of your ship's crew, as well as the vessel's overall combat readiness, including rigging and supplies."), new CharacterAttribute[2]
		{
			DefaultCharacterAttributes.Control,
			DefaultCharacterAttributes.Social
		});
		_skillShipmaster.Initialize(new TextObject("{=SSLTboWZ}Shipmaster"), new TextObject("{=CmXMqtcU}Improves your navigational abilities, the effectiveness of naval siege engines under your command, and the speed and quality of ship repairs and upgrades."), new CharacterAttribute[2]
		{
			DefaultCharacterAttributes.Vigor,
			DefaultCharacterAttributes.Intelligence
		});
	}

	public NavalSkills()
	{
		RegisterAll();
	}

	private void RegisterAll()
	{
		_skillMariner = Create("Mariner");
		_skillBoatswain = Create("Boatswain");
		_skillShipmaster = Create("Shipmaster");
		InitializeAll();
	}
}
