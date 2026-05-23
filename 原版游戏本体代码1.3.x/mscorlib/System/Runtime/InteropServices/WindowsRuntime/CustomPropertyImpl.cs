using System;
using System.Globalization;
using System.Reflection;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x02000A11 RID: 2577
	internal sealed class CustomPropertyImpl : ICustomProperty
	{
		// Token: 0x060065AE RID: 26030 RVA: 0x00159A98 File Offset: 0x00157C98
		public CustomPropertyImpl(PropertyInfo propertyInfo)
		{
			if (propertyInfo == null)
			{
				throw new ArgumentNullException("propertyInfo");
			}
			this.m_property = propertyInfo;
		}

		// Token: 0x17001177 RID: 4471
		// (get) Token: 0x060065AF RID: 26031 RVA: 0x00159ABB File Offset: 0x00157CBB
		public string Name
		{
			get
			{
				return this.m_property.Name;
			}
		}

		// Token: 0x17001178 RID: 4472
		// (get) Token: 0x060065B0 RID: 26032 RVA: 0x00159AC8 File Offset: 0x00157CC8
		public bool CanRead
		{
			get
			{
				return this.m_property.GetGetMethod() != null;
			}
		}

		// Token: 0x17001179 RID: 4473
		// (get) Token: 0x060065B1 RID: 26033 RVA: 0x00159ADB File Offset: 0x00157CDB
		public bool CanWrite
		{
			get
			{
				return this.m_property.GetSetMethod() != null;
			}
		}

		// Token: 0x060065B2 RID: 26034 RVA: 0x00159AEE File Offset: 0x00157CEE
		public object GetValue(object target)
		{
			return this.InvokeInternal(target, null, true);
		}

		// Token: 0x060065B3 RID: 26035 RVA: 0x00159AF9 File Offset: 0x00157CF9
		public object GetValue(object target, object indexValue)
		{
			return this.InvokeInternal(target, new object[] { indexValue }, true);
		}

		// Token: 0x060065B4 RID: 26036 RVA: 0x00159B0D File Offset: 0x00157D0D
		public void SetValue(object target, object value)
		{
			this.InvokeInternal(target, new object[] { value }, false);
		}

		// Token: 0x060065B5 RID: 26037 RVA: 0x00159B22 File Offset: 0x00157D22
		public void SetValue(object target, object value, object indexValue)
		{
			this.InvokeInternal(target, new object[] { indexValue, value }, false);
		}

		// Token: 0x060065B6 RID: 26038 RVA: 0x00159B3C File Offset: 0x00157D3C
		[SecuritySafeCritical]
		private object InvokeInternal(object target, object[] args, bool getValue)
		{
			IGetProxyTarget getProxyTarget = target as IGetProxyTarget;
			if (getProxyTarget != null)
			{
				target = getProxyTarget.GetTarget();
			}
			MethodInfo methodInfo = (getValue ? this.m_property.GetGetMethod(true) : this.m_property.GetSetMethod(true));
			if (methodInfo == null)
			{
				throw new ArgumentException(Environment.GetResourceString(getValue ? "Arg_GetMethNotFnd" : "Arg_SetMethNotFnd"));
			}
			if (!methodInfo.IsPublic)
			{
				throw new MethodAccessException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Arg_MethodAccessException_WithMethodName"), methodInfo.ToString(), methodInfo.DeclaringType.FullName));
			}
			RuntimeMethodInfo runtimeMethodInfo = methodInfo as RuntimeMethodInfo;
			if (runtimeMethodInfo == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeMethodInfo"));
			}
			return runtimeMethodInfo.UnsafeInvoke(target, BindingFlags.Default, null, args, null);
		}

		// Token: 0x1700117A RID: 4474
		// (get) Token: 0x060065B7 RID: 26039 RVA: 0x00159BFA File Offset: 0x00157DFA
		public Type Type
		{
			get
			{
				return this.m_property.PropertyType;
			}
		}

		// Token: 0x04002D44 RID: 11588
		private PropertyInfo m_property;
	}
}
