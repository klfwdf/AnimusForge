using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Serialization;

namespace Newtonsoft.Json.Utilities
{
	// Token: 0x02000068 RID: 104
	[NullableContext(1)]
	[Nullable(0)]
	internal class ReflectionObject
	{
		// Token: 0x170000CE RID: 206
		// (get) Token: 0x06000597 RID: 1431 RVA: 0x00017E89 File Offset: 0x00016089
		[Nullable(new byte[] { 2, 1 })]
		public ObjectConstructor<object> Creator
		{
			[return: Nullable(new byte[] { 2, 1 })]
			get;
		}

		// Token: 0x170000CF RID: 207
		// (get) Token: 0x06000598 RID: 1432 RVA: 0x00017E91 File Offset: 0x00016091
		public IDictionary<string, ReflectionMember> Members { get; }

		// Token: 0x06000599 RID: 1433 RVA: 0x00017E99 File Offset: 0x00016099
		private ReflectionObject([Nullable(new byte[] { 2, 1 })] ObjectConstructor<object> creator)
		{
			this.Members = new Dictionary<string, ReflectionMember>();
			this.Creator = creator;
		}

		// Token: 0x0600059A RID: 1434 RVA: 0x00017EB3 File Offset: 0x000160B3
		[return: Nullable(2)]
		public object GetValue(object target, string member)
		{
			return this.Members[member].Getter(target);
		}

		// Token: 0x0600059B RID: 1435 RVA: 0x00017ECC File Offset: 0x000160CC
		public void SetValue(object target, string member, [Nullable(2)] object value)
		{
			this.Members[member].Setter(target, value);
		}

		// Token: 0x0600059C RID: 1436 RVA: 0x00017EE6 File Offset: 0x000160E6
		public Type GetType(string member)
		{
			return this.Members[member].MemberType;
		}

		// Token: 0x0600059D RID: 1437 RVA: 0x00017EF9 File Offset: 0x000160F9
		public static ReflectionObject Create(Type t, params string[] memberNames)
		{
			return ReflectionObject.Create(t, null, memberNames);
		}

		// Token: 0x0600059E RID: 1438 RVA: 0x00017F04 File Offset: 0x00016104
		public static ReflectionObject Create(Type t, [Nullable(2)] MethodBase creator, params string[] memberNames)
		{
			ReflectionDelegateFactory reflectionDelegateFactory = JsonTypeReflector.ReflectionDelegateFactory;
			ObjectConstructor<object> creator2 = null;
			if (creator != null)
			{
				creator2 = reflectionDelegateFactory.CreateParameterizedConstructor(creator);
			}
			else if (ReflectionUtils.HasDefaultConstructor(t, false))
			{
				Func<object> ctor = reflectionDelegateFactory.CreateDefaultConstructor<object>(t);
				creator2 = ([Nullable(new byte[] { 1, 2 })] object[] args) => ctor();
			}
			ReflectionObject reflectionObject = new ReflectionObject(creator2);
			int i = 0;
			while (i < memberNames.Length)
			{
				string text = memberNames[i];
				MemberInfo[] member = t.GetMember(text, BindingFlags.Instance | BindingFlags.Public);
				if (member.Length != 1)
				{
					throw new ArgumentException("Expected a single member with the name '{0}'.".FormatWith(CultureInfo.InvariantCulture, text));
				}
				MemberInfo memberInfo = member.Single<MemberInfo>();
				ReflectionMember reflectionMember = new ReflectionMember();
				MemberTypes memberTypes = memberInfo.MemberType();
				if (memberTypes == MemberTypes.Field)
				{
					goto IL_AA;
				}
				if (memberTypes != MemberTypes.Method)
				{
					if (memberTypes == MemberTypes.Property)
					{
						goto IL_AA;
					}
					throw new ArgumentException("Unexpected member type '{0}' for member '{1}'.".FormatWith(CultureInfo.InvariantCulture, memberInfo.MemberType(), memberInfo.Name));
				}
				else
				{
					MethodInfo methodInfo = (MethodInfo)memberInfo;
					if (methodInfo.IsPublic)
					{
						ParameterInfo[] parameters = methodInfo.GetParameters();
						if (parameters.Length == 0 && methodInfo.ReturnType != typeof(void))
						{
							MethodCall<object, object> call = reflectionDelegateFactory.CreateMethodCall<object>(methodInfo);
							reflectionMember.Getter = (object target) => call(target, new object[0]);
						}
						else if (parameters.Length == 1 && methodInfo.ReturnType == typeof(void))
						{
							MethodCall<object, object> call = reflectionDelegateFactory.CreateMethodCall<object>(methodInfo);
							reflectionMember.Setter = delegate(object target, [Nullable(2)] object arg)
							{
								call(target, new object[] { arg });
							};
						}
					}
				}
				IL_1BF:
				reflectionMember.MemberType = ReflectionUtils.GetMemberUnderlyingType(memberInfo);
				reflectionObject.Members[text] = reflectionMember;
				i++;
				continue;
				IL_AA:
				if (ReflectionUtils.CanReadMemberValue(memberInfo, false))
				{
					reflectionMember.Getter = reflectionDelegateFactory.CreateGet<object>(memberInfo);
				}
				if (ReflectionUtils.CanSetMemberValue(memberInfo, false, false))
				{
					reflectionMember.Setter = reflectionDelegateFactory.CreateSet<object>(memberInfo);
					goto IL_1BF;
				}
				goto IL_1BF;
			}
			return reflectionObject;
		}
	}
}
