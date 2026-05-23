using System;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.Naval
{
	// Token: 0x0200022B RID: 555
	public class DefaultFigureheads
	{
		// Token: 0x1700084B RID: 2123
		// (get) Token: 0x060021AD RID: 8621 RVA: 0x00093A81 File Offset: 0x00091C81
		public static DefaultFigureheads Instance
		{
			get
			{
				return Campaign.Current.DefaultFigureheads;
			}
		}

		// Token: 0x1700084C RID: 2124
		// (get) Token: 0x060021AE RID: 8622 RVA: 0x00093A8D File Offset: 0x00091C8D
		public static Figurehead Hawk
		{
			get
			{
				return DefaultFigureheads.Instance._hawk;
			}
		}

		// Token: 0x1700084D RID: 2125
		// (get) Token: 0x060021AF RID: 8623 RVA: 0x00093A99 File Offset: 0x00091C99
		public static Figurehead Lion
		{
			get
			{
				return DefaultFigureheads.Instance._lion;
			}
		}

		// Token: 0x1700084E RID: 2126
		// (get) Token: 0x060021B0 RID: 8624 RVA: 0x00093AA5 File Offset: 0x00091CA5
		public static Figurehead Dragon
		{
			get
			{
				return DefaultFigureheads.Instance._dragon;
			}
		}

		// Token: 0x1700084F RID: 2127
		// (get) Token: 0x060021B1 RID: 8625 RVA: 0x00093AB1 File Offset: 0x00091CB1
		public static Figurehead WingsOfVictory
		{
			get
			{
				return DefaultFigureheads.Instance._wingsOfVictory;
			}
		}

		// Token: 0x17000850 RID: 2128
		// (get) Token: 0x060021B2 RID: 8626 RVA: 0x00093ABD File Offset: 0x00091CBD
		public static Figurehead Ram
		{
			get
			{
				return DefaultFigureheads.Instance._ram;
			}
		}

		// Token: 0x17000851 RID: 2129
		// (get) Token: 0x060021B3 RID: 8627 RVA: 0x00093AC9 File Offset: 0x00091CC9
		public static Figurehead SeaSerpent
		{
			get
			{
				return DefaultFigureheads.Instance._seaSerpent;
			}
		}

		// Token: 0x17000852 RID: 2130
		// (get) Token: 0x060021B4 RID: 8628 RVA: 0x00093AD5 File Offset: 0x00091CD5
		public static Figurehead Viper
		{
			get
			{
				return DefaultFigureheads.Instance._viper;
			}
		}

		// Token: 0x17000853 RID: 2131
		// (get) Token: 0x060021B5 RID: 8629 RVA: 0x00093AE1 File Offset: 0x00091CE1
		public static Figurehead SaberToothTiger
		{
			get
			{
				return DefaultFigureheads.Instance._saberToothTiger;
			}
		}

		// Token: 0x17000854 RID: 2132
		// (get) Token: 0x060021B6 RID: 8630 RVA: 0x00093AED File Offset: 0x00091CED
		public static Figurehead Siren
		{
			get
			{
				return DefaultFigureheads.Instance._siren;
			}
		}

		// Token: 0x17000855 RID: 2133
		// (get) Token: 0x060021B7 RID: 8631 RVA: 0x00093AF9 File Offset: 0x00091CF9
		public static Figurehead Horse
		{
			get
			{
				return DefaultFigureheads.Instance._horse;
			}
		}

		// Token: 0x17000856 RID: 2134
		// (get) Token: 0x060021B8 RID: 8632 RVA: 0x00093B05 File Offset: 0x00091D05
		public static Figurehead Turtle
		{
			get
			{
				return DefaultFigureheads.Instance._turtle;
			}
		}

		// Token: 0x17000857 RID: 2135
		// (get) Token: 0x060021B9 RID: 8633 RVA: 0x00093B11 File Offset: 0x00091D11
		public static Figurehead Boar
		{
			get
			{
				return DefaultFigureheads.Instance._boar;
			}
		}

		// Token: 0x17000858 RID: 2136
		// (get) Token: 0x060021BA RID: 8634 RVA: 0x00093B1D File Offset: 0x00091D1D
		public static Figurehead Oxen
		{
			get
			{
				return DefaultFigureheads.Instance._oxen;
			}
		}

		// Token: 0x17000859 RID: 2137
		// (get) Token: 0x060021BB RID: 8635 RVA: 0x00093B29 File Offset: 0x00091D29
		public static Figurehead Swan
		{
			get
			{
				return DefaultFigureheads.Instance._swan;
			}
		}

		// Token: 0x1700085A RID: 2138
		// (get) Token: 0x060021BC RID: 8636 RVA: 0x00093B35 File Offset: 0x00091D35
		public static Figurehead Deer
		{
			get
			{
				return DefaultFigureheads.Instance._deer;
			}
		}

		// Token: 0x1700085B RID: 2139
		// (get) Token: 0x060021BD RID: 8637 RVA: 0x00093B41 File Offset: 0x00091D41
		public static Figurehead Raven
		{
			get
			{
				return DefaultFigureheads.Instance._raven;
			}
		}

		// Token: 0x060021BE RID: 8638 RVA: 0x00093B4D File Offset: 0x00091D4D
		public DefaultFigureheads()
		{
			this.RegisterAll();
		}

		// Token: 0x060021BF RID: 8639 RVA: 0x00093B5C File Offset: 0x00091D5C
		private void RegisterAll()
		{
			this._hawk = MBObjectManager.Instance.RegisterPresumedObject<Figurehead>(new Figurehead("hawk"));
			this._lion = MBObjectManager.Instance.RegisterPresumedObject<Figurehead>(new Figurehead("lion"));
			this._dragon = MBObjectManager.Instance.RegisterPresumedObject<Figurehead>(new Figurehead("dragon"));
			this._wingsOfVictory = MBObjectManager.Instance.RegisterPresumedObject<Figurehead>(new Figurehead("wings_of_victory"));
			this._ram = MBObjectManager.Instance.RegisterPresumedObject<Figurehead>(new Figurehead("ram"));
			this._seaSerpent = MBObjectManager.Instance.RegisterPresumedObject<Figurehead>(new Figurehead("sea_serpent"));
			this._viper = MBObjectManager.Instance.RegisterPresumedObject<Figurehead>(new Figurehead("viper"));
			this._saberToothTiger = MBObjectManager.Instance.RegisterPresumedObject<Figurehead>(new Figurehead("saber_tooth_tiger"));
			this._siren = MBObjectManager.Instance.RegisterPresumedObject<Figurehead>(new Figurehead("siren"));
			this._horse = MBObjectManager.Instance.RegisterPresumedObject<Figurehead>(new Figurehead("horse"));
			this._turtle = MBObjectManager.Instance.RegisterPresumedObject<Figurehead>(new Figurehead("turtle"));
			this._boar = MBObjectManager.Instance.RegisterPresumedObject<Figurehead>(new Figurehead("boar"));
			this._oxen = MBObjectManager.Instance.RegisterPresumedObject<Figurehead>(new Figurehead("oxen"));
			this._swan = MBObjectManager.Instance.RegisterPresumedObject<Figurehead>(new Figurehead("swan"));
			this._deer = MBObjectManager.Instance.RegisterPresumedObject<Figurehead>(new Figurehead("deer"));
			this._raven = MBObjectManager.Instance.RegisterPresumedObject<Figurehead>(new Figurehead("raven"));
			this.InitializeAll();
		}

		// Token: 0x060021C0 RID: 8640 RVA: 0x00093D10 File Offset: 0x00091F10
		private void InitializeAll()
		{
			this._hawk.Initialize(new TextObject("{=VKFTub9a}Hawk", null), new TextObject("{=ku2DXiY9}Crew ranged accuracy {EFFECT_AMOUNT}%", null), 0.15f, MBObjectManager.Instance.GetObject<CultureObject>("aserai"), EffectIncrementType.AddFactor);
			this._lion.Initialize(new TextObject("{=D0SX1cFQ}Lion", null), new TextObject("{=EjjAmdXp}Crew battle morale {EFFECT_AMOUNT}", null), 10f, MBObjectManager.Instance.GetObject<CultureObject>("vlandia"), EffectIncrementType.Add);
			this._dragon.Initialize(new TextObject("{=GkvX7z6Y}Dragon", null), new TextObject("{=4Ok7GnHs}Boarded enemy crew morale {EFFECT_AMOUNT}", null), -5f, MBObjectManager.Instance.GetObject<CultureObject>("nord"), EffectIncrementType.Add);
			this._wingsOfVictory.Initialize(new TextObject("{=ci0npfYB}Wings Of Victory", null), new TextObject("{=mQuaMNVb}Party battle experience {EFFECT_AMOUNT}%", null), 0.15f, MBObjectManager.Instance.GetObject<CultureObject>("empire"), EffectIncrementType.AddFactor);
			this._ram.Initialize(new TextObject("{=gfQdYnsR}Ram", null), new TextObject("{=eJ4MC1KO}Ramming ship and morale damage {EFFECT_AMOUNT}%.", null), 0.2f, MBObjectManager.Instance.GetObject<CultureObject>("empire"), EffectIncrementType.AddFactor);
			this._seaSerpent.Initialize(new TextObject("{=fsb5EEbg}Sea Serpent", null), new TextObject("{=OraB7RjB}Fire damage resistance {EFFECT_AMOUNT}%", null), 0.4f, MBObjectManager.Instance.GetObject<CultureObject>("nord"), EffectIncrementType.AddFactor);
			this._viper.Initialize(new TextObject("{=LTOaBiw3}Viper", null), new TextObject("{=NxIUg152}Ballista reload speed {EFFECT_AMOUNT}", null), 0.25f, MBObjectManager.Instance.GetObject<CultureObject>("aserai"), EffectIncrementType.AddFactor);
			this._saberToothTiger.Initialize(new TextObject("{=113F2KC5}Saber Tooth Tiger", null), new TextObject("{=qWDM0Oa1}Archer armor penetration {EFFECT_AMOUNT}", null), 0.1f, MBObjectManager.Instance.GetObject<CultureObject>("vlandia"), EffectIncrementType.AddFactor);
			this._siren.Initialize(new TextObject("{=wrwdRGkW}Siren", null), new TextObject("{=iBPMtWzZ}Boarded enemy crew melee damage {EFFECT_AMOUNT}%", null), -0.1f, MBObjectManager.Instance.GetObject<CultureObject>("sturgia"), EffectIncrementType.AddFactor);
			this._horse.Initialize(new TextObject("{=LwfILaRH}Horse", null), new TextObject("{=sMCpa5Sk}Ship travel speed {EFFECT_AMOUNT}%", null), 0.1f, MBObjectManager.Instance.GetObject<CultureObject>("khuzait"), EffectIncrementType.AddFactor);
			this._turtle.Initialize(new TextObject("{=Ni8CSaxD}Turtle", null), new TextObject("{=bAWHXCsb}Crew shield hitpoints {EFFECT_AMOUNT}%", null), 0.4f, MBObjectManager.Instance.GetObject<CultureObject>("sturgia"), EffectIncrementType.AddFactor);
			this._boar.Initialize(new TextObject("{=0OrIliBh}Boar", null), new TextObject("{=FPZ9QOGl}Crew armor {EFFECT_AMOUNT}%", null), 0.1f, MBObjectManager.Instance.GetObject<CultureObject>("battania"), EffectIncrementType.AddFactor);
			this._oxen.Initialize(new TextObject("{=mGy1EcUd}Oxen", null), new TextObject("{=D2ZA2XT6}Crew hitpoints {EFFECT_AMOUNT}", null), 10f, MBObjectManager.Instance.GetObject<CultureObject>("battania"), EffectIncrementType.Add);
			this._swan.Initialize(new TextObject("{=ZSA1mySL}Swan", null), new TextObject("{=JJTWn3zs}Sail force {EFFECT_AMOUNT}%", null), 0.1f, MBObjectManager.Instance.GetObject<CultureObject>("empire"), EffectIncrementType.AddFactor);
			this._deer.Initialize(new TextObject("{=XbNVQdZN}Deer", null), new TextObject("{=foC3qNav}Oar force {EFFECT_AMOUNT}%", null), 0.15f, MBObjectManager.Instance.GetObject<CultureObject>("sturgia"), EffectIncrementType.AddFactor);
			this._raven.Initialize(new TextObject("{=NVKwvl1G}Raven", null), new TextObject("{=QsR8WTpA}Crew throwing weapon damage {EFFECT_AMOUNT}%", null), 0.1f, MBObjectManager.Instance.GetObject<CultureObject>("nord"), EffectIncrementType.AddFactor);
		}

		// Token: 0x040009BB RID: 2491
		private Figurehead _hawk;

		// Token: 0x040009BC RID: 2492
		private Figurehead _lion;

		// Token: 0x040009BD RID: 2493
		private Figurehead _dragon;

		// Token: 0x040009BE RID: 2494
		private Figurehead _wingsOfVictory;

		// Token: 0x040009BF RID: 2495
		private Figurehead _ram;

		// Token: 0x040009C0 RID: 2496
		private Figurehead _seaSerpent;

		// Token: 0x040009C1 RID: 2497
		private Figurehead _viper;

		// Token: 0x040009C2 RID: 2498
		private Figurehead _saberToothTiger;

		// Token: 0x040009C3 RID: 2499
		private Figurehead _siren;

		// Token: 0x040009C4 RID: 2500
		private Figurehead _horse;

		// Token: 0x040009C5 RID: 2501
		private Figurehead _turtle;

		// Token: 0x040009C6 RID: 2502
		private Figurehead _boar;

		// Token: 0x040009C7 RID: 2503
		private Figurehead _oxen;

		// Token: 0x040009C8 RID: 2504
		private Figurehead _swan;

		// Token: 0x040009C9 RID: 2505
		private Figurehead _deer;

		// Token: 0x040009CA RID: 2506
		private Figurehead _raven;
	}
}
