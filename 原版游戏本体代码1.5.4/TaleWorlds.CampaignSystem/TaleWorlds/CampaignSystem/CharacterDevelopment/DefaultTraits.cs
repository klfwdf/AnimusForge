using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CharacterDevelopment;

public class DefaultTraits
{
	private const int MaxPersonalityTraitValue = 2;

	private const int MinPersonalityTraitValue = -2;

	private const int MaxHiddenTraitValue = 20;

	private const int MinHiddenTraitValue = 0;

	private TraitObject _traitMercy;

	private TraitObject _traitValor;

	private TraitObject _traitHonor;

	private TraitObject _traitGenerosity;

	private TraitObject _traitCalculating;

	private TraitObject _traitPersonaCurt;

	private TraitObject _traitPersonaEarnest;

	private TraitObject _traitPersonaIronic;

	private TraitObject _traitPersonaSoftspoken;

	private TraitObject _traitEgalitarian;

	private TraitObject _traitOligarchic;

	private TraitObject _traitAuthoritarian;

	private TraitObject _traitSurgery;

	private TraitObject _traitTracking;

	private TraitObject _traitSergeantCommandSkills;

	private TraitObject _traitRogueSkills;

	private TraitObject _traitEngineerSkills;

	private TraitObject _traitBlacksmith;

	private TraitObject _traitScoutSkills;

	private TraitObject _traitTraderSkills;

	private TraitObject _traitFrequency;

	private TraitObject _traitCommander;

	private TraitObject _traitThug;

	private TraitObject _traitSmuggler;

	private TraitObject _traitNavalSoldier;

	private readonly TraitObject[] _personality;

	private static DefaultTraits Instance => Campaign.Current.DefaultTraits;

	public static TraitObject Frequency => Instance._traitFrequency;

	public static TraitObject Mercy => Instance._traitMercy;

	public static TraitObject Valor => Instance._traitValor;

	public static TraitObject Honor => Instance._traitHonor;

	public static TraitObject Generosity => Instance._traitGenerosity;

	public static TraitObject Calculating => Instance._traitCalculating;

	public static TraitObject PersonaCurt => Instance._traitPersonaCurt;

	public static TraitObject PersonaEarnest => Instance._traitPersonaEarnest;

	public static TraitObject PersonaIronic => Instance._traitPersonaIronic;

	public static TraitObject PersonaSoftspoken => Instance._traitPersonaSoftspoken;

	public static TraitObject Surgery => Instance._traitSurgery;

	public static TraitObject SergeantCommandSkills => Instance._traitSergeantCommandSkills;

	public static TraitObject RogueSkills => Instance._traitRogueSkills;

	public static TraitObject Siegecraft => Instance._traitEngineerSkills;

	public static TraitObject ScoutSkills => Instance._traitScoutSkills;

	public static TraitObject Blacksmith => Instance._traitBlacksmith;

	public static TraitObject Commander => Instance._traitCommander;

	public static TraitObject Trader => Instance._traitTraderSkills;

	public static TraitObject Thug => Instance._traitThug;

	public static TraitObject Smuggler => Instance._traitSmuggler;

	public static TraitObject Egalitarian => Instance._traitEgalitarian;

	public static TraitObject Oligarchic => Instance._traitOligarchic;

	public static TraitObject Authoritarian => Instance._traitAuthoritarian;

	public static TraitObject NavalSoldier => Instance._traitNavalSoldier;

	public static IEnumerable<TraitObject> Personality => Instance._personality;

	public DefaultTraits()
	{
		RegisterAll();
		_personality = new TraitObject[5] { _traitMercy, _traitValor, _traitHonor, _traitGenerosity, _traitCalculating };
	}

	public void RegisterAll()
	{
		_traitFrequency = Create("Frequency");
		_traitMercy = Create("Mercy");
		_traitValor = Create("Valor");
		_traitHonor = Create("Honor");
		_traitGenerosity = Create("Generosity");
		_traitCalculating = Create("Calculating");
		_traitPersonaCurt = Create("curt");
		_traitPersonaIronic = Create("ironic");
		_traitPersonaEarnest = Create("earnest");
		_traitPersonaSoftspoken = Create("softspoken");
		_traitCommander = Create("Commander");
		_traitTraderSkills = Create("Trader");
		_traitSurgery = Create("Surgeon");
		_traitTracking = Create("Tracking");
		_traitBlacksmith = Create("Blacksmith");
		_traitSergeantCommandSkills = Create("SergeantCommandSkills");
		_traitEngineerSkills = Create("EngineerSkills");
		_traitRogueSkills = Create("RogueSkills");
		_traitScoutSkills = Create("ScoutSkills");
		_traitThug = Create("Thug");
		_traitSmuggler = Create("Smuggler");
		_traitEgalitarian = Create("Egalitarian");
		_traitOligarchic = Create("Oligarchic");
		_traitAuthoritarian = Create("Authoritarian");
		_traitNavalSoldier = Create("NavalSoldier");
		InitializeAll();
	}

	private TraitObject Create(string stringId)
	{
		return Game.Current.ObjectManager.RegisterPresumedObject(new TraitObject(stringId));
	}

	private void InitializeAll()
	{
		_traitFrequency.Initialize(new TextObject("{=vsoyhPnl}Frequency"), new TextObject("{=!}Frequency Description"), isHidden: true, 0, 20);
		_traitMercy.Initialize(new TextObject("{=2I2uKJlw}Mercy"), new TextObject("{=Au7VCWTa}Mercy represents your general aversion to suffering and your willingness to help strangers or even enemies."), isHidden: false, -2, 2);
		_traitValor.Initialize(new TextObject("{=toQLHG6x}Valor"), new TextObject("{=Ugm9nO49}Valor represents your reputation for risking your life to win glory or wealth or advance your cause."), isHidden: false, -2, 2);
		_traitHonor.Initialize(new TextObject("{=0oGz5rVx}Honor"), new TextObject("{=1vYgkaaK}Honor represents your reputation for respecting your formal commitments, like keeping your word and obeying the law."), isHidden: false, -2, 2);
		_traitGenerosity.Initialize(new TextObject("{=IuWu5Bu7}Generosity"), new TextObject("{=IKzqzPDS}Generosity represents your loyalty to your kin and those who serve you, and your gratitude to those who have done you a favor."), isHidden: false, -2, 2);
		_traitCalculating.Initialize(new TextObject("{=5sMBbn7y}Calculating"), new TextObject("{=QKjF5gTR}Calculating represents your ability to control your emotions for the sake of your long-term interests."), isHidden: false, -2, 2);
		_traitPersonaCurt.Initialize(new TextObject("{=!}PersonaCurt"), new TextObject("{=!}PersonaCurt Description"), isHidden: false, -2, 2);
		_traitPersonaIronic.Initialize(new TextObject("{=!}PersonaIronic"), new TextObject("{=!}PersonaIronic Description"), isHidden: false, -2, 2);
		_traitPersonaEarnest.Initialize(new TextObject("{=!}PersonaEarnest"), new TextObject("{=!}PersonaEarnest Description"), isHidden: false, -2, 2);
		_traitPersonaSoftspoken.Initialize(new TextObject("{=!}PersonaSoftspoken"), new TextObject("{=!}PersonaSoftspoken Description"), isHidden: false, -2, 2);
		_traitCommander.Initialize(new TextObject("{=RvKwdXWs}Commander"), new TextObject("{=!}Commander Description"), isHidden: true, 0, 20);
		_traitSurgery.Initialize(new TextObject("{=QBPrRdQJ}Surgeon"), new TextObject("{=!}Surgeon Description"), isHidden: true, 0, 20);
		_traitTracking.Initialize(new TextObject("{=dx0hmeH6}Tracking"), new TextObject("{=!}Tracking Description"), isHidden: true, 0, 20);
		_traitBlacksmith.Initialize(new TextObject("{=bNnQt4jN}Blacksmith"), new TextObject("{=!}Blacksmith Description"), isHidden: true, 0, 20);
		_traitSergeantCommandSkills.Initialize(new TextObject("{=!}SergeantCommandSkills"), new TextObject("{=!}SergeantCommandSkills Description"), isHidden: true, 0, 20);
		_traitEngineerSkills.Initialize(new TextObject("{=!}EngineerSkills"), new TextObject("{=!}EngineerSkills Description"), isHidden: true, 0, 20);
		_traitRogueSkills.Initialize(new TextObject("{=!}RogueSkills"), new TextObject("{=!}RogueSkills Description"), isHidden: true, 0, 20);
		_traitScoutSkills.Initialize(new TextObject("{=!}ScoutSkills"), new TextObject("{=!}ScoutSkills Description"), isHidden: true, 0, 20);
		_traitTraderSkills.Initialize(new TextObject("{=!}TraderSkills"), new TextObject("{=!}Trader Description"), isHidden: true, 0, 20);
		_traitThug.Initialize(new TextObject("{=thugtrait}Thug"), new TextObject("{=Fjnw9ooa}Indicates a gang member specialized in extortion"), isHidden: true, 0, 20);
		_traitSmuggler.Initialize(new TextObject("{=eeWx1yYd}Smuggler"), new TextObject("{=87c7IhkZ}Indicates a gang member specialized in smuggling"), isHidden: true, 0, 20);
		_traitEgalitarian.Initialize(new TextObject("{=HMFb1gaq}Egalitarian"), new TextObject("{=!}Egalitarian Description"), isHidden: false, 0, 20);
		_traitOligarchic.Initialize(new TextObject("{=hR6Zo6pD}Oligarchic"), new TextObject("{=!}Oligarchic Description"), isHidden: false, 0, 20);
		_traitAuthoritarian.Initialize(new TextObject("{=NaMPa4ML}Authoritarian"), new TextObject("{=!}Authoritarian Description"), isHidden: false, 0, 20);
		_traitNavalSoldier.Initialize(new TextObject("{=rGUOr2wg}Naval Soldier"), new TextObject("{=!}Naval Soldier Description"), isHidden: true, 0, 20);
	}
}
