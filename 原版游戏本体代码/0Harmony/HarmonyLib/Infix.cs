using System;
using System.Collections.Generic;
using System.Reflection;

namespace HarmonyLib
{
	// Token: 0x02000021 RID: 33
	internal class Infix
	{
		// Token: 0x060000A8 RID: 168 RVA: 0x000052A7 File Offset: 0x000034A7
		internal Infix(Patch patch)
		{
			this.patch = patch;
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x060000A9 RID: 169 RVA: 0x000052B6 File Offset: 0x000034B6
		internal MethodInfo OuterMethod
		{
			get
			{
				return this.patch.PatchMethod;
			}
		}

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x060000AA RID: 170 RVA: 0x000052C3 File Offset: 0x000034C3
		internal MethodBase InnerMethod
		{
			get
			{
				return this.patch.innerMethod.Method;
			}
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x060000AB RID: 171 RVA: 0x000052D5 File Offset: 0x000034D5
		internal int[] Positions
		{
			get
			{
				return this.patch.innerMethod.positions;
			}
		}

		// Token: 0x060000AC RID: 172 RVA: 0x000052E8 File Offset: 0x000034E8
		internal bool Matches(MethodBase method, int index, int total)
		{
			if (method != this.InnerMethod)
			{
				return false;
			}
			if (this.Positions.Length == 0)
			{
				return true;
			}
			foreach (int pos in this.Positions)
			{
				if (pos > 0 && pos == index)
				{
					return true;
				}
				if (pos < 0 && index == total + pos + 1)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x060000AD RID: 173 RVA: 0x00005342 File Offset: 0x00003542
		internal IEnumerable<CodeInstruction> Apply(MethodCreatorConfig config, bool isPrefix)
		{
			Infix.<Apply>d__9 <Apply>d__ = new Infix.<Apply>d__9(-2);
			<Apply>d__.<>3__config = config;
			<Apply>d__.<>3__isPrefix = isPrefix;
			return <Apply>d__;
		}

		// Token: 0x04000057 RID: 87
		internal Patch patch;
	}
}
