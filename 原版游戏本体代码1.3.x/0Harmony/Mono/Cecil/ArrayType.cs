using System;
using System.Text;
using System.Threading;
using Mono.Cecil.Metadata;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x020001E5 RID: 485
	internal sealed class ArrayType : TypeSpecification
	{
		// Token: 0x17000228 RID: 552
		// (get) Token: 0x0600094F RID: 2383 RVA: 0x0001E748 File Offset: 0x0001C948
		public Collection<ArrayDimension> Dimensions
		{
			get
			{
				if (this.dimensions != null)
				{
					return this.dimensions;
				}
				Interlocked.CompareExchange<Collection<ArrayDimension>>(ref this.dimensions, new Collection<ArrayDimension> { default(ArrayDimension) }, null);
				return this.dimensions;
			}
		}

		// Token: 0x17000229 RID: 553
		// (get) Token: 0x06000950 RID: 2384 RVA: 0x0001E78D File Offset: 0x0001C98D
		public int Rank
		{
			get
			{
				if (this.dimensions != null)
				{
					return this.dimensions.Count;
				}
				return 1;
			}
		}

		// Token: 0x1700022A RID: 554
		// (get) Token: 0x06000951 RID: 2385 RVA: 0x0001E7A4 File Offset: 0x0001C9A4
		public bool IsVector
		{
			get
			{
				return this.dimensions == null || (this.dimensions.Count <= 1 && !this.dimensions[0].IsSized);
			}
		}

		// Token: 0x1700022B RID: 555
		// (get) Token: 0x06000952 RID: 2386 RVA: 0x0001B69F File Offset: 0x0001989F
		// (set) Token: 0x06000953 RID: 2387 RVA: 0x0001C627 File Offset: 0x0001A827
		public override bool IsValueType
		{
			get
			{
				return false;
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		// Token: 0x1700022C RID: 556
		// (get) Token: 0x06000954 RID: 2388 RVA: 0x0001E7E2 File Offset: 0x0001C9E2
		public override string Name
		{
			get
			{
				return base.Name + this.Suffix;
			}
		}

		// Token: 0x1700022D RID: 557
		// (get) Token: 0x06000955 RID: 2389 RVA: 0x0001E7F5 File Offset: 0x0001C9F5
		public override string FullName
		{
			get
			{
				return base.FullName + this.Suffix;
			}
		}

		// Token: 0x1700022E RID: 558
		// (get) Token: 0x06000956 RID: 2390 RVA: 0x0001E808 File Offset: 0x0001CA08
		private string Suffix
		{
			get
			{
				if (this.IsVector)
				{
					return "[]";
				}
				StringBuilder suffix = new StringBuilder();
				suffix.Append("[");
				for (int i = 0; i < this.dimensions.Count; i++)
				{
					if (i > 0)
					{
						suffix.Append(",");
					}
					suffix.Append(this.dimensions[i].ToString());
				}
				suffix.Append("]");
				return suffix.ToString();
			}
		}

		// Token: 0x1700022F RID: 559
		// (get) Token: 0x06000957 RID: 2391 RVA: 0x0001B6C7 File Offset: 0x000198C7
		public override bool IsArray
		{
			get
			{
				return true;
			}
		}

		// Token: 0x06000958 RID: 2392 RVA: 0x0001E88E File Offset: 0x0001CA8E
		public ArrayType(TypeReference type)
			: base(type)
		{
			Mixin.CheckType(type);
			this.etype = Mono.Cecil.Metadata.ElementType.Array;
		}

		// Token: 0x06000959 RID: 2393 RVA: 0x0001E8A8 File Offset: 0x0001CAA8
		public ArrayType(TypeReference type, int rank)
			: this(type)
		{
			Mixin.CheckType(type);
			if (rank == 1)
			{
				return;
			}
			this.dimensions = new Collection<ArrayDimension>(rank);
			for (int i = 0; i < rank; i++)
			{
				this.dimensions.Add(default(ArrayDimension));
			}
			this.etype = Mono.Cecil.Metadata.ElementType.Array;
		}

		// Token: 0x04000319 RID: 793
		private Collection<ArrayDimension> dimensions;
	}
}
