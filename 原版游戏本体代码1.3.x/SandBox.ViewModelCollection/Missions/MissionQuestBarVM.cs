using System;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Missions
{
	// Token: 0x0200002E RID: 46
	public class MissionQuestBarVM : ViewModel
	{
		// Token: 0x060003A9 RID: 937 RVA: 0x0000F9FC File Offset: 0x0000DBFC
		public void UpdateQuestValues(float minDetectionLevel, float maxDetectionLevel, float currentDetectionLevel)
		{
			this.MinimumQuestLevel = minDetectionLevel;
			this.MaximumQuestLevel = maxDetectionLevel;
			this.CurrentQuestLevel = currentDetectionLevel;
			this.CurrentQuestLevelRatio = MBMath.InverseLerp(this.MinimumQuestLevel, this.MaximumQuestLevel, this.CurrentQuestLevel);
			this.HasQuestLevel = this.CurrentQuestLevel > 0f;
		}

		// Token: 0x17000125 RID: 293
		// (get) Token: 0x060003AA RID: 938 RVA: 0x0000FA4E File Offset: 0x0000DC4E
		// (set) Token: 0x060003AB RID: 939 RVA: 0x0000FA56 File Offset: 0x0000DC56
		[DataSourceProperty]
		public bool HasQuestLevel
		{
			get
			{
				return this._hasQuestLevel;
			}
			set
			{
				if (value != this._hasQuestLevel)
				{
					this._hasQuestLevel = value;
					base.OnPropertyChangedWithValue(value, "HasQuestLevel");
				}
			}
		}

		// Token: 0x17000126 RID: 294
		// (get) Token: 0x060003AC RID: 940 RVA: 0x0000FA74 File Offset: 0x0000DC74
		// (set) Token: 0x060003AD RID: 941 RVA: 0x0000FA7C File Offset: 0x0000DC7C
		[DataSourceProperty]
		public float MinimumQuestLevel
		{
			get
			{
				return this._minimumQuestLevel;
			}
			set
			{
				if (value != this._minimumQuestLevel)
				{
					this._minimumQuestLevel = value;
					base.OnPropertyChangedWithValue(value, "MinimumQuestLevel");
				}
			}
		}

		// Token: 0x17000127 RID: 295
		// (get) Token: 0x060003AE RID: 942 RVA: 0x0000FA9A File Offset: 0x0000DC9A
		// (set) Token: 0x060003AF RID: 943 RVA: 0x0000FAA2 File Offset: 0x0000DCA2
		[DataSourceProperty]
		public float MaximumQuestLevel
		{
			get
			{
				return this._maximumQuestLevel;
			}
			set
			{
				if (value != this._maximumQuestLevel)
				{
					this._maximumQuestLevel = value;
					base.OnPropertyChangedWithValue(value, "MaximumQuestLevel");
				}
			}
		}

		// Token: 0x17000128 RID: 296
		// (get) Token: 0x060003B0 RID: 944 RVA: 0x0000FAC0 File Offset: 0x0000DCC0
		// (set) Token: 0x060003B1 RID: 945 RVA: 0x0000FAC8 File Offset: 0x0000DCC8
		[DataSourceProperty]
		public float CurrentQuestLevel
		{
			get
			{
				return this._currentQuestLevel;
			}
			set
			{
				if (value != this._currentQuestLevel)
				{
					this._currentQuestLevel = value;
					base.OnPropertyChangedWithValue(value, "CurrentQuestLevel");
				}
			}
		}

		// Token: 0x17000129 RID: 297
		// (get) Token: 0x060003B2 RID: 946 RVA: 0x0000FAE6 File Offset: 0x0000DCE6
		// (set) Token: 0x060003B3 RID: 947 RVA: 0x0000FAEE File Offset: 0x0000DCEE
		[DataSourceProperty]
		public float CurrentQuestLevelRatio
		{
			get
			{
				return this._currentQuestLevelRatio;
			}
			set
			{
				if (value != this._currentQuestLevelRatio)
				{
					this._currentQuestLevelRatio = value;
					base.OnPropertyChangedWithValue(value, "CurrentQuestLevelRatio");
				}
			}
		}

		// Token: 0x040001DD RID: 477
		private bool _hasQuestLevel;

		// Token: 0x040001DE RID: 478
		private float _minimumQuestLevel;

		// Token: 0x040001DF RID: 479
		private float _maximumQuestLevel;

		// Token: 0x040001E0 RID: 480
		private float _currentQuestLevel;

		// Token: 0x040001E1 RID: 481
		private float _currentQuestLevelRatio;
	}
}
