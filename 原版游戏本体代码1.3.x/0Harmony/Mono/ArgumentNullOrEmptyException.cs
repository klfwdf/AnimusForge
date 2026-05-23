using System;

namespace Mono
{
	// Token: 0x020001DB RID: 475
	internal class ArgumentNullOrEmptyException : ArgumentException
	{
		// Token: 0x06000890 RID: 2192 RVA: 0x0001B89A File Offset: 0x00019A9A
		public ArgumentNullOrEmptyException(string paramName)
			: base("Argument null or empty", paramName)
		{
		}
	}
}
