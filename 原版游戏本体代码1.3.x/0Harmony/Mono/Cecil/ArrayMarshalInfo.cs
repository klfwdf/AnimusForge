using System;

namespace Mono.Cecil
{
	// Token: 0x0200025D RID: 605
	internal sealed class ArrayMarshalInfo : MarshalInfo
	{
		// Token: 0x17000324 RID: 804
		// (get) Token: 0x06000DA7 RID: 3495 RVA: 0x0002D67F File Offset: 0x0002B87F
		// (set) Token: 0x06000DA8 RID: 3496 RVA: 0x0002D687 File Offset: 0x0002B887
		public NativeType ElementType
		{
			get
			{
				return this.element_type;
			}
			set
			{
				this.element_type = value;
			}
		}

		// Token: 0x17000325 RID: 805
		// (get) Token: 0x06000DA9 RID: 3497 RVA: 0x0002D690 File Offset: 0x0002B890
		// (set) Token: 0x06000DAA RID: 3498 RVA: 0x0002D698 File Offset: 0x0002B898
		public int SizeParameterIndex
		{
			get
			{
				return this.size_parameter_index;
			}
			set
			{
				this.size_parameter_index = value;
			}
		}

		// Token: 0x17000326 RID: 806
		// (get) Token: 0x06000DAB RID: 3499 RVA: 0x0002D6A1 File Offset: 0x0002B8A1
		// (set) Token: 0x06000DAC RID: 3500 RVA: 0x0002D6A9 File Offset: 0x0002B8A9
		public int Size
		{
			get
			{
				return this.size;
			}
			set
			{
				this.size = value;
			}
		}

		// Token: 0x17000327 RID: 807
		// (get) Token: 0x06000DAD RID: 3501 RVA: 0x0002D6B2 File Offset: 0x0002B8B2
		// (set) Token: 0x06000DAE RID: 3502 RVA: 0x0002D6BA File Offset: 0x0002B8BA
		public int SizeParameterMultiplier
		{
			get
			{
				return this.size_parameter_multiplier;
			}
			set
			{
				this.size_parameter_multiplier = value;
			}
		}

		// Token: 0x06000DAF RID: 3503 RVA: 0x0002D6C3 File Offset: 0x0002B8C3
		public ArrayMarshalInfo()
			: base(NativeType.Array)
		{
			this.element_type = NativeType.None;
			this.size_parameter_index = -1;
			this.size = -1;
			this.size_parameter_multiplier = -1;
		}

		// Token: 0x0400040A RID: 1034
		internal NativeType element_type;

		// Token: 0x0400040B RID: 1035
		internal int size_parameter_index;

		// Token: 0x0400040C RID: 1036
		internal int size;

		// Token: 0x0400040D RID: 1037
		internal int size_parameter_multiplier;
	}
}
