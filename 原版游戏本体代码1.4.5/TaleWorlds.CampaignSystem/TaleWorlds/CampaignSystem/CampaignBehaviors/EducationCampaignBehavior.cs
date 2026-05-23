using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class EducationCampaignBehavior : CampaignBehaviorBase, IEducationLogic
{
	private enum ChildAgeState : short
	{
		Invalid = -1,
		Year2 = 0,
		Year5 = 1,
		Year8 = 2,
		Year11 = 3,
		Year14 = 4,
		Year16 = 5,
		Count = 6,
		First = 0,
		Last = 5
	}

	private class EducationOption
	{
		public delegate bool EducationOptionConditionDelegate(EducationOption option, List<EducationOption> previousOptions);

		public delegate bool EducationOptionConsequenceDelegate(EducationOption option);

		public readonly EducationOptionConditionDelegate Condition;

		private readonly EducationOptionConsequenceDelegate _consequence;

		public readonly TextObject Title;

		public readonly TextObject Description;

		public readonly TextObject Effect;

		public readonly CharacterAttribute[] Attributes;

		public readonly SkillObject[] Skills;

		public readonly EducationCharacterProperties ChildProperties;

		public readonly EducationCharacterProperties SpecialCharacterProperties;

		public readonly int RandomValue;

		public void OnConsequence(Hero child)
		{
			_consequence?.Invoke(this);
			CharacterAttribute[] attributes = Attributes;
			foreach (CharacterAttribute attrib in attributes)
			{
				child.HeroDeveloper.AddAttribute(attrib, 1, checkUnspentPoints: false);
			}
			SkillObject[] skills = Skills;
			foreach (SkillObject skill in skills)
			{
				child.HeroDeveloper.AddFocus(skill, 1, checkUnspentFocusPoints: false);
				child.HeroDeveloper.ChangeSkillLevel(skill, 15);
			}
		}

		public EducationOption(TextObject title, TextObject description, TextObject effect, EducationOptionConditionDelegate condition, EducationOptionConsequenceDelegate consequence, CharacterAttribute[] attributes, SkillObject[] skills, EducationCharacterProperties childProperties, EducationCharacterProperties specialCharacterProperties = default(EducationCharacterProperties))
		{
			Title = title;
			Description = description;
			Condition = condition;
			_consequence = consequence;
			Attributes = attributes ?? new CharacterAttribute[0];
			Skills = skills ?? new SkillObject[0];
			Effect = GetEffectText(effect ?? TextObject.GetEmpty());
			ChildProperties = childProperties;
			SpecialCharacterProperties = specialCharacterProperties;
			RandomValue = MBRandom.RandomInt(0, int.MaxValue);
		}

		private TextObject GetEffectText(TextObject effect)
		{
			TextObject textObject = new TextObject("{=JfBTbsX2}{EFFECT_DESCRIPTION}{NEW_LINE_1}{SKILL_DESCRIPTION}{NEW_LINE_2}{ATTRIBUTE_DESCRIPTION}");
			TextObject textObject2;
			if (Skills.Length == 1)
			{
				textObject2 = new TextObject("{=I88vSwpb}{SKILL1} gains {NUMBER_FP} Focus Point and {NUMBER_SP} Skill Points.");
				textObject2.SetTextVariable("SKILL1", Skills[0].Name);
			}
			else if (Skills.Length == 2)
			{
				textObject2 = new TextObject("{=bvRVu0fO}{SKILL1} and {SKILL2} gain {NUMBER_FP} Focus Point and {NUMBER_SP} Skill Points.");
				textObject2.SetTextVariable("SKILL1", Skills[0].Name);
				textObject2.SetTextVariable("SKILL2", Skills[1].Name);
			}
			else
			{
				textObject2 = TextObject.GetEmpty();
			}
			TextObject textObject3;
			if (Attributes.Length == 1)
			{
				textObject3 = new TextObject("{=bm2DzxEl}{ATTRIBUTE1} is increased by {NUMBER_AP}.");
				textObject3.SetTextVariable("ATTRIBUTE1", Attributes.ElementAt(0).Name);
			}
			else if (Attributes.Length == 2)
			{
				textObject3 = new TextObject("{=2sQQh02s}{ATTRIBUTE1} and {ATTRIBUTE2} are increased by {NUMBER_AP}.");
				textObject3.SetTextVariable("ATTRIBUTE1", Attributes[0].Name);
				textObject3.SetTextVariable("ATTRIBUTE2", Attributes[1].Name);
			}
			else
			{
				textObject3 = TextObject.GetEmpty();
			}
			if (!TextObject.IsNullOrEmpty(textObject3))
			{
				textObject3.SetTextVariable("NUMBER_AP", 1);
			}
			if (!TextObject.IsNullOrEmpty(textObject2))
			{
				textObject2.SetTextVariable("NUMBER_FP", 1);
				textObject2.SetTextVariable("NUMBER_SP", 15);
			}
			textObject.SetTextVariable("SKILL_DESCRIPTION", textObject2);
			textObject.SetTextVariable("ATTRIBUTE_DESCRIPTION", textObject3);
			textObject.SetTextVariable("EFFECT_DESCRIPTION", effect);
			if (!effect.IsEmpty() && (!textObject2.IsEmpty() || !textObject3.IsEmpty()))
			{
				textObject.SetTextVariable("NEW_LINE_1", "\n");
			}
			else
			{
				textObject.SetTextVariable("NEW_LINE_1", TextObject.GetEmpty());
			}
			if (!textObject2.IsEmpty() && !textObject3.IsEmpty())
			{
				textObject.SetTextVariable("NEW_LINE_2", "\n");
			}
			else
			{
				textObject.SetTextVariable("NEW_LINE_2", TextObject.GetEmpty());
			}
			return textObject;
		}
	}

	private class EducationStage
	{
		private List<List<EducationPage>> _superPages;

		public readonly ChildAgeState Target;

		public int PageCount => _superPages.Count;

		public EducationStage(ChildAgeState targetAge)
		{
			Target = targetAge;
			_superPages = new List<List<EducationPage>>();
		}

		public EducationPage AddPage(int pageIndex, TextObject title, TextObject description, TextObject instruction, EducationCharacterProperties childProperties = default(EducationCharacterProperties), EducationCharacterProperties specialCharacterProperties = default(EducationCharacterProperties), EducationPage.EducationPageConditionDelegate condition = null)
		{
			while (pageIndex >= _superPages.Count)
			{
				_superPages.Add(new List<EducationPage>());
			}
			EducationPage educationPage = new EducationPage(pageIndex.ToString() + ";" + _superPages[pageIndex].Count, title, description, instruction, childProperties, specialCharacterProperties, condition);
			_superPages[pageIndex].Add(educationPage);
			return educationPage;
		}

		private Equipment GetChildEquipmentForOption(Hero child, string optionKey, List<string> previousOptions)
		{
			string[] array = optionKey.Split(new char[1] { ';' });
			if (!int.TryParse(array[0], out var result))
			{
				Debug.FailedAssert("/keys/ isnt set correctly", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\EducationCampaignBehavior.cs", "GetChildEquipmentForOption", 239);
			}
			Equipment equipment = null;
			if (Target == ChildAgeState.Year8)
			{
				equipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>($"child_education_equipments_stage_{(int)Target}_page_0_branch_child_{child.Culture.StringId}")?.DefaultEquipment;
			}
			else if (Target == ChildAgeState.Year16 && result > 0)
			{
				string arg = previousOptions[0].Split(new char[1] { ';' })[2];
				equipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>($"child_education_equipments_stage_{(int)Target}_page_0_branch_{arg}_{child.Culture.StringId}")?.DefaultEquipment;
			}
			else if (Target != ChildAgeState.Year2)
			{
				equipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>($"child_education_equipments_stage_{(int)Target}_page_{array[0]}_branch_{array[2]}_{child.Culture.StringId}")?.DefaultEquipment;
			}
			return equipment ?? MBEquipmentRoster.EmptyEquipment;
		}

		private Equipment GetChildEquipmentForPage(Hero child, EducationPage page, List<string> previousOptions)
		{
			Equipment equipment = null;
			if (Target == ChildAgeState.Year8 && previousOptions.Count == 0)
			{
				equipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>($"child_education_equipments_stage_{(int)Target}_page_0_branch_child_{child.Culture.StringId}")?.DefaultEquipment;
			}
			else if (previousOptions.Count > 0)
			{
				equipment = GetChildEquipmentForOption(child, previousOptions[0], previousOptions);
			}
			else if (Target != ChildAgeState.Year2)
			{
				equipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>($"child_education_equipments_stage_{(int)Target}_page_0_branch_default_{child.Culture.StringId}")?.DefaultEquipment;
			}
			return equipment ?? MBEquipmentRoster.EmptyEquipment;
		}

		private EducationCharacterProperties GetChildPropertiesForOption(Hero child, string optionKey, List<string> previousOptions)
		{
			string[] array = optionKey.Split(new char[1] { ';' });
			EducationOption option = GetOption(optionKey);
			if (!int.TryParse(array[0], out var _))
			{
				Debug.FailedAssert("/keys/ isnt set correctly", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\EducationCampaignBehavior.cs", "GetChildPropertiesForOption", 288);
			}
			Equipment childEquipmentForOption = GetChildEquipmentForOption(child, optionKey, previousOptions);
			return new EducationCharacterProperties(child.CharacterObject, childEquipmentForOption, option.ChildProperties.ActionId, option.ChildProperties.PrefabId, option.ChildProperties.UseOffHand);
		}

		private EducationCharacterProperties GetChildPropertiesForPage(Hero child, EducationPage page, List<string> previousOptions)
		{
			if (previousOptions.Count == 0 || page.ChildProperties != EducationCharacterProperties.Invalid)
			{
				string actionId = page.ChildProperties.ActionId;
				string prefabId = page.ChildProperties.PrefabId;
				bool useOffHand = page.ChildProperties.UseOffHand;
				return new EducationCharacterProperties(child.CharacterObject, GetChildEquipmentForPage(child, page, previousOptions), actionId, prefabId, useOffHand);
			}
			return GetChildPropertiesForOption(child, previousOptions[0], previousOptions);
		}

		private CharacterObject GetSpecialCharacterForOption(Hero child, string optionKey, List<string> previousOptions)
		{
			string[] array = optionKey.Split(new char[1] { ';' });
			if (!int.TryParse(array[0], out var result))
			{
				Debug.FailedAssert("/keys/ isnt set correctly", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\EducationCampaignBehavior.cs", "GetSpecialCharacterForOption", 331);
			}
			CharacterObject result2 = null;
			if (Target == ChildAgeState.Year8)
			{
				if (result == 0)
				{
					result2 = Game.Current.ObjectManager.GetObject<CharacterObject>($"child_education_templates_stage_{(int)Target}_page_0_branch_{array[2]}_{child.Culture.StringId}");
				}
				else
				{
					string arg = previousOptions[0].Split(new char[1] { ';' })[2];
					result2 = Game.Current.ObjectManager.GetObject<CharacterObject>($"child_education_templates_stage_{(int)Target}_page_0_branch_{arg}_{child.Culture.StringId}");
				}
			}
			return result2;
		}

		private EducationCharacterProperties GetSpecialCharacterPropertiesForPage(Hero child, EducationPage page, List<string> previousOptions)
		{
			EducationCharacterProperties result = EducationCharacterProperties.Invalid;
			if (Target == ChildAgeState.Year8)
			{
				if (page.SpecialCharacterProperties != EducationCharacterProperties.Invalid)
				{
					string actionId = page.SpecialCharacterProperties.ActionId;
					string prefabId = page.SpecialCharacterProperties.PrefabId;
					bool useOffHand = page.SpecialCharacterProperties.UseOffHand;
					CharacterObject specialCharacterForOption = GetSpecialCharacterForOption(child, previousOptions[0], previousOptions);
					result = new EducationCharacterProperties(specialCharacterForOption, specialCharacterForOption.Equipment, actionId, prefabId, useOffHand);
				}
				if (previousOptions.Count > 0)
				{
					result = GetSpecialCharacterPropertiesForOption(child, previousOptions[0], previousOptions);
				}
			}
			return result;
		}

		private EducationCharacterProperties GetSpecialCharacterPropertiesForOption(Hero child, string optionKey, List<string> previousOptions)
		{
			EducationCharacterProperties result = EducationCharacterProperties.Invalid;
			if (Target == ChildAgeState.Year8)
			{
				EducationOption option = GetOption(optionKey);
				CharacterObject specialCharacterForOption = GetSpecialCharacterForOption(child, optionKey, previousOptions);
				result = new EducationCharacterProperties(specialCharacterForOption, specialCharacterForOption.Equipment, option.SpecialCharacterProperties.ActionId, option.SpecialCharacterProperties.PrefabId, option.SpecialCharacterProperties.UseOffHand);
			}
			return result;
		}

		public EducationOption GetOption(string optionKey)
		{
			string[] array = optionKey.Split(new char[1] { ';' });
			return _superPages[int.Parse(array[0])][int.Parse(array[1])].GetOption(optionKey);
		}

		public EducationPage GetPage(List<string> previousOptionKeys)
		{
			List<EducationOption> list = StringIdToEducationOption(previousOptionKeys);
			int count = previousOptionKeys.Count;
			List<EducationPage> list2 = _superPages[count];
			for (int i = 0; i < list2.Count; i++)
			{
				EducationPage educationPage = list2[i];
				if ((educationPage.Condition == null || educationPage.Condition(educationPage, list)) && educationPage.GetAvailableOptions(list).Length != 0)
				{
					return educationPage;
				}
			}
			Debug.FailedAssert("Education page not found", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\EducationCampaignBehavior.cs", "GetPage", 421);
			return null;
		}

		public List<EducationOption> StringIdToEducationOption(List<string> previousOptionKeys)
		{
			List<EducationOption> list = new List<EducationOption>();
			foreach (string previousOptionKey in previousOptionKeys)
			{
				list.Add(GetOption(previousOptionKey));
			}
			return list;
		}

		public override string ToString()
		{
			return Target.ToString();
		}

		internal EducationCharacterProperties[] GetCharacterPropertiesForPage(Hero child, EducationPage page, List<string> previousChoices)
		{
			EducationCharacterProperties childPropertiesForPage = GetChildPropertiesForPage(child, page, previousChoices);
			EducationCharacterProperties specialCharacterPropertiesForPage = GetSpecialCharacterPropertiesForPage(child, page, previousChoices);
			List<EducationCharacterProperties> list = new List<EducationCharacterProperties>();
			if (childPropertiesForPage != EducationCharacterProperties.Invalid)
			{
				list.Add(childPropertiesForPage);
			}
			if (specialCharacterPropertiesForPage != EducationCharacterProperties.Invalid)
			{
				list.Add(specialCharacterPropertiesForPage);
			}
			return list.ToArray();
		}

		internal EducationCharacterProperties[] GetCharacterPropertiesForOption(Hero child, EducationOption option, string optionKey, List<string> previousOptions)
		{
			EducationCharacterProperties childPropertiesForOption = GetChildPropertiesForOption(child, optionKey, previousOptions);
			EducationCharacterProperties specialCharacterPropertiesForOption = GetSpecialCharacterPropertiesForOption(child, optionKey, previousOptions);
			List<EducationCharacterProperties> list = new List<EducationCharacterProperties>();
			if (childPropertiesForOption != EducationCharacterProperties.Invalid)
			{
				list.Add(childPropertiesForOption);
			}
			if (specialCharacterPropertiesForOption != EducationCharacterProperties.Invalid)
			{
				list.Add(specialCharacterPropertiesForOption);
			}
			return list.ToArray();
		}
	}

	public struct EducationCharacterProperties
	{
		public readonly CharacterObject Character;

		public readonly Equipment Equipment;

		public readonly string ActionId;

		public readonly string PrefabId;

		public readonly bool UseOffHand;

		public static readonly EducationCharacterProperties Default = new EducationCharacterProperties("act_inventory_idle_start");

		public static readonly EducationCharacterProperties Invalid = default(EducationCharacterProperties);

		public EducationCharacterProperties(CharacterObject character, Equipment equipment, string actionId, string prefabId, bool useOffHand)
		{
			Character = character;
			Equipment = equipment;
			ActionId = actionId;
			PrefabId = prefabId;
			UseOffHand = useOffHand;
		}

		public EducationCharacterProperties(string actionId, string prefabId, bool useOffHand)
			: this(null, null, actionId, prefabId, useOffHand)
		{
		}

		public EducationCharacterProperties(string actionId)
			: this(null, null, actionId, string.Empty, useOffHand: false)
		{
		}

		public static bool operator ==(EducationCharacterProperties a, EducationCharacterProperties b)
		{
			if (a.Character == b.Character && a.Equipment == b.Equipment && a.ActionId == b.ActionId)
			{
				return a.PrefabId == b.PrefabId;
			}
			return false;
		}

		public static bool operator !=(EducationCharacterProperties a, EducationCharacterProperties b)
		{
			return !(a == b);
		}

		public bool Equals(EducationCharacterProperties other)
		{
			if (Character.Equals(other.Character) && Equipment.Equals(other.Equipment) && ActionId.Equals(other.ActionId))
			{
				return PrefabId.Equals(other.PrefabId);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj != null && obj is EducationCharacterProperties)
			{
				return Equals((EducationCharacterProperties)obj);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (((((ActionId.GetHashCode() * 397) ^ Character.GetHashCode()) * 397) ^ Equipment.GetHashCode()) * 397) ^ PrefabId.GetHashCode();
		}

		public sbyte GetUsedHandBoneIndex()
		{
			if (!UseOffHand)
			{
				return FaceGen.GetBaseMonsterFromRace(Character.Race).MainHandItemBoneIndex;
			}
			return FaceGen.GetBaseMonsterFromRace(Character.Race).OffHandItemBoneIndex;
		}
	}

	private class EducationPage
	{
		public delegate bool EducationPageConditionDelegate(EducationPage page, List<EducationOption> previousOptions);

		public readonly EducationPageConditionDelegate Condition;

		public readonly TextObject Title;

		public readonly TextObject Description;

		public readonly TextObject Instruction;

		private readonly string _id;

		private int _keyIndex;

		private readonly Dictionary<string, EducationOption> _options;

		public readonly EducationCharacterProperties ChildProperties;

		public readonly EducationCharacterProperties SpecialCharacterProperties;

		public IEnumerable<EducationOption> Options => _options.Values;

		public EducationPage(string id, TextObject title, TextObject description, TextObject instruction, EducationCharacterProperties childProperties, EducationCharacterProperties specialCharacterProperties, EducationPageConditionDelegate condition = null)
		{
			_id = id;
			Condition = condition;
			Title = title;
			Description = description;
			Instruction = instruction;
			_options = new Dictionary<string, EducationOption>();
			_keyIndex = 0;
			ChildProperties = childProperties;
			SpecialCharacterProperties = specialCharacterProperties;
		}

		public void AddOption(EducationOption option)
		{
			_options.Add(_id + ";" + _keyIndex, option);
			_keyIndex++;
		}

		public EducationOption GetOption(string optionKey)
		{
			_options.TryGetValue(optionKey, out var value);
			return value;
		}

		public string[] GetAvailableOptions(List<EducationOption> previousEducationOptions)
		{
			List<string> list = new List<string>();
			foreach (KeyValuePair<string, EducationOption> option in _options)
			{
				if (option.Value.Condition == null || option.Value.Condition(option.Value, previousEducationOptions))
				{
					list.Add(option.Key);
				}
			}
			return list.ToArray();
		}
	}

	private const char Separator = ';';

	private const int AttributeIncrease = 1;

	private const int FocusIncrease = 1;

	private const int SkillIncrease = 15;

	private readonly TextObject _pickAttributeText = new TextObject("{=m7iBf6fQ}Pick an Attribute");

	private readonly TextObject _confirmResultsText = new TextObject("{=La9qAlfi}Confirm the Results");

	private readonly TextObject _chooseTalentText = new TextObject("{=K9fcqr0K}Choose a Talent");

	private readonly TextObject _chooseTutorText = new TextObject("{=B7JVLc4u}Choose a Tutor");

	private readonly TextObject _guideTutorText = new TextObject("{=VbWAsWWY}Guide the Tutor");

	private readonly TextObject _chooseFocusText = new TextObject("{=HBZS0bug}Choose a Focus");

	private readonly TextObject _chooseSkillText = new TextObject("{=5BEGa9ZS}Choose a Skill");

	private readonly TextObject _chooseGiftText = new TextObject("{=DcoDtW2A}Choose a Gift");

	private readonly TextObject _chooseAchievementText = new TextObject("{=26sKJehk}Choose an Achievement");

	private Dictionary<Hero, short> _previousEducations = new Dictionary<Hero, short>();

	private readonly TextObject _chooseTaskText = new TextObject("{=SUNKjdZ9}Choose a Task");

	private readonly TextObject _chooseRequestText = new TextObject("{=jNBVoObj}Choose a Request");

	private Hero _activeChild;

	private EducationStage _activeStage;

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_previousEducations", ref _previousEducations);
	}

	public override void RegisterEvents()
	{
		if (!CampaignOptions.IsLifeDeathCycleDisabled)
		{
			CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
			CampaignEvents.OnCharacterCreationIsOverEvent.AddNonSerializedListener(this, OnCharacterCreationOver);
			CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
			CampaignEvents.HeroComesOfAgeEvent.AddNonSerializedListener(this, OnHeroComesOfAge);
		}
	}

	private void OnHeroComesOfAge(Hero hero)
	{
		if (hero.Mother?.Clan == Clan.PlayerClan || hero.Father?.Clan == Clan.PlayerClan)
		{
			DoEducationUntil(hero, ChildAgeState.Count);
			_previousEducations.Remove(hero);
		}
	}

	private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail details, bool showNotifications)
	{
		if (victim.Clan == Clan.PlayerClan && _previousEducations.ContainsKey(victim))
		{
			_previousEducations.Remove(victim);
		}
	}

	private void OnCharacterCreationOver()
	{
		if (CampaignOptions.IsLifeDeathCycleDisabled)
		{
			CampaignEventDispatcher.Instance.RemoveListeners(this);
		}
	}

	private void OnDailyTick()
	{
		if (MapEvent.PlayerMapEvent != null)
		{
			return;
		}
		foreach (Hero hero in Clan.PlayerClan.Heroes)
		{
			if (!hero.IsAlive || hero == Hero.MainHero || !(hero.Age < (float)Campaign.Current.Models.AgeModel.HeroComesOfAge))
			{
				continue;
			}
			ChildAgeState lastDoneStage = GetLastDoneStage(hero);
			if (lastDoneStage == ChildAgeState.Year16)
			{
				continue;
			}
			ChildAgeState childAgeState = (ChildAgeState)MathF.Max((int)(lastDoneStage + 1), (int)GetClosestStage(hero));
			int num = ChildStateToAge(childAgeState);
			if ((hero.BirthDay + CampaignTime.Years(num)).IsPast && !HasNotificationForAge(hero, num))
			{
				DoEducationUntil(hero, childAgeState);
				if (!hero.IsDisabled)
				{
					ShowEducationNotification(hero, ChildStateToAge(childAgeState));
				}
			}
		}
	}

	public void GetOptionProperties(Hero child, string optionKey, List<string> previousOptions, out TextObject optionTitle, out TextObject description, out TextObject effect, out (CharacterAttribute, int)[] attributes, out (SkillObject, int)[] skills, out (SkillObject, int)[] focusPoints, out EducationCharacterProperties[] educationCharacterProperties)
	{
		EducationStage stage = GetStage(child);
		EducationOption option = stage.GetOption(optionKey);
		description = option.Description;
		effect = option.Effect;
		optionTitle = option.Title;
		educationCharacterProperties = stage.GetCharacterPropertiesForOption(child, option, optionKey, previousOptions);
		if (option.Attributes == null)
		{
			attributes = null;
		}
		else
		{
			attributes = new(CharacterAttribute, int)[option.Attributes.Length];
			for (int i = 0; i < option.Attributes.Length; i++)
			{
				attributes[i] = (option.Attributes[i], 1);
			}
		}
		if (option.Skills == null)
		{
			skills = null;
			focusPoints = null;
			return;
		}
		skills = new(SkillObject, int)[option.Skills.Length];
		focusPoints = new(SkillObject, int)[option.Skills.Length];
		for (int j = 0; j < option.Skills.Length; j++)
		{
			skills[j] = (option.Skills[j], 15);
			focusPoints[j] = (option.Skills[j], 1);
		}
	}

	public void GetPageProperties(Hero child, List<string> previousChoices, out TextObject title, out TextObject description, out TextObject instruction, out EducationCharacterProperties[] defaultCharacterProperties, out string[] availableOptions)
	{
		EducationStage stage = GetStage(child);
		EducationPage page = stage.GetPage(previousChoices);
		defaultCharacterProperties = stage.GetCharacterPropertiesForPage(child, page, previousChoices);
		title = page.Title;
		description = page.Description;
		instruction = page.Instruction;
		availableOptions = page.GetAvailableOptions(stage.StringIdToEducationOption(previousChoices));
	}

	public bool IsValidEducationNotification(EducationMapNotification data)
	{
		EducationStage stage = GetStage(data.Child);
		if (data.Child.IsAlive && data.Age > 0 && data.Child.Age < (float)Campaign.Current.Models.AgeModel.HeroComesOfAge)
		{
			return stage != null;
		}
		return false;
	}

	public void GetStageProperties(Hero child, out int pageCount)
	{
		EducationStage stage = GetStage(child);
		pageCount = stage.PageCount;
	}

	private ChildAgeState GetClosestStage(Hero child)
	{
		ChildAgeState result = ChildAgeState.Year2;
		int num = MathF.Round(child.Age);
		for (ChildAgeState childAgeState = ChildAgeState.Year2; childAgeState <= ChildAgeState.Year16; childAgeState++)
		{
			if (num >= ChildStateToAge(childAgeState))
			{
				result = childAgeState;
			}
		}
		return result;
	}

	private ChildAgeState GetLastDoneStage(Hero child)
	{
		if (_previousEducations.TryGetValue(child, out var value))
		{
			return (ChildAgeState)value;
		}
		return ChildAgeState.Invalid;
	}

	private void OnFinalize(EducationStage stage, Hero child, List<string> chosenOptions)
	{
		foreach (string chosenOption in chosenOptions)
		{
			stage.GetOption(chosenOption).OnConsequence(child);
		}
		CampaignEventDispatcher.Instance.OnChildEducationCompleted(child, ChildStateToAge(stage.Target));
		short target = (short)stage.Target;
		if (_previousEducations.ContainsKey(child))
		{
			_previousEducations[child] = target;
		}
		else
		{
			_previousEducations.Add(child, target);
		}
		_activeStage = null;
		_activeChild = null;
	}

	private bool HasNotificationForAge(Hero child, int age)
	{
		return Campaign.Current.CampaignInformationManager.InformationDataExists((EducationMapNotification notification) => notification.Child == child && notification.Age == age);
	}

	private void ShowEducationNotification(Hero child, int age)
	{
		TextObject textObject = GameTexts.FindText("str_education_notification_right");
		textObject.SetCharacterProperties("CHILD", child.CharacterObject, includeDetails: true);
		Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new EducationMapNotification(child, age, textObject));
		if (child.Father == Hero.MainHero || child.Mother == Hero.MainHero)
		{
			Debug.Print($"Showing Education Notification, Hero: {child.StringId}: {child.Name} - Age: {age}.");
		}
		if (!_previousEducations.ContainsKey(child))
		{
			_previousEducations.Add(child, -1);
		}
	}

	private void DoEducationUntil(Hero child, ChildAgeState childAgeState)
	{
		if (!_previousEducations.TryGetValue(child, out var value))
		{
			value = -1;
		}
		for (ChildAgeState childAgeState2 = (ChildAgeState)(value + 1); childAgeState2 < childAgeState; childAgeState2++)
		{
			if (childAgeState2 != ChildAgeState.Invalid)
			{
				EducationStage stage = GetStage(child, childAgeState2);
				DoStage(child, stage);
			}
		}
	}

	private void DoStage(Hero child, EducationStage stage)
	{
		List<string> list = new List<string>();
		for (int i = 0; i < stage.PageCount; i++)
		{
			string[] availableOptions = stage.GetPage(list).GetAvailableOptions(stage.StringIdToEducationOption(list));
			list.Add(availableOptions.GetRandomElement());
		}
		OnFinalize(stage, child, list);
	}

	public void Finalize(Hero child, List<string> chosenOptions)
	{
		OnFinalize(GetStage(child), child, chosenOptions);
	}

	private bool IsHeroChildOfPlayer(Hero child)
	{
		return Hero.MainHero.Children.Contains(child);
	}

	private bool ChildCultureHasLorekeeper(Hero child)
	{
		if (!(child.Culture.StringId == "khuzait"))
		{
			return child.Culture.StringId == "battania";
		}
		return true;
	}

	private bool ChildCultureHasBard(Hero child)
	{
		return child.Culture.StringId == "battania";
	}

	private EducationStage GetStage(Hero child)
	{
		short value;
		ChildAgeState state = ((!_previousEducations.TryGetValue(child, out value) || value == -1) ? GetClosestStage(child) : ((ChildAgeState)(value + 1)));
		return GetStage(child, state);
	}

	private EducationStage GetStage(Hero child, ChildAgeState state)
	{
		if (_activeStage == null || _activeStage.Target != state || child != _activeChild)
		{
			_activeChild = child;
			switch (state)
			{
			case ChildAgeState.Year2:
				_activeStage = CreateStage2(child);
				break;
			case ChildAgeState.Year5:
				_activeStage = CreateStage5(child);
				break;
			case ChildAgeState.Year8:
				_activeStage = CreateStage8(child);
				break;
			case ChildAgeState.Year11:
				_activeStage = CreateStage11(child);
				break;
			case ChildAgeState.Year14:
				_activeStage = CreateStage14(child);
				break;
			case ChildAgeState.Year16:
				_activeStage = CreateStage16(child);
				break;
			default:
				_activeStage = null;
				_activeChild = null;
				break;
			}
		}
		StringHelpers.SetCharacterProperties("CHILD", child.CharacterObject);
		return _activeStage;
	}

	private static int ChildStateToAge(ChildAgeState state)
	{
		return state switch
		{
			ChildAgeState.Year2 => 2, 
			ChildAgeState.Year5 => 5, 
			ChildAgeState.Year8 => 8, 
			ChildAgeState.Year11 => 11, 
			ChildAgeState.Year14 => 14, 
			ChildAgeState.Year16 => 16, 
			_ => -1, 
		};
	}

	private void Stage2Selection(List<SkillObject> skills, EducationPage previousPage, EducationPage currentPage, EducationCharacterProperties[] childProperties, EducationCharacterProperties[] tutorProperties)
	{
		for (int i = 0; i < skills.Count; i++)
		{
			int index = i;
			EducationCharacterProperties childProperties2 = childProperties[index];
			EducationCharacterProperties specialCharacterProperties = tutorProperties[index];
			SkillObject skill = skills[index];
			TextObject textObject = new TextObject("{=!}{SKILL}");
			TextObject textObject2 = new TextObject("{=!}{SKILL_DESC}");
			textObject.SetTextVariable("SKILL", skill.Name);
			textObject2.SetTextVariable("SKILL_DESC", previousPage.Options.First((EducationOption x) => x.Skills.Contains(skill)).Description);
			EducationOption.EducationOptionConditionDelegate condition = delegate(EducationOption o, List<EducationOption> previousOptions)
			{
				int num = previousOptions[0].RandomValue % skills.Count;
				int num2 = previousOptions[1].RandomValue % skills.Count;
				int num3;
				if ((num + num2) % skills.Count == index)
				{
					num3 = ((previousOptions[1].Skills[0] != skill) ? 1 : 0);
					if (num3 != 0)
					{
						currentPage.Description.SetTextVariable("SKILL", skill.Name);
					}
				}
				else
				{
					num3 = 0;
				}
				return (byte)num3 != 0;
			};
			currentPage.AddOption(new EducationOption(textObject, textObject2, null, condition, null, null, new SkillObject[1] { skill }, childProperties2, specialCharacterProperties));
			SkillObject alternativeSkill = skills[(index + MBRandom.RandomInt(1, 6)) % skills.Count];
			TextObject textObject3 = new TextObject("{=!}{SKILL}");
			TextObject textObject4 = new TextObject("{=!}{SKILL_DESC}");
			textObject3.SetTextVariable("SKILL", alternativeSkill.Name);
			textObject4.SetTextVariable("SKILL_DESC", previousPage.Options.First((EducationOption x) => x.Skills.Contains(alternativeSkill)).Description);
			EducationOption.EducationOptionConditionDelegate condition2 = delegate(EducationOption o, List<EducationOption> previousOptions)
			{
				int num = previousOptions[0].RandomValue % skills.Count;
				int num2 = previousOptions[1].RandomValue % skills.Count;
				int num3;
				if ((num + num2) % skills.Count == index)
				{
					num3 = ((previousOptions[1].Skills[0] == skill) ? 1 : 0);
					if (num3 != 0)
					{
						currentPage.Description.SetTextVariable("SKILL", alternativeSkill.Name);
					}
				}
				else
				{
					num3 = 0;
				}
				return (byte)num3 != 0;
			};
			currentPage.AddOption(new EducationOption(textObject3, textObject4, null, condition2, null, null, new SkillObject[1] { alternativeSkill }, childProperties2, specialCharacterProperties));
		}
	}

	private void Stage16Selection((TextObject, TextObject, SkillObject)[] titleDescSkillTuple, EducationPage currentPage, EducationCharacterProperties[] childProperties)
	{
		for (int i = 0; i < titleDescSkillTuple.Length; i++)
		{
			int index = i;
			(TextObject, TextObject, SkillObject) container = titleDescSkillTuple[index];
			EducationCharacterProperties childProperties2 = childProperties[index];
			SkillObject skill = container.Item3;
			EducationOption option = new EducationOption(new TextObject("{=!}{OUTCOME_TITLE}"), new TextObject("{=!}{OUTCOME_DESC}"), null, delegate(EducationOption o, List<EducationOption> previousOptions)
			{
				int num = previousOptions[0].RandomValue % titleDescSkillTuple.Length;
				int num2 = previousOptions[1].RandomValue % titleDescSkillTuple.Length;
				int num3 = (num + num2) % titleDescSkillTuple.Length;
				SkillObject previousPageSkill = previousOptions[1].Skills[0];
				bool num4 = index == num3;
				if (num4)
				{
					int num5 = titleDescSkillTuple.FindIndex(((TextObject, TextObject, SkillObject) x) => x.Item3 == previousPageSkill);
					if (num3 == num5)
					{
						container = titleDescSkillTuple[(index + 1) % titleDescSkillTuple.Length];
					}
					currentPage.Description.SetTextVariable("RANDOM_OUTCOME", container.Item1);
					currentPage.Description.SetTextVariable("SKILL", skill.Name);
				}
				o.Title.SetTextVariable("OUTCOME_TITLE", container.Item1);
				o.Description.SetTextVariable("OUTCOME_DESC", container.Item2);
				return num4;
			}, null, null, new SkillObject[1] { skill }, childProperties2);
			currentPage.AddOption(option);
		}
	}

	private EducationStage CreateStage2(Hero child)
	{
		EducationStage educationStage = new EducationStage(ChildAgeState.Year2);
		TextObject title = new TextObject("{=xc4ossl0}Infancy");
		Dictionary<CharacterAttribute, TextObject> dictionary = new Dictionary<CharacterAttribute, TextObject>
		{
			{
				DefaultCharacterAttributes.Vigor,
				new TextObject("{=h7aX2GOw}This child is quite strong, grabbing whatever {?CHILD.GENDER}she{?}he{\\?} likes climbing out of {?CHILD.GENDER}her{?}his{\\?} cradle whenever {?CHILD.GENDER}her{?}his{\\?} caretaker's back is turned.")
			},
			{
				DefaultCharacterAttributes.Control,
				new TextObject("{=pQSBdHC7}The child has exceptional coordination for someone {?CHILD.GENDER}her{?}his{\\?} age, and can catch a tossed ball and eat by {?CHILD.GENDER}herself{?}himself{\\?}.")
			},
			{
				DefaultCharacterAttributes.Endurance,
				new TextObject("{=xaNpQsjh}The child seems to never tire, running {?CHILD.GENDER}her{?}his{\\?} caretakers ragged, and is rarely cranky.")
			},
			{
				DefaultCharacterAttributes.Cunning,
				new TextObject("{=lF41sN5r}You see the glint of mischief on {?CHILD.GENDER}her{?}his{\\?} smiling face. Any sweet left unattended in the kitchen for even a few minutes is quickly stolen.")
			},
			{
				DefaultCharacterAttributes.Intelligence,
				new TextObject("{=KVDMVbT1}This child started speaking earlier than most of {?CHILD.GENDER}her{?}his{\\?} peers and can even string together simple sentences.")
			},
			{
				DefaultCharacterAttributes.Social,
				new TextObject("{=xXJnW5w0}The child pays close attention to anyone talking to {?CHILD.GENDER}her{?}him{\\?} and sometimes tries to comfort playmates in distress.")
			}
		};
		Dictionary<CharacterAttribute, EducationCharacterProperties> dictionary2 = new Dictionary<CharacterAttribute, EducationCharacterProperties>
		{
			{
				DefaultCharacterAttributes.Vigor,
				new EducationCharacterProperties("act_childhood_toddler_vigor")
			},
			{
				DefaultCharacterAttributes.Control,
				new EducationCharacterProperties("act_childhood_toddler_control")
			},
			{
				DefaultCharacterAttributes.Endurance,
				new EducationCharacterProperties("act_childhood_toddler_endurance")
			},
			{
				DefaultCharacterAttributes.Cunning,
				new EducationCharacterProperties("act_childhood_toddler_cunning")
			},
			{
				DefaultCharacterAttributes.Intelligence,
				new EducationCharacterProperties("act_childhood_toddler_intelligence")
			},
			{
				DefaultCharacterAttributes.Social,
				new EducationCharacterProperties("act_childhood_toddler_social")
			}
		};
		TextObject textObject = new TextObject("{=Il7pDS8i}People remark on how much {?PLAYER_CHILD}your{?}the{\\?} baby resembles {?CHILD.GENDER}her{?}his{\\?} parents. {CHILD.NAME} definitely has {?PLAYER_IS_FATHER}your{?}{?CHILD.GENDER}her{?}his{\\?} father's{\\?}...");
		textObject.SetTextVariable("PLAYER_IS_FATHER", (child.Father == Hero.MainHero) ? 1 : 0);
		EducationPage educationPage = educationStage.AddPage(childProperties: new EducationCharacterProperties("act_childhood_toddler_sleep", string.Empty, useOffHand: false), pageIndex: 0, title: title, description: textObject, instruction: _pickAttributeText);
		GetHighestThreeAttributes(child.Father, out var maxAttributes);
		for (int i = 0; i < maxAttributes.Length; i++)
		{
			EducationCharacterProperties childProperties = dictionary2[maxAttributes[i].Item1];
			educationPage.AddOption(new EducationOption(maxAttributes[i].Item1.Name, dictionary[maxAttributes[i].Item1], null, null, null, new CharacterAttribute[1] { maxAttributes[i].Item1 }, null, childProperties));
		}
		TextObject textObject2 = new TextObject("{=0vWaNd1m}At the same time, {?CHILD.GENDER}she{?}he{\\?} shows {?PLAYER_IS_MOTHER}your{?}{?CHILD.GENDER}her{?}his{\\?} mother's{\\?}....");
		textObject2.SetTextVariable("PLAYER_IS_MOTHER", (child.Mother == Hero.MainHero) ? 1 : 0);
		EducationPage educationPage2 = educationStage.AddPage(childProperties: new EducationCharacterProperties("act_childhood_toddler_tantrum", string.Empty, useOffHand: false), pageIndex: 1, title: title, description: textObject2, instruction: _pickAttributeText);
		GetHighestThreeAttributes(child.Mother, out var maxAttributes2);
		for (int j = 0; j < maxAttributes2.Length; j++)
		{
			EducationCharacterProperties childProperties2 = dictionary2[maxAttributes2[j].Item1];
			educationPage2.AddOption(new EducationOption(maxAttributes2[j].Item1.Name, dictionary[maxAttributes2[j].Item1], null, null, null, new CharacterAttribute[1] { maxAttributes2[j].Item1 }, null, childProperties2));
		}
		TextObject description = new TextObject("{=taJOTFrb}Despite its tender age, the baby already starts to show some aptitude in {ATR_1} and {ATR_2}.");
		int optionIndexer = 0;
		EducationPage stage_0_page_2 = educationStage.AddPage(2, title, description, _confirmResultsText);
		for (int k = 0; k < Attributes.All.Count; k++)
		{
			CharacterAttribute attributeOne = Attributes.All[k];
			EducationCharacterProperties childProperties3 = dictionary2[attributeOne];
			List<CharacterAttribute> list = Attributes.All.Where((CharacterAttribute x) => x != attributeOne).ToList();
			for (int num = 0; num < Attributes.All.Count - 1; num++)
			{
				CharacterAttribute attributeTwo = list[num];
				_ = Attributes.All.Count;
				TextObject textObject3 = new TextObject("{=ISoR0vaR}{ATR_1} and {ATR_2}");
				TextObject textObject4 = new TextObject("{=KiOBbxr3}In addition to {?CHILD.GENDER}her{?}his{\\?} parents traits, this baby also shows promising {ATR_1} and {ATR_2}.");
				textObject3.SetTextVariable("ATR_1", attributeOne.Name);
				textObject4.SetTextVariable("ATR_1", attributeOne.Name);
				textObject3.SetTextVariable("ATR_2", attributeTwo.Name);
				textObject4.SetTextVariable("ATR_2", attributeTwo.Name);
				EducationOption.EducationOptionConditionDelegate condition = delegate(EducationOption o, List<EducationOption> previousOptions)
				{
					if (o == stage_0_page_2.Options.FirstOrDefault())
					{
						optionIndexer = 0;
					}
					int num2 = ((previousOptions[0].Attributes[0] == previousOptions[1].Attributes[0]) ? 1 : 2);
					bool num3 = previousOptions[0].Attributes.Contains(attributeOne) || previousOptions[1].Attributes.Contains(attributeOne) || previousOptions[0].Attributes.Contains(attributeTwo) || previousOptions[1].Attributes.Contains(attributeTwo);
					int num4 = (Attributes.All.Count - num2 - 1) * (Attributes.All.Count - num2);
					int randomValue = previousOptions[0].RandomValue;
					int randomValue2 = previousOptions[1].RandomValue;
					int num5 = (randomValue % num4 + randomValue2 % num4) % num4;
					if (!num3)
					{
						int num6 = optionIndexer;
						optionIndexer = num6 + 1;
					}
					if (!num3 && num5 == optionIndexer % num4)
					{
						stage_0_page_2.Description.SetTextVariable("ATR_1", attributeOne.Name);
						stage_0_page_2.Description.SetTextVariable("ATR_2", attributeTwo.Name);
						return true;
					}
					return false;
				};
				stage_0_page_2.AddOption(new EducationOption(textObject3, textObject4, null, condition, null, new CharacterAttribute[2] { attributeOne, attributeTwo }, null, childProperties3));
			}
		}
		return educationStage;
	}

	private EducationStage CreateStage5(Hero child)
	{
		EducationStage educationStage = new EducationStage(ChildAgeState.Year5);
		TextObject title = new TextObject("{=8Yiwt1z6}Early Childhood");
		TextObject description = new TextObject("{=6PrmgKXa}{CHILD.NAME} is now old enough to play independently with the other children of the estate. You are particularly struck by how {?CHILD.GENDER}she{?}he{\\?}...");
		EducationPage educationPage = educationStage.AddPage(childProperties: new EducationCharacterProperties("act_inventory_idle_start", string.Empty, useOffHand: false), pageIndex: 0, title: title, description: description, instruction: _chooseTalentText);
		educationPage.AddOption(new EducationOption(new TextObject("{=aeWZRHy3}takes charge."), new TextObject("{=TyvEmZbC}{CHILD.NAME} is usually the one who decides what games {?CHILD.GENDER}her{?}his{\\?} friends will play, and leads them on imaginary adventures around the estate."), null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Social }, new SkillObject[1] { DefaultSkills.Leadership }, new EducationCharacterProperties("act_childhood_leader")));
		educationPage.AddOption(new EducationOption(new TextObject("{=auCinAKs}never tires out."), new TextObject("{=ymt7Ol6x}{?CHILD.GENDER}She{?}He{\\?} seems to have limitless energy, continuing with games long after all the other children have taken a break."), null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Endurance }, new SkillObject[1] { DefaultSkills.Athletics }, new EducationCharacterProperties("act_childhood_fierce")));
		educationPage.AddOption(new EducationOption(new TextObject("{=fvutqQH6}always hits {?CHILD.GENDER}her{?}his{\\?} mark."), new TextObject("{=i2qOUBF4}The child will win any game that involves throwing and aiming, and the local crows have learned to keep their distance lest they want to be hit by a stone."), null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Control }, new SkillObject[1] { DefaultSkills.Throwing }, new EducationCharacterProperties("act_childhood_ready_throw", "spear_new_f_1-9m", useOffHand: false)));
		educationPage.AddOption(new EducationOption(new TextObject("{=J80YXzR2}is fascinated by riddles."), new TextObject("{=QuffbfeU}The child asks any adults {?CHILD.GENDER}she{?}he{\\?} sees to ask {?CHILD.GENDER}her{?}him{\\?} one of the riddles that are loved by your people, and never gives up until it is solved."), null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Intelligence }, new SkillObject[1] { DefaultSkills.Engineering }, new EducationCharacterProperties("act_childhood_memory")));
		educationPage.AddOption(new EducationOption(new TextObject("{=tMkMi5C7}wins wrestling matches."), new TextObject("{=zulqIQQw}Children play rough, and this child is usually the one who winds up on top in any tussle."), null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Vigor }, new SkillObject[1] { DefaultSkills.TwoHanded }, new EducationCharacterProperties("act_childhood_athlete")));
		educationPage.AddOption(new EducationOption(new TextObject("{=h1GdWZjk}avoids chores."), new TextObject("{=qbLkFkWr}Usually when there is work to be done this child is nowhere to be found. You've learned some of {?CHILD.GENDER}her{?}his{\\?} hiding places, but {?CHILD.GENDER}she{?}he{\\?} always seems to find more."), null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Cunning }, new SkillObject[1] { DefaultSkills.Roguery }, new EducationCharacterProperties("act_childhood_spoiled")));
		TextObject description2 = new TextObject("{=GX0B9ngI}{CHILD.NAME} spends time by {?CHILD.GENDER}herself{?}himself{\\?} as well, frequently...");
		EducationPage educationPage2 = educationStage.AddPage(1, title, description2, _chooseTalentText);
		educationPage2.AddOption(new EducationOption(new TextObject("{=gkuS35ly}caring for your horses."), new TextObject("{=smdvQWvu}{?PLAYER_CHILD}Your{?}This{\\?} child loves animals of all kinds. You know that one day {?CHILD.GENDER}she{?}he{\\?} will be a great rider."), null, null, null, null, new SkillObject[1] { DefaultSkills.Riding }, new EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", useOffHand: true)));
		educationPage2.AddOption(new EducationOption(new TextObject("{=1Ehm6o1d}shooting at targets."), new TextObject("{=RkQjllml}Given even a few minutes of free time, {?PLAYER_CHILD}your{?}this{\\?} child will line up targets and shoot them with {?CHILD.GENDER}her{?}his{\\?} home-made bow."), null, null, null, null, new SkillObject[1] { DefaultSkills.Bow }, new EducationCharacterProperties("act_childhood_ready_bow", "bow_roman_a", useOffHand: true)));
		educationPage2.AddOption(new EducationOption(new TextObject("{=25vJYig0}trekking around nearby hills."), new TextObject("{=LJsckbyF}{?PLAYER_CHILD}Your{?}This{\\?} child spends hours exploring the edges of the estate, following animal tracks and looking for edible plants."), null, null, null, null, new SkillObject[1] { DefaultSkills.Scouting }, new EducationCharacterProperties("act_childhood_roguery", "carry_bostaff_rogue1", useOffHand: false)));
		educationPage2.AddOption(new EducationOption(new TextObject("{=54PfOh98}making {?CHILD.GENDER}her{?}his{\\?} own toys."), new TextObject("{=LfJhIQpb}With a few sticks and a bit of twine, {?CHILD.GENDER}she{?}he{\\?} can make recognizable animals, weapons or dolls."), null, null, null, null, new SkillObject[1] { DefaultSkills.Crafting }, new EducationCharacterProperties("act_childhood_artisan", "carry_linen", useOffHand: false)));
		educationPage2.AddOption(new EducationOption(new TextObject("{=hjR0Jvh1}fighting mock battles."), new TextObject("{=hGheavqY}{?PLAYER_CHILD}Your{?}This{\\?} child spends most of {?CHILD.GENDER}her{?}his{\\?} free time fighting imaginary monsters with {?CHILD.GENDER}her{?}his{\\?} wooden toy sword."), null, null, null, null, new SkillObject[1] { DefaultSkills.OneHanded }, new EducationCharacterProperties("act_childhood_play_2", "training_sword", useOffHand: false)));
		educationPage2.AddOption(new EducationOption(new TextObject("{=wX0uzvRE}playing board games."), new TextObject("{=H7NJ0QHQ}You see {?PLAYER_CHILD}your{?}the{\\?} child endlessly re-arranging the pieces, inventing new rules and playing out stories in {?CHILD.GENDER}her{?}his{\\?} head."), null, null, null, null, new SkillObject[1] { DefaultSkills.Tactics }, new EducationCharacterProperties("act_childhood_tactician")));
		foreach (EducationOption option2 in educationPage2.Options)
		{
			option2.Description.SetTextVariable("PLAYER_CHILD", IsHeroChildOfPlayer(child) ? 1 : 0);
		}
		TextObject stage_1_page_2_description = new TextObject("{=Zoes3ojA}{?CHILD.GENDER}Her{?}His{\\?} tutors continue to remark on {CHILD.NAME}'s progress, commending {?CHILD.GENDER}her{?}his{\\?} {ATR}.");
		EducationPage educationPage3 = educationStage.AddPage(2, title, stage_1_page_2_description, _confirmResultsText);
		(TextObject, TextObject, CharacterAttribute[])[] stage_1_page_2_options = new(TextObject, TextObject, CharacterAttribute[])[6]
		{
			(new TextObject("{=VdLkZthN}Mathematical Aptitude"), new TextObject("{=23YF84ib}{?PLAYER_CHILD}Your{?}The{\\?} child quickly solves problems in {?CHILD.GENDER}her{?}his{\\?} head, helping adults with their calculations."), new CharacterAttribute[1] { DefaultCharacterAttributes.Intelligence }),
			(new TextObject("{=6RckjM4S}Presence"), new TextObject("{=Instmiut}Your child is blessed with a strong and sonorous voice, making {?CHILD.GENDER}her{?}him{\\?} seem older and wiser than {?CHILD.GENDER}her{?}his{\\?} years."), new CharacterAttribute[1] { DefaultCharacterAttributes.Social }),
			(new TextObject("{=D1jGAX41}Courage"), new TextObject("{=WOriPdox}When trouble comes, {?PLAYER_CHILD}your child{?}{?CHILD.GENDER}she{?}he{\\?}{\\?} faces it head on."), new CharacterAttribute[1] { DefaultCharacterAttributes.Vigor }),
			(new TextObject("{=0D1ty8JA}Coordination"), new TextObject("{=ZkkHPpT7}Your child excels at any task that requires {?CHILD.GENDER}her{?}him{\\?} to use {?CHILD.GENDER}her{?}his{\\?} hands."), new CharacterAttribute[1] { DefaultCharacterAttributes.Control }),
			(new TextObject("{=JqaKRXNo}Craftiness"), new TextObject("{=1KWhhurv}Your tutor throws up his hands in the air at {CHILD.NAME}'s ability to lie {?CHILD.GENDER}her{?}his{\\?} way out of trouble, but can't help but admire it a little."), new CharacterAttribute[1] { DefaultCharacterAttributes.Cunning }),
			(new TextObject("{=la8jMuQ8}Energy"), new TextObject("{=RUjPcZhF}{?PLAYER_CHILD}Your{?}The{\\?} child is rarely even winded after {?CHILD.GENDER}her{?}his{\\?} daily exercises."), new CharacterAttribute[1] { DefaultCharacterAttributes.Endurance })
		};
		EducationCharacterProperties[] array = new EducationCharacterProperties[6]
		{
			new EducationCharacterProperties("act_childhood_closed_tutor"),
			new EducationCharacterProperties("act_childhood_confident2_tutor"),
			new EducationCharacterProperties("act_childhood_confident_tutor"),
			new EducationCharacterProperties("act_childhood_demure_tutor"),
			new EducationCharacterProperties("act_childhood_hip_tutor"),
			new EducationCharacterProperties("act_childhood_sharp")
		};
		EducationCharacterProperties[] array2 = new EducationCharacterProperties[6]
		{
			new EducationCharacterProperties("act_childhood_numbers"),
			new EducationCharacterProperties("act_childhood_manners"),
			new EducationCharacterProperties("act_childhood_fierce"),
			new EducationCharacterProperties("act_childhood_sharp"),
			new EducationCharacterProperties("act_childhood_genius"),
			new EducationCharacterProperties("act_childhood_fierce")
		};
		(TextObject, TextObject, CharacterAttribute[])[] array3 = stage_1_page_2_options;
		for (int i = 0; i < array3.Length; i++)
		{
			array3[i].Item2.SetTextVariable("PLAYER_CHILD", IsHeroChildOfPlayer(child) ? 1 : 0);
		}
		for (int j = 0; j < stage_1_page_2_options.Length; j++)
		{
			int index = j;
			EducationCharacterProperties childProperties = array2[index];
			EducationCharacterProperties specialCharacterProperties = array[index];
			(TextObject, TextObject, CharacterAttribute[]) container = stage_1_page_2_options[j];
			EducationOption option = new EducationOption(container.Item1, container.Item2, null, delegate(EducationOption o, List<EducationOption> previousOptions)
			{
				int num = previousOptions[0].RandomValue % stage_1_page_2_options.Length;
				int num2 = previousOptions[1].RandomValue % stage_1_page_2_options.Length;
				bool num3 = (num + num2) % stage_1_page_2_options.Length == index;
				if (num3)
				{
					stage_1_page_2_description.SetTextVariable("ATR", container.Item3[0].Name);
				}
				return num3;
			}, null, container.Item3, null, childProperties, specialCharacterProperties);
			educationPage3.AddOption(option);
		}
		return educationStage;
	}

	private EducationStage CreateStage8(Hero child)
	{
		TextObject title = new TextObject("{=CU3u0c02}Childhood");
		EducationStage educationStage = new EducationStage(ChildAgeState.Year8);
		TextObject description = new TextObject("{=lZSu0iOo}{CHILD.NAME} is now at an age when it is customary to assign a well-born child a tutor. You decided to entrust {?CHILD.GENDER}her{?}him{\\?} to your... ");
		EducationPage educationPage = educationStage.AddPage(childProperties: new EducationCharacterProperties("act_inventory_idle_start", string.Empty, useOffHand: false), pageIndex: 0, title: title, description: description, instruction: _chooseTutorText);
		TextObject title2 = new TextObject("{=aZjz2GfX}Master-at-arms");
		TextObject title3 = new TextObject("{=stewardprofession}Steward");
		TextObject title4 = new TextObject("{=qfzkMuLj}Artisan");
		TextObject textObject = new TextObject("{=a869yxLt}{?IS_BATTANIA_OR_KHUZAIT}Lorekeeper{?}Scholar{\\?}");
		textObject.SetTextVariable("IS_BATTANIA_OR_KHUZAIT", ChildCultureHasLorekeeper(child) ? 1 : 0);
		TextObject title5 = new TextObject("{=cpLFQzx0}Huntsman");
		TextObject textObject2 = new TextObject("{=vAjNG9yn}{?IS_BATTANIA}Bard{?}Minstrel{\\?}");
		textObject2.SetTextVariable("IS_BATTANIA", ChildCultureHasBard(child) ? 1 : 0);
		TextObject textObject3 = new TextObject("{=d2KpUKxc}The master-at-arms is responsible for the training and discipline of your troops. With his help {?PLAYER_CHILD}your{?}the{\\?} child should grow to be a strong warrior.");
		TextObject textObject4 = new TextObject("{=rB96005A}The steward is in charge of running the day-to-day affairs of your estate. {?PLAYER_CHILD}Your{?}The{\\?} child should learn early how people and supplies are managed.");
		TextObject textObject5 = new TextObject("{=9sdTLp49}The master artisan supervises the work of any smiths, carpenters or masons that you hire. {?PLAYER_CHILD}Your{?}The{\\?} child will join in the hard work and learn both stamina and craftsmanship.");
		TextObject textObject6 = new TextObject("{=G7yjkafk}The lorekeeper is responsible for teaching children the ancestral knowledge of the clan, including genealogy, law, and medicine. {?PLAYER_CHILD}Your{?}The{\\?} child should gain a respect for learning.");
		TextObject textObject7 = new TextObject("{=XD39Ra5X}The scholar advises a lord on subjects such as history, medicine, heraldry and even engineering. {?PLAYER_CHILD}Your{?}The{\\?} child should gain a respect for learning.");
		TextObject textObject8 = new TextObject("{=!}{INTELLIGENCE_DESC}");
		TextObject description2 = new TextObject("{=gqUtaPRM}The huntsman organizes hunts and keeps watch for poachers and thieves. Your child should learn how to handle hunting bows and crossbows and the basics of scouting and tracking.");
		TextObject textObject9 = new TextObject("{=yHB4plew}The {?IS_BATTANIA}bard{?}minstrel{\\?} sings and plays the lute, shawm or the vielle, and chants epic poems of daring deeds and impossible romances. They are also known to show their wards a bit about the seemier side of life.");
		textObject9.SetTextVariable("IS_BATTANIA", ChildCultureHasBard(child) ? 1 : 0);
		TextObject[] array = new TextObject[5] { textObject3, textObject4, textObject5, textObject8, textObject6 };
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetTextVariable("PLAYER_CHILD", IsHeroChildOfPlayer(child) ? 1 : 0);
		}
		bool flag = ChildCultureHasLorekeeper(child);
		textObject8.SetTextVariable("INTELLIGENCE_DESC", flag ? textObject6 : textObject7);
		EducationCharacterProperties childProperties = new EducationCharacterProperties("act_childhood_arms_2", "training_sword", useOffHand: false);
		EducationCharacterProperties specialCharacterProperties = new EducationCharacterProperties("act_childhood_closed_tutor");
		EducationCharacterProperties childProperties2 = new EducationCharacterProperties("act_childhood_manners");
		EducationCharacterProperties specialCharacterProperties2 = new EducationCharacterProperties("act_childhood_confident2_tutor");
		EducationCharacterProperties childProperties3 = new EducationCharacterProperties("act_childhood_artisan", "carry_linen", useOffHand: false);
		EducationCharacterProperties specialCharacterProperties3 = new EducationCharacterProperties("act_childhood_confident_tutor");
		EducationCharacterProperties childProperties4 = new EducationCharacterProperties("act_childhood_book", "character_creation_notebook", useOffHand: true);
		EducationCharacterProperties specialCharacterProperties4 = new EducationCharacterProperties("act_childhood_demure_tutor");
		EducationCharacterProperties childProperties5 = new EducationCharacterProperties("act_childhood_ready_bow", "bow_roman_a", useOffHand: true);
		EducationCharacterProperties specialCharacterProperties5 = new EducationCharacterProperties("act_childhood_hip_tutor");
		EducationCharacterProperties childProperties6 = new EducationCharacterProperties("act_childhood_leader");
		EducationCharacterProperties specialCharacterProperties6 = new EducationCharacterProperties("act_childhood_sharp", "carry_guitar", useOffHand: true);
		EducationOption stage_2_page_0_option_masterAtArms = new EducationOption(title2, textObject3, null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Vigor }, null, childProperties, specialCharacterProperties);
		EducationOption stage_2_page_0_option_steward = new EducationOption(title3, textObject4, null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Social }, null, childProperties2, specialCharacterProperties2);
		EducationOption stage_2_page_0_option_artisan = new EducationOption(title4, textObject5, null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Endurance }, null, childProperties3, specialCharacterProperties3);
		EducationOption stage_2_page_0_option_intelligence = new EducationOption(textObject, textObject8, null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Intelligence }, null, childProperties4, specialCharacterProperties4);
		EducationOption stage_2_page_0_option_huntsman = new EducationOption(title5, description2, null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Control }, null, childProperties5, specialCharacterProperties5);
		EducationOption stage_2_page_0_option_cunning = new EducationOption(textObject2, textObject9, null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Cunning }, null, childProperties6, specialCharacterProperties6);
		educationPage.AddOption(stage_2_page_0_option_masterAtArms);
		educationPage.AddOption(stage_2_page_0_option_steward);
		educationPage.AddOption(stage_2_page_0_option_artisan);
		educationPage.AddOption(stage_2_page_0_option_intelligence);
		educationPage.AddOption(stage_2_page_0_option_huntsman);
		educationPage.AddOption(stage_2_page_0_option_cunning);
		TextObject description3 = new TextObject("{=ZjWDqx2Y}You trusted the child of the clan to the master-at-arms, an experienced warrior who no longer rides to the battlefield but has forgotten none of his skills. You had him to focus on...");
		EducationPage educationPage2 = educationStage.AddPage(1, title, description3, _guideTutorText, default(EducationCharacterProperties), default(EducationCharacterProperties), (EducationPage page, List<EducationOption> previousOptions) => previousOptions.Contains(stage_2_page_0_option_masterAtArms));
		EducationCharacterProperties childProperties7 = new EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", useOffHand: true);
		EducationCharacterProperties childProperties8 = new EducationCharacterProperties("act_childhood_guard_up_staff", "carry_bostaff", useOffHand: false);
		EducationCharacterProperties childProperties9 = new EducationCharacterProperties("act_childhood_focus", "training_sword", useOffHand: false);
		EducationCharacterProperties childProperties10 = new EducationCharacterProperties("act_childhood_fierce");
		EducationCharacterProperties childProperties11 = new EducationCharacterProperties("act_childhood_arms_2", "training_sword", useOffHand: false);
		EducationCharacterProperties childProperties12 = new EducationCharacterProperties("act_childhood_ready_bow", "bow_roman_a", useOffHand: true);
		EducationCharacterProperties specialCharacterProperties7 = new EducationCharacterProperties("act_childhood_closed_tutor");
		EducationCharacterProperties specialCharacterProperties8 = new EducationCharacterProperties("act_childhood_closed_tutor");
		EducationCharacterProperties specialCharacterProperties9 = new EducationCharacterProperties("act_childhood_closed_tutor");
		EducationCharacterProperties specialCharacterProperties10 = new EducationCharacterProperties("act_childhood_closed_tutor");
		EducationCharacterProperties specialCharacterProperties11 = new EducationCharacterProperties("act_childhood_closed_tutor");
		EducationCharacterProperties specialCharacterProperties12 = new EducationCharacterProperties("act_childhood_closed_tutor");
		educationPage2.AddOption(new EducationOption(DefaultSkills.Riding.Name, new TextObject("{=K08ed2LS}You asked your master-at-arms to make sure that as a noble, this {?CHILD.GENDER}daughter{?}son{\\?} of the clan knows how to hold {?CHILD.GENDER}herself{?}himself{\\?} properly on the saddle."), null, null, null, null, new SkillObject[1] { DefaultSkills.Riding }, childProperties7, specialCharacterProperties7));
		educationPage2.AddOption(new EducationOption(DefaultSkills.Polearm.Name, new TextObject("{=LE0NCSPP}Every warrior needs to know the basics. Polearms are used by warriors of all classes, from feudal levies and urban militias to elite lancers."), null, null, null, null, new SkillObject[1] { DefaultSkills.Polearm }, childProperties8, specialCharacterProperties8));
		educationPage2.AddOption(new EducationOption(DefaultSkills.OneHanded.Name, new TextObject("{=PpuRM82X}The sword is the weapon of the noble warrior. It is useful both in the battlefield and in court, where it symbolizes a wearer's willingness to fight to protect {?CHILD.GENDER}her{?}his{\\?} honor."), null, null, null, null, new SkillObject[1] { DefaultSkills.OneHanded }, childProperties9, specialCharacterProperties9));
		educationPage2.AddOption(new EducationOption(DefaultSkills.Athletics.Name, new TextObject("{=C5UrdSgj}Fast legs and stamina are as vital to a warrior's survival as {?CHILD.GENDER}her{?}his{\\?} strength and skill."), null, null, null, null, new SkillObject[1] { DefaultSkills.Athletics }, childProperties10, specialCharacterProperties10));
		educationPage2.AddOption(new EducationOption(DefaultSkills.TwoHanded.Name, new TextObject("{=Adah5bay}The most powerful weapons require years of practice and conditioning to use properly. Their wielders need to start early."), null, null, null, null, new SkillObject[1] { DefaultSkills.TwoHanded }, childProperties11, specialCharacterProperties11));
		educationPage2.AddOption(new EducationOption(DefaultSkills.Bow.Name, new TextObject("{=QakanPLW}It will be years before the child can develop the strength, breathing techniques, and patience needed to wield a powerful bow, but it never hurts to start early."), null, null, null, null, new SkillObject[1] { DefaultSkills.Bow }, childProperties12, specialCharacterProperties12));
		foreach (EducationOption option in educationPage2.Options)
		{
			option.Description.SetTextVariable("PLAYER_CHILD", IsHeroChildOfPlayer(child) ? 1 : 0);
		}
		TextObject description4 = new TextObject("{=BTdTSzxF}You felt that your child should learn first and foremost how to manage the family property and govern your retainers.");
		EducationPage educationPage3 = educationStage.AddPage(1, title, description4, _guideTutorText, default(EducationCharacterProperties), default(EducationCharacterProperties), (EducationPage page, List<EducationOption> previousOptions) => previousOptions.Contains(stage_2_page_0_option_steward));
		EducationCharacterProperties childProperties13 = new EducationCharacterProperties("act_childhood_numbers");
		EducationCharacterProperties childProperties14 = new EducationCharacterProperties("act_childhood_memory");
		EducationCharacterProperties childProperties15 = new EducationCharacterProperties("act_childhood_manners_3");
		EducationCharacterProperties childProperties16 = new EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", useOffHand: true);
		EducationCharacterProperties childProperties17 = new EducationCharacterProperties("act_childhood_leader");
		EducationCharacterProperties childProperties18 = new EducationCharacterProperties("act_childhood_decisive");
		EducationCharacterProperties specialCharacterProperties13 = new EducationCharacterProperties("act_childhood_confident2_tutor");
		EducationCharacterProperties specialCharacterProperties14 = new EducationCharacterProperties("act_childhood_confident2_tutor");
		EducationCharacterProperties specialCharacterProperties15 = new EducationCharacterProperties("act_childhood_confident2_tutor");
		EducationCharacterProperties specialCharacterProperties16 = new EducationCharacterProperties("act_childhood_confident2_tutor");
		EducationCharacterProperties specialCharacterProperties17 = new EducationCharacterProperties("act_childhood_confident2_tutor");
		EducationCharacterProperties specialCharacterProperties18 = new EducationCharacterProperties("act_childhood_confident2_tutor");
		educationPage3.AddOption(new EducationOption(DefaultSkills.Steward.Name, new TextObject("{=XsQ5rabf}The first thing that a steward must know is to how to count add numbers together. "), null, null, null, null, new SkillObject[1] { DefaultSkills.Steward }, childProperties13, specialCharacterProperties13));
		educationPage3.AddOption(new EducationOption(DefaultSkills.Trade.Name, new TextObject("{=T0LaDdAs}It is important that the child of the clan learns about the goods and their prices, that kind of understanding will be useful for all {?CHILD.GENDER}her{?}his{\\?} life."), null, null, null, null, new SkillObject[1] { DefaultSkills.Trade }, childProperties14, specialCharacterProperties14));
		educationPage3.AddOption(new EducationOption(DefaultSkills.Charm.Name, new TextObject("{=pHmz4GZN}Everything is easier when people are pleased by your presence. Proper grace and etiquette will be useful even among enemies."), null, null, null, null, new SkillObject[1] { DefaultSkills.Charm }, childProperties15, specialCharacterProperties15));
		educationPage3.AddOption(new EducationOption(DefaultSkills.Riding.Name, new TextObject("{=hB72eE6b}Horses are among your clan's most valuable assets, so you encouraged your steward to take the child along on his frequent inspections of the stables."), null, null, null, null, new SkillObject[1] { DefaultSkills.Riding }, childProperties16, specialCharacterProperties16));
		educationPage3.AddOption(new EducationOption(DefaultSkills.Leadership.Name, new TextObject("{=N1WyvVHY}You instructed the child to pay close attention to how the steward exerts authority."), null, null, null, null, new SkillObject[1] { DefaultSkills.Leadership }, childProperties17, specialCharacterProperties17));
		educationPage3.AddOption(new EducationOption(DefaultSkills.Roguery.Name, new TextObject("{=SA4JbVo2}Your steward acts as magistrate in the lord's absence. {?PLAYER_CHILD}Your{?}The{\\?} child will sit at his side as he rules on land disputes, family feuds, and cases of alleged banditry, giving {?CHILD.GENDER}her{?}him{\\?} a look at the darker side of life in Calradia."), null, null, null, null, new SkillObject[1] { DefaultSkills.Roguery }, childProperties18, specialCharacterProperties18));
		foreach (EducationOption option2 in educationPage3.Options)
		{
			option2.Description.SetTextVariable("PLAYER_CHILD", IsHeroChildOfPlayer(child) ? 1 : 0);
		}
		TextObject description5 = new TextObject("{=6apaXF8k}You wanted this child to learn most from those who worked with their hands for a living. You asked an artisan in your employ to teach {?CHILD.GENDER}her{?}him{\\?}...");
		EducationPage educationPage4 = educationStage.AddPage(1, title, description5, _guideTutorText, default(EducationCharacterProperties), default(EducationCharacterProperties), (EducationPage page, List<EducationOption> previousOptions) => previousOptions.Contains(stage_2_page_0_option_artisan));
		EducationCharacterProperties childProperties19 = new EducationCharacterProperties("act_childhood_grit", "carry_hammer", useOffHand: false);
		EducationCharacterProperties childProperties20 = new EducationCharacterProperties("act_childhood_memory");
		EducationCharacterProperties childProperties21 = new EducationCharacterProperties("act_childhood_arms", "training_sword", useOffHand: false);
		EducationCharacterProperties childProperties22 = new EducationCharacterProperties("act_childhood_ready", "carry_book_left", useOffHand: true);
		EducationCharacterProperties childProperties23 = new EducationCharacterProperties("act_childhood_manners");
		EducationCharacterProperties childProperties24 = new EducationCharacterProperties("act_childhood_militia", "carry_hammer", useOffHand: false);
		EducationCharacterProperties specialCharacterProperties19 = new EducationCharacterProperties("act_childhood_confident_tutor");
		EducationCharacterProperties specialCharacterProperties20 = new EducationCharacterProperties("act_childhood_confident_tutor");
		EducationCharacterProperties specialCharacterProperties21 = new EducationCharacterProperties("act_childhood_confident_tutor");
		EducationCharacterProperties specialCharacterProperties22 = new EducationCharacterProperties("act_childhood_confident_tutor");
		EducationCharacterProperties specialCharacterProperties23 = new EducationCharacterProperties("act_childhood_confident_tutor");
		EducationCharacterProperties specialCharacterProperties24 = new EducationCharacterProperties("act_childhood_confident_tutor");
		educationPage4.AddOption(new EducationOption(DefaultSkills.Crafting.Name, new TextObject("{=bx6Jhhui}The artisan should make sure that the child knows the basics of smithing, fletching and pole-turning, and that men who work with their hands take pride in their work."), null, null, null, null, new SkillObject[1] { DefaultSkills.Crafting }, childProperties19, specialCharacterProperties19));
		educationPage4.AddOption(new EducationOption(DefaultSkills.Trade.Name, new TextObject("{=xHstOUBx}You made sure your artisan taught this child to recognize quality work, as well as its worth in denars."), null, null, null, null, new SkillObject[1] { DefaultSkills.Trade }, childProperties20, specialCharacterProperties20));
		educationPage4.AddOption(new EducationOption(DefaultSkills.OneHanded.Name, new TextObject("{=Ir7BP5bD}The best smiths are those that understand how their tools are used. Your child will be tasked with sparring with the weapons and testing their edges. "), null, null, null, null, new SkillObject[1] { DefaultSkills.OneHanded }, childProperties21, specialCharacterProperties21));
		educationPage4.AddOption(new EducationOption(DefaultSkills.Engineering.Name, new TextObject("{=N0eHkPV2}A craftsman makes parts and an engineer fits them together. The best artisans learn to do both."), null, null, null, null, new SkillObject[1] { DefaultSkills.Engineering }, childProperties22, specialCharacterProperties22));
		educationPage4.AddOption(new EducationOption(DefaultSkills.Steward.Name, new TextObject("{=tyzzUNN7}You instructed the artisan to teach your the child the basics of managing a workshop."), null, null, null, null, new SkillObject[1] { DefaultSkills.Steward }, childProperties23, specialCharacterProperties23));
		educationPage4.AddOption(new EducationOption(DefaultSkills.Athletics.Name, new TextObject("{=biqBLGwa}You told the artisan to make sure that {?PLAYER_CHILD}your{?}the{\\?} child works hard, and that {?CHILD.GENDER}she{?}he{\\?} swings the hammer for hours in the blazing heat of the forge. It tempers the soul, you believe."), null, null, null, null, new SkillObject[1] { DefaultSkills.Athletics }, childProperties24, specialCharacterProperties24));
		foreach (EducationOption option3 in educationPage4.Options)
		{
			option3.Description.SetTextVariable("PLAYER_CHILD", IsHeroChildOfPlayer(child) ? 1 : 0);
		}
		TextObject textObject10 = new TextObject("{=pb8fmbVl}You asked your {?IS_BATTANIA_OR_KHUZAIT}lorekeeper{?}scholar{\\?} to focus particularly on the art of...");
		textObject10.SetTextVariable("IS_BATTANIA_OR_KHUZAIT", ChildCultureHasLorekeeper(child) ? 1 : 0);
		EducationPage educationPage5 = educationStage.AddPage(1, title, textObject10, _guideTutorText, default(EducationCharacterProperties), default(EducationCharacterProperties), (EducationPage page, List<EducationOption> previousOptions) => previousOptions.Contains(stage_2_page_0_option_intelligence));
		EducationCharacterProperties childProperties25 = new EducationCharacterProperties("act_childhood_manners_3", "carry_book", useOffHand: false);
		EducationCharacterProperties childProperties26 = new EducationCharacterProperties("act_childhood_clever_2", "carry_scroll", useOffHand: false);
		EducationCharacterProperties childProperties27 = new EducationCharacterProperties("act_childhood_leader");
		EducationCharacterProperties childProperties28 = new EducationCharacterProperties("act_childhood_manners");
		EducationCharacterProperties childProperties29 = new EducationCharacterProperties("act_childhood_appearances");
		EducationCharacterProperties childProperties30 = new EducationCharacterProperties("act_childhood_tactician");
		EducationCharacterProperties specialCharacterProperties25 = new EducationCharacterProperties("act_childhood_demure_tutor");
		EducationCharacterProperties specialCharacterProperties26 = new EducationCharacterProperties("act_childhood_demure_tutor");
		EducationCharacterProperties specialCharacterProperties27 = new EducationCharacterProperties("act_childhood_demure_tutor");
		EducationCharacterProperties specialCharacterProperties28 = new EducationCharacterProperties("act_childhood_demure_tutor");
		EducationCharacterProperties specialCharacterProperties29 = new EducationCharacterProperties("act_childhood_demure_tutor");
		EducationCharacterProperties specialCharacterProperties30 = new EducationCharacterProperties("act_childhood_demure_tutor");
		educationPage5.AddOption(new EducationOption(DefaultSkills.Medicine.Name, new TextObject("{=5pGCtBhd}You asked that your child be schooled over the next few years in all the treatises that can be found on human body, its ailments, and their treatments."), null, null, null, null, new SkillObject[1] { DefaultSkills.Medicine }, childProperties25, specialCharacterProperties25));
		educationPage5.AddOption(new EducationOption(DefaultSkills.Engineering.Name, new TextObject("{=CtImGA4A}Most tutors teach mathematics from tracing the path of stars in the sky, but you wanted your {?IS_BATTANIA_OR_KHUZAIT}lorekeeper{?}scholar{\\?} to focus on the more practical architectural treatises."), null, null, null, null, new SkillObject[1] { DefaultSkills.Engineering }, childProperties26, specialCharacterProperties26));
		educationPage5.AddOption(new EducationOption(DefaultSkills.Leadership.Name, new TextObject("{=Nn8bmGUX}You make sure that the child would be taught the deeds of the leaders of your people, and memorize the rhetoric they used to inspire their followers."), null, null, null, null, new SkillObject[1] { DefaultSkills.Leadership }, childProperties27, specialCharacterProperties27));
		educationPage5.AddOption(new EducationOption(DefaultSkills.Charm.Name, new TextObject("{=MgPubWOa}The epics are full of men and women who preferred words to the sword, and who could win the friendship of allies and the admiration of enemies."), null, null, null, null, new SkillObject[1] { DefaultSkills.Charm }, childProperties28, specialCharacterProperties28));
		educationPage5.AddOption(new EducationOption(DefaultSkills.Steward.Name, new TextObject("{=c4NPLD0S}Any child of a noble clan needs to know how some kings and emperors accumulated wealth and how others squandered it."), null, null, null, null, new SkillObject[1] { DefaultSkills.Steward }, childProperties29, specialCharacterProperties29));
		educationPage5.AddOption(new EducationOption(DefaultSkills.Tactics.Name, new TextObject("{=3iED3Ca9}To craft a tactic one must first have an idea of what has worked in the past and what has failed, and any commander should know the course and outcome of as many battles as possible."), null, null, null, null, new SkillObject[1] { DefaultSkills.Tactics }, childProperties30, specialCharacterProperties30));
		foreach (EducationOption option4 in educationPage5.Options)
		{
			option4.Description.SetTextVariable("IS_BATTANIA_OR_KHUZAIT", ChildCultureHasLorekeeper(child) ? 1 : 0);
			option4.Description.SetTextVariable("PLAYER_CHILD", IsHeroChildOfPlayer(child) ? 1 : 0);
		}
		TextObject description6 = new TextObject("{=ed5JVSQm}You asked the huntsman to spend as much time as possible outdoors, accustomizing the child to the dangers and hardships of the wild. You asked him to teach the child...");
		EducationPage educationPage6 = educationStage.AddPage(1, title, description6, _guideTutorText, default(EducationCharacterProperties), default(EducationCharacterProperties), (EducationPage page, List<EducationOption> previousOptions) => previousOptions.Contains(stage_2_page_0_option_huntsman));
		EducationCharacterProperties childProperties31 = new EducationCharacterProperties("act_childhood_leader");
		EducationCharacterProperties childProperties32 = new EducationCharacterProperties("act_childhood_ready_bow", "bow_roman_a", useOffHand: true);
		EducationCharacterProperties childProperties33 = new EducationCharacterProperties("act_childhood_guard_up_staff", "carry_bostaff", useOffHand: false);
		EducationCharacterProperties childProperties34 = new EducationCharacterProperties("act_childhood_manners_3", "carry_book", useOffHand: false);
		EducationCharacterProperties childProperties35 = new EducationCharacterProperties("act_childhood_roguery", "carry_bostaff_rogue1", useOffHand: false);
		EducationCharacterProperties childProperties36 = new EducationCharacterProperties("act_childhood_tactician");
		EducationCharacterProperties specialCharacterProperties31 = new EducationCharacterProperties("act_childhood_hip_tutor");
		EducationCharacterProperties specialCharacterProperties32 = new EducationCharacterProperties("act_childhood_hip_tutor");
		EducationCharacterProperties specialCharacterProperties33 = new EducationCharacterProperties("act_childhood_hip_tutor");
		EducationCharacterProperties specialCharacterProperties34 = new EducationCharacterProperties("act_childhood_hip_tutor");
		EducationCharacterProperties specialCharacterProperties35 = new EducationCharacterProperties("act_childhood_hip_tutor");
		EducationCharacterProperties specialCharacterProperties36 = new EducationCharacterProperties("act_childhood_hip_tutor");
		educationPage6.AddOption(new EducationOption(DefaultSkills.Scouting.Name, new TextObject("{=lbHswWdb}Hunting is all about being aware of your surroundings and tracking your prey. You felt that the same skills would serve a lord well in journeys across the wild."), null, null, null, null, new SkillObject[1] { DefaultSkills.Scouting }, childProperties31, specialCharacterProperties31));
		educationPage6.AddOption(new EducationOption(DefaultSkills.Bow.Name, new TextObject("{=ItS9F61H}You told {CHILD.NAME} that any young noble worth {?CHILD.GENDER}her{?}his{\\?} salt should be able to bring down a deer from fifty paces away, or hit a rabbit on the run."), null, null, null, null, new SkillObject[1] { DefaultSkills.Bow }, childProperties32, specialCharacterProperties32));
		educationPage6.AddOption(new EducationOption(DefaultSkills.Polearm.Name, new TextObject("{=bZlhwK0a}Bows are all very well for small game, but those who pursue bears, boar or wolves bring spears along."), null, null, null, null, new SkillObject[1] { DefaultSkills.Polearm }, childProperties33, specialCharacterProperties33));
		educationPage6.AddOption(new EducationOption(DefaultSkills.Medicine.Name, new TextObject("{=rPP0OSBm}Treating sprained ankles, broken bones and lamed horses are as much a part of a hunting expedition as skinning and gutting your quarry."), null, null, null, null, new SkillObject[1] { DefaultSkills.Medicine }, childProperties34, specialCharacterProperties34));
		educationPage6.AddOption(new EducationOption(DefaultSkills.Athletics.Name, new TextObject("{=EbRDo6Lv}A hunter should be able to endure long treks over rough ground and pursue a wounded quarry until it drops."), null, null, null, null, new SkillObject[1] { DefaultSkills.Athletics }, childProperties35, specialCharacterProperties35));
		educationPage6.AddOption(new EducationOption(DefaultSkills.Tactics.Name, new TextObject("{=b9nvCgYF}A hunter knows how to prepare a trap and how to draw a prey to it, and, when dangerous predators are about, how to avoid becoming the hunted."), null, null, null, null, new SkillObject[1] { DefaultSkills.Tactics }, childProperties36, specialCharacterProperties36));
		foreach (EducationOption option5 in educationPage6.Options)
		{
			option5.Description.SetTextVariable("PLAYER_CHILD", IsHeroChildOfPlayer(child) ? 1 : 0);
		}
		TextObject textObject11 = new TextObject("{=CZuYne8X}Singing, dancing and playing an instrument is important but the {?IS_BATTANIA}bard{?}minstrel{\\?} knows more. You asked him to make sure the child is also skilled in...");
		textObject11.SetTextVariable("IS_BATTANIA", ChildCultureHasBard(child) ? 1 : 0);
		EducationPage educationPage7 = educationStage.AddPage(1, title, textObject11, _guideTutorText, default(EducationCharacterProperties), default(EducationCharacterProperties), (EducationPage page, List<EducationOption> previousOptions) => previousOptions.Contains(stage_2_page_0_option_cunning));
		EducationCharacterProperties childProperties37 = new EducationCharacterProperties("act_childhood_manners");
		EducationCharacterProperties childProperties38 = new EducationCharacterProperties("act_childhood_fierce");
		EducationCharacterProperties childProperties39 = new EducationCharacterProperties("act_childhood_leader");
		EducationCharacterProperties childProperties40 = new EducationCharacterProperties("act_childhood_leader");
		EducationCharacterProperties childProperties41 = new EducationCharacterProperties("act_childhood_ready_throw", "spear_new_f_1-9m", useOffHand: false);
		EducationCharacterProperties childProperties42 = new EducationCharacterProperties("act_childhood_roguery", "carry_bostaff_rogue1", useOffHand: false);
		EducationCharacterProperties specialCharacterProperties37 = new EducationCharacterProperties("act_childhood_sharp", "carry_guitar", useOffHand: true);
		EducationCharacterProperties specialCharacterProperties38 = new EducationCharacterProperties("act_childhood_sharp", "carry_guitar", useOffHand: true);
		EducationCharacterProperties specialCharacterProperties39 = new EducationCharacterProperties("act_childhood_sharp", "carry_guitar", useOffHand: true);
		EducationCharacterProperties specialCharacterProperties40 = new EducationCharacterProperties("act_childhood_sharp", "carry_guitar", useOffHand: true);
		EducationCharacterProperties specialCharacterProperties41 = new EducationCharacterProperties("act_childhood_sharp", "carry_guitar", useOffHand: true);
		EducationCharacterProperties specialCharacterProperties42 = new EducationCharacterProperties("act_childhood_sharp", "carry_guitar", useOffHand: true);
		educationPage7.AddOption(new EducationOption(DefaultSkills.Charm.Name, new TextObject("{=fxDr44K4}A silvered tongue gets you far. You want {CHILD.NAME} to learn how to ferret out people's motivations, flatter their egos, and convince them that their interests are aligned."), null, null, null, null, new SkillObject[1] { DefaultSkills.Charm }, childProperties37, specialCharacterProperties37));
		educationPage7.AddOption(new EducationOption(DefaultSkills.Athletics.Name, new TextObject("{=9m5M3Yf9}Some nobles may consider it vulgar and common, but the art of acrobatics is demanding and often has very practical applications."), null, null, null, null, new SkillObject[1] { DefaultSkills.Athletics }, childProperties38, specialCharacterProperties38));
		educationPage7.AddOption(new EducationOption(DefaultSkills.Scouting.Name, new TextObject("{=dqg4nlo9}{?IS_BATTANIA}Bard{?}Minstrel{\\?}s frequently sing of nature and the hunt and are often great travellers, skilled at living rough on the land. The child was encouraged to accompany the {?IS_BATTANIA}bard{?}minstrel{\\?} on his roamings."), null, null, null, null, new SkillObject[1] { DefaultSkills.Scouting }, childProperties39, specialCharacterProperties39));
		educationPage7.AddOption(new EducationOption(DefaultSkills.Leadership.Name, new TextObject("{=KC7rLf4b}The {?IS_BATTANIA}bard{?}minstrel{\\?} had the child memorize the speeches of epic heroes, teaching {?CHILD.GENDER}her{?}him{\\?} how to appeal to your people's pride in their ancestors."), null, null, null, null, new SkillObject[1] { DefaultSkills.Leadership }, childProperties40, specialCharacterProperties40));
		educationPage7.AddOption(new EducationOption(DefaultSkills.Throwing.Name, new TextObject("{=1oPRugXl}Trick knife throws and swift evasions can entertain a marketplace crowd, but are useful on the battlefield as well. "), null, null, null, null, new SkillObject[1] { DefaultSkills.Throwing }, childProperties41, specialCharacterProperties41));
		educationPage7.AddOption(new EducationOption(DefaultSkills.Roguery.Name, new TextObject("{=XDafe4Bg}The {?IS_BATTANIA}bard{?}minstrel{\\?} will take the child along on his adventures in town and you've indicated that you'll turn a blind eye, considering it a useful skill to know how to get in and out of trouble."), null, null, null, null, new SkillObject[1] { DefaultSkills.Roguery }, childProperties42, specialCharacterProperties42));
		foreach (EducationOption option6 in educationPage7.Options)
		{
			option6.Description.SetTextVariable("PLAYER_CHILD", IsHeroChildOfPlayer(child) ? 1 : 0);
			option6.Description.SetTextVariable("IS_BATTANIA", ChildCultureHasBard(child) ? 1 : 0);
		}
		TextObject description7 = new TextObject("{=KbnGyw0v}Your master-at-arms is happy with the child's progress. He informs you that {?CHILD.GENDER}she{?}he{\\?} shows some natural talent in {SKILL}.");
		EducationPage currentPage = educationStage.AddPage(2, title, description7, _confirmResultsText, default(EducationCharacterProperties), default(EducationCharacterProperties), (EducationPage page, List<EducationOption> previousOptions) => previousOptions.Contains(stage_2_page_0_option_masterAtArms));
		List<SkillObject> skills = new List<SkillObject>
		{
			DefaultSkills.Riding,
			DefaultSkills.Athletics,
			DefaultSkills.Bow,
			DefaultSkills.Polearm,
			DefaultSkills.TwoHanded,
			DefaultSkills.OneHanded
		};
		EducationCharacterProperties[] childProperties43 = new EducationCharacterProperties[6]
		{
			new EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", useOffHand: true),
			new EducationCharacterProperties("act_childhood_athlete"),
			new EducationCharacterProperties("act_childhood_ready_bow", "bow_roman_a", useOffHand: true),
			new EducationCharacterProperties("act_childhood_guard_up_staff", "carry_bostaff", useOffHand: false),
			new EducationCharacterProperties("act_childhood_arms_2", "training_sword", useOffHand: false),
			new EducationCharacterProperties("act_childhood_focus", "training_sword", useOffHand: false)
		};
		EducationCharacterProperties[] tutorProperties = new EducationCharacterProperties[6]
		{
			new EducationCharacterProperties("act_childhood_closed_tutor"),
			new EducationCharacterProperties("act_childhood_closed_tutor"),
			new EducationCharacterProperties("act_childhood_closed_tutor"),
			new EducationCharacterProperties("act_childhood_closed_tutor"),
			new EducationCharacterProperties("act_childhood_closed_tutor"),
			new EducationCharacterProperties("act_childhood_closed_tutor")
		};
		Stage2Selection(skills, educationPage2, currentPage, childProperties43, tutorProperties);
		TextObject description8 = new TextObject("{=GvSVxyO0}Your steward is happy with the child's progress. He informs you that {?CHILD.GENDER}she{?}he{\\?} shows some natural talent in {SKILL}.");
		EducationPage currentPage2 = educationStage.AddPage(2, title, description8, _confirmResultsText, default(EducationCharacterProperties), default(EducationCharacterProperties), (EducationPage page, List<EducationOption> previousOptions) => previousOptions.Contains(stage_2_page_0_option_steward));
		List<SkillObject> skills2 = new List<SkillObject>
		{
			DefaultSkills.Riding,
			DefaultSkills.Steward,
			DefaultSkills.Trade,
			DefaultSkills.Charm,
			DefaultSkills.Leadership,
			DefaultSkills.Roguery
		};
		EducationCharacterProperties[] childProperties44 = new EducationCharacterProperties[6]
		{
			new EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", useOffHand: true),
			new EducationCharacterProperties("act_childhood_manners_3"),
			new EducationCharacterProperties("act_childhood_genius"),
			new EducationCharacterProperties("act_childhood_manners"),
			new EducationCharacterProperties("act_childhood_leader_2"),
			new EducationCharacterProperties("act_childhood_roguery", "carry_bostaff_rogue1", useOffHand: false)
		};
		EducationCharacterProperties[] tutorProperties2 = new EducationCharacterProperties[6]
		{
			new EducationCharacterProperties("act_childhood_confident2_tutor"),
			new EducationCharacterProperties("act_childhood_confident2_tutor"),
			new EducationCharacterProperties("act_childhood_confident2_tutor"),
			new EducationCharacterProperties("act_childhood_confident2_tutor"),
			new EducationCharacterProperties("act_childhood_confident2_tutor"),
			new EducationCharacterProperties("act_childhood_confident2_tutor")
		};
		Stage2Selection(skills2, educationPage3, currentPage2, childProperties44, tutorProperties2);
		TextObject description9 = new TextObject("{=kwgCnueo}Your master artisan is happy with the child's progress. He informs you that {?CHILD.GENDER}she{?}he{\\?} shows some natural talent in {SKILL}.");
		EducationPage currentPage3 = educationStage.AddPage(2, title, description9, _confirmResultsText, default(EducationCharacterProperties), default(EducationCharacterProperties), (EducationPage page, List<EducationOption> previousOptions) => previousOptions.Contains(stage_2_page_0_option_artisan));
		List<SkillObject> skills3 = new List<SkillObject>
		{
			DefaultSkills.Crafting,
			DefaultSkills.OneHanded,
			DefaultSkills.Trade,
			DefaultSkills.Engineering,
			DefaultSkills.Steward,
			DefaultSkills.Athletics
		};
		EducationCharacterProperties[] childProperties45 = new EducationCharacterProperties[6]
		{
			new EducationCharacterProperties("act_childhood_artisan", "carry_linen", useOffHand: false),
			new EducationCharacterProperties("act_childhood_focus", "training_sword", useOffHand: false),
			new EducationCharacterProperties("act_childhood_genius"),
			new EducationCharacterProperties("act_childhood_grit", "carry_hammer", useOffHand: false),
			new EducationCharacterProperties("act_childhood_manners"),
			new EducationCharacterProperties("act_childhood_athlete")
		};
		EducationCharacterProperties[] tutorProperties3 = new EducationCharacterProperties[6]
		{
			new EducationCharacterProperties("act_childhood_confident_tutor"),
			new EducationCharacterProperties("act_childhood_confident_tutor"),
			new EducationCharacterProperties("act_childhood_confident_tutor"),
			new EducationCharacterProperties("act_childhood_confident_tutor"),
			new EducationCharacterProperties("act_childhood_confident_tutor"),
			new EducationCharacterProperties("act_childhood_confident_tutor")
		};
		Stage2Selection(skills3, educationPage4, currentPage3, childProperties45, tutorProperties3);
		TextObject textObject12 = new TextObject("{=gp1qYbgb}Your {?IS_BATTANIA_OR_KHUZAIT}lorekeeper{?}scholar{\\?} is happy with the child's progress. He informs you that {?CHILD.GENDER}she{?}he{\\?} shows some natural talent in {SKILL}.");
		textObject12.SetTextVariable("IS_BATTANIA_OR_KHUZAIT", ChildCultureHasLorekeeper(child) ? 1 : 0);
		EducationPage educationPage8 = educationStage.AddPage(2, title, textObject12, _confirmResultsText, default(EducationCharacterProperties), default(EducationCharacterProperties), (EducationPage page, List<EducationOption> previousOptions) => previousOptions.Contains(stage_2_page_0_option_intelligence));
		List<SkillObject> skills4 = new List<SkillObject>
		{
			DefaultSkills.Medicine,
			DefaultSkills.Charm,
			DefaultSkills.Tactics,
			DefaultSkills.Engineering,
			DefaultSkills.Steward,
			DefaultSkills.Leadership
		};
		EducationCharacterProperties[] childProperties46 = new EducationCharacterProperties[6]
		{
			new EducationCharacterProperties("act_childhood_ready", "carry_book_left", useOffHand: true),
			new EducationCharacterProperties("act_childhood_manners"),
			new EducationCharacterProperties("act_childhood_tactician"),
			new EducationCharacterProperties("act_childhood_clever_2", "carry_scroll", useOffHand: false),
			new EducationCharacterProperties("act_childhood_manners_3"),
			new EducationCharacterProperties("act_childhood_leader_2")
		};
		EducationCharacterProperties[] tutorProperties4 = new EducationCharacterProperties[6]
		{
			new EducationCharacterProperties("act_childhood_demure_tutor"),
			new EducationCharacterProperties("act_childhood_demure_tutor"),
			new EducationCharacterProperties("act_childhood_demure_tutor"),
			new EducationCharacterProperties("act_childhood_demure_tutor"),
			new EducationCharacterProperties("act_childhood_demure_tutor"),
			new EducationCharacterProperties("act_childhood_demure_tutor")
		};
		Stage2Selection(skills4, educationPage5, educationPage8, childProperties46, tutorProperties4);
		foreach (EducationOption option7 in educationPage8.Options)
		{
			option7.Title.SetTextVariable("IS_BATTANIA_OR_KHUZAIT", ChildCultureHasLorekeeper(child) ? 1 : 0);
			option7.Description.SetTextVariable("IS_BATTANIA_OR_KHUZAIT", ChildCultureHasLorekeeper(child) ? 1 : 0);
		}
		TextObject description10 = new TextObject("{=JGEQ68jc}Your huntsman is happy with the child's progress. He informs you that {?CHILD.GENDER}she{?}he{\\?} shows some natural talent in {SKILL}.");
		EducationPage currentPage4 = educationStage.AddPage(2, title, description10, _confirmResultsText, default(EducationCharacterProperties), default(EducationCharacterProperties), (EducationPage page, List<EducationOption> previousOptions) => previousOptions.Contains(stage_2_page_0_option_huntsman));
		List<SkillObject> skills5 = new List<SkillObject>
		{
			DefaultSkills.Scouting,
			DefaultSkills.Bow,
			DefaultSkills.Polearm,
			DefaultSkills.Medicine,
			DefaultSkills.Athletics,
			DefaultSkills.Tactics
		};
		EducationCharacterProperties[] childProperties47 = new EducationCharacterProperties[6]
		{
			new EducationCharacterProperties("act_childhood_leader"),
			new EducationCharacterProperties("act_childhood_ready_bow", "bow_roman_a", useOffHand: true),
			new EducationCharacterProperties("act_childhood_guard_up_staff", "carry_bostaff", useOffHand: false),
			new EducationCharacterProperties("act_childhood_ready", "carry_book_left", useOffHand: true),
			new EducationCharacterProperties("act_childhood_athlete"),
			new EducationCharacterProperties("act_childhood_tactician")
		};
		EducationCharacterProperties[] tutorProperties5 = new EducationCharacterProperties[6]
		{
			new EducationCharacterProperties("act_childhood_hip_tutor"),
			new EducationCharacterProperties("act_childhood_hip_tutor"),
			new EducationCharacterProperties("act_childhood_hip_tutor"),
			new EducationCharacterProperties("act_childhood_hip_tutor"),
			new EducationCharacterProperties("act_childhood_hip_tutor"),
			new EducationCharacterProperties("act_childhood_hip_tutor")
		};
		Stage2Selection(skills5, educationPage6, currentPage4, childProperties47, tutorProperties5);
		TextObject textObject13 = new TextObject("{=iSppZBje}You don't need to know the full story of the child's escapades in the {?IS_BATTANIA}bard{?}minstrel{\\?}'s company, but the {?IS_BATTANIA}bard{?}minstrel{\\?} does inform you that {?CHILD.GENDER}she{?}he{\\?} shows some natural talent in {SKILL}.");
		textObject13.SetTextVariable("IS_BATTANIA", ChildCultureHasBard(child) ? 1 : 0);
		EducationPage educationPage9 = educationStage.AddPage(2, title, textObject13, _confirmResultsText, default(EducationCharacterProperties), default(EducationCharacterProperties), (EducationPage page, List<EducationOption> previousOptions) => previousOptions.Contains(stage_2_page_0_option_cunning));
		List<SkillObject> skills6 = new List<SkillObject>
		{
			DefaultSkills.Scouting,
			DefaultSkills.Charm,
			DefaultSkills.Leadership,
			DefaultSkills.Throwing,
			DefaultSkills.Athletics,
			DefaultSkills.Roguery
		};
		EducationCharacterProperties[] childProperties48 = new EducationCharacterProperties[6]
		{
			new EducationCharacterProperties("act_childhood_leader"),
			new EducationCharacterProperties("act_childhood_manners"),
			new EducationCharacterProperties("act_childhood_leader_2"),
			new EducationCharacterProperties("act_childhood_ready_throw", "spear_new_f_1-9m", useOffHand: false),
			new EducationCharacterProperties("act_childhood_athlete"),
			new EducationCharacterProperties("act_childhood_roguery", "carry_bostaff_rogue1", useOffHand: false)
		};
		EducationCharacterProperties[] tutorProperties6 = new EducationCharacterProperties[6]
		{
			new EducationCharacterProperties("act_childhood_sharp", "carry_guitar", useOffHand: true),
			new EducationCharacterProperties("act_childhood_sharp", "carry_guitar", useOffHand: true),
			new EducationCharacterProperties("act_childhood_sharp", "carry_guitar", useOffHand: true),
			new EducationCharacterProperties("act_childhood_sharp", "carry_guitar", useOffHand: true),
			new EducationCharacterProperties("act_childhood_sharp", "carry_guitar", useOffHand: true),
			new EducationCharacterProperties("act_childhood_sharp", "carry_guitar", useOffHand: true)
		};
		Stage2Selection(skills6, educationPage7, educationPage9, childProperties48, tutorProperties6);
		foreach (EducationOption option8 in educationPage9.Options)
		{
			option8.Title.SetTextVariable("CUNNING_PROFESSION", ChildCultureHasBard(child) ? 1 : 0);
			option8.Description.SetTextVariable("CUNNING_PROFESSION", ChildCultureHasBard(child) ? 1 : 0);
		}
		return educationStage;
	}

	private EducationStage CreateStage11(Hero child)
	{
		TextObject title = new TextObject("{=ok8lSW6M}Youth");
		EducationStage educationStage = new EducationStage(ChildAgeState.Year11);
		TextObject description = new TextObject("{=Rmbd2OkI}You are usually away from your estate, but when you are able to spend time with {CHILD.NAME}, you encouraged {?CHILD.GENDER}her{?}him{\\?} to develop...");
		EducationPage educationPage = educationStage.AddPage(childProperties: new EducationCharacterProperties("act_inventory_idle_start", string.Empty, useOffHand: false), pageIndex: 0, title: title, description: description, instruction: _chooseFocusText);
		EducationCharacterProperties childProperties = new EducationCharacterProperties("act_childhood_athlete");
		EducationCharacterProperties childProperties2 = new EducationCharacterProperties("act_childhood_fierce");
		EducationCharacterProperties childProperties3 = new EducationCharacterProperties("act_childhood_book", "notebook", useOffHand: true);
		EducationCharacterProperties childProperties4 = new EducationCharacterProperties("act_childhood_tactician");
		EducationCharacterProperties childProperties5 = new EducationCharacterProperties("act_childhood_ready_bow", "bow_roman_a", useOffHand: true);
		EducationCharacterProperties childProperties6 = new EducationCharacterProperties("act_childhood_manners");
		educationPage.AddOption(new EducationOption(new TextObject("{=m4kr4RXD}{?CHILD.GENDER}her{?}his{\\?} strength."), new TextObject("{=QbwNmxrn}You told {?CHILD.GENDER}her{?}him{\\?} that skill and brains are all very well, but in many situations there is no substitute for sheer brute might."), null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Vigor }, null, childProperties));
		educationPage.AddOption(new EducationOption(new TextObject("{=3fvj2Eti}{?CHILD.GENDER}her{?}his{\\?} endurance."), new TextObject("{=GARAmzFT}To thrive you must learn to travel faster, work longer, and fight harder than anyone who stands against you."), null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Endurance }, null, childProperties2));
		educationPage.AddOption(new EducationOption(new TextObject("{=Gitd3DN8}a thirst for knowledge."), new TextObject("{=JHMHcZEp}You have seen many marvels on your travels and heard of many more, and one day, you believe, the philosophers of science will rule the world."), null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Intelligence }, null, childProperties3));
		educationPage.AddOption(new EducationOption(new TextObject("{=7P3wiojT}a sharp mind."), new TextObject("{=78dta9Ys}You told {?CHILD.GENDER}her{?}him{\\?} that it is cheaper, safer and wiser to win a battle without fighting, if it is possible to do so."), null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Cunning }, null, childProperties4));
		educationPage.AddOption(new EducationOption(new TextObject("{=LEoZ2DV5}a good eye and a steady hand."), new TextObject("{=8mk6gwvh}You told {?CHILD.GENDER}her{?}him{\\?} that a good eye can spot dangers from afar, and a steady hand can take them down before they get close."), null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Control }, null, childProperties5));
		educationPage.AddOption(new EducationOption(new TextObject("{=ulFmGvtj}an interest in people."), new TextObject("{=HaAjWFrt}You told {?CHILD.GENDER}her{?}him{\\?} that understanding people's motivations and turning them to your advantage is the difference between a warrior and a king."), null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Social }, null, childProperties6));
		TextObject textObject = new TextObject("{=Y8jAaICu}One day {?PLAYER_CHILD}your{?}the{\\?} child asks you which of your skills was most useful to you. You thought for a while, and answered...");
		textObject.SetTextVariable("PLAYER_CHILD", IsHeroChildOfPlayer(child) ? 1 : 0);
		EducationPage educationPage2 = educationStage.AddPage(1, title, textObject, _chooseSkillText);
		EducationCharacterProperties childProperties7 = new EducationCharacterProperties("act_childhood_guard_up_staff", "carry_bostaff", useOffHand: false);
		EducationCharacterProperties childProperties8 = new EducationCharacterProperties("act_childhood_tactician");
		EducationCharacterProperties childProperties9 = new EducationCharacterProperties("act_childhood_manners");
		EducationCharacterProperties childProperties10 = new EducationCharacterProperties("act_childhood_grit", "carry_hammer", useOffHand: false);
		EducationCharacterProperties childProperties11 = new EducationCharacterProperties("act_childhood_leader_2");
		EducationCharacterProperties childProperties12 = new EducationCharacterProperties("act_childhood_genius");
		educationPage2.AddOption(new EducationOption(DefaultSkills.Polearm.Name, new TextObject("{=3lDTSlpU}Even the most skilled swordsman or the strongest axe-wielder is to a lancer as the rabbit is to the eagle."), null, null, null, null, new SkillObject[1] { DefaultSkills.Polearm }, childProperties7));
		educationPage2.AddOption(new EducationOption(DefaultSkills.Tactics.Name, new TextObject("{=2s6cH2kE}Even the finest swordsman can perish if his lord is ignorant of the difference between necessary risks and reckless, unnecessary ones."), null, null, null, null, new SkillObject[1] { DefaultSkills.Tactics }, childProperties8));
		educationPage2.AddOption(new EducationOption(DefaultSkills.Steward.Name, new TextObject("{=ePNba38W}Armies win battles, but farms and towns win wars."), null, null, null, null, new SkillObject[1] { DefaultSkills.Steward }, childProperties9));
		educationPage2.AddOption(new EducationOption(DefaultSkills.Engineering.Name, new TextObject("{=bnPgkPv2}Towers, domes and gates are wonders that we must never take for granted."), null, null, null, null, new SkillObject[1] { DefaultSkills.Engineering }, childProperties10));
		educationPage2.AddOption(new EducationOption(DefaultSkills.Leadership.Name, new TextObject("{=jqstr7ZL}Learn to fire men's pride, stoke their anger and dispel their fears, and they will turn from men into lions who will do anything for you."), null, null, null, null, new SkillObject[1] { DefaultSkills.Leadership }, childProperties11));
		educationPage2.AddOption(new EducationOption(DefaultSkills.Trade.Name, new TextObject("{=T3vJp4PG}Whoever grasps how prices work can pluck silver out of thin air."), null, null, null, null, new SkillObject[1] { DefaultSkills.Trade }, childProperties12));
		TextObject textObject2 = new TextObject("{=LUro3Sqr}While spending time with {?PLAYER_CHILD}your{?}the{\\?} child, you notice that {?CHILD.GENDER}she{?}he{\\?} shows real ability in {RANDOM_OUTCOME}.");
		textObject2.SetTextVariable("PLAYER_CHILD", IsHeroChildOfPlayer(child) ? 1 : 0);
		EducationPage stage_3_page_2 = educationStage.AddPage(2, title, textObject2, _confirmResultsText);
		(TextObject, TextObject, SkillObject)[] stage_3_page_2_options = new(TextObject, TextObject, SkillObject)[6]
		{
			(DefaultSkills.Athletics.Name, new TextObject("{=tMUla7vz}{?CHILD.GENDER}She{?}He{\\?} is light but strong, and can outrun almost all of {?CHILD.GENDER}her{?}his{\\?} peers."), DefaultSkills.Athletics),
			(DefaultSkills.Riding.Name, new TextObject("{=MdjZ56VR}{?CHILD.GENDER}She{?}He{\\?} sits on the saddle so comfortably, like {?CHILD.GENDER}she{?}he{\\?} was born on it."), DefaultSkills.Riding),
			(DefaultSkills.Crafting.Name, new TextObject("{=OrbOwQGB}Sometimes you spot {?PLAYER_CHILD}your{?}the{\\?} child carving a piece of wood into a face or a fantastic beast, and you could not help but notice the subtle mastery of {?CHILD.GENDER}her{?}his{\\?} hands."), DefaultSkills.Crafting),
			(DefaultSkills.Medicine.Name, new TextObject("{=6EaKsRtu}Accidents are inevitable, and {?PLAYER_CHILD}your{?}the{\\?} child isn't bothered by the sight of blood. {?CHILD.GENDER}She{?}He{\\?} acts quickly to staunch bleeding and prevent infection."), DefaultSkills.Medicine),
			(DefaultSkills.Scouting.Name, new TextObject("{=7Hd5yW7B}{?CHILD.GENDER}She{?}He{\\?} is at home in the wilderness. {?CHILD.GENDER}She{?}He{\\?} moves like a cat, and has very keen ears and eyes."), DefaultSkills.Scouting),
			(DefaultSkills.Charm.Name, new TextObject("{=0FIbRZsi}You can not help but notice how people are put at ease by {?PLAYER_CHILD}your {?CHILD.GENDER}daughter{?}son{\\?}{?}the {?CHILD.GENDER}girl{?}boy{\\?}{\\?} and seek {?CHILD.GENDER}her{?}his{\\?} company."), DefaultSkills.Charm)
		};
		(TextObject, TextObject, SkillObject)[] array = stage_3_page_2_options;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Item2.SetTextVariable("PLAYER_CHILD", IsHeroChildOfPlayer(child) ? 1 : 0);
		}
		EducationCharacterProperties[] array2 = new EducationCharacterProperties[6]
		{
			new EducationCharacterProperties("act_childhood_athlete"),
			new EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", useOffHand: true),
			new EducationCharacterProperties("act_childhood_grit", "carry_hammer", useOffHand: false),
			new EducationCharacterProperties("act_childhood_manners_3", "carry_book", useOffHand: false),
			new EducationCharacterProperties("act_childhood_roguery", "carry_bostaff_rogue1", useOffHand: false),
			new EducationCharacterProperties("act_childhood_manners")
		};
		for (int j = 0; j < stage_3_page_2_options.Length; j++)
		{
			int index = j;
			EducationCharacterProperties childProperties13 = array2[index];
			(TextObject, TextObject, SkillObject) tuple = stage_3_page_2_options[index];
			TextObject optionTitle = tuple.Item1;
			TextObject item = tuple.Item2;
			SkillObject item2 = tuple.Item3;
			EducationOption.EducationOptionConditionDelegate condition = delegate(EducationOption o, List<EducationOption> previousOptions)
			{
				int num = previousOptions[0].RandomValue % stage_3_page_2_options.Length;
				int num2 = previousOptions[1].RandomValue % stage_3_page_2_options.Length;
				bool num3 = (num + num2) % stage_3_page_2_options.Length == index;
				if (num3)
				{
					stage_3_page_2.Description.SetTextVariable("RANDOM_OUTCOME", optionTitle);
				}
				return num3;
			};
			stage_3_page_2.AddOption(new EducationOption(optionTitle, item, null, condition, null, null, new SkillObject[1] { item2 }, childProperties13));
		}
		return educationStage;
	}

	private EducationStage CreateStage14(Hero child)
	{
		TextObject title = new TextObject("{=rcoueCmk}Adolescence");
		EducationStage educationStage = new EducationStage(ChildAgeState.Year14);
		TextObject description = new TextObject("{=3O1Pg3Ie}At {?CHILD.GENDER}her{?}his{\\?} 14th birthday you gave {CHILD.NAME} a special present. You have seen {?CHILD.GENDER}her{?}him{\\?} treasure it and believe it will shape who {?CHILD.GENDER}she{?}he{\\?} is. You gave {?CHILD.GENDER}her{?}him{\\?}...");
		EducationPage educationPage = educationStage.AddPage(childProperties: new EducationCharacterProperties("act_inventory_idle_start", string.Empty, useOffHand: false), pageIndex: 0, title: title, description: description, instruction: _chooseGiftText);
		EducationCharacterProperties childProperties = new EducationCharacterProperties("act_childhood_arms", "vlandia_twohanded_sword_c", useOffHand: false);
		EducationCharacterProperties childProperties2 = new EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", useOffHand: true);
		EducationCharacterProperties childProperties3 = new EducationCharacterProperties("act_childhood_clever_2", "carry_scroll", useOffHand: false);
		EducationCharacterProperties childProperties4 = new EducationCharacterProperties("act_childhood_sharp", "carry_game_left", useOffHand: true);
		EducationCharacterProperties childProperties5 = new EducationCharacterProperties("act_childhood_ready_bow", "bow_roman_a", useOffHand: true);
		EducationCharacterProperties childProperties6 = new EducationCharacterProperties("act_childhood_appearances");
		educationPage.AddOption(new EducationOption(new TextObject("{=BYadInfL}a well-balanced sword."), new TextObject("{=K31F1yPI}When you pick up this blade you can almost feel it dance in your hand. Its edge can cut through a stout poll or a human hair drifting on the wind. "), null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Vigor }, new SkillObject[1] { DefaultSkills.OneHanded }, childProperties));
		educationPage.AddOption(new EducationOption(new TextObject("{=l5aOTKUi}a magnificent steed."), new TextObject("{=DfbYtRyj}You had second thoughts about giving {?PLAYER_CHILD}your{?}the{\\?} child such a spirited animal, but {?CHILD.GENDER}she{?}he{\\?} lept upon its back and galloped like the wind across your pastures."), null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Endurance }, new SkillObject[1] { DefaultSkills.Riding }, childProperties2));
		educationPage.AddOption(new EducationOption(new TextObject("{=bwYRMGIN}a treatise on siegecraft."), new TextObject("{=cr2jmYxg}You remember poring over the hand-drawn schematics of mangonels and towers for hours, imagining that one day you might build one of these awesome instruments of destruction yourself."), null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Intelligence }, new SkillObject[1] { DefaultSkills.Engineering }, childProperties3));
		educationPage.AddOption(new EducationOption(new TextObject("{=iMtCbEb3}a finely carved gameboard."), new TextObject("{=fzxJZZEH}Each piece is a work of art. Even without an opponent one could set it up and gaze upon it for hours, contemplating moves and counter-moves."), null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Cunning }, new SkillObject[1] { DefaultSkills.Tactics }, childProperties4));
		educationPage.AddOption(new EducationOption(new TextObject("{=ocl6ECmt}a well-tempered bow."), new TextObject("{=S3RZLOsv}You can sense the power in the laminated sinews and wood of this weapon. It was made for the calloused hands of a veteran archer and {CHILD.NAME} may need years to be able to fully draw it back, but you know {?CHILD.GENDER}she{?}he{\\?} will be motivated to master it."), null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Control }, new SkillObject[1] { DefaultSkills.Bow }, childProperties5));
		educationPage.AddOption(new EducationOption(new TextObject("{=UpgqdJC0}a trip to your realm's court."), new TextObject("{=bTj16PqR}Every well-born youth wants to see the center of it all, where the lords and ladies gather in splendor to converse and connive. You invited the child to see the spectacle first hand, and provided the elegant clothes {?CHILD.GENDER}she{?}he{\\?}'d need to be part of it. "), null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Social }, new SkillObject[1] { DefaultSkills.Charm }, childProperties6));
		foreach (EducationOption option in educationPage.Options)
		{
			option.Description.SetTextVariable("PLAYER_CHILD", IsHeroChildOfPlayer(child) ? 1 : 0);
		}
		TextObject description2 = new TextObject("{=GOt2uRcJ}In adolescence, {?CHILD.GENDER}she{?}he{\\?} began to take on serious responsibilities and compete with adults as a near-equal. {?CHILD.GENDER}She{?}He{\\?} managed to...");
		EducationPage educationPage2 = educationStage.AddPage(1, title, description2, _chooseAchievementText);
		EducationCharacterProperties childProperties7 = new EducationCharacterProperties("act_childhood_apprentice", "vlandia_twohanded_sword_c", useOffHand: false);
		EducationCharacterProperties childProperties8 = new EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", useOffHand: true);
		EducationCharacterProperties childProperties9 = new EducationCharacterProperties("act_childhood_grit", "carry_hammer", useOffHand: false);
		EducationCharacterProperties childProperties10 = new EducationCharacterProperties("act_childhood_manners_3", "carry_book", useOffHand: false);
		EducationCharacterProperties childProperties11 = new EducationCharacterProperties("act_childhood_ready_bow", "bow_roman_a", useOffHand: true);
		EducationCharacterProperties childProperties12 = new EducationCharacterProperties("act_childhood_clever_2");
		educationPage2.AddOption(new EducationOption(new TextObject("{=IMaTTgPJ}defeat {?CHILD.GENDER}her{?}his{\\?} fencing instructor."), new TextObject("{=cnbeJh6Y}After many tries {?CHILD.GENDER}she{?}he{\\?} successfully beat {?CHILD.GENDER}her{?}his{\\?} tutor, fair and square, during sparring."), null, null, null, null, new SkillObject[1] { DefaultSkills.OneHanded }, childProperties7));
		educationPage2.AddOption(new EducationOption(new TextObject("{=EP0gEG0L}win a race."), new TextObject("{=7mbE4z2v}{?CHILD.GENDER}She{?}He{\\?} won a friendly horse racing competition, and was rewarded with a magnificent saddle."), null, null, null, null, new SkillObject[1] { DefaultSkills.Riding }, childProperties8));
		educationPage2.AddOption(new EducationOption(new TextObject("{=ksXTQXOX}craft a weapon."), new TextObject("{=6WUaFzlv}{?PLAYER_CHILD}Your{?}The{\\?} child forged a sword - blade, hilt and pommel. The artisan said that he has never seen such dedication and patience in one so young."), null, null, null, null, new SkillObject[1] { DefaultSkills.Engineering }, childProperties9));
		educationPage2.AddOption(new EducationOption(new TextObject("{=bX841Jau}learn the arts of healing."), new TextObject("{=3hFT34l1}{?PLAYER_CHILD}Your{?}The{\\?} child helps out the local physician, binding and cleaning wounds and, when the master is absent, prescribing remedies."), null, null, null, null, new SkillObject[1] { DefaultSkills.Medicine }, childProperties10));
		educationPage2.AddOption(new EducationOption(new TextObject("{=ltbU06Fi}become a crack shot."), new TextObject("{=7JoiyH1Y}You heard that {CHILD.NAME} could put an arrow through an arm ring from 100 paces away, but you didn't believe it until you saw it."), null, null, null, null, new SkillObject[1] { DefaultSkills.Bow }, childProperties11));
		educationPage2.AddOption(new EducationOption(new TextObject("{=pHbnAZtt}trade like a veteran merchant."), new TextObject("{=oaXAY8bw}{?PLAYER_CHILD}Your{?}The{\\?} child asked to borrow money to trade with a passing caravan. You figure {?CHILD.GENDER}she{?}he{\\?}'d be sharped and learn a lesson, but in fact the {?CHILD.GENDER}girl{?}boy{\\?} secured a very lucrative deal."), null, null, null, null, new SkillObject[1] { DefaultSkills.Trade }, childProperties12));
		foreach (EducationOption option2 in educationPage2.Options)
		{
			option2.Description.SetTextVariable("PLAYER_CHILD", IsHeroChildOfPlayer(child) ? 1 : 0);
		}
		TextObject stage_4_page_2_description = new TextObject("{=jukOfT2Z}Outside events also intruded on {CHILD.NAME}'s adolescence. You believe {?CHILD.GENDER}she{?}he{\\?} was particularly shaped by {RANDOM_OUTCOME}. This event increased {?CHILD.GENDER}her{?}his{\\?} skills in {SKILL_1} and {SKILL_2}.");
		EducationPage educationPage3 = educationStage.AddPage(2, title, stage_4_page_2_description, _confirmResultsText);
		(TextObject, TextObject, SkillObject[])[] stage_4_page_2_option = new(TextObject, TextObject, SkillObject[])[6]
		{
			(new TextObject("{=43zzbI1J}enemy incursions"), new TextObject("{=AaahFmxv}War was never too far away. On more than one occasion, {CHILD.NAME} spotted foes on the horizon and needed to ride away as quickly as possible."), new SkillObject[2]
			{
				DefaultSkills.Athletics,
				DefaultSkills.Riding
			}),
			(new TextObject("{=y9RcWMbQ}a local rivalry"), new TextObject("{=R7A3aPwi}A neighboring noble's brutish son took an interest in persecuting {CHILD.NAME}, but the young {?CHILD.GENDER}woman{?}man{\\?} gave him reason to look elsewhere for his prey."), new SkillObject[2]
			{
				DefaultSkills.OneHanded,
				DefaultSkills.TwoHanded
			}),
			(new TextObject("{=gHgUwteP}natural disasters"), new TextObject("{=lDMrl7sd}Your district saw more than its share of floods and fires, and {CHILD.NAME} joined in the effort to stem the destruction and rebuild afterwards."), new SkillObject[2]
			{
				DefaultSkills.Crafting,
				DefaultSkills.Engineering
			}),
			(new TextObject("{=mzu6exTe}an outbreak of plague"), new TextObject("{=kAtxHSu9}A great illness swept through your lands. Your child applied what {?CHILD.GENDER}she{?}he{\\?} had read in books on nursing fevers and isolating the sick to minimize deaths on your estate."), new SkillObject[2]
			{
				DefaultSkills.Medicine,
				DefaultSkills.Steward
			}),
			(new TextObject("{=Jc1YXXjN}an influx of wild beasts"), new TextObject("{=MX7toYav}{CHILD.NAME} joined a hunting pursuit in pursuit of a pack of wolves who had been ravaging the local livestock. {?CHILD.GENDER}She{?}He{\\?} tracked and took down one of the beasts with an arrow."), new SkillObject[2]
			{
				DefaultSkills.Scouting,
				DefaultSkills.Bow
			}),
			(new TextObject("{=aiBQo2MR}an outbreak of unrest"), new TextObject("{=yym5kCG5}After a particularly hard winter your tenants began to murmur about rising up and seizing your granaries. {CHILD.NAME} convinced them to be patient and wait for relief."), new SkillObject[2]
			{
				DefaultSkills.Charm,
				DefaultSkills.Leadership
			})
		};
		EducationCharacterProperties[] array = new EducationCharacterProperties[6]
		{
			new EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", useOffHand: true),
			new EducationCharacterProperties("act_childhood_arms_2", "vlandia_twohanded_sword_c", useOffHand: false),
			new EducationCharacterProperties("act_childhood_grit", "carry_hammer", useOffHand: false),
			new EducationCharacterProperties("act_childhood_ready", "carry_book_left", useOffHand: true),
			new EducationCharacterProperties("act_childhood_ready_bow", "bow_roman_a", useOffHand: true),
			new EducationCharacterProperties("act_childhood_honor")
		};
		for (int i = 0; i < stage_4_page_2_option.Length; i++)
		{
			int index = i;
			EducationCharacterProperties childProperties13 = array[index];
			(TextObject, TextObject, SkillObject[]) tuple = stage_4_page_2_option[index];
			TextObject optionTitle = tuple.Item1;
			TextObject item = tuple.Item2;
			SkillObject[] skills = tuple.Item3;
			EducationOption.EducationOptionConditionDelegate condition = delegate(EducationOption o, List<EducationOption> previousOptions)
			{
				int num = previousOptions[0].RandomValue % stage_4_page_2_option.Length;
				int num2 = previousOptions[1].RandomValue % stage_4_page_2_option.Length;
				bool num3 = (num + num2) % stage_4_page_2_option.Length == index;
				if (num3)
				{
					stage_4_page_2_description.SetTextVariable("RANDOM_OUTCOME", optionTitle);
					stage_4_page_2_description.SetTextVariable("SKILL_1", skills[0].Name);
					stage_4_page_2_description.SetTextVariable("SKILL_2", skills[1].Name);
				}
				return num3;
			};
			educationPage3.AddOption(new EducationOption(optionTitle, item, null, condition, null, null, skills, childProperties13));
		}
		return educationStage;
	}

	private EducationStage CreateStage16(Hero child)
	{
		TextObject title = new TextObject("{=Ww3uU5e6}Young Adulthood");
		EducationStage educationStage = new EducationStage(ChildAgeState.Year16);
		TextObject textObject = new TextObject("{=yJ0XRD9g}Eventually it was time for {?PLAYER_CHILD}your{?}the{\\?} child to travel far from home. You sent the young {?CHILD.GENDER}woman{?}man{\\?} away...");
		textObject.SetTextVariable("PLAYER_CHILD", IsHeroChildOfPlayer(child) ? 1 : 0);
		EducationPage educationPage = educationStage.AddPage(childProperties: new EducationCharacterProperties("act_inventory_idle_start", string.Empty, useOffHand: false), pageIndex: 0, title: title, description: textObject, instruction: _chooseTaskText);
		EducationCharacterProperties childProperties = new EducationCharacterProperties("act_childhood_apprentice", "vlandia_twohanded_sword_c", useOffHand: false);
		EducationCharacterProperties childProperties2 = new EducationCharacterProperties("act_childhood_genius");
		EducationCharacterProperties childProperties3 = new EducationCharacterProperties("act_childhood_grit", "carry_hammer", useOffHand: false);
		EducationCharacterProperties childProperties4 = new EducationCharacterProperties("act_childhood_honor");
		EducationCharacterProperties childProperties5 = new EducationCharacterProperties("act_childhood_leader");
		EducationCharacterProperties childProperties6 = new EducationCharacterProperties("act_childhood_manners");
		EducationOption stage_5_page_0_warriorOption = new EducationOption(new TextObject("{=Tbv2txJV}as a squire."), new TextObject("{=4CuMDzvd}You asked your best warrior to take {?CHILD.GENDER}her{?}him{\\?} under his wings and make sure {?CHILD.GENDER}she{?}he{\\?} gets the taste of battle without being seriously harmed."), null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Vigor }, new SkillObject[1] { DefaultSkills.OneHanded }, childProperties);
		EducationOption stage_5_page_0_merchantOption = new EducationOption(new TextObject("{=N62qjb8s}as an aide."), new TextObject("{=rZkQfBxt}You asked one of the local merchants to take the young {?CHILD.GENDER}woman{?}man{\\?} along with one of his caravans and make {?CHILD.GENDER}her{?}him{\\?} learn the secrets of the trade."), null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Cunning }, new SkillObject[1] { DefaultSkills.Trade }, childProperties2);
		EducationOption stage_5_page_0_siegeEngineerOption = new EducationOption(new TextObject("{=ezfehKrT}as an apprentice."), new TextObject("{=DOhOpmhV}You found {CHILD.NAME} an apprenticeship with a siege engineer. Even if it can be a bit dangerous, there's no substitute for that kind of practical experience."), null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Intelligence }, new SkillObject[1] { DefaultSkills.Engineering }, childProperties3);
		EducationOption stage_5_page_0_nobleOption = new EducationOption(new TextObject("{=SF244Jxx}as a noble's ward."), new TextObject("{=KgJaq5dx}You sent the young {?CHILD.GENDER}woman{?}man{\\?} to the hall of one of your fellow lords. There {?CHILD.GENDER}she{?}he{\\?} will learn how a different clan runs its affairs."), null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Control }, new SkillObject[1] { DefaultSkills.Bow }, childProperties4);
		EducationOption stage_5_page_0_ownWayOption = new EducationOption(new TextObject("{=sdQoXkKq}to find {?CHILD.GENDER}her{?}his{\\?} own way."), new TextObject("{=6q8tY6Sz}You remember your freebooting days fondly. You want {CHILD.NAME} to experience life as you did."), null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Endurance }, new SkillObject[1] { DefaultSkills.Athletics }, childProperties5);
		EducationOption stage_5_page_0_diplomatOption = new EducationOption(new TextObject("{=bai3aiDB}as an envoy."), new TextObject("{=hjOxidJj}The young {?CHILD.GENDER}woman{?}man{\\?} will spend {?CHILD.GENDER}her{?}his{\\?} time at a diplomatic mission, where {?CHILD.GENDER}she{?}he{\\?} will have chance to interact with people from foreign lands."), null, null, null, new CharacterAttribute[1] { DefaultCharacterAttributes.Social }, new SkillObject[1] { DefaultSkills.Charm }, childProperties6);
		educationPage.AddOption(stage_5_page_0_warriorOption);
		educationPage.AddOption(stage_5_page_0_merchantOption);
		educationPage.AddOption(stage_5_page_0_siegeEngineerOption);
		educationPage.AddOption(stage_5_page_0_nobleOption);
		educationPage.AddOption(stage_5_page_0_ownWayOption);
		educationPage.AddOption(stage_5_page_0_diplomatOption);
		TextObject description = new TextObject("{=V0QmrGda}Before {CHILD.NAME} left, you asked your warrior to make sure that the young {?CHILD.GENDER}woman{?}man{\\?}...");
		EducationPage educationPage2 = educationStage.AddPage(1, title, description, _chooseRequestText, default(EducationCharacterProperties), default(EducationCharacterProperties), (EducationPage page, List<EducationOption> previousOptions) => previousOptions.Contains(stage_5_page_0_warriorOption));
		TextObject textObject2 = new TextObject("{=WJSapWab}{?CHILD.GENDER}She{?}He{\\?} had a chance to fight in a few minor skirmishes without getting seriously injured.");
		TextObject textObject3 = new TextObject("{=La7wkS5M}The young {?CHILD.GENDER}woman{?}man{\\?} defeated several opponents and did honor to your clan's name.");
		TextObject textObject4 = new TextObject("{=fsr2yhrr}While there were few military feats to accomplish, {CHILD.NAME} still became quite popular among the troops and improved their morale in the process.");
		TextObject textObject5 = new TextObject("{=CpcWqufH}The young {?CHILD.GENDER}woman{?}man{\\?} was placed in command of a small group of militiamen, including some nearly twice {?CHILD.GENDER}her{?}his{\\?} age, and earned their respect.");
		TextObject textObject6 = new TextObject("{=VTzvlEV8}Sparring with total strangers, including some who would happily break {?CHILD.GENDER}her{?}his{\\?} teeth if {?CHILD.GENDER}she{?}he{\\?} let them, was a new and valuable experience for {CHILD.NAME}.");
		TextObject textObject7 = new TextObject("{=FekWDIUm}{?CHILD.GENDER}She{?}He{\\?} helped plan a small expedition in pursuit of a group of brigands.");
		EducationCharacterProperties childProperties7 = new EducationCharacterProperties("act_childhood_fierce", "vlandia_twohanded_sword_c", useOffHand: false);
		EducationCharacterProperties childProperties8 = new EducationCharacterProperties("act_childhood_guard_up_staff", "carry_bostaff", useOffHand: false);
		EducationCharacterProperties childProperties9 = new EducationCharacterProperties("act_childhood_militia", "vlandia_twohanded_sword_c", useOffHand: false);
		EducationCharacterProperties childProperties10 = new EducationCharacterProperties("act_childhood_leader_2");
		EducationCharacterProperties childProperties11 = new EducationCharacterProperties("act_childhood_arms_2", "vlandia_twohanded_sword_c", useOffHand: false);
		EducationCharacterProperties childProperties12 = new EducationCharacterProperties("act_childhood_tactician");
		educationPage2.AddOption(new EducationOption(new TextObject("{=6LpwsxYP}gets bloodied."), textObject2, null, null, null, null, new SkillObject[1] { DefaultSkills.OneHanded }, childProperties7));
		educationPage2.AddOption(new EducationOption(new TextObject("{=hes54oh0}joins in a tournament."), textObject3, null, null, null, null, new SkillObject[1] { DefaultSkills.Polearm }, childProperties8));
		educationPage2.AddOption(new EducationOption(new TextObject("{=z50GdIa3}learns to inspire the soldiers."), textObject4, null, null, null, null, new SkillObject[1] { DefaultSkills.Charm }, childProperties9));
		educationPage2.AddOption(new EducationOption(new TextObject("{=MpdPpIEY}leads a patrol."), textObject5, null, null, null, null, new SkillObject[1] { DefaultSkills.Leadership }, childProperties10));
		educationPage2.AddOption(new EducationOption(new TextObject("{=flCA80Ex}trains really hard."), textObject6, null, null, null, null, new SkillObject[1] { DefaultSkills.TwoHanded }, childProperties11));
		educationPage2.AddOption(new EducationOption(new TextObject("{=UuQDXIFD}hunts down bandits."), textObject7, null, null, null, null, new SkillObject[1] { DefaultSkills.Tactics }, childProperties12));
		TextObject description2 = new TextObject("{=cDVNFn4p}Before {CHILD.NAME} left, you told the merchant that {?CHILD.GENDER}she{?}he{\\?} should be entrusted with...");
		EducationPage educationPage3 = educationStage.AddPage(1, title, description2, _chooseRequestText, default(EducationCharacterProperties), default(EducationCharacterProperties), (EducationPage page, List<EducationOption> previousOptions) => previousOptions.Contains(stage_5_page_0_merchantOption));
		TextObject textObject8 = new TextObject("{=rVIzHhLO}The young {?CHILD.GENDER}woman{?}man{\\?}'s social graces helped put other merchants at ease as they exchanged information and worked out deals.");
		TextObject textObject9 = new TextObject("{=UYdvhdDl}When cargo went missing, the young {?CHILD.GENDER}woman{?}man{\\?} made the inquiries at the local black market that were necessary to get it back.");
		TextObject textObject10 = new TextObject("{=bCZbgrLU}The smith came down sick and the young {?CHILD.GENDER}woman{?}man{\\?} had to quickly learn how to craft horseshoes and nails.");
		TextObject textObject11 = new TextObject("{=1vQul57Y}The caravan got lost and it was {CHILD.NAME} who spotted the landmarks that got it back on track.");
		TextObject textObject12 = new TextObject("{=cyQXm7VA}The young {?CHILD.GENDER}woman{?}man{\\?} was charged with ensuring they had the food, saddles, animals, teamsters and guards for the journey.");
		TextObject textObject13 = new TextObject("{=egXc7J72}Caravan guards are not elite troops, and may slack off if not held to a high standard of discipline. {CHILD.NAME} made sure that they stayed sober and alert.");
		EducationCharacterProperties childProperties13 = new EducationCharacterProperties("act_childhood_manners");
		EducationCharacterProperties childProperties14 = new EducationCharacterProperties("act_childhood_fierce", "blacksmith_sword", useOffHand: false);
		EducationCharacterProperties childProperties15 = new EducationCharacterProperties("act_childhood_artisan", "carry_linen", useOffHand: false);
		EducationCharacterProperties childProperties16 = new EducationCharacterProperties("act_childhood_leader");
		EducationCharacterProperties childProperties17 = new EducationCharacterProperties("act_childhood_tactician");
		EducationCharacterProperties childProperties18 = new EducationCharacterProperties("act_childhood_leader");
		educationPage3.AddOption(new EducationOption(new TextObject("{=9NbAzOER}dealing with business partners."), textObject8, null, null, null, null, new SkillObject[1] { DefaultSkills.Charm }, childProperties13));
		educationPage3.AddOption(new EducationOption(new TextObject("{=QVZMiaV2}helping recover stolen goods."), textObject9, null, null, null, null, new SkillObject[1] { DefaultSkills.Roguery }, childProperties14));
		educationPage3.AddOption(new EducationOption(new TextObject("{=hfaw7Xx9}helping the artisan."), textObject10, null, null, null, null, new SkillObject[1] { DefaultSkills.Crafting }, childProperties15));
		educationPage3.AddOption(new EducationOption(new TextObject("{=XYRZIPD8}guiding the caravan."), textObject11, null, null, null, null, new SkillObject[1] { DefaultSkills.Scouting }, childProperties16));
		educationPage3.AddOption(new EducationOption(new TextObject("{=JX13Wd65}managing the logistics of travel."), textObject12, null, null, null, null, new SkillObject[1] { DefaultSkills.Steward }, childProperties17));
		educationPage3.AddOption(new EducationOption(new TextObject("{=rvqVuM5o}supervising the guards."), textObject13, null, null, null, null, new SkillObject[1] { DefaultSkills.Leadership }, childProperties18));
		TextObject description3 = new TextObject("{=U9nsz7pg}Before {CHILD.NAME} left, you asked the siege engineer to make sure that the young {?CHILD.GENDER}woman{?}man{\\?}...");
		EducationPage educationPage4 = educationStage.AddPage(1, title, description3, _chooseRequestText, default(EducationCharacterProperties), default(EducationCharacterProperties), (EducationPage page, List<EducationOption> previousOptions) => previousOptions.Contains(stage_5_page_0_siegeEngineerOption));
		TextObject textObject14 = new TextObject("{=MUaFiBva}{CHILD.NAME} paid close attention to the experienced engineers and became competent in a profession where small mistakes can have deadly consequences.");
		TextObject textObject15 = new TextObject("{=3ZZtAYXm}The young {?CHILD.GENDER}woman{?}man{\\?} learned to treat the mangled fingers and concussions without which no siege is complete.");
		TextObject textObject16 = new TextObject("{=VVBx07cK}The siege engines needed gears and bracings, and the master assigned {?PLAYER_CHILD}your{?}the{\\?} child to assist in forging them.");
		TextObject textObject17 = new TextObject("{=oCiWOutS}Sieges required tools, oil, good quality timber and, of course, food. It was young {?CHILD.GENDER}woman{?}man{\\?}'s responsibility to have them gathered, stocked and distributed.");
		TextObject textObject18 = new TextObject("{=ijootCu7}It was easy to get bogged down in the details of hurling rocks and assembling towers, but {CHILD.NAME} kept a keen eye on why some sieges succeeded and others failed.");
		TextObject textObject19 = new TextObject("{=igkNYcSI}The young {?CHILD.GENDER}woman{?}man{\\?} tells you how engineers have become an unofficial guild that transcends borders. Even men on opposite sides of a siege are known to discuss the technical details of their craft.");
		EducationCharacterProperties childProperties19 = new EducationCharacterProperties("act_childhood_ready", "carry_book_left", useOffHand: true);
		EducationCharacterProperties childProperties20 = new EducationCharacterProperties("act_childhood_gracious", "carry_book", useOffHand: false);
		EducationCharacterProperties childProperties21 = new EducationCharacterProperties("act_childhood_grit", "carry_hammer", useOffHand: false);
		EducationCharacterProperties childProperties22 = new EducationCharacterProperties("act_childhood_peddlers_2", "carry_sticks", useOffHand: false);
		EducationCharacterProperties childProperties23 = new EducationCharacterProperties("act_childhood_tactician");
		EducationCharacterProperties childProperties24 = new EducationCharacterProperties("act_childhood_manners");
		educationPage4.AddOption(new EducationOption(new TextObject("{=L1o5mb69}learns to construct siege engines."), textObject14, null, null, null, null, new SkillObject[1] { DefaultSkills.Engineering }, childProperties19));
		educationPage4.AddOption(new EducationOption(new TextObject("{=74pYaU18}treats injuries."), textObject15, null, null, null, null, new SkillObject[1] { DefaultSkills.Medicine }, childProperties20));
		educationPage4.AddOption(new EducationOption(new TextObject("{=vsFVlFa4}assists the smiths."), textObject16, null, null, null, null, new SkillObject[1] { DefaultSkills.Crafting }, childProperties21));
		educationPage4.AddOption(new EducationOption(new TextObject("{=jL8ntljn}procures supplies."), textObject17, null, null, null, null, new SkillObject[1] { DefaultSkills.Trade }, childProperties22));
		educationPage4.AddOption(new EducationOption(new TextObject("{=I6oqLFXh}focuses on the big picture."), textObject18, null, null, null, null, new SkillObject[1] { DefaultSkills.Tactics }, childProperties23));
		educationPage4.AddOption(new EducationOption(new TextObject("{=XTmpkNNg}socializes with other engineers."), textObject19, null, null, null, null, new SkillObject[1] { DefaultSkills.Charm }, childProperties24));
		TextObject description4 = new TextObject("{=53xrtZst}Before {CHILD.NAME} left, you asked the lord to make sure that the young {?CHILD.GENDER}woman{?}man{\\?}...");
		EducationPage educationPage5 = educationStage.AddPage(1, title, description4, _chooseRequestText, default(EducationCharacterProperties), default(EducationCharacterProperties), (EducationPage page, List<EducationOption> previousOptions) => previousOptions.Contains(stage_5_page_0_nobleOption));
		TextObject textObject20 = new TextObject("{=aCO0Md8z}When {?CHILD.GENDER}she{?}he{\\?} was ambused on patrol, {CHILD.NAME} fought the attackers off with sheer willpower.");
		TextObject textObject21 = new TextObject("{=0XOii9tg}The noble did not let {CHILD.NAME} go hand-to-hand with other warriors, but the young {?CHILD.GENDER}woman{?}man{\\?} joined the archers as they traded shots with enemy scouts.");
		TextObject textObject22 = new TextObject("{=9BoHka98}The young {?CHILD.GENDER}woman{?}man{\\?} pursued enemy scouts while avoiding their main force. There was little fighting but a great deal of riding.");
		TextObject textObject23 = new TextObject("{=WD6soDM1}It was a minor skirmish but a clear victory nonetheless. {CHILD.NAME} wasn't just there when the commander came up with the winning strategy but also took part in executing it.");
		TextObject textObject24 = new TextObject("{=1NCVDNri}The noble gave the young adult command of a small group of scouts. {?CHILD.GENDER}She{?}He{\\?} took them on patrol and even though some were twice {?CHILD.GENDER}her{?}his{\\?} age, {?CHILD.GENDER}she{?}he{\\?} won their respect.");
		TextObject textObject25 = new TextObject("{=EXwX0zrx}The young {?CHILD.GENDER}woman{?}man{\\?} faced a rival clan's outrider in single combat, and handed him a defeat that he wouldn't forget.");
		EducationCharacterProperties childProperties25 = new EducationCharacterProperties("act_childhood_athlete");
		EducationCharacterProperties childProperties26 = new EducationCharacterProperties("act_childhood_ready_bow", "bow_roman_a", useOffHand: true);
		EducationCharacterProperties childProperties27 = new EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", useOffHand: true);
		EducationCharacterProperties childProperties28 = new EducationCharacterProperties("act_childhood_tactician");
		EducationCharacterProperties childProperties29 = new EducationCharacterProperties("act_childhood_leader_2");
		EducationCharacterProperties childProperties30 = new EducationCharacterProperties("act_childhood_fierce");
		educationPage5.AddOption(new EducationOption(new TextObject("{=AWuEV99O}unleashes {?CHILD.GENDER}her{?}his{\\?} fighting spirit."), textObject20, null, null, null, null, new SkillObject[1] { DefaultSkills.Athletics }, childProperties25));
		educationPage5.AddOption(new EducationOption(new TextObject("{=Mk6dZBQa}skirmishes from a distance."), textObject21, null, null, null, null, new SkillObject[1] { DefaultSkills.Bow }, childProperties26));
		educationPage5.AddOption(new EducationOption(new TextObject("{=Mp7dT2u2}chases and is chased."), textObject22, null, null, null, null, new SkillObject[1] { DefaultSkills.Riding }, childProperties27));
		educationPage5.AddOption(new EducationOption(new TextObject("{=4pOb934Y}understands how a victory is won."), textObject23, null, null, null, null, new SkillObject[1] { DefaultSkills.Tactics }, childProperties28));
		educationPage5.AddOption(new EducationOption(new TextObject("{=eXLxHSls}leads men into enemy territory."), textObject24, null, null, null, null, new SkillObject[1] { DefaultSkills.Leadership }, childProperties29));
		educationPage5.AddOption(new EducationOption(new TextObject("{=1BFk0wAV}defeats an enemy."), textObject25, null, null, null, null, new SkillObject[1] { DefaultSkills.OneHanded }, childProperties30));
		TextObject description5 = new TextObject("{=4Bl1LWmZ}Before {CHILD.NAME} left, you encouraged {?CHILD.GENDER}her{?}him{\\?} to...");
		EducationPage educationPage6 = educationStage.AddPage(1, title, description5, _chooseRequestText, default(EducationCharacterProperties), default(EducationCharacterProperties), (EducationPage page, List<EducationOption> previousOptions) => previousOptions.Contains(stage_5_page_0_ownWayOption));
		TextObject textObject26 = new TextObject("{=WL1zcgYx}{?CHILD.GENDER}She{?}He{\\?} travelled on foot in the company of other pilgrims, often relying on the kindness of strangers for food and shelter.");
		TextObject textObject27 = new TextObject("{=K1HrvDLO}{?CHILD.GENDER}She{?}He{\\?} won't talk much about it, but you know that {?CHILD.GENDER}she{?}he{\\?} has seen the darker side of Calradia.");
		TextObject textObject28 = new TextObject("{=Fn7Pgia7}{?CHILD.GENDER}She{?}He{\\?} told you that {?CHILD.GENDER}her{?}his{\\?} skill with a lance paid for much of {?CHILD.GENDER}her{?}his{\\?} journey.");
		TextObject textObject29 = new TextObject("{=2EOvlPc2}Gripped by wanderlust, {?PLAYER_CHILD}your{?}the{\\?} child rode from the freezing woods of Sturgia to the blazing Nahasa.");
		TextObject textObject30 = new TextObject("{=KB0fv5Me}The young {?CHILD.GENDER}woman{?}man{\\?} charmed {?CHILD.GENDER}her{?}his{\\?} way into a circle of nobles and was a welcome guest at well-set tables.");
		TextObject textObject31 = new TextObject("{=UGda4FDZ}{CHILD.NAME} found work with an artisan for a season, keeping {?CHILD.GENDER}her{?}his{\\?} high birth a secret.");
		EducationCharacterProperties childProperties31 = new EducationCharacterProperties("act_childhood_roguery", "carry_bostaff_rogue1", useOffHand: false);
		EducationCharacterProperties childProperties32 = new EducationCharacterProperties("act_childhood_streets", "carry_bostaff_rogue1", useOffHand: false);
		EducationCharacterProperties childProperties33 = new EducationCharacterProperties("act_childhood_guard_up_staff", "carry_bostaff", useOffHand: false);
		EducationCharacterProperties childProperties34 = new EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", useOffHand: true);
		EducationCharacterProperties childProperties35 = new EducationCharacterProperties("act_childhood_appearances");
		EducationCharacterProperties childProperties36 = new EducationCharacterProperties("act_childhood_strenght", "carry_sticks", useOffHand: false);
		educationPage6.AddOption(new EducationOption(new TextObject("{=1LWWvGTT}participate in a pilgrimage."), textObject26, null, null, null, null, new SkillObject[1] { DefaultSkills.Athletics }, childProperties31));
		educationPage6.AddOption(new EducationOption(new TextObject("{=8alDLq0s}not to be too choosy about travelling companions."), textObject27, null, null, null, null, new SkillObject[1] { DefaultSkills.Roguery }, childProperties32));
		educationPage6.AddOption(new EducationOption(new TextObject("{=RxqCiO7F}competes in tournaments."), textObject28, null, null, null, null, new SkillObject[1] { DefaultSkills.Polearm }, childProperties33));
		educationPage6.AddOption(new EducationOption(new TextObject("{=ETfx08db}see as much of the world as {?CHILD.GENDER}she{?}he{\\?} could."), textObject29, null, null, null, null, new SkillObject[1] { DefaultSkills.Riding }, childProperties34));
		educationPage6.AddOption(new EducationOption(new TextObject("{=RhJdnAQY}enjoy the high life."), textObject30, null, null, null, null, new SkillObject[1] { DefaultSkills.Charm }, childProperties35));
		educationPage6.AddOption(new EducationOption(new TextObject("{=dObod6IZ}do some honest work."), textObject31, null, null, null, null, new SkillObject[1] { DefaultSkills.Crafting }, childProperties36));
		TextObject description6 = new TextObject("{=TaAHFZfd}You asked your head of expedition to make sure that your {?CHILD.GENDER}daughter{?}son{\\?}...");
		EducationPage educationPage7 = educationStage.AddPage(1, title, description6, _chooseRequestText, default(EducationCharacterProperties), default(EducationCharacterProperties), (EducationPage page, List<EducationOption> previousOptions) => previousOptions.Contains(stage_5_page_0_diplomatOption));
		TextObject textObject32 = new TextObject("{=Q39oLjSu}One of the entourage was thrown from a horse on the road. The young {?CHILD.GENDER}woman{?}man{\\?} set the broken bone and ensured it healed cleanly.");
		TextObject textObject33 = new TextObject("{=8AjlID6z}You hear talk of incriminating letters and loose tongues paid to stay silent. You're not sure exactly what happened, but {?PLAYER_CHILD}your{?}the{\\?} child seems more worldly than {?CHILD.GENDER}she{?}he{\\?} did when {?CHILD.GENDER}she{?}he{\\?} left.");
		TextObject textObject34 = new TextObject("{=4nUhnWZ3}There was a great deal of feasting and speech-making. From what you hear, {?CHILD.GENDER}she{?}he{\\?} acquitted {?CHILD.GENDER}herself{?}himself{\\?} well.");
		TextObject textObject35 = new TextObject("{=aCMZafK5}The fight was over a minor insult, and only to the first blood. But you're still relieved that the first blood in question belonged to the other youth.");
		TextObject textObject36 = new TextObject("{=5vosR2YO}The host was the kind of man who likes to do his negotiations from the saddle in pursuit of deer, and {CHILD.NAME} joined in every expedition.");
		TextObject textObject37 = new TextObject("{=taKIDPxj}Embassies carry gifts and attract bandits. Fighting them off made the trip much more exciting than {?CHILD.GENDER}she{?}he{\\?} had expected.");
		EducationCharacterProperties childProperties37 = new EducationCharacterProperties("act_childhood_ready", "carry_book_left", useOffHand: true);
		EducationCharacterProperties childProperties38 = new EducationCharacterProperties("act_childhood_hardened");
		EducationCharacterProperties childProperties39 = new EducationCharacterProperties("act_childhood_manners");
		EducationCharacterProperties childProperties40 = new EducationCharacterProperties("act_childhood_apprentice", "training_sword", useOffHand: false);
		EducationCharacterProperties childProperties41 = new EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", useOffHand: true);
		EducationCharacterProperties childProperties42 = new EducationCharacterProperties("act_childhood_tactician");
		educationPage7.AddOption(new EducationOption(new TextObject("{=6jgjJ3Ts}help anyone in need."), textObject32, null, null, null, null, new SkillObject[1] { DefaultSkills.Medicine }, childProperties37));
		educationPage7.AddOption(new EducationOption(new TextObject("{=FfZpLoVD}dabble in intrigue."), textObject33, null, null, null, null, new SkillObject[1] { DefaultSkills.Roguery }, childProperties38));
		educationPage7.AddOption(new EducationOption(new TextObject("{=EzbzOsJo}burnish {?CHILD.GENDER}her{?}his{\\?} social skills."), textObject34, null, null, null, null, new SkillObject[1] { DefaultSkills.Charm }, childProperties39));
		educationPage7.AddOption(new EducationOption(new TextObject("{=bdYtcgyB}never back down from a challenge."), textObject35, null, null, null, null, new SkillObject[1] { DefaultSkills.OneHanded }, childProperties40));
		educationPage7.AddOption(new EducationOption(new TextObject("{=hrlBxgzs}enjoy the pleasures of the hunt."), textObject36, null, null, null, null, new SkillObject[1] { DefaultSkills.Riding }, childProperties41));
		educationPage7.AddOption(new EducationOption(new TextObject("{=XMaeSoRq}never let down {?CHILD.GENDER}her{?}his{\\?} guard."), textObject37, null, null, null, null, new SkillObject[1] { DefaultSkills.Tactics }, childProperties42));
		EducationPage[] array = new EducationPage[6] { educationPage2, educationPage3, educationPage5, educationPage6, educationPage4, educationPage7 };
		for (int num = 0; num < array.Length; num++)
		{
			foreach (EducationOption option in array[num].Options)
			{
				option.Description.SetTextVariable("PLAYER_CHILD", IsHeroChildOfPlayer(child) ? 1 : 0);
			}
		}
		TextObject item = new TextObject("{=4VxFPTT0}got bloodied.");
		TextObject item2 = new TextObject("{=VAFmTAtx}won a tournament.");
		TextObject item3 = new TextObject("{=DiE5Mh2J}learned to inspire the soldiers.");
		TextObject item4 = new TextObject("{=hYTcXbyS}led a patrol.");
		TextObject item5 = new TextObject("{=webjbgTa}trained really hard.");
		TextObject item6 = new TextObject("{=SCpINAZ1}hunted down bandits.");
		TextObject item7 = new TextObject("{=YVyId3wX}dealt with business partners.");
		TextObject item8 = new TextObject("{=SBHwIaVP}recovered stolen goods.");
		TextObject item9 = new TextObject("{=o9XKbana}helped the artisan.");
		TextObject item10 = new TextObject("{=brixvCjC}guided the caravan.");
		TextObject item11 = new TextObject("{=CVVttuuQ}managed the logistics of travel.");
		TextObject item12 = new TextObject("{=xXOLhxRl}supervised the guards.");
		TextObject item13 = new TextObject("{=MHsa7s99}learned to construct siege engines.");
		TextObject item14 = new TextObject("{=SXbW99CH}treated injuries.");
		TextObject item15 = new TextObject("{=SGVW6NXR}assisted the smith.");
		TextObject item16 = new TextObject("{=9mWxhzDG}procured supplies.");
		TextObject item17 = new TextObject("{=a1PPbzbi}focused on the big picture.");
		TextObject item18 = new TextObject("{=nyatGUkU}socialized with other engineers.");
		TextObject item19 = new TextObject("{=g7lNg1ea}unleashed {?CHILD.GENDER}her{?}his{\\?} fighting spirit.");
		TextObject item20 = new TextObject("{=KjR0HhJv}skirmished from a distance.");
		TextObject item21 = new TextObject("{=SSavEblm}chased and was chased.");
		TextObject item22 = new TextObject("{=plYfFg4A}learned how a victory is won.");
		TextObject item23 = new TextObject("{=2OdXZUg9}led men into enemy territory.");
		TextObject item24 = new TextObject("{=SnEjsXsH}defeated an enemy.");
		TextObject item25 = new TextObject("{=9QLPm1o6}joined a pilgrimage.");
		TextObject item26 = new TextObject("{=E90ArBnZ}fell in with the wrong crowd.");
		TextObject item27 = new TextObject("{=VAFmTAtx}won a tournament.");
		TextObject item28 = new TextObject("{=7hYUleHh}rode to the edge of the world.");
		TextObject item29 = new TextObject("{=PgN4BFNs}enjoyed the high life.");
		TextObject item30 = new TextObject("{=lsddqaAx}did some honest work.");
		TextObject item31 = new TextObject("{=gCBZlAzs}treated an injury.");
		TextObject item32 = new TextObject("{=CNYoZKyf}dabbled in intrigue.");
		TextObject item33 = new TextObject("{=83n7Oa7e}burnished {?CHILD.GENDER}her{?}his{\\?} social skills.");
		TextObject item34 = new TextObject("{=p5Wo8rNb}won a duel.");
		TextObject item35 = new TextObject("{=O9amiAiB}joined a hunting party.");
		TextObject item36 = new TextObject("{=XFZcqNBB}battled brigands.");
		TextObject description7 = new TextObject("{=Yk3xrawy}When {?CHILD.GENDER}she{?}he{\\?} returns, {CHILD.NAME} tells you the story of how {?CHILD.GENDER}she{?}he{\\?} {RANDOM_OUTCOME} That event increased {?CHILD.GENDER}her{?}his{\\?} skill in {SKILL}.");
		EducationPage currentPage = educationStage.AddPage(2, title, description7, _confirmResultsText, default(EducationCharacterProperties), default(EducationCharacterProperties), (EducationPage p, List<EducationOption> previousOptions) => previousOptions.Contains(stage_5_page_0_warriorOption));
		(TextObject, TextObject, SkillObject)[] titleDescSkillTuple = new(TextObject, TextObject, SkillObject)[6]
		{
			(item, textObject2, DefaultSkills.OneHanded),
			(item2, textObject3, DefaultSkills.Polearm),
			(item3, textObject4, DefaultSkills.Charm),
			(item4, textObject5, DefaultSkills.Leadership),
			(item5, textObject6, DefaultSkills.TwoHanded),
			(item6, textObject7, DefaultSkills.Tactics)
		};
		EducationCharacterProperties[] childProperties43 = new EducationCharacterProperties[6]
		{
			new EducationCharacterProperties("act_childhood_apprentice", "vlandia_twohanded_sword_c", useOffHand: false),
			new EducationCharacterProperties("act_childhood_militia", "carry_bostaff_rogue1", useOffHand: false),
			new EducationCharacterProperties("act_conversation_confident_loop"),
			new EducationCharacterProperties("act_conversation_hip_loop"),
			new EducationCharacterProperties("act_childhood_arms_2", "vlandia_twohanded_sword_c", useOffHand: false),
			new EducationCharacterProperties("act_conversation_closed_loop")
		};
		Stage16Selection(titleDescSkillTuple, currentPage, childProperties43);
		EducationPage currentPage2 = educationStage.AddPage(2, title, description7, _confirmResultsText, default(EducationCharacterProperties), default(EducationCharacterProperties), (EducationPage p, List<EducationOption> previousOptions) => previousOptions.Contains(stage_5_page_0_merchantOption));
		(TextObject, TextObject, SkillObject)[] titleDescSkillTuple2 = new(TextObject, TextObject, SkillObject)[6]
		{
			(item7, textObject8, DefaultSkills.Charm),
			(item8, textObject9, DefaultSkills.Roguery),
			(item9, textObject10, DefaultSkills.Crafting),
			(item10, textObject11, DefaultSkills.Scouting),
			(item11, textObject12, DefaultSkills.Steward),
			(item12, textObject13, DefaultSkills.Leadership)
		};
		EducationCharacterProperties[] childProperties44 = new EducationCharacterProperties[6]
		{
			new EducationCharacterProperties("act_conversation_confident_loop"),
			new EducationCharacterProperties("act_childhood_roguery", "carry_bostaff_rogue1", useOffHand: false),
			new EducationCharacterProperties("act_childhood_artisan", "carry_linen", useOffHand: false),
			new EducationCharacterProperties("act_childhood_vibrant"),
			new EducationCharacterProperties("act_conversation_confident_loop"),
			new EducationCharacterProperties("act_conversation_hip_loop")
		};
		Stage16Selection(titleDescSkillTuple2, currentPage2, childProperties44);
		EducationPage currentPage3 = educationStage.AddPage(2, title, description7, _confirmResultsText, default(EducationCharacterProperties), default(EducationCharacterProperties), (EducationPage p, List<EducationOption> previousOptions) => previousOptions.Contains(stage_5_page_0_siegeEngineerOption));
		(TextObject, TextObject, SkillObject)[] titleDescSkillTuple3 = new(TextObject, TextObject, SkillObject)[6]
		{
			(item13, textObject14, DefaultSkills.Engineering),
			(item14, textObject15, DefaultSkills.Medicine),
			(item15, textObject16, DefaultSkills.Crafting),
			(item16, textObject17, DefaultSkills.Trade),
			(item17, textObject18, DefaultSkills.Tactics),
			(item18, textObject19, DefaultSkills.Charm)
		};
		EducationCharacterProperties[] childProperties45 = new EducationCharacterProperties[6]
		{
			new EducationCharacterProperties("act_childhood_grit", "carry_hammer", useOffHand: false),
			new EducationCharacterProperties("act_childhood_manners_3", "carry_book", useOffHand: false),
			new EducationCharacterProperties("act_childhood_artisan", "carry_linen", useOffHand: false),
			new EducationCharacterProperties("act_childhood_genius"),
			new EducationCharacterProperties("act_childhood_tactician"),
			new EducationCharacterProperties("act_childhood_manners")
		};
		Stage16Selection(titleDescSkillTuple3, currentPage3, childProperties45);
		EducationPage currentPage4 = educationStage.AddPage(2, title, description7, _confirmResultsText, default(EducationCharacterProperties), default(EducationCharacterProperties), (EducationPage p, List<EducationOption> previousOptions) => previousOptions.Contains(stage_5_page_0_nobleOption));
		(TextObject, TextObject, SkillObject)[] titleDescSkillTuple4 = new(TextObject, TextObject, SkillObject)[6]
		{
			(item19, textObject20, DefaultSkills.Athletics),
			(item20, textObject21, DefaultSkills.Bow),
			(item21, textObject22, DefaultSkills.Riding),
			(item22, textObject23, DefaultSkills.Tactics),
			(item23, textObject24, DefaultSkills.Leadership),
			(item24, textObject25, DefaultSkills.OneHanded)
		};
		EducationCharacterProperties[] childProperties46 = new EducationCharacterProperties[6]
		{
			new EducationCharacterProperties("act_childhood_athlete"),
			new EducationCharacterProperties("act_conversation_confident2_loop"),
			new EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", useOffHand: true),
			new EducationCharacterProperties("act_childhood_tactician"),
			new EducationCharacterProperties("act_conversation_hip_loop"),
			new EducationCharacterProperties("act_childhood_apprentice", "training_sword", useOffHand: false)
		};
		Stage16Selection(titleDescSkillTuple4, currentPage4, childProperties46);
		EducationPage currentPage5 = educationStage.AddPage(2, title, description7, _confirmResultsText, default(EducationCharacterProperties), default(EducationCharacterProperties), (EducationPage p, List<EducationOption> previousOptions) => previousOptions.Contains(stage_5_page_0_ownWayOption));
		(TextObject, TextObject, SkillObject)[] titleDescSkillTuple5 = new(TextObject, TextObject, SkillObject)[6]
		{
			(item25, textObject26, DefaultSkills.Athletics),
			(item26, textObject27, DefaultSkills.Roguery),
			(item27, textObject28, DefaultSkills.Polearm),
			(item28, textObject29, DefaultSkills.Riding),
			(item29, textObject30, DefaultSkills.Charm),
			(item30, textObject31, DefaultSkills.Crafting)
		};
		EducationCharacterProperties[] childProperties47 = new EducationCharacterProperties[6]
		{
			new EducationCharacterProperties("act_childhood_athlete"),
			new EducationCharacterProperties("act_childhood_roguery", "carry_bostaff_rogue1", useOffHand: false),
			new EducationCharacterProperties("act_childhood_guard_up_staff", "carry_bostaff", useOffHand: false),
			new EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", useOffHand: true),
			new EducationCharacterProperties("act_childhood_manners"),
			new EducationCharacterProperties("act_childhood_grit", "carry_hammer", useOffHand: false)
		};
		Stage16Selection(titleDescSkillTuple5, currentPage5, childProperties47);
		EducationPage currentPage6 = educationStage.AddPage(2, title, description7, _confirmResultsText, default(EducationCharacterProperties), default(EducationCharacterProperties), (EducationPage p, List<EducationOption> previousOptions) => previousOptions.Contains(stage_5_page_0_diplomatOption));
		(TextObject, TextObject, SkillObject)[] titleDescSkillTuple6 = new(TextObject, TextObject, SkillObject)[6]
		{
			(item31, textObject32, DefaultSkills.Medicine),
			(item32, textObject33, DefaultSkills.Roguery),
			(item33, textObject34, DefaultSkills.Charm),
			(item34, textObject35, DefaultSkills.OneHanded),
			(item35, textObject36, DefaultSkills.Riding),
			(item36, textObject37, DefaultSkills.Tactics)
		};
		EducationCharacterProperties[] childProperties48 = new EducationCharacterProperties[6]
		{
			new EducationCharacterProperties("act_childhood_ready", "carry_book", useOffHand: false),
			new EducationCharacterProperties("act_childhood_roguery", "carry_bostaff_rogue1", useOffHand: false),
			new EducationCharacterProperties("act_childhood_manners"),
			new EducationCharacterProperties("act_childhood_apprentice", "vlandia_twohanded_sword_c", useOffHand: false),
			new EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", useOffHand: true),
			new EducationCharacterProperties("act_childhood_tactician")
		};
		Stage16Selection(titleDescSkillTuple6, currentPage6, childProperties48);
		return educationStage;
	}

	private void GetHighestThreeAttributes(Hero hero, out (CharacterAttribute, int)[] maxAttributes)
	{
		(CharacterAttribute, int)[] array = new(CharacterAttribute, int)[Attributes.All.Count];
		for (int i = 0; i < Attributes.All.Count; i++)
		{
			CharacterAttribute characterAttribute = Attributes.All[i];
			array[i] = (characterAttribute, hero?.GetAttributeValue(characterAttribute) ?? MBRandom.RandomInt(1, 10));
		}
		maxAttributes = array.OrderByDescending(((CharacterAttribute, int) x) => x.Item2).Take(3).ToArray();
	}
}
