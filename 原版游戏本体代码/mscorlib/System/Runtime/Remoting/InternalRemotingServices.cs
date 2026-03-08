using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Metadata;
using System.Runtime.Serialization;
using System.Security;

namespace System.Runtime.Remoting
{
	// Token: 0x020007CB RID: 1995
	[SecurityCritical]
	[ComVisible(true)]
	public class InternalRemotingServices
	{
		// Token: 0x06005685 RID: 22149 RVA: 0x0013319D File Offset: 0x0013139D
		[SecurityCritical]
		[Conditional("_LOGGING")]
		public static void DebugOutChnl(string s)
		{
			Message.OutToUnmanagedDebugger("CHNL:" + s + "\n");
		}

		// Token: 0x06005686 RID: 22150 RVA: 0x001331B4 File Offset: 0x001313B4
		[Conditional("_LOGGING")]
		public static void RemotingTrace(params object[] messages)
		{
		}

		// Token: 0x06005687 RID: 22151 RVA: 0x001331B6 File Offset: 0x001313B6
		[Conditional("_DEBUG")]
		public static void RemotingAssert(bool condition, string message)
		{
		}

		// Token: 0x06005688 RID: 22152 RVA: 0x001331B8 File Offset: 0x001313B8
		[SecurityCritical]
		[CLSCompliant(false)]
		public static void SetServerIdentity(MethodCall m, object srvID)
		{
			((IInternalMessage)m).ServerIdentityObject = (ServerIdentity)srvID;
		}

		// Token: 0x06005689 RID: 22153 RVA: 0x001331D4 File Offset: 0x001313D4
		internal static RemotingMethodCachedData GetReflectionCachedData(MethodBase mi)
		{
			RuntimeMethodInfo runtimeMethodInfo;
			if ((runtimeMethodInfo = mi as RuntimeMethodInfo) != null)
			{
				return runtimeMethodInfo.RemotingCache;
			}
			RuntimeConstructorInfo runtimeConstructorInfo;
			if ((runtimeConstructorInfo = mi as RuntimeConstructorInfo) != null)
			{
				return runtimeConstructorInfo.RemotingCache;
			}
			throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeReflectionObject"));
		}

		// Token: 0x0600568A RID: 22154 RVA: 0x00133222 File Offset: 0x00131422
		internal static RemotingTypeCachedData GetReflectionCachedData(RuntimeType type)
		{
			return type.RemotingCache;
		}

		// Token: 0x0600568B RID: 22155 RVA: 0x0013322C File Offset: 0x0013142C
		internal static RemotingCachedData GetReflectionCachedData(MemberInfo mi)
		{
			MethodBase mi2;
			if ((mi2 = mi as MethodBase) != null)
			{
				return InternalRemotingServices.GetReflectionCachedData(mi2);
			}
			RuntimeType type;
			if ((type = mi as RuntimeType) != null)
			{
				return InternalRemotingServices.GetReflectionCachedData(type);
			}
			RuntimeFieldInfo runtimeFieldInfo;
			if ((runtimeFieldInfo = mi as RuntimeFieldInfo) != null)
			{
				return runtimeFieldInfo.RemotingCache;
			}
			SerializationFieldInfo serializationFieldInfo;
			if ((serializationFieldInfo = mi as SerializationFieldInfo) != null)
			{
				return serializationFieldInfo.RemotingCache;
			}
			throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeReflectionObject"));
		}

		// Token: 0x0600568C RID: 22156 RVA: 0x001332AC File Offset: 0x001314AC
		internal static RemotingCachedData GetReflectionCachedData(RuntimeParameterInfo reflectionObject)
		{
			return reflectionObject.RemotingCache;
		}

		// Token: 0x0600568D RID: 22157 RVA: 0x001332B4 File Offset: 0x001314B4
		[SecurityCritical]
		public static SoapAttribute GetCachedSoapAttribute(object reflectionObject)
		{
			MemberInfo memberInfo = reflectionObject as MemberInfo;
			RuntimeParameterInfo runtimeParameterInfo = reflectionObject as RuntimeParameterInfo;
			if (memberInfo != null)
			{
				return InternalRemotingServices.GetReflectionCachedData(memberInfo).GetSoapAttribute();
			}
			if (runtimeParameterInfo != null)
			{
				return InternalRemotingServices.GetReflectionCachedData(runtimeParameterInfo).GetSoapAttribute();
			}
			return null;
		}
	}
}
