using System;
using System.Collections.Generic;

namespace TaleWorlds.Core
{
	// Token: 0x020000BB RID: 187
	public interface IMissionTroopSupplier
	{
		// Token: 0x060009E8 RID: 2536
		IEnumerable<IAgentOriginBase> SupplyTroops(int numberToAllocate);

		// Token: 0x060009E9 RID: 2537
		IAgentOriginBase SupplyOneTroop();

		// Token: 0x060009EA RID: 2538
		IEnumerable<IAgentOriginBase> GetAllTroops();

		// Token: 0x060009EB RID: 2539
		BasicCharacterObject GetGeneralCharacter();

		// Token: 0x17000354 RID: 852
		// (get) Token: 0x060009EC RID: 2540
		int NumRemovedTroops { get; }

		// Token: 0x17000355 RID: 853
		// (get) Token: 0x060009ED RID: 2541
		int NumTroopsNotSupplied { get; }

		// Token: 0x17000356 RID: 854
		// (get) Token: 0x060009EE RID: 2542
		bool AnyTroopRemainsToBeSupplied { get; }

		// Token: 0x060009EF RID: 2543
		int GetNumberOfPlayerControllableTroops();
	}
}
