using System;
using TaleWorlds.CampaignSystem.ViewModelCollection.Quests;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.Missions.NameMarker
{
	// Token: 0x02000033 RID: 51
	public abstract class MissionNameMarkerTargetBaseVM : ViewModel
	{
		// Token: 0x060003CC RID: 972 RVA: 0x0000FFAE File Offset: 0x0000E1AE
		public MissionNameMarkerTargetBaseVM()
		{
			this.Quests = new MBBindingList<QuestMarkerVM>();
		}

		// Token: 0x060003CD RID: 973
		public abstract void UpdatePosition(Camera missionCamera);

		// Token: 0x060003CE RID: 974
		public abstract bool Equals(MissionNameMarkerTargetBaseVM other);

		// Token: 0x060003CF RID: 975
		protected abstract TextObject GetName();

		// Token: 0x060003D0 RID: 976 RVA: 0x0000FFD7 File Offset: 0x0000E1D7
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Name = this.GetName().ToString();
		}

		// Token: 0x060003D1 RID: 977 RVA: 0x0000FFF0 File Offset: 0x0000E1F0
		protected void UpdatePositionWith(Camera missionCamera, Vec3 worldPosition)
		{
			float a = -100f;
			float b = -100f;
			float num = 0f;
			MBWindowManager.WorldToScreenInsideUsableArea(missionCamera, worldPosition, ref a, ref b, ref num);
			if (num > 0f)
			{
				this.ScreenPosition = new Vec2(a, b);
				this.Distance = (int)(worldPosition - missionCamera.Position).Length;
				return;
			}
			this.Distance = -1;
			this.ScreenPosition = new Vec2(-500f, -500f);
		}

		// Token: 0x060003D2 RID: 978 RVA: 0x0001006A File Offset: 0x0000E26A
		public void SetEnabledState(bool enabled)
		{
			this.IsEnabled = this.IsPersistent || enabled;
		}

		// Token: 0x1700012B RID: 299
		// (get) Token: 0x060003D3 RID: 979 RVA: 0x0001007A File Offset: 0x0000E27A
		// (set) Token: 0x060003D4 RID: 980 RVA: 0x00010082 File Offset: 0x0000E282
		[DataSourceProperty]
		public MBBindingList<QuestMarkerVM> Quests
		{
			get
			{
				return this._quests;
			}
			set
			{
				if (value != this._quests)
				{
					this._quests = value;
					base.OnPropertyChangedWithValue<MBBindingList<QuestMarkerVM>>(value, "Quests");
				}
			}
		}

		// Token: 0x1700012C RID: 300
		// (get) Token: 0x060003D5 RID: 981 RVA: 0x000100A0 File Offset: 0x0000E2A0
		// (set) Token: 0x060003D6 RID: 982 RVA: 0x000100A8 File Offset: 0x0000E2A8
		[DataSourceProperty]
		public Vec2 ScreenPosition
		{
			get
			{
				return this._screenPosition;
			}
			set
			{
				if (value.x != this._screenPosition.x || value.y != this._screenPosition.y)
				{
					this._screenPosition = value;
					base.OnPropertyChangedWithValue(value, "ScreenPosition");
				}
			}
		}

		// Token: 0x1700012D RID: 301
		// (get) Token: 0x060003D7 RID: 983 RVA: 0x000100E3 File Offset: 0x0000E2E3
		// (set) Token: 0x060003D8 RID: 984 RVA: 0x000100EB File Offset: 0x0000E2EB
		[DataSourceProperty]
		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				if (value != this._name)
				{
					this._name = value;
					base.OnPropertyChangedWithValue<string>(value, "Name");
				}
			}
		}

		// Token: 0x1700012E RID: 302
		// (get) Token: 0x060003D9 RID: 985 RVA: 0x0001010E File Offset: 0x0000E30E
		// (set) Token: 0x060003DA RID: 986 RVA: 0x00010116 File Offset: 0x0000E316
		[DataSourceProperty]
		public string IconType
		{
			get
			{
				return this._iconType;
			}
			set
			{
				if (value != this._iconType)
				{
					this._iconType = value;
					base.OnPropertyChangedWithValue<string>(value, "IconType");
				}
			}
		}

		// Token: 0x1700012F RID: 303
		// (get) Token: 0x060003DB RID: 987 RVA: 0x00010139 File Offset: 0x0000E339
		// (set) Token: 0x060003DC RID: 988 RVA: 0x00010141 File Offset: 0x0000E341
		[DataSourceProperty]
		public string NameType
		{
			get
			{
				return this._nameType;
			}
			set
			{
				if (value != this._nameType)
				{
					this._nameType = value;
					base.OnPropertyChangedWithValue<string>(value, "NameType");
				}
			}
		}

		// Token: 0x17000130 RID: 304
		// (get) Token: 0x060003DD RID: 989 RVA: 0x00010164 File Offset: 0x0000E364
		// (set) Token: 0x060003DE RID: 990 RVA: 0x0001016C File Offset: 0x0000E36C
		[DataSourceProperty]
		public int Distance
		{
			get
			{
				return this._distance;
			}
			set
			{
				if (value != this._distance)
				{
					this._distance = value;
					base.OnPropertyChangedWithValue(value, "Distance");
				}
			}
		}

		// Token: 0x17000131 RID: 305
		// (get) Token: 0x060003DF RID: 991 RVA: 0x0001018A File Offset: 0x0000E38A
		// (set) Token: 0x060003E0 RID: 992 RVA: 0x00010192 File Offset: 0x0000E392
		[DataSourceProperty]
		public bool IsEnabled
		{
			get
			{
				return this._isEnabled;
			}
			set
			{
				if (value != this._isEnabled)
				{
					this._isEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsEnabled");
				}
			}
		}

		// Token: 0x17000132 RID: 306
		// (get) Token: 0x060003E1 RID: 993 RVA: 0x000101B0 File Offset: 0x0000E3B0
		// (set) Token: 0x060003E2 RID: 994 RVA: 0x000101B8 File Offset: 0x0000E3B8
		[DataSourceProperty]
		public bool IsTracked
		{
			get
			{
				return this._isTracked;
			}
			set
			{
				if (value != this._isTracked)
				{
					this._isTracked = value;
					base.OnPropertyChangedWithValue(value, "IsTracked");
				}
			}
		}

		// Token: 0x17000133 RID: 307
		// (get) Token: 0x060003E3 RID: 995 RVA: 0x000101D6 File Offset: 0x0000E3D6
		// (set) Token: 0x060003E4 RID: 996 RVA: 0x000101DE File Offset: 0x0000E3DE
		[DataSourceProperty]
		public bool IsQuestMainStory
		{
			get
			{
				return this._isQuestMainStory;
			}
			set
			{
				if (value != this._isQuestMainStory)
				{
					this._isQuestMainStory = value;
					base.OnPropertyChangedWithValue(value, "IsQuestMainStory");
				}
			}
		}

		// Token: 0x17000134 RID: 308
		// (get) Token: 0x060003E5 RID: 997 RVA: 0x000101FC File Offset: 0x0000E3FC
		// (set) Token: 0x060003E6 RID: 998 RVA: 0x00010204 File Offset: 0x0000E404
		[DataSourceProperty]
		public bool IsEnemy
		{
			get
			{
				return this._isEnemy;
			}
			set
			{
				if (value != this._isEnemy)
				{
					this._isEnemy = value;
					base.OnPropertyChangedWithValue(value, "IsEnemy");
				}
			}
		}

		// Token: 0x17000135 RID: 309
		// (get) Token: 0x060003E7 RID: 999 RVA: 0x00010222 File Offset: 0x0000E422
		// (set) Token: 0x060003E8 RID: 1000 RVA: 0x0001022A File Offset: 0x0000E42A
		[DataSourceProperty]
		public bool IsFriendly
		{
			get
			{
				return this._isFriendly;
			}
			set
			{
				if (value != this._isFriendly)
				{
					this._isFriendly = value;
					base.OnPropertyChangedWithValue(value, "IsFriendly");
				}
			}
		}

		// Token: 0x17000136 RID: 310
		// (get) Token: 0x060003E9 RID: 1001 RVA: 0x00010248 File Offset: 0x0000E448
		// (set) Token: 0x060003EA RID: 1002 RVA: 0x00010250 File Offset: 0x0000E450
		[DataSourceProperty]
		public bool IsPersistent
		{
			get
			{
				return this._isPersistent;
			}
			set
			{
				if (value != this._isPersistent)
				{
					this._isPersistent = value;
					base.OnPropertyChangedWithValue(value, "IsPersistent");
					if (this.IsPersistent)
					{
						this.SetEnabledState(true);
						return;
					}
					if (!this.IsEnabled)
					{
						this.SetEnabledState(false);
					}
				}
			}
		}

		// Token: 0x040001FC RID: 508
		private MBBindingList<QuestMarkerVM> _quests;

		// Token: 0x040001FD RID: 509
		private Vec2 _screenPosition;

		// Token: 0x040001FE RID: 510
		private int _distance;

		// Token: 0x040001FF RID: 511
		private string _name;

		// Token: 0x04000200 RID: 512
		private string _iconType = string.Empty;

		// Token: 0x04000201 RID: 513
		private string _nameType = string.Empty;

		// Token: 0x04000202 RID: 514
		private bool _isEnabled;

		// Token: 0x04000203 RID: 515
		private bool _isTracked;

		// Token: 0x04000204 RID: 516
		private bool _isQuestMainStory;

		// Token: 0x04000205 RID: 517
		private bool _isEnemy;

		// Token: 0x04000206 RID: 518
		private bool _isFriendly;

		// Token: 0x04000207 RID: 519
		private bool _isPersistent;
	}
}
