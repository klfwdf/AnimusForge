using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameState
{
	// Token: 0x02000388 RID: 904
	public class CharacterDeveloperState : GameState
	{
		// Token: 0x17000C5D RID: 3165
		// (get) Token: 0x06003445 RID: 13381 RVA: 0x000D5EE4 File Offset: 0x000D40E4
		public override bool IsMenuState
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000C5E RID: 3166
		// (get) Token: 0x06003446 RID: 13382 RVA: 0x000D5EE7 File Offset: 0x000D40E7
		// (set) Token: 0x06003447 RID: 13383 RVA: 0x000D5EEF File Offset: 0x000D40EF
		public Hero InitialSelectedHero { get; private set; }

		// Token: 0x06003448 RID: 13384 RVA: 0x000D5EF8 File Offset: 0x000D40F8
		public CharacterDeveloperState()
		{
		}

		// Token: 0x06003449 RID: 13385 RVA: 0x000D5F00 File Offset: 0x000D4100
		public CharacterDeveloperState(Hero initialSelectedHero)
		{
			this.InitialSelectedHero = initialSelectedHero;
		}

		// Token: 0x17000C5F RID: 3167
		// (get) Token: 0x0600344A RID: 13386 RVA: 0x000D5F0F File Offset: 0x000D410F
		// (set) Token: 0x0600344B RID: 13387 RVA: 0x000D5F17 File Offset: 0x000D4117
		public ICharacterDeveloperStateHandler Handler
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

		// Token: 0x04000EE7 RID: 3815
		private ICharacterDeveloperStateHandler _handler;
	}
}
