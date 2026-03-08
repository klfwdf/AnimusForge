using System;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200007F RID: 127
	public class DefaultPolicies
	{
		// Token: 0x170003FD RID: 1021
		// (get) Token: 0x0600107E RID: 4222 RVA: 0x0004E8D3 File Offset: 0x0004CAD3
		private static DefaultPolicies Instance
		{
			get
			{
				return Campaign.Current.DefaultPolicies;
			}
		}

		// Token: 0x170003FE RID: 1022
		// (get) Token: 0x0600107F RID: 4223 RVA: 0x0004E8DF File Offset: 0x0004CADF
		public static PolicyObject LandTax
		{
			get
			{
				return DefaultPolicies.Instance._policyLandTax;
			}
		}

		// Token: 0x170003FF RID: 1023
		// (get) Token: 0x06001080 RID: 4224 RVA: 0x0004E8EB File Offset: 0x0004CAEB
		public static PolicyObject StateMonopolies
		{
			get
			{
				return DefaultPolicies.Instance._policyStateMonopolies;
			}
		}

		// Token: 0x17000400 RID: 1024
		// (get) Token: 0x06001081 RID: 4225 RVA: 0x0004E8F7 File Offset: 0x0004CAF7
		public static PolicyObject SacredMajesty
		{
			get
			{
				return DefaultPolicies.Instance._policySacredMajesty;
			}
		}

		// Token: 0x17000401 RID: 1025
		// (get) Token: 0x06001082 RID: 4226 RVA: 0x0004E903 File Offset: 0x0004CB03
		public static PolicyObject Magistrates
		{
			get
			{
				return DefaultPolicies.Instance._policyMagistrates;
			}
		}

		// Token: 0x17000402 RID: 1026
		// (get) Token: 0x06001083 RID: 4227 RVA: 0x0004E90F File Offset: 0x0004CB0F
		public static PolicyObject DebasementOfTheCurrency
		{
			get
			{
				return DefaultPolicies.Instance._policyDebasementOfTheCurrency;
			}
		}

		// Token: 0x17000403 RID: 1027
		// (get) Token: 0x06001084 RID: 4228 RVA: 0x0004E91B File Offset: 0x0004CB1B
		public static PolicyObject PrecarialLandTenure
		{
			get
			{
				return DefaultPolicies.Instance._policyPrecarialLandTenure;
			}
		}

		// Token: 0x17000404 RID: 1028
		// (get) Token: 0x06001085 RID: 4229 RVA: 0x0004E927 File Offset: 0x0004CB27
		public static PolicyObject CrownDuty
		{
			get
			{
				return DefaultPolicies.Instance._policyCrownDuty;
			}
		}

		// Token: 0x17000405 RID: 1029
		// (get) Token: 0x06001086 RID: 4230 RVA: 0x0004E933 File Offset: 0x0004CB33
		public static PolicyObject ImperialTowns
		{
			get
			{
				return DefaultPolicies.Instance._policyImperialTowns;
			}
		}

		// Token: 0x17000406 RID: 1030
		// (get) Token: 0x06001087 RID: 4231 RVA: 0x0004E93F File Offset: 0x0004CB3F
		public static PolicyObject RoyalCommissions
		{
			get
			{
				return DefaultPolicies.Instance._policyRoyalCommissions;
			}
		}

		// Token: 0x17000407 RID: 1031
		// (get) Token: 0x06001088 RID: 4232 RVA: 0x0004E94B File Offset: 0x0004CB4B
		public static PolicyObject RoyalGuard
		{
			get
			{
				return DefaultPolicies.Instance._policyRoyalGuard;
			}
		}

		// Token: 0x17000408 RID: 1032
		// (get) Token: 0x06001089 RID: 4233 RVA: 0x0004E957 File Offset: 0x0004CB57
		public static PolicyObject WarTax
		{
			get
			{
				return DefaultPolicies.Instance._policyWarTax;
			}
		}

		// Token: 0x17000409 RID: 1033
		// (get) Token: 0x0600108A RID: 4234 RVA: 0x0004E963 File Offset: 0x0004CB63
		public static PolicyObject RoyalPrivilege
		{
			get
			{
				return DefaultPolicies.Instance._policyRoyalPrivilege;
			}
		}

		// Token: 0x1700040A RID: 1034
		// (get) Token: 0x0600108B RID: 4235 RVA: 0x0004E96F File Offset: 0x0004CB6F
		public static PolicyObject Senate
		{
			get
			{
				return DefaultPolicies.Instance._policySenate;
			}
		}

		// Token: 0x1700040B RID: 1035
		// (get) Token: 0x0600108C RID: 4236 RVA: 0x0004E97B File Offset: 0x0004CB7B
		public static PolicyObject LordsPrivyCouncil
		{
			get
			{
				return DefaultPolicies.Instance._policyLordsPrivyCouncil;
			}
		}

		// Token: 0x1700040C RID: 1036
		// (get) Token: 0x0600108D RID: 4237 RVA: 0x0004E987 File Offset: 0x0004CB87
		public static PolicyObject MilitaryCoronae
		{
			get
			{
				return DefaultPolicies.Instance._policyMilitaryCoronae;
			}
		}

		// Token: 0x1700040D RID: 1037
		// (get) Token: 0x0600108E RID: 4238 RVA: 0x0004E993 File Offset: 0x0004CB93
		public static PolicyObject FeudalInheritance
		{
			get
			{
				return DefaultPolicies.Instance._policyFeudalInheritance;
			}
		}

		// Token: 0x1700040E RID: 1038
		// (get) Token: 0x0600108F RID: 4239 RVA: 0x0004E99F File Offset: 0x0004CB9F
		public static PolicyObject Serfdom
		{
			get
			{
				return DefaultPolicies.Instance._policySerfdom;
			}
		}

		// Token: 0x1700040F RID: 1039
		// (get) Token: 0x06001090 RID: 4240 RVA: 0x0004E9AB File Offset: 0x0004CBAB
		public static PolicyObject NobleRetinues
		{
			get
			{
				return DefaultPolicies.Instance._policyNobleRetinues;
			}
		}

		// Token: 0x17000410 RID: 1040
		// (get) Token: 0x06001091 RID: 4241 RVA: 0x0004E9B7 File Offset: 0x0004CBB7
		public static PolicyObject CastleCharters
		{
			get
			{
				return DefaultPolicies.Instance._policyCastleCharters;
			}
		}

		// Token: 0x17000411 RID: 1041
		// (get) Token: 0x06001092 RID: 4242 RVA: 0x0004E9C3 File Offset: 0x0004CBC3
		public static PolicyObject Bailiffs
		{
			get
			{
				return DefaultPolicies.Instance._policyBailiffs;
			}
		}

		// Token: 0x17000412 RID: 1042
		// (get) Token: 0x06001093 RID: 4243 RVA: 0x0004E9CF File Offset: 0x0004CBCF
		public static PolicyObject HuntingRights
		{
			get
			{
				return DefaultPolicies.Instance._policyHuntingRights;
			}
		}

		// Token: 0x17000413 RID: 1043
		// (get) Token: 0x06001094 RID: 4244 RVA: 0x0004E9DB File Offset: 0x0004CBDB
		public static PolicyObject RoadTolls
		{
			get
			{
				return DefaultPolicies.Instance._policyRoadTolls;
			}
		}

		// Token: 0x17000414 RID: 1044
		// (get) Token: 0x06001095 RID: 4245 RVA: 0x0004E9E7 File Offset: 0x0004CBE7
		public static PolicyObject Marshals
		{
			get
			{
				return DefaultPolicies.Instance._policyMarshals;
			}
		}

		// Token: 0x17000415 RID: 1045
		// (get) Token: 0x06001096 RID: 4246 RVA: 0x0004E9F3 File Offset: 0x0004CBF3
		public static PolicyObject CouncilOfTheCommons
		{
			get
			{
				return DefaultPolicies.Instance._policyCouncilOfTheCommons;
			}
		}

		// Token: 0x17000416 RID: 1046
		// (get) Token: 0x06001097 RID: 4247 RVA: 0x0004E9FF File Offset: 0x0004CBFF
		public static PolicyObject ForgivenessOfDebts
		{
			get
			{
				return DefaultPolicies.Instance._policyForgivenessOfDebts;
			}
		}

		// Token: 0x17000417 RID: 1047
		// (get) Token: 0x06001098 RID: 4248 RVA: 0x0004EA0B File Offset: 0x0004CC0B
		public static PolicyObject Citizenship
		{
			get
			{
				return DefaultPolicies.Instance._policyCitizenship;
			}
		}

		// Token: 0x17000418 RID: 1048
		// (get) Token: 0x06001099 RID: 4249 RVA: 0x0004EA17 File Offset: 0x0004CC17
		public static PolicyObject TribunesOfThePeople
		{
			get
			{
				return DefaultPolicies.Instance._policyTribunesOfThePeople;
			}
		}

		// Token: 0x17000419 RID: 1049
		// (get) Token: 0x0600109A RID: 4250 RVA: 0x0004EA23 File Offset: 0x0004CC23
		public static PolicyObject GrazingRights
		{
			get
			{
				return DefaultPolicies.Instance._policyGrazingRights;
			}
		}

		// Token: 0x1700041A RID: 1050
		// (get) Token: 0x0600109B RID: 4251 RVA: 0x0004EA2F File Offset: 0x0004CC2F
		public static PolicyObject Lawspeakers
		{
			get
			{
				return DefaultPolicies.Instance._policyLawspeakers;
			}
		}

		// Token: 0x1700041B RID: 1051
		// (get) Token: 0x0600109C RID: 4252 RVA: 0x0004EA3B File Offset: 0x0004CC3B
		public static PolicyObject TrialByJury
		{
			get
			{
				return DefaultPolicies.Instance._policyTrialByJury;
			}
		}

		// Token: 0x1700041C RID: 1052
		// (get) Token: 0x0600109D RID: 4253 RVA: 0x0004EA47 File Offset: 0x0004CC47
		public static PolicyObject Cantons
		{
			get
			{
				return DefaultPolicies.Instance._policyCantons;
			}
		}

		// Token: 0x1700041D RID: 1053
		// (get) Token: 0x0600109E RID: 4254 RVA: 0x0004EA53 File Offset: 0x0004CC53
		public static PolicyObject LandGrantsForVeteran
		{
			get
			{
				return DefaultPolicies.Instance._policyLandGrandsForVeteran;
			}
		}

		// Token: 0x0600109F RID: 4255 RVA: 0x0004EA5F File Offset: 0x0004CC5F
		public DefaultPolicies()
		{
			this.RegisterAll();
		}

		// Token: 0x060010A0 RID: 4256 RVA: 0x0004EA70 File Offset: 0x0004CC70
		private void RegisterAll()
		{
			this._policyLandTax = this.Create("policy_land_tax");
			this._policyStateMonopolies = this.Create("policy_state_monopolies");
			this._policySacredMajesty = this.Create("policy_sacred_majesty");
			this._policyMagistrates = this.Create("policy_magistrates");
			this._policyDebasementOfTheCurrency = this.Create("policy_debasement_of_the_currency");
			this._policyPrecarialLandTenure = this.Create("policy_precarial_land_tenure");
			this._policyCrownDuty = this.Create("policy_crown_duty");
			this._policyImperialTowns = this.Create("policy_imperial_towns");
			this._policyRoyalCommissions = this.Create("policy_royal_commissions");
			this._policyRoyalGuard = this.Create("policy_royal_guard");
			this._policyWarTax = this.Create("policy_war_tax");
			this._policyRoyalPrivilege = this.Create("policy_royal_privilege");
			this._policySenate = this.Create("policy_senate");
			this._policyLordsPrivyCouncil = this.Create("policy_lords_privy_council");
			this._policyMilitaryCoronae = this.Create("policy_military_coronae");
			this._policyFeudalInheritance = this.Create("policy_feudal_inheritance");
			this._policySerfdom = this.Create("policy_serfdom");
			this._policyNobleRetinues = this.Create("policy_noble_retinues");
			this._policyCastleCharters = this.Create("policy_castle_charters");
			this._policyBailiffs = this.Create("policy_bailiffs");
			this._policyHuntingRights = this.Create("policy_hunting_rights");
			this._policyRoadTolls = this.Create("policy_road_tolls");
			this._policyMarshals = this.Create("policy_marshals");
			this._policyCouncilOfTheCommons = this.Create("policy_council_of_the_commons");
			this._policyCitizenship = this.Create("policy_citizenship");
			this._policyForgivenessOfDebts = this.Create("policy_forgiveness_of_debts");
			this._policyTribunesOfThePeople = this.Create("policy_tribunes_of_the_people");
			this._policyGrazingRights = this.Create("policy_grazing_rights");
			this._policyLawspeakers = this.Create("policy_lawspeakers");
			this._policyTrialByJury = this.Create("policy_trial_by_jury");
			this._policyCantons = this.Create("policy_cantons");
			this._policyLandGrandsForVeteran = this.Create("policy_land_grands_for_veteran");
			this.InitializeAll();
		}

		// Token: 0x060010A1 RID: 4257 RVA: 0x0004ECA3 File Offset: 0x0004CEA3
		private PolicyObject Create(string stringId)
		{
			return Game.Current.ObjectManager.RegisterPresumedObject<PolicyObject>(new PolicyObject(stringId));
		}

		// Token: 0x060010A2 RID: 4258 RVA: 0x0004ECBC File Offset: 0x0004CEBC
		private void InitializeAll()
		{
			this._policyLandTax.Initialize(new TextObject("{=Tw2FaO0m}Land Tax", null), new TextObject("{=MWvzJSH1}A shift in the tax system that put more emphasis on property and less on the head tax charged to everyone could collect more from wealthy landowners.", null), new TextObject("{=0I6xdead}taxing landowners on their property", null), new TextObject("{=OWwPj500}5% of the village income is paid to the ruler clan as tax{newline}5% less village income for clans", null), 0.7f, 0.15f, -0.7f);
			this._policyStateMonopolies.Initialize(new TextObject("{=SXCudsWT}State Monopolies", null), new TextObject("{=2Qx4bL66}The ruler has a monopoly on certain goods, although practically he can license out production to merchants and collect a portion of the proceeds.", null), new TextObject("{=DIQF5bAX}giving the state monopolies on key goods", null), new TextObject("{=kIVfA5cv}Ruler clan gains 5% of settlement as tax per town{newline}Workshop production is decreased by 10%", null), 0.75f, 0.1f, -0.6f);
			this._policySacredMajesty.Initialize(new TextObject("{=rqHFujhr}Sacred Majesty", null), new TextObject("{=HQGUoRTW}The ruler is considered semi-divine and certain rituals are to be performed in his or her presence, increasing his or her air of authority.", null), new TextObject("{=00bd3Huo}performing new rituals that treat the ruler as semi-divine", null), new TextObject("{=RbjN7VQ2}Ruler clan earns 3 influence per day{newline}Non-ruler clans lose 0.5 influence per day", null), 0.85f, 0.1f, -0.9f);
			this._policyMagistrates.Initialize(new TextObject("{=ZhlpuMxb}Magistrates", null), new TextObject("{=QlZc9CNQ}Rulers could appoint magistrates to rule in disputes and solve crimes. This could cut down on gang activity and lawlessness, but was often greatly resented by communities.", null), new TextObject("{=E3b8iKjc}allowing the ruler to appoint magistrates", null), new TextObject("{=QLNfsk93}Town security is increased by 1 per day{newline}Town taxes are reduced by 5%", null), 0.6f, 0.35f, 0.1f);
			this._policyDebasementOfTheCurrency.Initialize(new TextObject("{=9lygvooT}Debasement Of The Currency", null), new TextObject("{=5WR5DsOz}Rulers could make money fast by debasing the currency and minting more, but this would cause prices to rise.", null), new TextObject("{=EmXai6dm}debasing the currency", null), new TextObject("{=6kn630ka}Ruler clan gains 100 denars per day for each town in the kingdom{newline}Settlement loyalty is decreased by 1 per day", null), 0.7f, 0.1f, -0.7f);
			this._policyPrecarialLandTenure.Initialize(new TextObject("{=ft08QZrA}Precarial Land Tenure", null), new TextObject("{=MF40cm9k}Land grants are considered to be temporary offices rather than the rightful inheritance of lords. In practice heirs tend to take over their family fiefs, but it's easier under Depositions to remove them.", null), new TextObject("{=Vl8BHCvF}allowing the ruler to easily revoke fiefs", null), new TextObject("{=6DQjKwt2}The influence cost of proposing settlement annexation is reduced by 50% for the ruler clan", null), 0.75f, 0f, -0.6f);
			this._policyCrownDuty.Initialize(new TextObject("{=dfyNVkFV}Crown Duty", null), new TextObject("{=elvb5y2I}The ruler is allowed to impose special taxes on trade in towns, payable directly to the royal or imperial treasury.", null), new TextObject("{=w3GIUD3u}allowing the ruler to collect special tariffs", null), new TextObject("{=bDANTRwA}5% tax on tariffs is paid to the ruler clan{newline}Higher trade penalty in towns{newline}Town prosperity is decreased by 1 per day", null), 0.75f, 0.15f, -0.4f);
			this._policyImperialTowns.Initialize(new TextObject("{=l5kDEI6N}Imperial Towns", null), new TextObject("{=ieIWHAtc}A ruler can grant towns special privileges based on their 'immediacy', special access to his person without going through lords or other vassals.", null), new TextObject("{=QoubP07U}allowing the ruler to grant special privileges to towns", null), new TextObject("{=8qwvZ15E}Towns held by the ruler clan gain 1 Loyalty and 1 Prosperity per day{newline}Towns held by non-ruler clans lose 0.3 Loyalty per day", null), 0.7f, 0.15f, -0.3f);
			this._policyRoyalCommissions.Initialize(new TextObject("{=XYDhkb6h}Royal Commissions", null), new TextObject("{=AzKq8AfI}In theory, the king or the emperor has the sole right to command men in the field. Anyone commanding an army does so in the king's name.", null), new TextObject("{=kPqs7EIf}giving the ruler more power to summon armies", null), new TextObject("{=L9n6yu1X}The influence cost of creating an army is reduced by 30% for the ruler{newline}Armies led by the ruler earn cohesion at 30% less cost{newline}Armies led by non-ruler nobles cost 10% more influence to create", null), 0.65f, 0f, -0.45f);
			this._policyRoyalGuard.Initialize(new TextObject("{=F41aPt80}Royal Guard", null), new TextObject("{=eibt105C}The ruler maintains a prestigious personal guard. It attracts warriors who might otherwise serve their local lord.", null), new TextObject("{=bhoCBIWB}authorizing the ruler to have a large private bodyguard", null), new TextObject("{=GwAZMQ8b}Ruler's party size is increased by 60{newline}Non-ruling clans lose 0.2 influence per day.", null), 0.75f, 0f, -0.5f);
			this._policyWarTax.Initialize(new TextObject("{=AlZB8WIb}War Tax", null), new TextObject("{=b4TeiuoJ}Exceptional taxes were often applied in wartime.", null), new TextObject("{=76TgEba5}letting the ruler collect extra taxes in wartime", null), new TextObject("{=O4iki0FD}Ruler gains 5% tax from all settlements{newline}Towns lose 1 prosperity per day{newline}The influence cost of declaring war is doubled for the ruler clan", null), 0.7f, -0.1f, -0.65f);
			this._policyRoyalPrivilege.Initialize(new TextObject("{=Rl1AHKSp}Royal Privilege", null), new TextObject("{=ifnnu3g4}There is a long list of reasons why a ruler can reject a law passed by the council. A ruler does not need to search long to find an excuse for a veto.", null), new TextObject("{=aKLak7nn}giving the ruler broader powers to veto laws", null), new TextObject("{=DG3JbOa2}For kingdom decisions, the influence cost of the ruler overriding the popular decision outcome is reduced by 20%", null), 0.8f, -0.15f, -0.75f);
			this._policySenate.Initialize(new TextObject("{=8pjMAqOg}Senate", null), new TextObject("{=D3W9Qi0Z}All lords have a formal role on the council.", null), new TextObject("{=lXSeaba5}having the lords of the realm meet as a permanent council", null), new TextObject("{=TsvBHBdX}Tier 3+ clans gain 0.5 influence per day, influence cost of inviting lower tier clans to army are increased by 10%", null), -0.7f, 0.85f, 0.7f);
			this._policyLordsPrivyCouncil.Initialize(new TextObject("{=JaZ7T2Wj}Lords' Privy Council", null), new TextObject("{=on2EmlUT}A small council of the greatest lords of the realm. This gives the main clans extra influence, but prevents other clans from climbing into their ranks.", null), new TextObject("{=bxWITUaN}having the greatest lords of the realm meet as a small privy council", null), new TextObject("{=LpdAa1NY}Tier 5+ clans gain 0.5 influence per day, influence cost of inviting lower tier clans to army are increased by 20%", null), -0.5f, 0.7f, -0.15f);
			this._policyMilitaryCoronae.Initialize(new TextObject("{=IBlJ42MN}Military Coronae", null), new TextObject("{=ceq6ZMIx}Lords can vote to award each other decorations and distinctions for battlefield achievements, allowing them greater opportunities to share military glory with the ruler.", null), new TextObject("{=EG06bDxi}granting awards for deeds in the field", null), new TextObject("{=uCXGZ2YP}Military achievements grant 20% more influence{newline}Troop wages are increased by 10%", null), -0.15f, 0.6f, 0.35f);
			this._policyFeudalInheritance.Initialize(new TextObject("{=xbvei0Cb}Feudal Inheritance", null), new TextObject("{=AlFTInfU}States with strict and formal laws of inheritance make it more difficult to revoke land.", null), new TextObject("{=fj5mYvNE}recognizing the formal inheritance of fiefs", null), new TextObject("{=aWWzrwAw}The cost of revoking a fief from a clan is doubled{newline}Clans gain 0.1 influence for each fief they own", null), -0.75f, 0.75f, 0.65f);
			this._policySerfdom.Initialize(new TextObject("{=8FPCRv5L}Serfdom", null), new TextObject("{=0Qh7Pa9E}Tenants are forbidden from leaving the lands of their lords without notice.", null), new TextObject("{=5Wld45hP}forbidding tenants from leaving their lords' lands", null), new TextObject("{=H9Px95rR}Villages grant 0.2 influence per day to the owner clan{newline}Towns gain 1 security but lose 1 militia per day", null), -0.4f, 0.5f, -0.25f);
			this._policyNobleRetinues.Initialize(new TextObject("{=7Pk3bFPC}Noble Retinues", null), new TextObject("{=yXb8bphB}Nobles are expected to raise sizable retinues.", null), new TextObject("{=W3xUQxAa}encouraging lords to have large private armies", null), new TextObject("{=LIjN3rYD}Tier 5+ clans lose 1 influence per day and the party size of their leaders is increased by 40", null), -0.35f, 0.65f, -0.45f);
			this._policyCastleCharters.Initialize(new TextObject("{=W6XMWJ8R}Castle Charters", null), new TextObject("{=t8TfagYL}Nobles are encouraged to fortify their estates, and can requisition labor and materials to do so.", null), new TextObject("{=hsIvt3WC}encouraging lords to fortify their estates", null), new TextObject("{=RdbLbpgO}Castle upgrade costs are reduced by 20%", null), -0.65f, 0.45f, 0f);
			this._policyBailiffs.Initialize(new TextObject("{=HKT5MnjP}Bailiffs", null), new TextObject("{=nmnp9S7k}Nobles have the right to appoint bailiffs.", null), new TextObject("{=GFnqIuxy}encouraging lords to appoint bailiffs in their fiefs", null), new TextObject("{=XFcmlN1J}Town security is increased by 1 per day{newline}Towns with a security greater than 60 yield 1 additional influence to the owner clan.{newline}Tax from towns are reduced by 5%", null), 0f, 0.4f, -0.1f);
			this._policyHuntingRights.Initialize(new TextObject("{=0mKYUNb8}Hunting Rights", null), new TextObject("{=ZMTkq5TG}Nobles and other landowners have exclusive rights to hunt in forests.", null), new TextObject("{=gOn7a7if}granting lords exclusive hunting rights to nearby forests", null), new TextObject("{=agaSYd5t}Food production in towns and castles are increased by 2{newline}Town loyalty is decreased by 0.2", null), -0.2f, 0.35f, -0.15f);
			this._policyRoadTolls.Initialize(new TextObject("{=bOtYmSP8}Road Tolls", null), new TextObject("{=nP7KOISK}Local landowners have the right to collect tolls on commerce.", null), new TextObject("{=upK62aLR}allowing lords tolls on roads running through their lands", null), new TextObject("{=dLkfbU0a}Trade tax paid to the town owner is increased by 3%{newline}Town prosperity is decreased by 0.2", null), -0.5f, 0.45f, -0.35f);
			this._policyMarshals.Initialize(new TextObject("{=WSU35a7F}Marshals", null), new TextObject("{=7EP0NgzU}The highest ranking of nobles have the de facto right to assemble large armies.", null), new TextObject("{=cBAVR7e8}granting high-ranking nobles the right to summon large armies", null), new TextObject("{=0wxRZ9AV}Armies led by Tier 5+ nobles require 10% less influence{newline}Influence of the ruler clan is reduced by 1 per day", null), -0.45f, 0.5f, 0f);
			this._policyCouncilOfTheCommons.Initialize(new TextObject("{=bMSI9Bt3}Council of the Commons", null), new TextObject("{=55CsWKbg}Some kingdoms, especially those that evolved from a city-state or a tribe, had popular assemblies that most of its members had the right to attend. Its powers were often limited, since it could only meet periodically, but it still gave the public the right to participate in government.", null), new TextObject("{=srByX06Y}letting all citizens meet and vote on some issues", null), new TextObject("{=UZ0mPm8b}Each notable yields 0.1 influence per day to the settlement's owner clan{newline}Tax from fortifications 5% decreased", null), -0.5f, 0.1f, 0.7f);
			this._policyForgivenessOfDebts.Initialize(new TextObject("{=Vzsu5nZV}Forgiveness of Debts", null), new TextObject("{=Lgmisw4L}This reform limits the degree to which lords and merchants can lend to their tenants and employees and then demand repayment, or seize their assets or their freedom. It effectively bans serfdom.", null), new TextObject("{=9YKV4SNH}restricting what creditor may do to collect debts", null), new TextObject("{=xJ2uDcob}Settlement loyalty is increased by 2 per day{newline}Settlement production is reduced by 5%", null), -0.4f, -0.4f, 0.6f);
			this._policyCitizenship.Initialize(new TextObject("{=sYNFwOVg}Citizenship", null), new TextObject("{=O5sBO9sQ}Many empires granted their populations citizenship, which usually came with a series of rights. Of course, citizenship could not be granted immediately to conquered provinces until the population showed it was willing to adopt the ways of the empire, including its language, clothes, and religious cults.", null), new TextObject("{=dvEkfaab}recognizing the common folk in the realm as full citizens", null), new TextObject("{=qEOXka0Q}+0.5 Loyalty per day to settlements that have the same culture as their owner clan{newline}Settlement militia production is increased by 1{newline}-0.5 Loyalty per day to settlements with a different culture than its owner clan", null), -0.65f, -0.35f, 0.7f);
			this._policyTribunesOfThePeople.Initialize(new TextObject("{=IJdGTOAe}Tribunes of the People", null), new TextObject("{=VzmzX9Ln}This revives the tribunate, an institution in ancient oligarchic republics that gave representation to families without patrician standing. These elected officers could veto legislation from the more aristocratic Senate.", null), new TextObject("{=auftprN9}allowing the common people to elect tribes to represent them", null), new TextObject("{=YOBeOFxY}Town taxes paid to the ruler are reduced by 5%{newline}Town loyalty is increased by 1 per day", null), -0.6f, -0.2f, 0.55f);
			this._policyGrazingRights.Initialize(new TextObject("{=mb25Ue3f}Grazing Rights", null), new TextObject("{=fjj0pJXV}Landowners could often assert legal rights to common areas and charge villages money to use them. If ordinary people petitioned a ruler, however, he might give them the right to use all common areas for hunting or grazing as members of the village.", null), new TextObject("{=1Y5p40uP}granting villagers the right to graze on land held in common", null), new TextObject("{=PG8anNca}Settlement loyalty is increased by 0.5 per day{newline}Daily hearth production at villages decreases by 0.25 per day", null), -0.75f, -0.3f, 0.7f);
			this._policyLawspeakers.Initialize(new TextObject("{=EBCV0LcU}Lawspeakers", null), new TextObject("{=U7s1LycQ}This refers to the practice of appointing independent elders to remind the council of the law and past precedents. It allows commoners without education to make complex legal arguments.", null), new TextObject("{=7kNxogN8}appointing independent elders to uphold the law", null), new TextObject("{=bFEdxs6Y}All clans whose leader has high Charm gain 1 influence per day{newline}All clans whose leader has low Charm lose 1 influence per day", null), 0f, 0.25f, 0.45f);
			this._policyTrialByJury.Initialize(new TextObject("{=yNAzCfxc}Trial by Jury", null), new TextObject("{=InFseOAA}This limits the ability of magistrates to condemn those they consider criminals quickly. It prevents arbitrary abuse of power, but landowners or gang leaders can sometimes use threats or bribes to manipulate it.", null), new TextObject("{=L9aNOiJo}granting those accused of major crimes the right to trial by jury", null), new TextObject("{=ZJ2tJnJk}Settlement loyalty is increased by 0.5 per day{newline}Settlement security is decreased by 0.2 per day{newline}Clans lose 1 influence per day", null), -0.3f, 0.1f, 0.6f);
			this._policyCantons.Initialize(new TextObject("{=D6YLpUQa}Cantons", null), new TextObject("{=FMUlfbJf}Rulers organize farmers into groups of households responsible for supplying troops. This makes recruiting easier, but at the cost of their economic productivity.", null), new TextObject("{=PXhIFXbv}organizing households to supply military recruits", null), new TextObject("{=bPpdw81a}Daily militia production is increased by 1{newline}Recruits replenish 20% faster{newline}Tax income in settlements are reduced by 10%", null), -0.2f, -0.1f, 0.4f);
			this._policyLandGrandsForVeteran.Initialize(new TextObject("{=RFRVL8bK}Land Grants for Veterans", null), new TextObject("{=aaUeq1dp}Veterans of the realm's armies are allowed to claim abandoned farmland and receive reduced taxes under the expectation that they will train their children and fellow villagers for future military service.", null), new TextObject("{=waWOKitN}Offering state-owned land to those who served for years in the armies of the realm.", null), new TextObject("{=zKtJHYuI}Settlement militia veterancy increased by +10%{newline}Village tax income -5%", null), -0.35f, -0.15f, 0.5f);
		}

		// Token: 0x040004EB RID: 1259
		private PolicyObject _policyLandTax;

		// Token: 0x040004EC RID: 1260
		private PolicyObject _policyStateMonopolies;

		// Token: 0x040004ED RID: 1261
		private PolicyObject _policySacredMajesty;

		// Token: 0x040004EE RID: 1262
		private PolicyObject _policyMagistrates;

		// Token: 0x040004EF RID: 1263
		private PolicyObject _policyDebasementOfTheCurrency;

		// Token: 0x040004F0 RID: 1264
		private PolicyObject _policyPrecarialLandTenure;

		// Token: 0x040004F1 RID: 1265
		private PolicyObject _policyCrownDuty;

		// Token: 0x040004F2 RID: 1266
		private PolicyObject _policyImperialTowns;

		// Token: 0x040004F3 RID: 1267
		private PolicyObject _policyRoyalCommissions;

		// Token: 0x040004F4 RID: 1268
		private PolicyObject _policyRoyalGuard;

		// Token: 0x040004F5 RID: 1269
		private PolicyObject _policyWarTax;

		// Token: 0x040004F6 RID: 1270
		private PolicyObject _policyRoyalPrivilege;

		// Token: 0x040004F7 RID: 1271
		private PolicyObject _policySenate;

		// Token: 0x040004F8 RID: 1272
		private PolicyObject _policyLordsPrivyCouncil;

		// Token: 0x040004F9 RID: 1273
		private PolicyObject _policyMilitaryCoronae;

		// Token: 0x040004FA RID: 1274
		private PolicyObject _policyFeudalInheritance;

		// Token: 0x040004FB RID: 1275
		private PolicyObject _policySerfdom;

		// Token: 0x040004FC RID: 1276
		private PolicyObject _policyNobleRetinues;

		// Token: 0x040004FD RID: 1277
		private PolicyObject _policyCastleCharters;

		// Token: 0x040004FE RID: 1278
		private PolicyObject _policyBailiffs;

		// Token: 0x040004FF RID: 1279
		private PolicyObject _policyHuntingRights;

		// Token: 0x04000500 RID: 1280
		private PolicyObject _policyRoadTolls;

		// Token: 0x04000501 RID: 1281
		private PolicyObject _policyMarshals;

		// Token: 0x04000502 RID: 1282
		private PolicyObject _policyCouncilOfTheCommons;

		// Token: 0x04000503 RID: 1283
		private PolicyObject _policyCitizenship;

		// Token: 0x04000504 RID: 1284
		private PolicyObject _policyForgivenessOfDebts;

		// Token: 0x04000505 RID: 1285
		private PolicyObject _policyTribunesOfThePeople;

		// Token: 0x04000506 RID: 1286
		private PolicyObject _policyGrazingRights;

		// Token: 0x04000507 RID: 1287
		private PolicyObject _policyLawspeakers;

		// Token: 0x04000508 RID: 1288
		private PolicyObject _policyTrialByJury;

		// Token: 0x04000509 RID: 1289
		private PolicyObject _policyCantons;

		// Token: 0x0400050A RID: 1290
		private PolicyObject _policyLandGrandsForVeteran;
	}
}
