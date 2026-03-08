using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameState
{
	// Token: 0x0200038E RID: 910
	public class EducationState : GameState
	{
		// Token: 0x17000C6A RID: 3178
		// (get) Token: 0x06003468 RID: 13416 RVA: 0x000D6034 File Offset: 0x000D4234
		public override bool IsMenuState
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000C6B RID: 3179
		// (get) Token: 0x06003469 RID: 13417 RVA: 0x000D6037 File Offset: 0x000D4237
		// (set) Token: 0x0600346A RID: 13418 RVA: 0x000D603F File Offset: 0x000D423F
		public Hero Child { get; private set; }

		// Token: 0x17000C6C RID: 3180
		// (get) Token: 0x0600346B RID: 13419 RVA: 0x000D6048 File Offset: 0x000D4248
		// (set) Token: 0x0600346C RID: 13420 RVA: 0x000D6050 File Offset: 0x000D4250
		public IEducationStateHandler Handler
		{
			get
			{
				return this._handler;
			}
			set
			{
				this._handler = value;
			}
		}

		// Token: 0x0600346D RID: 13421 RVA: 0x000D6059 File Offset: 0x000D4259
		public EducationState()
		{
			Debug.FailedAssert("Do not use EducationState with default constructor!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameState\\EducationState.cs", ".ctor", 22);
		}

		// Token: 0x0600346E RID: 13422 RVA: 0x000D6077 File Offset: 0x000D4277
		public EducationState(Hero child)
		{
			this.Child = child;
		}

		// Token: 0x04000EF1 RID: 3825
		private IEducationStateHandler _handler;
	}
}
