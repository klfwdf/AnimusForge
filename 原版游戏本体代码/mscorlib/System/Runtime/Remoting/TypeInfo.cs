using System;
using System.Security;

namespace System.Runtime.Remoting
{
	// Token: 0x020007B8 RID: 1976
	[Serializable]
	internal class TypeInfo : IRemotingTypeInfo
	{
		// Token: 0x17000E18 RID: 3608
		// (get) Token: 0x0600558C RID: 21900 RVA: 0x0012FE90 File Offset: 0x0012E090
		// (set) Token: 0x0600558D RID: 21901 RVA: 0x0012FE98 File Offset: 0x0012E098
		public virtual string TypeName
		{
			[SecurityCritical]
			get
			{
				return this.serverType;
			}
			[SecurityCritical]
			set
			{
				this.serverType = value;
			}
		}

		// Token: 0x0600558E RID: 21902 RVA: 0x0012FEA4 File Offset: 0x0012E0A4
		[SecurityCritical]
		public virtual bool CanCastTo(Type castType, object o)
		{
			if (null != castType)
			{
				if (castType == typeof(MarshalByRefObject) || castType == typeof(object))
				{
					return true;
				}
				if (castType.IsInterface)
				{
					return this.interfacesImplemented != null && this.CanCastTo(castType, this.InterfacesImplemented);
				}
				if (castType.IsMarshalByRef)
				{
					if (this.CompareTypes(castType, this.serverType))
					{
						return true;
					}
					if (this.serverHierarchy != null && this.CanCastTo(castType, this.ServerHierarchy))
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x0600558F RID: 21903 RVA: 0x0012FF33 File Offset: 0x0012E133
		[SecurityCritical]
		internal static string GetQualifiedTypeName(RuntimeType type)
		{
			if (type == null)
			{
				return null;
			}
			return RemotingServices.GetDefaultQualifiedTypeName(type);
		}

		// Token: 0x06005590 RID: 21904 RVA: 0x0012FF48 File Offset: 0x0012E148
		internal static bool ParseTypeAndAssembly(string typeAndAssembly, out string typeName, out string assemName)
		{
			if (typeAndAssembly == null)
			{
				typeName = null;
				assemName = null;
				return false;
			}
			int num = typeAndAssembly.IndexOf(',');
			if (num == -1)
			{
				typeName = typeAndAssembly;
				assemName = null;
				return true;
			}
			typeName = typeAndAssembly.Substring(0, num);
			assemName = typeAndAssembly.Substring(num + 1).Trim();
			return true;
		}

		// Token: 0x06005591 RID: 21905 RVA: 0x0012FF90 File Offset: 0x0012E190
		[SecurityCritical]
		internal TypeInfo(RuntimeType typeOfObj)
		{
			this.ServerType = TypeInfo.GetQualifiedTypeName(typeOfObj);
			RuntimeType runtimeType = (RuntimeType)typeOfObj.BaseType;
			int num = 0;
			while (runtimeType != typeof(MarshalByRefObject) && runtimeType != null)
			{
				runtimeType = (RuntimeType)runtimeType.BaseType;
				num++;
			}
			string[] array = null;
			if (num > 0)
			{
				array = new string[num];
				runtimeType = (RuntimeType)typeOfObj.BaseType;
				for (int i = 0; i < num; i++)
				{
					array[i] = TypeInfo.GetQualifiedTypeName(runtimeType);
					runtimeType = (RuntimeType)runtimeType.BaseType;
				}
			}
			this.ServerHierarchy = array;
			Type[] interfaces = typeOfObj.GetInterfaces();
			string[] array2 = null;
			bool isInterface = typeOfObj.IsInterface;
			if (interfaces.Length != 0 || isInterface)
			{
				array2 = new string[interfaces.Length + (isInterface ? 1 : 0)];
				for (int j = 0; j < interfaces.Length; j++)
				{
					array2[j] = TypeInfo.GetQualifiedTypeName((RuntimeType)interfaces[j]);
				}
				if (isInterface)
				{
					array2[array2.Length - 1] = TypeInfo.GetQualifiedTypeName(typeOfObj);
				}
			}
			this.InterfacesImplemented = array2;
		}

		// Token: 0x17000E19 RID: 3609
		// (get) Token: 0x06005592 RID: 21906 RVA: 0x0013009F File Offset: 0x0012E29F
		// (set) Token: 0x06005593 RID: 21907 RVA: 0x001300A7 File Offset: 0x0012E2A7
		internal string ServerType
		{
			get
			{
				return this.serverType;
			}
			set
			{
				this.serverType = value;
			}
		}

		// Token: 0x17000E1A RID: 3610
		// (get) Token: 0x06005594 RID: 21908 RVA: 0x001300B0 File Offset: 0x0012E2B0
		// (set) Token: 0x06005595 RID: 21909 RVA: 0x001300B8 File Offset: 0x0012E2B8
		private string[] ServerHierarchy
		{
			get
			{
				return this.serverHierarchy;
			}
			set
			{
				this.serverHierarchy = value;
			}
		}

		// Token: 0x17000E1B RID: 3611
		// (get) Token: 0x06005596 RID: 21910 RVA: 0x001300C1 File Offset: 0x0012E2C1
		// (set) Token: 0x06005597 RID: 21911 RVA: 0x001300C9 File Offset: 0x0012E2C9
		private string[] InterfacesImplemented
		{
			get
			{
				return this.interfacesImplemented;
			}
			set
			{
				this.interfacesImplemented = value;
			}
		}

		// Token: 0x06005598 RID: 21912 RVA: 0x001300D4 File Offset: 0x0012E2D4
		[SecurityCritical]
		private bool CompareTypes(Type type1, string type2)
		{
			Type right = RemotingServices.InternalGetTypeFromQualifiedTypeName(type2);
			return type1 == right;
		}

		// Token: 0x06005599 RID: 21913 RVA: 0x001300F0 File Offset: 0x0012E2F0
		[SecurityCritical]
		private bool CanCastTo(Type castType, string[] types)
		{
			bool result = false;
			if (null != castType)
			{
				for (int i = 0; i < types.Length; i++)
				{
					if (this.CompareTypes(castType, types[i]))
					{
						result = true;
						break;
					}
				}
			}
			return result;
		}

		// Token: 0x04002766 RID: 10086
		private string serverType;

		// Token: 0x04002767 RID: 10087
		private string[] serverHierarchy;

		// Token: 0x04002768 RID: 10088
		private string[] interfacesImplemented;
	}
}
