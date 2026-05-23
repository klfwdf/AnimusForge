using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace System.Reflection
{
	// Token: 0x02000601 RID: 1537
	[ClassInterface(ClassInterfaceType.None)]
	[ComDefaultInterface(typeof(_MemberInfo))]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
	[Serializable]
	public abstract class MemberInfo : ICustomAttributeProvider, _MemberInfo
	{
		// Token: 0x060046D8 RID: 18136 RVA: 0x00103039 File Offset: 0x00101239
		internal virtual bool CacheEquals(object o)
		{
			throw new NotImplementedException();
		}

		// Token: 0x17000AB3 RID: 2739
		// (get) Token: 0x060046D9 RID: 18137
		public abstract MemberTypes MemberType { get; }

		// Token: 0x17000AB4 RID: 2740
		// (get) Token: 0x060046DA RID: 18138
		[__DynamicallyInvokable]
		public abstract string Name
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x17000AB5 RID: 2741
		// (get) Token: 0x060046DB RID: 18139
		[__DynamicallyInvokable]
		public abstract Type DeclaringType
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x17000AB6 RID: 2742
		// (get) Token: 0x060046DC RID: 18140
		[__DynamicallyInvokable]
		public abstract Type ReflectedType
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x17000AB7 RID: 2743
		// (get) Token: 0x060046DD RID: 18141 RVA: 0x00103040 File Offset: 0x00101240
		[__DynamicallyInvokable]
		public virtual IEnumerable<CustomAttributeData> CustomAttributes
		{
			[__DynamicallyInvokable]
			get
			{
				return this.GetCustomAttributesData();
			}
		}

		// Token: 0x060046DE RID: 18142
		[__DynamicallyInvokable]
		public abstract object[] GetCustomAttributes(bool inherit);

		// Token: 0x060046DF RID: 18143
		[__DynamicallyInvokable]
		public abstract object[] GetCustomAttributes(Type attributeType, bool inherit);

		// Token: 0x060046E0 RID: 18144
		[__DynamicallyInvokable]
		public abstract bool IsDefined(Type attributeType, bool inherit);

		// Token: 0x060046E1 RID: 18145 RVA: 0x00103048 File Offset: 0x00101248
		public virtual IList<CustomAttributeData> GetCustomAttributesData()
		{
			throw new NotImplementedException();
		}

		// Token: 0x17000AB8 RID: 2744
		// (get) Token: 0x060046E2 RID: 18146 RVA: 0x0010304F File Offset: 0x0010124F
		public virtual int MetadataToken
		{
			get
			{
				throw new InvalidOperationException();
			}
		}

		// Token: 0x17000AB9 RID: 2745
		// (get) Token: 0x060046E3 RID: 18147 RVA: 0x00103056 File Offset: 0x00101256
		[__DynamicallyInvokable]
		public virtual Module Module
		{
			[__DynamicallyInvokable]
			get
			{
				if (this is Type)
				{
					return ((Type)this).Module;
				}
				throw new NotImplementedException();
			}
		}

		// Token: 0x060046E4 RID: 18148 RVA: 0x00103074 File Offset: 0x00101274
		[__DynamicallyInvokable]
		public static bool operator ==(MemberInfo left, MemberInfo right)
		{
			if (left == right)
			{
				return true;
			}
			if (left == null || right == null)
			{
				return false;
			}
			Type left2;
			Type right2;
			if ((left2 = left as Type) != null && (right2 = right as Type) != null)
			{
				return left2 == right2;
			}
			MethodBase left3;
			MethodBase right3;
			if ((left3 = left as MethodBase) != null && (right3 = right as MethodBase) != null)
			{
				return left3 == right3;
			}
			FieldInfo left4;
			FieldInfo right4;
			if ((left4 = left as FieldInfo) != null && (right4 = right as FieldInfo) != null)
			{
				return left4 == right4;
			}
			EventInfo left5;
			EventInfo right5;
			if ((left5 = left as EventInfo) != null && (right5 = right as EventInfo) != null)
			{
				return left5 == right5;
			}
			PropertyInfo left6;
			PropertyInfo right6;
			return (left6 = left as PropertyInfo) != null && (right6 = right as PropertyInfo) != null && left6 == right6;
		}

		// Token: 0x060046E5 RID: 18149 RVA: 0x00103164 File Offset: 0x00101364
		[__DynamicallyInvokable]
		public static bool operator !=(MemberInfo left, MemberInfo right)
		{
			return !(left == right);
		}

		// Token: 0x060046E6 RID: 18150 RVA: 0x00103170 File Offset: 0x00101370
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}

		// Token: 0x060046E7 RID: 18151 RVA: 0x00103179 File Offset: 0x00101379
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x060046E8 RID: 18152 RVA: 0x00103181 File Offset: 0x00101381
		Type _MemberInfo.GetType()
		{
			return base.GetType();
		}

		// Token: 0x060046E9 RID: 18153 RVA: 0x00103189 File Offset: 0x00101389
		void _MemberInfo.GetTypeInfoCount(out uint pcTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060046EA RID: 18154 RVA: 0x00103190 File Offset: 0x00101390
		void _MemberInfo.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060046EB RID: 18155 RVA: 0x00103197 File Offset: 0x00101397
		void _MemberInfo.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060046EC RID: 18156 RVA: 0x0010319E File Offset: 0x0010139E
		void _MemberInfo.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
		{
			throw new NotImplementedException();
		}
	}
}
