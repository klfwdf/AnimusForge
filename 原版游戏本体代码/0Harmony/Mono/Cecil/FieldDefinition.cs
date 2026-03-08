using System;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000238 RID: 568
	internal sealed class FieldDefinition : FieldReference, IMemberDefinition, ICustomAttributeProvider, IMetadataTokenProvider, IConstantProvider, IMarshalInfoProvider
	{
		// Token: 0x06000C74 RID: 3188 RVA: 0x0002B08C File Offset: 0x0002928C
		private void ResolveLayout()
		{
			if (this.offset != -2)
			{
				return;
			}
			if (!base.HasImage)
			{
				this.offset = -1;
				return;
			}
			object syncRoot = this.Module.SyncRoot;
			lock (syncRoot)
			{
				if (this.offset == -2)
				{
					this.offset = this.Module.Read<FieldDefinition, int>(this, (FieldDefinition field, MetadataReader reader) => reader.ReadFieldLayout(field));
				}
			}
		}

		// Token: 0x170002A6 RID: 678
		// (get) Token: 0x06000C75 RID: 3189 RVA: 0x0002B124 File Offset: 0x00029324
		public bool HasLayoutInfo
		{
			get
			{
				if (this.offset >= 0)
				{
					return true;
				}
				this.ResolveLayout();
				return this.offset >= 0;
			}
		}

		// Token: 0x170002A7 RID: 679
		// (get) Token: 0x06000C76 RID: 3190 RVA: 0x0002B143 File Offset: 0x00029343
		// (set) Token: 0x06000C77 RID: 3191 RVA: 0x0002B16C File Offset: 0x0002936C
		public int Offset
		{
			get
			{
				if (this.offset >= 0)
				{
					return this.offset;
				}
				this.ResolveLayout();
				if (this.offset < 0)
				{
					return -1;
				}
				return this.offset;
			}
			set
			{
				this.offset = value;
			}
		}

		// Token: 0x170002A8 RID: 680
		// (get) Token: 0x06000C78 RID: 3192 RVA: 0x0002B175 File Offset: 0x00029375
		// (set) Token: 0x06000C79 RID: 3193 RVA: 0x0002B182 File Offset: 0x00029382
		internal FieldDefinitionProjection WindowsRuntimeProjection
		{
			get
			{
				return (FieldDefinitionProjection)this.projection;
			}
			set
			{
				this.projection = value;
			}
		}

		// Token: 0x06000C7A RID: 3194 RVA: 0x0002B18C File Offset: 0x0002938C
		private void ResolveRVA()
		{
			if (this.rva != -2)
			{
				return;
			}
			if (!base.HasImage)
			{
				return;
			}
			object syncRoot = this.Module.SyncRoot;
			lock (syncRoot)
			{
				if (this.rva == -2)
				{
					this.rva = this.Module.Read<FieldDefinition, int>(this, (FieldDefinition field, MetadataReader reader) => reader.ReadFieldRVA(field));
				}
			}
		}

		// Token: 0x170002A9 RID: 681
		// (get) Token: 0x06000C7B RID: 3195 RVA: 0x0002B21C File Offset: 0x0002941C
		public int RVA
		{
			get
			{
				if (this.rva > 0)
				{
					return this.rva;
				}
				this.ResolveRVA();
				if (this.rva <= 0)
				{
					return 0;
				}
				return this.rva;
			}
		}

		// Token: 0x170002AA RID: 682
		// (get) Token: 0x06000C7C RID: 3196 RVA: 0x0002B245 File Offset: 0x00029445
		// (set) Token: 0x06000C7D RID: 3197 RVA: 0x0002B275 File Offset: 0x00029475
		public byte[] InitialValue
		{
			get
			{
				if (this.initial_value != null)
				{
					return this.initial_value;
				}
				this.ResolveRVA();
				if (this.initial_value == null)
				{
					this.initial_value = Empty<byte>.Array;
				}
				return this.initial_value;
			}
			set
			{
				this.initial_value = value;
				this.HasFieldRVA = !this.initial_value.IsNullOrEmpty<byte>();
				this.rva = 0;
			}
		}

		// Token: 0x170002AB RID: 683
		// (get) Token: 0x06000C7E RID: 3198 RVA: 0x0002B299 File Offset: 0x00029499
		// (set) Token: 0x06000C7F RID: 3199 RVA: 0x0002B2A1 File Offset: 0x000294A1
		public FieldAttributes Attributes
		{
			get
			{
				return (FieldAttributes)this.attributes;
			}
			set
			{
				if (base.IsWindowsRuntimeProjection && value != (FieldAttributes)this.attributes)
				{
					throw new InvalidOperationException();
				}
				this.attributes = (ushort)value;
			}
		}

		// Token: 0x170002AC RID: 684
		// (get) Token: 0x06000C80 RID: 3200 RVA: 0x0002B2C1 File Offset: 0x000294C1
		// (set) Token: 0x06000C81 RID: 3201 RVA: 0x0002B2E5 File Offset: 0x000294E5
		public bool HasConstant
		{
			get
			{
				this.ResolveConstant(ref this.constant, this.Module);
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

		// Token: 0x170002AD RID: 685
		// (get) Token: 0x06000C82 RID: 3202 RVA: 0x0002B2F5 File Offset: 0x000294F5
		// (set) Token: 0x06000C83 RID: 3203 RVA: 0x0002B307 File Offset: 0x00029507
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

		// Token: 0x170002AE RID: 686
		// (get) Token: 0x06000C84 RID: 3204 RVA: 0x0002B310 File Offset: 0x00029510
		public bool HasCustomAttributes
		{
			get
			{
				if (this.custom_attributes != null)
				{
					return this.custom_attributes.Count > 0;
				}
				return this.GetHasCustomAttributes(this.Module);
			}
		}

		// Token: 0x170002AF RID: 687
		// (get) Token: 0x06000C85 RID: 3205 RVA: 0x0002B335 File Offset: 0x00029535
		public Collection<CustomAttribute> CustomAttributes
		{
			get
			{
				return this.custom_attributes ?? this.GetCustomAttributes(ref this.custom_attributes, this.Module);
			}
		}

		// Token: 0x170002B0 RID: 688
		// (get) Token: 0x06000C86 RID: 3206 RVA: 0x0002B353 File Offset: 0x00029553
		public bool HasMarshalInfo
		{
			get
			{
				return this.marshal_info != null || this.GetHasMarshalInfo(this.Module);
			}
		}

		// Token: 0x170002B1 RID: 689
		// (get) Token: 0x06000C87 RID: 3207 RVA: 0x0002B36B File Offset: 0x0002956B
		// (set) Token: 0x06000C88 RID: 3208 RVA: 0x0002B389 File Offset: 0x00029589
		public MarshalInfo MarshalInfo
		{
			get
			{
				return this.marshal_info ?? this.GetMarshalInfo(ref this.marshal_info, this.Module);
			}
			set
			{
				this.marshal_info = value;
			}
		}

		// Token: 0x170002B2 RID: 690
		// (get) Token: 0x06000C89 RID: 3209 RVA: 0x0002B392 File Offset: 0x00029592
		// (set) Token: 0x06000C8A RID: 3210 RVA: 0x0002B3A1 File Offset: 0x000295A1
		public bool IsCompilerControlled
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7, 0U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7, 0U, value);
			}
		}

		// Token: 0x170002B3 RID: 691
		// (get) Token: 0x06000C8B RID: 3211 RVA: 0x0002B3B7 File Offset: 0x000295B7
		// (set) Token: 0x06000C8C RID: 3212 RVA: 0x0002B3C6 File Offset: 0x000295C6
		public bool IsPrivate
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7, 1U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7, 1U, value);
			}
		}

		// Token: 0x170002B4 RID: 692
		// (get) Token: 0x06000C8D RID: 3213 RVA: 0x0002B3DC File Offset: 0x000295DC
		// (set) Token: 0x06000C8E RID: 3214 RVA: 0x0002B3EB File Offset: 0x000295EB
		public bool IsFamilyAndAssembly
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7, 2U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7, 2U, value);
			}
		}

		// Token: 0x170002B5 RID: 693
		// (get) Token: 0x06000C8F RID: 3215 RVA: 0x0002B401 File Offset: 0x00029601
		// (set) Token: 0x06000C90 RID: 3216 RVA: 0x0002B410 File Offset: 0x00029610
		public bool IsAssembly
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7, 3U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7, 3U, value);
			}
		}

		// Token: 0x170002B6 RID: 694
		// (get) Token: 0x06000C91 RID: 3217 RVA: 0x0002B426 File Offset: 0x00029626
		// (set) Token: 0x06000C92 RID: 3218 RVA: 0x0002B435 File Offset: 0x00029635
		public bool IsFamily
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7, 4U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7, 4U, value);
			}
		}

		// Token: 0x170002B7 RID: 695
		// (get) Token: 0x06000C93 RID: 3219 RVA: 0x0002B44B File Offset: 0x0002964B
		// (set) Token: 0x06000C94 RID: 3220 RVA: 0x0002B45A File Offset: 0x0002965A
		public bool IsFamilyOrAssembly
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7, 5U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7, 5U, value);
			}
		}

		// Token: 0x170002B8 RID: 696
		// (get) Token: 0x06000C95 RID: 3221 RVA: 0x0002B470 File Offset: 0x00029670
		// (set) Token: 0x06000C96 RID: 3222 RVA: 0x0002B47F File Offset: 0x0002967F
		public bool IsPublic
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7, 6U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7, 6U, value);
			}
		}

		// Token: 0x170002B9 RID: 697
		// (get) Token: 0x06000C97 RID: 3223 RVA: 0x0002B495 File Offset: 0x00029695
		// (set) Token: 0x06000C98 RID: 3224 RVA: 0x0002B4A4 File Offset: 0x000296A4
		public bool IsStatic
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

		// Token: 0x170002BA RID: 698
		// (get) Token: 0x06000C99 RID: 3225 RVA: 0x0002B4BA File Offset: 0x000296BA
		// (set) Token: 0x06000C9A RID: 3226 RVA: 0x0002B4C9 File Offset: 0x000296C9
		public bool IsInitOnly
		{
			get
			{
				return this.attributes.GetAttributes(32);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(32, value);
			}
		}

		// Token: 0x170002BB RID: 699
		// (get) Token: 0x06000C9B RID: 3227 RVA: 0x0002B4DF File Offset: 0x000296DF
		// (set) Token: 0x06000C9C RID: 3228 RVA: 0x0002B4EE File Offset: 0x000296EE
		public bool IsLiteral
		{
			get
			{
				return this.attributes.GetAttributes(64);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(64, value);
			}
		}

		// Token: 0x170002BC RID: 700
		// (get) Token: 0x06000C9D RID: 3229 RVA: 0x0002B504 File Offset: 0x00029704
		// (set) Token: 0x06000C9E RID: 3230 RVA: 0x0002B516 File Offset: 0x00029716
		public bool IsNotSerialized
		{
			get
			{
				return this.attributes.GetAttributes(128);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(128, value);
			}
		}

		// Token: 0x170002BD RID: 701
		// (get) Token: 0x06000C9F RID: 3231 RVA: 0x0002B52F File Offset: 0x0002972F
		// (set) Token: 0x06000CA0 RID: 3232 RVA: 0x0002B541 File Offset: 0x00029741
		public bool IsSpecialName
		{
			get
			{
				return this.attributes.GetAttributes(512);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(512, value);
			}
		}

		// Token: 0x170002BE RID: 702
		// (get) Token: 0x06000CA1 RID: 3233 RVA: 0x0002B55A File Offset: 0x0002975A
		// (set) Token: 0x06000CA2 RID: 3234 RVA: 0x0002B56C File Offset: 0x0002976C
		public bool IsPInvokeImpl
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

		// Token: 0x170002BF RID: 703
		// (get) Token: 0x06000CA3 RID: 3235 RVA: 0x0002B585 File Offset: 0x00029785
		// (set) Token: 0x06000CA4 RID: 3236 RVA: 0x0002B597 File Offset: 0x00029797
		public bool IsRuntimeSpecialName
		{
			get
			{
				return this.attributes.GetAttributes(1024);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(1024, value);
			}
		}

		// Token: 0x170002C0 RID: 704
		// (get) Token: 0x06000CA5 RID: 3237 RVA: 0x0002B5B0 File Offset: 0x000297B0
		// (set) Token: 0x06000CA6 RID: 3238 RVA: 0x0002B5C2 File Offset: 0x000297C2
		public bool HasDefault
		{
			get
			{
				return this.attributes.GetAttributes(32768);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(32768, value);
			}
		}

		// Token: 0x170002C1 RID: 705
		// (get) Token: 0x06000CA7 RID: 3239 RVA: 0x0002B5DB File Offset: 0x000297DB
		// (set) Token: 0x06000CA8 RID: 3240 RVA: 0x0002B5ED File Offset: 0x000297ED
		public bool HasFieldRVA
		{
			get
			{
				return this.attributes.GetAttributes(256);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(256, value);
			}
		}

		// Token: 0x170002C2 RID: 706
		// (get) Token: 0x06000CA9 RID: 3241 RVA: 0x0001B6C7 File Offset: 0x000198C7
		public override bool IsDefinition
		{
			get
			{
				return true;
			}
		}

		// Token: 0x170002C3 RID: 707
		// (get) Token: 0x06000CAA RID: 3242 RVA: 0x0002A9E4 File Offset: 0x00028BE4
		// (set) Token: 0x06000CAB RID: 3243 RVA: 0x0002A9F1 File Offset: 0x00028BF1
		public new TypeDefinition DeclaringType
		{
			get
			{
				return (TypeDefinition)base.DeclaringType;
			}
			set
			{
				base.DeclaringType = value;
			}
		}

		// Token: 0x06000CAC RID: 3244 RVA: 0x0002B606 File Offset: 0x00029806
		public FieldDefinition(string name, FieldAttributes attributes, TypeReference fieldType)
			: base(name, fieldType)
		{
			this.attributes = (ushort)attributes;
		}

		// Token: 0x06000CAD RID: 3245 RVA: 0x0001B6A2 File Offset: 0x000198A2
		public override FieldDefinition Resolve()
		{
			return this;
		}

		// Token: 0x040003CB RID: 971
		private ushort attributes;

		// Token: 0x040003CC RID: 972
		private Collection<CustomAttribute> custom_attributes;

		// Token: 0x040003CD RID: 973
		private int offset = -2;

		// Token: 0x040003CE RID: 974
		internal int rva = -2;

		// Token: 0x040003CF RID: 975
		private byte[] initial_value;

		// Token: 0x040003D0 RID: 976
		private object constant = Mixin.NotResolved;

		// Token: 0x040003D1 RID: 977
		private MarshalInfo marshal_info;
	}
}
