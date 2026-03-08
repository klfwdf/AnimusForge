using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security;

namespace System.Reflection.Emit
{
	// Token: 0x02000634 RID: 1588
	internal class DynamicScope
	{
		// Token: 0x06004A21 RID: 18977 RVA: 0x0010C507 File Offset: 0x0010A707
		internal DynamicScope()
		{
			this.m_tokens = new List<object>();
			this.m_tokens.Add(null);
		}

		// Token: 0x17000B90 RID: 2960
		internal object this[int token]
		{
			get
			{
				token &= 16777215;
				if (token < 0 || token > this.m_tokens.Count)
				{
					return null;
				}
				return this.m_tokens[token];
			}
		}

		// Token: 0x06004A23 RID: 18979 RVA: 0x0010C551 File Offset: 0x0010A751
		internal int GetTokenFor(VarArgMethod varArgMethod)
		{
			this.m_tokens.Add(varArgMethod);
			return (this.m_tokens.Count - 1) | 167772160;
		}

		// Token: 0x06004A24 RID: 18980 RVA: 0x0010C572 File Offset: 0x0010A772
		internal string GetString(int token)
		{
			return this[token] as string;
		}

		// Token: 0x06004A25 RID: 18981 RVA: 0x0010C580 File Offset: 0x0010A780
		internal byte[] ResolveSignature(int token, int fromMethod)
		{
			if (fromMethod == 0)
			{
				return (byte[])this[token];
			}
			VarArgMethod varArgMethod = this[token] as VarArgMethod;
			if (varArgMethod == null)
			{
				return null;
			}
			return varArgMethod.m_signature.GetSignature(true);
		}

		// Token: 0x06004A26 RID: 18982 RVA: 0x0010C5BC File Offset: 0x0010A7BC
		[SecuritySafeCritical]
		public int GetTokenFor(RuntimeMethodHandle method)
		{
			IRuntimeMethodInfo methodInfo = method.GetMethodInfo();
			RuntimeMethodHandleInternal value = methodInfo.Value;
			if (methodInfo != null && !RuntimeMethodHandle.IsDynamicMethod(value))
			{
				RuntimeType declaringType = RuntimeMethodHandle.GetDeclaringType(value);
				if (declaringType != null && RuntimeTypeHandle.HasInstantiation(declaringType))
				{
					MethodBase methodBase = RuntimeType.GetMethodBase(methodInfo);
					Type genericTypeDefinition = methodBase.DeclaringType.GetGenericTypeDefinition();
					throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_MethodDeclaringTypeGenericLcg"), methodBase, genericTypeDefinition));
				}
			}
			this.m_tokens.Add(method);
			return (this.m_tokens.Count - 1) | 100663296;
		}

		// Token: 0x06004A27 RID: 18983 RVA: 0x0010C650 File Offset: 0x0010A850
		public int GetTokenFor(RuntimeMethodHandle method, RuntimeTypeHandle typeContext)
		{
			this.m_tokens.Add(new GenericMethodInfo(method, typeContext));
			return (this.m_tokens.Count - 1) | 100663296;
		}

		// Token: 0x06004A28 RID: 18984 RVA: 0x0010C677 File Offset: 0x0010A877
		public int GetTokenFor(DynamicMethod method)
		{
			this.m_tokens.Add(method);
			return (this.m_tokens.Count - 1) | 100663296;
		}

		// Token: 0x06004A29 RID: 18985 RVA: 0x0010C698 File Offset: 0x0010A898
		public int GetTokenFor(RuntimeFieldHandle field)
		{
			this.m_tokens.Add(field);
			return (this.m_tokens.Count - 1) | 67108864;
		}

		// Token: 0x06004A2A RID: 18986 RVA: 0x0010C6BE File Offset: 0x0010A8BE
		public int GetTokenFor(RuntimeFieldHandle field, RuntimeTypeHandle typeContext)
		{
			this.m_tokens.Add(new GenericFieldInfo(field, typeContext));
			return (this.m_tokens.Count - 1) | 67108864;
		}

		// Token: 0x06004A2B RID: 18987 RVA: 0x0010C6E5 File Offset: 0x0010A8E5
		public int GetTokenFor(RuntimeTypeHandle type)
		{
			this.m_tokens.Add(type);
			return (this.m_tokens.Count - 1) | 33554432;
		}

		// Token: 0x06004A2C RID: 18988 RVA: 0x0010C70B File Offset: 0x0010A90B
		public int GetTokenFor(string literal)
		{
			this.m_tokens.Add(literal);
			return (this.m_tokens.Count - 1) | 1879048192;
		}

		// Token: 0x06004A2D RID: 18989 RVA: 0x0010C72C File Offset: 0x0010A92C
		public int GetTokenFor(byte[] signature)
		{
			this.m_tokens.Add(signature);
			return (this.m_tokens.Count - 1) | 285212672;
		}

		// Token: 0x04001E96 RID: 7830
		internal List<object> m_tokens;
	}
}
