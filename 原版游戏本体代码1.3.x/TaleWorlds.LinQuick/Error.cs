using System;

namespace TaleWorlds.LinQuick
{
	// Token: 0x02000004 RID: 4
	internal static class Error
	{
		// Token: 0x06000060 RID: 96 RVA: 0x00003799 File Offset: 0x00001999
		internal static Exception ArgumentNull(string s)
		{
			return new ArgumentNullException(s);
		}

		// Token: 0x06000061 RID: 97 RVA: 0x000037A1 File Offset: 0x000019A1
		internal static Exception ArgumentOutOfRange(string s)
		{
			return new ArgumentOutOfRangeException(s);
		}

		// Token: 0x06000062 RID: 98 RVA: 0x000037A9 File Offset: 0x000019A9
		internal static Exception MoreThanOneElement()
		{
			return new InvalidOperationException("Sequence contains more than one element");
		}

		// Token: 0x06000063 RID: 99 RVA: 0x000037B5 File Offset: 0x000019B5
		internal static Exception MoreThanOneMatch()
		{
			return new InvalidOperationException("Sequence contains more than one matching element");
		}

		// Token: 0x06000064 RID: 100 RVA: 0x000037C1 File Offset: 0x000019C1
		internal static Exception NoElements()
		{
			return new InvalidOperationException("Sequence contains no elements");
		}

		// Token: 0x06000065 RID: 101 RVA: 0x000037CD File Offset: 0x000019CD
		internal static Exception NoMatch()
		{
			return new InvalidOperationException("Sequence contains no matching element");
		}

		// Token: 0x06000066 RID: 102 RVA: 0x000037D9 File Offset: 0x000019D9
		internal static Exception NotSupported()
		{
			return new NotSupportedException();
		}
	}
}
