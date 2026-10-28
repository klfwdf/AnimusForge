using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem;

public class DefaultPolicies
{
	private PolicyObject _policyLandTax;

	private PolicyObject _policyStateMonopolies;

	private PolicyObject _policySacredMajesty;

	private PolicyObject _policyMagistrates;

	private PolicyObject _policyDebasementOfTheCurrency;

	private PolicyObject _policyPrecarialLandTenure;

	private PolicyObject _policyCrownDuty;

	private PolicyObject _policyImperialTowns;

	private PolicyObject _policyRoyalCommissions;

	private PolicyObject _policyRoyalGuard;

	private PolicyObject _policyWarTax;

	private PolicyObject _policyRoyalPrivilege;

	private PolicyObject _policySenate;

	private PolicyObject _policyLordsPrivyCouncil;

	private PolicyObject _policyMilitaryCoronae;

	private PolicyObject _policyFeudalInheritance;

	private PolicyObject _policySerfdom;

	private PolicyObject _policyNobleRetinues;

	private PolicyObject _policyCastleCharters;

	private PolicyObject _policyBailiffs;

	private PolicyObject _policyHuntingRights;

	private PolicyObject _policyRoadTolls;

	private PolicyObject _policyMarshals;

	private PolicyObject _policyCouncilOfTheCommons;

	private PolicyObject _policyCitizenship;

	private PolicyObject _policyForgivenessOfDebts;

	private PolicyObject _policyTribunesOfThePeople;

	private PolicyObject _policyGrazingRights;

	private PolicyObject _policyLawspeakers;

	private PolicyObject _policyTrialByJury;

	private PolicyObject _policyCantons;

	private PolicyObject _policyLandGrandsForVeteran;

	private static DefaultPolicies Instance => Campaign.Current.DefaultPolicies;

	public static PolicyObject LandTax => Instance._policyLandTax;

	public static PolicyObject StateMonopolies => Instance._policyStateMonopolies;

	public static PolicyObject SacredMajesty => Instance._policySacredMajesty;

	public static PolicyObject Magistrates => Instance._policyMagistrates;

	public static PolicyObject DebasementOfTheCurrency => Instance._policyDebasementOfTheCurrency;

	public static PolicyObject PrecarialLandTenure => Instance._policyPrecarialLandTenure;

	public static PolicyObject CrownDuty => Instance._policyCrownDuty;

	public static PolicyObject ImperialTowns => Instance._policyImperialTowns;

	public static PolicyObject RoyalCommissions => Instance._policyRoyalCommissions;

	public static PolicyObject RoyalGuard => Instance._policyRoyalGuard;

	public static PolicyObject WarTax => Instance._policyWarTax;

	public static PolicyObject RoyalPrivilege => Instance._policyRoyalPrivilege;

	public static PolicyObject Senate => Instance._policySenate;

	public static PolicyObject LordsPrivyCouncil => Instance._policyLordsPrivyCouncil;

	public static PolicyObject MilitaryCoronae => Instance._policyMilitaryCoronae;

	public static PolicyObject FeudalInheritance => Instance._policyFeudalInheritance;

	public static PolicyObject Serfdom => Instance._policySerfdom;

	public static PolicyObject NobleRetinues => Instance._policyNobleRetinues;

	public static PolicyObject CastleCharters => Instance._policyCastleCharters;

	public static PolicyObject Bailiffs => Instance._policyBailiffs;

	public static PolicyObject HuntingRights => Instance._policyHuntingRights;

	public static PolicyObject RoadTolls => Instance._policyRoadTolls;

	public static PolicyObject Marshals => Instance._policyMarshals;

	public static PolicyObject CouncilOfTheCommons => Instance._policyCouncilOfTheCommons;

	public static PolicyObject ForgivenessOfDebts => Instance._policyForgivenessOfDebts;

	public static PolicyObject Citizenship => Instance._policyCitizenship;

	public static PolicyObject TribunesOfThePeople => Instance._policyTribunesOfThePeople;

	public static PolicyObject GrazingRights => Instance._policyGrazingRights;

	public static PolicyObject Lawspeakers => Instance._policyLawspeakers;

	public static PolicyObject TrialByJury => Instance._policyTrialByJury;

	public static PolicyObject Cantons => Instance._policyCantons;

	public static PolicyObject LandGrantsForVeteran => Instance._policyLandGrandsForVeteran;

	public DefaultPolicies()
	{
		RegisterAll();
	}

	private void RegisterAll()
	{
		_policyLandTax = Create("policy_land_tax");
		_policyStateMonopolies = Create("policy_state_monopolies");
		_policySacredMajesty = Create("policy_sacred_majesty");
		_policyMagistrates = Create("policy_magistrates");
		_policyDebasementOfTheCurrency = Create("policy_debasement_of_the_currency");
		_policyPrecarialLandTenure = Create("policy_precarial_land_tenure");
		_policyCrownDuty = Create("policy_crown_duty");
		_policyImperialTowns = Create("policy_imperial_towns");
		_policyRoyalCommissions = Create("policy_royal_commissions");
		_policyRoyalGuard = Create("policy_royal_guard");
		_policyWarTax = Create("policy_war_tax");
		_policyRoyalPrivilege = Create("policy_royal_privilege");
		_policySenate = Create("policy_senate");
		_policyLordsPrivyCouncil = Create("policy_lords_privy_council");
		_policyMilitaryCoronae = Create("policy_military_coronae");
		_policyFeudalInheritance = Create("policy_feudal_inheritance");
		_policySerfdom = Create("policy_serfdom");
		_policyNobleRetinues = Create("policy_noble_retinues");
		_policyCastleCharters = Create("policy_castle_charters");
		_policyBailiffs = Create("policy_bailiffs");
		_policyHuntingRights = Create("policy_hunting_rights");
		_policyRoadTolls = Create("policy_road_tolls");
		_policyMarshals = Create("policy_marshals");
		_policyCouncilOfTheCommons = Create("policy_council_of_the_commons");
		_policyCitizenship = Create("policy_citizenship");
		_policyForgivenessOfDebts = Create("policy_forgiveness_of_debts");
		_policyTribunesOfThePeople = Create("policy_tribunes_of_the_people");
		_policyGrazingRights = Create("policy_grazing_rights");
		_policyLawspeakers = Create("policy_lawspeakers");
		_policyTrialByJury = Create("policy_trial_by_jury");
		_policyCantons = Create("policy_cantons");
		_policyLandGrandsForVeteran = Create("policy_land_grands_for_veteran");
		InitializeAll();
	}

	private PolicyObject Create(string stringId)
	{
		return Game.Current.ObjectManager.RegisterPresumedObject(new PolicyObject(stringId));
	}

	private void InitializeAll()
	{
		_policyLandTax.Initialize(new TextObject("{=Tw2FaO0m}Land Tax"), new TextObject("{=MWvzJSH1}A shift in the tax system that put more emphasis on property and less on the head tax charged to everyone could collect more from wealthy landowners."), new TextObject("{=0I6xdead}taxing landowners on their property"), new TextObject("{=OWwPj500}5% of the village income is paid to the ruler clan as tax{newline}5% less village income for clans"), 0.7f, 0.15f, -0.7f);
		_policyStateMonopolies.Initialize(new TextObject("{=SXCudsWT}State Monopolies"), new TextObject("{=2Qx4bL66}The ruler has a monopoly on certain goods, although practically he can license out production to merchants and collect a portion of the proceeds."), new TextObject("{=DIQF5bAX}giving the state monopolies on key goods"), new TextObject("{=kIVfA5cv}Ruler clan gains 5% of settlement as tax per town{newline}Workshop production is decreased by 10%"), 0.75f, 0.1f, -0.6f);
		_policySacredMajesty.Initialize(new TextObject("{=rqHFujhr}Sacred Majesty"), new TextObject("{=HQGUoRTW}The ruler is considered semi-divine and certain rituals are to be performed in his or her presence, increasing his or her air of authority."), new TextObject("{=00bd3Huo}performing new rituals that treat the ruler as semi-divine"), new TextObject("{=RbjN7VQ2}Ruler clan earns 3 influence per day{newline}Non-ruler clans lose 0.5 influence per day"), 0.85f, 0.1f, -0.9f);
		_policyMagistrates.Initialize(new TextObject("{=ZhlpuMxb}Magistrates"), new TextObject("{=QlZc9CNQ}Rulers could appoint magistrates to rule in disputes and solve crimes. This could cut down on gang activity and lawlessness, but was often greatly resented by communities."), new TextObject("{=E3b8iKjc}allowing the ruler to appoint magistrates"), new TextObject("{=QLNfsk93}Town security is increased by 1 per day{newline}Town taxes are reduced by 5%"), 0.6f, 0.35f, 0.1f);
		_policyDebasementOfTheCurrency.Initialize(new TextObject("{=9lygvooT}Debasement Of The Currency"), new TextObject("{=5WR5DsOz}Rulers could make money fast by debasing the currency and minting more, but this would cause prices to rise."), new TextObject("{=EmXai6dm}debasing the currency"), new TextObject("{=6kn630ka}Ruler clan gains 100 denars per day for each town in the kingdom{newline}Settlement loyalty is decreased by 1 per day"), 0.7f, 0.1f, -0.7f);
		_policyPrecarialLandTenure.Initialize(new TextObject("{=ft08QZrA}Precarial Land Tenure"), new TextObject("{=MF40cm9k}Land grants are considered to be temporary offices rather than the rightful inheritance of lords. In practice heirs tend to take over their family fiefs, but it's easier under Depositions to remove them."), new TextObject("{=Vl8BHCvF}allowing the ruler to easily revoke fiefs"), new TextObject("{=6DQjKwt2}The influence cost of proposing settlement annexation is reduced by 50% for the ruler clan"), 0.75f, 0f, -0.6f);
		_policyCrownDuty.Initialize(new TextObject("{=dfyNVkFV}Crown Duty"), new TextObject("{=elvb5y2I}The ruler is allowed to impose special taxes on trade in towns, payable directly to the royal or imperial treasury."), new TextObject("{=w3GIUD3u}allowing the ruler to collect special tariffs"), new TextObject("{=bDANTRwA}5% tax on tariffs is paid to the ruler clan{newline}Higher trade penalty in towns{newline}Town prosperity is decreased by 1 per day"), 0.75f, 0.15f, -0.4f);
		_policyImperialTowns.Initialize(new TextObject("{=l5kDEI6N}Imperial Towns"), new TextObject("{=ieIWHAtc}A ruler can grant towns special privileges based on their 'immediacy', special access to his person without going through lords or other vassals."), new TextObject("{=QoubP07U}allowing the ruler to grant special privileges to towns"), new TextObject("{=8qwvZ15E}Towns held by the ruler clan gain 1 Loyalty and 1 Prosperity per day{newline}Towns held by non-ruler clans lose 0.3 Loyalty per day"), 0.7f, 0.15f, -0.3f);
		_policyRoyalCommissions.Initialize(new TextObject("{=XYDhkb6h}Royal Commissions"), new TextObject("{=AzKq8AfI}In theory, the king or the emperor has the sole right to command men in the field. Anyone commanding an army does so in the king's name."), new TextObject("{=kPqs7EIf}giving the ruler more power to summon armies"), new TextObject("{=L9n6yu1X}The influence cost of creating an army is reduced by 30% for the ruler{newline}Armies led by the ruler earn cohesion at 30% less cost{newline}Armies led by non-ruler nobles cost 10% more influence to create"), 0.65f, 0f, -0.45f);
		_policyRoyalGuard.Initialize(new TextObject("{=F41aPt80}Royal Guard"), new TextObject("{=eibt105C}The ruler maintains a prestigious personal guard. It attracts warriors who might otherwise serve their local lord."), new TextObject("{=bhoCBIWB}authorizing the ruler to have a large private bodyguard"), new TextObject("{=GwAZMQ8b}Ruler's party size is increased by 60{newline}Non-ruling clans lose 0.2 influence per day."), 0.75f, 0f, -0.5f);
		_policyWarTax.Initialize(new TextObject("{=AlZB8WIb}War Tax"), new TextObject("{=b4TeiuoJ}Exceptional taxes were often applied in wartime."), new TextObject("{=76TgEba5}letting the ruler collect extra taxes in wartime"), new TextObject("{=O4iki0FD}Ruler gains 5% tax from all settlements{newline}Towns lose 1 prosperity per day{newline}The influence cost of declaring war is doubled for the ruler clan"), 0.7f, -0.1f, -0.65f);
		_policyRoyalPrivilege.Initialize(new TextObject("{=Rl1AHKSp}Royal Privilege"), new TextObject("{=ifnnu3g4}There is a long list of reasons why a ruler can reject a law passed by the council. A ruler does not need to search long to find an excuse for a veto."), new TextObject("{=aKLak7nn}giving the ruler broader powers to veto laws"), new TextObject("{=DG3JbOa2}For kingdom decisions, the influence cost of the ruler overriding the popular decision outcome is reduced by 20%"), 0.8f, -0.15f, -0.75f);
		_policySenate.Initialize(new TextObject("{=8pjMAqOg}Senate"), new TextObject("{=D3W9Qi0Z}All lords have a formal role on the council."), new TextObject("{=lXSeaba5}having the lords of the realm meet as a permanent council"), new TextObject("{=TsvBHBdX}Tier 3+ clans gain 0.5 influence per day, influence cost of inviting lower tier clans to army are increased by 10%"), -0.7f, 0.85f, 0.7f);
		_policyLordsPrivyCouncil.Initialize(new TextObject("{=JaZ7T2Wj}Lords' Privy Council"), new TextObject("{=on2EmlUT}A small council of the greatest lords of the realm. This gives the main clans extra influence, but prevents other clans from climbing into their ranks."), new TextObject("{=bxWITUaN}having the greatest lords of the realm meet as a small privy council"), new TextObject("{=LpdAa1NY}Tier 5+ clans gain 0.5 influence per day, influence cost of inviting lower tier clans to army are increased by 20%"), -0.5f, 0.7f, -0.15f);
		_policyMilitaryCoronae.Initialize(new TextObject("{=IBlJ42MN}Military Coronae"), new TextObject("{=ceq6ZMIx}Lords can vote to award each other decorations and distinctions for battlefield achievements, allowing them greater opportunities to share military glory with the ruler."), new TextObject("{=EG06bDxi}granting awards for deeds in the field"), new TextObject("{=uCXGZ2YP}Military achievements grant 20% more influence{newline}Troop wages are increased by 10%"), -0.15f, 0.6f, 0.35f);
		_policyFeudalInheritance.Initialize(new TextObject("{=xbvei0Cb}Feudal Inheritance"), new TextObject("{=AlFTInfU}States with strict and formal laws of inheritance make it more difficult to revoke land."), new TextObject("{=fj5mYvNE}recognizing the formal inheritance of fiefs"), new TextObject("{=aWWzrwAw}The cost of revoking a fief from a clan is doubled{newline}Clans gain 0.1 influence for each fief they own"), -0.75f, 0.75f, 0.65f);
		_policySerfdom.Initialize(new TextObject("{=8FPCRv5L}Serfdom"), new TextObject("{=0Qh7Pa9E}Tenants are forbidden from leaving the lands of their lords without notice."), new TextObject("{=5Wld45hP}forbidding tenants from leaving their lords' lands"), new TextObject("{=H9Px95rR}Villages grant 0.2 influence per day to the owner clan{newline}Towns gain 1 security but lose 1 militia per day"), -0.4f, 0.5f, -0.25f);
		_policyNobleRetinues.Initialize(new TextObject("{=7Pk3bFPC}Noble Retinues"), new TextObject("{=yXb8bphB}Nobles are expected to raise sizable retinues."), new TextObject("{=W3xUQxAa}encouraging lords to have large private armies"), new TextObject("{=LIjN3rYD}Tier 5+ clans lose 1 influence per day and the party size of their leaders is increased by 40"), -0.35f, 0.65f, -0.45f);
		_policyCastleCharters.Initialize(new TextObject("{=W6XMWJ8R}Castle Charters"), new TextObject("{=t8TfagYL}Nobles are encouraged to fortify their estates, and can requisition labor and materials to do so."), new TextObject("{=hsIvt3WC}encouraging lords to fortify their estates"), new TextObject("{=RdbLbpgO}Castle upgrade costs are reduced by 20%"), -0.65f, 0.45f, 0f);
		_policyBailiffs.Initialize(new TextObject("{=HKT5MnjP}Bailiffs"), new TextObject("{=nmnp9S7k}Nobles have the right to appoint bailiffs."), new TextObject("{=GFnqIuxy}encouraging lords to appoint bailiffs in their fiefs"), new TextObject("{=XFcmlN1J}Town security is increased by 1 per day{newline}Towns with a security greater than 60 yield 1 additional influence to the owner clan.{newline}Tax from towns are reduced by 5%"), 0f, 0.4f, -0.1f);
		_policyHuntingRights.Initialize(new TextObject("{=0mKYUNb8}Hunting Rights"), new TextObject("{=ZMTkq5TG}Nobles and other landowners have exclusive rights to hunt in forests."), new TextObject("{=gOn7a7if}granting lords exclusive hunting rights to nearby forests"), new TextObject("{=agaSYd5t}Food production in towns and castles are increased by 2{newline}Town loyalty is decreased by 0.2"), -0.2f, 0.35f, -0.15f);
		_policyRoadTolls.Initialize(new TextObject("{=bOtYmSP8}Road Tolls"), new TextObject("{=nP7KOISK}Local landowners have the right to collect tolls on commerce."), new TextObject("{=upK62aLR}allowing lords tolls on roads running through their lands"), new TextObject("{=dLkfbU0a}Trade tax paid to the town owner is increased by 3%{newline}Town prosperity is decreased by 0.2"), -0.5f, 0.45f, -0.35f);
		_policyMarshals.Initialize(new TextObject("{=WSU35a7F}Marshals"), new TextObject("{=7EP0NgzU}The highest ranking of nobles have the de facto right to assemble large armies."), new TextObject("{=cBAVR7e8}granting high-ranking nobles the right to summon large armies"), new TextObject("{=0wxRZ9AV}Armies led by Tier 5+ nobles require 10% less influence{newline}Influence of the ruler clan is reduced by 1 per day"), -0.45f, 0.5f, 0f);
		_policyCouncilOfTheCommons.Initialize(new TextObject("{=bMSI9Bt3}Council of the Commons"), new TextObject("{=55CsWKbg}Some kingdoms, especially those that evolved from a city-state or a tribe, had popular assemblies that most of its members had the right to attend. Its powers were often limited, since it could only meet periodically, but it still gave the public the right to participate in government."), new TextObject("{=srByX06Y}letting all citizens meet and vote on some issues"), new TextObject("{=UZ0mPm8b}Each notable yields 0.1 influence per day to the settlement's owner clan{newline}Tax from fortifications 5% decreased"), -0.5f, 0.1f, 0.7f);
		_policyForgivenessOfDebts.Initialize(new TextObject("{=Vzsu5nZV}Forgiveness of Debts"), new TextObject("{=Lgmisw4L}This reform limits the degree to which lords and merchants can lend to their tenants and employees and then demand repayment, or seize their assets or their freedom. It effectively bans serfdom."), new TextObject("{=9YKV4SNH}restricting what creditor may do to collect debts"), new TextObject("{=xJ2uDcob}Settlement loyalty is increased by 2 per day{newline}Settlement production is reduced by 5%"), -0.4f, -0.4f, 0.6f);
		_policyCitizenship.Initialize(new TextObject("{=sYNFwOVg}Citizenship"), new TextObject("{=O5sBO9sQ}Many empires granted their populations citizenship, which usually came with a series of rights. Of course, citizenship could not be granted immediately to conquered provinces until the population showed it was willing to adopt the ways of the empire, including its language, clothes, and religious cults."), new TextObject("{=dvEkfaab}recognizing the common folk in the realm as full citizens"), new TextObject("{=qEOXka0Q}+0.5 Loyalty per day to settlements that have the same culture as their owner clan{newline}Settlement militia production is increased by 1{newline}-0.5 Loyalty per day to settlements with a different culture than its owner clan"), -0.65f, -0.35f, 0.7f);
		_policyTribunesOfThePeople.Initialize(new TextObject("{=IJdGTOAe}Tribunes of the People"), new TextObject("{=VzmzX9Ln}This revives the tribunate, an institution in ancient oligarchic republics that gave representation to families without patrician standing. These elected officers could veto legislation from the more aristocratic Senate."), new TextObject("{=auftprN9}allowing the common people to elect tribes to represent them"), new TextObject("{=YOBeOFxY}Town taxes paid to the ruler are reduced by 5%{newline}Town loyalty is increased by 1 per day"), -0.6f, -0.2f, 0.55f);
		_policyGrazingRights.Initialize(new TextObject("{=mb25Ue3f}Grazing Rights"), new TextObject("{=fjj0pJXV}Landowners could often assert legal rights to common areas and charge villages money to use them. If ordinary people petitioned a ruler, however, he might give them the right to use all common areas for hunting or grazing as members of the village."), new TextObject("{=1Y5p40uP}granting villagers the right to graze on land held in common"), new TextObject("{=PG8anNca}Settlement loyalty is increased by 0.5 per day{newline}Daily hearth production at villages decreases by 0.25 per day"), -0.75f, -0.3f, 0.7f);
		_policyLawspeakers.Initialize(new TextObject("{=EBCV0LcU}Lawspeakers"), new TextObject("{=U7s1LycQ}This refers to the practice of appointing independent elders to remind the council of the law and past precedents. It allows commoners without education to make complex legal arguments."), new TextObject("{=7kNxogN8}appointing independent elders to uphold the law"), new TextObject("{=bFEdxs6Y}All clans whose leader has high Charm gain 1 influence per day{newline}All clans whose leader has low Charm lose 1 influence per day"), 0f, 0.25f, 0.45f);
		_policyTrialByJury.Initialize(new TextObject("{=yNAzCfxc}Trial by Jury"), new TextObject("{=InFseOAA}This limits the ability of magistrates to condemn those they consider criminals quickly. It prevents arbitrary abuse of power, but landowners or gang leaders can sometimes use threats or bribes to manipulate it."), new TextObject("{=L9aNOiJo}granting those accused of major crimes the right to trial by jury"), new TextObject("{=ZJ2tJnJk}Settlement loyalty is increased by 0.5 per day{newline}Settlement security is decreased by 0.2 per day{newline}Clans lose 1 influence per day"), -0.3f, 0.1f, 0.6f);
		_policyCantons.Initialize(new TextObject("{=D6YLpUQa}Cantons"), new TextObject("{=FMUlfbJf}Rulers organize farmers into groups of households responsible for supplying troops. This makes recruiting easier, but at the cost of their economic productivity."), new TextObject("{=PXhIFXbv}organizing households to supply military recruits"), new TextObject("{=bPpdw81a}Daily militia production is increased by 1{newline}Recruits replenish 20% faster{newline}Tax income in settlements are reduced by 10%"), -0.2f, -0.1f, 0.4f);
		_policyLandGrandsForVeteran.Initialize(new TextObject("{=RFRVL8bK}Land Grants for Veterans"), new TextObject("{=aaUeq1dp}Veterans of the realm's armies are allowed to claim abandoned farmland and receive reduced taxes under the expectation that they will train their children and fellow villagers for future military service."), new TextObject("{=waWOKitN}Offering state-owned land to those who served for years in the armies of the realm."), new TextObject("{=zKtJHYuI}Settlement militia veterancy increased by +10%{newline}Village tax income -5%"), -0.35f, -0.15f, 0.5f);
	}
}
