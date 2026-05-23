using System;

namespace Mono.Cecil
{
	// Token: 0x02000236 RID: 566
	internal sealed class ExportedType : IMetadataTokenProvider
	{
		// Token: 0x17000285 RID: 645
		// (get) Token: 0x06000C2F RID: 3119 RVA: 0x0002AB0F File Offset: 0x00028D0F
		// (set) Token: 0x06000C30 RID: 3120 RVA: 0x0002AB17 File Offset: 0x00028D17
		public string Namespace
		{
			get
			{
				return this.@namespace;
			}
			set
			{
				this.@namespace = value;
			}
		}

		// Token: 0x17000286 RID: 646
		// (get) Token: 0x06000C31 RID: 3121 RVA: 0x0002AB20 File Offset: 0x00028D20
		// (set) Token: 0x06000C32 RID: 3122 RVA: 0x0002AB28 File Offset: 0x00028D28
		public string Name
		{
			get
			{
				return this.name;
			}
			set
			{
				this.name = value;
			}
		}

		// Token: 0x17000287 RID: 647
		// (get) Token: 0x06000C33 RID: 3123 RVA: 0x0002AB31 File Offset: 0x00028D31
		// (set) Token: 0x06000C34 RID: 3124 RVA: 0x0002AB39 File Offset: 0x00028D39
		public TypeAttributes Attributes
		{
			get
			{
				return (TypeAttributes)this.attributes;
			}
			set
			{
				this.attributes = (uint)value;
			}
		}

		// Token: 0x17000288 RID: 648
		// (get) Token: 0x06000C35 RID: 3125 RVA: 0x0002AB42 File Offset: 0x00028D42
		// (set) Token: 0x06000C36 RID: 3126 RVA: 0x0002AB5E File Offset: 0x00028D5E
		public IMetadataScope Scope
		{
			get
			{
				if (this.declaring_type != null)
				{
					return this.declaring_type.Scope;
				}
				return this.scope;
			}
			set
			{
				if (this.declaring_type != null)
				{
					this.declaring_type.Scope = value;
					return;
				}
				this.scope = value;
			}
		}

		// Token: 0x17000289 RID: 649
		// (get) Token: 0x06000C37 RID: 3127 RVA: 0x0002AB7C File Offset: 0x00028D7C
		// (set) Token: 0x06000C38 RID: 3128 RVA: 0x0002AB84 File Offset: 0x00028D84
		public ExportedType DeclaringType
		{
			get
			{
				return this.declaring_type;
			}
			set
			{
				this.declaring_type = value;
			}
		}

		// Token: 0x1700028A RID: 650
		// (get) Token: 0x06000C39 RID: 3129 RVA: 0x0002AB8D File Offset: 0x00028D8D
		// (set) Token: 0x06000C3A RID: 3130 RVA: 0x0002AB95 File Offset: 0x00028D95
		public MetadataToken MetadataToken
		{
			get
			{
				return this.token;
			}
			set
			{
				this.token = value;
			}
		}

		// Token: 0x1700028B RID: 651
		// (get) Token: 0x06000C3B RID: 3131 RVA: 0x0002AB9E File Offset: 0x00028D9E
		// (set) Token: 0x06000C3C RID: 3132 RVA: 0x0002ABA6 File Offset: 0x00028DA6
		public int Identifier
		{
			get
			{
				return this.identifier;
			}
			set
			{
				this.identifier = value;
			}
		}

		// Token: 0x1700028C RID: 652
		// (get) Token: 0x06000C3D RID: 3133 RVA: 0x0002ABAF File Offset: 0x00028DAF
		// (set) Token: 0x06000C3E RID: 3134 RVA: 0x0002ABBE File Offset: 0x00028DBE
		public bool IsNotPublic
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7U, 0U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7U, 0U, value);
			}
		}

		// Token: 0x1700028D RID: 653
		// (get) Token: 0x06000C3F RID: 3135 RVA: 0x0002ABD4 File Offset: 0x00028DD4
		// (set) Token: 0x06000C40 RID: 3136 RVA: 0x0002ABE3 File Offset: 0x00028DE3
		public bool IsPublic
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7U, 1U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7U, 1U, value);
			}
		}

		// Token: 0x1700028E RID: 654
		// (get) Token: 0x06000C41 RID: 3137 RVA: 0x0002ABF9 File Offset: 0x00028DF9
		// (set) Token: 0x06000C42 RID: 3138 RVA: 0x0002AC08 File Offset: 0x00028E08
		public bool IsNestedPublic
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7U, 2U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7U, 2U, value);
			}
		}

		// Token: 0x1700028F RID: 655
		// (get) Token: 0x06000C43 RID: 3139 RVA: 0x0002AC1E File Offset: 0x00028E1E
		// (set) Token: 0x06000C44 RID: 3140 RVA: 0x0002AC2D File Offset: 0x00028E2D
		public bool IsNestedPrivate
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7U, 3U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7U, 3U, value);
			}
		}

		// Token: 0x17000290 RID: 656
		// (get) Token: 0x06000C45 RID: 3141 RVA: 0x0002AC43 File Offset: 0x00028E43
		// (set) Token: 0x06000C46 RID: 3142 RVA: 0x0002AC52 File Offset: 0x00028E52
		public bool IsNestedFamily
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7U, 4U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7U, 4U, value);
			}
		}

		// Token: 0x17000291 RID: 657
		// (get) Token: 0x06000C47 RID: 3143 RVA: 0x0002AC68 File Offset: 0x00028E68
		// (set) Token: 0x06000C48 RID: 3144 RVA: 0x0002AC77 File Offset: 0x00028E77
		public bool IsNestedAssembly
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7U, 5U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7U, 5U, value);
			}
		}

		// Token: 0x17000292 RID: 658
		// (get) Token: 0x06000C49 RID: 3145 RVA: 0x0002AC8D File Offset: 0x00028E8D
		// (set) Token: 0x06000C4A RID: 3146 RVA: 0x0002AC9C File Offset: 0x00028E9C
		public bool IsNestedFamilyAndAssembly
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7U, 6U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7U, 6U, value);
			}
		}

		// Token: 0x17000293 RID: 659
		// (get) Token: 0x06000C4B RID: 3147 RVA: 0x0002ACB2 File Offset: 0x00028EB2
		// (set) Token: 0x06000C4C RID: 3148 RVA: 0x0002ACC1 File Offset: 0x00028EC1
		public bool IsNestedFamilyOrAssembly
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7U, 7U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7U, 7U, value);
			}
		}

		// Token: 0x17000294 RID: 660
		// (get) Token: 0x06000C4D RID: 3149 RVA: 0x0002ACD7 File Offset: 0x00028ED7
		// (set) Token: 0x06000C4E RID: 3150 RVA: 0x0002ACE7 File Offset: 0x00028EE7
		public bool IsAutoLayout
		{
			get
			{
				return this.attributes.GetMaskedAttributes(24U, 0U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(24U, 0U, value);
			}
		}

		// Token: 0x17000295 RID: 661
		// (get) Token: 0x06000C4F RID: 3151 RVA: 0x0002ACFE File Offset: 0x00028EFE
		// (set) Token: 0x06000C50 RID: 3152 RVA: 0x0002AD0E File Offset: 0x00028F0E
		public bool IsSequentialLayout
		{
			get
			{
				return this.attributes.GetMaskedAttributes(24U, 8U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(24U, 8U, value);
			}
		}

		// Token: 0x17000296 RID: 662
		// (get) Token: 0x06000C51 RID: 3153 RVA: 0x0002AD25 File Offset: 0x00028F25
		// (set) Token: 0x06000C52 RID: 3154 RVA: 0x0002AD36 File Offset: 0x00028F36
		public bool IsExplicitLayout
		{
			get
			{
				return this.attributes.GetMaskedAttributes(24U, 16U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(24U, 16U, value);
			}
		}

		// Token: 0x17000297 RID: 663
		// (get) Token: 0x06000C53 RID: 3155 RVA: 0x0002AD4E File Offset: 0x00028F4E
		// (set) Token: 0x06000C54 RID: 3156 RVA: 0x0002AD5E File Offset: 0x00028F5E
		public bool IsClass
		{
			get
			{
				return this.attributes.GetMaskedAttributes(32U, 0U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(32U, 0U, value);
			}
		}

		// Token: 0x17000298 RID: 664
		// (get) Token: 0x06000C55 RID: 3157 RVA: 0x0002AD75 File Offset: 0x00028F75
		// (set) Token: 0x06000C56 RID: 3158 RVA: 0x0002AD86 File Offset: 0x00028F86
		public bool IsInterface
		{
			get
			{
				return this.attributes.GetMaskedAttributes(32U, 32U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(32U, 32U, value);
			}
		}

		// Token: 0x17000299 RID: 665
		// (get) Token: 0x06000C57 RID: 3159 RVA: 0x0002AD9E File Offset: 0x00028F9E
		// (set) Token: 0x06000C58 RID: 3160 RVA: 0x0002ADB0 File Offset: 0x00028FB0
		public bool IsAbstract
		{
			get
			{
				return this.attributes.GetAttributes(128U);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(128U, value);
			}
		}

		// Token: 0x1700029A RID: 666
		// (get) Token: 0x06000C59 RID: 3161 RVA: 0x0002ADC9 File Offset: 0x00028FC9
		// (set) Token: 0x06000C5A RID: 3162 RVA: 0x0002ADDB File Offset: 0x00028FDB
		public bool IsSealed
		{
			get
			{
				return this.attributes.GetAttributes(256U);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(256U, value);
			}
		}

		// Token: 0x1700029B RID: 667
		// (get) Token: 0x06000C5B RID: 3163 RVA: 0x0002ADF4 File Offset: 0x00028FF4
		// (set) Token: 0x06000C5C RID: 3164 RVA: 0x0002AE06 File Offset: 0x00029006
		public bool IsSpecialName
		{
			get
			{
				return this.attributes.GetAttributes(1024U);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(1024U, value);
			}
		}

		// Token: 0x1700029C RID: 668
		// (get) Token: 0x06000C5D RID: 3165 RVA: 0x0002AE1F File Offset: 0x0002901F
		// (set) Token: 0x06000C5E RID: 3166 RVA: 0x0002AE31 File Offset: 0x00029031
		public bool IsImport
		{
			get
			{
				return this.attributes.GetAttributes(4096U);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(4096U, value);
			}
		}

		// Token: 0x1700029D RID: 669
		// (get) Token: 0x06000C5F RID: 3167 RVA: 0x0002AE4A File Offset: 0x0002904A
		// (set) Token: 0x06000C60 RID: 3168 RVA: 0x0002AE5C File Offset: 0x0002905C
		public bool IsSerializable
		{
			get
			{
				return this.attributes.GetAttributes(8192U);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(8192U, value);
			}
		}

		// Token: 0x1700029E RID: 670
		// (get) Token: 0x06000C61 RID: 3169 RVA: 0x0002AE75 File Offset: 0x00029075
		// (set) Token: 0x06000C62 RID: 3170 RVA: 0x0002AE88 File Offset: 0x00029088
		public bool IsAnsiClass
		{
			get
			{
				return this.attributes.GetMaskedAttributes(196608U, 0U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(196608U, 0U, value);
			}
		}

		// Token: 0x1700029F RID: 671
		// (get) Token: 0x06000C63 RID: 3171 RVA: 0x0002AEA2 File Offset: 0x000290A2
		// (set) Token: 0x06000C64 RID: 3172 RVA: 0x0002AEB9 File Offset: 0x000290B9
		public bool IsUnicodeClass
		{
			get
			{
				return this.attributes.GetMaskedAttributes(196608U, 65536U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(196608U, 65536U, value);
			}
		}

		// Token: 0x170002A0 RID: 672
		// (get) Token: 0x06000C65 RID: 3173 RVA: 0x0002AED7 File Offset: 0x000290D7
		// (set) Token: 0x06000C66 RID: 3174 RVA: 0x0002AEEE File Offset: 0x000290EE
		public bool IsAutoClass
		{
			get
			{
				return this.attributes.GetMaskedAttributes(196608U, 131072U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(196608U, 131072U, value);
			}
		}

		// Token: 0x170002A1 RID: 673
		// (get) Token: 0x06000C67 RID: 3175 RVA: 0x0002AF0C File Offset: 0x0002910C
		// (set) Token: 0x06000C68 RID: 3176 RVA: 0x0002AF1E File Offset: 0x0002911E
		public bool IsBeforeFieldInit
		{
			get
			{
				return this.attributes.GetAttributes(1048576U);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(1048576U, value);
			}
		}

		// Token: 0x170002A2 RID: 674
		// (get) Token: 0x06000C69 RID: 3177 RVA: 0x0002AF37 File Offset: 0x00029137
		// (set) Token: 0x06000C6A RID: 3178 RVA: 0x0002AF49 File Offset: 0x00029149
		public bool IsRuntimeSpecialName
		{
			get
			{
				return this.attributes.GetAttributes(2048U);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(2048U, value);
			}
		}

		// Token: 0x170002A3 RID: 675
		// (get) Token: 0x06000C6B RID: 3179 RVA: 0x0002AF62 File Offset: 0x00029162
		// (set) Token: 0x06000C6C RID: 3180 RVA: 0x0002AF74 File Offset: 0x00029174
		public bool HasSecurity
		{
			get
			{
				return this.attributes.GetAttributes(262144U);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(262144U, value);
			}
		}

		// Token: 0x170002A4 RID: 676
		// (get) Token: 0x06000C6D RID: 3181 RVA: 0x0002AF8D File Offset: 0x0002918D
		// (set) Token: 0x06000C6E RID: 3182 RVA: 0x0002AF9F File Offset: 0x0002919F
		public bool IsForwarder
		{
			get
			{
				return this.attributes.GetAttributes(2097152U);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(2097152U, value);
			}
		}

		// Token: 0x170002A5 RID: 677
		// (get) Token: 0x06000C6F RID: 3183 RVA: 0x0002AFB8 File Offset: 0x000291B8
		public string FullName
		{
			get
			{
				string fullname = (string.IsNullOrEmpty(this.@namespace) ? this.name : (this.@namespace + "." + this.name));
				if (this.declaring_type != null)
				{
					return this.declaring_type.FullName + "/" + fullname;
				}
				return fullname;
			}
		}

		// Token: 0x06000C70 RID: 3184 RVA: 0x0002B011 File Offset: 0x00029211
		public ExportedType(string @namespace, string name, ModuleDefinition module, IMetadataScope scope)
		{
			this.@namespace = @namespace;
			this.name = name;
			this.scope = scope;
			this.module = module;
		}

		// Token: 0x06000C71 RID: 3185 RVA: 0x0002B036 File Offset: 0x00029236
		public override string ToString()
		{
			return this.FullName;
		}

		// Token: 0x06000C72 RID: 3186 RVA: 0x0002B03E File Offset: 0x0002923E
		public TypeDefinition Resolve()
		{
			return this.module.Resolve(this.CreateReference());
		}

		// Token: 0x06000C73 RID: 3187 RVA: 0x0002B051 File Offset: 0x00029251
		internal TypeReference CreateReference()
		{
			return new TypeReference(this.@namespace, this.name, this.module, this.scope)
			{
				DeclaringType = ((this.declaring_type != null) ? this.declaring_type.CreateReference() : null)
			};
		}

		// Token: 0x040003B0 RID: 944
		private string @namespace;

		// Token: 0x040003B1 RID: 945
		private string name;

		// Token: 0x040003B2 RID: 946
		private uint attributes;

		// Token: 0x040003B3 RID: 947
		private IMetadataScope scope;

		// Token: 0x040003B4 RID: 948
		private ModuleDefinition module;

		// Token: 0x040003B5 RID: 949
		private int identifier;

		// Token: 0x040003B6 RID: 950
		private ExportedType declaring_type;

		// Token: 0x040003B7 RID: 951
		internal MetadataToken token;
	}
}
