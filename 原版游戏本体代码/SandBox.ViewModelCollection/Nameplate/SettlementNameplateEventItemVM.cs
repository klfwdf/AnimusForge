using System;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Nameplate
{
	// Token: 0x0200001D RID: 29
	public class SettlementNameplateEventItemVM : ViewModel
	{
		// Token: 0x060002C6 RID: 710 RVA: 0x0000C15D File Offset: 0x0000A35D
		public SettlementNameplateEventItemVM(SettlementNameplateEventItemVM.SettlementEventType eventType)
		{
			this.EventType = eventType;
			this.Type = (int)eventType;
			this.AdditionalParameters = "";
		}

		// Token: 0x060002C7 RID: 711 RVA: 0x0000C17E File Offset: 0x0000A37E
		public SettlementNameplateEventItemVM(string productionIconId = "")
		{
			this.EventType = SettlementNameplateEventItemVM.SettlementEventType.Production;
			this.Type = (int)this.EventType;
			this.AdditionalParameters = productionIconId;
		}

		// Token: 0x170000E6 RID: 230
		// (get) Token: 0x060002C8 RID: 712 RVA: 0x0000C1A0 File Offset: 0x0000A3A0
		// (set) Token: 0x060002C9 RID: 713 RVA: 0x0000C1A8 File Offset: 0x0000A3A8
		[DataSourceProperty]
		public int Type
		{
			get
			{
				return this._type;
			}
			set
			{
				if (value != this._type)
				{
					this._type = value;
					base.OnPropertyChangedWithValue(value, "Type");
				}
			}
		}

		// Token: 0x170000E7 RID: 231
		// (get) Token: 0x060002CA RID: 714 RVA: 0x0000C1C6 File Offset: 0x0000A3C6
		// (set) Token: 0x060002CB RID: 715 RVA: 0x0000C1CE File Offset: 0x0000A3CE
		[DataSourceProperty]
		public string AdditionalParameters
		{
			get
			{
				return this._additionalParameters;
			}
			set
			{
				if (value != this._additionalParameters)
				{
					this._additionalParameters = value;
					base.OnPropertyChangedWithValue<string>(value, "AdditionalParameters");
				}
			}
		}

		// Token: 0x0400015A RID: 346
		public readonly SettlementNameplateEventItemVM.SettlementEventType EventType;

		// Token: 0x0400015B RID: 347
		private int _type;

		// Token: 0x0400015C RID: 348
		private string _additionalParameters;

		// Token: 0x02000086 RID: 134
		public enum SettlementEventType
		{
			// Token: 0x04000373 RID: 883
			Tournament,
			// Token: 0x04000374 RID: 884
			AvailableIssue,
			// Token: 0x04000375 RID: 885
			ActiveQuest,
			// Token: 0x04000376 RID: 886
			ActiveStoryQuest,
			// Token: 0x04000377 RID: 887
			TrackedIssue,
			// Token: 0x04000378 RID: 888
			TrackedStoryQuest,
			// Token: 0x04000379 RID: 889
			Production
		}
	}
}
