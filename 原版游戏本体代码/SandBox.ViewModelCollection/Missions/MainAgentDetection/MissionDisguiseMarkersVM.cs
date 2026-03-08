using System;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Missions.MainAgentDetection
{
	// Token: 0x02000043 RID: 67
	public class MissionDisguiseMarkersVM : ViewModel
	{
		// Token: 0x06000453 RID: 1107 RVA: 0x00011634 File Offset: 0x0000F834
		public MissionDisguiseMarkersVM()
		{
			this.HostileAgents = new MBBindingList<MissionDisguiseMarkerItemVM>();
		}

		// Token: 0x17000150 RID: 336
		// (get) Token: 0x06000454 RID: 1108 RVA: 0x00011647 File Offset: 0x0000F847
		// (set) Token: 0x06000455 RID: 1109 RVA: 0x0001164F File Offset: 0x0000F84F
		[DataSourceProperty]
		public MissionDisguiseMarkerItemVM TargetAgent
		{
			get
			{
				return this._targetAgent;
			}
			set
			{
				if (value != this._targetAgent)
				{
					this._targetAgent = value;
					base.OnPropertyChangedWithValue<MissionDisguiseMarkerItemVM>(value, "TargetAgent");
				}
			}
		}

		// Token: 0x17000151 RID: 337
		// (get) Token: 0x06000456 RID: 1110 RVA: 0x0001166D File Offset: 0x0000F86D
		// (set) Token: 0x06000457 RID: 1111 RVA: 0x00011675 File Offset: 0x0000F875
		[DataSourceProperty]
		public MBBindingList<MissionDisguiseMarkerItemVM> HostileAgents
		{
			get
			{
				return this._hostileAgents;
			}
			set
			{
				if (value != this._hostileAgents)
				{
					this._hostileAgents = value;
					base.OnPropertyChangedWithValue<MBBindingList<MissionDisguiseMarkerItemVM>>(value, "HostileAgents");
				}
			}
		}

		// Token: 0x04000233 RID: 563
		private MissionDisguiseMarkerItemVM _targetAgent;

		// Token: 0x04000234 RID: 564
		private MBBindingList<MissionDisguiseMarkerItemVM> _hostileAgents;
	}
}
