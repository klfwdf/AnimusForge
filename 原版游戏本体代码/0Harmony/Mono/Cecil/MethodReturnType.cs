using System;
using System.Threading;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000271 RID: 625
	internal sealed class MethodReturnType : IConstantProvider, IMetadataTokenProvider, ICustomAttributeProvider, IMarshalInfoProvider
	{
		// Token: 0x1700038A RID: 906
		// (get) Token: 0x06000EC2 RID: 3778 RVA: 0x0002F9FA File Offset: 0x0002DBFA
		public IMethodSignature Method
		{
			get
			{
				return this.method;
			}
		}

		// Token: 0x1700038B RID: 907
		// (get) Token: 0x06000EC3 RID: 3779 RVA: 0x0002FA02 File Offset: 0x0002DC02
		// (set) Token: 0x06000EC4 RID: 3780 RVA: 0x0002FA0A File Offset: 0x0002DC0A
		public TypeReference ReturnType
		{
			get
			{
				return this.return_type;
			}
			set
			{
				this.return_type = value;
			}
		}

		// Token: 0x1700038C RID: 908
		// (get) Token: 0x06000EC5 RID: 3781 RVA: 0x0002FA13 File Offset: 0x0002DC13
		internal ParameterDefinition Parameter
		{
			get
			{
				if (this.parameter == null)
				{
					Interlocked.CompareExchange<ParameterDefinition>(ref this.parameter, new ParameterDefinition(this.return_type, this.method), null);
				}
				return this.parameter;
			}
		}

		// Token: 0x1700038D RID: 909
		// (get) Token: 0x06000EC6 RID: 3782 RVA: 0x0002FA41 File Offset: 0x0002DC41
		// (set) Token: 0x06000EC7 RID: 3783 RVA: 0x0002FA4E File Offset: 0x0002DC4E
		public MetadataToken MetadataToken
		{
			get
			{
				return this.Parameter.MetadataToken;
			}
			set
			{
				this.Parameter.MetadataToken = value;
			}
		}

		// Token: 0x1700038E RID: 910
		// (get) Token: 0x06000EC8 RID: 3784 RVA: 0x0002FA5C File Offset: 0x0002DC5C
		// (set) Token: 0x06000EC9 RID: 3785 RVA: 0x0002FA69 File Offset: 0x0002DC69
		public ParameterAttributes Attributes
		{
			get
			{
				return this.Parameter.Attributes;
			}
			set
			{
				this.Parameter.Attributes = value;
			}
		}

		// Token: 0x1700038F RID: 911
		// (get) Token: 0x06000ECA RID: 3786 RVA: 0x0002FA77 File Offset: 0x0002DC77
		// (set) Token: 0x06000ECB RID: 3787 RVA: 0x0002FA84 File Offset: 0x0002DC84
		public string Name
		{
			get
			{
				return this.Parameter.Name;
			}
			set
			{
				this.Parameter.Name = value;
			}
		}

		// Token: 0x17000390 RID: 912
		// (get) Token: 0x06000ECC RID: 3788 RVA: 0x0002FA92 File Offset: 0x0002DC92
		public bool HasCustomAttributes
		{
			get
			{
				return this.parameter != null && this.parameter.HasCustomAttributes;
			}
		}

		// Token: 0x17000391 RID: 913
		// (get) Token: 0x06000ECD RID: 3789 RVA: 0x0002FAA9 File Offset: 0x0002DCA9
		public Collection<CustomAttribute> CustomAttributes
		{
			get
			{
				return this.Parameter.CustomAttributes;
			}
		}

		// Token: 0x17000392 RID: 914
		// (get) Token: 0x06000ECE RID: 3790 RVA: 0x0002FAB6 File Offset: 0x0002DCB6
		// (set) Token: 0x06000ECF RID: 3791 RVA: 0x0002FACD File Offset: 0x0002DCCD
		public bool HasDefault
		{
			get
			{
				return this.parameter != null && this.parameter.HasDefault;
			}
			set
			{
				this.Parameter.HasDefault = value;
			}
		}

		// Token: 0x17000393 RID: 915
		// (get) Token: 0x06000ED0 RID: 3792 RVA: 0x0002FADB File Offset: 0x0002DCDB
		// (set) Token: 0x06000ED1 RID: 3793 RVA: 0x0002FAF2 File Offset: 0x0002DCF2
		public bool HasConstant
		{
			get
			{
				return this.parameter != null && this.parameter.HasConstant;
			}
			set
			{
				this.Parameter.HasConstant = value;
			}
		}

		// Token: 0x17000394 RID: 916
		// (get) Token: 0x06000ED2 RID: 3794 RVA: 0x0002FB00 File Offset: 0x0002DD00
		// (set) Token: 0x06000ED3 RID: 3795 RVA: 0x0002FB0D File Offset: 0x0002DD0D
		public object Constant
		{
			get
			{
				return this.Parameter.Constant;
			}
			set
			{
				this.Parameter.Constant = value;
			}
		}

		// Token: 0x17000395 RID: 917
		// (get) Token: 0x06000ED4 RID: 3796 RVA: 0x0002FB1B File Offset: 0x0002DD1B
		// (set) Token: 0x06000ED5 RID: 3797 RVA: 0x0002FB32 File Offset: 0x0002DD32
		public bool HasFieldMarshal
		{
			get
			{
				return this.parameter != null && this.parameter.HasFieldMarshal;
			}
			set
			{
				this.Parameter.HasFieldMarshal = value;
			}
		}

		// Token: 0x17000396 RID: 918
		// (get) Token: 0x06000ED6 RID: 3798 RVA: 0x0002FB40 File Offset: 0x0002DD40
		public bool HasMarshalInfo
		{
			get
			{
				return this.parameter != null && this.parameter.HasMarshalInfo;
			}
		}

		// Token: 0x17000397 RID: 919
		// (get) Token: 0x06000ED7 RID: 3799 RVA: 0x0002FB57 File Offset: 0x0002DD57
		// (set) Token: 0x06000ED8 RID: 3800 RVA: 0x0002FB64 File Offset: 0x0002DD64
		public MarshalInfo MarshalInfo
		{
			get
			{
				return this.Parameter.MarshalInfo;
			}
			set
			{
				this.Parameter.MarshalInfo = value;
			}
		}

		// Token: 0x06000ED9 RID: 3801 RVA: 0x0002FB72 File Offset: 0x0002DD72
		public MethodReturnType(IMethodSignature method)
		{
			this.method = method;
		}

		// Token: 0x04000489 RID: 1161
		internal IMethodSignature method;

		// Token: 0x0400048A RID: 1162
		internal ParameterDefinition parameter;

		// Token: 0x0400048B RID: 1163
		private TypeReference return_type;
	}
}
