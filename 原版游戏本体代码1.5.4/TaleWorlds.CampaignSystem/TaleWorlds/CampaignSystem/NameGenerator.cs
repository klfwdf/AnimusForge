using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem;

public class NameGenerator
{
	private readonly Dictionary<int, int> _nameCodeAndCount;

	private MBList<TextObject> _imperialNamesMale;

	private MBList<TextObject> _imperialNamesFemale;

	private MBList<TextObject> _preacherNames;

	private MBList<TextObject> _merchantNames;

	private MBList<TextObject> _artisanNames;

	private MBList<TextObject> _gangLeaderNames;

	public static NameGenerator Current => Campaign.Current.NameGenerator;

	public NameGenerator()
	{
		_nameCodeAndCount = new Dictionary<int, int>();
	}

	internal void Initialize()
	{
		InitializePersonNames();
		InitializeNameCodeAndCountDictionary();
	}

	private void InitializeNameCodeAndCountDictionary()
	{
		foreach (Hero allAliveHero in Hero.AllAliveHeroes)
		{
			if (!allAliveHero.FirstName.HasSameValue(allAliveHero.Name))
			{
				AddName(allAliveHero.FirstName);
			}
			AddName(allAliveHero.Name);
		}
		foreach (Hero deadOrDisabledHero in Hero.DeadOrDisabledHeroes)
		{
			if (!deadOrDisabledHero.FirstName.HasSameValue(deadOrDisabledHero.Name))
			{
				AddName(deadOrDisabledHero.FirstName);
			}
			AddName(deadOrDisabledHero.Name);
		}
	}

	public void GenerateHeroNameAndHeroFullName(Hero hero, out TextObject firstName, out TextObject fullName, bool useDeterministicValues = true)
	{
		firstName = GenerateHeroFirstName(hero);
		fullName = GenerateHeroFullName(hero, firstName, useDeterministicValues);
	}

	private TextObject GenerateHeroFullName(Hero hero, TextObject heroFirstName, bool useDeterministicValues = true)
	{
		TextObject textObject = heroFirstName;
		Clan clan = hero.Clan;
		uint deterministicIndex = 0u;
		if (hero.IsNotable)
		{
			deterministicIndex = (uint)hero.HomeSettlement.Notables.ToList().IndexOf(hero);
		}
		if (hero.IsWanderer)
		{
			textObject = hero.Template.Name.CopyTextObject();
		}
		else if (clan != null && (clan.IsMafia || clan.IsNomad || clan.IsUnderMercenaryService || clan.IsSect))
		{
			textObject = new TextObject("{=4z1t75be}{FIRSTNAME} of the {CLAN_NAME}");
			textObject.SetTextVariable("CLAN_NAME", hero.Clan.InformalName);
		}
		else if (hero.IsArtisan)
		{
			int index = SelectNameIndex(hero, Current._artisanNames, deterministicIndex, useDeterministicValues);
			AddName(_artisanNames[index]);
			textObject = _artisanNames[index].CopyTextObject();
		}
		else if (hero.IsGangLeader)
		{
			int index2 = SelectNameIndex(hero, Current._gangLeaderNames, deterministicIndex, useDeterministicValues);
			AddName(_gangLeaderNames[index2]);
			textObject = Current._gangLeaderNames[index2].CopyTextObject();
		}
		else if (hero.IsPreacher)
		{
			int index3 = SelectNameIndex(hero, Current._preacherNames, deterministicIndex, useDeterministicValues);
			AddName(_preacherNames[index3]);
			textObject = Current._preacherNames[index3].CopyTextObject();
		}
		else if (hero.IsMerchant)
		{
			if (hero.HomeSettlement != null && hero.HomeSettlement.IsTown)
			{
				if (hero.OwnedWorkshops.Count > 0)
				{
					textObject = GameTexts.FindText("str_merchant_name");
					TextObject variable = hero.OwnedWorkshops[0].WorkshopType.JobName.CopyTextObject();
					textObject.SetTextVariable("JOB_NAME", variable);
				}
				else
				{
					int index4 = SelectNameIndex(hero, Current._merchantNames, deterministicIndex, useDeterministicValues);
					AddName(_merchantNames[index4]);
					textObject = Current._merchantNames[index4].CopyTextObject();
				}
			}
		}
		else if (hero.IsRuralNotable || hero.IsHeadman)
		{
			textObject = new TextObject("{=YTAdoNHW}{FIRSTNAME} of {VILLAGE_NAME}");
			textObject.SetTextVariable("VILLAGE_NAME", hero.HomeSettlement.Name);
		}
		textObject.SetTextVariable("FEMALE", hero.IsFemale ? 1 : 0);
		textObject.SetTextVariable("IMPERIAL", (hero.Culture.StringId == "empire") ? 1 : 0);
		textObject.SetTextVariable("COASTAL", (hero.Culture.StringId == "empire" || hero.Culture.StringId == "nord" || hero.Culture.StringId == "vlandia") ? 1 : 0);
		textObject.SetTextVariable("NORTHERN", (hero.Culture.StringId == "battania" || hero.Culture.StringId == "nord" || hero.Culture.StringId == "sturgia") ? 1 : 0);
		if (textObject != heroFirstName)
		{
			textObject.SetTextVariable("FIRSTNAME", heroFirstName);
		}
		else
		{
			textObject.SetTextVariable("FIRSTNAME", heroFirstName.ToString());
		}
		return textObject;
	}

	public TextObject GenerateHeroFirstName(Hero hero)
	{
		MBReadOnlyList<TextObject> nameListForCulture = GetNameListForCulture(hero.Culture, hero.IsFemale);
		int index = SelectNameIndex(hero, nameListForCulture, 0u, useDeterministicValues: false);
		AddName(nameListForCulture[index]);
		return nameListForCulture[index].CopyTextObject();
	}

	public TextObject GenerateFirstNameForPlayer(CultureObject culture, bool isFemale)
	{
		MBReadOnlyList<TextObject> nameListForCulture = GetNameListForCulture(culture, isFemale);
		int index = MBRandom.NondeterministicRandomInt % nameListForCulture.Count;
		return nameListForCulture[index].CopyTextObject();
	}

	public TextObject GenerateClanName(CultureObject culture, Settlement clanOriginSettlement)
	{
		TextObject[] clanNameListForCulture = GetClanNameListForCulture(culture);
		Dictionary<TextObject, int> dictionary = new Dictionary<TextObject, int>();
		TextObject[] array = clanNameListForCulture;
		foreach (TextObject clanNameElement in array)
		{
			if (!dictionary.ContainsKey(clanNameElement))
			{
				int num = Clan.All.Count((Clan t) => t.Name.Equals(clanNameElement)) * 3;
				num += Clan.All.Count((Clan t) => t.Name.HasSameValue(clanNameElement));
				dictionary.Add(clanNameElement, num);
			}
			else
			{
				Debug.FailedAssert("Duplicate name in Clan Name list", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\NameGenerator.cs", "GenerateClanName", 196);
			}
		}
		int num2 = dictionary.Values.Max() + 1;
		List<(TextObject, float)> list = new List<(TextObject, float)>();
		foreach (TextObject key in dictionary.Keys)
		{
			list.Add((key, num2 - dictionary[key]));
		}
		MBRandom.ChooseWeighted(list, out var chosenIndex);
		TextObject textObject = dictionary.ElementAt(chosenIndex).Key.CopyTextObject();
		if (culture.StringId.ToLower() == "vlandia")
		{
			textObject.SetTextVariable("ORIGIN_SETTLEMENT", clanOriginSettlement.Name);
		}
		return textObject;
	}

	private void InitializePersonNames()
	{
		_imperialNamesMale = new MBList<TextObject>
		{
			new TextObject("{=aeLgc0cU}Acthon"),
			new TextObject("{=tWDyWroN}Amnon"),
			new TextObject("{=uTFjknE2}Andros"),
			new TextObject("{=QjJAbaoT}Apys"),
			new TextObject("{=zInIqBD0}Arenicos"),
			new TextObject("{=W1uwgvAQ}Arion"),
			new TextObject("{=5VzWRMTn}Artimendros"),
			new TextObject("{=rGuYLmez}Ascyron"),
			new TextObject("{=J1nh9YiN}Atilon"),
			new TextObject("{=NBAGX54b}Avigos"),
			new TextObject("{=9cDw6vPF}Cadomenos"),
			new TextObject("{=lD1sl2XC}Camnon"),
			new TextObject("{=LP7yYoGQ}Caribos"),
			new TextObject("{=VhbasI9J}Castor"),
			new TextObject("{=VG9ng2n2}Chandion"),
			new TextObject("{=aezwJGvY}Chenon"),
			new TextObject("{=acp2IcMs}Crotor"),
			new TextObject("{=XKxeaF0I}Dalidos"),
			new TextObject("{=pCBkH35Q}Danos"),
			new TextObject("{=2OJEyP0d}Dasys"),
			new TextObject("{=SoTUisL3}Deltisos"),
			new TextObject("{=HfuAdGQX}Destor"),
			new TextObject("{=lEHtUGed}Diocosos"),
			new TextObject("{=5EiKEdi5}Dorion"),
			new TextObject("{=yAZC6F7P}Ecsorios"),
			new TextObject("{=rUvnbDi2}Encurion"),
			new TextObject("{=KjzJjj5n}Eronys"),
			new TextObject("{=gYit4RTe}Euchor"),
			new TextObject("{=uW38UtSH}Eupitor"),
			new TextObject("{=WfAuSpJq}Eutos"),
			new TextObject("{=RyorjCXF}Galon"),
			new TextObject("{=ZFlIT1tH}Ganimynos"),
			new TextObject("{=f3hTGKIP}Garitops"),
			new TextObject("{=iDxygnVF}Gerotheon"),
			new TextObject("{=rFbAjSWM}Gorigos"),
			new TextObject("{=4lEEqyCg}Jacorios"),
			new TextObject("{=DwzrsJxS}Jamanys"),
			new TextObject("{=IU63oxWD}Jemynon"),
			new TextObject("{=t1OpnZph}Jeremos"),
			new TextObject("{=katzUyMI}Joron"),
			new TextObject("{=GWaJ7Ksq}Joculos"),
			new TextObject("{=BCy1KpvR}Lacalion"),
			new TextObject("{=6LMiTKZz}Lamenon"),
			new TextObject("{=F13aYbuk}Lavalios"),
			new TextObject("{=bHbR5Mgy}Losys"),
			new TextObject("{=CbxLZdVg}Lycos"),
			new TextObject("{=HCXHcBzT}Mattis"),
			new TextObject("{=X21PahQn}Menaclys"),
			new TextObject("{=tTuaiLS6}Meritor"),
			new TextObject("{=qqW5n5Ox}Milos"),
			new TextObject("{=5yFy4U4i}Morynon"),
			new TextObject("{=ud6rhXbn}Mostiros"),
			new TextObject("{=FF3dQKc1}Nethor"),
			new TextObject("{=u1lTJoPO}Nemos"),
			new TextObject("{=sDFb1PRI}Nortos"),
			new TextObject("{=QGIAbglw}Obron"),
			new TextObject("{=ZmQx4gT3}Olichor"),
			new TextObject("{=pq0w8kry}Orachos"),
			new TextObject("{=JFVseSa3}Oros"),
			new TextObject("{=7UkzXWTQ}Osarios"),
			new TextObject("{=cZIhxH9e}Pacarios"),
			new TextObject("{=9sokmYMZ}Padmos"),
			new TextObject("{=NXOOJs2X}Patrys"),
			new TextObject("{=plx8kkxa}Pelicos"),
			new TextObject("{=1cKxsHSh}Penton"),
			new TextObject("{=vgBRa2BE}Poraclys"),
			new TextObject("{=q186AQHt}Phadon"),
			new TextObject("{=LMWiJi6V}Phirentos"),
			new TextObject("{=XZxIpCQH}Phorys"),
			new TextObject("{=5eTb2xr7}Sanion"),
			new TextObject("{=Zm4a2xsf}Salusios"),
			new TextObject("{=egCbjHDT}Semnon"),
			new TextObject("{=GeJobcre}Sinor"),
			new TextObject("{=E4WhEvu7}Sotherys"),
			new TextObject("{=FClEZIkT}Sovos"),
			new TextObject("{=rVUhYuvE}Suterios"),
			new TextObject("{=Axe5FkET}Talison"),
			new TextObject("{=PR4IqkTW}Temeon"),
			new TextObject("{=9xyXdX4I}Tharos"),
			new TextObject("{=zfUhnG7y}Themestios"),
			new TextObject("{=P7iNhhPl}Turiados"),
			new TextObject("{=kfNW01y5}Tynops"),
			new TextObject("{=wbJzSg3X}Ulbesos"),
			new TextObject("{=SRncaw79}Urios"),
			new TextObject("{=pRbKbPK3}Vadrios"),
			new TextObject("{=TiJOjUi5}Valaos"),
			new TextObject("{=5nEh20ju}Vasylops"),
			new TextObject("{=bWb245zO}Voleos"),
			new TextObject("{=WTePYMNF}Zaraclys"),
			new TextObject("{=p0hcyZdp}Zenon"),
			new TextObject("{=3RBVn5yi}Zoros"),
			new TextObject("{=uCyAnus4}Zostios")
		};
		_imperialNamesFemale = new MBList<TextObject>
		{
			new TextObject("{=BNnLbOkN}Adinea"),
			new TextObject("{=EGatdCLg}Alena"),
			new TextObject("{=UPaP0B2L}Alchyla"),
			new TextObject("{=QyXwJXIV}Andrasa"),
			new TextObject("{=NM5f1Q6I}Ariada"),
			new TextObject("{=bmHAYBwX}Catila"),
			new TextObject("{=AdK4Ilzw}Chalia"),
			new TextObject("{=xVSOKf0p}Chara"),
			new TextObject("{=915frsPd}Corena"),
			new TextObject("{=BNuZ6nvd}Daniria"),
			new TextObject("{=YBGCXSEx}Debana"),
			new TextObject("{=Oy5dH7gZ}Elea"),
			new TextObject("{=rA3KybBX}Ethirea"),
			new TextObject("{=qiTtJyHE}Gala"),
			new TextObject("{=fGmpS0Dr}Gandarina"),
			new TextObject("{=a6qaFH7L}Herena"),
			new TextObject("{=LE9mRhSs}Hespedia"),
			new TextObject("{=RHXdqjQY}Ilina"),
			new TextObject("{=HSlPLC4m}Ira"),
			new TextObject("{=028iCb8B}Jythea"),
			new TextObject("{=6vtmYjTW}Jolanna"),
			new TextObject("{=Ew69yN84}Juthys"),
			new TextObject("{=Jif1C3X3}Laria"),
			new TextObject("{=2oy7atk6}Lundana"),
			new TextObject("{=dwFZFQ6V}Lysica"),
			new TextObject("{=2fYYfHUI}Martira"),
			new TextObject("{=Vxt0xTvV}Mavea"),
			new TextObject("{=lgvtLEDA}Melchea"),
			new TextObject("{=QvfUqzpF}Mina"),
			new TextObject("{=11KckWau}Mitara"),
			new TextObject("{=uLjRHv9p}Nadea"),
			new TextObject("{=FUT6eXfw}Phaea"),
			new TextObject("{=WrAMfIG1}Phenoria"),
			new TextObject("{=XMTD2clw}Rhoe"),
			new TextObject("{=0XEaaoah}Rosazia"),
			new TextObject("{=L9weGfoX}Salea"),
			new TextObject("{=nSoJkeBI}Sittacea"),
			new TextObject("{=V1QLbhRl}Sora"),
			new TextObject("{=b2aRoXsb}Tessa"),
			new TextObject("{=bQKbW8Tx}Thasyna"),
			new TextObject("{=CvVJyKYA}Thelea"),
			new TextObject("{=VzhbUL60}Vendelia"),
			new TextObject("{=a2ajWcI3}Viria"),
			new TextObject("{=wbLqHvjE}Zerosica"),
			new TextObject("{=zxZH2WbD}Zimena"),
			new TextObject("{=AccBcEIt}Zoana")
		};
		_preacherNames = new MBList<TextObject>
		{
			new TextObject("{=UuypR3B4}{FIRSTNAME} of the Gourd"),
			new TextObject("{=T3x9hVg9}{FIRSTNAME} of the Chalice"),
			new TextObject("{=N6DvC4hN}{FIRSTNAME} of the Mirror"),
			new TextObject("{=QUFmBtbk}{FIRSTNAME} of the Sandal"),
			new TextObject("{=1gMfbcaO}{FIRSTNAME} of the Staff"),
			new TextObject("{=6M3mbAHQ}{FIRSTNAME} of the Rose"),
			new TextObject("{=YYYBGdh5}{FIRSTNAME} of the Lamp"),
			new TextObject("{=6qssnVgo}{FIRSTNAME} of the Pomegranate"),
			new TextObject("{=TOGBPjmO}{FIRSTNAME} of the Seal"),
			new TextObject("{=CQWKrd0a}{FIRSTNAME} of the Spinning-Wheel"),
			new TextObject("{=6ALN43LY}{FIRSTNAME} of the Bell"),
			new TextObject("{=TJKi43Hv}{FIRSTNAME} of the Scroll"),
			new TextObject("{=tzuN76ma}{FIRSTNAME} of the Axe"),
			new TextObject("{=fZXEqTIP}{FIRSTNAME} of the Plough"),
			new TextObject("{=TVRbkuhC}{FIRSTNAME} of the Trident"),
			new TextObject("{=SdK678BT}{FIRSTNAME} of the Cavern"),
			new TextObject("{=bu2rmgY1}{FIRSTNAME} of the Willow-Tree"),
			new TextObject("{=uyTmrmCW}{FIRSTNAME} of the Reeds"),
			new TextObject("{=YYyoYwH2}{FIRSTNAME} of the Pasture"),
			new TextObject("{=QskefraA}{FIRSTNAME} of the Ram"),
			new TextObject("{=TrGGbtS4}{FIRSTNAME} of the Dove"),
			new TextObject("{=glTzcivI}{FIRSTNAME} of the Spring"),
			new TextObject("{=fYe25aEt}{FIRSTNAME} of the Well"),
			new TextObject("{=TtaEimaV}{FIRSTNAME} of the Bridge"),
			new TextObject("{=TaouqUu7}{FIRSTNAME} of the Steps"),
			new TextObject("{=zrDWbEJR}{FIRSTNAME} of the Gate"),
			new TextObject("{=xdmhzukY}{FIRSTNAME} of the Hearth"),
			new TextObject("{=UBk50qwW}{FIRSTNAME} of the Mound"),
			new TextObject("{=4t5zOiVF}{FIRSTNAME} of the Pillar"),
			new TextObject("{=3raSG4Mi}{FIRSTNAME} of the Covenant"),
			new TextObject("{=bP3XdKK3}{FIRSTNAME} of the Dawn"),
			new TextObject("{=36ZmyM8V}{FIRSTNAME} of the Harvest"),
			new TextObject("{=G6BC8HXY}{FIRSTNAME} of the Leavening")
		};
		_merchantNames = new MBList<TextObject>
		{
			new TextObject("{=KQ1js10G}{FIRSTNAME} the Appraiser"),
			new TextObject("{=4RWpqxwE}{FIRSTNAME} the Broker"),
			new TextObject("{=nunbdOY1}{FIRSTNAME} the Supplier"),
			new TextObject("{=3WYVggyD}{FIRSTNAME} the {?COASTAL}Mariner{?}Horsetrader{\\?}"),
			new TextObject("{=iCSVZj2e}{FIRSTNAME} the {?NORTHERN}Far-Farer{?}Caravanner{\\?}"),
			new TextObject("{=asePjBVy}{FIRSTNAME} the {?FEMALE}Freedwoman{?}Freedman{\\?}"),
			new TextObject("{=KiUVswtx}{FIRSTNAME} the Mercer"),
			new TextObject("{=wuMJobac}{FIRSTNAME} the Factor"),
			new TextObject("{=Jin8cj45}{FIRSTNAME} the Minter"),
			new TextObject("{=w290a2DV}{FIRSTNAME} the {?IMPERIAL}Sutler{?}Goodstrader{\\?}"),
			new TextObject("{=npuC7IBM}{FIRSTNAME} the Dyer"),
			new TextObject("{=tx7iJMnc}{FIRSTNAME} the Silkvendor"),
			new TextObject("{=BC4BC0ZC}{FIRSTNAME} the Spicetrader"),
			new TextObject("{=vp0FClX1}{FIRSTNAME} the Cargomaster"),
			new TextObject("{=8trsbRav}{FIRSTNAME} the {?FEMALE}Widow{?}Orphan{\\?}"),
			new TextObject("{=pbDr5JFs}{FIRSTNAME} the Steward"),
			new TextObject("{=AhiGlNRG}{FIRSTNAME} the {?NORTHERN}Furtrader{?}Incensetrader{\\?}")
		};
		_artisanNames = new MBList<TextObject>
		{
			new TextObject("{=3TIbxe5d}{FIRSTNAME} the Brewer"),
			new TextObject("{=TX48zCzF}{FIRSTNAME} the Carpenter"),
			new TextObject("{=KDOFexQb}{FIRSTNAME} the Chandler"),
			new TextObject("{=Bsp30p3g}{FIRSTNAME} the Cooper"),
			new TextObject("{=npuC7IBM}{FIRSTNAME} the Dyer"),
			new TextObject("{=CpafrIbY}{FIRSTNAME} the Miller"),
			new TextObject("{=kiJxwqVh}{FIRSTNAME} the Wheeler"),
			new TextObject("{=tTFUSJoe}{FIRSTNAME} the Smith"),
			new TextObject("{=zE3sKAb2}{FIRSTNAME} the Turner"),
			new TextObject("{=gSmXyxue}{FIRSTNAME} the Tanner")
		};
		_gangLeaderNames = new MBList<TextObject>
		{
			new TextObject("{=5utDJYUv}{FIRSTNAME} the Knife"),
			new TextObject("{=TW4iKHCt}{FIRSTNAME} Foulbreath"),
			new TextObject("{=7h3wBoIt}Bloody {FIRSTNAME}"),
			new TextObject("{=kJlOvZEm}Boss {FIRSTNAME}"),
			new TextObject("{=Oq3OFXyC}Lucky {FIRSTNAME}"),
			new TextObject("{=AZbJuZwF}{FIRSTNAME} Knucklebones"),
			new TextObject("{=yG0JIiaS}{FIRSTNAME} the Jackal"),
			new TextObject("{=aa1lM2MV}{FIRSTNAME} the Angel"),
			new TextObject("{=EUJlNTrf}Pretty {FIRSTNAME}"),
			new TextObject("{=EnaT6Ma3}{FIRSTNAME} the Cat"),
			new TextObject("{=Bk62qb7O}Ironskull {FIRSTNAME}"),
			new TextObject("{=rFESkhK0}{FIRSTNAME} the Slicer"),
			new TextObject("{=pL3s39hv}Clever {FIRSTNAME}"),
			new TextObject("{=nNUZOwhb}Redeye {FIRSTNAME}"),
			new TextObject("{=xudfzjgJ}Little {FIRSTNAME}"),
			new TextObject("{=awCsv4UM}Tiny {FIRSTNAME}"),
			new TextObject("{=u9LBrZnr}{FIRSTNAME} the Shark"),
			new TextObject("{=uBT9fuIi}Snake-eyes {FIRSTNAME}"),
			new TextObject("{=UAXaL9ro}Leadfoot {FIRSTNAME}"),
			new TextObject("{=DCF2JOiJ}Stonehead {FIRSTNAME}"),
			new TextObject("{=A5Gw3GNn}{FIRSTNAME} the Malady"),
			new TextObject("{=aqp9ZtXb}{FIRSTNAME} the Wart"),
			new TextObject("{=FrLta5zf}{FIRSTNAME} the Fist"),
			new TextObject("{=L6N2YLa6}{FIRSTNAME} the Finger"),
			new TextObject("{=VtjMGTWH}{FIRSTNAME} the Scorpion"),
			new TextObject("{=3JOd0l1N}{FIRSTNAME} the Spider"),
			new TextObject("{=ynwbmuoG}{FIRSTNAME} the Viper"),
			new TextObject("{=K4MRSU6i}Sleepy {FIRSTNAME}"),
			new TextObject("{=6jrl3Rbb}{FIRSTNAME} Fishsauce"),
			new TextObject("{=6gjSupBN}{FIRSTNAME} Mutton-pie"),
			new TextObject("{=y4vyNZxg}{FIRSTNAME} Sourwine"),
			new TextObject("{=qhe6SGa3}{FIRSTNAME} Stewbones"),
			new TextObject("{=c7cdMWA3}Buttermilk {FIRSTNAME}"),
			new TextObject("{=bqXpBNvF}Cinnamon {FIRSTNAME}"),
			new TextObject("{=lwLhrGWV}{FIRSTNAME} Flatcakes"),
			new TextObject("{=r9Tp4UGy}Honeytongue {FIRSTNAME}"),
			new TextObject("{=MRJ06SU7}{FIRSTNAME} the Thorn"),
			new TextObject("{=6tBhhNaC}{FIRSTNAME} Rottentooth"),
			new TextObject("{=Z48lYHBl}{FIRSTNAME} the Lamb"),
			new TextObject("{=z8LbFyNA}Dogface {FIRSTNAME}"),
			new TextObject("{=qezuzVuY}{FIRSTNAME} the Goat"),
			new TextObject("{=JiAmC0NZ}{FIRSTNAME} the Mule"),
			new TextObject("{=qmwv27To}{FIRSTNAME} the Mouse"),
			new TextObject("{=ajePb62s}Quicksilver {FIRSTNAME}"),
			new TextObject("{=3NROvpcO}Slowhand {FIRSTNAME}"),
			new TextObject("{=zo13Dkoh}Crushfinger {FIRSTNAME}"),
			new TextObject("{=9Sa3bzlE}{FIRSTNAME} the Anvil"),
			new TextObject("{=FSa61zD4}{FIRSTNAME} the Hammer"),
			new TextObject("{=WzBo28iT}{FIRSTNAME} the Scythe"),
			new TextObject("{=MaK0r9as}{FIRSTNAME} the Cudgel"),
			new TextObject("{=gbAztaSq}{FIRSTNAME} the Gutting-Knife"),
			new TextObject("{=tI8aoxXC}{FIRSTNAME} the Needle"),
			new TextObject("{=4ATx01zS}{FIRSTNAME} the Rock"),
			new TextObject("{=1Tft1d4A}{FIRSTNAME} the Boulder"),
			new TextObject("{=3qjJzjZb}{FIRSTNAME} the Beetle"),
			new TextObject("{=0B6HlgnN}{FIRSTNAME} the Lizard"),
			new TextObject("{=2wixoeOF}Hairy {FIRSTNAME}"),
			new TextObject("{=NTPVzs9z}Poxy {FIRSTNAME}"),
			new TextObject("{=chiIHo4b}Mangy {FIRSTNAME}"),
			new TextObject("{=aIaRIsw4}Scabby {FIRSTNAME}"),
			new TextObject("{=ubZmYdMn}Rancid {FIRSTNAME}"),
			new TextObject("{=xTtHdTsS}Poison {FIRSTNAME}"),
			new TextObject("{=uO99raT7}Snotnose {FIRSTNAME}"),
			new TextObject("{=t968gMty}{FIRSTNAME} the {?FEMALE}Lady{?}Bastard{\\?}"),
			new TextObject("{=lkLNrscj}{FIRSTNAME} the {?FEMALE}Maid{?}Steward{\\?}"),
			new TextObject("{=ujDbk6Qa}{FIRSTNAME} the {?FEMALE}Widow{?}Widow-maker{\\?}"),
			new TextObject("{=fh1auwJW}{FIRSTNAME} the {?FEMALE}She-Wolf{?}Stallion{\\?}")
		};
	}

	public MBReadOnlyList<TextObject> GetNameListForCulture(CultureObject npcCulture, bool isFemale)
	{
		MBReadOnlyList<TextObject> result = (isFemale ? _imperialNamesFemale : _imperialNamesMale);
		if (isFemale)
		{
			if (!npcCulture.FemaleNameList.IsEmpty())
			{
				result = npcCulture.FemaleNameList;
			}
		}
		else if (!npcCulture.MaleNameList.IsEmpty())
		{
			result = npcCulture.MaleNameList;
		}
		return result;
	}

	private TextObject[] GetClanNameListForCulture(CultureObject clanCulture)
	{
		TextObject[] result = null;
		if (!clanCulture.ClanNameList.IsEmpty())
		{
			result = clanCulture.ClanNameList.ToArray();
		}
		else
		{
			Debug.FailedAssert("Missing culture in clan name generation", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\NameGenerator.cs", "GetClanNameListForCulture", 810);
		}
		return result;
	}

	public void AddName(TextObject name)
	{
		int key = CreateNameCode(name);
		if (_nameCodeAndCount != null)
		{
			if (_nameCodeAndCount.TryGetValue(key, out var value))
			{
				_nameCodeAndCount[key] = value + 1;
			}
			else
			{
				_nameCodeAndCount.Add(key, 1);
			}
		}
	}

	private int CreateNameCode(TextObject name)
	{
		return name.GetValueHashCode();
	}

	private int CalculateNameScore(Hero hero, TextObject name)
	{
		int num = 5000;
		IEnumerable<Hero> enumerable;
		if (hero != null)
		{
			if (!hero.IsNotable)
			{
				enumerable = ((hero.Template != null && hero.Occupation == Occupation.Wanderer) ? Hero.AllAliveHeroes.WhereQ((Hero h) => hero.Template.Equals(h.Template)) : ((hero.Clan == null || hero.Occupation != Occupation.Lord) ? new List<Hero>() : hero.Clan.AliveLords));
			}
			else if (hero.IsMerchant && hero.OwnedWorkshops.Count > 0)
			{
				List<Hero> list = new List<Hero>();
				foreach (Town allTown in Town.AllTowns)
				{
					Workshop[] workshops = allTown.Workshops;
					foreach (Workshop workshop in workshops)
					{
						if (workshop.Owner != hero && workshop.WorkshopType == hero.OwnedWorkshops[0].WorkshopType)
						{
							list.Add(workshop.Owner);
						}
					}
				}
				enumerable = list;
			}
			else
			{
				enumerable = hero.BornSettlement.Notables;
			}
		}
		else
		{
			enumerable = new List<Hero>();
		}
		foreach (Hero item in enumerable)
		{
			if (item != null)
			{
				if (name.HasSameValue(item.Name))
				{
					num -= 500;
				}
				if (name.HasSameValue(item.FirstName))
				{
					num -= 1000;
				}
			}
		}
		if (_nameCodeAndCount.TryGetValue(CreateNameCode(name), out var value))
		{
			num -= value;
		}
		return num;
	}

	private int SelectNameIndex(Hero hero, MBReadOnlyList<TextObject> nameList, uint deterministicIndex, bool useDeterministicValues)
	{
		int num = (useDeterministicValues ? hero.HomeSettlement.RandomIntWithSeed(deterministicIndex) : MBRandom.RandomInt()) % nameList.Count;
		int result = 0;
		int num2 = int.MinValue;
		for (int i = 0; i < nameList.Count; i++)
		{
			int num3 = (i + num) % nameList.Count;
			TextObject name = nameList[num3];
			int num4 = CalculateNameScore(hero, name);
			if (num2 < num4)
			{
				num2 = num4;
				result = num3;
			}
		}
		return result;
	}
}
