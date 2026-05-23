using System;
using Helpers;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameState
{
	// Token: 0x02000399 RID: 921
	public class PartyState : PlayerGameState
	{
		// Token: 0x17000C84 RID: 3204
		// (get) Token: 0x060034E9 RID: 13545 RVA: 0x000D67DB File Offset: 0x000D49DB
		public override bool IsMenuState
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000C85 RID: 3205
		// (get) Token: 0x060034EA RID: 13546 RVA: 0x000D67DE File Offset: 0x000D49DE
		// (set) Token: 0x060034EB RID: 13547 RVA: 0x000D67E6 File Offset: 0x000D49E6
		public PartyScreenLogic PartyScreenLogic { get; set; }

		// Token: 0x17000C86 RID: 3206
		// (get) Token: 0x060034EC RID: 13548 RVA: 0x000D67EF File Offset: 0x000D49EF
		// (set) Token: 0x060034ED RID: 13549 RVA: 0x000D67F7 File Offset: 0x000D49F7
		public PartyScreenHelper.PartyScreenMode PartyScreenMode { get; set; }

		// Token: 0x17000C87 RID: 3207
		// (get) Token: 0x060034EE RID: 13550 RVA: 0x000D6800 File Offset: 0x000D4A00
		// (set) Token: 0x060034EF RID: 13551 RVA: 0x000D6808 File Offset: 0x000D4A08
		public bool IsDonating { get; set; }

		// Token: 0x17000C88 RID: 3208
		// (get) Token: 0x060034F0 RID: 13552 RVA: 0x000D6811 File Offset: 0x000D4A11
		// (set) Token: 0x060034F1 RID: 13553 RVA: 0x000D6819 File Offset: 0x000D4A19
		public IPartyScreenLogicHandler Handler { get; set; }

		// Token: 0x060034F2 RID: 13554 RVA: 0x000D6822 File Offset: 0x000D4A22
		public void RequestUserInput(string text, Action accept, Action cancel)
		{
			if (this.Handler != null)
			{
				this.Handler.RequestUserInput(text, accept, cancel);
			}
		}
	}
}
