using TaleWorlds.Localization;

namespace TaleWorlds.Core;

public class DefaultCharacterAttributes
{
	private CharacterAttribute _control;

	private CharacterAttribute _vigor;

	private CharacterAttribute _endurance;

	private CharacterAttribute _cunning;

	private CharacterAttribute _social;

	private CharacterAttribute _intelligence;

	private static DefaultCharacterAttributes Instance => Game.Current.DefaultCharacterAttributes;

	public static CharacterAttribute Vigor => Instance._vigor;

	public static CharacterAttribute Control => Instance._control;

	public static CharacterAttribute Endurance => Instance._endurance;

	public static CharacterAttribute Cunning => Instance._cunning;

	public static CharacterAttribute Social => Instance._social;

	public static CharacterAttribute Intelligence => Instance._intelligence;

	private CharacterAttribute Create(string stringId)
	{
		return Game.Current.ObjectManager.RegisterPresumedObject(new CharacterAttribute(stringId));
	}

	internal DefaultCharacterAttributes()
	{
		RegisterAll();
	}

	private void RegisterAll()
	{
		_vigor = Create("vigor");
		_control = Create("control");
		_endurance = Create("endurance");
		_cunning = Create("cunning");
		_social = Create("social");
		_intelligence = Create("intelligence");
		InitializeAll();
	}

	private void InitializeAll()
	{
		_vigor.Initialize(new TextObject("{=YWkdD7Ki}Vigor"), new TextObject("{=jJ9sLOLb}Vigor represents the ability to move with speed and force. It's important for melee combat."), new TextObject("{=Ve8xoa3i}VIG"));
		_control.Initialize(new TextObject("{=controlskill}Control"), new TextObject("{=vx0OCvaj}Control represents the ability to use strength without sacrificing precision. It's necessary for using ranged weapons."), new TextObject("{=HuXafdmR}CTR"));
		_endurance.Initialize(new TextObject("{=kvOavzcs}Endurance"), new TextObject("{=K8rCOQUZ}Endurance is the ability to perform taxing physical activity for a long time."), new TextObject("{=d2ApwXJr}END"));
		_cunning.Initialize(new TextObject("{=JZM1mQvb}Cunning"), new TextObject("{=YO5LUfiO}Cunning is the ability to predict what other people will do, and to outwit their plans."), new TextObject("{=tH6Ooj0P}CNG"));
		_social.Initialize(new TextObject("{=socialskill}Social"), new TextObject("{=XMDTt96y}Social is the ability to understand people's motivations and to sway them."), new TextObject("{=PHoxdReD}SOC"));
		_intelligence.Initialize(new TextObject("{=sOrJoxiC}Intelligence"), new TextObject("{=TeUtEGV0}Intelligence represents aptitude for reading and theoretical learning."), new TextObject("{=Bn7IsMpu}INT"));
	}
}
