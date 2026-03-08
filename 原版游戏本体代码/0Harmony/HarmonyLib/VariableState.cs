using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace HarmonyLib
{
	// Token: 0x0200005D RID: 93
	internal class VariableState
	{
		// Token: 0x060001CD RID: 461 RVA: 0x0000D1EC File Offset: 0x0000B3EC
		public void Add(InjectionType type, LocalBuilder local)
		{
			this.injected[type] = local;
		}

		// Token: 0x060001CE RID: 462 RVA: 0x0000D1FB File Offset: 0x0000B3FB
		public void Add(string name, LocalBuilder local)
		{
			this.other[name] = local;
		}

		// Token: 0x060001CF RID: 463 RVA: 0x0000D20A File Offset: 0x0000B40A
		public bool TryGetValue(InjectionType type, out LocalBuilder local)
		{
			return this.injected.TryGetValue(type, out local);
		}

		// Token: 0x060001D0 RID: 464 RVA: 0x0000D219 File Offset: 0x0000B419
		public bool TryGetValue(string name, out LocalBuilder local)
		{
			return this.other.TryGetValue(name, out local);
		}

		// Token: 0x17000014 RID: 20
		public LocalBuilder this[InjectionType type]
		{
			get
			{
				LocalBuilder local;
				if (this.injected.TryGetValue(type, out local))
				{
					return local;
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(42, 1);
				defaultInterpolatedStringHandler.AppendLiteral("VariableState: variable of type ");
				defaultInterpolatedStringHandler.AppendFormatted<InjectionType>(type);
				defaultInterpolatedStringHandler.AppendLiteral(" not found");
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			set
			{
				this.injected[type] = value;
			}
		}

		// Token: 0x17000015 RID: 21
		public LocalBuilder this[string name]
		{
			get
			{
				LocalBuilder local;
				if (this.other.TryGetValue(name, out local))
				{
					return local;
				}
				throw new ArgumentException("VariableState: variable named '" + name + "' not found");
			}
			set
			{
				this.other[name] = value;
			}
		}

		// Token: 0x0400013E RID: 318
		private readonly Dictionary<InjectionType, LocalBuilder> injected = new Dictionary<InjectionType, LocalBuilder>();

		// Token: 0x0400013F RID: 319
		private readonly Dictionary<string, LocalBuilder> other = new Dictionary<string, LocalBuilder>();
	}
}
