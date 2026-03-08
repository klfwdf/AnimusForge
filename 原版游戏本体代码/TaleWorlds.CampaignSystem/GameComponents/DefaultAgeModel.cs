using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x020000ED RID: 237
	public class DefaultAgeModel : AgeModel
	{
		// Token: 0x170005F9 RID: 1529
		// (get) Token: 0x060015E0 RID: 5600 RVA: 0x00062F55 File Offset: 0x00061155
		public override int BecomeInfantAge
		{
			get
			{
				return 3;
			}
		}

		// Token: 0x170005FA RID: 1530
		// (get) Token: 0x060015E1 RID: 5601 RVA: 0x00062F58 File Offset: 0x00061158
		public override int BecomeChildAge
		{
			get
			{
				return 6;
			}
		}

		// Token: 0x170005FB RID: 1531
		// (get) Token: 0x060015E2 RID: 5602 RVA: 0x00062F5B File Offset: 0x0006115B
		public override int BecomeTeenagerAge
		{
			get
			{
				return 14;
			}
		}

		// Token: 0x170005FC RID: 1532
		// (get) Token: 0x060015E3 RID: 5603 RVA: 0x00062F5F File Offset: 0x0006115F
		public override int HeroComesOfAge
		{
			get
			{
				return 18;
			}
		}

		// Token: 0x170005FD RID: 1533
		// (get) Token: 0x060015E4 RID: 5604 RVA: 0x00062F63 File Offset: 0x00061163
		public override int MiddleAdultHoodAge
		{
			get
			{
				return 35;
			}
		}

		// Token: 0x170005FE RID: 1534
		// (get) Token: 0x060015E5 RID: 5605 RVA: 0x00062F67 File Offset: 0x00061167
		public override int BecomeOldAge
		{
			get
			{
				return 55;
			}
		}

		// Token: 0x170005FF RID: 1535
		// (get) Token: 0x060015E6 RID: 5606 RVA: 0x00062F6B File Offset: 0x0006116B
		public override int MaxAge
		{
			get
			{
				return 128;
			}
		}

		// Token: 0x060015E7 RID: 5607 RVA: 0x00062F74 File Offset: 0x00061174
		public override void GetAgeLimitForLocation(CharacterObject character, out int minimumAge, out int maximumAge, string additionalTags = "")
		{
			if (character.Occupation == Occupation.TavernWench)
			{
				minimumAge = 20;
				maximumAge = 28;
				return;
			}
			if (character.Occupation == Occupation.Townsfolk)
			{
				if (additionalTags == "TavernVisitor")
				{
					minimumAge = 20;
					maximumAge = 60;
					return;
				}
				if (additionalTags == "TavernDrinker")
				{
					minimumAge = 20;
					maximumAge = 40;
					return;
				}
				if (additionalTags == "SlowTownsman")
				{
					minimumAge = 50;
					maximumAge = 70;
					return;
				}
				if (additionalTags == "TownsfolkCarryingStuff")
				{
					minimumAge = 20;
					maximumAge = 40;
					return;
				}
				if (additionalTags == "BroomsWoman")
				{
					minimumAge = 30;
					maximumAge = 45;
					return;
				}
				if (additionalTags == "Dancer")
				{
					minimumAge = 20;
					maximumAge = 28;
					return;
				}
				if (additionalTags == "Beggar")
				{
					minimumAge = 60;
					maximumAge = 90;
					return;
				}
				if (additionalTags == "Child")
				{
					minimumAge = this.BecomeChildAge;
					maximumAge = this.BecomeTeenagerAge;
					return;
				}
				if (additionalTags == "Teenager")
				{
					minimumAge = this.BecomeTeenagerAge;
					maximumAge = this.HeroComesOfAge;
					return;
				}
				if (additionalTags == "Infant")
				{
					minimumAge = this.BecomeInfantAge;
					maximumAge = this.BecomeChildAge;
					return;
				}
				if (additionalTags == "Notary" || additionalTags == "Barber")
				{
					minimumAge = 30;
					maximumAge = 80;
					return;
				}
				minimumAge = this.HeroComesOfAge;
				maximumAge = 70;
				return;
			}
			else if (character.Occupation == Occupation.Villager)
			{
				if (additionalTags == "TownsfolkCarryingStuff")
				{
					minimumAge = 20;
					maximumAge = 40;
					return;
				}
				if (additionalTags == "Child")
				{
					minimumAge = this.BecomeChildAge;
					maximumAge = this.BecomeTeenagerAge;
					return;
				}
				if (additionalTags == "Teenager")
				{
					minimumAge = this.BecomeTeenagerAge;
					maximumAge = this.HeroComesOfAge;
					return;
				}
				if (additionalTags == "Infant")
				{
					minimumAge = this.BecomeInfantAge;
					maximumAge = this.BecomeChildAge;
					return;
				}
				minimumAge = this.HeroComesOfAge;
				maximumAge = 70;
				return;
			}
			else
			{
				if (character.Occupation == Occupation.TavernGameHost)
				{
					minimumAge = 30;
					maximumAge = 40;
					return;
				}
				if (character.Occupation == Occupation.Musician)
				{
					minimumAge = 20;
					maximumAge = 40;
					return;
				}
				if (character.Occupation == Occupation.ArenaMaster)
				{
					minimumAge = 30;
					maximumAge = 60;
					return;
				}
				if (character.Occupation == Occupation.ShopWorker)
				{
					minimumAge = 18;
					maximumAge = 50;
					return;
				}
				if (character.Occupation == Occupation.Tavernkeeper)
				{
					minimumAge = 40;
					maximumAge = 80;
					return;
				}
				if (character.Occupation == Occupation.RansomBroker)
				{
					minimumAge = 30;
					maximumAge = 60;
					return;
				}
				if (character.Occupation == Occupation.Blacksmith || character.Occupation == Occupation.GoodsTrader || character.Occupation == Occupation.HorseTrader || character.Occupation == Occupation.Armorer || character.Occupation == Occupation.Weaponsmith)
				{
					minimumAge = 30;
					maximumAge = 80;
					return;
				}
				if (additionalTags == "AlleyGangMember")
				{
					minimumAge = 30;
					maximumAge = 40;
					return;
				}
				minimumAge = this.HeroComesOfAge;
				maximumAge = this.MaxAge;
				return;
			}
		}

		// Token: 0x04000735 RID: 1845
		public const string TavernVisitorTag = "TavernVisitor";

		// Token: 0x04000736 RID: 1846
		public const string TavernDrinkerTag = "TavernDrinker";

		// Token: 0x04000737 RID: 1847
		public const string SlowTownsmanTag = "SlowTownsman";

		// Token: 0x04000738 RID: 1848
		public const string TownsfolkCarryingStuffTag = "TownsfolkCarryingStuff";

		// Token: 0x04000739 RID: 1849
		public const string BroomsWomanTag = "BroomsWoman";

		// Token: 0x0400073A RID: 1850
		public const string DancerTag = "Dancer";

		// Token: 0x0400073B RID: 1851
		public const string BeggarTag = "Beggar";

		// Token: 0x0400073C RID: 1852
		public const string ChildTag = "Child";

		// Token: 0x0400073D RID: 1853
		public const string TeenagerTag = "Teenager";

		// Token: 0x0400073E RID: 1854
		public const string InfantTag = "Infant";

		// Token: 0x0400073F RID: 1855
		public const string NotaryTag = "Notary";

		// Token: 0x04000740 RID: 1856
		public const string BarberTag = "Barber";

		// Token: 0x04000741 RID: 1857
		public const string AlleyGangMemberTag = "AlleyGangMember";
	}
}
