using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Metadata;
using System.Runtime.Serialization;
using System.Security;
using System.Threading;

namespace System.Reflection
{
	// Token: 0x02000617 RID: 1559
	[Serializable]
	internal sealed class RuntimeParameterInfo : ParameterInfo, ISerializable
	{
		// Token: 0x0600482F RID: 18479 RVA: 0x00106400 File Offset: 0x00104600
		[SecurityCritical]
		internal static ParameterInfo[] GetParameters(IRuntimeMethodInfo method, MemberInfo member, Signature sig)
		{
			ParameterInfo parameterInfo;
			return RuntimeParameterInfo.GetParameters(method, member, sig, out parameterInfo, false);
		}

		// Token: 0x06004830 RID: 18480 RVA: 0x00106418 File Offset: 0x00104618
		[SecurityCritical]
		internal static ParameterInfo GetReturnParameter(IRuntimeMethodInfo method, MemberInfo member, Signature sig)
		{
			ParameterInfo result;
			RuntimeParameterInfo.GetParameters(method, member, sig, out result, true);
			return result;
		}

		// Token: 0x06004831 RID: 18481 RVA: 0x00106434 File Offset: 0x00104634
		[SecurityCritical]
		internal static ParameterInfo[] GetParameters(IRuntimeMethodInfo methodHandle, MemberInfo member, Signature sig, out ParameterInfo returnParameter, bool fetchReturnParameter)
		{
			returnParameter = null;
			int num = sig.Arguments.Length;
			ParameterInfo[] array = (fetchReturnParameter ? null : new ParameterInfo[num]);
			int methodDef = RuntimeMethodHandle.GetMethodDef(methodHandle);
			int num2 = 0;
			if (!System.Reflection.MetadataToken.IsNullToken(methodDef))
			{
				MetadataImport metadataImport = RuntimeTypeHandle.GetMetadataImport(RuntimeMethodHandle.GetDeclaringType(methodHandle));
				MetadataEnumResult metadataEnumResult;
				metadataImport.EnumParams(methodDef, out metadataEnumResult);
				num2 = metadataEnumResult.Length;
				if (num2 > num + 1)
				{
					throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ParameterSignatureMismatch"));
				}
				for (int i = 0; i < num2; i++)
				{
					int num3 = metadataEnumResult[i];
					int num4;
					ParameterAttributes attributes;
					metadataImport.GetParamDefProps(num3, out num4, out attributes);
					num4--;
					if (fetchReturnParameter && num4 == -1)
					{
						if (returnParameter != null)
						{
							throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ParameterSignatureMismatch"));
						}
						returnParameter = new RuntimeParameterInfo(sig, metadataImport, num3, num4, attributes, member);
					}
					else if (!fetchReturnParameter && num4 >= 0)
					{
						if (num4 >= num)
						{
							throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ParameterSignatureMismatch"));
						}
						array[num4] = new RuntimeParameterInfo(sig, metadataImport, num3, num4, attributes, member);
					}
				}
			}
			if (fetchReturnParameter)
			{
				if (returnParameter == null)
				{
					returnParameter = new RuntimeParameterInfo(sig, MetadataImport.EmptyImport, 0, -1, ParameterAttributes.None, member);
				}
			}
			else if (num2 < array.Length + 1)
			{
				for (int j = 0; j < array.Length; j++)
				{
					if (array[j] == null)
					{
						array[j] = new RuntimeParameterInfo(sig, MetadataImport.EmptyImport, 0, j, ParameterAttributes.None, member);
					}
				}
			}
			return array;
		}

		// Token: 0x17000B34 RID: 2868
		// (get) Token: 0x06004832 RID: 18482 RVA: 0x0010658C File Offset: 0x0010478C
		internal MethodBase DefiningMethod
		{
			get
			{
				return (this.m_originalMember != null) ? this.m_originalMember : (this.MemberImpl as MethodBase);
			}
		}

		// Token: 0x06004833 RID: 18483 RVA: 0x001065BC File Offset: 0x001047BC
		[SecurityCritical]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			info.SetType(typeof(ParameterInfo));
			info.AddValue("AttrsImpl", this.Attributes);
			info.AddValue("ClassImpl", this.ParameterType);
			info.AddValue("DefaultValueImpl", this.DefaultValue);
			info.AddValue("MemberImpl", this.Member);
			info.AddValue("NameImpl", this.Name);
			info.AddValue("PositionImpl", this.Position);
			info.AddValue("_token", this.m_tkParamDef);
		}

		// Token: 0x06004834 RID: 18484 RVA: 0x00106663 File Offset: 0x00104863
		internal RuntimeParameterInfo(RuntimeParameterInfo accessor, RuntimePropertyInfo property)
			: this(accessor, property)
		{
			this.m_signature = property.Signature;
		}

		// Token: 0x06004835 RID: 18485 RVA: 0x0010667C File Offset: 0x0010487C
		private RuntimeParameterInfo(RuntimeParameterInfo accessor, MemberInfo member)
		{
			this.MemberImpl = member;
			this.m_originalMember = accessor.MemberImpl as MethodBase;
			this.NameImpl = accessor.Name;
			this.m_nameIsCached = true;
			this.ClassImpl = accessor.ParameterType;
			this.PositionImpl = accessor.Position;
			this.AttrsImpl = accessor.Attributes;
			this.m_tkParamDef = (System.Reflection.MetadataToken.IsNullToken(accessor.MetadataToken) ? 134217728 : accessor.MetadataToken);
			this.m_scope = accessor.m_scope;
		}

		// Token: 0x06004836 RID: 18486 RVA: 0x0010670C File Offset: 0x0010490C
		private RuntimeParameterInfo(Signature signature, MetadataImport scope, int tkParamDef, int position, ParameterAttributes attributes, MemberInfo member)
		{
			this.PositionImpl = position;
			this.MemberImpl = member;
			this.m_signature = signature;
			this.m_tkParamDef = (System.Reflection.MetadataToken.IsNullToken(tkParamDef) ? 134217728 : tkParamDef);
			this.m_scope = scope;
			this.AttrsImpl = attributes;
			this.ClassImpl = null;
			this.NameImpl = null;
		}

		// Token: 0x06004837 RID: 18487 RVA: 0x0010676C File Offset: 0x0010496C
		internal RuntimeParameterInfo(MethodInfo owner, string name, Type parameterType, int position)
		{
			this.MemberImpl = owner;
			this.NameImpl = name;
			this.m_nameIsCached = true;
			this.m_noMetadata = true;
			this.ClassImpl = parameterType;
			this.PositionImpl = position;
			this.AttrsImpl = ParameterAttributes.None;
			this.m_tkParamDef = 134217728;
			this.m_scope = MetadataImport.EmptyImport;
		}

		// Token: 0x17000B35 RID: 2869
		// (get) Token: 0x06004838 RID: 18488 RVA: 0x001067CC File Offset: 0x001049CC
		public override Type ParameterType
		{
			get
			{
				if (this.ClassImpl == null)
				{
					RuntimeType classImpl;
					if (this.PositionImpl == -1)
					{
						classImpl = this.m_signature.ReturnType;
					}
					else
					{
						classImpl = this.m_signature.Arguments[this.PositionImpl];
					}
					this.ClassImpl = classImpl;
				}
				return this.ClassImpl;
			}
		}

		// Token: 0x17000B36 RID: 2870
		// (get) Token: 0x06004839 RID: 18489 RVA: 0x00106820 File Offset: 0x00104A20
		public override string Name
		{
			[SecuritySafeCritical]
			get
			{
				if (!this.m_nameIsCached)
				{
					if (!System.Reflection.MetadataToken.IsNullToken(this.m_tkParamDef))
					{
						string nameImpl = this.m_scope.GetName(this.m_tkParamDef).ToString();
						this.NameImpl = nameImpl;
					}
					this.m_nameIsCached = true;
				}
				return this.NameImpl;
			}
		}

		// Token: 0x17000B37 RID: 2871
		// (get) Token: 0x0600483A RID: 18490 RVA: 0x0010687C File Offset: 0x00104A7C
		public override bool HasDefaultValue
		{
			get
			{
				if (this.m_noMetadata || this.m_noDefaultValue)
				{
					return false;
				}
				object defaultValueInternal = this.GetDefaultValueInternal(false);
				return defaultValueInternal != DBNull.Value;
			}
		}

		// Token: 0x17000B38 RID: 2872
		// (get) Token: 0x0600483B RID: 18491 RVA: 0x001068AE File Offset: 0x00104AAE
		public override object DefaultValue
		{
			get
			{
				return this.GetDefaultValue(false);
			}
		}

		// Token: 0x17000B39 RID: 2873
		// (get) Token: 0x0600483C RID: 18492 RVA: 0x001068B7 File Offset: 0x00104AB7
		public override object RawDefaultValue
		{
			get
			{
				return this.GetDefaultValue(true);
			}
		}

		// Token: 0x0600483D RID: 18493 RVA: 0x001068C0 File Offset: 0x00104AC0
		private object GetDefaultValue(bool raw)
		{
			if (this.m_noMetadata)
			{
				return null;
			}
			object obj = this.GetDefaultValueInternal(raw);
			if (obj == DBNull.Value && base.IsOptional)
			{
				obj = Type.Missing;
			}
			return obj;
		}

		// Token: 0x0600483E RID: 18494 RVA: 0x001068F8 File Offset: 0x00104AF8
		[SecuritySafeCritical]
		private object GetDefaultValueInternal(bool raw)
		{
			if (this.m_noDefaultValue)
			{
				return DBNull.Value;
			}
			object obj = null;
			if (this.ParameterType == typeof(DateTime))
			{
				if (raw)
				{
					CustomAttributeTypedArgument customAttributeTypedArgument = CustomAttributeData.Filter(CustomAttributeData.GetCustomAttributes(this), typeof(DateTimeConstantAttribute), 0);
					if (customAttributeTypedArgument.ArgumentType != null)
					{
						return new DateTime((long)customAttributeTypedArgument.Value);
					}
				}
				else
				{
					object[] customAttributes = this.GetCustomAttributes(typeof(DateTimeConstantAttribute), false);
					if (customAttributes != null && customAttributes.Length != 0)
					{
						return ((DateTimeConstantAttribute)customAttributes[0]).Value;
					}
				}
			}
			if (!System.Reflection.MetadataToken.IsNullToken(this.m_tkParamDef))
			{
				obj = MdConstant.GetValue(this.m_scope, this.m_tkParamDef, this.ParameterType.GetTypeHandleInternal(), raw);
			}
			if (obj == DBNull.Value)
			{
				if (raw)
				{
					using (IEnumerator<CustomAttributeData> enumerator = CustomAttributeData.GetCustomAttributes(this).GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							CustomAttributeData customAttributeData = enumerator.Current;
							Type declaringType = customAttributeData.Constructor.DeclaringType;
							if (declaringType == typeof(DateTimeConstantAttribute))
							{
								obj = DateTimeConstantAttribute.GetRawDateTimeConstant(customAttributeData);
							}
							else if (declaringType == typeof(DecimalConstantAttribute))
							{
								obj = DecimalConstantAttribute.GetRawDecimalConstant(customAttributeData);
							}
							else if (declaringType.IsSubclassOf(RuntimeParameterInfo.s_CustomConstantAttributeType))
							{
								obj = CustomConstantAttribute.GetRawConstant(customAttributeData);
							}
						}
						goto IL_1A7;
					}
				}
				object[] customAttributes2 = this.GetCustomAttributes(RuntimeParameterInfo.s_CustomConstantAttributeType, false);
				if (customAttributes2.Length != 0)
				{
					obj = ((CustomConstantAttribute)customAttributes2[0]).Value;
				}
				else
				{
					customAttributes2 = this.GetCustomAttributes(RuntimeParameterInfo.s_DecimalConstantAttributeType, false);
					if (customAttributes2.Length != 0)
					{
						obj = ((DecimalConstantAttribute)customAttributes2[0]).Value;
					}
				}
			}
			IL_1A7:
			if (obj == DBNull.Value)
			{
				this.m_noDefaultValue = true;
			}
			return obj;
		}

		// Token: 0x0600483F RID: 18495 RVA: 0x00106ACC File Offset: 0x00104CCC
		internal RuntimeModule GetRuntimeModule()
		{
			RuntimeMethodInfo runtimeMethodInfo = this.Member as RuntimeMethodInfo;
			RuntimeConstructorInfo runtimeConstructorInfo = this.Member as RuntimeConstructorInfo;
			RuntimePropertyInfo runtimePropertyInfo = this.Member as RuntimePropertyInfo;
			if (runtimeMethodInfo != null)
			{
				return runtimeMethodInfo.GetRuntimeModule();
			}
			if (runtimeConstructorInfo != null)
			{
				return runtimeConstructorInfo.GetRuntimeModule();
			}
			if (runtimePropertyInfo != null)
			{
				return runtimePropertyInfo.GetRuntimeModule();
			}
			return null;
		}

		// Token: 0x17000B3A RID: 2874
		// (get) Token: 0x06004840 RID: 18496 RVA: 0x00106B2E File Offset: 0x00104D2E
		public override int MetadataToken
		{
			get
			{
				return this.m_tkParamDef;
			}
		}

		// Token: 0x06004841 RID: 18497 RVA: 0x00106B36 File Offset: 0x00104D36
		public override Type[] GetRequiredCustomModifiers()
		{
			return this.m_signature.GetCustomModifiers(this.PositionImpl + 1, true);
		}

		// Token: 0x06004842 RID: 18498 RVA: 0x00106B4C File Offset: 0x00104D4C
		public override Type[] GetOptionalCustomModifiers()
		{
			return this.m_signature.GetCustomModifiers(this.PositionImpl + 1, false);
		}

		// Token: 0x06004843 RID: 18499 RVA: 0x00106B62 File Offset: 0x00104D62
		public override object[] GetCustomAttributes(bool inherit)
		{
			if (System.Reflection.MetadataToken.IsNullToken(this.m_tkParamDef))
			{
				return EmptyArray<object>.Value;
			}
			return CustomAttribute.GetCustomAttributes(this, typeof(object) as RuntimeType);
		}

		// Token: 0x06004844 RID: 18500 RVA: 0x00106B8C File Offset: 0x00104D8C
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			if (attributeType == null)
			{
				throw new ArgumentNullException("attributeType");
			}
			if (System.Reflection.MetadataToken.IsNullToken(this.m_tkParamDef))
			{
				return EmptyArray<object>.Value;
			}
			RuntimeType runtimeType = attributeType.UnderlyingSystemType as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "attributeType");
			}
			return CustomAttribute.GetCustomAttributes(this, runtimeType);
		}

		// Token: 0x06004845 RID: 18501 RVA: 0x00106BF4 File Offset: 0x00104DF4
		[SecuritySafeCritical]
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			if (attributeType == null)
			{
				throw new ArgumentNullException("attributeType");
			}
			if (System.Reflection.MetadataToken.IsNullToken(this.m_tkParamDef))
			{
				return false;
			}
			RuntimeType runtimeType = attributeType.UnderlyingSystemType as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "attributeType");
			}
			return CustomAttribute.IsDefined(this, runtimeType);
		}

		// Token: 0x06004846 RID: 18502 RVA: 0x00106C55 File Offset: 0x00104E55
		public override IList<CustomAttributeData> GetCustomAttributesData()
		{
			return CustomAttributeData.GetCustomAttributesInternal(this);
		}

		// Token: 0x17000B3B RID: 2875
		// (get) Token: 0x06004847 RID: 18503 RVA: 0x00106C60 File Offset: 0x00104E60
		internal RemotingParameterCachedData RemotingCache
		{
			get
			{
				RemotingParameterCachedData remotingParameterCachedData = this.m_cachedData;
				if (remotingParameterCachedData == null)
				{
					remotingParameterCachedData = new RemotingParameterCachedData(this);
					RemotingParameterCachedData remotingParameterCachedData2 = Interlocked.CompareExchange<RemotingParameterCachedData>(ref this.m_cachedData, remotingParameterCachedData, null);
					if (remotingParameterCachedData2 != null)
					{
						remotingParameterCachedData = remotingParameterCachedData2;
					}
				}
				return remotingParameterCachedData;
			}
		}

		// Token: 0x04001DF6 RID: 7670
		private static readonly Type s_DecimalConstantAttributeType = typeof(DecimalConstantAttribute);

		// Token: 0x04001DF7 RID: 7671
		private static readonly Type s_CustomConstantAttributeType = typeof(CustomConstantAttribute);

		// Token: 0x04001DF8 RID: 7672
		[NonSerialized]
		private int m_tkParamDef;

		// Token: 0x04001DF9 RID: 7673
		[NonSerialized]
		private MetadataImport m_scope;

		// Token: 0x04001DFA RID: 7674
		[NonSerialized]
		private Signature m_signature;

		// Token: 0x04001DFB RID: 7675
		[NonSerialized]
		private volatile bool m_nameIsCached;

		// Token: 0x04001DFC RID: 7676
		[NonSerialized]
		private readonly bool m_noMetadata;

		// Token: 0x04001DFD RID: 7677
		[NonSerialized]
		private bool m_noDefaultValue;

		// Token: 0x04001DFE RID: 7678
		[NonSerialized]
		private MethodBase m_originalMember;

		// Token: 0x04001DFF RID: 7679
		private RemotingParameterCachedData m_cachedData;
	}
}
