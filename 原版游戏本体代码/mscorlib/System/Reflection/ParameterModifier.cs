using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x02000618 RID: 1560
	[ComVisible(true)]
	[Serializable]
	public struct ParameterModifier
	{
		// Token: 0x06004849 RID: 18505 RVA: 0x00106CB2 File Offset: 0x00104EB2
		public ParameterModifier(int parameterCount)
		{
			if (parameterCount <= 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_ParmArraySize"));
			}
			this._byRef = new bool[parameterCount];
		}

		// Token: 0x17000B3C RID: 2876
		// (get) Token: 0x0600484A RID: 18506 RVA: 0x00106CD4 File Offset: 0x00104ED4
		internal bool[] IsByRefArray
		{
			get
			{
				return this._byRef;
			}
		}

		// Token: 0x17000B3D RID: 2877
		public bool this[int index]
		{
			get
			{
				return this._byRef[index];
			}
			set
			{
				this._byRef[index] = value;
			}
		}

		// Token: 0x04001E00 RID: 7680
		private bool[] _byRef;
	}
}
