using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Reflection.Emit
{
	// Token: 0x0200062F RID: 1583
	internal class TypeNameBuilder
	{
		// Token: 0x0600498E RID: 18830
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern IntPtr CreateTypeNameBuilder();

		// Token: 0x0600498F RID: 18831
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void ReleaseTypeNameBuilder(IntPtr pAQN);

		// Token: 0x06004990 RID: 18832
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void OpenGenericArguments(IntPtr tnb);

		// Token: 0x06004991 RID: 18833
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void CloseGenericArguments(IntPtr tnb);

		// Token: 0x06004992 RID: 18834
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void OpenGenericArgument(IntPtr tnb);

		// Token: 0x06004993 RID: 18835
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void CloseGenericArgument(IntPtr tnb);

		// Token: 0x06004994 RID: 18836
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void AddName(IntPtr tnb, string name);

		// Token: 0x06004995 RID: 18837
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void AddPointer(IntPtr tnb);

		// Token: 0x06004996 RID: 18838
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void AddByRef(IntPtr tnb);

		// Token: 0x06004997 RID: 18839
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void AddSzArray(IntPtr tnb);

		// Token: 0x06004998 RID: 18840
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void AddArray(IntPtr tnb, int rank);

		// Token: 0x06004999 RID: 18841
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void AddAssemblySpec(IntPtr tnb, string assemblySpec);

		// Token: 0x0600499A RID: 18842
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void ToString(IntPtr tnb, StringHandleOnStack retString);

		// Token: 0x0600499B RID: 18843
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void Clear(IntPtr tnb);

		// Token: 0x0600499C RID: 18844 RVA: 0x0010A9BC File Offset: 0x00108BBC
		[SecuritySafeCritical]
		internal static string ToString(Type type, TypeNameBuilder.Format format)
		{
			if ((format == TypeNameBuilder.Format.FullName || format == TypeNameBuilder.Format.AssemblyQualifiedName) && !type.IsGenericTypeDefinition && type.ContainsGenericParameters)
			{
				return null;
			}
			TypeNameBuilder typeNameBuilder = new TypeNameBuilder(TypeNameBuilder.CreateTypeNameBuilder());
			typeNameBuilder.Clear();
			typeNameBuilder.ConstructAssemblyQualifiedNameWorker(type, format);
			string result = typeNameBuilder.ToString();
			typeNameBuilder.Dispose();
			return result;
		}

		// Token: 0x0600499D RID: 18845 RVA: 0x0010AA0A File Offset: 0x00108C0A
		private TypeNameBuilder(IntPtr typeNameBuilder)
		{
			this.m_typeNameBuilder = typeNameBuilder;
		}

		// Token: 0x0600499E RID: 18846 RVA: 0x0010AA19 File Offset: 0x00108C19
		[SecurityCritical]
		internal void Dispose()
		{
			TypeNameBuilder.ReleaseTypeNameBuilder(this.m_typeNameBuilder);
		}

		// Token: 0x0600499F RID: 18847 RVA: 0x0010AA28 File Offset: 0x00108C28
		[SecurityCritical]
		private void AddElementType(Type elementType)
		{
			if (elementType.HasElementType)
			{
				this.AddElementType(elementType.GetElementType());
			}
			if (elementType.IsPointer)
			{
				this.AddPointer();
				return;
			}
			if (elementType.IsByRef)
			{
				this.AddByRef();
				return;
			}
			if (elementType.IsSzArray)
			{
				this.AddSzArray();
				return;
			}
			if (elementType.IsArray)
			{
				this.AddArray(elementType.GetArrayRank());
			}
		}

		// Token: 0x060049A0 RID: 18848 RVA: 0x0010AA8C File Offset: 0x00108C8C
		[SecurityCritical]
		private void ConstructAssemblyQualifiedNameWorker(Type type, TypeNameBuilder.Format format)
		{
			Type type2 = type;
			while (type2.HasElementType)
			{
				type2 = type2.GetElementType();
			}
			List<Type> list = new List<Type>();
			Type type3 = type2;
			while (type3 != null)
			{
				list.Add(type3);
				type3 = (type3.IsGenericParameter ? null : type3.DeclaringType);
			}
			for (int i = list.Count - 1; i >= 0; i--)
			{
				Type type4 = list[i];
				string text = type4.Name;
				if (i == list.Count - 1 && type4.Namespace != null && type4.Namespace.Length != 0)
				{
					text = type4.Namespace + "." + text;
				}
				this.AddName(text);
			}
			if (type2.IsGenericType && (!type2.IsGenericTypeDefinition || format == TypeNameBuilder.Format.ToString))
			{
				Type[] genericArguments = type2.GetGenericArguments();
				this.OpenGenericArguments();
				for (int j = 0; j < genericArguments.Length; j++)
				{
					TypeNameBuilder.Format format2 = ((format == TypeNameBuilder.Format.FullName) ? TypeNameBuilder.Format.AssemblyQualifiedName : format);
					this.OpenGenericArgument();
					this.ConstructAssemblyQualifiedNameWorker(genericArguments[j], format2);
					this.CloseGenericArgument();
				}
				this.CloseGenericArguments();
			}
			this.AddElementType(type);
			if (format == TypeNameBuilder.Format.AssemblyQualifiedName)
			{
				this.AddAssemblySpec(type.Module.Assembly.FullName);
			}
		}

		// Token: 0x060049A1 RID: 18849 RVA: 0x0010ABBA File Offset: 0x00108DBA
		[SecurityCritical]
		private void OpenGenericArguments()
		{
			TypeNameBuilder.OpenGenericArguments(this.m_typeNameBuilder);
		}

		// Token: 0x060049A2 RID: 18850 RVA: 0x0010ABC7 File Offset: 0x00108DC7
		[SecurityCritical]
		private void CloseGenericArguments()
		{
			TypeNameBuilder.CloseGenericArguments(this.m_typeNameBuilder);
		}

		// Token: 0x060049A3 RID: 18851 RVA: 0x0010ABD4 File Offset: 0x00108DD4
		[SecurityCritical]
		private void OpenGenericArgument()
		{
			TypeNameBuilder.OpenGenericArgument(this.m_typeNameBuilder);
		}

		// Token: 0x060049A4 RID: 18852 RVA: 0x0010ABE1 File Offset: 0x00108DE1
		[SecurityCritical]
		private void CloseGenericArgument()
		{
			TypeNameBuilder.CloseGenericArgument(this.m_typeNameBuilder);
		}

		// Token: 0x060049A5 RID: 18853 RVA: 0x0010ABEE File Offset: 0x00108DEE
		[SecurityCritical]
		private void AddName(string name)
		{
			TypeNameBuilder.AddName(this.m_typeNameBuilder, name);
		}

		// Token: 0x060049A6 RID: 18854 RVA: 0x0010ABFC File Offset: 0x00108DFC
		[SecurityCritical]
		private void AddPointer()
		{
			TypeNameBuilder.AddPointer(this.m_typeNameBuilder);
		}

		// Token: 0x060049A7 RID: 18855 RVA: 0x0010AC09 File Offset: 0x00108E09
		[SecurityCritical]
		private void AddByRef()
		{
			TypeNameBuilder.AddByRef(this.m_typeNameBuilder);
		}

		// Token: 0x060049A8 RID: 18856 RVA: 0x0010AC16 File Offset: 0x00108E16
		[SecurityCritical]
		private void AddSzArray()
		{
			TypeNameBuilder.AddSzArray(this.m_typeNameBuilder);
		}

		// Token: 0x060049A9 RID: 18857 RVA: 0x0010AC23 File Offset: 0x00108E23
		[SecurityCritical]
		private void AddArray(int rank)
		{
			TypeNameBuilder.AddArray(this.m_typeNameBuilder, rank);
		}

		// Token: 0x060049AA RID: 18858 RVA: 0x0010AC31 File Offset: 0x00108E31
		[SecurityCritical]
		private void AddAssemblySpec(string assemblySpec)
		{
			TypeNameBuilder.AddAssemblySpec(this.m_typeNameBuilder, assemblySpec);
		}

		// Token: 0x060049AB RID: 18859 RVA: 0x0010AC40 File Offset: 0x00108E40
		[SecuritySafeCritical]
		public override string ToString()
		{
			string result = null;
			TypeNameBuilder.ToString(this.m_typeNameBuilder, JitHelpers.GetStringHandleOnStack(ref result));
			return result;
		}

		// Token: 0x060049AC RID: 18860 RVA: 0x0010AC62 File Offset: 0x00108E62
		[SecurityCritical]
		private void Clear()
		{
			TypeNameBuilder.Clear(this.m_typeNameBuilder);
		}

		// Token: 0x04001E83 RID: 7811
		private IntPtr m_typeNameBuilder;

		// Token: 0x02000C3E RID: 3134
		internal enum Format
		{
			// Token: 0x0400374B RID: 14155
			ToString,
			// Token: 0x0400374C RID: 14156
			FullName,
			// Token: 0x0400374D RID: 14157
			AssemblyQualifiedName
		}
	}
}
