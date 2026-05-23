using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Conversation.Persuasion
{
	// Token: 0x020002A0 RID: 672
	public class Persuasion
	{
		// Token: 0x170008E8 RID: 2280
		// (get) Token: 0x060023CF RID: 9167 RVA: 0x000999FF File Offset: 0x00097BFF
		public float DifficultyMultiplier
		{
			get
			{
				return this._difficultyMultiplier;
			}
		}

		// Token: 0x170008E9 RID: 2281
		// (get) Token: 0x060023D0 RID: 9168 RVA: 0x00099A07 File Offset: 0x00097C07
		// (set) Token: 0x060023D1 RID: 9169 RVA: 0x00099A0F File Offset: 0x00097C0F
		public float Progress { get; private set; }

		// Token: 0x060023D2 RID: 9170 RVA: 0x00099A18 File Offset: 0x00097C18
		public Persuasion(float goalValue, float successValue, float failValue, float criticalSuccessValue, float criticalFailValue, float initialProgress, PersuasionDifficulty difficulty)
		{
			this._chosenOptions = new List<Tuple<PersuasionOptionArgs, PersuasionOptionResult>>();
			this.GoalValue = Campaign.Current.Models.PersuasionModel.CalculatePersuasionGoalValue(CharacterObject.OneToOneConversationCharacter, goalValue);
			this.SuccessValue = successValue;
			this.FailValue = failValue;
			this.CriticalSuccessValue = criticalSuccessValue;
			this.CriticalFailValue = criticalFailValue;
			this._difficulty = difficulty;
			if (initialProgress < 0f)
			{
				this.Progress = Campaign.Current.Models.PersuasionModel.CalculateInitialPersuasionProgress(CharacterObject.OneToOneConversationCharacter, this.GoalValue, this.SuccessValue);
			}
			else
			{
				this.Progress = initialProgress;
			}
			this._difficultyMultiplier = Campaign.Current.Models.PersuasionModel.GetDifficulty(difficulty);
		}

		// Token: 0x060023D3 RID: 9171 RVA: 0x00099AD8 File Offset: 0x00097CD8
		public void CommitProgress(PersuasionOptionArgs persuasionOptionArgs)
		{
			PersuasionOptionResult persuasionOptionResult = this.GetResult(persuasionOptionArgs);
			persuasionOptionResult = this.CheckPerkEffectOnResult(persuasionOptionResult);
			Tuple<PersuasionOptionArgs, PersuasionOptionResult> tuple = new Tuple<PersuasionOptionArgs, PersuasionOptionResult>(persuasionOptionArgs, persuasionOptionResult);
			persuasionOptionArgs.BlockTheOption(true);
			this._chosenOptions.Add(tuple);
			this.Progress = MathF.Clamp(this.Progress + this.GetPersuasionOptionResultValue(persuasionOptionResult), 0f, this.GoalValue);
			CampaignEventDispatcher.Instance.OnPersuasionProgressCommitted(tuple);
		}

		// Token: 0x060023D4 RID: 9172 RVA: 0x00099B40 File Offset: 0x00097D40
		private PersuasionOptionResult CheckPerkEffectOnResult(PersuasionOptionResult result)
		{
			PersuasionOptionResult result2 = result;
			if (result == PersuasionOptionResult.CriticalFailure && Hero.MainHero.GetPerkValue(DefaultPerks.Charm.ForgivableGrievances) && MBRandom.RandomFloat <= DefaultPerks.Charm.ForgivableGrievances.PrimaryBonus)
			{
				TextObject textObject = new TextObject("{=5IQriov5}You avoided critical failure because of {PERK_NAME}.", null);
				textObject.SetTextVariable("PERK_NAME", DefaultPerks.Charm.ForgivableGrievances.Name);
				InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), Color.White));
				result2 = PersuasionOptionResult.Failure;
			}
			return result2;
		}

		// Token: 0x060023D5 RID: 9173 RVA: 0x00099BAC File Offset: 0x00097DAC
		private float GetPersuasionOptionResultValue(PersuasionOptionResult result)
		{
			switch (result)
			{
			case PersuasionOptionResult.CriticalFailure:
				return -this.CriticalFailValue;
			case PersuasionOptionResult.Failure:
				return 0f;
			case PersuasionOptionResult.Success:
				return this.SuccessValue;
			case PersuasionOptionResult.CriticalSuccess:
				return this.CriticalSuccessValue;
			case PersuasionOptionResult.Miss:
				return 0f;
			default:
				return 0f;
			}
		}

		// Token: 0x060023D6 RID: 9174 RVA: 0x00099BFC File Offset: 0x00097DFC
		private PersuasionOptionResult GetResult(PersuasionOptionArgs optionArgs)
		{
			float num;
			float num2;
			float num3;
			float num4;
			Campaign.Current.Models.PersuasionModel.GetChances(optionArgs, out num, out num2, out num3, out num4, this._difficultyMultiplier);
			float num5 = MBRandom.RandomFloat;
			if (num5 < num2)
			{
				return PersuasionOptionResult.CriticalSuccess;
			}
			num5 -= num2;
			if (num5 < num)
			{
				return PersuasionOptionResult.Success;
			}
			num5 -= num;
			if (num5 < num4)
			{
				return PersuasionOptionResult.Failure;
			}
			num5 -= num4;
			if (num5 < num3)
			{
				return PersuasionOptionResult.CriticalFailure;
			}
			return PersuasionOptionResult.Miss;
		}

		// Token: 0x060023D7 RID: 9175 RVA: 0x00099C62 File Offset: 0x00097E62
		public IEnumerable<Tuple<PersuasionOptionArgs, PersuasionOptionResult>> GetChosenOptions()
		{
			return this._chosenOptions.AsReadOnly();
		}

		// Token: 0x04000AD0 RID: 2768
		public readonly float SuccessValue;

		// Token: 0x04000AD1 RID: 2769
		public readonly float FailValue;

		// Token: 0x04000AD2 RID: 2770
		public readonly float CriticalSuccessValue;

		// Token: 0x04000AD3 RID: 2771
		public readonly float CriticalFailValue;

		// Token: 0x04000AD4 RID: 2772
		private readonly float _difficultyMultiplier;

		// Token: 0x04000AD5 RID: 2773
		private readonly PersuasionDifficulty _difficulty;

		// Token: 0x04000AD6 RID: 2774
		private readonly List<Tuple<PersuasionOptionArgs, PersuasionOptionResult>> _chosenOptions;

		// Token: 0x04000AD7 RID: 2775
		public readonly float GoalValue;
	}
}
