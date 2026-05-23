using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Serialization;
using System.Security;
using System.Text;

namespace System.Reflection
{
	// Token: 0x0200061C RID: 1564
	[Serializable]
	internal sealed class RuntimePropertyInfo : PropertyInfo, ISerializable
	{
		// Token: 0x06004877 RID: 18551 RVA: 0x00106F40 File Offset: 0x00105140
		[SecurityCritical]
		internal RuntimePropertyInfo(int tkProperty, RuntimeType declaredType, RuntimeType.RuntimeTypeCache reflectedTypeCache, out bool isPrivate)
		{
			MetadataImport metadataImport = declaredType.GetRuntimeModule().MetadataImport;
			this.m_token = tkProperty;
			this.m_reflectedTypeCache = reflectedTypeCache;
			this.m_declaringType = declaredType;
			ConstArray constArray;
			metadataImport.GetPropertyProps(tkProperty, out this.m_utf8name, out this.m_flags, out constArray);
			RuntimeMethodInfo runtimeMethodInfo;
			Associates.AssignAssociates(metadataImport, tkProperty, declaredType, reflectedTypeCache.GetRuntimeType(), out runtimeMethodInfo, out runtimeMethodInfo, out runtimeMethodInfo, out this.m_getterMethod, out this.m_setterMethod, out this.m_otherMethod, out isPrivate, out this.m_bindingFlags);
		}

		// Token: 0x06004878 RID: 18552 RVA: 0x00106FB8 File Offset: 0x001051B8
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		internal override bool CacheEquals(object o)
		{
			RuntimePropertyInfo runtimePropertyInfo = o as RuntimePropertyInfo;
			return runtimePropertyInfo != null && runtimePropertyInfo.m_token == this.m_token && RuntimeTypeHandle.GetModule(this.m_declaringType).Equals(RuntimeTypeHandle.GetModule(runtimePropertyInfo.m_declaringType));
		}

		// Token: 0x17000B46 RID: 2886
		// (get) Token: 0x06004879 RID: 18553 RVA: 0x00106FFC File Offset: 0x001051FC
		internal unsafe Signature Signature
		{
			[SecuritySafeCritical]
			get
			{
				if (this.m_signature == null)
				{
					void* ptr;
					PropertyAttributes propertyAttributes;
					ConstArray constArray;
					this.GetRuntimeModule().MetadataImport.GetPropertyProps(this.m_token, out ptr, out propertyAttributes, out constArray);
					this.m_signature = new Signature(constArray.Signature.ToPointer(), constArray.Length, this.m_declaringType);
				}
				return this.m_signature;
			}
		}

		// Token: 0x0600487A RID: 18554 RVA: 0x0010705E File Offset: 0x0010525E
		internal bool EqualsSig(RuntimePropertyInfo target)
		{
			return Signature.CompareSig(this.Signature, target.Signature);
		}

		// Token: 0x17000B47 RID: 2887
		// (get) Token: 0x0600487B RID: 18555 RVA: 0x00107071 File Offset: 0x00105271
		internal BindingFlags BindingFlags
		{
			get
			{
				return this.m_bindingFlags;
			}
		}

		// Token: 0x0600487C RID: 18556 RVA: 0x00107079 File Offset: 0x00105279
		public override string ToString()
		{
			return this.FormatNameAndSig(false);
		}

		// Token: 0x0600487D RID: 18557 RVA: 0x00107084 File Offset: 0x00105284
		private string FormatNameAndSig(bool serialization)
		{
			StringBuilder stringBuilder = new StringBuilder(this.PropertyType.FormatTypeName(serialization));
			stringBuilder.Append(" ");
			stringBuilder.Append(this.Name);
			RuntimeType[] arguments = this.Signature.Arguments;
			if (arguments.Length != 0)
			{
				stringBuilder.Append(" [");
				StringBuilder stringBuilder2 = stringBuilder;
				Type[] parameterTypes = arguments;
				stringBuilder2.Append(MethodBase.ConstructParameters(parameterTypes, this.Signature.CallingConvention, serialization));
				stringBuilder.Append("]");
			}
			return stringBuilder.ToString();
		}

		// Token: 0x0600487E RID: 18558 RVA: 0x00107105 File Offset: 0x00105305
		public override object[] GetCustomAttributes(bool inherit)
		{
			return CustomAttribute.GetCustomAttributes(this, typeof(object) as RuntimeType);
		}

		// Token: 0x0600487F RID: 18559 RVA: 0x0010711C File Offset: 0x0010531C
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

		// Token: 0x06004880 RID: 18560 RVA: 0x00107170 File Offset: 0x00105370
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

		// Token: 0x06004881 RID: 18561 RVA: 0x001071C2 File Offset: 0x001053C2
		public override IList<CustomAttributeData> GetCustomAttributesData()
		{
			return CustomAttributeData.GetCustomAttributesInternal(this);
		}

		// Token: 0x17000B48 RID: 2888
		// (get) Token: 0x06004882 RID: 18562 RVA: 0x001071CA File Offset: 0x001053CA
		public override MemberTypes MemberType
		{
			get
			{
				return MemberTypes.Property;
			}
		}

		// Token: 0x17000B49 RID: 2889
		// (get) Token: 0x06004883 RID: 18563 RVA: 0x001071D0 File Offset: 0x001053D0
		public override string Name
		{
			[SecuritySafeCritical]
			get
			{
				if (this.m_name == null)
				{
					this.m_name = new Utf8String(this.m_utf8name).ToString();
				}
				return this.m_name;
			}
		}

		// Token: 0x17000B4A RID: 2890
		// (get) Token: 0x06004884 RID: 18564 RVA: 0x0010720A File Offset: 0x0010540A
		public override Type DeclaringType
		{
			get
			{
				return this.m_declaringType;
			}
		}

		// Token: 0x17000B4B RID: 2891
		// (get) Token: 0x06004885 RID: 18565 RVA: 0x00107212 File Offset: 0x00105412
		public override Type ReflectedType
		{
			get
			{
				return this.ReflectedTypeInternal;
			}
		}

		// Token: 0x17000B4C RID: 2892
		// (get) Token: 0x06004886 RID: 18566 RVA: 0x0010721A File Offset: 0x0010541A
		private RuntimeType ReflectedTypeInternal
		{
			get
			{
				return this.m_reflectedTypeCache.GetRuntimeType();
			}
		}

		// Token: 0x17000B4D RID: 2893
		// (get) Token: 0x06004887 RID: 18567 RVA: 0x00107227 File Offset: 0x00105427
		public override int MetadataToken
		{
			get
			{
				return this.m_token;
			}
		}

		// Token: 0x17000B4E RID: 2894
		// (get) Token: 0x06004888 RID: 18568 RVA: 0x0010722F File Offset: 0x0010542F
		public override Module Module
		{
			get
			{
				return this.GetRuntimeModule();
			}
		}

		// Token: 0x06004889 RID: 18569 RVA: 0x00107237 File Offset: 0x00105437
		internal RuntimeModule GetRuntimeModule()
		{
			return this.m_declaringType.GetRuntimeModule();
		}

		// Token: 0x0600488A RID: 18570 RVA: 0x00107244 File Offset: 0x00105444
		public override Type[] GetRequiredCustomModifiers()
		{
			return this.Signature.GetCustomModifiers(0, true);
		}

		// Token: 0x0600488B RID: 18571 RVA: 0x00107253 File Offset: 0x00105453
		public override Type[] GetOptionalCustomModifiers()
		{
			return this.Signature.GetCustomModifiers(0, false);
		}

		// Token: 0x0600488C RID: 18572 RVA: 0x00107264 File Offset: 0x00105464
		[SecuritySafeCritical]
		internal object GetConstantValue(bool raw)
		{
			object value = MdConstant.GetValue(this.GetRuntimeModule().MetadataImport, this.m_token, this.PropertyType.GetTypeHandleInternal(), raw);
			if (value == DBNull.Value)
			{
				throw new InvalidOperationException(Environment.GetResourceString("Arg_EnumLitValueNotFound"));
			}
			return value;
		}

		// Token: 0x0600488D RID: 18573 RVA: 0x001072AD File Offset: 0x001054AD
		public override object GetConstantValue()
		{
			return this.GetConstantValue(false);
		}

		// Token: 0x0600488E RID: 18574 RVA: 0x001072B6 File Offset: 0x001054B6
		public override object GetRawConstantValue()
		{
			return this.GetConstantValue(true);
		}

		// Token: 0x0600488F RID: 18575 RVA: 0x001072C0 File Offset: 0x001054C0
		public override MethodInfo[] GetAccessors(bool nonPublic)
		{
			List<MethodInfo> list = new List<MethodInfo>();
			if (Associates.IncludeAccessor(this.m_getterMethod, nonPublic))
			{
				list.Add(this.m_getterMethod);
			}
			if (Associates.IncludeAccessor(this.m_setterMethod, nonPublic))
			{
				list.Add(this.m_setterMethod);
			}
			if (this.m_otherMethod != null)
			{
				for (int i = 0; i < this.m_otherMethod.Length; i++)
				{
					if (Associates.IncludeAccessor(this.m_otherMethod[i], nonPublic))
					{
						list.Add(this.m_otherMethod[i]);
					}
				}
			}
			return list.ToArray();
		}

		// Token: 0x17000B4F RID: 2895
		// (get) Token: 0x06004890 RID: 18576 RVA: 0x00107346 File Offset: 0x00105546
		public override Type PropertyType
		{
			get
			{
				return this.Signature.ReturnType;
			}
		}

		// Token: 0x06004891 RID: 18577 RVA: 0x00107353 File Offset: 0x00105553
		public override MethodInfo GetGetMethod(bool nonPublic)
		{
			if (!Associates.IncludeAccessor(this.m_getterMethod, nonPublic))
			{
				return null;
			}
			return this.m_getterMethod;
		}

		// Token: 0x06004892 RID: 18578 RVA: 0x0010736B File Offset: 0x0010556B
		public override MethodInfo GetSetMethod(bool nonPublic)
		{
			if (!Associates.IncludeAccessor(this.m_setterMethod, nonPublic))
			{
				return null;
			}
			return this.m_setterMethod;
		}

		// Token: 0x06004893 RID: 18579 RVA: 0x00107384 File Offset: 0x00105584
		public override ParameterInfo[] GetIndexParameters()
		{
			ParameterInfo[] indexParametersNoCopy = this.GetIndexParametersNoCopy();
			int num = indexParametersNoCopy.Length;
			if (num == 0)
			{
				return indexParametersNoCopy;
			}
			ParameterInfo[] array = new ParameterInfo[num];
			Array.Copy(indexParametersNoCopy, array, num);
			return array;
		}

		// Token: 0x06004894 RID: 18580 RVA: 0x001073B4 File Offset: 0x001055B4
		internal ParameterInfo[] GetIndexParametersNoCopy()
		{
			if (this.m_parameters == null)
			{
				int num = 0;
				ParameterInfo[] array = null;
				MethodInfo methodInfo = this.GetGetMethod(true);
				if (methodInfo != null)
				{
					array = methodInfo.GetParametersNoCopy();
					num = array.Length;
				}
				else
				{
					methodInfo = this.GetSetMethod(true);
					if (methodInfo != null)
					{
						array = methodInfo.GetParametersNoCopy();
						num = array.Length - 1;
					}
				}
				ParameterInfo[] array2 = new ParameterInfo[num];
				for (int i = 0; i < num; i++)
				{
					array2[i] = new RuntimeParameterInfo((RuntimeParameterInfo)array[i], this);
				}
				this.m_parameters = array2;
			}
			return this.m_parameters;
		}

		// Token: 0x17000B50 RID: 2896
		// (get) Token: 0x06004895 RID: 18581 RVA: 0x00107440 File Offset: 0x00105640
		public override PropertyAttributes Attributes
		{
			get
			{
				return this.m_flags;
			}
		}

		// Token: 0x17000B51 RID: 2897
		// (get) Token: 0x06004896 RID: 18582 RVA: 0x00107448 File Offset: 0x00105648
		public override bool CanRead
		{
			get
			{
				return this.m_getterMethod != null;
			}
		}

		// Token: 0x17000B52 RID: 2898
		// (get) Token: 0x06004897 RID: 18583 RVA: 0x00107456 File Offset: 0x00105656
		public override bool CanWrite
		{
			get
			{
				return this.m_setterMethod != null;
			}
		}

		// Token: 0x06004898 RID: 18584 RVA: 0x00107464 File Offset: 0x00105664
		[DebuggerStepThrough]
		[DebuggerHidden]
		public override object GetValue(object obj, object[] index)
		{
			return this.GetValue(obj, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, index, null);
		}

		// Token: 0x06004899 RID: 18585 RVA: 0x00107474 File Offset: 0x00105674
		[DebuggerStepThrough]
		[DebuggerHidden]
		public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
		{
			MethodInfo getMethod = this.GetGetMethod(true);
			if (getMethod == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_GetMethNotFnd"));
			}
			return getMethod.Invoke(obj, invokeAttr, binder, index, null);
		}

		// Token: 0x0600489A RID: 18586 RVA: 0x001074AE File Offset: 0x001056AE
		[DebuggerStepThrough]
		[DebuggerHidden]
		public override void SetValue(object obj, object value, object[] index)
		{
			this.SetValue(obj, value, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, index, null);
		}

		// Token: 0x0600489B RID: 18587 RVA: 0x001074C0 File Offset: 0x001056C0
		[DebuggerStepThrough]
		[DebuggerHidden]
		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
		{
			MethodInfo setMethod = this.GetSetMethod(true);
			if (setMethod == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_SetMethNotFnd"));
			}
			object[] array;
			if (index != null)
			{
				array = new object[index.Length + 1];
				for (int i = 0; i < index.Length; i++)
				{
					array[i] = index[i];
				}
				array[index.Length] = value;
			}
			else
			{
				array = new object[] { value };
			}
			setMethod.Invoke(obj, invokeAttr, binder, array, culture);
		}

		// Token: 0x0600489C RID: 18588 RVA: 0x00107538 File Offset: 0x00105738
		[SecurityCritical]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			MemberInfoSerializationHolder.GetSerializationInfo(info, this.Name, this.ReflectedTypeInternal, this.ToString(), this.SerializationToString(), MemberTypes.Property, null);
		}

		// Token: 0x0600489D RID: 18589 RVA: 0x00107569 File Offset: 0x00105769
		internal string SerializationToString()
		{
			return this.FormatNameAndSig(true);
		}

		// Token: 0x04001E0C RID: 7692
		private int m_token;

		// Token: 0x04001E0D RID: 7693
		private string m_name;

		// Token: 0x04001E0E RID: 7694
		[SecurityCritical]
		private unsafe void* m_utf8name;

		// Token: 0x04001E0F RID: 7695
		private PropertyAttributes m_flags;

		// Token: 0x04001E10 RID: 7696
		private RuntimeType.RuntimeTypeCache m_reflectedTypeCache;

		// Token: 0x04001E11 RID: 7697
		private RuntimeMethodInfo m_getterMethod;

		// Token: 0x04001E12 RID: 7698
		private RuntimeMethodInfo m_setterMethod;

		// Token: 0x04001E13 RID: 7699
		private MethodInfo[] m_otherMethod;

		// Token: 0x04001E14 RID: 7700
		private RuntimeType m_declaringType;

		// Token: 0x04001E15 RID: 7701
		private BindingFlags m_bindingFlags;

		// Token: 0x04001E16 RID: 7702
		private Signature m_signature;

		// Token: 0x04001E17 RID: 7703
		private ParameterInfo[] m_parameters;
	}
}
