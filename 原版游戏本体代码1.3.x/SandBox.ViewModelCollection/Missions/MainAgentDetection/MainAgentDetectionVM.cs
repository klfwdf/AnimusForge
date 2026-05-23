using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Missions.MainAgentDetection
{
	// Token: 0x02000041 RID: 65
	public class MainAgentDetectionVM : ViewModel
	{
		// Token: 0x0600042B RID: 1067 RVA: 0x000110CE File Offset: 0x0000F2CE
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.SuspicionFullText = new TextObject("{=KgTFCWG8}You are suspicious", null).ToString();
		}

		// Token: 0x0600042C RID: 1068 RVA: 0x000110EC File Offset: 0x0000F2EC
		public void UpdateDetectionValues(float minDetectionLevel, float maxDetectionLevel, float currentDetectionLevel)
		{
			this.MinimumDetectionLevel = minDetectionLevel;
			this.MaximumDetectionLevel = maxDetectionLevel;
			this.CurrentDetectionLevel = currentDetectionLevel;
			this.CurrentDetectionLevelRatio = MBMath.InverseLerp(this.MinimumDetectionLevel, this.MaximumDetectionLevel, this.CurrentDetectionLevel);
			this.HasDetection = this.CurrentDetectionLevel > 0f;
			this.HasReachedSuspicionTreshold = this.CurrentDetectionLevel >= this.MaximumDetectionLevel;
		}

		// Token: 0x1700013F RID: 319
		// (get) Token: 0x0600042D RID: 1069 RVA: 0x00011155 File Offset: 0x0000F355
		// (set) Token: 0x0600042E RID: 1070 RVA: 0x0001115D File Offset: 0x0000F35D
		[DataSourceProperty]
		public bool HasDetection
		{
			get
			{
				return this._hasDetection;
			}
			set
			{
				if (value != this._hasDetection)
				{
					this._hasDetection = value;
					base.OnPropertyChangedWithValue(value, "HasDetection");
				}
			}
		}

		// Token: 0x17000140 RID: 320
		// (get) Token: 0x0600042F RID: 1071 RVA: 0x0001117B File Offset: 0x0000F37B
		// (set) Token: 0x06000430 RID: 1072 RVA: 0x00011183 File Offset: 0x0000F383
		[DataSourceProperty]
		public bool HasReachedSuspicionTreshold
		{
			get
			{
				return this._hasReachedSuspicionTreshold;
			}
			set
			{
				if (value != this._hasReachedSuspicionTreshold)
				{
					this._hasReachedSuspicionTreshold = value;
					base.OnPropertyChangedWithValue(value, "HasReachedSuspicionTreshold");
				}
			}
		}

		// Token: 0x17000141 RID: 321
		// (get) Token: 0x06000431 RID: 1073 RVA: 0x000111A1 File Offset: 0x0000F3A1
		// (set) Token: 0x06000432 RID: 1074 RVA: 0x000111A9 File Offset: 0x0000F3A9
		[DataSourceProperty]
		public float MinimumDetectionLevel
		{
			get
			{
				return this._minimumDetectionLevel;
			}
			set
			{
				if (value != this._minimumDetectionLevel)
				{
					this._minimumDetectionLevel = value;
					base.OnPropertyChangedWithValue(value, "MinimumDetectionLevel");
				}
			}
		}

		// Token: 0x17000142 RID: 322
		// (get) Token: 0x06000433 RID: 1075 RVA: 0x000111C7 File Offset: 0x0000F3C7
		// (set) Token: 0x06000434 RID: 1076 RVA: 0x000111CF File Offset: 0x0000F3CF
		[DataSourceProperty]
		public float MaximumDetectionLevel
		{
			get
			{
				return this._maximumDetectionLevel;
			}
			set
			{
				if (value != this._maximumDetectionLevel)
				{
					this._maximumDetectionLevel = value;
					base.OnPropertyChangedWithValue(value, "MaximumDetectionLevel");
				}
			}
		}

		// Token: 0x17000143 RID: 323
		// (get) Token: 0x06000435 RID: 1077 RVA: 0x000111ED File Offset: 0x0000F3ED
		// (set) Token: 0x06000436 RID: 1078 RVA: 0x000111F5 File Offset: 0x0000F3F5
		[DataSourceProperty]
		public float CurrentDetectionLevel
		{
			get
			{
				return this._currentDetectionLevel;
			}
			set
			{
				if (value != this._currentDetectionLevel)
				{
					this._currentDetectionLevel = value;
					base.OnPropertyChangedWithValue(value, "CurrentDetectionLevel");
				}
			}
		}

		// Token: 0x17000144 RID: 324
		// (get) Token: 0x06000437 RID: 1079 RVA: 0x00011213 File Offset: 0x0000F413
		// (set) Token: 0x06000438 RID: 1080 RVA: 0x0001121B File Offset: 0x0000F41B
		[DataSourceProperty]
		public float CurrentDetectionLevelRatio
		{
			get
			{
				return this._currentDetectionLevelRatio;
			}
			set
			{
				if (value != this._currentDetectionLevelRatio)
				{
					this._currentDetectionLevelRatio = value;
					base.OnPropertyChangedWithValue(value, "CurrentDetectionLevelRatio");
				}
			}
		}

		// Token: 0x17000145 RID: 325
		// (get) Token: 0x06000439 RID: 1081 RVA: 0x00011239 File Offset: 0x0000F439
		// (set) Token: 0x0600043A RID: 1082 RVA: 0x00011241 File Offset: 0x0000F441
		[DataSourceProperty]
		public string SuspicionFullText
		{
			get
			{
				return this._suspicionFullText;
			}
			set
			{
				if (value != this._suspicionFullText)
				{
					this._suspicionFullText = value;
					base.OnPropertyChangedWithValue<string>(value, "SuspicionFullText");
				}
			}
		}

		// Token: 0x0400021F RID: 543
		private bool _hasDetection;

		// Token: 0x04000220 RID: 544
		private bool _hasReachedSuspicionTreshold;

		// Token: 0x04000221 RID: 545
		private float _minimumDetectionLevel;

		// Token: 0x04000222 RID: 546
		private float _maximumDetectionLevel;

		// Token: 0x04000223 RID: 547
		private float _currentDetectionLevel;

		// Token: 0x04000224 RID: 548
		private float _currentDetectionLevelRatio;

		// Token: 0x04000225 RID: 549
		private string _suspicionFullText;
	}
}
