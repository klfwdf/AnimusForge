using System;
using System.Diagnostics;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x0200007E RID: 126
	[EngineClass("rglResource")]
	public abstract class Resource : NativeObject
	{
		// Token: 0x1700007C RID: 124
		// (get) Token: 0x06000AAF RID: 2735 RVA: 0x0000B02E File Offset: 0x0000922E
		public bool IsValid
		{
			get
			{
				return base.Pointer != UIntPtr.Zero;
			}
		}

		// Token: 0x06000AB0 RID: 2736 RVA: 0x0000B040 File Offset: 0x00009240
		protected Resource()
		{
		}

		// Token: 0x06000AB1 RID: 2737 RVA: 0x0000B048 File Offset: 0x00009248
		internal Resource(UIntPtr pointer)
		{
			base.Construct(pointer);
		}

		// Token: 0x06000AB2 RID: 2738 RVA: 0x0000B057 File Offset: 0x00009257
		[Conditional("_RGL_KEEP_ASSERTS")]
		protected void CheckResourceParameter(Resource param, string paramName = "")
		{
			if (param == null)
			{
				throw new NullReferenceException(paramName);
			}
			if (!param.IsValid)
			{
				throw new ArgumentException(paramName);
			}
		}
	}
}
