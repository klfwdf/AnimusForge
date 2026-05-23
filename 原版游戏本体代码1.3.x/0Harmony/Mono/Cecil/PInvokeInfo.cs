using System;

namespace Mono.Cecil
{
	// Token: 0x0200028B RID: 651
	internal sealed class PInvokeInfo
	{
		// Token: 0x17000412 RID: 1042
		// (get) Token: 0x06001009 RID: 4105 RVA: 0x0003175C File Offset: 0x0002F95C
		// (set) Token: 0x0600100A RID: 4106 RVA: 0x00031764 File Offset: 0x0002F964
		public PInvokeAttributes Attributes
		{
			get
			{
				return (PInvokeAttributes)this.attributes;
			}
			set
			{
				this.attributes = (ushort)value;
			}
		}

		// Token: 0x17000413 RID: 1043
		// (get) Token: 0x0600100B RID: 4107 RVA: 0x0003176D File Offset: 0x0002F96D
		// (set) Token: 0x0600100C RID: 4108 RVA: 0x00031775 File Offset: 0x0002F975
		public string EntryPoint
		{
			get
			{
				return this.entry_point;
			}
			set
			{
				this.entry_point = value;
			}
		}

		// Token: 0x17000414 RID: 1044
		// (get) Token: 0x0600100D RID: 4109 RVA: 0x0003177E File Offset: 0x0002F97E
		// (set) Token: 0x0600100E RID: 4110 RVA: 0x00031786 File Offset: 0x0002F986
		public ModuleReference Module
		{
			get
			{
				return this.module;
			}
			set
			{
				this.module = value;
			}
		}

		// Token: 0x17000415 RID: 1045
		// (get) Token: 0x0600100F RID: 4111 RVA: 0x0003178F File Offset: 0x0002F98F
		// (set) Token: 0x06001010 RID: 4112 RVA: 0x0003179D File Offset: 0x0002F99D
		public bool IsNoMangle
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

		// Token: 0x17000416 RID: 1046
		// (get) Token: 0x06001011 RID: 4113 RVA: 0x000317B2 File Offset: 0x0002F9B2
		// (set) Token: 0x06001012 RID: 4114 RVA: 0x000317C1 File Offset: 0x0002F9C1
		public bool IsCharSetNotSpec
		{
			get
			{
				return this.attributes.GetMaskedAttributes(6, 0U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(6, 0U, value);
			}
		}

		// Token: 0x17000417 RID: 1047
		// (get) Token: 0x06001013 RID: 4115 RVA: 0x000317D7 File Offset: 0x0002F9D7
		// (set) Token: 0x06001014 RID: 4116 RVA: 0x000317E6 File Offset: 0x0002F9E6
		public bool IsCharSetAnsi
		{
			get
			{
				return this.attributes.GetMaskedAttributes(6, 2U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(6, 2U, value);
			}
		}

		// Token: 0x17000418 RID: 1048
		// (get) Token: 0x06001015 RID: 4117 RVA: 0x000317FC File Offset: 0x0002F9FC
		// (set) Token: 0x06001016 RID: 4118 RVA: 0x0003180B File Offset: 0x0002FA0B
		public bool IsCharSetUnicode
		{
			get
			{
				return this.attributes.GetMaskedAttributes(6, 4U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(6, 4U, value);
			}
		}

		// Token: 0x17000419 RID: 1049
		// (get) Token: 0x06001017 RID: 4119 RVA: 0x00031821 File Offset: 0x0002FA21
		// (set) Token: 0x06001018 RID: 4120 RVA: 0x00031830 File Offset: 0x0002FA30
		public bool IsCharSetAuto
		{
			get
			{
				return this.attributes.GetMaskedAttributes(6, 6U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(6, 6U, value);
			}
		}

		// Token: 0x1700041A RID: 1050
		// (get) Token: 0x06001019 RID: 4121 RVA: 0x00031846 File Offset: 0x0002FA46
		// (set) Token: 0x0600101A RID: 4122 RVA: 0x00031855 File Offset: 0x0002FA55
		public bool SupportsLastError
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

		// Token: 0x1700041B RID: 1051
		// (get) Token: 0x0600101B RID: 4123 RVA: 0x0003186B File Offset: 0x0002FA6B
		// (set) Token: 0x0600101C RID: 4124 RVA: 0x00031882 File Offset: 0x0002FA82
		public bool IsCallConvWinapi
		{
			get
			{
				return this.attributes.GetMaskedAttributes(1792, 256U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(1792, 256U, value);
			}
		}

		// Token: 0x1700041C RID: 1052
		// (get) Token: 0x0600101D RID: 4125 RVA: 0x000318A0 File Offset: 0x0002FAA0
		// (set) Token: 0x0600101E RID: 4126 RVA: 0x000318B7 File Offset: 0x0002FAB7
		public bool IsCallConvCdecl
		{
			get
			{
				return this.attributes.GetMaskedAttributes(1792, 512U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(1792, 512U, value);
			}
		}

		// Token: 0x1700041D RID: 1053
		// (get) Token: 0x0600101F RID: 4127 RVA: 0x000318D5 File Offset: 0x0002FAD5
		// (set) Token: 0x06001020 RID: 4128 RVA: 0x000318EC File Offset: 0x0002FAEC
		public bool IsCallConvStdCall
		{
			get
			{
				return this.attributes.GetMaskedAttributes(1792, 768U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(1792, 768U, value);
			}
		}

		// Token: 0x1700041E RID: 1054
		// (get) Token: 0x06001021 RID: 4129 RVA: 0x0003190A File Offset: 0x0002FB0A
		// (set) Token: 0x06001022 RID: 4130 RVA: 0x00031921 File Offset: 0x0002FB21
		public bool IsCallConvThiscall
		{
			get
			{
				return this.attributes.GetMaskedAttributes(1792, 1024U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(1792, 1024U, value);
			}
		}

		// Token: 0x1700041F RID: 1055
		// (get) Token: 0x06001023 RID: 4131 RVA: 0x0003193F File Offset: 0x0002FB3F
		// (set) Token: 0x06001024 RID: 4132 RVA: 0x00031956 File Offset: 0x0002FB56
		public bool IsCallConvFastcall
		{
			get
			{
				return this.attributes.GetMaskedAttributes(1792, 1280U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(1792, 1280U, value);
			}
		}

		// Token: 0x17000420 RID: 1056
		// (get) Token: 0x06001025 RID: 4133 RVA: 0x00031974 File Offset: 0x0002FB74
		// (set) Token: 0x06001026 RID: 4134 RVA: 0x00031985 File Offset: 0x0002FB85
		public bool IsBestFitEnabled
		{
			get
			{
				return this.attributes.GetMaskedAttributes(48, 16U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(48, 16U, value);
			}
		}

		// Token: 0x17000421 RID: 1057
		// (get) Token: 0x06001027 RID: 4135 RVA: 0x0003199D File Offset: 0x0002FB9D
		// (set) Token: 0x06001028 RID: 4136 RVA: 0x000319AE File Offset: 0x0002FBAE
		public bool IsBestFitDisabled
		{
			get
			{
				return this.attributes.GetMaskedAttributes(48, 32U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(48, 32U, value);
			}
		}

		// Token: 0x17000422 RID: 1058
		// (get) Token: 0x06001029 RID: 4137 RVA: 0x000319C6 File Offset: 0x0002FBC6
		// (set) Token: 0x0600102A RID: 4138 RVA: 0x000319DD File Offset: 0x0002FBDD
		public bool IsThrowOnUnmappableCharEnabled
		{
			get
			{
				return this.attributes.GetMaskedAttributes(12288, 4096U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(12288, 4096U, value);
			}
		}

		// Token: 0x17000423 RID: 1059
		// (get) Token: 0x0600102B RID: 4139 RVA: 0x000319FB File Offset: 0x0002FBFB
		// (set) Token: 0x0600102C RID: 4140 RVA: 0x00031A12 File Offset: 0x0002FC12
		public bool IsThrowOnUnmappableCharDisabled
		{
			get
			{
				return this.attributes.GetMaskedAttributes(12288, 8192U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(12288, 8192U, value);
			}
		}

		// Token: 0x0600102D RID: 4141 RVA: 0x00031A30 File Offset: 0x0002FC30
		public PInvokeInfo(PInvokeAttributes attributes, string entryPoint, ModuleReference module)
		{
			this.attributes = (ushort)attributes;
			this.entry_point = entryPoint;
			this.module = module;
		}

		// Token: 0x0400055D RID: 1373
		private ushort attributes;

		// Token: 0x0400055E RID: 1374
		private string entry_point;

		// Token: 0x0400055F RID: 1375
		private ModuleReference module;
	}
}
