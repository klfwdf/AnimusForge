using System;
using System.IO;
using System.Runtime.Serialization;

namespace Mono.Cecil
{
	// Token: 0x02000228 RID: 552
	[Serializable]
	internal sealed class AssemblyResolutionException : FileNotFoundException
	{
		// Token: 0x17000252 RID: 594
		// (get) Token: 0x06000BB2 RID: 2994 RVA: 0x00029905 File Offset: 0x00027B05
		public AssemblyNameReference AssemblyReference
		{
			get
			{
				return this.reference;
			}
		}

		// Token: 0x06000BB3 RID: 2995 RVA: 0x0002990D File Offset: 0x00027B0D
		public AssemblyResolutionException(AssemblyNameReference reference)
			: this(reference, null)
		{
		}

		// Token: 0x06000BB4 RID: 2996 RVA: 0x00029917 File Offset: 0x00027B17
		public AssemblyResolutionException(AssemblyNameReference reference, Exception innerException)
			: base(string.Format("Failed to resolve assembly: '{0}'", reference), innerException)
		{
			this.reference = reference;
		}

		// Token: 0x06000BB5 RID: 2997 RVA: 0x00029932 File Offset: 0x00027B32
		private AssemblyResolutionException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x0400038A RID: 906
		private readonly AssemblyNameReference reference;
	}
}
