using System;
using System.Diagnostics;

namespace System.Runtime.Versioning
{
	// Token: 0x02000728 RID: 1832
	[Conditional("FEATURE_READYTORUN")]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	internal sealed class NonVersionableAttribute : Attribute
	{
	}
}
