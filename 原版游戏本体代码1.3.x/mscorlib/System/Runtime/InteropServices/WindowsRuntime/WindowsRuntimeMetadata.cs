using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009F7 RID: 2551
	public static class WindowsRuntimeMetadata
	{
		// Token: 0x060064DC RID: 25820 RVA: 0x001579EA File Offset: 0x00155BEA
		[SecurityCritical]
		public static IEnumerable<string> ResolveNamespace(string namespaceName, IEnumerable<string> packageGraphFilePaths)
		{
			return WindowsRuntimeMetadata.ResolveNamespace(namespaceName, null, packageGraphFilePaths);
		}

		// Token: 0x060064DD RID: 25821 RVA: 0x001579F4 File Offset: 0x00155BF4
		[SecurityCritical]
		public static IEnumerable<string> ResolveNamespace(string namespaceName, string windowsSdkFilePath, IEnumerable<string> packageGraphFilePaths)
		{
			if (namespaceName == null)
			{
				throw new ArgumentNullException("namespaceName");
			}
			string[] array = null;
			if (packageGraphFilePaths != null)
			{
				List<string> list = new List<string>(packageGraphFilePaths);
				array = new string[list.Count];
				int num = 0;
				foreach (string text in list)
				{
					array[num] = text;
					num++;
				}
			}
			string[] result = null;
			WindowsRuntimeMetadata.nResolveNamespace(namespaceName, windowsSdkFilePath, array, (array == null) ? 0 : array.Length, JitHelpers.GetObjectHandleOnStack<string[]>(ref result));
			return result;
		}

		// Token: 0x060064DE RID: 25822
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void nResolveNamespace(string namespaceName, string windowsSdkFilePath, string[] packageGraphFilePaths, int cPackageGraphFilePaths, ObjectHandleOnStack retFileNames);

		// Token: 0x14000020 RID: 32
		// (add) Token: 0x060064DF RID: 25823 RVA: 0x00157A8C File Offset: 0x00155C8C
		// (remove) Token: 0x060064E0 RID: 25824 RVA: 0x00157AC0 File Offset: 0x00155CC0
		[method: SecurityCritical]
		public static event EventHandler<NamespaceResolveEventArgs> ReflectionOnlyNamespaceResolve;

		// Token: 0x060064E1 RID: 25825 RVA: 0x00157AF4 File Offset: 0x00155CF4
		internal static RuntimeAssembly[] OnReflectionOnlyNamespaceResolveEvent(AppDomain appDomain, RuntimeAssembly assembly, string namespaceName)
		{
			EventHandler<NamespaceResolveEventArgs> reflectionOnlyNamespaceResolve = WindowsRuntimeMetadata.ReflectionOnlyNamespaceResolve;
			if (reflectionOnlyNamespaceResolve != null)
			{
				Delegate[] invocationList = reflectionOnlyNamespaceResolve.GetInvocationList();
				int num = invocationList.Length;
				for (int i = 0; i < num; i++)
				{
					NamespaceResolveEventArgs namespaceResolveEventArgs = new NamespaceResolveEventArgs(namespaceName, assembly);
					((EventHandler<NamespaceResolveEventArgs>)invocationList[i])(appDomain, namespaceResolveEventArgs);
					Collection<Assembly> resolvedAssemblies = namespaceResolveEventArgs.ResolvedAssemblies;
					if (resolvedAssemblies.Count > 0)
					{
						RuntimeAssembly[] array = new RuntimeAssembly[resolvedAssemblies.Count];
						int num2 = 0;
						foreach (Assembly asm in resolvedAssemblies)
						{
							array[num2] = AppDomain.GetRuntimeAssembly(asm);
							num2++;
						}
						return array;
					}
				}
			}
			return null;
		}

		// Token: 0x14000021 RID: 33
		// (add) Token: 0x060064E2 RID: 25826 RVA: 0x00157BB8 File Offset: 0x00155DB8
		// (remove) Token: 0x060064E3 RID: 25827 RVA: 0x00157BEC File Offset: 0x00155DEC
		[method: SecurityCritical]
		public static event EventHandler<DesignerNamespaceResolveEventArgs> DesignerNamespaceResolve;

		// Token: 0x060064E4 RID: 25828 RVA: 0x00157C20 File Offset: 0x00155E20
		internal static string[] OnDesignerNamespaceResolveEvent(AppDomain appDomain, string namespaceName)
		{
			EventHandler<DesignerNamespaceResolveEventArgs> designerNamespaceResolve = WindowsRuntimeMetadata.DesignerNamespaceResolve;
			if (designerNamespaceResolve != null)
			{
				Delegate[] invocationList = designerNamespaceResolve.GetInvocationList();
				int num = invocationList.Length;
				for (int i = 0; i < num; i++)
				{
					DesignerNamespaceResolveEventArgs designerNamespaceResolveEventArgs = new DesignerNamespaceResolveEventArgs(namespaceName);
					((EventHandler<DesignerNamespaceResolveEventArgs>)invocationList[i])(appDomain, designerNamespaceResolveEventArgs);
					Collection<string> resolvedAssemblyFiles = designerNamespaceResolveEventArgs.ResolvedAssemblyFiles;
					if (resolvedAssemblyFiles.Count > 0)
					{
						string[] array = new string[resolvedAssemblyFiles.Count];
						int num2 = 0;
						foreach (string text in resolvedAssemblyFiles)
						{
							if (string.IsNullOrEmpty(text))
							{
								throw new ArgumentException(Environment.GetResourceString("Arg_EmptyOrNullString"), "DesignerNamespaceResolveEventArgs.ResolvedAssemblyFiles");
							}
							array[num2] = text;
							num2++;
						}
						return array;
					}
				}
			}
			return null;
		}
	}
}
