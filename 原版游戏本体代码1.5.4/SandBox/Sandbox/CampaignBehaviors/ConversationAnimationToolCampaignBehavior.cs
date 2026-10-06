using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Engine;

namespace SandBox.CampaignBehaviors;

public class ConversationAnimationToolCampaignBehavior : CampaignBehaviorBase
{
	private static bool _isToolEnabled = false;

	private static int _characterType = -1;

	private static int _characterState = -1;

	private static int _characterGender = -1;

	private static int _characterAge = -1;

	private static int _characterWoundedState = -1;

	private static int _equipmentType = -1;

	private static int _relationType = -1;

	private static int _personaType = -1;

	public override void RegisterEvents()
	{
		CampaignEvents.TickEvent.AddNonSerializedListener(this, Tick);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void Tick(float dt)
	{
		if (_isToolEnabled)
		{
			StartImGUIWindow("Conversation Animation Test Tool");
			ImGUITextArea("Character Type:", separatorNeeded: false, onSameLine: false);
			ImGUITextArea("0 for noble, 1 for notable, 2 for companion, 3 for troop", separatorNeeded: false, onSameLine: false);
			ImGUIIntegerField("Enter character type: ", ref _characterType, separatorNeeded: false, onSameLine: false);
			Separator();
			ImGUITextArea("Character State:", separatorNeeded: false, onSameLine: false);
			ImGUITextArea("0 for active, 1 for prisoner", separatorNeeded: false, onSameLine: false);
			ImGUIIntegerField("Enter character state: ", ref _characterState, separatorNeeded: false, onSameLine: false);
			Separator();
			ImGUITextArea("Character Gender:", separatorNeeded: false, onSameLine: false);
			ImGUITextArea("0 for male, 1 for female", separatorNeeded: false, onSameLine: false);
			ImGUIIntegerField("Enter character gender: ", ref _characterGender, separatorNeeded: false, onSameLine: false);
			Separator();
			ImGUITextArea("Character Age:", separatorNeeded: false, onSameLine: false);
			ImGUITextArea("Enter a custom age or leave -1 for not changing the age value", separatorNeeded: false, onSameLine: false);
			ImGUIIntegerField("Enter character age: ", ref _characterAge, separatorNeeded: false, onSameLine: false);
			Separator();
			ImGUITextArea("Character Wounded State:", separatorNeeded: false, onSameLine: false);
			ImGUITextArea("Change to 1 to change character state to wounded", separatorNeeded: false, onSameLine: false);
			ImGUIIntegerField("Enter character wounded state: ", ref _characterWoundedState, separatorNeeded: false, onSameLine: false);
			Separator();
			ImGUITextArea("Character Equipment Type:", separatorNeeded: false, onSameLine: false);
			ImGUITextArea("Change to 1 to change to equipment to civilian, default equipment is battle", separatorNeeded: false, onSameLine: false);
			ImGUIIntegerField("Enter equipment type: ", ref _equipmentType, separatorNeeded: false, onSameLine: false);
			Separator();
			ImGUITextArea("Character Relation With Main Hero:", separatorNeeded: false, onSameLine: false);
			ImGUITextArea("Leave -1 for no change, 0 for enemy, 1 for neutral, 2 for friend", separatorNeeded: false, onSameLine: false);
			ImGUIIntegerField("Enter relation type: ", ref _relationType, separatorNeeded: false, onSameLine: false);
			Separator();
			ImGUITextArea("Character Persona Type:", separatorNeeded: false, onSameLine: false);
			ImGUITextArea("Leave -1 for no change, 0 for curt, 1 for earnest, 2 for ironic, 3 for softspoken", separatorNeeded: false, onSameLine: false);
			ImGUIIntegerField("Enter persona type: ", ref _personaType, separatorNeeded: false, onSameLine: false);
			Separator();
			if (ImGUIButton(" Start Conversation ", smallButton: true))
			{
				StartConversation();
			}
			EndImGUIWindow();
		}
	}

	public static void CloseConversationAnimationTool()
	{
		_isToolEnabled = false;
		_characterType = -1;
		_characterState = -1;
		_characterGender = -1;
		_characterAge = -1;
		_characterWoundedState = -1;
		_equipmentType = -1;
		_relationType = -1;
		_personaType = -1;
	}

	private static void StartConversation()
	{
		bool flag = true;
		bool flag2 = true;
		Occupation occupation = Occupation.NotAssigned;
		switch (_characterType)
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
		if (_characterState == 0)
		{
			flag3 = true;
		}
		else if (_characterState == 1)
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
		if (_characterGender == 1)
		{
			flag5 = true;
		}
		else if (_characterGender == 0)
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
		if (_characterAge == -1)
		{
			flag6 = false;
		}
		else if (_characterAge > 0 && _characterAge <= 128)
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
		bool flag7 = _characterWoundedState == 1;
		bool flag8 = _equipmentType == 1;
		if (_relationType != 0 && _relationType != 1 && _relationType != 2)
		{
			return;
		}
		CharacterObject characterObject = null;
		if (flag2)
		{
			Hero hero = null;
			foreach (Hero allAliveHero in Hero.AllAliveHeroes)
			{
				if (allAliveHero != Hero.MainHero && allAliveHero.Occupation == occupation && allAliveHero.IsFemale == flag5 && (allAliveHero.PartyBelongedTo == null || allAliveHero.PartyBelongedTo.MapEvent == null))
				{
					hero = allAliveHero;
					break;
				}
			}
			if (hero == null)
			{
				hero = HeroCreator.CreateNotable(occupation);
			}
			if (flag6)
			{
				hero.SetBirthDay(HeroHelper.GetRandomBirthDayForAge(_characterAge));
			}
			if (flag4)
			{
				TakePrisonerAction.Apply(PartyBase.MainParty, hero);
			}
			if (flag7)
			{
				hero.MakeWounded();
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
			foreach (CharacterObject item in CharacterObject.All)
			{
				if (item.Occupation == occupation && item.IsFemale == flag5)
				{
					characterObject = item;
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
		if (characterObject.IsHero && _relationType != -1)
		{
			Hero heroObject = characterObject.HeroObject;
			float relationWithPlayer = heroObject.GetRelationWithPlayer();
			float num = 0f;
			if (_relationType == 0 && !heroObject.IsEnemy(Hero.MainHero))
			{
				num = 0f - relationWithPlayer - 15f;
			}
			else if (_relationType == 1 && !heroObject.IsNeutral(Hero.MainHero))
			{
				num = 0f - relationWithPlayer;
			}
			else if (_relationType == 2 && !heroObject.IsFriend(Hero.MainHero))
			{
				num = 0f - relationWithPlayer + 15f;
			}
			ChangeRelationAction.ApplyPlayerRelation(heroObject, (int)num);
		}
		CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter), new ConversationCharacterData(characterObject, null, noHorse: false, noWeapon: false, spawnAfterFight: false, flag8, flag8));
		CloseConversationAnimationTool();
	}

	private static void StartImGUIWindow(string str)
	{
		Imgui.BeginMainThreadScope();
		Imgui.Begin(str);
	}

	private static void ImGUITextArea(string text, bool separatorNeeded, bool onSameLine)
	{
		Imgui.Text(text);
		ImGUISeparatorSameLineHandler(separatorNeeded, onSameLine);
	}

	private static bool ImGUIButton(string buttonText, bool smallButton)
	{
		if (smallButton)
		{
			return Imgui.SmallButton(buttonText);
		}
		return Imgui.Button(buttonText);
	}

	private static void ImGUIIntegerField(string fieldText, ref int value, bool separatorNeeded, bool onSameLine)
	{
		Imgui.InputInt(fieldText, ref value);
		ImGUISeparatorSameLineHandler(separatorNeeded, onSameLine);
	}

	private static void ImGUICheckBox(string text, ref bool is_checked, bool separatorNeeded, bool onSameLine)
	{
		Imgui.Checkbox(text, ref is_checked);
		ImGUISeparatorSameLineHandler(separatorNeeded, onSameLine);
	}

	private static void ImGUISeparatorSameLineHandler(bool separatorNeeded, bool onSameLine)
	{
		if (separatorNeeded)
		{
			Separator();
		}
		if (onSameLine)
		{
			OnSameLine();
		}
	}

	private static void OnSameLine()
	{
		Imgui.SameLine();
	}

	private static void Separator()
	{
		Imgui.Separator();
	}

	private static void EndImGUIWindow()
	{
		Imgui.End();
		Imgui.EndMainThreadScope();
	}
}
