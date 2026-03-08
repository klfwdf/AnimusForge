using System;
using System.Globalization;

namespace System.Reflection
{
	// Token: 0x020005FC RID: 1532
	[Serializable]
	internal struct MetadataToken
	{
		// Token: 0x0600467D RID: 18045 RVA: 0x00102A56 File Offset: 0x00100C56
		public static implicit operator int(MetadataToken token)
		{
			return token.Value;
		}

		// Token: 0x0600467E RID: 18046 RVA: 0x00102A5E File Offset: 0x00100C5E
		public static implicit operator MetadataToken(int token)
		{
			return new MetadataToken(token);
		}

		// Token: 0x0600467F RID: 18047 RVA: 0x00102A68 File Offset: 0x00100C68
		public static bool IsTokenOfType(int token, params MetadataTokenType[] types)
		{
			for (int i = 0; i < types.Length; i++)
			{
				if ((MetadataTokenType)((long)token & (long)((ulong)(-16777216))) == types[i])
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06004680 RID: 18048 RVA: 0x00102A95 File Offset: 0x00100C95
		public static bool IsNullToken(int token)
		{
			return (token & 16777215) == 0;
		}

		// Token: 0x06004681 RID: 18049 RVA: 0x00102AA1 File Offset: 0x00100CA1
		public MetadataToken(int token)
		{
			this.Value = token;
		}

		// Token: 0x17000AA0 RID: 2720
		// (get) Token: 0x06004682 RID: 18050 RVA: 0x00102AAA File Offset: 0x00100CAA
		public bool IsGlobalTypeDefToken
		{
			get
			{
				return this.Value == 33554433;
			}
		}

		// Token: 0x17000AA1 RID: 2721
		// (get) Token: 0x06004683 RID: 18051 RVA: 0x00102AB9 File Offset: 0x00100CB9
		public MetadataTokenType TokenType
		{
			get
			{
				return (MetadataTokenType)((long)this.Value & (long)((ulong)(-16777216)));
			}
		}

		// Token: 0x17000AA2 RID: 2722
		// (get) Token: 0x06004684 RID: 18052 RVA: 0x00102ACA File Offset: 0x00100CCA
		public bool IsTypeRef
		{
			get
			{
				return this.TokenType == MetadataTokenType.TypeRef;
			}
		}

		// Token: 0x17000AA3 RID: 2723
		// (get) Token: 0x06004685 RID: 18053 RVA: 0x00102AD9 File Offset: 0x00100CD9
		public bool IsTypeDef
		{
			get
			{
				return this.TokenType == MetadataTokenType.TypeDef;
			}
		}

		// Token: 0x17000AA4 RID: 2724
		// (get) Token: 0x06004686 RID: 18054 RVA: 0x00102AE8 File Offset: 0x00100CE8
		public bool IsFieldDef
		{
			get
			{
				return this.TokenType == MetadataTokenType.FieldDef;
			}
		}

		// Token: 0x17000AA5 RID: 2725
		// (get) Token: 0x06004687 RID: 18055 RVA: 0x00102AF7 File Offset: 0x00100CF7
		public bool IsMethodDef
		{
			get
			{
				return this.TokenType == MetadataTokenType.MethodDef;
			}
		}

		// Token: 0x17000AA6 RID: 2726
		// (get) Token: 0x06004688 RID: 18056 RVA: 0x00102B06 File Offset: 0x00100D06
		public bool IsMemberRef
		{
			get
			{
				return this.TokenType == MetadataTokenType.MemberRef;
			}
		}

		// Token: 0x17000AA7 RID: 2727
		// (get) Token: 0x06004689 RID: 18057 RVA: 0x00102B15 File Offset: 0x00100D15
		public bool IsEvent
		{
			get
			{
				return this.TokenType == MetadataTokenType.Event;
			}
		}

		// Token: 0x17000AA8 RID: 2728
		// (get) Token: 0x0600468A RID: 18058 RVA: 0x00102B24 File Offset: 0x00100D24
		public bool IsProperty
		{
			get
			{
				return this.TokenType == MetadataTokenType.Property;
			}
		}

		// Token: 0x17000AA9 RID: 2729
		// (get) Token: 0x0600468B RID: 18059 RVA: 0x00102B33 File Offset: 0x00100D33
		public bool IsParamDef
		{
			get
			{
				return this.TokenType == MetadataTokenType.ParamDef;
			}
		}

		// Token: 0x17000AAA RID: 2730
		// (get) Token: 0x0600468C RID: 18060 RVA: 0x00102B42 File Offset: 0x00100D42
		public bool IsTypeSpec
		{
			get
			{
				return this.TokenType == MetadataTokenType.TypeSpec;
			}
		}

		// Token: 0x17000AAB RID: 2731
		// (get) Token: 0x0600468D RID: 18061 RVA: 0x00102B51 File Offset: 0x00100D51
		public bool IsMethodSpec
		{
			get
			{
				return this.TokenType == MetadataTokenType.MethodSpec;
			}
		}

		// Token: 0x17000AAC RID: 2732
		// (get) Token: 0x0600468E RID: 18062 RVA: 0x00102B60 File Offset: 0x00100D60
		public bool IsString
		{
			get
			{
				return this.TokenType == MetadataTokenType.String;
			}
		}

		// Token: 0x17000AAD RID: 2733
		// (get) Token: 0x0600468F RID: 18063 RVA: 0x00102B6F File Offset: 0x00100D6F
		public bool IsSignature
		{
			get
			{
				return this.TokenType == MetadataTokenType.Signature;
			}
		}

		// Token: 0x17000AAE RID: 2734
		// (get) Token: 0x06004690 RID: 18064 RVA: 0x00102B7E File Offset: 0x00100D7E
		public bool IsModule
		{
			get
			{
				return this.TokenType == MetadataTokenType.Module;
			}
		}

		// Token: 0x17000AAF RID: 2735
		// (get) Token: 0x06004691 RID: 18065 RVA: 0x00102B89 File Offset: 0x00100D89
		public bool IsAssembly
		{
			get
			{
				return this.TokenType == MetadataTokenType.Assembly;
			}
		}

		// Token: 0x17000AB0 RID: 2736
		// (get) Token: 0x06004692 RID: 18066 RVA: 0x00102B98 File Offset: 0x00100D98
		public bool IsGenericPar
		{
			get
			{
				return this.TokenType == MetadataTokenType.GenericPar;
			}
		}

		// Token: 0x06004693 RID: 18067 RVA: 0x00102BA7 File Offset: 0x00100DA7
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "0x{0:x8}", this.Value);
		}

		// Token: 0x04001D50 RID: 7504
		public int Value;
	}
}
