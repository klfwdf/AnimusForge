using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameState
{
	// Token: 0x02000387 RID: 903
	public class BarberState : GameState
	{
		// Token: 0x17000C5B RID: 3163
		// (get) Token: 0x06003440 RID: 13376 RVA: 0x000D5EB2 File Offset: 0x000D40B2
		public override bool IsMenuState
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000C5C RID: 3164
		// (get) Token: 0x06003441 RID: 13377 RVA: 0x000D5EB5 File Offset: 0x000D40B5
		// (set) Token: 0x06003442 RID: 13378 RVA: 0x000D5EBD File Offset: 0x000D40BD
		public IFaceGeneratorCustomFilter Filter { get; private set; }

		// Token: 0x06003443 RID: 13379 RVA: 0x000D5EC6 File Offset: 0x000D40C6
		public BarberState()
		{
		}

		// Token: 0x06003444 RID: 13380 RVA: 0x000D5ECE File Offset: 0x000D40CE
		public BarberState(BasicCharacterObject character, IFaceGeneratorCustomFilter filter)
		{
			this.Character = character;
			this.Filter = filter;
		}

		// Token: 0x04000EE4 RID: 3812
		public BasicCharacterObject Character;
	}
}
