using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.Remoting.Metadata;
using System.Security;
using System.Threading;

namespace System.Runtime.Serialization
{
	// Token: 0x02000740 RID: 1856
	internal sealed class SerializationFieldInfo : FieldInfo
	{
		// Token: 0x17000D75 RID: 3445
		// (get) Token: 0x060051D9 RID: 20953 RVA: 0x0011FF07 File Offset: 0x0011E107
		public override Module Module
		{
			get
			{
				return this.m_field.Module;
			}
		}

		// Token: 0x17000D76 RID: 3446
		// (get) Token: 0x060051DA RID: 20954 RVA: 0x0011FF14 File Offset: 0x0011E114
		public override int MetadataToken
		{
			get
			{
				return this.m_field.MetadataToken;
			}
		}

		// Token: 0x060051DB RID: 20955 RVA: 0x0011FF21 File Offset: 0x0011E121
		internal SerializationFieldInfo(RuntimeFieldInfo field, string namePrefix)
		{
			this.m_field = field;
			this.m_serializationName = namePrefix + "+" + this.m_field.Name;
		}

		// Token: 0x17000D77 RID: 3447
		// (get) Token: 0x060051DC RID: 20956 RVA: 0x0011FF4C File Offset: 0x0011E14C
		public override string Name
		{
			get
			{
				return this.m_serializationName;
			}
		}

		// Token: 0x17000D78 RID: 3448
		// (get) Token: 0x060051DD RID: 20957 RVA: 0x0011FF54 File Offset: 0x0011E154
		public override Type DeclaringType
		{
			get
			{
				return this.m_field.DeclaringType;
			}
		}

		// Token: 0x17000D79 RID: 3449
		// (get) Token: 0x060051DE RID: 20958 RVA: 0x0011FF61 File Offset: 0x0011E161
		public override Type ReflectedType
		{
			get
			{
				return this.m_field.ReflectedType;
			}
		}

		// Token: 0x060051DF RID: 20959 RVA: 0x0011FF6E File Offset: 0x0011E16E
		public override object[] GetCustomAttributes(bool inherit)
		{
			return this.m_field.GetCustomAttributes(inherit);
		}

		// Token: 0x060051E0 RID: 20960 RVA: 0x0011FF7C File Offset: 0x0011E17C
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			return this.m_field.GetCustomAttributes(attributeType, inherit);
		}

		// Token: 0x060051E1 RID: 20961 RVA: 0x0011FF8B File Offset: 0x0011E18B
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			return this.m_field.IsDefined(attributeType, inherit);
		}

		// Token: 0x17000D7A RID: 3450
		// (get) Token: 0x060051E2 RID: 20962 RVA: 0x0011FF9A File Offset: 0x0011E19A
		public override Type FieldType
		{
			get
			{
				return this.m_field.FieldType;
			}
		}

		// Token: 0x060051E3 RID: 20963 RVA: 0x0011FFA7 File Offset: 0x0011E1A7
		public override object GetValue(object obj)
		{
			return this.m_field.GetValue(obj);
		}

		// Token: 0x060051E4 RID: 20964 RVA: 0x0011FFB8 File Offset: 0x0011E1B8
		[SecurityCritical]
		internal object InternalGetValue(object obj)
		{
			RtFieldInfo rtFieldInfo = this.m_field as RtFieldInfo;
			if (rtFieldInfo != null)
			{
				rtFieldInfo.CheckConsistency(obj);
				return rtFieldInfo.UnsafeGetValue(obj);
			}
			return this.m_field.GetValue(obj);
		}

		// Token: 0x060051E5 RID: 20965 RVA: 0x0011FFF5 File Offset: 0x0011E1F5
		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
		{
			this.m_field.SetValue(obj, value, invokeAttr, binder, culture);
		}

		// Token: 0x060051E6 RID: 20966 RVA: 0x0012000C File Offset: 0x0011E20C
		[SecurityCritical]
		internal void InternalSetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
		{
			RtFieldInfo rtFieldInfo = this.m_field as RtFieldInfo;
			if (rtFieldInfo != null)
			{
				rtFieldInfo.CheckConsistency(obj);
				rtFieldInfo.UnsafeSetValue(obj, value, invokeAttr, binder, culture);
				return;
			}
			this.m_field.SetValue(obj, value, invokeAttr, binder, culture);
		}

		// Token: 0x17000D7B RID: 3451
		// (get) Token: 0x060051E7 RID: 20967 RVA: 0x00120055 File Offset: 0x0011E255
		internal RuntimeFieldInfo FieldInfo
		{
			get
			{
				return this.m_field;
			}
		}

		// Token: 0x17000D7C RID: 3452
		// (get) Token: 0x060051E8 RID: 20968 RVA: 0x0012005D File Offset: 0x0011E25D
		public override RuntimeFieldHandle FieldHandle
		{
			get
			{
				return this.m_field.FieldHandle;
			}
		}

		// Token: 0x17000D7D RID: 3453
		// (get) Token: 0x060051E9 RID: 20969 RVA: 0x0012006A File Offset: 0x0011E26A
		public override FieldAttributes Attributes
		{
			get
			{
				return this.m_field.Attributes;
			}
		}

		// Token: 0x17000D7E RID: 3454
		// (get) Token: 0x060051EA RID: 20970 RVA: 0x00120078 File Offset: 0x0011E278
		internal RemotingFieldCachedData RemotingCache
		{
			get
			{
				RemotingFieldCachedData remotingFieldCachedData = this.m_cachedData;
				if (remotingFieldCachedData == null)
				{
					remotingFieldCachedData = new RemotingFieldCachedData(this);
					RemotingFieldCachedData remotingFieldCachedData2 = Interlocked.CompareExchange<RemotingFieldCachedData>(ref this.m_cachedData, remotingFieldCachedData, null);
					if (remotingFieldCachedData2 != null)
					{
						remotingFieldCachedData = remotingFieldCachedData2;
					}
				}
				return remotingFieldCachedData;
			}
		}

		// Token: 0x04002446 RID: 9286
		internal const string FakeNameSeparatorString = "+";

		// Token: 0x04002447 RID: 9287
		private RuntimeFieldInfo m_field;

		// Token: 0x04002448 RID: 9288
		private string m_serializationName;

		// Token: 0x04002449 RID: 9289
		private RemotingFieldCachedData m_cachedData;
	}
}
