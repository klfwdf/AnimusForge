using System;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x0200079C RID: 1948
	internal sealed class NameInfo
	{
		// Token: 0x0600545B RID: 21595 RVA: 0x001294BD File Offset: 0x001276BD
		internal NameInfo()
		{
		}

		// Token: 0x0600545C RID: 21596 RVA: 0x001294C8 File Offset: 0x001276C8
		internal void Init()
		{
			this.NIFullName = null;
			this.NIobjectId = 0L;
			this.NIassemId = 0L;
			this.NIprimitiveTypeEnum = InternalPrimitiveTypeE.Invalid;
			this.NItype = null;
			this.NIisSealed = false;
			this.NItransmitTypeOnObject = false;
			this.NItransmitTypeOnMember = false;
			this.NIisParentTypeOnObject = false;
			this.NIisArray = false;
			this.NIisArrayItem = false;
			this.NIarrayEnum = InternalArrayTypeE.Empty;
			this.NIsealedStatusChecked = false;
		}

		// Token: 0x17000DDA RID: 3546
		// (get) Token: 0x0600545D RID: 21597 RVA: 0x00129532 File Offset: 0x00127732
		public bool IsSealed
		{
			get
			{
				if (!this.NIsealedStatusChecked)
				{
					this.NIisSealed = this.NItype.IsSealed;
					this.NIsealedStatusChecked = true;
				}
				return this.NIisSealed;
			}
		}

		// Token: 0x17000DDB RID: 3547
		// (get) Token: 0x0600545E RID: 21598 RVA: 0x0012955A File Offset: 0x0012775A
		// (set) Token: 0x0600545F RID: 21599 RVA: 0x0012957B File Offset: 0x0012777B
		public string NIname
		{
			get
			{
				if (this.NIFullName == null)
				{
					this.NIFullName = this.NItype.FullName;
				}
				return this.NIFullName;
			}
			set
			{
				this.NIFullName = value;
			}
		}

		// Token: 0x04002665 RID: 9829
		internal string NIFullName;

		// Token: 0x04002666 RID: 9830
		internal long NIobjectId;

		// Token: 0x04002667 RID: 9831
		internal long NIassemId;

		// Token: 0x04002668 RID: 9832
		internal InternalPrimitiveTypeE NIprimitiveTypeEnum;

		// Token: 0x04002669 RID: 9833
		internal Type NItype;

		// Token: 0x0400266A RID: 9834
		internal bool NIisSealed;

		// Token: 0x0400266B RID: 9835
		internal bool NIisArray;

		// Token: 0x0400266C RID: 9836
		internal bool NIisArrayItem;

		// Token: 0x0400266D RID: 9837
		internal bool NItransmitTypeOnObject;

		// Token: 0x0400266E RID: 9838
		internal bool NItransmitTypeOnMember;

		// Token: 0x0400266F RID: 9839
		internal bool NIisParentTypeOnObject;

		// Token: 0x04002670 RID: 9840
		internal InternalArrayTypeE NIarrayEnum;

		// Token: 0x04002671 RID: 9841
		private bool NIsealedStatusChecked;
	}
}
