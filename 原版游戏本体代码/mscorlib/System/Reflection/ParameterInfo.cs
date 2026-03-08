using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace System.Reflection
{
	// Token: 0x02000616 RID: 1558
	[ClassInterface(ClassInterfaceType.None)]
	[ComDefaultInterface(typeof(_ParameterInfo))]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class ParameterInfo : _ParameterInfo, ICustomAttributeProvider, IObjectReference
	{
		// Token: 0x06004811 RID: 18449 RVA: 0x001061A6 File Offset: 0x001043A6
		protected ParameterInfo()
		{
		}

		// Token: 0x06004812 RID: 18450 RVA: 0x001061AE File Offset: 0x001043AE
		internal void SetName(string name)
		{
			this.NameImpl = name;
		}

		// Token: 0x06004813 RID: 18451 RVA: 0x001061B7 File Offset: 0x001043B7
		internal void SetAttributes(ParameterAttributes attributes)
		{
			this.AttrsImpl = attributes;
		}

		// Token: 0x17000B25 RID: 2853
		// (get) Token: 0x06004814 RID: 18452 RVA: 0x001061C0 File Offset: 0x001043C0
		[__DynamicallyInvokable]
		public virtual Type ParameterType
		{
			[__DynamicallyInvokable]
			get
			{
				return this.ClassImpl;
			}
		}

		// Token: 0x17000B26 RID: 2854
		// (get) Token: 0x06004815 RID: 18453 RVA: 0x001061C8 File Offset: 0x001043C8
		[__DynamicallyInvokable]
		public virtual string Name
		{
			[__DynamicallyInvokable]
			get
			{
				return this.NameImpl;
			}
		}

		// Token: 0x17000B27 RID: 2855
		// (get) Token: 0x06004816 RID: 18454 RVA: 0x001061D0 File Offset: 0x001043D0
		[__DynamicallyInvokable]
		public virtual bool HasDefaultValue
		{
			[__DynamicallyInvokable]
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x17000B28 RID: 2856
		// (get) Token: 0x06004817 RID: 18455 RVA: 0x001061D7 File Offset: 0x001043D7
		[__DynamicallyInvokable]
		public virtual object DefaultValue
		{
			[__DynamicallyInvokable]
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x17000B29 RID: 2857
		// (get) Token: 0x06004818 RID: 18456 RVA: 0x001061DE File Offset: 0x001043DE
		public virtual object RawDefaultValue
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x17000B2A RID: 2858
		// (get) Token: 0x06004819 RID: 18457 RVA: 0x001061E5 File Offset: 0x001043E5
		[__DynamicallyInvokable]
		public virtual int Position
		{
			[__DynamicallyInvokable]
			get
			{
				return this.PositionImpl;
			}
		}

		// Token: 0x17000B2B RID: 2859
		// (get) Token: 0x0600481A RID: 18458 RVA: 0x001061ED File Offset: 0x001043ED
		[__DynamicallyInvokable]
		public virtual ParameterAttributes Attributes
		{
			[__DynamicallyInvokable]
			get
			{
				return this.AttrsImpl;
			}
		}

		// Token: 0x17000B2C RID: 2860
		// (get) Token: 0x0600481B RID: 18459 RVA: 0x001061F5 File Offset: 0x001043F5
		[__DynamicallyInvokable]
		public virtual MemberInfo Member
		{
			[__DynamicallyInvokable]
			get
			{
				return this.MemberImpl;
			}
		}

		// Token: 0x17000B2D RID: 2861
		// (get) Token: 0x0600481C RID: 18460 RVA: 0x001061FD File Offset: 0x001043FD
		[__DynamicallyInvokable]
		public bool IsIn
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & ParameterAttributes.In) > ParameterAttributes.None;
			}
		}

		// Token: 0x17000B2E RID: 2862
		// (get) Token: 0x0600481D RID: 18461 RVA: 0x0010620A File Offset: 0x0010440A
		[__DynamicallyInvokable]
		public bool IsOut
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & ParameterAttributes.Out) > ParameterAttributes.None;
			}
		}

		// Token: 0x17000B2F RID: 2863
		// (get) Token: 0x0600481E RID: 18462 RVA: 0x00106217 File Offset: 0x00104417
		[__DynamicallyInvokable]
		public bool IsLcid
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & ParameterAttributes.Lcid) > ParameterAttributes.None;
			}
		}

		// Token: 0x17000B30 RID: 2864
		// (get) Token: 0x0600481F RID: 18463 RVA: 0x00106224 File Offset: 0x00104424
		[__DynamicallyInvokable]
		public bool IsRetval
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & ParameterAttributes.Retval) > ParameterAttributes.None;
			}
		}

		// Token: 0x17000B31 RID: 2865
		// (get) Token: 0x06004820 RID: 18464 RVA: 0x00106231 File Offset: 0x00104431
		[__DynamicallyInvokable]
		public bool IsOptional
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & ParameterAttributes.Optional) > ParameterAttributes.None;
			}
		}

		// Token: 0x17000B32 RID: 2866
		// (get) Token: 0x06004821 RID: 18465 RVA: 0x00106240 File Offset: 0x00104440
		public virtual int MetadataToken
		{
			get
			{
				RuntimeParameterInfo runtimeParameterInfo = this as RuntimeParameterInfo;
				if (runtimeParameterInfo != null)
				{
					return runtimeParameterInfo.MetadataToken;
				}
				return 134217728;
			}
		}

		// Token: 0x06004822 RID: 18466 RVA: 0x00106263 File Offset: 0x00104463
		public virtual Type[] GetRequiredCustomModifiers()
		{
			return EmptyArray<Type>.Value;
		}

		// Token: 0x06004823 RID: 18467 RVA: 0x0010626A File Offset: 0x0010446A
		public virtual Type[] GetOptionalCustomModifiers()
		{
			return EmptyArray<Type>.Value;
		}

		// Token: 0x06004824 RID: 18468 RVA: 0x00106271 File Offset: 0x00104471
		[__DynamicallyInvokable]
		public override string ToString()
		{
			return this.ParameterType.FormatTypeName() + " " + this.Name;
		}

		// Token: 0x17000B33 RID: 2867
		// (get) Token: 0x06004825 RID: 18469 RVA: 0x0010628E File Offset: 0x0010448E
		[__DynamicallyInvokable]
		public virtual IEnumerable<CustomAttributeData> CustomAttributes
		{
			[__DynamicallyInvokable]
			get
			{
				return this.GetCustomAttributesData();
			}
		}

		// Token: 0x06004826 RID: 18470 RVA: 0x00106296 File Offset: 0x00104496
		[__DynamicallyInvokable]
		public virtual object[] GetCustomAttributes(bool inherit)
		{
			return EmptyArray<object>.Value;
		}

		// Token: 0x06004827 RID: 18471 RVA: 0x0010629D File Offset: 0x0010449D
		[__DynamicallyInvokable]
		public virtual object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			if (attributeType == null)
			{
				throw new ArgumentNullException("attributeType");
			}
			return EmptyArray<object>.Value;
		}

		// Token: 0x06004828 RID: 18472 RVA: 0x001062B8 File Offset: 0x001044B8
		[__DynamicallyInvokable]
		public virtual bool IsDefined(Type attributeType, bool inherit)
		{
			if (attributeType == null)
			{
				throw new ArgumentNullException("attributeType");
			}
			return false;
		}

		// Token: 0x06004829 RID: 18473 RVA: 0x001062CF File Offset: 0x001044CF
		public virtual IList<CustomAttributeData> GetCustomAttributesData()
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600482A RID: 18474 RVA: 0x001062D6 File Offset: 0x001044D6
		void _ParameterInfo.GetTypeInfoCount(out uint pcTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600482B RID: 18475 RVA: 0x001062DD File Offset: 0x001044DD
		void _ParameterInfo.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600482C RID: 18476 RVA: 0x001062E4 File Offset: 0x001044E4
		void _ParameterInfo.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600482D RID: 18477 RVA: 0x001062EB File Offset: 0x001044EB
		void _ParameterInfo.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600482E RID: 18478 RVA: 0x001062F4 File Offset: 0x001044F4
		[SecurityCritical]
		public object GetRealObject(StreamingContext context)
		{
			if (this.MemberImpl == null)
			{
				throw new SerializationException(Environment.GetResourceString("Serialization_InsufficientState"));
			}
			MemberTypes memberType = this.MemberImpl.MemberType;
			if (memberType != MemberTypes.Constructor && memberType != MemberTypes.Method)
			{
				if (memberType != MemberTypes.Property)
				{
					throw new SerializationException(Environment.GetResourceString("Serialization_NoParameterInfo"));
				}
				ParameterInfo[] array = ((RuntimePropertyInfo)this.MemberImpl).GetIndexParametersNoCopy();
				if (array != null && this.PositionImpl > -1 && this.PositionImpl < array.Length)
				{
					return array[this.PositionImpl];
				}
				throw new SerializationException(Environment.GetResourceString("Serialization_BadParameterInfo"));
			}
			else if (this.PositionImpl == -1)
			{
				if (this.MemberImpl.MemberType == MemberTypes.Method)
				{
					return ((MethodInfo)this.MemberImpl).ReturnParameter;
				}
				throw new SerializationException(Environment.GetResourceString("Serialization_BadParameterInfo"));
			}
			else
			{
				ParameterInfo[] array = ((MethodBase)this.MemberImpl).GetParametersNoCopy();
				if (array != null && this.PositionImpl < array.Length)
				{
					return array[this.PositionImpl];
				}
				throw new SerializationException(Environment.GetResourceString("Serialization_BadParameterInfo"));
			}
		}

		// Token: 0x04001DED RID: 7661
		protected string NameImpl;

		// Token: 0x04001DEE RID: 7662
		protected Type ClassImpl;

		// Token: 0x04001DEF RID: 7663
		protected int PositionImpl;

		// Token: 0x04001DF0 RID: 7664
		protected ParameterAttributes AttrsImpl;

		// Token: 0x04001DF1 RID: 7665
		protected object DefaultValueImpl;

		// Token: 0x04001DF2 RID: 7666
		protected MemberInfo MemberImpl;

		// Token: 0x04001DF3 RID: 7667
		[OptionalField]
		private IntPtr _importer;

		// Token: 0x04001DF4 RID: 7668
		[OptionalField]
		private int _token;

		// Token: 0x04001DF5 RID: 7669
		[OptionalField]
		private bool bExtraConstChecked;
	}
}
