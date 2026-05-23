using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000BF RID: 191
	public class HeroExecutionSceneNotificationData : SceneNotificationData
	{
		// Token: 0x17000562 RID: 1378
		// (get) Token: 0x060013E0 RID: 5088 RVA: 0x0005C197 File Offset: 0x0005A397
		public Hero Executer { get; }

		// Token: 0x17000563 RID: 1379
		// (get) Token: 0x060013E1 RID: 5089 RVA: 0x0005C19F File Offset: 0x0005A39F
		public Hero Victim { get; }

		// Token: 0x17000564 RID: 1380
		// (get) Token: 0x060013E2 RID: 5090 RVA: 0x0005C1A7 File Offset: 0x0005A3A7
		public override bool IsNegativeOptionShown { get; }

		// Token: 0x17000565 RID: 1381
		// (get) Token: 0x060013E3 RID: 5091 RVA: 0x0005C1AF File Offset: 0x0005A3AF
		public override string SceneID
		{
			get
			{
				return "scn_execution_notification";
			}
		}

		// Token: 0x17000566 RID: 1382
		// (get) Token: 0x060013E4 RID: 5092 RVA: 0x0005C1B6 File Offset: 0x0005A3B6
		public override TextObject NegativeText
		{
			get
			{
				return GameTexts.FindText("str_execution_negative_action", null);
			}
		}

		// Token: 0x17000567 RID: 1383
		// (get) Token: 0x060013E5 RID: 5093 RVA: 0x0005C1C3 File Offset: 0x0005A3C3
		public override bool IsAffirmativeOptionShown
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000568 RID: 1384
		// (get) Token: 0x060013E6 RID: 5094 RVA: 0x0005C1C6 File Offset: 0x0005A3C6
		public override TextObject TitleText { get; }

		// Token: 0x17000569 RID: 1385
		// (get) Token: 0x060013E7 RID: 5095 RVA: 0x0005C1CE File Offset: 0x0005A3CE
		public override TextObject AffirmativeText { get; }

		// Token: 0x1700056A RID: 1386
		// (get) Token: 0x060013E8 RID: 5096 RVA: 0x0005C1D6 File Offset: 0x0005A3D6
		public override TextObject AffirmativeTitleText { get; }

		// Token: 0x1700056B RID: 1387
		// (get) Token: 0x060013E9 RID: 5097 RVA: 0x0005C1DE File Offset: 0x0005A3DE
		public override TextObject AffirmativeHintText { get; }

		// Token: 0x1700056C RID: 1388
		// (get) Token: 0x060013EA RID: 5098 RVA: 0x0005C1E6 File Offset: 0x0005A3E6
		public override TextObject AffirmativeHintTextExtended { get; }

		// Token: 0x1700056D RID: 1389
		// (get) Token: 0x060013EB RID: 5099 RVA: 0x0005C1EE File Offset: 0x0005A3EE
		public override TextObject AffirmativeDescriptionText { get; }

		// Token: 0x1700056E RID: 1390
		// (get) Token: 0x060013EC RID: 5100 RVA: 0x0005C1F6 File Offset: 0x0005A3F6
		public override SceneNotificationData.RelevantContextType RelevantContext { get; }

		// Token: 0x060013ED RID: 5101 RVA: 0x0005C200 File Offset: 0x0005A400
		public override SceneNotificationData.SceneNotificationCharacter[] GetSceneNotificationCharacters()
		{
			Equipment equipment = this.Victim.BattleEquipment.Clone(true);
			equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.NumAllWeaponSlots, default(EquipmentElement));
			equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.WeaponItemBeginSlot, default(EquipmentElement));
			equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon1, default(EquipmentElement));
			equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon2, default(EquipmentElement));
			equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon3, default(EquipmentElement));
			equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.ExtraWeaponSlot, default(EquipmentElement));
			ItemObject item = Items.All.FirstOrDefault((ItemObject i) => i.StringId == "execution_axe");
			Equipment equipment2 = this.Executer.BattleEquipment.Clone(true);
			equipment2.AddEquipmentToSlotWithoutAgent(EquipmentIndex.WeaponItemBeginSlot, new EquipmentElement(item, null, null, false));
			equipment2.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon1, default(EquipmentElement));
			equipment2.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon2, default(EquipmentElement));
			equipment2.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon3, default(EquipmentElement));
			equipment2.AddEquipmentToSlotWithoutAgent(EquipmentIndex.ExtraWeaponSlot, default(EquipmentElement));
			return new SceneNotificationData.SceneNotificationCharacter[]
			{
				CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(this.Victim, equipment, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false),
				CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(this.Executer, equipment2, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false)
			};
		}

		// Token: 0x060013EE RID: 5102 RVA: 0x0005C354 File Offset: 0x0005A554
		private HeroExecutionSceneNotificationData(Hero executingHero, Hero dyingHero, TextObject titleText, TextObject affirmativeTitleText, TextObject affirmativeActionText, TextObject affirmativeActionDescriptionText, TextObject affirmativeActionHintText, TextObject affirmativeActionHintExtendedText, bool isNegativeOptionShown, Action onAffirmativeAction, SceneNotificationData.RelevantContextType relevantContextType = SceneNotificationData.RelevantContextType.Any)
		{
			this.Executer = executingHero;
			this.Victim = dyingHero;
			this.TitleText = titleText;
			this.AffirmativeTitleText = affirmativeTitleText;
			this.AffirmativeText = affirmativeActionText;
			this.AffirmativeDescriptionText = affirmativeActionDescriptionText;
			this.AffirmativeHintText = affirmativeActionHintText;
			this.AffirmativeHintTextExtended = affirmativeActionHintExtendedText;
			this.IsNegativeOptionShown = isNegativeOptionShown;
			this.RelevantContext = relevantContextType;
			this._onAffirmativeAction = onAffirmativeAction;
			this._runAffirmativeActionAtClose = false;
		}

		// Token: 0x060013EF RID: 5103 RVA: 0x0005C3C3 File Offset: 0x0005A5C3
		public override void OnCloseAction()
		{
			this.PostponedAffirmativeAction();
		}

		// Token: 0x060013F0 RID: 5104 RVA: 0x0005C3CB File Offset: 0x0005A5CB
		public override void OnAffirmativeAction()
		{
			base.OnAffirmativeAction();
			this._runAffirmativeActionAtClose = true;
		}

		// Token: 0x060013F1 RID: 5105 RVA: 0x0005C3DC File Offset: 0x0005A5DC
		private void PostponedAffirmativeAction()
		{
			if (this._runAffirmativeActionAtClose)
			{
				if (this._onAffirmativeAction != null)
				{
					this._onAffirmativeAction();
				}
				else if (this.Victim != Hero.MainHero)
				{
					if (MobileParty.MainParty.MapEvent != null)
					{
						KillCharacterAction.ApplyByExecutionAfterMapEvent(this.Victim, this.Executer, true, true);
					}
					else
					{
						KillCharacterAction.ApplyByExecution(this.Victim, this.Executer, true, true);
					}
				}
			}
			this._runAffirmativeActionAtClose = false;
		}

		// Token: 0x060013F2 RID: 5106 RVA: 0x0005C450 File Offset: 0x0005A650
		public static HeroExecutionSceneNotificationData CreateForPlayerExecutingHero(Hero dyingHero, Action onAffirmativeAction, SceneNotificationData.RelevantContextType relevantContextType = SceneNotificationData.RelevantContextType.Any, bool showNegativeOption = true)
		{
			GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(CampaignTime.Now));
			GameTexts.SetVariable("YEAR", CampaignTime.Now.GetYear);
			GameTexts.SetVariable("NAME", dyingHero.Name);
			TextObject textObject = GameTexts.FindText("str_execution_positive_action", null);
			textObject.SetCharacterProperties("DYING_HERO", dyingHero.CharacterObject, false);
			return new HeroExecutionSceneNotificationData(Hero.MainHero, dyingHero, GameTexts.FindText("str_executing_prisoner", null), GameTexts.FindText("str_executed_prisoner", null), textObject, GameTexts.FindText("str_execute_prisoner_desc", null), HeroExecutionSceneNotificationData.GetExecuteTroopHintText(dyingHero, false), HeroExecutionSceneNotificationData.GetExecuteTroopHintText(dyingHero, true), showNegativeOption, onAffirmativeAction, relevantContextType);
		}

		// Token: 0x060013F3 RID: 5107 RVA: 0x0005C4F4 File Offset: 0x0005A6F4
		public static HeroExecutionSceneNotificationData CreateForInformingPlayer(Hero executingHero, Hero dyingHero, SceneNotificationData.RelevantContextType relevantContextType = SceneNotificationData.RelevantContextType.Any)
		{
			GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(CampaignTime.Now));
			GameTexts.SetVariable("YEAR", CampaignTime.Now.GetYear);
			GameTexts.SetVariable("NAME", dyingHero.Name);
			TextObject textObject = new TextObject("{=uYjEknNX}{VICTIM.NAME}'s execution by {EXECUTER.NAME}", null);
			textObject.SetCharacterProperties("VICTIM", dyingHero.CharacterObject, false);
			textObject.SetCharacterProperties("EXECUTER", executingHero.CharacterObject, false);
			return new HeroExecutionSceneNotificationData(executingHero, dyingHero, textObject, GameTexts.FindText("str_executed_prisoner", null), GameTexts.FindText("str_proceed", null), null, null, null, false, null, relevantContextType);
		}

		// Token: 0x060013F4 RID: 5108 RVA: 0x0005C590 File Offset: 0x0005A790
		private static TextObject GetExecuteTroopHintText(Hero dyingHero, bool showAll)
		{
			Dictionary<Clan, int> dictionary = new Dictionary<Clan, int>();
			GameTexts.SetVariable("LEFT", new TextObject("{=jxypVgl2}Relation Changes", null));
			string text = GameTexts.FindText("str_LEFT_colon", null).ToString();
			if (dyingHero.Clan != null)
			{
				foreach (Clan clan in Clan.All)
				{
					foreach (Hero hero in clan.Heroes)
					{
						if (!hero.IsHumanPlayerCharacter && hero.IsAlive && hero != dyingHero && (!hero.IsLord || hero.Clan.Leader == hero))
						{
							bool flag;
							int relationChangeForExecutingHero = Campaign.Current.Models.ExecutionRelationModel.GetRelationChangeForExecutingHero(dyingHero, hero, out flag);
							if (relationChangeForExecutingHero != 0)
							{
								if (dictionary.ContainsKey(clan))
								{
									if (relationChangeForExecutingHero < dictionary[clan])
									{
										dictionary[clan] = relationChangeForExecutingHero;
									}
								}
								else
								{
									dictionary.Add(clan, relationChangeForExecutingHero);
								}
							}
						}
					}
				}
				GameTexts.SetVariable("newline", "\n");
				List<KeyValuePair<Clan, int>> list = (from change in dictionary
					orderby change.Value
					select change).ToList<KeyValuePair<Clan, int>>();
				int num = 0;
				foreach (KeyValuePair<Clan, int> keyValuePair in list)
				{
					Clan key = keyValuePair.Key;
					int value = keyValuePair.Value;
					GameTexts.SetVariable("LEFT", key.Name);
					GameTexts.SetVariable("RIGHT", value);
					string content = GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon", null).ToString();
					GameTexts.SetVariable("STR1", text);
					GameTexts.SetVariable("STR2", content);
					text = GameTexts.FindText("str_string_newline_string", null).ToString();
					num++;
					if (!showAll && num == HeroExecutionSceneNotificationData.MaxShownRelationChanges)
					{
						TextObject content2 = new TextObject("{=DPTPuyip}And {NUMBER} more...", null);
						GameTexts.SetVariable("NUMBER", dictionary.Count - num);
						GameTexts.SetVariable("STR1", text);
						GameTexts.SetVariable("STR2", content2);
						text = GameTexts.FindText("str_string_newline_string", null).ToString();
						TextObject textObject = new TextObject("{=u12ocP9f}Hold '{EXTEND_KEY}' for more info.", null);
						textObject.SetTextVariable("EXTEND_KEY", GameTexts.FindText("str_game_key_text", "anyalt"));
						GameTexts.SetVariable("STR1", text);
						GameTexts.SetVariable("STR2", textObject);
						text = GameTexts.FindText("str_string_newline_string", null).ToString();
						break;
					}
				}
				return new TextObject("{=!}" + text, null);
			}
			return TextObject.GetEmpty();
		}

		// Token: 0x04000696 RID: 1686
		private bool _runAffirmativeActionAtClose;

		// Token: 0x04000697 RID: 1687
		private readonly Action _onAffirmativeAction;

		// Token: 0x04000698 RID: 1688
		protected static int MaxShownRelationChanges = 8;
	}
}
