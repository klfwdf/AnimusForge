using System;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CharacterCreationContent
{
	// Token: 0x02000215 RID: 533
	public sealed class NarrativeMenuOption
	{
		// Token: 0x170007E5 RID: 2021
		// (get) Token: 0x06001FFC RID: 8188 RVA: 0x0008E8BE File Offset: 0x0008CABE
		public TextObject PositiveEffectText
		{
			get
			{
				return this.Args.PositiveEffectText;
			}
		}

		// Token: 0x06001FFD RID: 8189 RVA: 0x0008E8CC File Offset: 0x0008CACC
		public NarrativeMenuOption(string stringId, TextObject text, TextObject descriptionText, GetNarrativeMenuOptionArgsDelegate getNarrativeMenuOptionArgs, NarrativeMenuOptionOnConditionDelegate onCondition, NarrativeMenuOptionOnSelectDelegate onSelect, NarrativeMenuOptionOnConsequenceDelegate onConsequence)
		{
			this.StringId = stringId;
			this.Text = text;
			this.DescriptionText = descriptionText;
			this._onConditionInternal = onCondition;
			this._onSelectInternal = onSelect;
			this._onConsequenceInternal = onConsequence;
			this._getNarrativeMenuOptionArgs = getNarrativeMenuOptionArgs;
			this.Args = new NarrativeMenuOptionArgs();
		}

		// Token: 0x06001FFE RID: 8190 RVA: 0x0008E91F File Offset: 0x0008CB1F
		public bool OnCondition(CharacterCreationManager characterCreationManager)
		{
			return this._onConditionInternal == null || this._onConditionInternal(characterCreationManager);
		}

		// Token: 0x06001FFF RID: 8191 RVA: 0x0008E938 File Offset: 0x0008CB38
		public void OnSelect(CharacterCreationManager characterCreationManager)
		{
			GetNarrativeMenuOptionArgsDelegate getNarrativeMenuOptionArgs = this._getNarrativeMenuOptionArgs;
			if (getNarrativeMenuOptionArgs != null)
			{
				getNarrativeMenuOptionArgs(this.Args);
			}
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.IsHuman)
				{
					narrativeMenuCharacter.SetRightHandItem("");
					narrativeMenuCharacter.SetLeftHandItem("");
					narrativeMenuCharacter.EquipLeftHandItemWithEquipmentIndex(EquipmentIndex.WeaponItemBeginSlot);
					narrativeMenuCharacter.EquipRightHandItemWithEquipmentIndex(EquipmentIndex.Weapon1);
				}
			}
			NarrativeMenuOptionOnSelectDelegate onSelectInternal = this._onSelectInternal;
			if (onSelectInternal == null)
			{
				return;
			}
			onSelectInternal(characterCreationManager);
		}

		// Token: 0x06002000 RID: 8192 RVA: 0x0008E9E0 File Offset: 0x0008CBE0
		public void OnConsequence(CharacterCreationManager characterCreationManager)
		{
			NarrativeMenuOptionOnConsequenceDelegate onConsequenceInternal = this._onConsequenceInternal;
			if (onConsequenceInternal == null)
			{
				return;
			}
			onConsequenceInternal(characterCreationManager);
		}

		// Token: 0x06002001 RID: 8193 RVA: 0x0008E9F3 File Offset: 0x0008CBF3
		public void SetOnCondition(NarrativeMenuOptionOnConditionDelegate onCondition)
		{
			this._onConditionInternal = onCondition;
		}

		// Token: 0x06002002 RID: 8194 RVA: 0x0008E9FC File Offset: 0x0008CBFC
		public void SetOnSelect(NarrativeMenuOptionOnSelectDelegate onSelect)
		{
			this._onSelectInternal = onSelect;
		}

		// Token: 0x06002003 RID: 8195 RVA: 0x0008EA05 File Offset: 0x0008CC05
		public void SetOnConsequence(NarrativeMenuOptionOnConsequenceDelegate onConsequence)
		{
			this._onConsequenceInternal = onConsequence;
		}

		// Token: 0x06002004 RID: 8196 RVA: 0x0008EA10 File Offset: 0x0008CC10
		public void ApplyFinalEffects(CharacterCreationContent characterCreationContent)
		{
			characterCreationContent.ApplySkillAndAttributeEffects(this.Args.AffectedSkills.ToList<SkillObject>(), this.Args.FocusToAdd, this.Args.SkillLevelToAdd, this.Args.EffectedAttribute, this.Args.AttributeLevelToAdd, this.Args.AffectedTraits.ToList<TraitObject>(), this.Args.TraitLevelToAdd, this.Args.RenownToAdd, this.Args.GoldToAdd, this.Args.UnspentFocusToAdd, this.Args.UnspentAttributeToAdd);
		}

		// Token: 0x0400095E RID: 2398
		public readonly string StringId;

		// Token: 0x0400095F RID: 2399
		public readonly TextObject Text;

		// Token: 0x04000960 RID: 2400
		public readonly TextObject DescriptionText;

		// Token: 0x04000961 RID: 2401
		private NarrativeMenuOptionOnConditionDelegate _onConditionInternal;

		// Token: 0x04000962 RID: 2402
		private NarrativeMenuOptionOnSelectDelegate _onSelectInternal;

		// Token: 0x04000963 RID: 2403
		private NarrativeMenuOptionOnConsequenceDelegate _onConsequenceInternal;

		// Token: 0x04000964 RID: 2404
		private readonly GetNarrativeMenuOptionArgsDelegate _getNarrativeMenuOptionArgs;

		// Token: 0x04000965 RID: 2405
		public readonly NarrativeMenuOptionArgs Args;
	}
}
