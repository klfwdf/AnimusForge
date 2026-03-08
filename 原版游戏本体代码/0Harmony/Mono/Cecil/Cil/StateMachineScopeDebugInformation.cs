using System;
using Mono.Collections.Generic;

namespace Mono.Cecil.Cil
{
	// Token: 0x0200031A RID: 794
	internal sealed class StateMachineScopeDebugInformation : CustomDebugInformation
	{
		// Token: 0x17000546 RID: 1350
		// (get) Token: 0x06001495 RID: 5269 RVA: 0x00041284 File Offset: 0x0003F484
		public Collection<StateMachineScope> Scopes
		{
			get
			{
				Collection<StateMachineScope> result;
				if ((result = this.scopes) == null)
				{
					result = (this.scopes = new Collection<StateMachineScope>());
				}
				return result;
			}
		}

		// Token: 0x17000547 RID: 1351
		// (get) Token: 0x06001496 RID: 5270 RVA: 0x0001B6C7 File Offset: 0x000198C7
		public override CustomDebugInformationKind Kind
		{
			get
			{
				return CustomDebugInformationKind.StateMachineScope;
			}
		}

		// Token: 0x06001497 RID: 5271 RVA: 0x000412A9 File Offset: 0x0003F4A9
		public StateMachineScopeDebugInformation()
			: base(StateMachineScopeDebugInformation.KindIdentifier)
		{
		}

		// Token: 0x04000A4F RID: 2639
		internal Collection<StateMachineScope> scopes;

		// Token: 0x04000A50 RID: 2640
		public static Guid KindIdentifier = new Guid("{6DA9A61E-F8C7-4874-BE62-68BC5630DF71}");
	}
}
