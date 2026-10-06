using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.Naval;

public class DefaultFigureheads
{
	private Figurehead _hawk;

	private Figurehead _lion;

	private Figurehead _dragon;

	private Figurehead _wingsOfVictory;

	private Figurehead _ram;

	private Figurehead _seaSerpent;

	private Figurehead _viper;

	private Figurehead _saberToothTiger;

	private Figurehead _siren;

	private Figurehead _horse;

	private Figurehead _turtle;

	private Figurehead _boar;

	private Figurehead _oxen;

	private Figurehead _swan;

	private Figurehead _deer;

	private Figurehead _raven;

	public static DefaultFigureheads Instance => Campaign.Current.DefaultFigureheads;

	public static Figurehead Hawk => Instance._hawk;

	public static Figurehead Lion => Instance._lion;

	public static Figurehead Dragon => Instance._dragon;

	public static Figurehead WingsOfVictory => Instance._wingsOfVictory;

	public static Figurehead Ram => Instance._ram;

	public static Figurehead SeaSerpent => Instance._seaSerpent;

	public static Figurehead Viper => Instance._viper;

	public static Figurehead SaberToothTiger => Instance._saberToothTiger;

	public static Figurehead Siren => Instance._siren;

	public static Figurehead Horse => Instance._horse;

	public static Figurehead Turtle => Instance._turtle;

	public static Figurehead Boar => Instance._boar;

	public static Figurehead Oxen => Instance._oxen;

	public static Figurehead Swan => Instance._swan;

	public static Figurehead Deer => Instance._deer;

	public static Figurehead Raven => Instance._raven;

	public DefaultFigureheads()
	{
		RegisterAll();
	}

	private void RegisterAll()
	{
		_hawk = MBObjectManager.Instance.RegisterPresumedObject(new Figurehead("hawk"));
		_lion = MBObjectManager.Instance.RegisterPresumedObject(new Figurehead("lion"));
		_dragon = MBObjectManager.Instance.RegisterPresumedObject(new Figurehead("dragon"));
		_wingsOfVictory = MBObjectManager.Instance.RegisterPresumedObject(new Figurehead("wings_of_victory"));
		_ram = MBObjectManager.Instance.RegisterPresumedObject(new Figurehead("ram"));
		_seaSerpent = MBObjectManager.Instance.RegisterPresumedObject(new Figurehead("sea_serpent"));
		_viper = MBObjectManager.Instance.RegisterPresumedObject(new Figurehead("viper"));
		_saberToothTiger = MBObjectManager.Instance.RegisterPresumedObject(new Figurehead("saber_tooth_tiger"));
		_siren = MBObjectManager.Instance.RegisterPresumedObject(new Figurehead("siren"));
		_horse = MBObjectManager.Instance.RegisterPresumedObject(new Figurehead("horse"));
		_turtle = MBObjectManager.Instance.RegisterPresumedObject(new Figurehead("turtle"));
		_boar = MBObjectManager.Instance.RegisterPresumedObject(new Figurehead("boar"));
		_oxen = MBObjectManager.Instance.RegisterPresumedObject(new Figurehead("oxen"));
		_swan = MBObjectManager.Instance.RegisterPresumedObject(new Figurehead("swan"));
		_deer = MBObjectManager.Instance.RegisterPresumedObject(new Figurehead("deer"));
		_raven = MBObjectManager.Instance.RegisterPresumedObject(new Figurehead("raven"));
		InitializeAll();
	}

	private void InitializeAll()
	{
		_hawk.Initialize(new TextObject("{=VKFTub9a}Hawk"), new TextObject("{=ku2DXiY9}Crew ranged accuracy {EFFECT_AMOUNT}%"), 0.15f, MBObjectManager.Instance.GetObject<CultureObject>("aserai"), EffectIncrementType.AddFactor);
		_lion.Initialize(new TextObject("{=D0SX1cFQ}Lion"), new TextObject("{=EjjAmdXp}Crew battle morale {EFFECT_AMOUNT}"), 10f, MBObjectManager.Instance.GetObject<CultureObject>("vlandia"), EffectIncrementType.Add);
		_dragon.Initialize(new TextObject("{=GkvX7z6Y}Dragon"), new TextObject("{=4Ok7GnHs}Boarded enemy crew morale {EFFECT_AMOUNT}"), -5f, MBObjectManager.Instance.GetObject<CultureObject>("nord"), EffectIncrementType.Add);
		_wingsOfVictory.Initialize(new TextObject("{=ci0npfYB}Wings Of Victory"), new TextObject("{=mQuaMNVb}Party battle experience {EFFECT_AMOUNT}%"), 0.15f, MBObjectManager.Instance.GetObject<CultureObject>("empire"), EffectIncrementType.AddFactor);
		_ram.Initialize(new TextObject("{=shipFigureheadRam}Ram"), new TextObject("{=eJ4MC1KO}Ramming ship and morale damage {EFFECT_AMOUNT}%."), 0.2f, MBObjectManager.Instance.GetObject<CultureObject>("empire"), EffectIncrementType.AddFactor);
		_seaSerpent.Initialize(new TextObject("{=fsb5EEbg}Sea Serpent"), new TextObject("{=OraB7RjB}Fire damage resistance {EFFECT_AMOUNT}%"), 0.4f, MBObjectManager.Instance.GetObject<CultureObject>("nord"), EffectIncrementType.AddFactor);
		_viper.Initialize(new TextObject("{=LTOaBiw3}Viper"), new TextObject("{=NxIUg152}Ballista reload speed {EFFECT_AMOUNT}"), 0.25f, MBObjectManager.Instance.GetObject<CultureObject>("aserai"), EffectIncrementType.AddFactor);
		_saberToothTiger.Initialize(new TextObject("{=113F2KC5}Saber Tooth Tiger"), new TextObject("{=qWDM0Oa1}Archer armor penetration {EFFECT_AMOUNT}"), 0.1f, MBObjectManager.Instance.GetObject<CultureObject>("vlandia"), EffectIncrementType.AddFactor);
		_siren.Initialize(new TextObject("{=wrwdRGkW}Siren"), new TextObject("{=iBPMtWzZ}Boarded enemy crew melee damage {EFFECT_AMOUNT}%"), -0.1f, MBObjectManager.Instance.GetObject<CultureObject>("sturgia"), EffectIncrementType.AddFactor);
		_horse.Initialize(new TextObject("{=LwfILaRH}Horse"), new TextObject("{=sMCpa5Sk}Ship travel speed {EFFECT_AMOUNT}%"), 0.1f, MBObjectManager.Instance.GetObject<CultureObject>("khuzait"), EffectIncrementType.AddFactor);
		_turtle.Initialize(new TextObject("{=Ni8CSaxD}Turtle"), new TextObject("{=bAWHXCsb}Crew shield hitpoints {EFFECT_AMOUNT}%"), 0.4f, MBObjectManager.Instance.GetObject<CultureObject>("sturgia"), EffectIncrementType.AddFactor);
		_boar.Initialize(new TextObject("{=0OrIliBh}Boar"), new TextObject("{=FPZ9QOGl}Crew armor {EFFECT_AMOUNT}%"), 0.1f, MBObjectManager.Instance.GetObject<CultureObject>("battania"), EffectIncrementType.AddFactor);
		_oxen.Initialize(new TextObject("{=mGy1EcUd}Oxen"), new TextObject("{=D2ZA2XT6}Crew hitpoints {EFFECT_AMOUNT}"), 10f, MBObjectManager.Instance.GetObject<CultureObject>("battania"), EffectIncrementType.Add);
		_swan.Initialize(new TextObject("{=ZSA1mySL}Swan"), new TextObject("{=JJTWn3zs}Sail force {EFFECT_AMOUNT}%"), 0.1f, MBObjectManager.Instance.GetObject<CultureObject>("empire"), EffectIncrementType.AddFactor);
		_deer.Initialize(new TextObject("{=XbNVQdZN}Deer"), new TextObject("{=foC3qNav}Oar force {EFFECT_AMOUNT}%"), 0.15f, MBObjectManager.Instance.GetObject<CultureObject>("sturgia"), EffectIncrementType.AddFactor);
		_raven.Initialize(new TextObject("{=NVKwvl1G}Raven"), new TextObject("{=QsR8WTpA}Crew throwing weapon damage {EFFECT_AMOUNT}%"), 0.1f, MBObjectManager.Instance.GetObject<CultureObject>("nord"), EffectIncrementType.AddFactor);
	}
}
