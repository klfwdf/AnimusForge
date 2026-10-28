using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CharacterCreationContent;

public sealed class NarrativeMenuOption
{
	public readonly string StringId;

	public readonly TextObject Text;

	public readonly TextObject DescriptionText;

	private NarrativeMenuOptionOnConditionDelegate _onConditionInternal;

	private NarrativeMenuOptionOnSelectDelegate _onSelectInternal;

	private NarrativeMenuOptionOnConsequenceDelegate _onConsequenceInternal;

	private readonly GetNarrativeMenuOptionArgsDelegate _getNarrativeMenuOptionArgs;

	public readonly NarrativeMenuOptionArgs Args;

	public TextObject PositiveEffectText => Args.PositiveEffectText;

	public NarrativeMenuOption(string stringId, TextObject text, TextObject descriptionText, GetNarrativeMenuOptionArgsDelegate getNarrativeMenuOptionArgs, NarrativeMenuOptionOnConditionDelegate onCondition, NarrativeMenuOptionOnSelectDelegate onSelect, NarrativeMenuOptionOnConsequenceDelegate onConsequence)
	{
		StringId = stringId;
		Text = text;
		DescriptionText = descriptionText;
		_onConditionInternal = onCondition;
		_onSelectInternal = onSelect;
		_onConsequenceInternal = onConsequence;
		_getNarrativeMenuOptionArgs = getNarrativeMenuOptionArgs;
		Args = new NarrativeMenuOptionArgs();
	}

	public bool OnCondition(CharacterCreationManager characterCreationManager)
	{
		if (_onConditionInternal != null)
		{
			return _onConditionInternal(characterCreationManager);
		}
		return true;
	}

	public void OnSelect(CharacterCreationManager characterCreationManager)
	{
		_getNarrativeMenuOptionArgs?.Invoke(Args);
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.IsHuman)
			{
				character.SetRightHandItem("");
				character.SetLeftHandItem("");
				character.EquipLeftHandItemWithEquipmentIndex(EquipmentIndex.WeaponItemBeginSlot);
				character.EquipRightHandItemWithEquipmentIndex(EquipmentIndex.Weapon1);
			}
		}
		_onSelectInternal?.Invoke(characterCreationManager);
	}

	public void OnConsequence(CharacterCreationManager characterCreationManager)
	{
		_onConsequenceInternal?.Invoke(characterCreationManager);
	}

	public void SetOnCondition(NarrativeMenuOptionOnConditionDelegate onCondition)
	{
		_onConditionInternal = onCondition;
	}

	public void SetOnSelect(NarrativeMenuOptionOnSelectDelegate onSelect)
	{
		_onSelectInternal = onSelect;
	}

	public void SetOnConsequence(NarrativeMenuOptionOnConsequenceDelegate onConsequence)
	{
		_onConsequenceInternal = onConsequence;
	}

	public void ApplyFinalEffects(CharacterCreationContent characterCreationContent)
	{
		characterCreationContent.ApplySkillAndAttributeEffects(Args.AffectedSkills.ToList(), Args.FocusToAdd, Args.SkillLevelToAdd, Args.EffectedAttribute, Args.AttributeLevelToAdd, Args.AffectedTraits.ToList(), Args.TraitLevelToAdd, Args.RenownToAdd, Args.GoldToAdd, Args.UnspentFocusToAdd, Args.UnspentAttributeToAdd);
	}
}
