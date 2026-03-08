using System;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x02000797 RID: 1943
	[Serializable]
	internal sealed class SizedArray : ICloneable
	{
		// Token: 0x06005446 RID: 21574 RVA: 0x00128F5B File Offset: 0x0012715B
		internal SizedArray()
		{
			this.objects = new object[16];
			this.negObjects = new object[4];
		}

		// Token: 0x06005447 RID: 21575 RVA: 0x00128F7C File Offset: 0x0012717C
		internal SizedArray(int length)
		{
			this.objects = new object[length];
			this.negObjects = new object[length];
		}

		// Token: 0x06005448 RID: 21576 RVA: 0x00128F9C File Offset: 0x0012719C
		private SizedArray(SizedArray sizedArray)
		{
			this.objects = new object[sizedArray.objects.Length];
			sizedArray.objects.CopyTo(this.objects, 0);
			this.negObjects = new object[sizedArray.negObjects.Length];
			sizedArray.negObjects.CopyTo(this.negObjects, 0);
		}

		// Token: 0x06005449 RID: 21577 RVA: 0x00128FF9 File Offset: 0x001271F9
		public object Clone()
		{
			return new SizedArray(this);
		}

		// Token: 0x17000DD8 RID: 3544
		internal object this[int index]
		{
			get
			{
				if (index < 0)
				{
					if (-index > this.negObjects.Length - 1)
					{
						return null;
					}
					return this.negObjects[-index];
				}
				else
				{
					if (index > this.objects.Length - 1)
					{
						return null;
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
				object obj = this.objects[index];
				this.objects[index] = value;
			}
		}

		// Token: 0x0600544C RID: 21580 RVA: 0x00129090 File Offset: 0x00127290
		internal void IncreaseCapacity(int index)
		{
			try
			{
				if (index < 0)
				{
					int num = Math.Max(this.negObjects.Length * 2, -index + 1);
					object[] destinationArray = new object[num];
					Array.Copy(this.negObjects, 0, destinationArray, 0, this.negObjects.Length);
					this.negObjects = destinationArray;
				}
				else
				{
					int num2 = Math.Max(this.objects.Length * 2, index + 1);
					object[] destinationArray2 = new object[num2];
					Array.Copy(this.objects, 0, destinationArray2, 0, this.objects.Length);
					this.objects = destinationArray2;
				}
			}
			catch (Exception)
			{
				throw new SerializationException(Environment.GetResourceString("Serialization_CorruptedStream"));
			}
		}

		// Token: 0x04002653 RID: 9811
		internal object[] objects;

		// Token: 0x04002654 RID: 9812
		internal object[] negObjects;
	}
}
