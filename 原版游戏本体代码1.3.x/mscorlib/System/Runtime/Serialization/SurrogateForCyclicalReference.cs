using System;
using System.Security;

namespace System.Runtime.Serialization
{
	// Token: 0x02000730 RID: 1840
	internal sealed class SurrogateForCyclicalReference : ISerializationSurrogate
	{
		// Token: 0x060051A3 RID: 20899 RVA: 0x0011FD9E File Offset: 0x0011DF9E
		internal SurrogateForCyclicalReference(ISerializationSurrogate innerSurrogate)
		{
			if (innerSurrogate == null)
			{
				throw new ArgumentNullException("innerSurrogate");
			}
			this.innerSurrogate = innerSurrogate;
		}

		// Token: 0x060051A4 RID: 20900 RVA: 0x0011FDBB File Offset: 0x0011DFBB
		[SecurityCritical]
		public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
		{
			this.innerSurrogate.GetObjectData(obj, info, context);
		}

		// Token: 0x060051A5 RID: 20901 RVA: 0x0011FDCB File Offset: 0x0011DFCB
		[SecurityCritical]
		public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
		{
			return this.innerSurrogate.SetObjectData(obj, info, context, selector);
		}

		// Token: 0x04002440 RID: 9280
		private ISerializationSurrogate innerSurrogate;
	}
}
