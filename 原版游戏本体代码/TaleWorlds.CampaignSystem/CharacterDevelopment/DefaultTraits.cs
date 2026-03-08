using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CharacterDevelopment
{
	// Token: 0x020003A8 RID: 936
	public class DefaultTraits
	{
		// Token: 0x17000CBC RID: 3260
		// (get) Token: 0x060035CE RID: 13774 RVA: 0x000E0819 File Offset: 0x000DEA19
		private static DefaultTraits Instance
		{
			get
			{
				return Campaign.Current.DefaultTraits;
			}
		}

		// Token: 0x17000CBD RID: 3261
		// (get) Token: 0x060035CF RID: 13775 RVA: 0x000E0825 File Offset: 0x000DEA25
		public static TraitObject Frequency
		{
			get
			{
				return DefaultTraits.Instance._traitFrequency;
			}
		}

		// Token: 0x17000CBE RID: 3262
		// (get) Token: 0x060035D0 RID: 13776 RVA: 0x000E0831 File Offset: 0x000DEA31
		public static TraitObject Mercy
		{
			get
			{
				return DefaultTraits.Instance._traitMercy;
			}
		}

		// Token: 0x17000CBF RID: 3263
		// (get) Token: 0x060035D1 RID: 13777 RVA: 0x000E083D File Offset: 0x000DEA3D
		public static TraitObject Valor
		{
			get
			{
				return DefaultTraits.Instance._traitValor;
			}
		}

		// Token: 0x17000CC0 RID: 3264
		// (get) Token: 0x060035D2 RID: 13778 RVA: 0x000E0849 File Offset: 0x000DEA49
		public static TraitObject Honor
		{
			get
			{
				return DefaultTraits.Instance._traitHonor;
			}
		}

		// Token: 0x17000CC1 RID: 3265
		// (get) Token: 0x060035D3 RID: 13779 RVA: 0x000E0855 File Offset: 0x000DEA55
		public static TraitObject Generosity
		{
			get
			{
				return DefaultTraits.Instance._traitGenerosity;
			}
		}

		// Token: 0x17000CC2 RID: 3266
		// (get) Token: 0x060035D4 RID: 13780 RVA: 0x000E0861 File Offset: 0x000DEA61
		public static TraitObject Calculating
		{
			get
			{
				return DefaultTraits.Instance._traitCalculating;
			}
		}

		// Token: 0x17000CC3 RID: 3267
		// (get) Token: 0x060035D5 RID: 13781 RVA: 0x000E086D File Offset: 0x000DEA6D
		public static TraitObject PersonaCurt
		{
			get
			{
				return DefaultTraits.Instance._traitPersonaCurt;
			}
		}

		// Token: 0x17000CC4 RID: 3268
		// (get) Token: 0x060035D6 RID: 13782 RVA: 0x000E0879 File Offset: 0x000DEA79
		public static TraitObject PersonaEarnest
		{
			get
			{
				return DefaultTraits.Instance._traitPersonaEarnest;
			}
		}

		// Token: 0x17000CC5 RID: 3269
		// (get) Token: 0x060035D7 RID: 13783 RVA: 0x000E0885 File Offset: 0x000DEA85
		public static TraitObject PersonaIronic
		{
			get
			{
				return DefaultTraits.Instance._traitPersonaIronic;
			}
		}

		// Token: 0x17000CC6 RID: 3270
		// (get) Token: 0x060035D8 RID: 13784 RVA: 0x000E0891 File Offset: 0x000DEA91
		public static TraitObject PersonaSoftspoken
		{
			get
			{
				return DefaultTraits.Instance._traitPersonaSoftspoken;
			}
		}

		// Token: 0x17000CC7 RID: 3271
		// (get) Token: 0x060035D9 RID: 13785 RVA: 0x000E089D File Offset: 0x000DEA9D
		public static TraitObject Surgery
		{
			get
			{
				return DefaultTraits.Instance._traitSurgery;
			}
		}

		// Token: 0x17000CC8 RID: 3272
		// (get) Token: 0x060035DA RID: 13786 RVA: 0x000E08A9 File Offset: 0x000DEAA9
		public static TraitObject SergeantCommandSkills
		{
			get
			{
				return DefaultTraits.Instance._traitSergeantCommandSkills;
			}
		}

		// Token: 0x17000CC9 RID: 3273
		// (get) Token: 0x060035DB RID: 13787 RVA: 0x000E08B5 File Offset: 0x000DEAB5
		public static TraitObject RogueSkills
		{
			get
			{
				return DefaultTraits.Instance._traitRogueSkills;
			}
		}

		// Token: 0x17000CCA RID: 3274
		// (get) Token: 0x060035DC RID: 13788 RVA: 0x000E08C1 File Offset: 0x000DEAC1
		public static TraitObject Siegecraft
		{
			get
			{
				return DefaultTraits.Instance._traitEngineerSkills;
			}
		}

		// Token: 0x17000CCB RID: 3275
		// (get) Token: 0x060035DD RID: 13789 RVA: 0x000E08CD File Offset: 0x000DEACD
		public static TraitObject ScoutSkills
		{
			get
			{
				return DefaultTraits.Instance._traitScoutSkills;
			}
		}

		// Token: 0x17000CCC RID: 3276
		// (get) Token: 0x060035DE RID: 13790 RVA: 0x000E08D9 File Offset: 0x000DEAD9
		public static TraitObject Blacksmith
		{
			get
			{
				return DefaultTraits.Instance._traitBlacksmith;
			}
		}

		// Token: 0x17000CCD RID: 3277
		// (get) Token: 0x060035DF RID: 13791 RVA: 0x000E08E5 File Offset: 0x000DEAE5
		public static TraitObject Commander
		{
			get
			{
				return DefaultTraits.Instance._traitCommander;
			}
		}

		// Token: 0x17000CCE RID: 3278
		// (get) Token: 0x060035E0 RID: 13792 RVA: 0x000E08F1 File Offset: 0x000DEAF1
		public static TraitObject Trader
		{
			get
			{
				return DefaultTraits.Instance._traitTraderSkills;
			}
		}

		// Token: 0x17000CCF RID: 3279
		// (get) Token: 0x060035E1 RID: 13793 RVA: 0x000E08FD File Offset: 0x000DEAFD
		public static TraitObject Thug
		{
			get
			{
				return DefaultTraits.Instance._traitThug;
			}
		}

		// Token: 0x17000CD0 RID: 3280
		// (get) Token: 0x060035E2 RID: 13794 RVA: 0x000E0909 File Offset: 0x000DEB09
		public static TraitObject Smuggler
		{
			get
			{
				return DefaultTraits.Instance._traitSmuggler;
			}
		}

		// Token: 0x17000CD1 RID: 3281
		// (get) Token: 0x060035E3 RID: 13795 RVA: 0x000E0915 File Offset: 0x000DEB15
		public static TraitObject Egalitarian
		{
			get
			{
				return DefaultTraits.Instance._traitEgalitarian;
			}
		}

		// Token: 0x17000CD2 RID: 3282
		// (get) Token: 0x060035E4 RID: 13796 RVA: 0x000E0921 File Offset: 0x000DEB21
		public static TraitObject Oligarchic
		{
			get
			{
				return DefaultTraits.Instance._traitOligarchic;
			}
		}

		// Token: 0x17000CD3 RID: 3283
		// (get) Token: 0x060035E5 RID: 13797 RVA: 0x000E092D File Offset: 0x000DEB2D
		public static TraitObject Authoritarian
		{
			get
			{
				return DefaultTraits.Instance._traitAuthoritarian;
			}
		}

		// Token: 0x17000CD4 RID: 3284
		// (get) Token: 0x060035E6 RID: 13798 RVA: 0x000E0939 File Offset: 0x000DEB39
		public static TraitObject NavalSoldier
		{
			get
			{
				return DefaultTraits.Instance._traitNavalSoldier;
			}
		}

		// Token: 0x17000CD5 RID: 3285
		// (get) Token: 0x060035E7 RID: 13799 RVA: 0x000E0945 File Offset: 0x000DEB45
		public static IEnumerable<TraitObject> Personality
		{
			get
			{
				return DefaultTraits.Instance._personality;
			}
		}

		// Token: 0x060035E8 RID: 13800 RVA: 0x000E0954 File Offset: 0x000DEB54
		public DefaultTraits()
		{
			this.RegisterAll();
			this._personality = new TraitObject[] { this._traitMercy, this._traitValor, this._traitHonor, this._traitGenerosity, this._traitCalculating };
		}

		// Token: 0x060035E9 RID: 13801 RVA: 0x000E09A8 File Offset: 0x000DEBA8
		public void RegisterAll()
		{
			this._traitFrequency = this.Create("Frequency");
			this._traitMercy = this.Create("Mercy");
			this._traitValor = this.Create("Valor");
			this._traitHonor = this.Create("Honor");
			this._traitGenerosity = this.Create("Generosity");
			this._traitCalculating = this.Create("Calculating");
			this._traitPersonaCurt = this.Create("curt");
			this._traitPersonaIronic = this.Create("ironic");
			this._traitPersonaEarnest = this.Create("earnest");
			this._traitPersonaSoftspoken = this.Create("softspoken");
			this._traitCommander = this.Create("Commander");
			this._traitTraderSkills = this.Create("Trader");
			this._traitSurgery = this.Create("Surgeon");
			this._traitTracking = this.Create("Tracking");
			this._traitBlacksmith = this.Create("Blacksmith");
			this._traitSergeantCommandSkills = this.Create("SergeantCommandSkills");
			this._traitEngineerSkills = this.Create("EngineerSkills");
			this._traitRogueSkills = this.Create("RogueSkills");
			this._traitScoutSkills = this.Create("ScoutSkills");
			this._traitThug = this.Create("Thug");
			this._traitSmuggler = this.Create("Smuggler");
			this._traitEgalitarian = this.Create("Egalitarian");
			this._traitOligarchic = this.Create("Oligarchic");
			this._traitAuthoritarian = this.Create("Authoritarian");
			this._traitNavalSoldier = this.Create("NavalSoldier");
			this.InitializeAll();
		}

		// Token: 0x060035EA RID: 13802 RVA: 0x000E0B64 File Offset: 0x000DED64
		private TraitObject Create(string stringId)
		{
			return Game.Current.ObjectManager.RegisterPresumedObject<TraitObject>(new TraitObject(stringId));
		}

		// Token: 0x060035EB RID: 13803 RVA: 0x000E0B7C File Offset: 0x000DED7C
		private void InitializeAll()
		{
			this._traitFrequency.Initialize(new TextObject("{=vsoyhPnl}Frequency", null), new TextObject("{=!}Frequency Description", null), true, 0, 20);
			this._traitMercy.Initialize(new TextObject("{=2I2uKJlw}Mercy", null), new TextObject("{=Au7VCWTa}Mercy represents your general aversion to suffering and your willingness to help strangers or even enemies.", null), false, -2, 2);
			this._traitValor.Initialize(new TextObject("{=toQLHG6x}Valor", null), new TextObject("{=Ugm9nO49}Valor represents your reputation for risking your life to win glory or wealth or advance your cause.", null), false, -2, 2);
			this._traitHonor.Initialize(new TextObject("{=0oGz5rVx}Honor", null), new TextObject("{=1vYgkaaK}Honor represents your reputation for respecting your formal commitments, like keeping your word and obeying the law.", null), false, -2, 2);
			this._traitGenerosity.Initialize(new TextObject("{=IuWu5Bu7}Generosity", null), new TextObject("{=IKzqzPDS}Generosity represents your loyalty to your kin and those who serve you, and your gratitude to those who have done you a favor.", null), false, -2, 2);
			this._traitCalculating.Initialize(new TextObject("{=5sMBbn7y}Calculating", null), new TextObject("{=QKjF5gTR}Calculating represents your ability to control your emotions for the sake of your long-term interests.", null), false, -2, 2);
			this._traitPersonaCurt.Initialize(new TextObject("{=!}PersonaCurt", null), new TextObject("{=!}PersonaCurt Description", null), false, -2, 2);
			this._traitPersonaIronic.Initialize(new TextObject("{=!}PersonaIronic", null), new TextObject("{=!}PersonaIronic Description", null), false, -2, 2);
			this._traitPersonaEarnest.Initialize(new TextObject("{=!}PersonaEarnest", null), new TextObject("{=!}PersonaEarnest Description", null), false, -2, 2);
			this._traitPersonaSoftspoken.Initialize(new TextObject("{=!}PersonaSoftspoken", null), new TextObject("{=!}PersonaSoftspoken Description", null), false, -2, 2);
			this._traitCommander.Initialize(new TextObject("{=RvKwdXWs}Commander", null), new TextObject("{=!}Commander Description", null), true, 0, 20);
			this._traitSurgery.Initialize(new TextObject("{=QBPrRdQJ}Surgeon", null), new TextObject("{=!}Surgeon Description", null), true, 0, 20);
			this._traitTracking.Initialize(new TextObject("{=dx0hmeH6}Tracking", null), new TextObject("{=!}Tracking Description", null), true, 0, 20);
			this._traitBlacksmith.Initialize(new TextObject("{=bNnQt4jN}Blacksmith", null), new TextObject("{=!}Blacksmith Description", null), true, 0, 20);
			this._traitSergeantCommandSkills.Initialize(new TextObject("{=!}SergeantCommandSkills", null), new TextObject("{=!}SergeantCommandSkills Description", null), true, 0, 20);
			this._traitEngineerSkills.Initialize(new TextObject("{=!}EngineerSkills", null), new TextObject("{=!}EngineerSkills Description", null), true, 0, 20);
			this._traitRogueSkills.Initialize(new TextObject("{=!}RogueSkills", null), new TextObject("{=!}RogueSkills Description", null), true, 0, 20);
			this._traitScoutSkills.Initialize(new TextObject("{=!}ScoutSkills", null), new TextObject("{=!}ScoutSkills Description", null), true, 0, 20);
			this._traitTraderSkills.Initialize(new TextObject("{=!}TraderSkills", null), new TextObject("{=!}Trader Description", null), true, 0, 20);
			this._traitThug.Initialize(new TextObject("{=thugtrait}Thug", null), new TextObject("{=Fjnw9ooa}Indicates a gang member specialized in extortion", null), true, 0, 20);
			this._traitSmuggler.Initialize(new TextObject("{=eeWx1yYd}Smuggler", null), new TextObject("{=87c7IhkZ}Indicates a gang member specialized in smuggling", null), true, 0, 20);
			this._traitEgalitarian.Initialize(new TextObject("{=HMFb1gaq}Egalitarian", null), new TextObject("{=!}Egalitarian Description", null), false, 0, 20);
			this._traitOligarchic.Initialize(new TextObject("{=hR6Zo6pD}Oligarchic", null), new TextObject("{=!}Oligarchic Description", null), false, 0, 20);
			this._traitAuthoritarian.Initialize(new TextObject("{=NaMPa4ML}Authoritarian", null), new TextObject("{=!}Authoritarian Description", null), false, 0, 20);
			this._traitNavalSoldier.Initialize(new TextObject("{=rGUOr2wg}Naval Soldier", null), new TextObject("{=!}Naval Soldier Description", null), true, 0, 20);
		}

		// Token: 0x040010BD RID: 4285
		private const int MaxPersonalityTraitValue = 2;

		// Token: 0x040010BE RID: 4286
		private const int MinPersonalityTraitValue = -2;

		// Token: 0x040010BF RID: 4287
		private const int MaxHiddenTraitValue = 20;

		// Token: 0x040010C0 RID: 4288
		private const int MinHiddenTraitValue = 0;

		// Token: 0x040010C1 RID: 4289
		private TraitObject _traitMercy;

		// Token: 0x040010C2 RID: 4290
		private TraitObject _traitValor;

		// Token: 0x040010C3 RID: 4291
		private TraitObject _traitHonor;

		// Token: 0x040010C4 RID: 4292
		private TraitObject _traitGenerosity;

		// Token: 0x040010C5 RID: 4293
		private TraitObject _traitCalculating;

		// Token: 0x040010C6 RID: 4294
		private TraitObject _traitPersonaCurt;

		// Token: 0x040010C7 RID: 4295
		private TraitObject _traitPersonaEarnest;

		// Token: 0x040010C8 RID: 4296
		private TraitObject _traitPersonaIronic;

		// Token: 0x040010C9 RID: 4297
		private TraitObject _traitPersonaSoftspoken;

		// Token: 0x040010CA RID: 4298
		private TraitObject _traitEgalitarian;

		// Token: 0x040010CB RID: 4299
		private TraitObject _traitOligarchic;

		// Token: 0x040010CC RID: 4300
		private TraitObject _traitAuthoritarian;

		// Token: 0x040010CD RID: 4301
		private TraitObject _traitSurgery;

		// Token: 0x040010CE RID: 4302
		private TraitObject _traitTracking;

		// Token: 0x040010CF RID: 4303
		private TraitObject _traitSergeantCommandSkills;

		// Token: 0x040010D0 RID: 4304
		private TraitObject _traitRogueSkills;

		// Token: 0x040010D1 RID: 4305
		private TraitObject _traitEngineerSkills;

		// Token: 0x040010D2 RID: 4306
		private TraitObject _traitBlacksmith;

		// Token: 0x040010D3 RID: 4307
		private TraitObject _traitScoutSkills;

		// Token: 0x040010D4 RID: 4308
		private TraitObject _traitTraderSkills;

		// Token: 0x040010D5 RID: 4309
		private TraitObject _traitFrequency;

		// Token: 0x040010D6 RID: 4310
		private TraitObject _traitCommander;

		// Token: 0x040010D7 RID: 4311
		private TraitObject _traitThug;

		// Token: 0x040010D8 RID: 4312
		private TraitObject _traitSmuggler;

		// Token: 0x040010D9 RID: 4313
		private TraitObject _traitNavalSoldier;

		// Token: 0x040010DA RID: 4314
		private readonly TraitObject[] _personality;
	}
}
