using System;

namespace TaleWorlds.CampaignSystem.ViewModelCollection
{
	// Token: 0x02000010 RID: 16
	public class ActionCampaignOptionData : CampaignOptionData
	{
		// Token: 0x060000C1 RID: 193 RVA: 0x000042E8 File Offset: 0x000024E8
		public ActionCampaignOptionData(string identifier, int priorityIndex, CampaignOptionEnableState enableState, Action action, Func<CampaignOptionDisableStatus> getIsDisabledWithReason = null)
			: base(identifier, priorityIndex, enableState, null, null, getIsDisabledWithReason, false, null, null)
		{
			this._action = action;
		}

		// Token: 0x060000C2 RID: 194 RVA: 0x0000430D File Offset: 0x0000250D
		public override CampaignOptionDataType GetDataType()
		{
			return CampaignOptionDataType.Action;
		}

		// Token: 0x060000C3 RID: 195 RVA: 0x00004310 File Offset: 0x00002510
		public void ExecuteAction()
		{
			Action action = this._action;
			if (action == null)
			{
				return;
			}
			action();
		}

		// Token: 0x04000061 RID: 97
		private Action _action;
	}
}
