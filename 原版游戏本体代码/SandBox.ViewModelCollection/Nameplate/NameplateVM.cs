using System;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Nameplate
{
	// Token: 0x02000018 RID: 24
	public class NameplateVM : ViewModel
	{
		// Token: 0x170000C4 RID: 196
		// (get) Token: 0x0600023C RID: 572 RVA: 0x00009CA8 File Offset: 0x00007EA8
		// (set) Token: 0x0600023D RID: 573 RVA: 0x00009CB0 File Offset: 0x00007EB0
		public double Scale { get; set; }

		// Token: 0x170000C5 RID: 197
		// (get) Token: 0x0600023E RID: 574 RVA: 0x00009CB9 File Offset: 0x00007EB9
		// (set) Token: 0x0600023F RID: 575 RVA: 0x00009CC1 File Offset: 0x00007EC1
		public int NameplateOrder { get; set; }

		// Token: 0x06000241 RID: 577 RVA: 0x00009CD2 File Offset: 0x00007ED2
		protected void OnTutorialNotificationElementChanged(TutorialNotificationElementChangeEvent obj)
		{
			this.RefreshTutorialStatus(((obj != null) ? obj.NewNotificationElementID : null) ?? string.Empty);
		}

		// Token: 0x06000242 RID: 578 RVA: 0x00009CEF File Offset: 0x00007EEF
		public virtual void RefreshDynamicProperties(bool forceUpdate)
		{
		}

		// Token: 0x06000243 RID: 579 RVA: 0x00009CF1 File Offset: 0x00007EF1
		public virtual void RefreshPosition()
		{
		}

		// Token: 0x06000244 RID: 580 RVA: 0x00009CF3 File Offset: 0x00007EF3
		public virtual void RefreshRelationStatus()
		{
		}

		// Token: 0x06000245 RID: 581 RVA: 0x00009CF5 File Offset: 0x00007EF5
		public virtual void RefreshTutorialStatus(string newTutorialHighlightElementID)
		{
		}

		// Token: 0x170000C6 RID: 198
		// (get) Token: 0x06000246 RID: 582 RVA: 0x00009CF7 File Offset: 0x00007EF7
		// (set) Token: 0x06000247 RID: 583 RVA: 0x00009CFF File Offset: 0x00007EFF
		public string FactionColor
		{
			get
			{
				return this._factionColor;
			}
			set
			{
				if (value != this._factionColor)
				{
					this._factionColor = value;
					base.OnPropertyChangedWithValue<string>(value, "FactionColor");
				}
			}
		}

		// Token: 0x170000C7 RID: 199
		// (get) Token: 0x06000248 RID: 584 RVA: 0x00009D22 File Offset: 0x00007F22
		// (set) Token: 0x06000249 RID: 585 RVA: 0x00009D2A File Offset: 0x00007F2A
		public float DistanceToCamera
		{
			get
			{
				return this._distanceToCamera;
			}
			set
			{
				if (value != this._distanceToCamera)
				{
					this._distanceToCamera = value;
					base.OnPropertyChangedWithValue(value, "DistanceToCamera");
				}
			}
		}

		// Token: 0x170000C8 RID: 200
		// (get) Token: 0x0600024A RID: 586 RVA: 0x00009D48 File Offset: 0x00007F48
		// (set) Token: 0x0600024B RID: 587 RVA: 0x00009D50 File Offset: 0x00007F50
		public bool IsVisibleOnMap
		{
			get
			{
				return this._isVisibleOnMap;
			}
			set
			{
				if (value != this._isVisibleOnMap)
				{
					this._isVisibleOnMap = value;
					base.OnPropertyChangedWithValue(value, "IsVisibleOnMap");
				}
			}
		}

		// Token: 0x170000C9 RID: 201
		// (get) Token: 0x0600024C RID: 588 RVA: 0x00009D6E File Offset: 0x00007F6E
		// (set) Token: 0x0600024D RID: 589 RVA: 0x00009D76 File Offset: 0x00007F76
		public bool IsTargetedByTutorial
		{
			get
			{
				return this._isTargetedByTutorial;
			}
			set
			{
				if (value != this._isTargetedByTutorial)
				{
					this._isTargetedByTutorial = value;
					base.OnPropertyChangedWithValue(value, "IsTargetedByTutorial");
					base.OnPropertyChanged("ShouldShowFullName");
					base.OnPropertyChanged("IsTracked");
				}
			}
		}

		// Token: 0x170000CA RID: 202
		// (get) Token: 0x0600024E RID: 590 RVA: 0x00009DAA File Offset: 0x00007FAA
		// (set) Token: 0x0600024F RID: 591 RVA: 0x00009DB2 File Offset: 0x00007FB2
		public Vec2 Position
		{
			get
			{
				return this._position;
			}
			set
			{
				if (this._position != value)
				{
					this._position = value;
					base.OnPropertyChangedWithValue(value, "Position");
				}
			}
		}

		// Token: 0x170000CB RID: 203
		// (get) Token: 0x06000250 RID: 592 RVA: 0x00009DD5 File Offset: 0x00007FD5
		// (set) Token: 0x06000251 RID: 593 RVA: 0x00009DDD File Offset: 0x00007FDD
		public bool CanParley
		{
			get
			{
				return this._canParley;
			}
			set
			{
				if (value != this._canParley)
				{
					this._canParley = value;
					base.OnPropertyChangedWithValue(value, "CanParley");
				}
			}
		}

		// Token: 0x04000105 RID: 261
		protected bool _bindIsTargetedByTutorial;

		// Token: 0x04000106 RID: 262
		private Vec2 _position;

		// Token: 0x04000107 RID: 263
		private bool _isVisibleOnMap;

		// Token: 0x04000108 RID: 264
		private string _factionColor;

		// Token: 0x04000109 RID: 265
		private bool _isTargetedByTutorial;

		// Token: 0x0400010A RID: 266
		private float _distanceToCamera;

		// Token: 0x0400010B RID: 267
		private bool _canParley;

		// Token: 0x0200007E RID: 126
		protected enum NameplateSize
		{
			// Token: 0x04000364 RID: 868
			Small,
			// Token: 0x04000365 RID: 869
			Normal,
			// Token: 0x04000366 RID: 870
			Big
		}
	}
}
