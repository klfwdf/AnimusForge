using System;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Engine;

namespace SandBox.CampaignBehaviors
{
	// Token: 0x020000D1 RID: 209
	public class ConversationAnimationToolCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x0600097D RID: 2429 RVA: 0x0004770C File Offset: 0x0004590C
		public override void RegisterEvents()
		{
			CampaignEvents.TickEvent.AddNonSerializedListener(this, new Action<float>(this.Tick));
		}

		// Token: 0x0600097E RID: 2430 RVA: 0x00047725 File Offset: 0x00045925
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x0600097F RID: 2431 RVA: 0x00047728 File Offset: 0x00045928
		private void Tick(float dt)
		{
			if (ConversationAnimationToolCampaignBehavior._isToolEnabled)
			{
				ConversationAnimationToolCampaignBehavior.StartImGUIWindow("Conversation Animation Test Tool");
				ConversationAnimationToolCampaignBehavior.ImGUITextArea("Character Type:", false, false);
				ConversationAnimationToolCampaignBehavior.ImGUITextArea("0 for noble, 1 for notable, 2 for companion, 3 for troop", false, false);
				ConversationAnimationToolCampaignBehavior.ImGUIIntegerField("Enter character type: ", ref ConversationAnimationToolCampaignBehavior._characterType, false, false);
				ConversationAnimationToolCampaignBehavior.Separator();
				ConversationAnimationToolCampaignBehavior.ImGUITextArea("Character State:", false, false);
				ConversationAnimationToolCampaignBehavior.ImGUITextArea("0 for active, 1 for prisoner", false, false);
				ConversationAnimationToolCampaignBehavior.ImGUIIntegerField("Enter character state: ", ref ConversationAnimationToolCampaignBehavior._characterState, false, false);
				ConversationAnimationToolCampaignBehavior.Separator();
				ConversationAnimationToolCampaignBehavior.ImGUITextArea("Character Gender:", false, false);
				ConversationAnimationToolCampaignBehavior.ImGUITextArea("0 for male, 1 for female", false, false);
				ConversationAnimationToolCampaignBehavior.ImGUIIntegerField("Enter character gender: ", ref ConversationAnimationToolCampaignBehavior._characterGender, false, false);
				ConversationAnimationToolCampaignBehavior.Separator();
				ConversationAnimationToolCampaignBehavior.ImGUITextArea("Character Age:", false, false);
				ConversationAnimationToolCampaignBehavior.ImGUITextArea("Enter a custom age or leave -1 for not changing the age value", false, false);
				ConversationAnimationToolCampaignBehavior.ImGUIIntegerField("Enter character age: ", ref ConversationAnimationToolCampaignBehavior._characterAge, false, false);
				ConversationAnimationToolCampaignBehavior.Separator();
				ConversationAnimationToolCampaignBehavior.ImGUITextArea("Character Wounded State:", false, false);
				ConversationAnimationToolCampaignBehavior.ImGUITextArea("Change to 1 to change character state to wounded", false, false);
				ConversationAnimationToolCampaignBehavior.ImGUIIntegerField("Enter character wounded state: ", ref ConversationAnimationToolCampaignBehavior._characterWoundedState, false, false);
				ConversationAnimationToolCampaignBehavior.Separator();
				ConversationAnimationToolCampaignBehavior.ImGUITextArea("Character Equipment Type:", false, false);
				ConversationAnimationToolCampaignBehavior.ImGUITextArea("Change to 1 to change to equipment to civilian, default equipment is battle", false, false);
				ConversationAnimationToolCampaignBehavior.ImGUIIntegerField("Enter equipment type: ", ref ConversationAnimationToolCampaignBehavior._equipmentType, false, false);
				ConversationAnimationToolCampaignBehavior.Separator();
				ConversationAnimationToolCampaignBehavior.ImGUITextArea("Character Relation With Main Hero:", false, false);
				ConversationAnimationToolCampaignBehavior.ImGUITextArea("Leave -1 for no change, 0 for enemy, 1 for neutral, 2 for friend", false, false);
				ConversationAnimationToolCampaignBehavior.ImGUIIntegerField("Enter relation type: ", ref ConversationAnimationToolCampaignBehavior._relationType, false, false);
				ConversationAnimationToolCampaignBehavior.Separator();
				ConversationAnimationToolCampaignBehavior.ImGUITextArea("Character Persona Type:", false, false);
				ConversationAnimationToolCampaignBehavior.ImGUITextArea("Leave -1 for no change, 0 for curt, 1 for earnest, 2 for ironic, 3 for softspoken", false, false);
				ConversationAnimationToolCampaignBehavior.ImGUIIntegerField("Enter persona type: ", ref ConversationAnimationToolCampaignBehavior._personaType, false, false);
				ConversationAnimationToolCampaignBehavior.Separator();
				if (ConversationAnimationToolCampaignBehavior.ImGUIButton(" Start Conversation ", true))
				{
					ConversationAnimationToolCampaignBehavior.StartConversation();
				}
				ConversationAnimationToolCampaignBehavior.EndImGUIWindow();
			}
		}

		// Token: 0x06000980 RID: 2432 RVA: 0x000478D0 File Offset: 0x00045AD0
		public static void CloseConversationAnimationTool()
		{
			ConversationAnimationToolCampaignBehavior._isToolEnabled = false;
			ConversationAnimationToolCampaignBehavior._characterType = -1;
			ConversationAnimationToolCampaignBehavior._characterState = -1;
			ConversationAnimationToolCampaignBehavior._characterGender = -1;
			ConversationAnimationToolCampaignBehavior._characterAge = -1;
			ConversationAnimationToolCampaignBehavior._characterWoundedState = -1;
			ConversationAnimationToolCampaignBehavior._equipmentType = -1;
			ConversationAnimationToolCampaignBehavior._relationType = -1;
			ConversationAnimationToolCampaignBehavior._personaType = -1;
		}

		// Token: 0x06000981 RID: 2433 RVA: 0x00047908 File Offset: 0x00045B08
		private static void StartConversation()
		{
			bool flag = true;
			bool flag2 = true;
			Occupation occupation = Occupation.NotAssigned;
			switch (ConversationAnimationToolCampaignBehavior._characterType)
			{
			case 0:
				occupation = Occupation.Lord;
				break;
			case 1:
				occupation = Occupation.Merchant;
				break;
			case 2:
				occupation = Occupation.Wanderer;
				break;
			case 3:
				occupation = Occupation.Soldier;
				flag2 = false;
				break;
			default:
				flag = false;
				break;
			}
			if (!flag)
			{
				return;
			}
			bool flag3 = false;
			bool flag4 = false;
			if (ConversationAnimationToolCampaignBehavior._characterState == 0)
			{
				flag3 = true;
			}
			else if (ConversationAnimationToolCampaignBehavior._characterState == 1)
			{
				flag4 = true;
			}
			else
			{
				flag = false;
			}
			if (!flag)
			{
				return;
			}
			bool flag5 = false;
			if (ConversationAnimationToolCampaignBehavior._characterGender == 1)
			{
				flag5 = true;
			}
			else if (ConversationAnimationToolCampaignBehavior._characterGender == 0)
			{
				flag5 = false;
			}
			else
			{
				flag = false;
			}
			if (!flag)
			{
				return;
			}
			bool flag6 = false;
			if (ConversationAnimationToolCampaignBehavior._characterAge == -1)
			{
				flag6 = false;
			}
			else if (ConversationAnimationToolCampaignBehavior._characterAge > 0 && ConversationAnimationToolCampaignBehavior._characterAge <= 128)
			{
				flag6 = true;
			}
			else
			{
				flag = false;
			}
			if (!flag)
			{
				return;
			}
			bool flag7 = ConversationAnimationToolCampaignBehavior._characterWoundedState == 1;
			bool flag8 = ConversationAnimationToolCampaignBehavior._equipmentType == 1;
			if (ConversationAnimationToolCampaignBehavior._relationType != 0 && ConversationAnimationToolCampaignBehavior._relationType != 1 && ConversationAnimationToolCampaignBehavior._relationType != 2)
			{
				return;
			}
			CharacterObject characterObject = null;
			if (flag2)
			{
				Hero hero = null;
				foreach (Hero hero2 in Hero.AllAliveHeroes)
				{
					if (hero2 != Hero.MainHero && hero2.Occupation == occupation && hero2.IsFemale == flag5 && (hero2.PartyBelongedTo == null || hero2.PartyBelongedTo.MapEvent == null))
					{
						hero = hero2;
						break;
					}
				}
				if (hero == null)
				{
					hero = HeroCreator.CreateNotable(occupation, null);
				}
				if (flag6)
				{
					hero.SetBirthDay(HeroHelper.GetRandomBirthDayForAge((float)ConversationAnimationToolCampaignBehavior._characterAge));
				}
				if (flag4)
				{
					TakePrisonerAction.Apply(PartyBase.MainParty, hero);
				}
				if (flag7)
				{
					hero.MakeWounded(null, KillCharacterAction.KillCharacterActionDetail.None);
				}
				if (flag3)
				{
					hero.ChangeState(Hero.CharacterStates.Active);
				}
				hero.IsFemale = flag5;
				characterObject = hero.CharacterObject;
			}
			else
			{
				foreach (CharacterObject characterObject2 in CharacterObject.All)
				{
					if (characterObject2.Occupation == occupation && characterObject2.IsFemale == flag5)
					{
						characterObject = characterObject2;
						break;
					}
				}
				if (characterObject == null)
				{
					characterObject = Campaign.Current.ObjectManager.GetObject<CultureObject>("empire").BasicTroop;
				}
			}
			if (characterObject == null)
			{
				return;
			}
			if (characterObject.IsHero && ConversationAnimationToolCampaignBehavior._relationType != -1)
			{
				Hero heroObject = characterObject.HeroObject;
				float relationWithPlayer = heroObject.GetRelationWithPlayer();
				float num = 0f;
				if (ConversationAnimationToolCampaignBehavior._relationType == 0 && !heroObject.IsEnemy(Hero.MainHero))
				{
					num = -relationWithPlayer - 15f;
				}
				else if (ConversationAnimationToolCampaignBehavior._relationType == 1 && !heroObject.IsNeutral(Hero.MainHero))
				{
					num = -relationWithPlayer;
				}
				else if (ConversationAnimationToolCampaignBehavior._relationType == 2 && !heroObject.IsFriend(Hero.MainHero))
				{
					num = -relationWithPlayer + 15f;
				}
				ChangeRelationAction.ApplyPlayerRelation(heroObject, (int)num, true, true);
			}
			CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, null, false, false, false, false, false, false), new ConversationCharacterData(characterObject, null, false, false, false, flag8, flag8, false));
			ConversationAnimationToolCampaignBehavior.CloseConversationAnimationTool();
		}

		// Token: 0x06000982 RID: 2434 RVA: 0x00047C20 File Offset: 0x00045E20
		private static void StartImGUIWindow(string str)
		{
			Imgui.BeginMainThreadScope();
			Imgui.Begin(str);
		}

		// Token: 0x06000983 RID: 2435 RVA: 0x00047C2D File Offset: 0x00045E2D
		private static void ImGUITextArea(string text, bool separatorNeeded, bool onSameLine)
		{
			Imgui.Text(text);
			ConversationAnimationToolCampaignBehavior.ImGUISeparatorSameLineHandler(separatorNeeded, onSameLine);
		}

		// Token: 0x06000984 RID: 2436 RVA: 0x00047C3C File Offset: 0x00045E3C
		private static bool ImGUIButton(string buttonText, bool smallButton)
		{
			if (smallButton)
			{
				return Imgui.SmallButton(buttonText);
			}
			return Imgui.Button(buttonText);
		}

		// Token: 0x06000985 RID: 2437 RVA: 0x00047C4E File Offset: 0x00045E4E
		private static void ImGUIIntegerField(string fieldText, ref int value, bool separatorNeeded, bool onSameLine)
		{
			Imgui.InputInt(fieldText, ref value);
			ConversationAnimationToolCampaignBehavior.ImGUISeparatorSameLineHandler(separatorNeeded, onSameLine);
		}

		// Token: 0x06000986 RID: 2438 RVA: 0x00047C5F File Offset: 0x00045E5F
		private static void ImGUICheckBox(string text, ref bool is_checked, bool separatorNeeded, bool onSameLine)
		{
			Imgui.Checkbox(text, ref is_checked);
			ConversationAnimationToolCampaignBehavior.ImGUISeparatorSameLineHandler(separatorNeeded, onSameLine);
		}

		// Token: 0x06000987 RID: 2439 RVA: 0x00047C70 File Offset: 0x00045E70
		private static void ImGUISeparatorSameLineHandler(bool separatorNeeded, bool onSameLine)
		{
			if (separatorNeeded)
			{
				ConversationAnimationToolCampaignBehavior.Separator();
			}
			if (onSameLine)
			{
				ConversationAnimationToolCampaignBehavior.OnSameLine();
			}
		}

		// Token: 0x06000988 RID: 2440 RVA: 0x00047C82 File Offset: 0x00045E82
		private static void OnSameLine()
		{
			Imgui.SameLine(0f, 0f);
		}

		// Token: 0x06000989 RID: 2441 RVA: 0x00047C93 File Offset: 0x00045E93
		private static void Separator()
		{
			Imgui.Separator();
		}

		// Token: 0x0600098A RID: 2442 RVA: 0x00047C9A File Offset: 0x00045E9A
		private static void EndImGUIWindow()
		{
			Imgui.End();
			Imgui.EndMainThreadScope();
		}

		// Token: 0x04000472 RID: 1138
		private static bool _isToolEnabled = false;

		// Token: 0x04000473 RID: 1139
		private static int _characterType = -1;

		// Token: 0x04000474 RID: 1140
		private static int _characterState = -1;

		// Token: 0x04000475 RID: 1141
		private static int _characterGender = -1;

		// Token: 0x04000476 RID: 1142
		private static int _characterAge = -1;

		// Token: 0x04000477 RID: 1143
		private static int _characterWoundedState = -1;

		// Token: 0x04000478 RID: 1144
		private static int _equipmentType = -1;

		// Token: 0x04000479 RID: 1145
		private static int _relationType = -1;

		// Token: 0x0400047A RID: 1146
		private static int _personaType = -1;
	}
}
