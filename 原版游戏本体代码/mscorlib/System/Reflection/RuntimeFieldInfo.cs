using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Metadata;
using System.Runtime.Serialization;
using System.Security;
using System.Threading;

namespace System.Reflection
{
	// Token: 0x020005E6 RID: 1510
	[Serializable]
	internal abstract class RuntimeFieldInfo : FieldInfo, ISerializable
	{
		// Token: 0x0600460F RID: 17935 RVA: 0x00101A7B File Offset: 0x000FFC7B
		protected RuntimeFieldInfo()
		{
		}

		// Token: 0x06004610 RID: 17936 RVA: 0x00101A83 File Offset: 0x000FFC83
		protected RuntimeFieldInfo(RuntimeType.RuntimeTypeCache reflectedTypeCache, RuntimeType declaringType, BindingFlags bindingFlags)
		{
			this.m_bindingFlags = bindingFlags;
			this.m_declaringType = declaringType;
			this.m_reflectedTypeCache = reflectedTypeCache;
		}

		// Token: 0x17000A82 RID: 2690
		// (get) Token: 0x06004611 RID: 17937 RVA: 0x00101AA0 File Offset: 0x000FFCA0
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

		// Token: 0x17000A83 RID: 2691
		// (get) Token: 0x06004612 RID: 17938 RVA: 0x00101AD2 File Offset: 0x000FFCD2
		internal BindingFlags BindingFlags
		{
			get
			{
				return this.m_bindingFlags;
			}
		}

		// Token: 0x17000A84 RID: 2692
		// (get) Token: 0x06004613 RID: 17939 RVA: 0x00101ADA File Offset: 0x000FFCDA
		private RuntimeType ReflectedTypeInternal
		{
			get
			{
				return this.m_reflectedTypeCache.GetRuntimeType();
			}
		}

		// Token: 0x06004614 RID: 17940 RVA: 0x00101AE7 File Offset: 0x000FFCE7
		internal RuntimeType GetDeclaringTypeInternal()
		{
			return this.m_declaringType;
		}

		// Token: 0x06004615 RID: 17941 RVA: 0x00101AEF File Offset: 0x000FFCEF
		internal RuntimeType GetRuntimeType()
		{
			return this.m_declaringType;
		}

		// Token: 0x06004616 RID: 17942
		internal abstract RuntimeModule GetRuntimeModule();

		// Token: 0x17000A85 RID: 2693
		// (get) Token: 0x06004617 RID: 17943 RVA: 0x00101AF7 File Offset: 0x000FFCF7
		public override MemberTypes MemberType
		{
			get
			{
				return MemberTypes.Field;
			}
		}

		// Token: 0x17000A86 RID: 2694
		// (get) Token: 0x06004618 RID: 17944 RVA: 0x00101AFA File Offset: 0x000FFCFA
		public override Type ReflectedType
		{
			get
			{
				if (!this.m_reflectedTypeCache.IsGlobal)
				{
					return this.ReflectedTypeInternal;
				}
				return null;
			}
		}

		// Token: 0x17000A87 RID: 2695
		// (get) Token: 0x06004619 RID: 17945 RVA: 0x00101B11 File Offset: 0x000FFD11
		public override Type DeclaringType
		{
			get
			{
				if (!this.m_reflectedTypeCache.IsGlobal)
				{
					return this.m_declaringType;
				}
				return null;
			}
		}

		// Token: 0x17000A88 RID: 2696
		// (get) Token: 0x0600461A RID: 17946 RVA: 0x00101B28 File Offset: 0x000FFD28
		public override Module Module
		{
			get
			{
				return this.GetRuntimeModule();
			}
		}

		// Token: 0x0600461B RID: 17947 RVA: 0x00101B30 File Offset: 0x000FFD30
		public override string ToString()
		{
			if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
			{
				return this.FieldType.ToString() + " " + this.Name;
			}
			return this.FieldType.FormatTypeName() + " " + this.Name;
		}

		// Token: 0x0600461C RID: 17948 RVA: 0x00101B70 File Offset: 0x000FFD70
		public override object[] GetCustomAttributes(bool inherit)
		{
			return CustomAttribute.GetCustomAttributes(this, typeof(object) as RuntimeType);
		}

		// Token: 0x0600461D RID: 17949 RVA: 0x00101B88 File Offset: 0x000FFD88
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			if (attributeType == null)
			{
				throw new ArgumentNullException("attributeType");
			}
			RuntimeType runtimeType = attributeType.UnderlyingSystemType as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "attributeType");
			}
			return CustomAttribute.GetCustomAttributes(this, runtimeType);
		}

		// Token: 0x0600461E RID: 17950 RVA: 0x00101BDC File Offset: 0x000FFDDC
		[SecuritySafeCritical]
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			if (attributeType == null)
			{
				throw new ArgumentNullException("attributeType");
			}
			RuntimeType runtimeType = attributeType.UnderlyingSystemType as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "attributeType");
			}
			return CustomAttribute.IsDefined(this, runtimeType);
		}

		// Token: 0x0600461F RID: 17951 RVA: 0x00101C2E File Offset: 0x000FFE2E
		public override IList<CustomAttributeData> GetCustomAttributesData()
		{
			return CustomAttributeData.GetCustomAttributesInternal(this);
		}

		// Token: 0x06004620 RID: 17952 RVA: 0x00101C36 File Offset: 0x000FFE36
		[SecurityCritical]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			MemberInfoSerializationHolder.GetSerializationInfo(info, this.Name, this.ReflectedTypeInternal, this.ToString(), MemberTypes.Field);
		}

		// Token: 0x04001CB9 RID: 7353
		private BindingFlags m_bindingFlags;

		// Token: 0x04001CBA RID: 7354
		protected RuntimeType.RuntimeTypeCache m_reflectedTypeCache;

		// Token: 0x04001CBB RID: 7355
		protected RuntimeType m_declaringType;

		// Token: 0x04001CBC RID: 7356
		private RemotingFieldCachedData m_cachedData;
	}
}
