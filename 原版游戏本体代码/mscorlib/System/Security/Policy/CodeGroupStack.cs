using System;
using System.Collections;

namespace System.Security.Policy
{
	// Token: 0x02000367 RID: 871
	internal sealed class CodeGroupStack
	{
		// Token: 0x06002B15 RID: 11029 RVA: 0x000A0A77 File Offset: 0x0009EC77
		internal CodeGroupStack()
		{
			this.m_array = new ArrayList();
		}

		// Token: 0x06002B16 RID: 11030 RVA: 0x000A0A8A File Offset: 0x0009EC8A
		internal void Push(CodeGroupStackFrame element)
		{
			this.m_array.Add(element);
		}

		// Token: 0x06002B17 RID: 11031 RVA: 0x000A0A9C File Offset: 0x0009EC9C
		internal CodeGroupStackFrame Pop()
		{
			if (this.IsEmpty())
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EmptyStack"));
			}
			int count = this.m_array.Count;
			CodeGroupStackFrame result = (CodeGroupStackFrame)this.m_array[count - 1];
			this.m_array.RemoveAt(count - 1);
			return result;
		}

		// Token: 0x06002B18 RID: 11032 RVA: 0x000A0AF0 File Offset: 0x0009ECF0
		internal bool IsEmpty()
		{
			return this.m_array.Count == 0;
		}

		// Token: 0x04001190 RID: 4496
		private ArrayList m_array;
	}
}
