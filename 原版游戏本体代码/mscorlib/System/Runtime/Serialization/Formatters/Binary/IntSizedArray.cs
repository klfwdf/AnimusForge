using System;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x02000798 RID: 1944
	[Serializable]
	internal sealed class IntSizedArray : ICloneable
	{
		// Token: 0x0600544D RID: 21581 RVA: 0x00129138 File Offset: 0x00127338
		public IntSizedArray()
		{
		}

		// Token: 0x0600544E RID: 21582 RVA: 0x0012915C File Offset: 0x0012735C
		private IntSizedArray(IntSizedArray sizedArray)
		{
			this.objects = new int[sizedArray.objects.Length];
			sizedArray.objects.CopyTo(this.objects, 0);
			this.negObjects = new int[sizedArray.negObjects.Length];
			sizedArray.negObjects.CopyTo(this.negObjects, 0);
		}

		// Token: 0x0600544F RID: 21583 RVA: 0x001291D2 File Offset: 0x001273D2
		public object Clone()
		{
			return new IntSizedArray(this);
		}

		// Token: 0x17000DD9 RID: 3545
		internal int this[int index]
		{
			get
			{
				if (index < 0)
				{
					if (-index > this.negObjects.Length - 1)
					{
						return 0;
					}
					return this.negObjects[-index];
				}
				else
				{
					if (index > this.objects.Length - 1)
					{
						return 0;
					}
					return this.objects[index];
				}
			}
			set
			{
				if (index < 0)
				{
					if (-index > this.negObjects.Length - 1)
					{
						this.IncreaseCapacity(index);
					}
					this.negObjects[-index] = value;
					return;
				}
				if (index > this.objects.Length - 1)
				{
					this.IncreaseCapacity(index);
				}
				this.objects[index] = value;
			}
		}

		// Token: 0x06005452 RID: 21586 RVA: 0x00129264 File Offset: 0x00127464
		internal void IncreaseCapacity(int index)
		{
			try
			{
				if (index < 0)
				{
					int num = Math.Max(this.negObjects.Length * 2, -index + 1);
					int[] destinationArray = new int[num];
					Array.Copy(this.negObjects, 0, destinationArray, 0, this.negObjects.Length);
					this.negObjects = destinationArray;
				}
				else
				{
					int num2 = Math.Max(this.objects.Length * 2, index + 1);
					int[] destinationArray2 = new int[num2];
					Array.Copy(this.objects, 0, destinationArray2, 0, this.objects.Length);
					this.objects = destinationArray2;
				}
			}
			catch (Exception)
			{
				throw new SerializationException(Environment.GetResourceString("Serialization_CorruptedStream"));
			}
		}

		// Token: 0x04002655 RID: 9813
		internal int[] objects = new int[16];

		// Token: 0x04002656 RID: 9814
		internal int[] negObjects = new int[4];
	}
}
