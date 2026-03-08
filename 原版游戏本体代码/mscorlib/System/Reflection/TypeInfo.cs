using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x02000627 RID: 1575
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public abstract class TypeInfo : Type, IReflectableType
	{
		// Token: 0x060048F2 RID: 18674 RVA: 0x00107D70 File Offset: 0x00105F70
		[FriendAccessAllowed]
		internal TypeInfo()
		{
		}

		// Token: 0x060048F3 RID: 18675 RVA: 0x00107D78 File Offset: 0x00105F78
		[__DynamicallyInvokable]
		TypeInfo IReflectableType.GetTypeInfo()
		{
			return this;
		}

		// Token: 0x060048F4 RID: 18676 RVA: 0x00107D7B File Offset: 0x00105F7B
		[__DynamicallyInvokable]
		public virtual Type AsType()
		{
			return this;
		}

		// Token: 0x17000B62 RID: 2914
		// (get) Token: 0x060048F5 RID: 18677 RVA: 0x00107D7E File Offset: 0x00105F7E
		[__DynamicallyInvokable]
		public virtual Type[] GenericTypeParameters
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.IsGenericTypeDefinition)
				{
					return this.GetGenericArguments();
				}
				return Type.EmptyTypes;
			}
		}

		// Token: 0x060048F6 RID: 18678 RVA: 0x00107D94 File Offset: 0x00105F94
		[__DynamicallyInvokable]
		public virtual bool IsAssignableFrom(TypeInfo typeInfo)
		{
			if (typeInfo == null)
			{
				return false;
			}
			if (this == typeInfo)
			{
				return true;
			}
			if (typeInfo.IsSubclassOf(this))
			{
				return true;
			}
			if (base.IsInterface)
			{
				return typeInfo.ImplementInterface(this);
			}
			if (this.IsGenericParameter)
			{
				Type[] genericParameterConstraints = this.GetGenericParameterConstraints();
				for (int i = 0; i < genericParameterConstraints.Length; i++)
				{
					if (!genericParameterConstraints[i].IsAssignableFrom(typeInfo))
					{
						return false;
					}
				}
				return true;
			}
			return false;
		}

		// Token: 0x060048F7 RID: 18679 RVA: 0x00107DFF File Offset: 0x00105FFF
		[__DynamicallyInvokable]
		public virtual EventInfo GetDeclaredEvent(string name)
		{
			return this.GetEvent(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		}

		// Token: 0x060048F8 RID: 18680 RVA: 0x00107E0A File Offset: 0x0010600A
		[__DynamicallyInvokable]
		public virtual FieldInfo GetDeclaredField(string name)
		{
			return this.GetField(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		}

		// Token: 0x060048F9 RID: 18681 RVA: 0x00107E15 File Offset: 0x00106015
		[__DynamicallyInvokable]
		public virtual MethodInfo GetDeclaredMethod(string name)
		{
			return base.GetMethod(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		}

		// Token: 0x060048FA RID: 18682 RVA: 0x00107E20 File Offset: 0x00106020
		[__DynamicallyInvokable]
		public virtual IEnumerable<MethodInfo> GetDeclaredMethods(string name)
		{
			foreach (MethodInfo methodInfo in this.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
			{
				if (methodInfo.Name == name)
				{
					yield return methodInfo;
				}
			}
			MethodInfo[] array = null;
			yield break;
		}

		// Token: 0x060048FB RID: 18683 RVA: 0x00107E38 File Offset: 0x00106038
		[__DynamicallyInvokable]
		public virtual TypeInfo GetDeclaredNestedType(string name)
		{
			Type nestedType = this.GetNestedType(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (nestedType == null)
			{
				return null;
			}
			return nestedType.GetTypeInfo();
		}

		// Token: 0x060048FC RID: 18684 RVA: 0x00107E60 File Offset: 0x00106060
		[__DynamicallyInvokable]
		public virtual PropertyInfo GetDeclaredProperty(string name)
		{
			return base.GetProperty(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		}

		// Token: 0x17000B63 RID: 2915
		// (get) Token: 0x060048FD RID: 18685 RVA: 0x00107E6B File Offset: 0x0010606B
		[__DynamicallyInvokable]
		public virtual IEnumerable<ConstructorInfo> DeclaredConstructors
		{
			[__DynamicallyInvokable]
			get
			{
				return this.GetConstructors(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
		}

		// Token: 0x17000B64 RID: 2916
		// (get) Token: 0x060048FE RID: 18686 RVA: 0x00107E75 File Offset: 0x00106075
		[__DynamicallyInvokable]
		public virtual IEnumerable<EventInfo> DeclaredEvents
		{
			[__DynamicallyInvokable]
			get
			{
				return this.GetEvents(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
		}

		// Token: 0x17000B65 RID: 2917
		// (get) Token: 0x060048FF RID: 18687 RVA: 0x00107E7F File Offset: 0x0010607F
		[__DynamicallyInvokable]
		public virtual IEnumerable<FieldInfo> DeclaredFields
		{
			[__DynamicallyInvokable]
			get
			{
				return this.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
		}

		// Token: 0x17000B66 RID: 2918
		// (get) Token: 0x06004900 RID: 18688 RVA: 0x00107E89 File Offset: 0x00106089
		[__DynamicallyInvokable]
		public virtual IEnumerable<MemberInfo> DeclaredMembers
		{
			[__DynamicallyInvokable]
			get
			{
				return this.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
		}

		// Token: 0x17000B67 RID: 2919
		// (get) Token: 0x06004901 RID: 18689 RVA: 0x00107E93 File Offset: 0x00106093
		[__DynamicallyInvokable]
		public virtual IEnumerable<MethodInfo> DeclaredMethods
		{
			[__DynamicallyInvokable]
			get
			{
				return this.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
		}

		// Token: 0x17000B68 RID: 2920
		// (get) Token: 0x06004902 RID: 18690 RVA: 0x00107EA0 File Offset: 0x001060A0
		[__DynamicallyInvokable]
		public virtual IEnumerable<TypeInfo> DeclaredNestedTypes
		{
			[__DynamicallyInvokable]
			get
			{
				foreach (Type type in this.GetNestedTypes(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
				{
					yield return type.GetTypeInfo();
				}
				Type[] array = null;
				yield break;
			}
		}

		// Token: 0x17000B69 RID: 2921
		// (get) Token: 0x06004903 RID: 18691 RVA: 0x00107EBD File Offset: 0x001060BD
		[__DynamicallyInvokable]
		public virtual IEnumerable<PropertyInfo> DeclaredProperties
		{
			[__DynamicallyInvokable]
			get
			{
				return this.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
		}

		// Token: 0x17000B6A RID: 2922
		// (get) Token: 0x06004904 RID: 18692 RVA: 0x00107EC7 File Offset: 0x001060C7
		[__DynamicallyInvokable]
		public virtual IEnumerable<Type> ImplementedInterfaces
		{
			[__DynamicallyInvokable]
			get
			{
				return this.GetInterfaces();
			}
		}
	}
}
