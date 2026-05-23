using System;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameState
{
	// Token: 0x0200039E RID: 926
	public class PortState : GameState
	{
		// Token: 0x17000C89 RID: 3209
		// (get) Token: 0x060034FB RID: 13563 RVA: 0x000D6842 File Offset: 0x000D4A42
		public override bool IsMenuState
		{
			get
			{
				return true;
			}
		}

		// Token: 0x060034FC RID: 13564 RVA: 0x000D6845 File Offset: 0x000D4A45
		public PortState()
		{
			Debug.FailedAssert("do not use parameterless constructor.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameState\\PortState.cs", ".ctor", 39);
		}

		// Token: 0x060034FD RID: 13565 RVA: 0x000D6864 File Offset: 0x000D4A64
		public PortState(PartyBase leftOwner, PartyBase rightOwner, PortScreenModes portScreenMode)
		{
			this.PortScreenMode = portScreenMode;
			this.LeftOwner = leftOwner;
			this.RightOwner = rightOwner;
			this.LeftShips = ((leftOwner != null) ? leftOwner.Ships : null);
			this.RightShips = ((rightOwner != null) ? rightOwner.Ships : null);
		}

		// Token: 0x060034FE RID: 13566 RVA: 0x000D68B0 File Offset: 0x000D4AB0
		public PortState(PartyBase leftOwner, PartyBase rightOwner, Action onEndAction, PortScreenModes portScreenMode)
		{
			this.PortScreenMode = portScreenMode;
			this.LeftOwner = leftOwner;
			this.RightOwner = rightOwner;
			this.LeftShips = ((leftOwner != null) ? leftOwner.Ships : null);
			this.RightShips = ((rightOwner != null) ? rightOwner.Ships : null);
			this.OnEndAction = onEndAction;
		}

		// Token: 0x060034FF RID: 13567 RVA: 0x000D6904 File Offset: 0x000D4B04
		public PortState(MBReadOnlyList<Ship> leftShips, MBReadOnlyList<Ship> rightShips, PortScreenModes portScreenMode)
		{
			this.PortScreenMode = portScreenMode;
			this.LeftOwner = null;
			this.RightOwner = null;
			this.LeftShips = leftShips;
			this.RightShips = rightShips;
		}

		// Token: 0x06003500 RID: 13568 RVA: 0x000D692F File Offset: 0x000D4B2F
		public PortState(PartyBase leftOwner, PartyBase rightOwner, MBReadOnlyList<Ship> leftShips, MBReadOnlyList<Ship> rightShips, PortScreenModes portScreenMode)
		{
			this.PortScreenMode = portScreenMode;
			this.LeftOwner = leftOwner;
			this.RightOwner = rightOwner;
			this.LeftShips = leftShips;
			this.RightShips = rightShips;
		}

		// Token: 0x06003501 RID: 13569 RVA: 0x000D695C File Offset: 0x000D4B5C
		public PortState(Settlement settlement, PartyBase rightOwner, PortScreenModes portScreenMode)
		{
			this.PortScreenMode = portScreenMode;
			this.LeftOwner = settlement.Party;
			this.RightOwner = rightOwner;
			this.LeftShips = settlement.Party.Ships;
			this.RightShips = rightOwner.Ships;
		}

		// Token: 0x06003502 RID: 13570 RVA: 0x000D699B File Offset: 0x000D4B9B
		protected override void OnFinalize()
		{
			base.OnFinalize();
			Action onEndAction = this.OnEndAction;
			if (onEndAction == null)
			{
				return;
			}
			onEndAction();
		}

		// Token: 0x04000F10 RID: 3856
		public readonly PortScreenModes PortScreenMode;

		// Token: 0x04000F11 RID: 3857
		public readonly PartyBase LeftOwner;

		// Token: 0x04000F12 RID: 3858
		public readonly PartyBase RightOwner;

		// Token: 0x04000F13 RID: 3859
		public readonly MBReadOnlyList<Ship> LeftShips;

		// Token: 0x04000F14 RID: 3860
		public readonly MBReadOnlyList<Ship> RightShips;

		// Token: 0x04000F15 RID: 3861
		public readonly Action OnEndAction;
	}
}
