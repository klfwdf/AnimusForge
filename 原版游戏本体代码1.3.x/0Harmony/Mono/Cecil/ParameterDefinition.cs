using System;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000286 RID: 646
	internal sealed class ParameterDefinition : ParameterReference, ICustomAttributeProvider, IMetadataTokenProvider, IConstantProvider, IMarshalInfoProvider
	{
		// Token: 0x170003FC RID: 1020
		// (get) Token: 0x06000FD6 RID: 4054 RVA: 0x0003139D File Offset: 0x0002F59D
		// (set) Token: 0x06000FD7 RID: 4055 RVA: 0x000313A5 File Offset: 0x0002F5A5
		public ParameterAttributes Attributes
		{
			get
			{
				return (ParameterAttributes)this.attributes;
			}
			set
			{
				this.attributes = (ushort)value;
			}
		}

		// Token: 0x170003FD RID: 1021
		// (get) Token: 0x06000FD8 RID: 4056 RVA: 0x000313AE File Offset: 0x0002F5AE
		public IMethodSignature Method
		{
			get
			{
				return this.method;
			}
		}

		// Token: 0x170003FE RID: 1022
		// (get) Token: 0x06000FD9 RID: 4057 RVA: 0x000313B6 File Offset: 0x0002F5B6
		public int Sequence
		{
			get
			{
				if (this.method == null)
				{
					return -1;
				}
				if (!this.method.HasImplicitThis())
				{
					return this.index;
				}
				return this.index + 1;
			}
		}

		// Token: 0x170003FF RID: 1023
		// (get) Token: 0x06000FDA RID: 4058 RVA: 0x000313DE File Offset: 0x0002F5DE
		// (set) Token: 0x06000FDB RID: 4059 RVA: 0x00031407 File Offset: 0x0002F607
		public bool HasConstant
		{
			get
			{
				this.ResolveConstant(ref this.constant, this.parameter_type.Module);
				return this.constant != Mixin.NoValue;
			}
			set
			{
				if (!value)
				{
					this.constant = Mixin.NoValue;
				}
			}
		}

		// Token: 0x17000400 RID: 1024
		// (get) Token: 0x06000FDC RID: 4060 RVA: 0x00031417 File Offset: 0x0002F617
		// (set) Token: 0x06000FDD RID: 4061 RVA: 0x00031429 File Offset: 0x0002F629
		public object Constant
		{
			get
			{
				if (!this.HasConstant)
				{
					return null;
				}
				return this.constant;
			}
			set
			{
				this.constant = value;
			}
		}

		// Token: 0x17000401 RID: 1025
		// (get) Token: 0x06000FDE RID: 4062 RVA: 0x00031432 File Offset: 0x0002F632
		public bool HasCustomAttributes
		{
			get
			{
				if (this.custom_attributes != null)
				{
					return this.custom_attributes.Count > 0;
				}
				return this.GetHasCustomAttributes(this.parameter_type.Module);
			}
		}

		// Token: 0x17000402 RID: 1026
		// (get) Token: 0x06000FDF RID: 4063 RVA: 0x0003145C File Offset: 0x0002F65C
		public Collection<CustomAttribute> CustomAttributes
		{
			get
			{
				return this.custom_attributes ?? this.GetCustomAttributes(ref this.custom_attributes, this.parameter_type.Module);
			}
		}

		// Token: 0x17000403 RID: 1027
		// (get) Token: 0x06000FE0 RID: 4064 RVA: 0x0003147F File Offset: 0x0002F67F
		public bool HasMarshalInfo
		{
			get
			{
				return this.marshal_info != null || this.GetHasMarshalInfo(this.parameter_type.Module);
			}
		}

		// Token: 0x17000404 RID: 1028
		// (get) Token: 0x06000FE1 RID: 4065 RVA: 0x0003149C File Offset: 0x0002F69C
		// (set) Token: 0x06000FE2 RID: 4066 RVA: 0x000314BF File Offset: 0x0002F6BF
		public MarshalInfo MarshalInfo
		{
			get
			{
				return this.marshal_info ?? this.GetMarshalInfo(ref this.marshal_info, this.parameter_type.Module);
			}
			set
			{
				this.marshal_info = value;
			}
		}

		// Token: 0x17000405 RID: 1029
		// (get) Token: 0x06000FE3 RID: 4067 RVA: 0x000314C8 File Offset: 0x0002F6C8
		// (set) Token: 0x06000FE4 RID: 4068 RVA: 0x000314D6 File Offset: 0x0002F6D6
		public bool IsIn
		{
			get
			{
				return this.attributes.GetAttributes(1);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(1, value);
			}
		}

		// Token: 0x17000406 RID: 1030
		// (get) Token: 0x06000FE5 RID: 4069 RVA: 0x000314EB File Offset: 0x0002F6EB
		// (set) Token: 0x06000FE6 RID: 4070 RVA: 0x000314F9 File Offset: 0x0002F6F9
		public bool IsOut
		{
			get
			{
				return this.attributes.GetAttributes(2);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(2, value);
			}
		}

		// Token: 0x17000407 RID: 1031
		// (get) Token: 0x06000FE7 RID: 4071 RVA: 0x0003150E File Offset: 0x0002F70E
		// (set) Token: 0x06000FE8 RID: 4072 RVA: 0x0003151C File Offset: 0x0002F71C
		public bool IsLcid
		{
			get
			{
				return this.attributes.GetAttributes(4);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(4, value);
			}
		}

		// Token: 0x17000408 RID: 1032
		// (get) Token: 0x06000FE9 RID: 4073 RVA: 0x00031531 File Offset: 0x0002F731
		// (set) Token: 0x06000FEA RID: 4074 RVA: 0x0003153F File Offset: 0x0002F73F
		public bool IsReturnValue
		{
			get
			{
				return this.attributes.GetAttributes(8);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(8, value);
			}
		}

		// Token: 0x17000409 RID: 1033
		// (get) Token: 0x06000FEB RID: 4075 RVA: 0x00031554 File Offset: 0x0002F754
		// (set) Token: 0x06000FEC RID: 4076 RVA: 0x00031563 File Offset: 0x0002F763
		public bool IsOptional
		{
			get
			{
				return this.attributes.GetAttributes(16);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(16, value);
			}
		}

		// Token: 0x1700040A RID: 1034
		// (get) Token: 0x06000FED RID: 4077 RVA: 0x00031579 File Offset: 0x0002F779
		// (set) Token: 0x06000FEE RID: 4078 RVA: 0x0003158B File Offset: 0x0002F78B
		public bool HasDefault
		{
			get
			{
				return this.attributes.GetAttributes(4096);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(4096, value);
			}
		}

		// Token: 0x1700040B RID: 1035
		// (get) Token: 0x06000FEF RID: 4079 RVA: 0x000315A4 File Offset: 0x0002F7A4
		// (set) Token: 0x06000FF0 RID: 4080 RVA: 0x000315B6 File Offset: 0x0002F7B6
		public bool HasFieldMarshal
		{
			get
			{
				return this.attributes.GetAttributes(8192);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(8192, value);
			}
		}

		// Token: 0x06000FF1 RID: 4081 RVA: 0x000315CF File Offset: 0x0002F7CF
		internal ParameterDefinition(TypeReference parameterType, IMethodSignature method)
			: this(string.Empty, ParameterAttributes.None, parameterType)
		{
			this.method = method;
		}

		// Token: 0x06000FF2 RID: 4082 RVA: 0x000315E5 File Offset: 0x0002F7E5
		public ParameterDefinition(TypeReference parameterType)
			: this(string.Empty, ParameterAttributes.None, parameterType)
		{
		}

		// Token: 0x06000FF3 RID: 4083 RVA: 0x000315F4 File Offset: 0x0002F7F4
		public ParameterDefinition(string name, ParameterAttributes attributes, TypeReference parameterType)
			: base(name, parameterType)
		{
			this.attributes = (ushort)attributes;
			this.token = new MetadataToken(TokenType.Param);
		}

		// Token: 0x06000FF4 RID: 4084 RVA: 0x0001B6A2 File Offset: 0x000198A2
		public override ParameterDefinition Resolve()
		{
			return this;
		}

		// Token: 0x0400053F RID: 1343
		private ushort attributes;

		// Token: 0x04000540 RID: 1344
		internal IMethodSignature method;

		// Token: 0x04000541 RID: 1345
		private object constant = Mixin.NotResolved;

		// Token: 0x04000542 RID: 1346
		private Collection<CustomAttribute> custom_attributes;

		// Token: 0x04000543 RID: 1347
		private MarshalInfo marshal_info;
	}
}
