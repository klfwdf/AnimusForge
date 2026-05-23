using System.Collections.Generic;
using System.Linq;
using Helpers;
using StoryMode.StoryModeObjects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace StoryMode.Quests.FirstPhase;

public class BannerInvestigationQuest : StoryModeQuestBase
{
	private const int NotablesToTalkAmount = 10;

	private const string MonchugStringId = "lord_6_1";

	private const string MesuiStringId = "lord_6_4";

	private const string HurunagStringId = "lord_6_16";

	private const string CaladogStringId = "lord_5_1";

	private const string ErgeonStringId = "lord_5_3";

	private const string MelidirStringId = "lord_5_5";

	private const string DerthertStringId = "lord_4_1";

	private const string UntheryStringId = "lord_4_5";

	private const string IngaltherStringId = "lord_4_16";

	private const string UnqidStringId = "lord_3_1";

	private const string AdramStringId = "lord_3_3";

	private const string TaisStringId = "lord_3_5";

	private const string RaganvadStringId = "lord_2_1";

	private const string OlekStringId = "lord_2_3";

	private const string GodunStringId = "lord_2_5";

	private const string LuconStringId = "lord_1_1";

	private const string PentonStringId = "lord_1_5";

	private const string GariosStringId = "lord_1_7";

	private const string RhagaeaStringId = "lord_1_14";

	[SaveableField(1)]
	private Dictionary<Hero, bool> _noblesToTalk;

	[SaveableField(2)]
	private bool _allNoblesDead;

	[SaveableField(3)]
	private bool _battleSummarized;

	[SaveableField(4)]
	private JournalLog _talkedNotablesQuestLog;

	private TextObject _startQuestLog => new TextObject("{=ysRVZiA6}As you explore Calradia, you can learn more about your artifact and its importance by asking any lord or lady about the Empire's recent history.");

	private TextObject _endQuestLog => new TextObject("{=oA4iTPyV}You have collected enough information.");

	public override TextObject Title => new TextObject("{=zLRlmitp}Investigate 'Neretzes' Folly'");

	public override bool IsRemainingTimeHidden => false;

	public BannerInvestigationQuest()
		: base("investigate_neretzes_banner_quest", null, StoryModeManager.Current.MainStoryLine.FirstPhase.FirstPhaseEndTime)
	{
		_allNoblesDead = false;
	}

	protected override void InitializeQuestOnGameLoad()
	{
		SetDialogs();
	}

	protected override void HourlyTick()
	{
	}

	protected override void RegisterEvents()
	{
		CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
		CampaignEvents.OnPartyRemovedEvent.AddNonSerializedListener(this, OnPartyRemoved);
		CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, OnPartySpawned);
		CampaignEvents.OnPartyLeaderChangedEvent.AddNonSerializedListener(this, OnPartyLeaderChanged);
	}

	private void OnPartyLeaderChanged(MobileParty mobileParty, Hero oldLeader)
	{
		bool num = mobileParty.LeaderHero != null && _noblesToTalk.ContainsKey(mobileParty.LeaderHero);
		bool flag = IsTracked(mobileParty);
		if (num)
		{
			if (!flag)
			{
				AddTrackedObject(mobileParty);
			}
		}
		else if (flag)
		{
			RemoveTrackedObject(mobileParty);
		}
	}

	private void InitializeNotablesToTalkList()
	{
		_noblesToTalk = new Dictionary<Hero, bool>();
		Hero hero = Campaign.Current.CampaignObjectManager.Find<Hero>("lord_6_1");
		if (hero.IsAlive)
		{
			_noblesToTalk.Add(hero, value: false);
		}
		Hero hero2 = Campaign.Current.CampaignObjectManager.Find<Hero>("lord_6_4");
		if (hero2.IsAlive)
		{
			_noblesToTalk.Add(hero2, value: false);
		}
		Hero hero3 = Campaign.Current.CampaignObjectManager.Find<Hero>("lord_6_16");
		if (hero3.IsAlive)
		{
			_noblesToTalk.Add(hero3, value: false);
		}
		Hero hero4 = Campaign.Current.CampaignObjectManager.Find<Hero>("lord_5_1");
		if (hero4.IsAlive)
		{
			_noblesToTalk.Add(hero4, value: false);
		}
		Hero hero5 = Campaign.Current.CampaignObjectManager.Find<Hero>("lord_5_3");
		if (hero5.IsAlive)
		{
			_noblesToTalk.Add(hero5, value: false);
		}
		Hero hero6 = Campaign.Current.CampaignObjectManager.Find<Hero>("lord_5_5");
		if (hero6.IsAlive)
		{
			_noblesToTalk.Add(hero6, value: false);
		}
		Hero hero7 = Campaign.Current.CampaignObjectManager.Find<Hero>("lord_4_1");
		if (hero7.IsAlive)
		{
			_noblesToTalk.Add(hero7, value: false);
		}
		Hero hero8 = Campaign.Current.CampaignObjectManager.Find<Hero>("lord_4_16");
		if (hero8.IsAlive)
		{
			_noblesToTalk.Add(hero8, value: false);
		}
		Hero hero9 = Campaign.Current.CampaignObjectManager.Find<Hero>("lord_4_5");
		if (hero9.IsAlive)
		{
			_noblesToTalk.Add(hero9, value: false);
		}
		Hero hero10 = Campaign.Current.CampaignObjectManager.Find<Hero>("lord_3_1");
		if (hero10.IsAlive)
		{
			_noblesToTalk.Add(hero10, value: false);
		}
		Hero hero11 = Campaign.Current.CampaignObjectManager.Find<Hero>("lord_3_3");
		if (hero11.IsAlive)
		{
			_noblesToTalk.Add(hero11, value: false);
		}
		Hero hero12 = Campaign.Current.CampaignObjectManager.Find<Hero>("lord_3_5");
		if (hero12.IsAlive)
		{
			_noblesToTalk.Add(hero12, value: false);
		}
		Hero hero13 = Campaign.Current.CampaignObjectManager.Find<Hero>("lord_2_1");
		if (hero13.IsAlive)
		{
			_noblesToTalk.Add(hero13, value: false);
		}
		Hero hero14 = Campaign.Current.CampaignObjectManager.Find<Hero>("lord_2_3");
		if (hero14.IsAlive)
		{
			_noblesToTalk.Add(hero14, value: false);
		}
		Hero hero15 = Campaign.Current.CampaignObjectManager.Find<Hero>("lord_2_5");
		if (hero15.IsAlive)
		{
			_noblesToTalk.Add(hero15, value: false);
		}
		Hero hero16 = Campaign.Current.CampaignObjectManager.Find<Hero>("lord_1_1");
		if (hero16.IsAlive)
		{
			_noblesToTalk.Add(hero16, value: false);
		}
		Hero hero17 = Campaign.Current.CampaignObjectManager.Find<Hero>("lord_1_5");
		if (hero17.IsAlive)
		{
			_noblesToTalk.Add(hero17, value: false);
		}
		Hero hero18 = Campaign.Current.CampaignObjectManager.Find<Hero>("lord_1_14");
		if (hero18.IsAlive)
		{
			_noblesToTalk.Add(hero18, value: false);
		}
		Hero hero19 = Campaign.Current.CampaignObjectManager.Find<Hero>("lord_1_7");
		if (hero19.IsAlive)
		{
			_noblesToTalk.Add(hero19, value: false);
		}
		foreach (KeyValuePair<Hero, bool> item in _noblesToTalk)
		{
			AddTrackedObject(item.Key);
			if (item.Key.PartyBelongedTo != null)
			{
				AddTrackedObject(item.Key.PartyBelongedTo);
			}
		}
	}

	protected override void OnStartQuest()
	{
		InitializeNotablesToTalkList();
		SetDialogs();
		_talkedNotablesQuestLog = AddDiscreteLog(_startQuestLog, new TextObject("{=T8naYoGH}Nobles to Talk to"), 0, 10);
	}

	private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
	{
		if (!_allNoblesDead && _noblesToTalk.ContainsKey(victim))
		{
			if (IsTracked(victim))
			{
				RemoveTrackedObject(victim);
			}
			UpdateAllNoblesDead();
		}
	}

	private void UpdateAllNoblesDead()
	{
		int num = 0;
		int num2 = 0;
		foreach (KeyValuePair<Hero, bool> item in _noblesToTalk)
		{
			if (item.Value)
			{
				num++;
			}
			else if (item.Key.IsAlive)
			{
				num2++;
			}
		}
		if (num2 <= 0)
		{
			_allNoblesDead = true;
		}
		if (num >= 9)
		{
			Campaign.Current.ConversationManager.RemoveRelatedLines(this);
			SetDialogs();
		}
	}

	protected override void SetDialogs()
	{
		SetNobleDialogs();
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("hero_main_options", 150).BeginPlayerOptions().PlayerSpecialOption(new TextObject("{=qV1e0x8i}What is 'Neretzes' Folly'?"))
			.Condition(() => Hero.OneToOneConversationHero != null && CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Lord && Hero.OneToOneConversationHero.Clan != Hero.MainHero.Clan && !_battleSummarized && !MobileParty.MainParty.IsInRaftState && (MobileParty.ConversationParty == null || !MobileParty.ConversationParty.IsInRaftState))
			.NpcLine(new TextObject("{=hFYG3lXw}Well, that's what some people call the great Battle of Pendraic in the year 1077.[ib:normal][if:convo_normal]"))
			.NpcLine(new TextObject("{=TKpFB4qN}Emperor Neretzes led an army accompanied by Khuzaits and Aserai to fight a coalition of Sturgians, Battanians and Vlandians."))
			.NpcLine(new TextObject("{=0yxEvgGf}It was a disaster for him - he died in it - but the victors didn't fare much better."))
			.PlayerLine(new TextObject("{=AmBEgOyq}I see..."))
			.Consequence(BattleSummarized)
			.GotoDialogState("lord_pretalk"), this);
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("hero_main_options", 150).BeginPlayerOptions().PlayerSpecialOption(new TextObject("{=6UIa4784}Can you tell me anything about the battle of Pendraic?"))
			.Condition(talk_with_any_noble_condition)
			.NpcLine("{=HDBkwjgf}{NOBLE_ANSWER}")
			.Condition(talk_with_any_noble_continue_condition)
			.Consequence(talk_with_any_noble_consequence)
			.GotoDialogState("lord_pretalk"), this);
	}

	private bool talk_with_any_noble_condition()
	{
		if (_battleSummarized && Hero.OneToOneConversationHero != null && CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Lord && Hero.OneToOneConversationHero.Clan != Hero.MainHero.Clan && !_noblesToTalk.ContainsKey(Hero.OneToOneConversationHero) && !MobileParty.MainParty.IsInRaftState)
		{
			if (MobileParty.ConversationParty != null)
			{
				return !MobileParty.ConversationParty.IsInRaftState;
			}
			return true;
		}
		return false;
	}

	private bool talk_with_any_noble_continue_condition()
	{
		TextObject textObject;
		if (_allNoblesDead)
		{
			textObject = new TextObject("{=HOb07MJF}I don't know much about it. I know two who might, though. {IMPERIAL_MENTOR.LINK} - she once served as Neretez's unofficial spymaster. Then there's {ANTI_IMPERIAL_MENTOR.LINK}. He served on Neretzes' bodyguard, but people say now he's very different than he was back then.");
			StringHelpers.SetCharacterProperties("IMPERIAL_MENTOR", StoryModeHeroes.ImperialMentor.CharacterObject, textObject);
			StringHelpers.SetCharacterProperties("ANTI_IMPERIAL_MENTOR", StoryModeHeroes.AntiImperialMentor.CharacterObject, textObject);
		}
		else
		{
			textObject = new TextObject("{=HddJT1Ve}I wasn't there. I know {HERO.LINK} has some thoughts about it.");
			StringHelpers.SetCharacterProperties("HERO", ((!_noblesToTalk.Any((KeyValuePair<Hero, bool> n) => !n.Value && n.Key.IsAlive && n.Key.Culture == Hero.OneToOneConversationHero.Culture)) ? _noblesToTalk.First((KeyValuePair<Hero, bool> n) => !n.Value && n.Key.IsAlive) : _noblesToTalk.First((KeyValuePair<Hero, bool> n) => !n.Value && n.Key.IsAlive && n.Key.Culture == Hero.OneToOneConversationHero.Culture)).Key.CharacterObject, textObject);
		}
		MBTextManager.SetTextVariable("NOBLE_ANSWER", textObject);
		return true;
	}

	private void BattleSummarized()
	{
		_battleSummarized = true;
		Campaign.Current.ConversationManager.ConversationEndOneShot += UpdateAllNoblesDead;
	}

	private void talk_with_any_noble_consequence()
	{
		if (_allNoblesDead)
		{
			CompleteQuestWithSuccess();
		}
		Campaign.Current.ConversationManager.ConversationEndOneShot += UpdateAllNoblesDead;
	}

	private void SetNobleDialogs()
	{
		foreach (KeyValuePair<Hero, bool> item in _noblesToTalk)
		{
			if (!item.Value)
			{
				TextObject answer;
				TextObject answer2;
				TextObject answer3;
				TextObject answer4;
				if (item.Key.StringId == "lord_6_1")
				{
					answer = new TextObject("{=L1V8L2N1}Yes. The Emperor Neretzes had offered to hire our warriors as mercenaries. I saw nothing wrong with that.[ib:confident3][if:convo_bored]");
					answer2 = new TextObject("{=A9fChKmz}The Empire was an old bear. Well-fed, slow-moving. It wanted to keep what it had. The Sturgians were, and are, hungry wolves. Like us. Sometimes wolves hunt in packs, and sometimes they don't. Sometimes one wolf wants the lion to kill his rival.");
					answer3 = new TextObject("{=tBGtavMG}Most of those who went were Khergits. They are a young clan. Their lineage is not like ours. They were always looking to prove themselves. Anyway, at the battle, their noyan, Solun, was slain alongside most of the males of his house. What can I say? A thirst for glory is dangerous, both to the thirsty one and those around him.");
					answer4 = new TextObject("{=UVuwofhS}Clans rise, clans fall. My duty is to all the Khuzaits. Look at it this way. Were it not for her husband's death, Lady Mesui would never have inherited the leadership of the Khergits. Death creates opportunity. The survivors of a great battle make a great show of mourning, but inside they rejoice.[ib:closed][if:convo_mocking_revenge]");
				}
				else if (item.Key.StringId == "lord_6_4")
				{
					answer = new TextObject("{=OKgsM4nr}I curse that name. It took from me my husband, two brothers, more cousins than I can count.[ib:confident2][if:convo_stern]");
					answer2 = new TextObject("{=KYW1UiKm}The Khergits were never the richest of the clans, but we made up for it with our valor. When word spread that the Emperor was promising silver for men to ride at his side, against the Sturgians and Battanians and others, of course our young brave boys lept at the chance. My husband, the bravest and best of them all, led them.");
					answer3 = new TextObject("{=kYtRsxSL}We fought the Vlandians. We won, but there was a great slaughter. My husband's horse was slain and he was ridden down, though he died amid a pile of Vlandian dead. Elsewhere on the field, the Emperor was having his head hewn off with a Sturgian axe, and thus was in no position to pay us.");
					answer4 = new TextObject("{=i5OOargr}Such are the fortunes of war. But what came afterwards... When word spread of what happened to our menfolk, the other clans - the Arkits in particular - knew we were weak. Our herds were raided. Anyone who protested was killed. Monchug did little to stop it. It taught us that valor will get you killed, but treachery will make you rich.[if:convo_angry]");
				}
				else if (item.Key.StringId == "lord_6_16")
				{
					answer = new TextObject("{=NGbgfPoP}I was there. Many of the Khuzaits went. Mostly the Khergit clan, who were hungry for glory, but I was also young and hungry for glory so I went along as well.[ib:demure2][if:convo_bored]");
					answer2 = new TextObject("{=kw9sltab}The Battanians had planned an ambush up in a wooded pass for the imperial vanguard, then the Vlandians and Sturgians were to come sweeping down on their flanks in the battle. Our scouts found the Battanian ambush, but Neretzes did not listen, and blundered into it anyway.");
					answer3 = new TextObject("{=Lep5YKdt}While Neretzes' vanguard was getting slaughtered, we met the Vlandians. But the Vlandians brought lots of crossbowmen, and our horse archers took heavy losses. Eventually the armored imperial cataphracts showed up, and rolled over the crossbows. But we were caught in a melee with the Vlandian knights, and that was where things got bloody.");
					answer4 = new TextObject("{=4Baa38ra}We won, barely, with the help of the imperials, but the Khergits were mauled. Since then, the Khergits have been rather weak - and you know what happens to the weak. Still, no one told them to put all their eggs in one basket like that.[if:convo_bored2]");
				}
				else if (item.Key.StringId == "lord_5_1")
				{
					answer = new TextObject("{=Y6OJHdMH}I am a busy man, but there is always time to talk about the blessed battle of Pendraic.[if:convo_mocking_teasing][ib:confident3]");
					answer2 = new TextObject("{=bmitHhDt}Our dear old beloved King Aeril, a wonderful man but with a heart perhaps just a might too tender, did not wish for us to go off to war. But then he disappeared and I, his son-in-law, ascended to the kingship. The clans cried out for war! They had a hundred years of crimes against them to avenge. I, a father to my people, gave them what they wanted.[if:convo_calm_friendly]");
					answer3 = new TextObject("{=XEuhxpCX}Now, the Empire uses tricks and traps in war. No Battanian fears to meet an imperial soldier, man-to-man, but we thought it would be a good laugh to use their tricks against them. So we laid an ambush, on both sides of a wooded pass, and wouldn't you know? They marched right into it.[if:convo_happy]");
					answer4 = new TextObject("{=aMBoh7gL}They turned and twisted as our arrows rained down upon them, like fish going frantic in a pond as you draw the net tighter. Then, when they were greatly discomfited, we took up our falxes and swords and reaped the harvest. Oh, there was some unpleasantness later with the Sturgians, about the spoils of war, but what a grand old day it was![if:convo_relaxed_happy]");
				}
				else if (item.Key.StringId == "lord_5_3")
				{
					answer = new TextObject("{=4Oc6MEvR}Ah... For any son of Battania, there will be no prouder moment in his life than that day. Any true son of Battania, anyway.[if:convo_calm_friendly][ib:hip]");
					answer2 = new TextObject("{=WUa1gnkS}Look, right before the battle, our high king, Aeril, disappears. And his adopted son Caladog becomes king. That sets tongues to wagging, you know? But let me tell you - old Aeril could never in his life have won such a victory as did Caladog, that day.");
					answer3 = new TextObject("{=2DZOCsy5}We waited for them, like wolves in the wood, as their vanguard came up the winding road. They came without archers to protect them. Caladog blew his horn, and our bowmen fired on them from all sides. They turned their shields one way, and were hit from the other. A glorious thing to watch...[if:convo_huge_smile]");
					answer4 = new TextObject("{=x3KpQouT}When they were all good and addled like frightened sheep, running this way and that, the rest of us warriors descended upon them with our falxes and swords. I cleaved this way and that. I took 12 heads, and mine was far from the greatest catch. Ah, the grandchildren tire of me telling this story...[if:convo_calm_friendly]");
				}
				else if (item.Key.StringId == "lord_5_5")
				{
					answer = new TextObject("{=SFJFIaAD}Well... King Caladog's great victory... Who would dare say anything to tarnish its shine?[if:convo_undecided_open][ib:closed]");
					answer2 = new TextObject("{=T7ye4wUT}King Aeril disappeared while hunting, and Caladog becomes king. He leads the tribes to war. Oh, we were eager enough, even though Aeril had made a truce with the Emperor, sealed by oaths. When we were dazzled with the prospect of vengeance, who cares about our sacred word and honor?[ib:confident]");
					answer3 = new TextObject("{=vjOHmHOH}The ambush... Masterfully planned and executed, that none can deny. But I will also not deny that the Sturgians fought the main body of the imperial forces, and the Vlandians fought their famous cavalry, so I don't think the greatest glory went to the sons of Battania.[if:convo_contemptuous]");
					answer4 = new TextObject("{=kJxPtbHo}At the end of the day, what have we gained? The Sturgians hate us worse than ever. The Vlandians too. The Empire, I suppose, is shattered. What can I say... I believe that wars should have a goal. But I am a minority, it seems, among our people.[ib:closed]");
				}
				else if (item.Key.StringId == "lord_4_1")
				{
					answer = new TextObject("{=0IeByuam}It was a victory, of the kind that is almost as bad as a defeat.[if:convo_dismayed][ib:confident2]");
					answer2 = new TextObject("{=vaLehwRl}We had given an oath to the Empire, to join them if attacked. It seemed clear to me that we should have honored our oath, that the Battanians and Sturgians were aggressors, but, there is always room to argue details. Ultimately our barons did not wish to fight with the Empire, so they resisted coming to its help.[if:convo_undecided_closed]");
					answer3 = new TextObject("{=6qgXAcZB}Neretzes, when he heard we were hesitating, sent us a message calling us cowards and traitors. And you say that to a Vlandian noble at your peril. Neretzes should have known what he was doing. We joined the Sturgians.[if:convo_undecided_closed]");
					answer4 = new TextObject("{=xjk4hNXD}I did not fight in the battle. I stood on a hill telling my commanders where to go and who to attack. And we did rather well, I think you've heard. Still, we took losses - heavy losses, and gained little. And for this the barons blamed me, even though it was their idea to fight. I learned that day that a king should always lead, never follow. But it was a bitter lesson.[if:convo_contemptuous][ib:closed]");
				}
				else if (item.Key.StringId == "lord_4_16")
				{
					answer = new TextObject("{=zRZd90cZ}I was there. I was just a young squire then.");
					answer2 = new TextObject("{=sejEf69A}I have heard no sweeter music than the thunder of our hooves as we bore down on the Aserai rabble. We fell on them like a falcon plunges upon a rabbit.[if:convo_bemused]");
					answer3 = new TextObject("{=Og8nep1w}They had overextended themselves, chasing the imperial archers. Light foot before our knights - there was no contest. Let me tell you something - nine-tenths of victory is recognizing when your enemy has made a mistake. The rash perish as swiftly as the weak, and deserve it just as much.[ib:hip]");
					answer4 = new TextObject("{=WN1tHPnB}We should have gone on to seize all the western empire. If Derthert had any manhood, we'd have done so. But his heart was never in the war. He believed he'd broken his oath to the Empire by helping the Sturgians, and it gnawed at him. He'd have made a fine lackey. Instead he's our king.[if:convo_contemptuous]");
				}
				else if (item.Key.StringId == "lord_4_5")
				{
					answer = new TextObject("{=9sAGLK6R}Yes. I was tasked by Derthert with command of the crossbowmen.[if:convo_grave][ib:confident3]");
					answer2 = new TextObject("{=duD8SJDb}Our knights spotted a mass of Aserai light infantry spread across a valley, and charged them. They weren't ready to meet the onslaught, and were routed. But then a bunch of Khuzaits showed up and kept their distance, giving the knights a hard time, shooting their horses out from under them. That's when we showed up.");
					answer3 = new TextObject("{=iQeMllr8}When it was just us fighting the horse archers, we were winning.. A man on foot can shoot as well or better than a man on horseback, all things being equal, and there were a lot more of us. So they started to go down and galloped off. The knights of course pursued, and that's when the problems started.");
					answer4 = new TextObject("{=BleybhsD}Imperial cataphracts showed up, armored head to foot and their horses too, so they just ignored our shooting and tore right through us. I was swept away in the retreat and saw no more of the battle. King Derthert had a good enough plan but the barons - Ingalther, Aldric, that lot - ignored him, as they always do.");
				}
				else if (item.Key.StringId == "lord_3_1")
				{
					answer = new TextObject("{=BstbQV5Z}It was a tragedy that gnawed at the roots of all the great families of Calradia, even ours, so far away from the battle.[if:convo_annoyed][ib:normal2]");
					answer2 = new TextObject("{=aGX3L8mV}We heard that the Empire was making war on the Sturgians, or maybe it was the other way around. I thought that we had no stake in this quarrel but Nimr, a fiery young hero from the Bani Sarran, asked me for permission to take some young warriors, eager for glory.");
					answer3 = new TextObject("{=Zz6IvIH9}The Empire had left us alone for a while, and Neretzes was offering silver for men, so I thought, “Why not? Let them help the Empire.” Ah, I should have known. The best course with wars is to have as little to do with them as you possibly can.");
					answer4 = new TextObject("{=2NHnVb9b}So Nimr went, and fought, and won glory, but also got a number of men killed, especially those of the Banu Qild. And he became boastful, and arrogant. And then... Well, that is the beginning of the great feud between Sarranis and Qildis, but the rest of the story I should perhaps leave for someone else.[ib:closed][if:convo_undecided_closed]");
				}
				else if (item.Key.StringId == "lord_3_3")
				{
					answer = new TextObject("{=XIIISsIg}There was never a prouder moment for the Bani Sarran.[if:convo_calm_friendly][ib:hip]");
					answer2 = new TextObject("{=dWVsS3D8}The bravest and most valiant son of our clan, Nimr, led off a large group of Aserai warriors to fight for the Empire, for gold and glory. I went with them. When we saw the Battanian archers come down from the hills, Nimr was ready. He gave the word: we held our shields over our heads as the arrows rained down, then threw our javelins and charged. We cut them down.");
					answer3 = new TextObject("{=EOSyvdFG}Then the Vlandian knights came. We were attacked on two sides and the Emperor, who could have sent men to save us, took his time. Perhaps he wanted the best of the Aserai to die, lest we become too powerful later. But that betrayal was nothing compared to what we received from our fellow Aserai of the Banu Qild.");
					answer4 = new TextObject("{=Xg6jhfPt}Nimr returned, in well-deserved glory. A daughter of the Banu Qild took an interest in him, and they had a secret affair, as the youth sometimes do. As heroes do. But Nimr's acts wounded the Qildi's pride. They kidnapped him, slew him and hung him in a cage in their market. We will forgive the Empire and the Vlandians. The Qildis... better not ask me that.[if:convo_contemptuous][ib:confident]");
				}
				else if (item.Key.StringId == "lord_3_5")
				{
					answer = new TextObject("{=D4MuSwRB}A sad day for the Banu Qild. But we had our vengeance.[if:convo_stern][ib:hip]");
					answer2 = new TextObject("{=Qb0wsarP}There was a warrior named Nimr, of the Bani Sarran. He was brave, but arrogant. Of course the young people loved him. He wanted to lead men to fight with the Empire, and though there was no gain for us, Unqid let him. Unqid can be weak sometimes.[if:convo_thinking]");
					answer3 = new TextObject("{=flkmzKDz}Many Qildi youth went with him. They died in their hundreds. And there was no gain - except for Nimr, who for some reason people considered a hero. It was despicable how they fawned on him.[if:convo_annoyed]");
					answer4 = new TextObject("{=SF7dqoEe}Of course Nimr's arrogance doubled, and doubled again. And then he dealt us a great insult. I will not say what that insult was, because it no longer exists. We wiped it out. In the traditional way. You may ask someone else about that.");
				}
				else if (item.Key.StringId == "lord_2_1")
				{
					answer = new TextObject("{=lIArdHty}Yes. The day my father died, thanks to Battanian treachery.[if:convo_annoyed][ib:convo_hip2]");
					answer2 = new TextObject("{=RtryETdA}When they pledged to support us in the battle, we believed they would stand with us in the shield wall, like men. But of course this is not the Battanian way. They sprung some woodland trickery up in the hills, killed off Neretzes' vanguard, and no doubt spent the rest of the battle whooping and boasting and chopping the heads off of men who were already dead.[if:convo_angry]");
					answer3 = new TextObject("{=flGyoqa6}It was Sturgians who met Neretzes' guard face-to-face. My father ordered me to stay back as he led them into battle, but he was at their head. He forced them back, then they broke and ran for the shelter of their camp. We went and attacked their ramparts, and broke them, but my father was hit by an imperial mace at the moment of his triumph and died.");
					answer4 = new TextObject("{=Ri6NmpEE}I will never forget when a messenger ran to tell me that my father was dead. But I knew I must swallow my grief, because now I was king. I rode down into the ruins of the imperial camp to take their banner as a trophy, my inheritance won by my father and passed down to me. Oh, some of the boyars were insubordinate. But I have since showed them that I am master.[if:convo_contemptuous]");
				}
				else if (item.Key.StringId == "lord_2_3")
				{
					answer = new TextObject("{=X3pDRmjK}A victory, won by my father, claimed by Raganvad.[if:convo_annoyed][ib:warrior2]");
					answer2 = new TextObject("{=scjogyUD}Old king Vadinslav was brave enough. He led us all into battle. I stood at my father's side as we faced the imperials eye-to-eye over the tops of our shields. It was like any battle where shield walls meet - thrust and push, struggling to stay on your feet, but you can't really describe it. Let's just say it's the kind of battle that Sturgians usually win.[ib:confident2]");
					answer3 = new TextObject("{=sJoO1JzN}When the imperials had had enough of us they broke and ran for the ramparts. There they threw darts and rocks and their cursed fire. We had to go up ladders, one by one. Vadinslav was hit by a mace and went down; my father then went up, cleaving as he went, and rallied us and led us to victory.");
					answer4 = new TextObject("{=sSaZVD2e}My father took the imperial dragon banner from dead Neretzes' hands - it's a famous story - and but then the little prince Raganvad tried to claim it. My father broke it over his knee, threw it at him, and told him to get his own toys to play with. Hah! It was a good, good day.[if:convo_huge_smile]");
				}
				else if (item.Key.StringId == "lord_2_5")
				{
					answer = new TextObject("{=jGhWOnjx}Yes. It was madness. The greatest blow struck against the Empire in a lifetime, and we squandered it squabbling among ourselves about a flag.[if:convo_stern][ib:closed2]");
					answer2 = new TextObject("{=ZkSthA9d}They say Olek the Old had pried the dragon banner out of the Emperor's dead hands. But then Prince Raganvad, who had not so much as drawn his sword in the battle, claimed it as a trophy. Olek, who was covered in his enemies' blood, laughed at Raganvad and told him to go find his own toy to play with. Raganvad struck him, so Olek broke the banner staff over his knee and threw it in his prince's face.");
					answer3 = new TextObject("{=3BjIb3CG}Or perhaps it was just Raganvad. He was stewing in his anger when up comes the Battanian king, Caladog. The Battanians had taken their time stripping the bodies of the imperial vanguard and the Sturgians were angry at them, so Raganvad called him a coward. Caladog sneers at him and walks off.");
					answer4 = new TextObject("{=ez4rbGmx}Insults his most powerful vassal and then insults his most valued ally. A fine day's work, wouldn't you say? But he has grown wiser since, though no more pleasant to spend time with.[ib:demure2]");
				}
				else if (item.Key.StringId == "lord_1_1")
				{
					answer = new TextObject("{=yRAl8YRL}Yes. I was a junior officer on Neretzes' staff. People say much about the battle that betrays a lack of understanding, of Neretzes and of the circumstances he faced.[if:convo_undecided_closed][ib:closed]");
					answer2 = new TextObject("{=bdbZLJf3}Neretzes had an obligation to avenge the Battanian attacks on our land. He marched out, with all the forces he could gather. The Vlandians betrayed us, but that's what you expect from honorless barbarians. Fortune favored the enemy. What matters is that we did what honor required.[if:convo_thinking]");
					answer3 = new TextObject("{=CRmUZvaW}Perhaps Neretzes was rash, sending our infantry up into the hills to storm the Battanian fort. But he thought he could grab the pass quickly, before the enemy had time to reinforce it. If he had made the other wager and that turned out to be wrong, people would say he was hesitant.[ib:demure2]");
					answer4 = new TextObject("{=wajioDaK}I stayed with Neretzes until we were forced back to our camp by the Sturgian infantry, and then fought on the battlements. Eventually we could hold them no longer. I did not see what happened to Neretzes or to our banner. Arenicos got us out of there, and got us home. I did not respect Arenicos before, but that day I saw he was worthy to be Emperor.");
				}
				else if (item.Key.StringId == "lord_1_5")
				{
					answer = new TextObject("{=HL9lzfjY}Of course I can. You know my name, whose son I am.[if:convo_beaten][ib:closed]");
					answer2 = new TextObject("{=iQrwsvb1}We had no choice but to go to war. Anyone who tells you they would have done otherwise is either a liar or a coward or both. The Sturgians attacked us, and needed to be chastised. We lost an army and a banner. But we did not lose our honor, and without honor the Empire would be finished.");
					answer3 = new TextObject("{=aYozMy3Z}We lost because the Vlandians broke their oaths, and fought us when they should have fought with us. I was given command of the cataphracts, and we easily crushed their crossbowmen. Their knights gave us more trouble. Meanwhile, the Sturgian infantry came down and attacked our main force. That's where my father fell. ");
					answer4 = new TextObject("{=cYibaPhn}The barbarians just kept coming and coming. I fought my way out with some loyal men, and made my way back to the capital. But I found that Arenicos had got there before me, and had himself declared Emperor. He always was a cunning operator.");
				}
				else if (item.Key.StringId == "lord_1_7")
				{
					answer = new TextObject("{=kJwV17Jx}Yes. We will never forget that day... The day we learned that the old men who claimed they had the right to rule us were doddering incompetents.[if:convo_stern][ib:closed]");
					answer2 = new TextObject("{=Os79KwFa}I was with the vanguard. Neretzes apparently knew that the Battanians had planned an ambush - the Khuzait scouts had told him. But he never bothered to inform us. So up we went, along a lovely wooded stream, until the Battanian arrows started whooshing in from all sides.[if:convo_bored2][ib:normal2]");
					answer3 = new TextObject("{=AGIPUYvy}We had our shields but you can only point them in one direction at once. So we started to drop, one by one, until the Battanian falxmen came screaming out of the trees. Ordinarily they'd be very vulnerable to archers, but, well, old Neretzes hadn't thought to send any along with us. So they came upon us, chopping and slashing, and we fought until we broke.");
					answer4 = new TextObject("{=4IS4Lou7}I ran too. And any man who tells you he wouldn't, in those circumstances, is a liar. When I was sitting in the cold woods later that night, hiding with other fugitives, listening to the barbarians whoop and holler as they chopped off heads as trophies - I promise them that no Calradian soldier should again be led into battle by an emperor who knows so little of war.[if:convo_thinking][ib:hip]");
				}
				else if (item.Key.StringId == "lord_1_14")
				{
					answer = new TextObject("{=sn3Eabme}Of course. I did not witness the battle, but my husband Arenicos spoke frequently of it.[if:convo_calm_friendly]");
					answer2 = new TextObject("{=AhWyBEhd}He was one of the emperor's trusted commanders. He could not stop Neretzes from marching to defeat, but he managed to salvage something from the disaster. When the Sturgians came over our barricades, he managed to lead a group of Neretzes' guardsmen out the back.");
					answer3 = new TextObject("{=kdR3CeL8}My husband's small force held together, and were joined by stragglers and fugitives. He described the march back - no food, little water, marching day and night to keep ahead of the enemy's outriders. But they survived - the only organized imperial force to do so.");
					answer4 = new TextObject("{=9aYCfBVz}The city was in a state of panic after hearing rumors of what happened. Arenicos kept things from descending into chaos. When it came time for the Senate to choose the next Emperor, there was no question that it should be him. I loved him before as a man, but that day learned to love him as something more: what a gift he was to the people of Calradia![ib:demure2]");
				}
				else
				{
					Debug.FailedAssert("Unable to set notable dialogue!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\StoryMode\\Quests\\FirstPhase\\BannerInvestigationQuest.cs", "SetNobleDialogs", 579);
					answer = null;
					answer2 = null;
					answer3 = null;
					answer4 = null;
				}
				CreateNobleDialog(item.Key, answer, answer2, answer3, answer4);
			}
		}
	}

	private void CreateNobleDialog(Hero noble, TextObject answer1, TextObject answer2, TextObject answer3, TextObject answer4)
	{
		DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("hero_main_options", 150).BeginPlayerOptions().PlayerSpecialOption(new TextObject("{=6UIa4784}Can you tell me anything about the battle of Pendraic?"))
			.Condition(() => talk_with_quest_noble_condition() && Hero.OneToOneConversationHero == noble)
			.NpcLine(answer1)
			.NpcLine(answer2)
			.NpcLine(answer3)
			.NpcLine(answer4);
		if (_noblesToTalk.Count((KeyValuePair<Hero, bool> kvp) => kvp.Value) >= 9)
		{
			dialogFlow.NpcLine(new TextObject("{=p4qJ4KSm}If you want more information, there are two people you might try to speak to. {IMPERIAL_MENTOR.NAME} worked as a sort of unofficial spymaster for Neretzes. She lives near {IMPERIAL_MENTOR_SETTLEMENT}."));
			dialogFlow.Condition(talk_about_mentors_condition);
			dialogFlow.NpcLine(new TextObject("{=q80FFaBj}Then there is {ANTI_IMPERIAL_MENTOR.NAME}, who was his bodyguard. He's supposed to be near {ANTI_IMPERIAL_MENTOR_SETTLEMENT} - though I hear he's changed quite a bit since then."));
		}
		dialogFlow.PlayerLine(new TextObject("{=ShG19Xhi}Thank you..."));
		dialogFlow.Consequence(talk_with_quest_noble_consequence);
		dialogFlow.EndPlayerOptions();
		dialogFlow.GotoDialogState("lord_pretalk");
		Campaign.Current.ConversationManager.AddDialogFlow(dialogFlow, this);
	}

	private bool talk_about_mentors_condition()
	{
		StringHelpers.SetCharacterProperties("IMPERIAL_MENTOR", StoryModeHeroes.ImperialMentor.CharacterObject);
		StringHelpers.SetCharacterProperties("ANTI_IMPERIAL_MENTOR", StoryModeHeroes.AntiImperialMentor.CharacterObject);
		MBTextManager.SetTextVariable("IMPERIAL_MENTOR_SETTLEMENT", StoryModeManager.Current.MainStoryLine.ImperialMentorSettlement.EncyclopediaLinkWithName);
		MBTextManager.SetTextVariable("ANTI_IMPERIAL_MENTOR_SETTLEMENT", StoryModeManager.Current.MainStoryLine.AntiImperialMentorSettlement.EncyclopediaLinkWithName);
		return true;
	}

	private bool talk_with_quest_noble_condition()
	{
		if (Hero.OneToOneConversationHero != null && _battleSummarized && _noblesToTalk.ContainsKey(Hero.OneToOneConversationHero))
		{
			return !_noblesToTalk[Hero.OneToOneConversationHero];
		}
		return false;
	}

	private void talk_with_quest_noble_consequence()
	{
		_noblesToTalk[Hero.OneToOneConversationHero] = true;
		RemoveTrackedObject(Hero.OneToOneConversationHero);
		if (Hero.OneToOneConversationHero.PartyBelongedTo != null)
		{
			RemoveTrackedObject(Hero.OneToOneConversationHero.PartyBelongedTo);
		}
		_talkedNotablesQuestLog.UpdateCurrentProgress(_noblesToTalk.Count((KeyValuePair<Hero, bool> n) => n.Value));
		TextObject textObject = new TextObject("{=DQzEgUzu}You talked with {HERO.LINK} and got some valuable information that may help you understand the artifact.");
		StringHelpers.SetCharacterProperties("HERO", CharacterObject.OneToOneConversationCharacter, textObject);
		AddLog(textObject);
		if (_talkedNotablesQuestLog.CurrentProgress == _talkedNotablesQuestLog.Range)
		{
			CompleteQuestWithSuccess();
		}
		else
		{
			Campaign.Current.ConversationManager.ConversationEndOneShot += UpdateAllNoblesDead;
		}
	}

	private void OnPartyRemoved(PartyBase party)
	{
		if (party.IsMobile && IsTracked(party.MobileParty))
		{
			RemoveTrackedObject(party.MobileParty);
		}
	}

	private void OnPartySpawned(MobileParty spawnedParty)
	{
		if (spawnedParty.IsLordParty && spawnedParty.LeaderHero != null && _noblesToTalk.ContainsKey(spawnedParty.LeaderHero) && !_noblesToTalk[spawnedParty.LeaderHero])
		{
			AddTrackedObject(spawnedParty);
		}
	}

	protected override void OnCompleteWithSuccess()
	{
		_noblesToTalk.Clear();
		AddLog(_endQuestLog);
	}

	internal static void AutoGeneratedStaticCollectObjectsBannerInvestigationQuest(object o, List<object> collectedObjects)
	{
		((BannerInvestigationQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		collectedObjects.Add(_noblesToTalk);
		collectedObjects.Add(_talkedNotablesQuestLog);
	}

	internal static object AutoGeneratedGetMemberValue_noblesToTalk(object o)
	{
		return ((BannerInvestigationQuest)o)._noblesToTalk;
	}

	internal static object AutoGeneratedGetMemberValue_allNoblesDead(object o)
	{
		return ((BannerInvestigationQuest)o)._allNoblesDead;
	}

	internal static object AutoGeneratedGetMemberValue_battleSummarized(object o)
	{
		return ((BannerInvestigationQuest)o)._battleSummarized;
	}

	internal static object AutoGeneratedGetMemberValue_talkedNotablesQuestLog(object o)
	{
		return ((BannerInvestigationQuest)o)._talkedNotablesQuestLog;
	}
}
