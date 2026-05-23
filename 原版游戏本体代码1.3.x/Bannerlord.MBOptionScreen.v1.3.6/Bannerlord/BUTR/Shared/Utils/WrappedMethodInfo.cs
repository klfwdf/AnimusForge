using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Bannerlord.BUTR.Shared.Utils
{
	// Token: 0x0200004D RID: 77
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class WrappedMethodInfo : MethodInfo
	{
		// Token: 0x06000254 RID: 596 RVA: 0x00008C38 File Offset: 0x00006E38
		public WrappedMethodInfo(MethodInfo actualMethodInfo, object instance)
		{
			this._methodInfoImplementation = actualMethodInfo;
			this._instance = instance;
		}

		// Token: 0x170000BB RID: 187
		// (get) Token: 0x06000255 RID: 597 RVA: 0x00008C4E File Offset: 0x00006E4E
		public override MethodAttributes Attributes
		{
			get
			{
				return this._methodInfoImplementation.Attributes;
			}
		}

		// Token: 0x170000BC RID: 188
		// (get) Token: 0x06000256 RID: 598 RVA: 0x00008C5B File Offset: 0x00006E5B
		public override CallingConventions CallingConvention
		{
			get
			{
				return this._methodInfoImplementation.CallingConvention;
			}
		}

		// Token: 0x170000BD RID: 189
		// (get) Token: 0x06000257 RID: 599 RVA: 0x00008C68 File Offset: 0x00006E68
		public override bool ContainsGenericParameters
		{
			get
			{
				return this._methodInfoImplementation.ContainsGenericParameters;
			}
		}

		// Token: 0x170000BE RID: 190
		// (get) Token: 0x06000258 RID: 600 RVA: 0x00008C75 File Offset: 0x00006E75
		public override IEnumerable<CustomAttributeData> CustomAttributes
		{
			get
			{
				return this._methodInfoImplementation.CustomAttributes;
			}
		}

		// Token: 0x170000BF RID: 191
		// (get) Token: 0x06000259 RID: 601 RVA: 0x00008C82 File Offset: 0x00006E82
		[Nullable(2)]
		public override Type DeclaringType
		{
			[NullableContext(2)]
			get
			{
				return this._methodInfoImplementation.DeclaringType;
			}
		}

		// Token: 0x170000C0 RID: 192
		// (get) Token: 0x0600025A RID: 602 RVA: 0x00008C8F File Offset: 0x00006E8F
		public override bool IsGenericMethod
		{
			get
			{
				return this._methodInfoImplementation.IsGenericMethod;
			}
		}

		// Token: 0x170000C1 RID: 193
		// (get) Token: 0x0600025B RID: 603 RVA: 0x00008C9C File Offset: 0x00006E9C
		public override bool IsGenericMethodDefinition
		{
			get
			{
				return this._methodInfoImplementation.IsGenericMethodDefinition;
			}
		}

		// Token: 0x170000C2 RID: 194
		// (get) Token: 0x0600025C RID: 604 RVA: 0x00008CA9 File Offset: 0x00006EA9
		public override bool IsSecurityCritical
		{
			get
			{
				return this._methodInfoImplementation.IsSecurityCritical;
			}
		}

		// Token: 0x170000C3 RID: 195
		// (get) Token: 0x0600025D RID: 605 RVA: 0x00008CB6 File Offset: 0x00006EB6
		public override bool IsSecuritySafeCritical
		{
			get
			{
				return this._methodInfoImplementation.IsSecuritySafeCritical;
			}
		}

		// Token: 0x170000C4 RID: 196
		// (get) Token: 0x0600025E RID: 606 RVA: 0x00008CC3 File Offset: 0x00006EC3
		public override bool IsSecurityTransparent
		{
			get
			{
				return this._methodInfoImplementation.IsSecurityTransparent;
			}
		}

		// Token: 0x170000C5 RID: 197
		// (get) Token: 0x0600025F RID: 607 RVA: 0x00008CD0 File Offset: 0x00006ED0
		public override MemberTypes MemberType
		{
			get
			{
				return this._methodInfoImplementation.MemberType;
			}
		}

		// Token: 0x170000C6 RID: 198
		// (get) Token: 0x06000260 RID: 608 RVA: 0x00008CDD File Offset: 0x00006EDD
		public override int MetadataToken
		{
			get
			{
				return this._methodInfoImplementation.MetadataToken;
			}
		}

		// Token: 0x170000C7 RID: 199
		// (get) Token: 0x06000261 RID: 609 RVA: 0x00008CEA File Offset: 0x00006EEA
		public override RuntimeMethodHandle MethodHandle
		{
			get
			{
				return this._methodInfoImplementation.MethodHandle;
			}
		}

		// Token: 0x170000C8 RID: 200
		// (get) Token: 0x06000262 RID: 610 RVA: 0x00008CF7 File Offset: 0x00006EF7
		public override MethodImplAttributes MethodImplementationFlags
		{
			get
			{
				return this._methodInfoImplementation.MethodImplementationFlags;
			}
		}

		// Token: 0x170000C9 RID: 201
		// (get) Token: 0x06000263 RID: 611 RVA: 0x00008D04 File Offset: 0x00006F04
		public override Module Module
		{
			get
			{
				return this._methodInfoImplementation.Module;
			}
		}

		// Token: 0x170000CA RID: 202
		// (get) Token: 0x06000264 RID: 612 RVA: 0x00008D11 File Offset: 0x00006F11
		public override string Name
		{
			get
			{
				return this._methodInfoImplementation.Name;
			}
		}

		// Token: 0x170000CB RID: 203
		// (get) Token: 0x06000265 RID: 613 RVA: 0x00008D1E File Offset: 0x00006F1E
		[Nullable(2)]
		public override Type ReflectedType
		{
			[NullableContext(2)]
			get
			{
				return this._methodInfoImplementation.ReflectedType;
			}
		}

		// Token: 0x170000CC RID: 204
		// (get) Token: 0x06000266 RID: 614 RVA: 0x00008D2B File Offset: 0x00006F2B
		public override ParameterInfo ReturnParameter
		{
			get
			{
				return this._methodInfoImplementation.ReturnParameter;
			}
		}

		// Token: 0x170000CD RID: 205
		// (get) Token: 0x06000267 RID: 615 RVA: 0x00008D38 File Offset: 0x00006F38
		public override Type ReturnType
		{
			get
			{
				return this._methodInfoImplementation.ReturnType;
			}
		}

		// Token: 0x170000CE RID: 206
		// (get) Token: 0x06000268 RID: 616 RVA: 0x00008D45 File Offset: 0x00006F45
		public override ICustomAttributeProvider ReturnTypeCustomAttributes
		{
			get
			{
				return this._methodInfoImplementation.ReturnTypeCustomAttributes;
			}
		}

		// Token: 0x06000269 RID: 617 RVA: 0x00008D52 File Offset: 0x00006F52
		public override Delegate CreateDelegate(Type delegateType)
		{
			return this._methodInfoImplementation.CreateDelegate(delegateType);
		}

		// Token: 0x0600026A RID: 618 RVA: 0x00008D60 File Offset: 0x00006F60
		public override Delegate CreateDelegate(Type delegateType, [Nullable(2)] object target)
		{
			return this._methodInfoImplementation.CreateDelegate(delegateType, target);
		}

		// Token: 0x0600026B RID: 619 RVA: 0x00008D6F File Offset: 0x00006F6F
		public override MethodInfo GetBaseDefinition()
		{
			return this._methodInfoImplementation.GetBaseDefinition();
		}

		// Token: 0x0600026C RID: 620 RVA: 0x00008D7C File Offset: 0x00006F7C
		public override object[] GetCustomAttributes(bool inherit)
		{
			return this._methodInfoImplementation.GetCustomAttributes(inherit);
		}

		// Token: 0x0600026D RID: 621 RVA: 0x00008D8A File Offset: 0x00006F8A
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			return this._methodInfoImplementation.GetCustomAttributes(attributeType, inherit);
		}

		// Token: 0x0600026E RID: 622 RVA: 0x00008D99 File Offset: 0x00006F99
		public override IList<CustomAttributeData> GetCustomAttributesData()
		{
			return this._methodInfoImplementation.GetCustomAttributesData();
		}

		// Token: 0x0600026F RID: 623 RVA: 0x00008DA6 File Offset: 0x00006FA6
		public override Type[] GetGenericArguments()
		{
			return this._methodInfoImplementation.GetGenericArguments();
		}

		// Token: 0x06000270 RID: 624 RVA: 0x00008DB3 File Offset: 0x00006FB3
		public override MethodInfo GetGenericMethodDefinition()
		{
			return this._methodInfoImplementation.GetGenericMethodDefinition();
		}

		// Token: 0x06000271 RID: 625 RVA: 0x00008DC0 File Offset: 0x00006FC0
		[NullableContext(2)]
		public override MethodBody GetMethodBody()
		{
			return this._methodInfoImplementation.GetMethodBody();
		}

		// Token: 0x06000272 RID: 626 RVA: 0x00008DCD File Offset: 0x00006FCD
		public override MethodImplAttributes GetMethodImplementationFlags()
		{
			return this._methodInfoImplementation.GetMethodImplementationFlags();
		}

		// Token: 0x06000273 RID: 627 RVA: 0x00008DDA File Offset: 0x00006FDA
		public override ParameterInfo[] GetParameters()
		{
			return this._methodInfoImplementation.GetParameters();
		}

		// Token: 0x06000274 RID: 628 RVA: 0x00008DE7 File Offset: 0x00006FE7
		[NullableContext(2)]
		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
		{
			return this._methodInfoImplementation.Invoke(this._instance, invokeAttr, binder, parameters, culture);
		}

		// Token: 0x06000275 RID: 629 RVA: 0x00008E00 File Offset: 0x00007000
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			return this._methodInfoImplementation.IsDefined(attributeType, inherit);
		}

		// Token: 0x06000276 RID: 630 RVA: 0x00008E0F File Offset: 0x0000700F
		public override MethodInfo MakeGenericMethod(params Type[] typeArguments)
		{
			return this._methodInfoImplementation.MakeGenericMethod(typeArguments);
		}

		// Token: 0x06000277 RID: 631 RVA: 0x00008E1D File Offset: 0x0000701D
		[NullableContext(2)]
		public override string ToString()
		{
			return this._methodInfoImplementation.ToString();
		}

		// Token: 0x06000278 RID: 632 RVA: 0x00008E2C File Offset: 0x0000702C
		[NullableContext(2)]
		public override bool Equals(object obj)
		{
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
			return result;
		}

		// Token: 0x06000279 RID: 633 RVA: 0x00008E80 File Offset: 0x00007080
		public override int GetHashCode()
		{
			return this._methodInfoImplementation.GetHashCode();
		}

		// Token: 0x040000F1 RID: 241
		private readonly object _instance;

		// Token: 0x040000F2 RID: 242
		private readonly MethodInfo _methodInfoImplementation;
	}
}
