using System;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Education
{
	// Token: 0x020000F7 RID: 247
	public class EducationOptionVM : StringItemWithActionVM
	{
		// Token: 0x1700075B RID: 1883
		// (get) Token: 0x06001633 RID: 5683 RVA: 0x00056993 File Offset: 0x00054B93
		// (set) Token: 0x06001634 RID: 5684 RVA: 0x0005699B File Offset: 0x00054B9B
		public string OptionEffect { get; private set; }

		// Token: 0x1700075C RID: 1884
		// (get) Token: 0x06001635 RID: 5685 RVA: 0x000569A4 File Offset: 0x00054BA4
		// (set) Token: 0x06001636 RID: 5686 RVA: 0x000569AC File Offset: 0x00054BAC
		public string OptionDescription { get; private set; }

		// Token: 0x1700075D RID: 1885
		// (get) Token: 0x06001637 RID: 5687 RVA: 0x000569B5 File Offset: 0x00054BB5
		// (set) Token: 0x06001638 RID: 5688 RVA: 0x000569BD File Offset: 0x00054BBD
		public EducationCampaignBehavior.EducationCharacterProperties[] CharacterProperties { get; private set; }

		// Token: 0x1700075E RID: 1886
		// (get) Token: 0x06001639 RID: 5689 RVA: 0x000569C6 File Offset: 0x00054BC6
		// (set) Token: 0x0600163A RID: 5690 RVA: 0x000569CE File Offset: 0x00054BCE
		public string ActionID { get; private set; }

		// Token: 0x1700075F RID: 1887
		// (get) Token: 0x0600163B RID: 5691 RVA: 0x000569D7 File Offset: 0x00054BD7
		// (set) Token: 0x0600163C RID: 5692 RVA: 0x000569DF File Offset: 0x00054BDF
		public ValueTuple<CharacterAttribute, int>[] OptionAttributes { get; private set; }

		// Token: 0x17000760 RID: 1888
		// (get) Token: 0x0600163D RID: 5693 RVA: 0x000569E8 File Offset: 0x00054BE8
		// (set) Token: 0x0600163E RID: 5694 RVA: 0x000569F0 File Offset: 0x00054BF0
		public ValueTuple<SkillObject, int>[] OptionSkills { get; private set; }

		// Token: 0x17000761 RID: 1889
		// (get) Token: 0x0600163F RID: 5695 RVA: 0x000569F9 File Offset: 0x00054BF9
		// (set) Token: 0x06001640 RID: 5696 RVA: 0x00056A01 File Offset: 0x00054C01
		public ValueTuple<SkillObject, int>[] OptionFocusPoints { get; private set; }

		// Token: 0x06001641 RID: 5697 RVA: 0x00056A0C File Offset: 0x00054C0C
		public EducationOptionVM(Action<object> onExecute, string optionId, TextObject optionText, TextObject optionDescription, TextObject optionEffect, bool isSelected, ValueTuple<CharacterAttribute, int>[] optionAttributes, ValueTuple<SkillObject, int>[] optionSkills, ValueTuple<SkillObject, int>[] optionFocusPoints, EducationCampaignBehavior.EducationCharacterProperties[] characterProperties)
			: base(onExecute, optionText.ToString(), optionId)
		{
			this.IsSelected = isSelected;
			this.CharacterProperties = characterProperties;
			this._optionTextObject = optionText;
			this._optionDescriptionObject = optionDescription;
			this._optionEffectObject = optionEffect;
			this.OptionAttributes = optionAttributes;
			this.OptionSkills = optionSkills;
			this.OptionFocusPoints = optionFocusPoints;
			this.RefreshValues();
		}

		// Token: 0x06001642 RID: 5698 RVA: 0x00056A6C File Offset: 0x00054C6C
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.OptionEffect = this._optionEffectObject.ToString();
			this.OptionDescription = this._optionDescriptionObject.ToString();
			base.ActionText = this._optionTextObject.ToString();
		}

		// Token: 0x17000762 RID: 1890
		// (get) Token: 0x06001643 RID: 5699 RVA: 0x00056AA7 File Offset: 0x00054CA7
		// (set) Token: 0x06001644 RID: 5700 RVA: 0x00056AAF File Offset: 0x00054CAF
		[DataSourceProperty]
		public bool IsSelected
		{
			get
			{
				return this._isSelected;
			}
			set
			{
				if (value != this._isSelected)
				{
					this._isSelected = value;
					base.OnPropertyChangedWithValue(value, "IsSelected");
				}
			}
		}

		// Token: 0x04000A2D RID: 2605
		private readonly TextObject _optionTextObject;

		// Token: 0x04000A2E RID: 2606
		private readonly TextObject _optionDescriptionObject;

		// Token: 0x04000A2F RID: 2607
		private readonly TextObject _optionEffectObject;

		// Token: 0x04000A30 RID: 2608
		private bool _isSelected;
	}
}
