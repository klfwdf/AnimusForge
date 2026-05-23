using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;

namespace System
{
	// Token: 0x02000155 RID: 341
	[Serializable]
	internal class UnitySerializationHolder : ISerializable, IObjectReference
	{
		// Token: 0x0600155D RID: 5469 RVA: 0x0003E624 File Offset: 0x0003C824
		internal static void GetUnitySerializationInfo(SerializationInfo info, Missing missing)
		{
			info.SetType(typeof(UnitySerializationHolder));
			info.AddValue("UnityType", 3);
		}

		// Token: 0x0600155E RID: 5470 RVA: 0x0003E644 File Offset: 0x0003C844
		internal static RuntimeType AddElementTypes(SerializationInfo info, RuntimeType type)
		{
			List<int> list = new List<int>();
			while (type.HasElementType)
			{
				if (type.IsSzArray)
				{
					list.Add(3);
				}
				else if (type.IsArray)
				{
					list.Add(type.GetArrayRank());
					list.Add(2);
				}
				else if (type.IsPointer)
				{
					list.Add(1);
				}
				else if (type.IsByRef)
				{
					list.Add(4);
				}
				type = (RuntimeType)type.GetElementType();
			}
			info.AddValue("ElementTypes", list.ToArray(), typeof(int[]));
			return type;
		}

		// Token: 0x0600155F RID: 5471 RVA: 0x0003E6D8 File Offset: 0x0003C8D8
		internal Type MakeElementTypes(Type type)
		{
			for (int i = this.m_elementTypes.Length - 1; i >= 0; i--)
			{
				if (this.m_elementTypes[i] == 3)
				{
					type = type.MakeArrayType();
				}
				else if (this.m_elementTypes[i] == 2)
				{
					type = type.MakeArrayType(this.m_elementTypes[--i]);
				}
				else if (this.m_elementTypes[i] == 1)
				{
					type = type.MakePointerType();
				}
				else if (this.m_elementTypes[i] == 4)
				{
					type = type.MakeByRefType();
				}
			}
			return type;
		}

		// Token: 0x06001560 RID: 5472 RVA: 0x0003E75C File Offset: 0x0003C95C
		internal static void GetUnitySerializationInfo(SerializationInfo info, RuntimeType type)
		{
			if (type.GetRootElementType().IsGenericParameter)
			{
				type = UnitySerializationHolder.AddElementTypes(info, type);
				info.SetType(typeof(UnitySerializationHolder));
				info.AddValue("UnityType", 7);
				info.AddValue("GenericParameterPosition", type.GenericParameterPosition);
				info.AddValue("DeclaringMethod", type.DeclaringMethod, typeof(MethodBase));
				info.AddValue("DeclaringType", type.DeclaringType, typeof(Type));
				return;
			}
			int unityType = 4;
			if (!type.IsGenericTypeDefinition && type.ContainsGenericParameters)
			{
				unityType = 8;
				type = UnitySerializationHolder.AddElementTypes(info, type);
				info.AddValue("GenericArguments", type.GetGenericArguments(), typeof(Type[]));
				type = (RuntimeType)type.GetGenericTypeDefinition();
			}
			UnitySerializationHolder.GetUnitySerializationInfo(info, unityType, type.FullName, type.GetRuntimeAssembly());
		}

		// Token: 0x06001561 RID: 5473 RVA: 0x0003E83C File Offset: 0x0003CA3C
		internal static void GetUnitySerializationInfo(SerializationInfo info, int unityType, string data, RuntimeAssembly assembly)
		{
			info.SetType(typeof(UnitySerializationHolder));
			info.AddValue("Data", data, typeof(string));
			info.AddValue("UnityType", unityType);
			string value;
			if (assembly == null)
			{
				value = string.Empty;
			}
			else
			{
				value = assembly.FullName;
			}
			info.AddValue("AssemblyName", value);
		}

		// Token: 0x06001562 RID: 5474 RVA: 0x0003E8A0 File Offset: 0x0003CAA0
		internal UnitySerializationHolder(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			this.m_unityType = info.GetInt32("UnityType");
			if (this.m_unityType == 3)
			{
				return;
			}
			if (this.m_unityType == 7)
			{
				this.m_declaringMethod = info.GetValue("DeclaringMethod", typeof(MethodBase)) as MethodBase;
				this.m_declaringType = info.GetValue("DeclaringType", typeof(Type)) as Type;
				this.m_genericParameterPosition = info.GetInt32("GenericParameterPosition");
				this.m_elementTypes = info.GetValue("ElementTypes", typeof(int[])) as int[];
				return;
			}
			if (this.m_unityType == 8)
			{
				this.m_instantiation = info.GetValue("GenericArguments", typeof(Type[])) as Type[];
				this.m_elementTypes = info.GetValue("ElementTypes", typeof(int[])) as int[];
			}
			this.m_data = info.GetString("Data");
			this.m_assemblyName = info.GetString("AssemblyName");
		}

		// Token: 0x06001563 RID: 5475 RVA: 0x0003E9C2 File Offset: 0x0003CBC2
		private void ThrowInsufficientInformation(string field)
		{
			throw new SerializationException(Environment.GetResourceString("Serialization_InsufficientDeserializationState", new object[] { field }));
		}

		// Token: 0x06001564 RID: 5476 RVA: 0x0003E9DD File Offset: 0x0003CBDD
		[SecurityCritical]
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnitySerHolder"));
		}

		// Token: 0x06001565 RID: 5477 RVA: 0x0003E9F0 File Offset: 0x0003CBF0
		[SecurityCritical]
		public virtual object GetRealObject(StreamingContext context)
		{
			switch (this.m_unityType)
			{
			case 1:
				return Empty.Value;
			case 2:
				return DBNull.Value;
			case 3:
				return Missing.Value;
			case 4:
			{
				if (this.m_data == null || this.m_data.Length == 0)
				{
					this.ThrowInsufficientInformation("Data");
				}
				if (this.m_assemblyName == null)
				{
					this.ThrowInsufficientInformation("AssemblyName");
				}
				if (this.m_assemblyName.Length == 0)
				{
					return Type.GetType(this.m_data, true, false);
				}
				Assembly assembly = Assembly.Load(this.m_assemblyName);
				return assembly.GetType(this.m_data, true, false);
			}
			case 5:
			{
				if (this.m_data == null || this.m_data.Length == 0)
				{
					this.ThrowInsufficientInformation("Data");
				}
				if (this.m_assemblyName == null)
				{
					this.ThrowInsufficientInformation("AssemblyName");
				}
				Assembly assembly = Assembly.Load(this.m_assemblyName);
				Module module = assembly.GetModule(this.m_data);
				if (module == null)
				{
					throw new SerializationException(Environment.GetResourceString("Serialization_UnableToFindModule", new object[] { this.m_data, this.m_assemblyName }));
				}
				return module;
			}
			case 6:
				if (this.m_data == null || this.m_data.Length == 0)
				{
					this.ThrowInsufficientInformation("Data");
				}
				if (this.m_assemblyName == null)
				{
					this.ThrowInsufficientInformation("AssemblyName");
				}
				return Assembly.Load(this.m_assemblyName);
			case 7:
				if (this.m_declaringMethod == null && this.m_declaringType == null)
				{
					this.ThrowInsufficientInformation("DeclaringMember");
				}
				if (this.m_declaringMethod != null)
				{
					return this.m_declaringMethod.GetGenericArguments()[this.m_genericParameterPosition];
				}
				return this.MakeElementTypes(this.m_declaringType.GetGenericArguments()[this.m_genericParameterPosition]);
			case 8:
			{
				this.m_unityType = 4;
				Type type = this.GetRealObject(context) as Type;
				this.m_unityType = 8;
				if (this.m_instantiation[0] == null)
				{
					return null;
				}
				return this.MakeElementTypes(type.MakeGenericType(this.m_instantiation));
			}
			default:
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidUnity"));
			}
		}

		// Token: 0x040006FC RID: 1788
		internal const int EmptyUnity = 1;

		// Token: 0x040006FD RID: 1789
		internal const int NullUnity = 2;

		// Token: 0x040006FE RID: 1790
		internal const int MissingUnity = 3;

		// Token: 0x040006FF RID: 1791
		internal const int RuntimeTypeUnity = 4;

		// Token: 0x04000700 RID: 1792
		internal const int ModuleUnity = 5;

		// Token: 0x04000701 RID: 1793
		internal const int AssemblyUnity = 6;

		// Token: 0x04000702 RID: 1794
		internal const int GenericParameterTypeUnity = 7;

		// Token: 0x04000703 RID: 1795
		internal const int PartialInstantiationTypeUnity = 8;

		// Token: 0x04000704 RID: 1796
		internal const int Pointer = 1;

		// Token: 0x04000705 RID: 1797
		internal const int Array = 2;

		// Token: 0x04000706 RID: 1798
		internal const int SzArray = 3;

		// Token: 0x04000707 RID: 1799
		internal const int ByRef = 4;

		// Token: 0x04000708 RID: 1800
		private Type[] m_instantiation;

		// Token: 0x04000709 RID: 1801
		private int[] m_elementTypes;

		// Token: 0x0400070A RID: 1802
		private int m_genericParameterPosition;

		// Token: 0x0400070B RID: 1803
		private Type m_declaringType;

		// Token: 0x0400070C RID: 1804
		private MethodBase m_declaringMethod;

		// Token: 0x0400070D RID: 1805
		private string m_data;

		// Token: 0x0400070E RID: 1806
		private string m_assemblyName;

		// Token: 0x0400070F RID: 1807
		private int m_unityType;
	}
}
