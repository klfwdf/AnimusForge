using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Missions.MainAgentDetection
{
	// Token: 0x02000044 RID: 68
	public class MissionLosingTargetVM : ViewModel
	{
		// Token: 0x06000458 RID: 1112 RVA: 0x00011693 File Offset: 0x0000F893
		public MissionLosingTargetVM()
		{
			this.RefreshValues();
		}

		// Token: 0x06000459 RID: 1113 RVA: 0x000116A1 File Offset: 0x0000F8A1
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.LosingTargetWarningText = new TextObject("{=kXy4R7ca}You are about to lose the target.", null).ToString();
		}

		// Token: 0x0600045A RID: 1114 RVA: 0x000116BF File Offset: 0x0000F8BF
		public void UpdateLosingTargetValues(bool isLosingTarget, float losingTargetTimer, float losingTargetTreshold)
		{
			this.IsLosingTarget = isLosingTarget;
			this.LosingTargetRatio = MathF.Clamp(losingTargetTimer / losingTargetTreshold * 100f, 0f, 100f);
		}

		// Token: 0x17000152 RID: 338
		// (get) Token: 0x0600045B RID: 1115 RVA: 0x000116E6 File Offset: 0x0000F8E6
		// (set) Token: 0x0600045C RID: 1116 RVA: 0x000116EE File Offset: 0x0000F8EE
		[DataSourceProperty]
		public bool IsLosingTarget
		{
			get
			{
				return this._isLosingTarget;
			}
			set
			{
				if (value != this._isLosingTarget)
				{
					this._isLosingTarget = value;
					base.OnPropertyChangedWithValue(value, "IsLosingTarget");
				}
			}
		}

		// Token: 0x17000153 RID: 339
		// (get) Token: 0x0600045D RID: 1117 RVA: 0x0001170C File Offset: 0x0000F90C
		// (set) Token: 0x0600045E RID: 1118 RVA: 0x00011714 File Offset: 0x0000F914
		[DataSourceProperty]
		public float LosingTargetRatio
		{
			get
			{
				return this._losingTargetRatio;
			}
			set
			{
				if (value != this._losingTargetRatio)
				{
					this._losingTargetRatio = value;
					base.OnPropertyChangedWithValue(value, "LosingTargetRatio");
				}
			}
		}

		// Token: 0x17000154 RID: 340
		// (get) Token: 0x0600045F RID: 1119 RVA: 0x00011732 File Offset: 0x0000F932
		// (set) Token: 0x06000460 RID: 1120 RVA: 0x0001173A File Offset: 0x0000F93A
		[DataSourceProperty]
		public string LosingTargetWarningText
		{
			get
			{
				return this._losingTargetWarningText;
			}
			set
			{
				if (value != this._losingTargetWarningText)
				{
					this._losingTargetWarningText = value;
					base.OnPropertyChangedWithValue<string>(value, "LosingTargetWarningText");
				}
			}
		}

		// Token: 0x04000235 RID: 565
		private bool _isLosingTarget;

		// Token: 0x04000236 RID: 566
		private float _losingTargetRatio;

		// Token: 0x04000237 RID: 567
		private string _losingTargetWarningText;
	}
}
