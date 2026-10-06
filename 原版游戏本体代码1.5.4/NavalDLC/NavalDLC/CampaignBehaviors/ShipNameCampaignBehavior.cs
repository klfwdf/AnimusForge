using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.CampaignBehaviors;

public class ShipNameCampaignBehavior : CampaignBehaviorBase
{
	[Flags]
	private enum NameTrait
	{
		None = 0,
		Aserai = 2,
		Battania = 4,
		Empire = 8,
		Khuzait = 0x10,
		Nord = 0x20,
		Sturgia = 0x40,
		Vlandia = 0x80,
		Light = 0x100,
		Medium = 0x200,
		Heavy = 0x400,
		Trade = 0x800,
		LightAndMedium = 0x300
	}

	private MBReadOnlyList<(TextObject, NameTrait, float)> _fullNames = new MBReadOnlyList<(TextObject, NameTrait, float)>(new List<(TextObject, NameTrait, float)>
	{
		(new TextObject("{=p4zJbD3a}Righteous {NAME}"), NameTrait.Empire | NameTrait.Heavy, 4f),
		(new TextObject("{=EQHW6TPk}Glorious {NAME}"), NameTrait.Empire | NameTrait.Heavy, 4f),
		(new TextObject("{=FUMvrsE2}Angelic {NAME}"), NameTrait.Empire | NameTrait.Heavy, 4f),
		(new TextObject("{=obOVM8pM}Holy {NAME}"), NameTrait.Empire | NameTrait.Heavy, 4f),
		(new TextObject("{=N6CT6M1E}Sacred {NAME}"), NameTrait.Empire | NameTrait.Heavy, 4f),
		(new TextObject("{=M1Q36S4d}Divine {NAME}"), NameTrait.Empire | NameTrait.Heavy, 4f),
		(new TextObject("{=GYiAqvCR}Enduring {NAME}"), NameTrait.Empire | NameTrait.Heavy, 4f),
		(new TextObject("{=oIG8QbiK}Invincible {NAME}"), NameTrait.Empire | NameTrait.Heavy, 4f),
		(new TextObject("{=3VaHDxBO}{NAME} of the Senate"), NameTrait.Empire | NameTrait.Heavy, 4f),
		(new TextObject("{=RrI6uJAN}Royal {NAME}"), NameTrait.Vlandia | NameTrait.Heavy, 4f),
		(new TextObject("{=NT9EcONe}King's {NAME}"), NameTrait.Vlandia | NameTrait.Heavy, 4f),
		(new TextObject("{=IQ1Q0ncJ}Sable {NAME}"), NameTrait.LightAndMedium | NameTrait.Vlandia | NameTrait.Heavy, 4f),
		(new TextObject("{=LTa1b6T1}Crimson {NAME}"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Vlandia | NameTrait.Heavy, 4f),
		(new TextObject("{=l1Rs5EKR}Scarlet {NAME}"), NameTrait.LightAndMedium | NameTrait.Vlandia | NameTrait.Heavy, 4f),
		(new TextObject("{=aaYhWD7n}Azure {NAME}"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Vlandia | NameTrait.Heavy, 4f),
		(new TextObject("{=IgPHnuWN}Red {NAME}"), NameTrait.LightAndMedium | NameTrait.Empire, 4f),
		(new TextObject("{=IgPHnuWN}Red {NAME}"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Vlandia | NameTrait.Heavy, 4f),
		(new TextObject("{=IgPHnuWN}Red {NAME}"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Khuzait | NameTrait.Nord | NameTrait.Sturgia | NameTrait.Heavy | NameTrait.Trade, 4f),
		(new TextObject("{=DqMfR4H9}Green {NAME}"), NameTrait.LightAndMedium | NameTrait.Empire, 4f),
		(new TextObject("{=DqMfR4H9}Green {NAME}"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Vlandia | NameTrait.Heavy, 4f),
		(new TextObject("{=DqMfR4H9}Green {NAME}"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Khuzait | NameTrait.Nord | NameTrait.Sturgia | NameTrait.Heavy | NameTrait.Trade, 4f),
		(new TextObject("{=rqlsyT28}Golden {NAME}"), NameTrait.LightAndMedium | NameTrait.Empire, 4f),
		(new TextObject("{=rqlsyT28}Golden {NAME}"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Vlandia | NameTrait.Heavy, 4f),
		(new TextObject("{=rqlsyT28}Golden {NAME}"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Khuzait | NameTrait.Nord | NameTrait.Sturgia | NameTrait.Heavy | NameTrait.Trade, 4f),
		(new TextObject("{=WDuVTmua}Black {NAME}"), NameTrait.LightAndMedium | NameTrait.Empire, 4f),
		(new TextObject("{=WDuVTmua}Black {NAME}"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Vlandia | NameTrait.Heavy, 4f),
		(new TextObject("{=WDuVTmua}Black {NAME}"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Khuzait | NameTrait.Nord | NameTrait.Sturgia | NameTrait.Heavy | NameTrait.Trade, 4f),
		(new TextObject("{=YCHKJWPH}Silver {NAME}"), NameTrait.LightAndMedium | NameTrait.Empire, 4f),
		(new TextObject("{=YCHKJWPH}Silver {NAME}"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Vlandia | NameTrait.Heavy, 4f),
		(new TextObject("{=YCHKJWPH}Silver {NAME}"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Khuzait | NameTrait.Nord | NameTrait.Sturgia | NameTrait.Heavy | NameTrait.Trade, 4f),
		(new TextObject("{=vseUmK09}Gray {NAME}"), NameTrait.LightAndMedium | NameTrait.Empire, 4f),
		(new TextObject("{=vseUmK09}Gray {NAME}"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Vlandia | NameTrait.Heavy, 4f),
		(new TextObject("{=vseUmK09}Gray {NAME}"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Khuzait | NameTrait.Nord | NameTrait.Sturgia | NameTrait.Heavy | NameTrait.Trade, 4f),
		(new TextObject("{=4W6VIFQy}White {NAME}"), NameTrait.LightAndMedium | NameTrait.Empire, 4f),
		(new TextObject("{=4W6VIFQy}White {NAME}"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Vlandia | NameTrait.Heavy, 4f),
		(new TextObject("{=4W6VIFQy}White {NAME}"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Khuzait | NameTrait.Nord | NameTrait.Sturgia | NameTrait.Heavy | NameTrait.Trade, 4f),
		(new TextObject("{=5h7uC3ea}Sea {NAME}"), NameTrait.Aserai | NameTrait.Battania | NameTrait.Khuzait | NameTrait.Nord | NameTrait.Sturgia | NameTrait.Vlandia | NameTrait.Heavy, 4f),
		(new TextObject("{=T6M299YZ}Iron {NAME}"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Khuzait | NameTrait.Heavy, 4f),
		(new TextObject("{=vBBVysYn}Bronze {NAME}"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Khuzait | NameTrait.Heavy, 4f),
		(new TextObject("{=YK07f3P5}{NAME} of the Ice"), NameTrait.Vlandia | NameTrait.Trade, 4f),
		(new TextObject("{=MmJLmoTG}{NAME} of the North Wind"), NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade, 4f),
		(new TextObject("{=vGOdCk10}{NAME} of the West Wind"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade, 4f),
		(new TextObject("{=Gv8QS2ir}{NAME} of the South Wind"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade, 4f),
		(new TextObject("{=GUGb3elb}{NAME} of the Desert Wind"), NameTrait.Aserai | NameTrait.Trade, 4f),
		(new TextObject("{=DDO8zNWb}{NAME} of the Steppe Wind"), NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade, 4f),
		(new TextObject("{=errJ9sPD}{NAME} of the East Wind"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade, 4f),
		(new TextObject("{=C5eXktem}{NAME} of the Tempest"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Trade, 4f),
		(new TextObject("{=PPxkvzaI}{NAME} of the Seven Seas"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Trade, 4f),
		(new TextObject("{=aBpPqNSV}{NAME} of the Oceans"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Trade, 4f),
		(new TextObject("{=o4f2am1S}{NAME} of the Four Winds"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade, 4f),
		(new TextObject("{=zGXMh3cK}{NAME} of the Summer Wind"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Trade, 4f),
		(new TextObject("{=7wacHLoB}{NAME} of the Monsoons"), NameTrait.Aserai | NameTrait.Trade, 4f),
		(new TextObject("{=Sr5g7eGT}{NAME} of the Hidden Isles"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade, 4f),
		(new TextObject("{=2bpDbXgH}{NAME} of the Southern Isles"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Trade, 4f),
		(new TextObject("{=vaKsHBk4}{NAME} of the Jade Sea"), NameTrait.Aserai | NameTrait.Trade, 4f),
		(new TextObject("{=X2hW2ZK8}{NAME} of the Lysian Gates"), NameTrait.Empire | NameTrait.Trade, 4f),
		(new TextObject("{=7SNvOuJZ}{NAME} of the Perfumed Isles"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Trade, 4f),
		(new TextObject("{=8nOygNES}{NAME} of the North Star"), NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade, 4f),
		(new TextObject("{=jXkBO9cE}{NAME} of the Southern Stars"), NameTrait.Aserai | NameTrait.Trade, 4f),
		(new TextObject("{=YdrcIKM4}{NAME} of the Evening Star"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade, 4f),
		(new TextObject("{=KHvKnQlq}{NAME} of Balion"), NameTrait.Vlandia | NameTrait.Trade, 4f),
		(new TextObject("{=D1FhdIi9}{NAME} of Geroia"), NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade, 4f),
		(new TextObject("{=BoahRhBP}{NAME} of the Biscan"), NameTrait.Vlandia | NameTrait.Trade, 4f),
		(new TextObject("{=upUpjeLB}{NAME} of Charas"), NameTrait.Vlandia | NameTrait.Trade, 4f),
		(new TextObject("{=AkL8Au7C}{NAME} of Vostrum"), NameTrait.Empire | NameTrait.Trade, 4f),
		(new TextObject("{=1a7vKHgp}{NAME} of Zeonica"), NameTrait.Empire | NameTrait.Trade, 4f),
		(new TextObject("{=1FIatE6X}{NAME} of Ostican"), NameTrait.Vlandia | NameTrait.Trade, 4f),
		(new TextObject("{=RFus9sqB}Ouroboros"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Empire | NameTrait.Heavy, 1f),
		(new TextObject("{=boyQwJ1m}Houndfish"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Khuzait | NameTrait.Nord | NameTrait.Sturgia | NameTrait.Vlandia, 1f),
		(new TextObject("{=lTbqQ9bz}Dogfish"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Battania | NameTrait.Khuzait | NameTrait.Nord | NameTrait.Sturgia | NameTrait.Vlandia, 1f),
		(new TextObject("{=jQpgsw8r}Swordfish"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Vlandia, 1f),
		(new TextObject("{=87bHj9A2}Sawfish"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire, 1f),
		(new TextObject("{=27YBAeBC}Blackfish"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Battania | NameTrait.Khuzait | NameTrait.Nord | NameTrait.Sturgia | NameTrait.Vlandia, 1f),
		(new TextObject("{=Rq9LsTZd}Codfish"), NameTrait.Battania | NameTrait.Sturgia | NameTrait.Vlandia | NameTrait.Trade, 1f),
		(new TextObject("{=9awY7d7g}Mergus"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Vlandia | NameTrait.Trade, 1f),
		(new TextObject("{=W1UHaqXn}Storm-Petrel"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Khuzait | NameTrait.Nord | NameTrait.Sturgia | NameTrait.Vlandia, 1f),
		(new TextObject("{=9YgoQgfF}Mermaid"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade, 1f),
		(new TextObject("{=ObA0FlxH}Golden Mermaid"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade, 1f),
		(new TextObject("{=7fvh1EnT}Silver Mermaid"), NameTrait.Aserai | NameTrait.Vlandia | NameTrait.Trade, 1f),
		(new TextObject("{=lGJttwHt}Golden Dromedary"), NameTrait.Aserai | NameTrait.Trade, 1f),
		(new TextObject("{=1IBdhv6m}White Dromedary"), NameTrait.Aserai | NameTrait.Trade, 1f),
		(new TextObject("{=bGZjuUDa}Black Dromedary"), NameTrait.Aserai | NameTrait.Trade, 1f),
		(new TextObject("{=AYMp00V6}Camel of the Nahasa"), NameTrait.Aserai | NameTrait.Trade, 1f),
		(new TextObject("{=bKIfIrpa}Fighting Cockerel"), NameTrait.LightAndMedium | NameTrait.Sturgia | NameTrait.Vlandia, 1f),
		(new TextObject("{=aPhXFPcT}Red Rooster"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Sturgia | NameTrait.Vlandia, 1f),
		(new TextObject("{=WJX03gKf}Golden Eel"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Battania | NameTrait.Sturgia | NameTrait.Vlandia, 1f),
		(new TextObject("{=aBveRvWW}Silver Eel"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Battania | NameTrait.Empire | NameTrait.Sturgia | NameTrait.Vlandia, 1f),
		(new TextObject("{=UDgMFtzL}Moray Eel"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Battania | NameTrait.Empire | NameTrait.Vlandia, 1f),
		(new TextObject("{=xQi4P54b}Beluga"), NameTrait.LightAndMedium | NameTrait.Sturgia, 1f),
		(new TextObject("{=sTEQvac6}Kraken"), NameTrait.LightAndMedium | NameTrait.Empire | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=MsLvZKOY}Stingray"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire, 1f),
		(new TextObject("{=5R17a1JM}Lobster"), NameTrait.Battania | NameTrait.Sturgia | NameTrait.Vlandia | NameTrait.Trade, 1f),
		(new TextObject("{=dOGq1Kna}Mullet"), NameTrait.Battania | NameTrait.Sturgia | NameTrait.Vlandia | NameTrait.Trade, 1f),
		(new TextObject("{=pTEJQmFt}Mackerel"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Battania | NameTrait.Vlandia, 1f),
		(new TextObject("{=7hp485w6}Herring"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Sturgia | NameTrait.Trade, 1f),
		(new TextObject("{=z7K50H9r}Albacore"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Trade, 1f),
		(new TextObject("{=9u7Xc1Ut}Senate and People"), NameTrait.Empire | NameTrait.Heavy, 1f),
		(new TextObject("{=vLmdGlBp}Thalassarch"), NameTrait.Empire | NameTrait.Heavy, 1f),
		(new TextObject("{=9AiyiCb1}Great Tethys"), NameTrait.Empire | NameTrait.Heavy, 1f),
		(new TextObject("{=fXHy3mOb}Might of Cetus"), NameTrait.Empire | NameTrait.Heavy, 1f),
		(new TextObject("{=6Cdlb2cd}Banner of Calradios"), NameTrait.Empire | NameTrait.Heavy, 1f),
		(new TextObject("{=868vxgZt}Sun of Alixenios"), NameTrait.Empire | NameTrait.Heavy, 1f),
		(new TextObject("{=YlOkpsf8}Autokrator"), NameTrait.Empire | NameTrait.Heavy, 1f),
		(new TextObject("{=fupLwOHL}Vasileos"), NameTrait.Empire | NameTrait.Heavy, 1f),
		(new TextObject("{=JxZ5IMJp}Princess Sarpea"), NameTrait.Empire | NameTrait.Heavy, 1f),
		(new TextObject("{=ghoX4M9O}Mount Aracathos"), NameTrait.Empire | NameTrait.Heavy, 1f),
		(new TextObject("{=uKvTvMP4}Mount Erithrys"), NameTrait.Empire | NameTrait.Heavy, 1f),
		(new TextObject("{=Gn4eEJOa}Wrath of Typhon"), NameTrait.Empire | NameTrait.Heavy, 1f),
		(new TextObject("{=6LlLXwOR}Smile of Akhileos"), NameTrait.Empire | NameTrait.Heavy, 1f),
		(new TextObject("{=qQpwMEVO}Revenge of Serapeos"), NameTrait.Empire | NameTrait.Heavy, 1f),
		(new TextObject("{=FYmdME8j}Transtemean Wind"), NameTrait.Empire | NameTrait.Heavy, 1f),
		(new TextObject("{=MBAesOd0}Zeonic Wind"), NameTrait.LightAndMedium | NameTrait.Empire, 1f),
		(new TextObject("{=7sqi9P0w}Zephyr"), NameTrait.LightAndMedium | NameTrait.Empire, 1f),
		(new TextObject("{=cVbeR6AW}Lycanthropos"), NameTrait.LightAndMedium | NameTrait.Empire, 1f),
		(new TextObject("{=ACzfQv3T}Vrykolakas"), NameTrait.LightAndMedium | NameTrait.Empire, 1f),
		(new TextObject("{=0jgcowNU}Nereid"), NameTrait.LightAndMedium | NameTrait.Empire, 1f),
		(new TextObject("{=0RUL4jh8}Lamia"), NameTrait.LightAndMedium | NameTrait.Empire, 1f),
		(new TextObject("{=9Pr1gVrR}Myrmidon"), NameTrait.LightAndMedium | NameTrait.Empire, 1f),
		(new TextObject("{=RDKUfi9l}Hippalectryon"), NameTrait.LightAndMedium | NameTrait.Empire, 1f),
		(new TextObject("{=Qn2xEoOz}Scourge of the Barbarians"), NameTrait.Empire | NameTrait.Heavy, 1f),
		(new TextObject("{=VNtVAxQF}Tamer of the Myzead"), NameTrait.Empire | NameTrait.Heavy, 1f),
		(new TextObject("{=H1rQdt0h}Subduer of the Perassic"), NameTrait.Empire | NameTrait.Heavy, 1f),
		(new TextObject("{=FNcuiONh}Sea Tsar"), NameTrait.Sturgia | NameTrait.Heavy, 1f),
		(new TextObject("{=840RUJwg}Bogatyr"), NameTrait.Sturgia | NameTrait.Heavy, 1f),
		(new TextObject("{=Xbr5wKmo}Archangel"), NameTrait.Sturgia | NameTrait.Heavy, 1f),
		(new TextObject("{=eSbZi6xs}Moryana"), NameTrait.Sturgia | NameTrait.Heavy, 1f),
		(new TextObject("{=wUrg3H2w}Chernobog's Laughter"), NameTrait.Sturgia | NameTrait.Heavy, 1f),
		(new TextObject("{=V1t5aRMl}Stallion of Tyal"), NameTrait.LightAndMedium | NameTrait.Sturgia | NameTrait.Heavy, 1f),
		(new TextObject("{=p4OZyYgh}Vodyanoy"), NameTrait.LightAndMedium | NameTrait.Sturgia, 1f),
		(new TextObject("{=U1Qn1JZM}Karakaz"), NameTrait.LightAndMedium | NameTrait.Sturgia, 1f),
		(new TextObject("{=8zEWkLAE}Rusalka"), NameTrait.LightAndMedium | NameTrait.Sturgia, 1f),
		(new TextObject("{=dQwETpdC}Scythe of Nav"), NameTrait.LightAndMedium | NameTrait.Sturgia, 1f),
		(new TextObject("{=7ASz5f1a}Bear of Velos"), NameTrait.LightAndMedium | NameTrait.Sturgia, 1f),
		(new TextObject("{=Ciat3lsP}Mandate of the Great Sky"), NameTrait.Khuzait | NameTrait.Heavy, 1f),
		(new TextObject("{=TmUmadhP}Sons of the She-Wolf"), NameTrait.Khuzait | NameTrait.Heavy, 1f),
		(new TextObject("{=DrsB9HMG}Will of the Kurultai"), NameTrait.Khuzait | NameTrait.Heavy, 1f),
		(new TextObject("{=u51uZVjs}Arrow of Urkhun"), NameTrait.Khuzait | NameTrait.Heavy, 1f),
		(new TextObject("{=aCocYZSt}Steed of the Ultaiga"), NameTrait.LightAndMedium | NameTrait.Khuzait, 1f),
		(new TextObject("{=zjZpbQ1z}Gift of Bura Khan"), NameTrait.Khuzait | NameTrait.Heavy, 1f),
		(new TextObject("{=LUnfNHnM}Sword of Matyr"), NameTrait.LightAndMedium | NameTrait.Khuzait, 1f),
		(new TextObject("{=iPSzZd9B}Shyngay's Delight"), NameTrait.LightAndMedium | NameTrait.Khuzait, 1f),
		(new TextObject("{=aN2WQlbB}Sign of Ulgen"), NameTrait.Khuzait | NameTrait.Heavy, 1f),
		(new TextObject("{=gm5hsK25}Fury of Erlik"), NameTrait.LightAndMedium | NameTrait.Khuzait, 1f),
		(new TextObject("{=QSw6aJGV}Blessing of Ulukayin"), NameTrait.LightAndMedium | NameTrait.Khuzait, 1f),
		(new TextObject("{=qXb7YoGc}Asaligat"), NameTrait.LightAndMedium | NameTrait.Khuzait, 1f),
		(new TextObject("{=K4XxeGbc}Tulpar"), NameTrait.LightAndMedium | NameTrait.Khuzait, 1f),
		(new TextObject("{=Q33lWCaj}Talon of the Zilant"), NameTrait.LightAndMedium | NameTrait.Khuzait, 1f),
		(new TextObject("{=LGCKTQy2}Konrul"), NameTrait.LightAndMedium | NameTrait.Khuzait, 1f),
		(new TextObject("{=ofecuaPj}Guiding Star"), NameTrait.LightAndMedium | NameTrait.Khuzait, 1f),
		(new TextObject("{=RUv05qsg}Light of Dawn"), NameTrait.LightAndMedium | NameTrait.Khuzait, 1f),
		(new TextObject("{=xmz9r8P1}Wind Horse"), NameTrait.LightAndMedium | NameTrait.Khuzait, 1f),
		(new TextObject("{=5bv0Hc84}Storm-Spirit"), NameTrait.LightAndMedium | NameTrait.Khuzait, 1f),
		(new TextObject("{=C0fTWQGk}Ironskin"), NameTrait.LightAndMedium | NameTrait.Khuzait, 1f),
		(new TextObject("{=aBtcQVtm}Simurgh"), NameTrait.LightAndMedium | NameTrait.Khuzait, 1f),
		(new TextObject("{=LtGauLC1}Sigil of Queen Eshora"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Heavy, 1f),
		(new TextObject("{=woCajT5W}Consort of Tiamat"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Heavy, 1f),
		(new TextObject("{=2ReQJtZI}Invincible Sun"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Heavy, 1f),
		(new TextObject("{=WAwkgdCj}Feather of Truth"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Heavy, 1f),
		(new TextObject("{=P4IE7fXb}Lamassu"), NameTrait.LightAndMedium | NameTrait.Aserai, 1f),
		(new TextObject("{=DKz1aPbN}Warding Hand"), NameTrait.LightAndMedium | NameTrait.Aserai, 1f),
		(new TextObject("{=Z1x9wz33}Steed of Asera"), NameTrait.LightAndMedium | NameTrait.Aserai, 1f),
		(new TextObject("{=cdmwGRa4}Haboob"), NameTrait.LightAndMedium | NameTrait.Aserai, 1f),
		(new TextObject("{=5aFO3p3l}Simoom"), NameTrait.LightAndMedium | NameTrait.Aserai, 1f),
		(new TextObject("{=q5Lfs1qS}Ghibli"), NameTrait.LightAndMedium | NameTrait.Aserai, 1f),
		(new TextObject("{=KgSyBBFP}Pharaoh's Eye"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Heavy, 1f),
		(new TextObject("{=oHPSr4VW}Khamsin"), NameTrait.LightAndMedium | NameTrait.Aserai, 1f),
		(new TextObject("{=o1RLBGsT}Golden Rukh"), NameTrait.Aserai | NameTrait.Heavy, 1f),
		(new TextObject("{=TZ5CIXBH}Rukh's Talons"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Heavy, 1f),
		(new TextObject("{=SaiEuTfS}Bird of Jebel Qaf"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Heavy, 1f),
		(new TextObject("{=tnDzXWd5}Whirlwind"), NameTrait.LightAndMedium | NameTrait.Aserai, 1f),
		(new TextObject("{=PWZQUadt}Moon Upon Clouds"), NameTrait.LightAndMedium | NameTrait.Aserai, 1f),
		(new TextObject("{=nJBxI8hg}Anqa of the Sunset"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Heavy, 1f),
		(new TextObject("{=VPevzPsM}Saluqi Hound"), NameTrait.LightAndMedium | NameTrait.Aserai, 1f),
		(new TextObject("{=yE3Y8IO8}Ghula's Kiss"), NameTrait.LightAndMedium | NameTrait.Aserai, 1f),
		(new TextObject("{=no4fOW8Y}Water of Ziram"), NameTrait.LightAndMedium | NameTrait.Aserai, 1f),
		(new TextObject("{=9aozS6Mk}Lord of the Horns"), NameTrait.LightAndMedium | NameTrait.Aserai, 1f),
		(new TextObject("{=0jnH1Uza}Djinn-King"), NameTrait.LightAndMedium | NameTrait.Aserai, 1f),
		(new TextObject("{=5jeB6Sxa}Djinn's Cavalcade"), NameTrait.LightAndMedium | NameTrait.Aserai, 1f),
		(new TextObject("{=6JqD7i7C}Blue Flame"), NameTrait.LightAndMedium | NameTrait.Aserai, 1f),
		(new TextObject("{=oHeJRau1}Breath of the Djinn"), NameTrait.LightAndMedium | NameTrait.Aserai, 1f),
		(new TextObject("{=SgBaUv2r}Red Planet"), NameTrait.LightAndMedium | NameTrait.Aserai, 1f),
		(new TextObject("{=5fFzNCdZ}Malaq's Defiance"), NameTrait.LightAndMedium | NameTrait.Aserai, 1f),
		(new TextObject("{=pYhlkp70}Raging Hamadryas"), NameTrait.LightAndMedium | NameTrait.Aserai, 1f),
		(new TextObject("{=Q4sO8AZd}Nahasawi"), NameTrait.LightAndMedium | NameTrait.Aserai, 1f),
		(new TextObject("{=QHft3oRr}Rock of Glanys"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Heavy, 1f),
		(new TextObject("{=vYLbwCeF}Battle-Howl of Curlac"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Heavy, 1f),
		(new TextObject("{=gJ8JqhaQ}Mare of Eria"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Heavy, 1f),
		(new TextObject("{=imQbR7Cg}Boar of Torc Lugh"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Heavy, 1f),
		(new TextObject("{=nATHwmtR}Queen Tara"), NameTrait.Battania | NameTrait.Heavy, 1f),
		(new TextObject("{=SZPRJAAf}Bull of Cul"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Heavy, 1f),
		(new TextObject("{=LNDMIMVb}Lir's Wrath"), NameTrait.LightAndMedium | NameTrait.Battania, 1f),
		(new TextObject("{=tUvmEb8T}Dornal of the Harp"), NameTrait.LightAndMedium | NameTrait.Battania, 1f),
		(new TextObject("{=eHZguHKP}Hound of the Otherworld"), NameTrait.LightAndMedium | NameTrait.Battania, 1f),
		(new TextObject("{=AdNm9ieF}Bellow of Tryth"), NameTrait.LightAndMedium | NameTrait.Battania, 1f),
		(new TextObject("{=NLQvMD8L}Ark of the Gal"), NameTrait.LightAndMedium | NameTrait.Battania, 1f),
		(new TextObject("{=IpVZvz06}Shriek of Cathern"), NameTrait.LightAndMedium | NameTrait.Battania, 1f),
		(new TextObject("{=he6PPJIv}Ocean-Steed"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=Ln6EAz7S}Wave-Breaker"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=QRbebVR6}Salt Mare"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=1ybOm0PV}Woe-Bringer"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=tCzb3orN}Widow-Maker"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=Xj2dvQZe}Barrow-Filler"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=4FIUt3SN}Ran's Doorman"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=Wr1aXEU6}Hull-Biter"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=cba5bHbj}Stormcrow"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=yqBo2714}Gale-Rider"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=eV8ZaxVK}Eel-Feeder"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=n2c3mOar}Oaken Serpent"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=2o9iEiZD}Naglfar-Builder"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=5vRL70It}Devouring Wolf"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=nGUBW6v0}Fryr's Pocket-Contents"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=O3HM2T4Y}Corpse-Forger"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=5mxFBDao}Wind's Teeth"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=5bo6R8Pj}Bloody Wake"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=rMFwEIBG}Terror's Envoy"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=GF8NI4ak}Scythe of Men"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=b4dbbGSO}Foe-Scatterer"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=gazxad3b}Death's Harbringer"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=y8t3Hbq0}Breath-Quencher"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=d0ribjEv}Hralnar's Bane"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=4lCPiaXb}Utgard's Joke"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=uCCz0lIr}Keel-Snapper"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=qfmqjQVg}Draugr"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=u8xi68be}Frost-Giant"), NameTrait.Nord | NameTrait.Heavy, 1f),
		(new TextObject("{=2ZLW2bGS}Steed of the Whale-Road"), NameTrait.LightAndMedium | NameTrait.Nord | NameTrait.Heavy | NameTrait.Trade, 1f),
		(new TextObject("{=1gVLf0pN}Ale-Cask"), NameTrait.Nord | NameTrait.Trade, 1f),
		(new TextObject("{=vwV4IPbX}Rorqual"), NameTrait.LightAndMedium | NameTrait.Vlandia | NameTrait.Heavy, 1f),
		(new TextObject("{=yFOn97b1}Cachalot"), NameTrait.LightAndMedium | NameTrait.Vlandia, 1f),
		(new TextObject("{=oil0bod6}Wyvern"), NameTrait.LightAndMedium | NameTrait.Vlandia | NameTrait.Heavy, 1f),
		(new TextObject("{=Zp0vicNN}Salamander"), NameTrait.LightAndMedium | NameTrait.Vlandia, 1f),
		(new TextObject("{=TcNaPjpT}Basilisk"), NameTrait.LightAndMedium | NameTrait.Vlandia, 1f),
		(new TextObject("{=cvQAbYTz}Cameleopard"), NameTrait.LightAndMedium | NameTrait.Vlandia, 1f),
		(new TextObject("{=Ob41ouaL}Draconopedes"), NameTrait.LightAndMedium | NameTrait.Vlandia, 1f),
		(new TextObject("{=nX3uZGNy}Jackdaw"), NameTrait.LightAndMedium | NameTrait.Vlandia, 1f),
		(new TextObject("{=JqzQyz4P}Manticore"), NameTrait.LightAndMedium | NameTrait.Vlandia, 1f),
		(new TextObject("{=5rBfXrWW}Zedrosis"), NameTrait.LightAndMedium | NameTrait.Vlandia, 1f),
		(new TextObject("{=G9gL45wV}Hippocampus"), NameTrait.LightAndMedium | NameTrait.Vlandia, 1f),
		(new TextObject("{=AvdcY9xx}Porbeagle"), NameTrait.LightAndMedium | NameTrait.Vlandia, 1f),
		(new TextObject("{=B2jB58ID}Gatopard"), NameTrait.LightAndMedium | NameTrait.Vlandia, 1f),
		(new TextObject("{=buCTTnvU}Bold Vilund"), NameTrait.LightAndMedium | NameTrait.Vlandia | NameTrait.Heavy, 1f),
		(new TextObject("{=dVOdKBvE}Good King Bonneric"), NameTrait.LightAndMedium | NameTrait.Vlandia | NameTrait.Heavy, 1f),
		(new TextObject("{=0YhJD3ei}Worthy Rotbard"), NameTrait.LightAndMedium | NameTrait.Vlandia | NameTrait.Heavy, 1f),
		(new TextObject("{=miaiftLB}Paladin Aganalt"), NameTrait.LightAndMedium | NameTrait.Vlandia | NameTrait.Heavy, 1f),
		(new TextObject("{=oEuc40lO}Loyal Gundelm"), NameTrait.LightAndMedium | NameTrait.Vlandia | NameTrait.Heavy, 1f),
		(new TextObject("{=Q0U5s3wP}Bayard"), NameTrait.LightAndMedium | NameTrait.Vlandia | NameTrait.Heavy, 1f),
		(new TextObject("{=OyoWQKCf}Vigilant"), NameTrait.LightAndMedium | NameTrait.Vlandia | NameTrait.Heavy, 1f),
		(new TextObject("{=T5LYBako}Pale Horseman"), NameTrait.LightAndMedium | NameTrait.Vlandia | NameTrait.Heavy, 1f),
		(new TextObject("{=XiND8dN3}Saucy Gallard"), NameTrait.LightAndMedium | NameTrait.Vlandia | NameTrait.Heavy, 1f),
		(new TextObject("{=RBKgTtug}Cunning Tarsil"), NameTrait.LightAndMedium | NameTrait.Vlandia, 1f),
		(new TextObject("{=MBypc4Tk}Alerion-Bird"), NameTrait.LightAndMedium | NameTrait.Vlandia, 1f)
	});

	private MBReadOnlyList<(TextObject, NameTrait)> _firstNames = new MBReadOnlyList<(TextObject, NameTrait)>(new List<(TextObject, NameTrait)>
	{
		(new TextObject("{=n4V81LNV}Jackal"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire | NameTrait.Khuzait),
		(new TextObject("{=R8I4QRvS}Gazelle"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire),
		(new TextObject("{=Llbz2iqf}Leopard"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire | NameTrait.Khuzait),
		(new TextObject("{=oshK5hAJ}Panther"), NameTrait.LightAndMedium | NameTrait.Empire | NameTrait.Vlandia),
		(new TextObject("{=oe6XS1cg}Hound"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia),
		(new TextObject("{=lG0KHx9d}Lynx"), NameTrait.LightAndMedium | NameTrait.Empire | NameTrait.Khuzait | NameTrait.Sturgia | NameTrait.Vlandia),
		(new TextObject("{=eo1F3Ghs}Cheetah"), NameTrait.LightAndMedium | NameTrait.Aserai),
		(new TextObject("{=mdFJonjK}Ibex"), NameTrait.LightAndMedium | NameTrait.Aserai),
		(new TextObject("{=wRtDPT3i}Falcon"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire | NameTrait.Khuzait | NameTrait.Sturgia | NameTrait.Vlandia),
		(new TextObject("{=AUaUGDaS}Kestrel"), NameTrait.Empire | NameTrait.Sturgia | NameTrait.Vlandia),
		(new TextObject("{=aSuWXMiM}Eagle"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Battania | NameTrait.Empire | NameTrait.Khuzait | NameTrait.Vlandia),
		(new TextObject("{=4dDCLq6Y}Ostrich"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire),
		(new TextObject("{=NVKwvl1G}Raven"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Empire | NameTrait.Khuzait | NameTrait.Nord | NameTrait.Sturgia | NameTrait.Vlandia),
		(new TextObject("{=VKFTub9a}Hawk"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Battania | NameTrait.Empire | NameTrait.Khuzait | NameTrait.Vlandia),
		(new TextObject("{=3dFnXRau}Heron"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire | NameTrait.Sturgia | NameTrait.Vlandia),
		(new TextObject("{=aSuWXMiM}Eagle"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Battania | NameTrait.Empire | NameTrait.Khuzait | NameTrait.Nord | NameTrait.Sturgia | NameTrait.Vlandia),
		(new TextObject("{=spgbMr1c}Parrot"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire),
		(new TextObject("{=4D0Y25hE}Owl"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire | NameTrait.Khuzait | NameTrait.Sturgia | NameTrait.Vlandia),
		(new TextObject("{=6RvO9UVG}Serpent"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Battania | NameTrait.Empire),
		(new TextObject("{=LTOaBiw3}Viper"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia),
		(new TextObject("{=usWAF8Wz}Asp"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire | NameTrait.Khuzait),
		(new TextObject("{=MbwwhiBo}Wolf"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Battania | NameTrait.Empire | NameTrait.Khuzait | NameTrait.Nord | NameTrait.Sturgia | NameTrait.Vlandia),
		(new TextObject("{=jjLUSzAk}Fox"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Battania | NameTrait.Empire | NameTrait.Vlandia),
		(new TextObject("{=ZQ4yL6gm}Hind"), NameTrait.LightAndMedium | NameTrait.Empire | NameTrait.Vlandia),
		(new TextObject("{=GUZRA5FT}Mare"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire),
		(new TextObject("{=TamM3Dpt}Unicorn"), NameTrait.LightAndMedium | NameTrait.Empire | NameTrait.Vlandia),
		(new TextObject("{=MbwwhiBo}Wolf"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Empire | NameTrait.Vlandia),
		(new TextObject("{=v4IDh2rE}Ghost"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Battania | NameTrait.Empire | NameTrait.Vlandia),
		(new TextObject("{=shipNameRam}Ram"), NameTrait.LightAndMedium | NameTrait.Aserai),
		(new TextObject("{=C5bsSTdu}Witch"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire | NameTrait.Nord | NameTrait.Sturgia | NameTrait.Vlandia),
		(new TextObject("{=TvbM2SMy}Centaur"), NameTrait.LightAndMedium | NameTrait.Empire),
		(new TextObject("{=jINLipTa}Scorpion"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire),
		(new TextObject("{=xerBRVAL}Wasp"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire),
		(new TextObject("{=IM1Fbb2V}Hornet"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire),
		(new TextObject("{=sxkwm8qn}Palmatian"), NameTrait.LightAndMedium | NameTrait.Empire),
		(new TextObject("{=PnfGEvfu}Canterion"), NameTrait.LightAndMedium | NameTrait.Empire),
		(new TextObject("{=nIfwtTXx}Ibis"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire),
		(new TextObject("{=hLqXTb5N}Badger"), NameTrait.LightAndMedium | NameTrait.Empire | NameTrait.Sturgia | NameTrait.Vlandia),
		(new TextObject("{=0bjYJLMo}Ferret"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Empire),
		(new TextObject("{=cqPflDvo}Pelican"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia),
		(new TextObject("{=cqPflDvo}Pelican"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Sturgia | NameTrait.Vlandia | NameTrait.Trade),
		(new TextObject("{=1TBEbQbp}Dolphin"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia),
		(new TextObject("{=1TBEbQbp}Dolphin"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Sturgia | NameTrait.Trade),
		(new TextObject("{=S51n3cnJ}Gull"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia),
		(new TextObject("{=S51n3cnJ}Gull"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Khuzait | NameTrait.Sturgia | NameTrait.Trade),
		(new TextObject("{=ZTfLT4dD}Cormorant"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia),
		(new TextObject("{=ZTfLT4dD}Cormorant"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Khuzait | NameTrait.Sturgia | NameTrait.Trade),
		(new TextObject("{=Ludz63ZI}Albatross"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia),
		(new TextObject("{=Ludz63ZI}Albatross"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Khuzait | NameTrait.Sturgia | NameTrait.Trade),
		(new TextObject("{=dqD0HRje}Osprey"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia),
		(new TextObject("{=dqD0HRje}Osprey"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Khuzait | NameTrait.Sturgia | NameTrait.Trade),
		(new TextObject("{=tqgCWg4i}Marlin"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia),
		(new TextObject("{=nFFRwbCy}Barracuda"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia),
		(new TextObject("{=39zy02Jd}Hare"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Khuzait | NameTrait.Nord | NameTrait.Sturgia | NameTrait.Trade),
		(new TextObject("{=iW7EXqiS}Roebuck"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Sturgia | NameTrait.Trade),
		(new TextObject("{=Zyi2ILYy}Antelope"), NameTrait.LightAndMedium | NameTrait.Khuzait | NameTrait.Vlandia | NameTrait.Trade),
		(new TextObject("{=o61Aevoo}Spoonbill"), NameTrait.Khuzait | NameTrait.Trade),
		(new TextObject("{=96uWB0JQ}Kingfisher"), NameTrait.LightAndMedium | NameTrait.Khuzait | NameTrait.Sturgia | NameTrait.Trade),
		(new TextObject("{=ZgVtOLFQ}Otter"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Nord | NameTrait.Sturgia | NameTrait.Trade),
		(new TextObject("{=LivxbamB}Marten"), NameTrait.Battania | NameTrait.Nord | NameTrait.Sturgia | NameTrait.Trade),
		(new TextObject("{=xpWEXt4K}Heifer"), NameTrait.Battania | NameTrait.Sturgia | NameTrait.Trade),
		(new TextObject("{=ZSA1mySL}Swan"), NameTrait.LightAndMedium | NameTrait.Battania | NameTrait.Khuzait | NameTrait.Sturgia | NameTrait.Trade),
		(new TextObject("{=ZQ4yL6gm}Hind"), NameTrait.Battania | NameTrait.Khuzait | NameTrait.Sturgia | NameTrait.Trade),
		(new TextObject("{=8Aa4J5VU}Bear"), NameTrait.Battania | NameTrait.Khuzait | NameTrait.Vlandia | NameTrait.Heavy),
		(new TextObject("{=sRGUcmGT}Buffalo"), NameTrait.Khuzait | NameTrait.Heavy | NameTrait.Trade),
		(new TextObject("{=ecbh2GPS}Stallion"), NameTrait.LightAndMedium | NameTrait.Aserai | NameTrait.Khuzait | NameTrait.Sturgia | NameTrait.Heavy),
		(new TextObject("{=0OrIliBh}Boar"), NameTrait.Battania | NameTrait.Nord | NameTrait.Sturgia | NameTrait.Vlandia | NameTrait.Heavy),
		(new TextObject("{=gXKRAWmN}Behemoth"), NameTrait.Aserai | NameTrait.Vlandia | NameTrait.Heavy),
		(new TextObject("{=0bR3n4TR}Leviathan"), NameTrait.Aserai | NameTrait.Vlandia | NameTrait.Heavy),
		(new TextObject("{=GkvX7z6Y}Dragon"), NameTrait.Aserai | NameTrait.Nord | NameTrait.Sturgia | NameTrait.Vlandia | NameTrait.Heavy),
		(new TextObject("{=iB1OFdgG}Troll"), NameTrait.Nord | NameTrait.Heavy),
		(new TextObject("{=cfn3pbPM}Giant"), NameTrait.Nord | NameTrait.Vlandia | NameTrait.Heavy),
		(new TextObject("{=LeN5ab67}Griffin"), NameTrait.Aserai | NameTrait.Vlandia | NameTrait.Heavy),
		(new TextObject("{=lZJGAUmb}Crocodile"), NameTrait.Aserai | NameTrait.Vlandia | NameTrait.Heavy),
		(new TextObject("{=nEIeg8bj}Wyrm"), NameTrait.Nord | NameTrait.Heavy),
		(new TextObject("{=1EUJ4F5o}Bull"), NameTrait.Battania | NameTrait.Khuzait | NameTrait.Nord | NameTrait.Heavy),
		(new TextObject("{=D0SX1cFQ}Lion"), NameTrait.Khuzait | NameTrait.Vlandia | NameTrait.Heavy),
		(new TextObject("{=VMaalDyk}Elephant"), NameTrait.Aserai | NameTrait.Khuzait | NameTrait.Vlandia | NameTrait.Heavy),
		(new TextObject("{=54SsKRD0}Walrus"), NameTrait.Nord | NameTrait.Sturgia | NameTrait.Heavy | NameTrait.Trade),
		(new TextObject("{=8qMm3VIB}Majesty"), NameTrait.Empire | NameTrait.Vlandia | NameTrait.Heavy),
		(new TextObject("{=aoY4ekls}Imperium"), NameTrait.Empire | NameTrait.Heavy),
		(new TextObject("{=MWUzIGTJ}Destiny"), NameTrait.Empire | NameTrait.Heavy),
		(new TextObject("{=uUEvDtIY}Wrath"), NameTrait.Empire | NameTrait.Heavy),
		(new TextObject("{=4yqIAUZa}Concord"), NameTrait.Empire | NameTrait.Heavy),
		(new TextObject("{=azoX77Hp}Wisdom"), NameTrait.Empire | NameTrait.Vlandia | NameTrait.Heavy),
		(new TextObject("{=nrkcgic9}Triumph"), NameTrait.Empire | NameTrait.Heavy),
		(new TextObject("{=BvD7h8gD}Mandate"), NameTrait.Empire | NameTrait.Heavy),
		(new TextObject("{=YIStPMzW}Justice"), NameTrait.Empire | NameTrait.Heavy),
		(new TextObject("{=nVhc43US}Guardian"), NameTrait.Empire | NameTrait.Heavy),
		(new TextObject("{=9KPCUcTL}Sovereignty"), NameTrait.Empire | NameTrait.Heavy),
		(new TextObject("{=shipNameFury}Fury"), NameTrait.Empire | NameTrait.Vlandia | NameTrait.Heavy),
		(new TextObject("{=f5WWBvGQ}Splendor"), NameTrait.Empire | NameTrait.Vlandia | NameTrait.Heavy),
		(new TextObject("{=v5dpjybs}Bounty"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade),
		(new TextObject("{=L3bOOJ7Q}Treasure"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade),
		(new TextObject("{=Zpds5B8d}Chalice"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade),
		(new TextObject("{=WxxVi13T}Pearl"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade),
		(new TextObject("{=FPyhdxJl}Jewel"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade),
		(new TextObject("{=yW3FevJR}Diamond"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade),
		(new TextObject("{=bUNpw29g}Emerald"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade),
		(new TextObject("{=MnzHURUf}Fortune"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade),
		(new TextObject("{=G9zmdS4J}Blessing"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade),
		(new TextObject("{=oMP4RhpF}Luck"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade),
		(new TextObject("{=BMm6tsRm}Princess"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade),
		(new TextObject("{=smlzWHsW}Maiden"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade),
		(new TextObject("{=eodelMzf}Lady"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade),
		(new TextObject("{=SHWI20zH}Queen"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade),
		(new TextObject("{=4TKA4kbv}Bride"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade),
		(new TextObject("{=ZAbwnp54}Fragrance"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade),
		(new TextObject("{=FLa5OuyK}Wanderer"), NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade),
		(new TextObject("{=xKXE1YrD}Pilgrim"), NameTrait.Empire | NameTrait.Sturgia | NameTrait.Vlandia | NameTrait.Trade),
		(new TextObject("{=KTYNd9ps}Angel"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade),
		(new TextObject("{=JS17OAwM}Beacon"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade),
		(new TextObject("{=f8b5go27}Flower"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Trade),
		(new TextObject("{=jLcl52Vw}Rose"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade),
		(new TextObject("{=7EUZITUE}Lotus"), NameTrait.Aserai | NameTrait.Trade),
		(new TextObject("{=wJRNbgRJ}Jasmine"), NameTrait.Aserai | NameTrait.Empire | NameTrait.Trade),
		(new TextObject("{=oKLSbtdr}Lily"), NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade),
		(new TextObject("{=6hdP6O2N}Nymph"), NameTrait.Empire | NameTrait.Vlandia | NameTrait.Trade)
	});

	public override void SyncData(IDataStore dataStore)
	{
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnShipOwnerChangedEvent.AddNonSerializedListener(this, OnShipOwnerChanged);
	}

	private void OnShipOwnerChanged(Ship ship, PartyBase owner, ChangeShipOwnerAction.ShipOwnerChangeDetail detail)
	{
		if (detail == ChangeShipOwnerAction.ShipOwnerChangeDetail.ApplyByMobilePartyCreation || detail == ChangeShipOwnerAction.ShipOwnerChangeDetail.ApplyByProduction)
		{
			AssignNameToShip(ship);
		}
	}

	private TextObject GetRandomFullName(List<int> availableWeights, float totalWeight)
	{
		float num = MBRandom.RandomFloatRanged(totalWeight);
		for (int i = 0; i < availableWeights.Count; i++)
		{
			num -= _fullNames[availableWeights[i]].Item3;
			if (num < 0f)
			{
				return _fullNames[availableWeights[i]].Item1;
			}
		}
		return null;
	}

	private void AssignNameToShip(Ship ship)
	{
		float num = 0f;
		NameTrait nameFlags = GetNameFlags(ship);
		List<int> list = new List<int>();
		for (int i = 0; i < _fullNames.Count; i++)
		{
			if (_fullNames[i].Item2.HasAllFlags(nameFlags))
			{
				list.Add(i);
				num += _fullNames[i].Item3;
			}
		}
		if (list.Count <= 0)
		{
			return;
		}
		TextObject textObject = GetRandomFullName(list, num).CopyTextObject();
		list.Clear();
		for (int j = 0; j < _firstNames.Count; j++)
		{
			if (_firstNames[j].Item2.HasAllFlags(nameFlags))
			{
				list.Add(j);
			}
		}
		if (list.Count > 0)
		{
			TextObject variable = _firstNames[list.GetRandomElement()].Item1.CopyTextObject();
			textObject.SetTextVariable("NAME", variable);
			ship.SetName(textObject);
		}
	}

	private static NameTrait GetNameFlags(Ship ship)
	{
		NameTrait nameTrait = NameTrait.None;
		if (ship.ShipHull.IsTradeShip)
		{
			nameTrait |= NameTrait.Trade;
		}
		else if (ship.ShipHull.Type == ShipHull.ShipType.Light)
		{
			nameTrait |= NameTrait.Light;
		}
		else if (ship.ShipHull.Type == ShipHull.ShipType.Medium)
		{
			nameTrait |= NameTrait.Medium;
		}
		else if (ship.ShipHull.Type == ShipHull.ShipType.Heavy)
		{
			nameTrait |= NameTrait.Heavy;
		}
		CultureObject culture = ship.Owner.Culture;
		if (culture.StringId == "aserai")
		{
			nameTrait |= NameTrait.Aserai;
		}
		else if (culture.StringId == "khuzait")
		{
			nameTrait |= NameTrait.Khuzait;
		}
		else if (culture.StringId == "vlandia")
		{
			nameTrait |= NameTrait.Vlandia;
		}
		else if (culture.StringId == "sturgia")
		{
			nameTrait |= NameTrait.Sturgia;
		}
		else if (culture.StringId == "battania")
		{
			nameTrait |= NameTrait.Battania;
		}
		else if (culture.StringId == "empire")
		{
			nameTrait |= NameTrait.Empire;
		}
		else if (culture.StringId == "nord")
		{
			nameTrait |= NameTrait.Nord;
		}
		return nameTrait;
	}
}
