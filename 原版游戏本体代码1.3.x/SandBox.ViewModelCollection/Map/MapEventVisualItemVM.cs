using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.Map
{
	// Token: 0x02000046 RID: 70
	public class MapEventVisualItemVM : ViewModel
	{
		// Token: 0x17000156 RID: 342
		// (get) Token: 0x0600046A RID: 1130 RVA: 0x00011947 File Offset: 0x0000FB47
		// (set) Token: 0x0600046B RID: 1131 RVA: 0x0001194F File Offset: 0x0000FB4F
		public MapEvent MapEvent { get; private set; }

		// Token: 0x0600046C RID: 1132 RVA: 0x00011958 File Offset: 0x0000FB58
		public MapEventVisualItemVM(Camera mapCamera, MapEvent mapEvent)
		{
			this._mapCamera = mapCamera;
			this.MapEvent = mapEvent;
			this._mapEventPositionCache = mapEvent.Position;
		}

		// Token: 0x0600046D RID: 1133 RVA: 0x0001197A File Offset: 0x0000FB7A
		public void UpdateProperties()
		{
			this.EventType = (int)SandBoxUIHelper.GetMapEventVisualTypeFromMapEvent(this.MapEvent);
			this._isAVisibleEvent = this.MapEvent.IsVisible;
		}

		// Token: 0x0600046E RID: 1134 RVA: 0x000119A0 File Offset: 0x0000FBA0
		public void ParallelUpdatePosition()
		{
			this._latestX = 0f;
			this._latestY = 0f;
			this._latestW = 0f;
			if (this._mapEventPositionCache != this.MapEvent.Position)
			{
				this._mapEventPositionCache = this.MapEvent.Position;
			}
			MBWindowManager.WorldToScreenInsideUsableArea(this._mapCamera, this._mapEventPositionCache.AsVec3() + new Vec3(0f, 0f, 1.5f, -1f), ref this._latestX, ref this._latestY, ref this._latestW);
			this._bindPosition = new Vec2(this._latestX, this._latestY);
		}

		// Token: 0x0600046F RID: 1135 RVA: 0x00011A55 File Offset: 0x0000FC55
		public void DetermineIsVisibleOnMap()
		{
			this._bindIsVisibleOnMap = this._latestW > 0f && this._mapCamera.Position.z < 200f && this._isAVisibleEvent;
		}

		// Token: 0x06000470 RID: 1136 RVA: 0x00011A8A File Offset: 0x0000FC8A
		public void UpdateBindingProperties()
		{
			this.Position = this._bindPosition;
			this.IsVisibleOnMap = this._bindIsVisibleOnMap;
		}

		// Token: 0x17000157 RID: 343
		// (get) Token: 0x06000471 RID: 1137 RVA: 0x00011AA4 File Offset: 0x0000FCA4
		// (set) Token: 0x06000472 RID: 1138 RVA: 0x00011AAC File Offset: 0x0000FCAC
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

		// Token: 0x17000158 RID: 344
		// (get) Token: 0x06000473 RID: 1139 RVA: 0x00011ACF File Offset: 0x0000FCCF
		// (set) Token: 0x06000474 RID: 1140 RVA: 0x00011AD7 File Offset: 0x0000FCD7
		public int EventType
		{
			get
			{
				return this._eventType;
			}
			set
			{
				if (this._eventType != value)
				{
					this._eventType = value;
					base.OnPropertyChangedWithValue(value, "EventType");
				}
			}
		}

		// Token: 0x17000159 RID: 345
		// (get) Token: 0x06000475 RID: 1141 RVA: 0x00011AF5 File Offset: 0x0000FCF5
		// (set) Token: 0x06000476 RID: 1142 RVA: 0x00011AFD File Offset: 0x0000FCFD
		public bool IsVisibleOnMap
		{
			get
			{
				return this._isVisibleOnMap;
			}
			set
			{
				if (this._isVisibleOnMap != value)
				{
					this._isVisibleOnMap = value;
					base.OnPropertyChangedWithValue(value, "IsVisibleOnMap");
				}
			}
		}

		// Token: 0x0400023D RID: 573
		private Camera _mapCamera;

		// Token: 0x0400023E RID: 574
		private bool _isAVisibleEvent;

		// Token: 0x0400023F RID: 575
		private CampaignVec2 _mapEventPositionCache;

		// Token: 0x04000240 RID: 576
		private const float CameraDistanceCutoff = 200f;

		// Token: 0x04000241 RID: 577
		private Vec2 _bindPosition;

		// Token: 0x04000242 RID: 578
		private bool _bindIsVisibleOnMap;

		// Token: 0x04000243 RID: 579
		private float _latestX;

		// Token: 0x04000244 RID: 580
		private float _latestY;

		// Token: 0x04000245 RID: 581
		private float _latestW;

		// Token: 0x04000246 RID: 582
		private Vec2 _position;

		// Token: 0x04000247 RID: 583
		private int _eventType;

		// Token: 0x04000248 RID: 584
		private bool _isVisibleOnMap;
	}
}
