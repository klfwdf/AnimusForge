using System;
using System.Runtime.CompilerServices;
using Mono.Cecil;

namespace MonoMod.Utils
{
	// Token: 0x020008B9 RID: 2233
	// (Invoke) Token: 0x06002E5B RID: 11867
	internal delegate IMetadataTokenProvider Relinker(IMetadataTokenProvider mtp, [Nullable(2)] IGenericParameterProvider context);
}
