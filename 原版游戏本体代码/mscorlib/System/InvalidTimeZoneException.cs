using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace System
{
	// Token: 0x02000102 RID: 258
	[TypeForwardedFrom("System.Core, Version=3.5.0.0, Culture=Neutral, PublicKeyToken=b77a5c561934e089")]
	[__DynamicallyInvokable]
	[HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
	[Serializable]
	public class InvalidTimeZoneException : Exception
	{
		// Token: 0x06000FBB RID: 4027 RVA: 0x000302C1 File Offset: 0x0002E4C1
		[__DynamicallyInvokable]
		public InvalidTimeZoneException(string message)
			: base(message)
		{
		}

		// Token: 0x06000FBC RID: 4028 RVA: 0x000302CA File Offset: 0x0002E4CA
		[__DynamicallyInvokable]
		public InvalidTimeZoneException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		// Token: 0x06000FBD RID: 4029 RVA: 0x000302D4 File Offset: 0x0002E4D4
		protected InvalidTimeZoneException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x06000FBE RID: 4030 RVA: 0x000302DE File Offset: 0x0002E4DE
		[__DynamicallyInvokable]
		public InvalidTimeZoneException()
		{
		}
	}
}
