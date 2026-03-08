using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Serialization;
using System.Security;

namespace System.Reflection
{
	// Token: 0x020005E8 RID: 1512
	[Serializable]
	internal sealed class MdFieldInfo : RuntimeFieldInfo, ISerializable
	{
		// Token: 0x0600463C RID: 17980 RVA: 0x00102380 File Offset: 0x00100580
		internal MdFieldInfo(int tkField, FieldAttributes fieldAttributes, RuntimeTypeHandle declaringTypeHandle, RuntimeType.RuntimeTypeCache reflectedTypeCache, BindingFlags bindingFlags)
			: base(reflectedTypeCache, declaringTypeHandle.GetRuntimeType(), bindingFlags)
		{
			this.m_tkField = tkField;
			this.m_name = null;
			this.m_fieldAttributes = fieldAttributes;
		}

		// Token: 0x0600463D RID: 17981 RVA: 0x001023A8 File Offset: 0x001005A8
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		internal override bool CacheEquals(object o)
		{
			MdFieldInfo mdFieldInfo = o as MdFieldInfo;
			return mdFieldInfo != null && mdFieldInfo.m_tkField == this.m_tkField && this.m_declaringType.GetTypeHandleInternal().GetModuleHandle().Equals(mdFieldInfo.m_declaringType.GetTypeHandleInternal().GetModuleHandle());
		}

		// Token: 0x17000A91 RID: 2705
		// (get) Token: 0x0600463E RID: 17982 RVA: 0x00102400 File Offset: 0x00100600
		public override string Name
		{
			[SecuritySafeCritical]
			get
			{
				if (this.m_name == null)
				{
					this.m_name = this.GetRuntimeModule().MetadataImport.GetName(this.m_tkField).ToString();
				}
				return this.m_name;
			}
		}

		// Token: 0x17000A92 RID: 2706
		// (get) Token: 0x0600463F RID: 17983 RVA: 0x00102448 File Offset: 0x00100648
		public override int MetadataToken
		{
			get
			{
				return this.m_tkField;
			}
		}

		// Token: 0x06004640 RID: 17984 RVA: 0x00102450 File Offset: 0x00100650
		internal override RuntimeModule GetRuntimeModule()
		{
			return this.m_declaringType.GetRuntimeModule();
		}

		// Token: 0x17000A93 RID: 2707
		// (get) Token: 0x06004641 RID: 17985 RVA: 0x0010245D File Offset: 0x0010065D
		public override RuntimeFieldHandle FieldHandle
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		// Token: 0x17000A94 RID: 2708
		// (get) Token: 0x06004642 RID: 17986 RVA: 0x00102464 File Offset: 0x00100664
		public override FieldAttributes Attributes
		{
			get
			{
				return this.m_fieldAttributes;
			}
		}

		// Token: 0x17000A95 RID: 2709
		// (get) Token: 0x06004643 RID: 17987 RVA: 0x0010246C File Offset: 0x0010066C
		public override bool IsSecurityCritical
		{
			get
			{
				return this.DeclaringType.IsSecurityCritical;
			}
		}

		// Token: 0x17000A96 RID: 2710
		// (get) Token: 0x06004644 RID: 17988 RVA: 0x00102479 File Offset: 0x00100679
		public override bool IsSecuritySafeCritical
		{
			get
			{
				return this.DeclaringType.IsSecuritySafeCritical;
			}
		}

		// Token: 0x17000A97 RID: 2711
		// (get) Token: 0x06004645 RID: 17989 RVA: 0x00102486 File Offset: 0x00100686
		public override bool IsSecurityTransparent
		{
			get
			{
				return this.DeclaringType.IsSecurityTransparent;
			}
		}

		// Token: 0x06004646 RID: 17990 RVA: 0x00102493 File Offset: 0x00100693
		[DebuggerStepThrough]
		[DebuggerHidden]
		public override object GetValueDirect(TypedReference obj)
		{
			return this.GetValue(null);
		}

		// Token: 0x06004647 RID: 17991 RVA: 0x0010249C File Offset: 0x0010069C
		[DebuggerStepThrough]
		[DebuggerHidden]
		public override void SetValueDirect(TypedReference obj, object value)
		{
			throw new FieldAccessException(Environment.GetResourceString("Acc_ReadOnly"));
		}

		// Token: 0x06004648 RID: 17992 RVA: 0x001024AD File Offset: 0x001006AD
		[DebuggerStepThrough]
		[DebuggerHidden]
		public override object GetValue(object obj)
		{
			return this.GetValue(false);
		}

		// Token: 0x06004649 RID: 17993 RVA: 0x001024B6 File Offset: 0x001006B6
		public override object GetRawConstantValue()
		{
			return this.GetValue(true);
		}

		// Token: 0x0600464A RID: 17994 RVA: 0x001024C0 File Offset: 0x001006C0
		[SecuritySafeCritical]
		private object GetValue(bool raw)
		{
			object value = MdConstant.GetValue(this.GetRuntimeModule().MetadataImport, this.m_tkField, this.FieldType.GetTypeHandleInternal(), raw);
			if (value == DBNull.Value)
			{
				throw new NotSupportedException(Environment.GetResourceString("Arg_EnumLitValueNotFound"));
			}
			return value;
		}

		// Token: 0x0600464B RID: 17995 RVA: 0x00102509 File Offset: 0x00100709
		[DebuggerStepThrough]
		[DebuggerHidden]
		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
		{
			throw new FieldAccessException(Environment.GetResourceString("Acc_ReadOnly"));
		}

		// Token: 0x17000A98 RID: 2712
		// (get) Token: 0x0600464C RID: 17996 RVA: 0x0010251C File Offset: 0x0010071C
		public override Type FieldType
		{
			[SecuritySafeCritical]
			get
			{
				if (this.m_fieldType == null)
				{
					ConstArray sigOfFieldDef = this.GetRuntimeModule().MetadataImport.GetSigOfFieldDef(this.m_tkField);
					this.m_fieldType = new Signature(sigOfFieldDef.Signature.ToPointer(), sigOfFieldDef.Length, this.m_declaringType).FieldType;
				}
				return this.m_fieldType;
			}
		}

		// Token: 0x0600464D RID: 17997 RVA: 0x00102583 File Offset: 0x00100783
		public override Type[] GetRequiredCustomModifiers()
		{
			return EmptyArray<Type>.Value;
		}

		// Token: 0x0600464E RID: 17998 RVA: 0x0010258A File Offset: 0x0010078A
		public override Type[] GetOptionalCustomModifiers()
		{
			return EmptyArray<Type>.Value;
		}

		// Token: 0x04001CC2 RID: 7362
		private int m_tkField;

		// Token: 0x04001CC3 RID: 7363
		private string m_name;

		// Token: 0x04001CC4 RID: 7364
		private RuntimeType m_fieldType;

		// Token: 0x04001CC5 RID: 7365
		private FieldAttributes m_fieldAttributes;
	}
}
