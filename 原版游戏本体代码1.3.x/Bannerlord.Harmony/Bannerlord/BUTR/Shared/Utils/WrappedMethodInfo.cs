using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Bannerlord.BUTR.Shared.Utils
{
	// Token: 0x0200000B RID: 11
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class WrappedMethodInfo : MethodInfo
	{
		// Token: 0x0600004B RID: 75 RVA: 0x00003611 File Offset: 0x00001811
		public WrappedMethodInfo(MethodInfo actualMethodInfo, object instance)
		{
			this._methodInfoImplementation = actualMethodInfo;
			this._instance = instance;
		}

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x0600004C RID: 76 RVA: 0x00003629 File Offset: 0x00001829
		public override MethodAttributes Attributes
		{
			get
			{
				return this._methodInfoImplementation.Attributes;
			}
		}

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x0600004D RID: 77 RVA: 0x00003636 File Offset: 0x00001836
		public override CallingConventions CallingConvention
		{
			get
			{
				return this._methodInfoImplementation.CallingConvention;
			}
		}

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x0600004E RID: 78 RVA: 0x00003643 File Offset: 0x00001843
		public override bool ContainsGenericParameters
		{
			get
			{
				return this._methodInfoImplementation.ContainsGenericParameters;
			}
		}

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x0600004F RID: 79 RVA: 0x00003650 File Offset: 0x00001850
		public override IEnumerable<CustomAttributeData> CustomAttributes
		{
			get
			{
				return this._methodInfoImplementation.CustomAttributes;
			}
		}

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x06000050 RID: 80 RVA: 0x0000365D File Offset: 0x0000185D
		[Nullable(2)]
		public override Type DeclaringType
		{
			[NullableContext(2)]
			get
			{
				return this._methodInfoImplementation.DeclaringType;
			}
		}

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x06000051 RID: 81 RVA: 0x0000366A File Offset: 0x0000186A
		public override bool IsGenericMethod
		{
			get
			{
				return this._methodInfoImplementation.IsGenericMethod;
			}
		}

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x06000052 RID: 82 RVA: 0x00003677 File Offset: 0x00001877
		public override bool IsGenericMethodDefinition
		{
			get
			{
				return this._methodInfoImplementation.IsGenericMethodDefinition;
			}
		}

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x06000053 RID: 83 RVA: 0x00003684 File Offset: 0x00001884
		public override bool IsSecurityCritical
		{
			get
			{
				return this._methodInfoImplementation.IsSecurityCritical;
			}
		}

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x06000054 RID: 84 RVA: 0x00003691 File Offset: 0x00001891
		public override bool IsSecuritySafeCritical
		{
			get
			{
				return this._methodInfoImplementation.IsSecuritySafeCritical;
			}
		}

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x06000055 RID: 85 RVA: 0x0000369E File Offset: 0x0000189E
		public override bool IsSecurityTransparent
		{
			get
			{
				return this._methodInfoImplementation.IsSecurityTransparent;
			}
		}

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x06000056 RID: 86 RVA: 0x000036AB File Offset: 0x000018AB
		public override MemberTypes MemberType
		{
			get
			{
				return this._methodInfoImplementation.MemberType;
			}
		}

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x06000057 RID: 87 RVA: 0x000036B8 File Offset: 0x000018B8
		public override int MetadataToken
		{
			get
			{
				return this._methodInfoImplementation.MetadataToken;
			}
		}

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x06000058 RID: 88 RVA: 0x000036C5 File Offset: 0x000018C5
		public override RuntimeMethodHandle MethodHandle
		{
			get
			{
				return this._methodInfoImplementation.MethodHandle;
			}
		}

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x06000059 RID: 89 RVA: 0x000036D2 File Offset: 0x000018D2
		public override MethodImplAttributes MethodImplementationFlags
		{
			get
			{
				return this._methodInfoImplementation.MethodImplementationFlags;
			}
		}

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x0600005A RID: 90 RVA: 0x000036DF File Offset: 0x000018DF
		public override Module Module
		{
			get
			{
				return this._methodInfoImplementation.Module;
			}
		}

		// Token: 0x1700001F RID: 31
		// (get) Token: 0x0600005B RID: 91 RVA: 0x000036EC File Offset: 0x000018EC
		public override string Name
		{
			get
			{
				return this._methodInfoImplementation.Name;
			}
		}

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x0600005C RID: 92 RVA: 0x000036F9 File Offset: 0x000018F9
		[Nullable(2)]
		public override Type ReflectedType
		{
			[NullableContext(2)]
			get
			{
				return this._methodInfoImplementation.ReflectedType;
			}
		}

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x0600005D RID: 93 RVA: 0x00003706 File Offset: 0x00001906
		public override ParameterInfo ReturnParameter
		{
			get
			{
				return this._methodInfoImplementation.ReturnParameter;
			}
		}

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x0600005E RID: 94 RVA: 0x00003713 File Offset: 0x00001913
		public override Type ReturnType
		{
			get
			{
				return this._methodInfoImplementation.ReturnType;
			}
		}

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x0600005F RID: 95 RVA: 0x00003720 File Offset: 0x00001920
		public override ICustomAttributeProvider ReturnTypeCustomAttributes
		{
			get
			{
				return this._methodInfoImplementation.ReturnTypeCustomAttributes;
			}
		}

		// Token: 0x06000060 RID: 96 RVA: 0x0000372D File Offset: 0x0000192D
		public override Delegate CreateDelegate(Type delegateType)
		{
			return this._methodInfoImplementation.CreateDelegate(delegateType);
		}

		// Token: 0x06000061 RID: 97 RVA: 0x0000373B File Offset: 0x0000193B
		public override Delegate CreateDelegate(Type delegateType, [Nullable(2)] object target)
		{
			return this._methodInfoImplementation.CreateDelegate(delegateType, target);
		}

		// Token: 0x06000062 RID: 98 RVA: 0x0000374A File Offset: 0x0000194A
		public override MethodInfo GetBaseDefinition()
		{
			return this._methodInfoImplementation.GetBaseDefinition();
		}

		// Token: 0x06000063 RID: 99 RVA: 0x00003757 File Offset: 0x00001957
		public override object[] GetCustomAttributes(bool inherit)
		{
			return this._methodInfoImplementation.GetCustomAttributes(inherit);
		}

		// Token: 0x06000064 RID: 100 RVA: 0x00003765 File Offset: 0x00001965
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			return this._methodInfoImplementation.GetCustomAttributes(attributeType, inherit);
		}

		// Token: 0x06000065 RID: 101 RVA: 0x00003774 File Offset: 0x00001974
		public override IList<CustomAttributeData> GetCustomAttributesData()
		{
			return this._methodInfoImplementation.GetCustomAttributesData();
		}

		// Token: 0x06000066 RID: 102 RVA: 0x00003781 File Offset: 0x00001981
		public override Type[] GetGenericArguments()
		{
			return this._methodInfoImplementation.GetGenericArguments();
		}

		// Token: 0x06000067 RID: 103 RVA: 0x0000378E File Offset: 0x0000198E
		public override MethodInfo GetGenericMethodDefinition()
		{
			return this._methodInfoImplementation.GetGenericMethodDefinition();
		}

		// Token: 0x06000068 RID: 104 RVA: 0x0000379B File Offset: 0x0000199B
		[NullableContext(2)]
		public override MethodBody GetMethodBody()
		{
			return this._methodInfoImplementation.GetMethodBody();
		}

		// Token: 0x06000069 RID: 105 RVA: 0x000037A8 File Offset: 0x000019A8
		public override MethodImplAttributes GetMethodImplementationFlags()
		{
			return this._methodInfoImplementation.GetMethodImplementationFlags();
		}

		// Token: 0x0600006A RID: 106 RVA: 0x000037B5 File Offset: 0x000019B5
		public override ParameterInfo[] GetParameters()
		{
			return this._methodInfoImplementation.GetParameters();
		}

		// Token: 0x0600006B RID: 107 RVA: 0x000037C2 File Offset: 0x000019C2
		[NullableContext(2)]
		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
		{
			return this._methodInfoImplementation.Invoke(this._instance, invokeAttr, binder, parameters, culture);
		}

		// Token: 0x0600006C RID: 108 RVA: 0x000037DB File Offset: 0x000019DB
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			return this._methodInfoImplementation.IsDefined(attributeType, inherit);
		}

		// Token: 0x0600006D RID: 109 RVA: 0x000037EA File Offset: 0x000019EA
		public override MethodInfo MakeGenericMethod(params Type[] typeArguments)
		{
			return this._methodInfoImplementation.MakeGenericMethod(typeArguments);
		}

		// Token: 0x0600006E RID: 110 RVA: 0x000037F8 File Offset: 0x000019F8
		[NullableContext(2)]
		public override string ToString()
		{
			return this._methodInfoImplementation.ToString();
		}

		// Token: 0x0600006F RID: 111 RVA: 0x00003808 File Offset: 0x00001A08
		[NullableContext(2)]
		public override bool Equals(object obj)
		{
			if (!true)
			{
			}
			WrappedMethodInfo proxy = obj as WrappedMethodInfo;
			bool result;
			if (proxy == null)
			{
				MethodInfo propertyInfo = obj as MethodInfo;
				if (propertyInfo == null)
				{
					result = this._methodInfoImplementation.Equals(obj);
				}
				else
				{
					result = this._methodInfoImplementation.Equals(propertyInfo);
				}
			}
			else
			{
				result = this._methodInfoImplementation.Equals(proxy._methodInfoImplementation);
			}
			if (!true)
			{
			}
			return result;
		}

		// Token: 0x06000070 RID: 112 RVA: 0x0000386A File Offset: 0x00001A6A
		public override int GetHashCode()
		{
			return this._methodInfoImplementation.GetHashCode();
		}

		// Token: 0x04000021 RID: 33
		private readonly object _instance;

		// Token: 0x04000022 RID: 34
		private readonly MethodInfo _methodInfoImplementation;
	}
}
