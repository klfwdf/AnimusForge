using System;

namespace TaleWorlds.Engine
{
	// Token: 0x02000076 RID: 118
	public sealed class PhysicsJoint
	{
		// Token: 0x17000075 RID: 117
		// (get) Token: 0x06000A8D RID: 2701 RVA: 0x0000ACE8 File Offset: 0x00008EE8
		internal UIntPtr Pointer
		{
			get
			{
				return this._pointer;
			}
		}

		// Token: 0x06000A8E RID: 2702 RVA: 0x0000ACF0 File Offset: 0x00008EF0
		internal PhysicsJoint(UIntPtr ptr)
		{
			this._pointer = ptr;
		}

		// Token: 0x04000163 RID: 355
		private readonly UIntPtr _pointer;
	}
}
