using System;
using TaleWorlds.Localization;

namespace TaleWorlds.Core
{
	// Token: 0x02000055 RID: 85
	public class DefaultSkills
	{
		// Token: 0x1700027F RID: 639
		// (get) Token: 0x060006D1 RID: 1745 RVA: 0x00017C87 File Offset: 0x00015E87
		private static DefaultSkills Instance
		{
			get
			{
				return Game.Current.DefaultSkills;
			}
		}

		// Token: 0x17000280 RID: 640
		// (get) Token: 0x060006D2 RID: 1746 RVA: 0x00017C93 File Offset: 0x00015E93
		public static SkillObject OneHanded
		{
			get
			{
				return DefaultSkills.Instance._skillOneHanded;
			}
		}

		// Token: 0x17000281 RID: 641
		// (get) Token: 0x060006D3 RID: 1747 RVA: 0x00017C9F File Offset: 0x00015E9F
		public static SkillObject TwoHanded
		{
			get
			{
				return DefaultSkills.Instance._skillTwoHanded;
			}
		}

		// Token: 0x17000282 RID: 642
		// (get) Token: 0x060006D4 RID: 1748 RVA: 0x00017CAB File Offset: 0x00015EAB
		public static SkillObject Polearm
		{
			get
			{
				return DefaultSkills.Instance._skillPolearm;
			}
		}

		// Token: 0x17000283 RID: 643
		// (get) Token: 0x060006D5 RID: 1749 RVA: 0x00017CB7 File Offset: 0x00015EB7
		public static SkillObject Bow
		{
			get
			{
				return DefaultSkills.Instance._skillBow;
			}
		}

		// Token: 0x17000284 RID: 644
		// (get) Token: 0x060006D6 RID: 1750 RVA: 0x00017CC3 File Offset: 0x00015EC3
		public static SkillObject Crossbow
		{
			get
			{
				return DefaultSkills.Instance._skillCrossbow;
			}
		}

		// Token: 0x17000285 RID: 645
		// (get) Token: 0x060006D7 RID: 1751 RVA: 0x00017CCF File Offset: 0x00015ECF
		public static SkillObject Throwing
		{
			get
			{
				return DefaultSkills.Instance._skillThrowing;
			}
		}

		// Token: 0x17000286 RID: 646
		// (get) Token: 0x060006D8 RID: 1752 RVA: 0x00017CDB File Offset: 0x00015EDB
		public static SkillObject Riding
		{
			get
			{
				return DefaultSkills.Instance._skillRiding;
			}
		}

		// Token: 0x17000287 RID: 647
		// (get) Token: 0x060006D9 RID: 1753 RVA: 0x00017CE7 File Offset: 0x00015EE7
		public static SkillObject Athletics
		{
			get
			{
				return DefaultSkills.Instance._skillAthletics;
			}
		}

		// Token: 0x17000288 RID: 648
		// (get) Token: 0x060006DA RID: 1754 RVA: 0x00017CF3 File Offset: 0x00015EF3
		public static SkillObject Crafting
		{
			get
			{
				return DefaultSkills.Instance._skillCrafting;
			}
		}

		// Token: 0x17000289 RID: 649
		// (get) Token: 0x060006DB RID: 1755 RVA: 0x00017CFF File Offset: 0x00015EFF
		public static SkillObject Tactics
		{
			get
			{
				return DefaultSkills.Instance._skillTactics;
			}
		}

		// Token: 0x1700028A RID: 650
		// (get) Token: 0x060006DC RID: 1756 RVA: 0x00017D0B File Offset: 0x00015F0B
		public static SkillObject Scouting
		{
			get
			{
				return DefaultSkills.Instance._skillScouting;
			}
		}

		// Token: 0x1700028B RID: 651
		// (get) Token: 0x060006DD RID: 1757 RVA: 0x00017D17 File Offset: 0x00015F17
		public static SkillObject Roguery
		{
			get
			{
				return DefaultSkills.Instance._skillRoguery;
			}
		}

		// Token: 0x1700028C RID: 652
		// (get) Token: 0x060006DE RID: 1758 RVA: 0x00017D23 File Offset: 0x00015F23
		public static SkillObject Charm
		{
			get
			{
				return DefaultSkills.Instance._skillCharm;
			}
		}

		// Token: 0x1700028D RID: 653
		// (get) Token: 0x060006DF RID: 1759 RVA: 0x00017D2F File Offset: 0x00015F2F
		public static SkillObject Leadership
		{
			get
			{
				return DefaultSkills.Instance._skillLeadership;
			}
		}

		// Token: 0x1700028E RID: 654
		// (get) Token: 0x060006E0 RID: 1760 RVA: 0x00017D3B File Offset: 0x00015F3B
		public static SkillObject Trade
		{
			get
			{
				return DefaultSkills.Instance._skillTrade;
			}
		}

		// Token: 0x1700028F RID: 655
		// (get) Token: 0x060006E1 RID: 1761 RVA: 0x00017D47 File Offset: 0x00015F47
		public static SkillObject Steward
		{
			get
			{
				return DefaultSkills.Instance._skillSteward;
			}
		}

		// Token: 0x17000290 RID: 656
		// (get) Token: 0x060006E2 RID: 1762 RVA: 0x00017D53 File Offset: 0x00015F53
		public static SkillObject Medicine
		{
			get
			{
				return DefaultSkills.Instance._skillMedicine;
			}
		}

		// Token: 0x17000291 RID: 657
		// (get) Token: 0x060006E3 RID: 1763 RVA: 0x00017D5F File Offset: 0x00015F5F
		public static SkillObject Engineering
		{
			get
			{
				return DefaultSkills.Instance._skillEngineering;
			}
		}

		// Token: 0x060006E4 RID: 1764 RVA: 0x00017D6B File Offset: 0x00015F6B
		private SkillObject Create(string stringId)
		{
			return Game.Current.ObjectManager.RegisterPresumedObject<SkillObject>(new SkillObject(stringId));
		}

		// Token: 0x060006E5 RID: 1765 RVA: 0x00017D84 File Offset: 0x00015F84
		private void InitializeAll()
		{
			this._skillOneHanded.Initialize(new TextObject("{=PiHpR4QL}One Handed", null), new TextObject("{=yEkSSqIm}Mastery of fighting with one-handed weapons either with a shield or without.", null), new CharacterAttribute[] { DefaultCharacterAttributes.Vigor });
			this._skillTwoHanded.Initialize(new TextObject("{=t78atYqH}Two Handed", null), new TextObject("{=eoLbkhsY}Mastery of fighting with two-handed weapons of average length such as bigger axes and swords.", null), new CharacterAttribute[] { DefaultCharacterAttributes.Vigor });
			this._skillPolearm.Initialize(new TextObject("{=haax8kMa}Polearm", null), new TextObject("{=iKmXX7i3}Mastery of the spear, lance, staff and other polearms, both one-handed and two-handed.", null), new CharacterAttribute[] { DefaultCharacterAttributes.Vigor });
			this._skillBow.Initialize(new TextObject("{=5rj7xQE4}Bow", null), new TextObject("{=FLf5J3su}Familarity with bows and physical conditioning to shoot with them effectively.", null), new CharacterAttribute[] { DefaultCharacterAttributes.Control });
			this._skillCrossbow.Initialize(new TextObject("{=TTWL7RLe}Crossbow", null), new TextObject("{=haV3nLYA}Knowledge of operating and maintaining crossbows.", null), new CharacterAttribute[] { DefaultCharacterAttributes.Control });
			this._skillThrowing.Initialize(new TextObject("{=2wclahIJ}Throwing", null), new TextObject("{=NwTpATW5}Mastery of throwing projectiles accurately and with power.", null), new CharacterAttribute[] { DefaultCharacterAttributes.Control });
			this._skillRiding.Initialize(new TextObject("{=p9i3zRm9}Riding", null), new TextObject("{=H9Zamrao}The ability to control a horse, to keep your balance when it moves suddenly or unexpectedly, as well as general knowledge of horses, including their care and breeding.", null), new CharacterAttribute[] { DefaultCharacterAttributes.Endurance });
			this._skillAthletics.Initialize(new TextObject("{=skZS2UlW}Athletics", null), new TextObject("{=bVD9j0wI}Physical fitness, speed and balance.", null), new CharacterAttribute[] { DefaultCharacterAttributes.Endurance });
			this._skillCrafting.Initialize(new TextObject("{=smithingskill}Smithing", null), new TextObject("{=xWbkjccP}The knowledge of how to forge metal, match handle to blade, turn poles, sew scales, and other skills useful in the assembly of weapons and armor", null), new CharacterAttribute[] { DefaultCharacterAttributes.Endurance });
			this._skillScouting.Initialize(new TextObject("{=LJ6Krlbr}Scouting", null), new TextObject("{=kmBxaJZd}Knowledge of how to scan the wilderness for life. You can follow tracks, spot movement in the undergrowth, and spot an enemy across the valley from a flash of light on spearpoints or a dustcloud.", null), new CharacterAttribute[] { DefaultCharacterAttributes.Cunning });
			this._skillTactics.Initialize(new TextObject("{=m8o51fc7}Tactics", null), new TextObject("{=FQOFDrAu}Your judgment of how troops will perform in contact. This allows you to make a good prediction of when an unorthodox tactic will work, and when it won't.", null), new CharacterAttribute[] { DefaultCharacterAttributes.Cunning });
			this._skillRoguery.Initialize(new TextObject("{=V0ZMJ0PX}Roguery", null), new TextObject("{=81YLbLok}Experience with the darker side of human life. You can tell when a guard wants a bribe, you know how to intimidate someone, and have a good sense of what you can and can't get away with.", null), new CharacterAttribute[] { DefaultCharacterAttributes.Cunning });
			this._skillCharm.Initialize(new TextObject("{=EGeY1gfs}Charm", null), new TextObject("{=VajIVjkc}The ability to make a person like and trust you. You can make a good guess at people's motivations and the kinds of arguments to which they'll respond.", null), new CharacterAttribute[] { DefaultCharacterAttributes.Social });
			this._skillLeadership.Initialize(new TextObject("{=HsLfmEmb}Leadership", null), new TextObject("{=97EmbcHQ}The ability to inspire. You can fill individuals with confidence and stir up enthusiasm and courage in larger groups.", null), new CharacterAttribute[] { DefaultCharacterAttributes.Social });
			this._skillTrade.Initialize(new TextObject("{=GmcgoiGy}Trade", null), new TextObject("{=lsJMCkZy}Familiarity with the most common goods in the marketplace and their prices, as well as the ability to spot defective goods or tell if you've been shortchanged in quantity", null), new CharacterAttribute[] { DefaultCharacterAttributes.Social });
			this._skillSteward.Initialize(new TextObject("{=stewardskill}Steward", null), new TextObject("{=2K0iVRkW}Ability to organize a group and manage logistics. This helps you to run an estate or administer a town, and can increase the size of a party that you lead or in which you serve as quartermaster.", null), new CharacterAttribute[] { DefaultCharacterAttributes.Intelligence });
			this._skillMedicine.Initialize(new TextObject("{=JKH59XNp}Medicine", null), new TextObject("{=igg5sEh3}Knowledge of how to staunch bleeding, to set broken bones, to remove embedded weapons and clean wounds to prevent infection, and to apply poultices to relieve pain and soothe inflammation.", null), new CharacterAttribute[] { DefaultCharacterAttributes.Intelligence });
			this._skillEngineering.Initialize(new TextObject("{=engineeringskill}Engineering", null), new TextObject("{=hbaMnpVR}Knowledge of how to make things that can withstand powerful forces without collapsing. Useful for building both structures and the devices that knock them down.", null), new CharacterAttribute[] { DefaultCharacterAttributes.Intelligence });
		}

		// Token: 0x060006E6 RID: 1766 RVA: 0x000180F1 File Offset: 0x000162F1
		public DefaultSkills()
		{
			this.RegisterAll();
		}

		// Token: 0x060006E7 RID: 1767 RVA: 0x00018100 File Offset: 0x00016300
		private void RegisterAll()
		{
			this._skillOneHanded = this.Create("OneHanded");
			this._skillTwoHanded = this.Create("TwoHanded");
			this._skillPolearm = this.Create("Polearm");
			this._skillBow = this.Create("Bow");
			this._skillCrossbow = this.Create("Crossbow");
			this._skillThrowing = this.Create("Throwing");
			this._skillRiding = this.Create("Riding");
			this._skillAthletics = this.Create("Athletics");
			this._skillCrafting = this.Create("Crafting");
			this._skillTactics = this.Create("Tactics");
			this._skillScouting = this.Create("Scouting");
			this._skillRoguery = this.Create("Roguery");
			this._skillCharm = this.Create("Charm");
			this._skillTrade = this.Create("Trade");
			this._skillLeadership = this.Create("Leadership");
			this._skillSteward = this.Create("Steward");
			this._skillMedicine = this.Create("Medicine");
			this._skillEngineering = this.Create("Engineering");
			this.InitializeAll();
		}

		// Token: 0x0400035F RID: 863
		private SkillObject _skillEngineering;

		// Token: 0x04000360 RID: 864
		private SkillObject _skillMedicine;

		// Token: 0x04000361 RID: 865
		private SkillObject _skillLeadership;

		// Token: 0x04000362 RID: 866
		private SkillObject _skillSteward;

		// Token: 0x04000363 RID: 867
		private SkillObject _skillTrade;

		// Token: 0x04000364 RID: 868
		private SkillObject _skillCharm;

		// Token: 0x04000365 RID: 869
		private SkillObject _skillRoguery;

		// Token: 0x04000366 RID: 870
		private SkillObject _skillScouting;

		// Token: 0x04000367 RID: 871
		private SkillObject _skillTactics;

		// Token: 0x04000368 RID: 872
		private SkillObject _skillCrafting;

		// Token: 0x04000369 RID: 873
		private SkillObject _skillAthletics;

		// Token: 0x0400036A RID: 874
		private SkillObject _skillRiding;

		// Token: 0x0400036B RID: 875
		private SkillObject _skillThrowing;

		// Token: 0x0400036C RID: 876
		private SkillObject _skillCrossbow;

		// Token: 0x0400036D RID: 877
		private SkillObject _skillBow;

		// Token: 0x0400036E RID: 878
		private SkillObject _skillPolearm;

		// Token: 0x0400036F RID: 879
		private SkillObject _skillTwoHanded;

		// Token: 0x04000370 RID: 880
		private SkillObject _skillOneHanded;
	}
}
