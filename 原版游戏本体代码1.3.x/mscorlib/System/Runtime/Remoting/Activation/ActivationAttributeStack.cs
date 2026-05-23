using System;

namespace System.Runtime.Remoting.Activation
{
	// Token: 0x02000898 RID: 2200
	internal class ActivationAttributeStack
	{
		// Token: 0x06005D25 RID: 23845 RVA: 0x00146AA3 File Offset: 0x00144CA3
		internal ActivationAttributeStack()
		{
			this.activationTypes = new object[4];
			this.activationAttributes = new object[4];
			this.freeIndex = 0;
		}

		// Token: 0x06005D26 RID: 23846 RVA: 0x00146ACC File Offset: 0x00144CCC
		internal void Push(Type typ, object[] attr)
		{
			if (this.freeIndex == this.activationTypes.Length)
			{
				object[] destinationArray = new object[this.activationTypes.Length * 2];
				object[] destinationArray2 = new object[this.activationAttributes.Length * 2];
				Array.Copy(this.activationTypes, destinationArray, this.activationTypes.Length);
				Array.Copy(this.activationAttributes, destinationArray2, this.activationAttributes.Length);
				this.activationTypes = destinationArray;
				this.activationAttributes = destinationArray2;
			}
			this.activationTypes[this.freeIndex] = typ;
			this.activationAttributes[this.freeIndex] = attr;
			this.freeIndex++;
		}

		// Token: 0x06005D27 RID: 23847 RVA: 0x00146B69 File Offset: 0x00144D69
		internal object[] Peek(Type typ)
		{
			if (this.freeIndex == 0 || this.activationTypes[this.freeIndex - 1] != typ)
			{
				return null;
			}
			return (object[])this.activationAttributes[this.freeIndex - 1];
		}

		// Token: 0x06005D28 RID: 23848 RVA: 0x00146B9C File Offset: 0x00144D9C
		internal void Pop(Type typ)
		{
			if (this.freeIndex != 0 && this.activationTypes[this.freeIndex - 1] == typ)
			{
				this.freeIndex--;
				this.activationTypes[this.freeIndex] = null;
				this.activationAttributes[this.freeIndex] = null;
			}
		}

		// Token: 0x040029F3 RID: 10739
		private object[] activationTypes;

		// Token: 0x040029F4 RID: 10740
		private object[] activationAttributes;

		// Token: 0x040029F5 RID: 10741
		private int freeIndex;
	}
}
