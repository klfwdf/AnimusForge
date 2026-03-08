using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.Missions.NameMarker
{
	// Token: 0x02000031 RID: 49
	public abstract class MissionNameMarkerProvider
	{
		// Token: 0x060003BE RID: 958 RVA: 0x0000FE54 File Offset: 0x0000E054
		public MissionNameMarkerProvider()
		{
		}

		// Token: 0x060003BF RID: 959
		public abstract void CreateMarkers(List<MissionNameMarkerTargetBaseVM> markers);

		// Token: 0x060003C0 RID: 960 RVA: 0x0000FE5C File Offset: 0x0000E05C
		public void Initialize(Mission mission, Action onSetMarkersDirty)
		{
			this.OnInitialize(mission);
			this._initialized = true;
			this._onSetMarkersDirty = onSetMarkersDirty;
		}

		// Token: 0x060003C1 RID: 961 RVA: 0x0000FE73 File Offset: 0x0000E073
		public void Destroy(Mission mission)
		{
			this.OnDestroy(mission);
			this._initialized = false;
		}

		// Token: 0x060003C2 RID: 962 RVA: 0x0000FE83 File Offset: 0x0000E083
		public void Tick(float dt)
		{
			this.OnTick(dt);
		}

		// Token: 0x060003C3 RID: 963 RVA: 0x0000FE8C File Offset: 0x0000E08C
		protected virtual void OnInitialize(Mission mission)
		{
		}

		// Token: 0x060003C4 RID: 964 RVA: 0x0000FE8E File Offset: 0x0000E08E
		protected virtual void OnDestroy(Mission mission)
		{
		}

		// Token: 0x060003C5 RID: 965 RVA: 0x0000FE90 File Offset: 0x0000E090
		protected virtual void OnTick(float dt)
		{
		}

		// Token: 0x060003C6 RID: 966 RVA: 0x0000FE92 File Offset: 0x0000E092
		protected void SetMarkersDirty()
		{
			this._onSetMarkersDirty();
		}

		// Token: 0x040001F9 RID: 505
		private Action _onSetMarkersDirty;

		// Token: 0x040001FA RID: 506
		private bool _initialized;
	}
}
