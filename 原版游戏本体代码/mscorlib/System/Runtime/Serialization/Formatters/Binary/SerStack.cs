using System;
using System.Diagnostics;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x02000796 RID: 1942
	internal sealed class SerStack
	{
		// Token: 0x0600543C RID: 21564 RVA: 0x00128DDE File Offset: 0x00126FDE
		internal SerStack()
		{
			this.stackId = "System";
		}

		// Token: 0x0600543D RID: 21565 RVA: 0x00128E04 File Offset: 0x00127004
		internal SerStack(string stackId)
		{
			this.stackId = stackId;
		}

		// Token: 0x0600543E RID: 21566 RVA: 0x00128E28 File Offset: 0x00127028
		internal void Push(object obj)
		{
			if (this.top == this.objects.Length - 1)
			{
				this.IncreaseCapacity();
			}
			object[] array = this.objects;
			int num = this.top + 1;
			this.top = num;
			array[num] = obj;
		}

		// Token: 0x0600543F RID: 21567 RVA: 0x00128E68 File Offset: 0x00127068
		internal object Pop()
		{
			if (this.top < 0)
			{
				return null;
			}
			object result = this.objects[this.top];
			object[] array = this.objects;
			int num = this.top;
			this.top = num - 1;
			array[num] = null;
			return result;
		}

		// Token: 0x06005440 RID: 21568 RVA: 0x00128EA8 File Offset: 0x001270A8
		internal void IncreaseCapacity()
		{
			int num = this.objects.Length * 2;
			object[] destinationArray = new object[num];
			Array.Copy(this.objects, 0, destinationArray, 0, this.objects.Length);
			this.objects = destinationArray;
		}

		// Token: 0x06005441 RID: 21569 RVA: 0x00128EE4 File Offset: 0x001270E4
		internal object Peek()
		{
			if (this.top < 0)
			{
				return null;
			}
			return this.objects[this.top];
		}

		// Token: 0x06005442 RID: 21570 RVA: 0x00128EFE File Offset: 0x001270FE
		internal object PeekPeek()
		{
			if (this.top < 1)
			{
				return null;
			}
			return this.objects[this.top - 1];
		}

		// Token: 0x06005443 RID: 21571 RVA: 0x00128F1A File Offset: 0x0012711A
		internal int Count()
		{
			return this.top + 1;
		}

		// Token: 0x06005444 RID: 21572 RVA: 0x00128F24 File Offset: 0x00127124
		internal bool IsEmpty()
		{
			return this.top <= 0;
		}

		// Token: 0x06005445 RID: 21573 RVA: 0x00128F34 File Offset: 0x00127134
		[Conditional("SER_LOGGING")]
		internal void Dump()
		{
			for (int i = 0; i < this.Count(); i++)
			{
				object obj = this.objects[i];
			}
		}

		// Token: 0x0400264F RID: 9807
		internal object[] objects = new object[5];

		// Token: 0x04002650 RID: 9808
		internal string stackId;

		// Token: 0x04002651 RID: 9809
		internal int top = -1;

		// Token: 0x04002652 RID: 9810
		internal int next;
	}
}
