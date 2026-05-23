using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace SandBox.View.Map.Navigation
{
	// Token: 0x02000066 RID: 102
	public abstract class MapNavigationElementBase : INavigationElement
	{
		// Token: 0x1700007E RID: 126
		// (get) Token: 0x0600045B RID: 1115 RVA: 0x00024471 File Offset: 0x00022671
		public NavigationPermissionItem Permission
		{
			get
			{
				return this.GetPermission();
			}
		}

		// Token: 0x1700007F RID: 127
		// (get) Token: 0x0600045C RID: 1116 RVA: 0x00024479 File Offset: 0x00022679
		public TextObject Tooltip
		{
			get
			{
				return this.GetTooltip();
			}
		}

		// Token: 0x17000080 RID: 128
		// (get) Token: 0x0600045D RID: 1117 RVA: 0x00024481 File Offset: 0x00022681
		public TextObject AlertTooltip
		{
			get
			{
				return this.GetAlertTooltip();
			}
		}

		// Token: 0x17000081 RID: 129
		// (get) Token: 0x0600045E RID: 1118
		public abstract bool IsActive { get; }

		// Token: 0x17000082 RID: 130
		// (get) Token: 0x0600045F RID: 1119
		public abstract bool IsLockingNavigation { get; }

		// Token: 0x17000083 RID: 131
		// (get) Token: 0x06000460 RID: 1120
		public abstract bool HasAlert { get; }

		// Token: 0x17000084 RID: 132
		// (get) Token: 0x06000461 RID: 1121
		public abstract string StringId { get; }

		// Token: 0x06000462 RID: 1122
		public abstract void OpenView();

		// Token: 0x06000463 RID: 1123
		public abstract void OpenView(params object[] parameters);

		// Token: 0x06000464 RID: 1124
		public abstract void GoToLink();

		// Token: 0x17000085 RID: 133
		// (get) Token: 0x06000465 RID: 1125 RVA: 0x00024489 File Offset: 0x00022689
		protected Game _game
		{
			get
			{
				return Game.Current;
			}
		}

		// Token: 0x06000466 RID: 1126 RVA: 0x00024490 File Offset: 0x00022690
		public MapNavigationElementBase(MapNavigationHandler handler)
		{
			this._handler = handler;
			this._viewDataTracker = Campaign.Current.GetCampaignBehavior<IViewDataTracker>();
		}

		// Token: 0x06000467 RID: 1127
		protected abstract NavigationPermissionItem GetPermission();

		// Token: 0x06000468 RID: 1128
		protected abstract TextObject GetTooltip();

		// Token: 0x06000469 RID: 1129
		protected abstract TextObject GetAlertTooltip();

		// Token: 0x04000226 RID: 550
		protected readonly MapNavigationHandler _handler;

		// Token: 0x04000227 RID: 551
		protected readonly IViewDataTracker _viewDataTracker;
	}
}
