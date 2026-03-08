using System;
using System.Security.Util;

namespace System.Security
{
	// Token: 0x020001CD RID: 461
	internal static class BuiltInPermissionSets
	{
		// Token: 0x1700032E RID: 814
		// (get) Token: 0x06001C29 RID: 7209 RVA: 0x00060EF1 File Offset: 0x0005F0F1
		internal static NamedPermissionSet Everything
		{
			get
			{
				return BuiltInPermissionSets.GetOrDeserializeExtendablePermissionSet(ref BuiltInPermissionSets.s_everything, BuiltInPermissionSets.s_everythingXml, "<PermissionSet class = \"System.Security.PermissionSet\"\r\n                             version = \"1\">\r\n                  <IPermission class = \"System.Security.Permissions.MediaPermission, WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\"\r\n                               version = \"1\"\r\n                               Unrestricted = \"true\" />\r\n                  <IPermission class = \"System.Security.Permissions.WebBrowserPermission, WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\"\r\n                               version = \"1\"\r\n                               Unrestricted = \"true\" />\r\n              </PermissionSet>");
			}
		}

		// Token: 0x1700032F RID: 815
		// (get) Token: 0x06001C2A RID: 7210 RVA: 0x00060F07 File Offset: 0x0005F107
		internal static NamedPermissionSet Execution
		{
			get
			{
				return BuiltInPermissionSets.GetOrDeserializePermissionSet(ref BuiltInPermissionSets.s_execution, BuiltInPermissionSets.s_executionXml);
			}
		}

		// Token: 0x17000330 RID: 816
		// (get) Token: 0x06001C2B RID: 7211 RVA: 0x00060F18 File Offset: 0x0005F118
		internal static NamedPermissionSet FullTrust
		{
			get
			{
				return BuiltInPermissionSets.GetOrDeserializePermissionSet(ref BuiltInPermissionSets.s_fullTrust, BuiltInPermissionSets.s_fullTrustXml);
			}
		}

		// Token: 0x17000331 RID: 817
		// (get) Token: 0x06001C2C RID: 7212 RVA: 0x00060F29 File Offset: 0x0005F129
		internal static NamedPermissionSet Internet
		{
			get
			{
				return BuiltInPermissionSets.GetOrDeserializeExtendablePermissionSet(ref BuiltInPermissionSets.s_internet, BuiltInPermissionSets.s_internetXml, "<PermissionSet class = \"System.Security.PermissionSet\"\r\n                             version = \"1\">\r\n                  <IPermission class = \"System.Security.Permissions.MediaPermission, WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\"\r\n                               version = \"1\"\r\n                               Audio=\"SafeAudio\" Video=\"SafeVideo\" Image=\"SafeImage\" />\r\n                  <IPermission class = \"System.Security.Permissions.WebBrowserPermission, WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\"\r\n                               version = \"1\"\r\n                               Level=\"Safe\" />\r\n              </PermissionSet>");
			}
		}

		// Token: 0x17000332 RID: 818
		// (get) Token: 0x06001C2D RID: 7213 RVA: 0x00060F3F File Offset: 0x0005F13F
		internal static NamedPermissionSet LocalIntranet
		{
			get
			{
				return BuiltInPermissionSets.GetOrDeserializeExtendablePermissionSet(ref BuiltInPermissionSets.s_localIntranet, BuiltInPermissionSets.s_localIntranetXml, "<PermissionSet class = \"System.Security.PermissionSet\"\r\n                             version = \"1\">\r\n                  <IPermission class = \"System.Security.Permissions.MediaPermission, WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\"\r\n                               version = \"1\"\r\n                               Audio=\"SafeAudio\" Video=\"SafeVideo\" Image=\"SafeImage\" />\r\n                  <IPermission class = \"System.Security.Permissions.WebBrowserPermission, WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\"\r\n                               version = \"1\"\r\n                               Level=\"Safe\" />\r\n              </PermissionSet>");
			}
		}

		// Token: 0x17000333 RID: 819
		// (get) Token: 0x06001C2E RID: 7214 RVA: 0x00060F55 File Offset: 0x0005F155
		internal static NamedPermissionSet Nothing
		{
			get
			{
				return BuiltInPermissionSets.GetOrDeserializePermissionSet(ref BuiltInPermissionSets.s_nothing, BuiltInPermissionSets.s_nothingXml);
			}
		}

		// Token: 0x17000334 RID: 820
		// (get) Token: 0x06001C2F RID: 7215 RVA: 0x00060F66 File Offset: 0x0005F166
		internal static NamedPermissionSet SkipVerification
		{
			get
			{
				return BuiltInPermissionSets.GetOrDeserializePermissionSet(ref BuiltInPermissionSets.s_skipVerification, BuiltInPermissionSets.s_skipVerificationXml);
			}
		}

		// Token: 0x06001C30 RID: 7216 RVA: 0x00060F78 File Offset: 0x0005F178
		private static NamedPermissionSet GetOrDeserializeExtendablePermissionSet(ref NamedPermissionSet permissionSet, string permissionSetXml, string extensionXml)
		{
			if (permissionSet == null)
			{
				SecurityElement permissionSetXml2 = SecurityElement.FromString(permissionSetXml);
				NamedPermissionSet namedPermissionSet = new NamedPermissionSet(permissionSetXml2);
				PermissionSet permissionSetExtensions = BuiltInPermissionSets.GetPermissionSetExtensions(extensionXml);
				namedPermissionSet.InplaceUnion(permissionSetExtensions);
				permissionSet = namedPermissionSet;
			}
			return permissionSet.Copy() as NamedPermissionSet;
		}

		// Token: 0x06001C31 RID: 7217 RVA: 0x00060FB4 File Offset: 0x0005F1B4
		private static NamedPermissionSet GetOrDeserializePermissionSet(ref NamedPermissionSet permissionSet, string permissionSetXml)
		{
			if (permissionSet == null)
			{
				SecurityElement permissionSetXml2 = SecurityElement.FromString(permissionSetXml);
				NamedPermissionSet namedPermissionSet = new NamedPermissionSet(permissionSetXml2);
				permissionSet = namedPermissionSet;
			}
			return permissionSet.Copy() as NamedPermissionSet;
		}

		// Token: 0x06001C32 RID: 7218 RVA: 0x00060FE4 File Offset: 0x0005F1E4
		private static PermissionSet GetPermissionSetExtensions(string extensionXml)
		{
			SecurityElement securityElement = SecurityElement.FromString(extensionXml);
			SecurityElement el = (SecurityElement)securityElement.Children[0];
			if (XMLUtil.GetClassFromElement(el, true) != null)
			{
				return new NamedPermissionSet(securityElement);
			}
			return null;
		}

		// Token: 0x040009CD RID: 2509
		private static readonly string s_everythingXml = "<PermissionSet class = \"System.Security.NamedPermissionSet\"\r\n                             version = \"1\"\r\n                             Name = \"Everything\"\r\n                             Description = \"" + Environment.GetResourceString("Policy_PS_Everything") + "\"\r\n                  <IPermission class = \"System.Data.OleDb.OleDbPermission, System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Unrestricted = \"true\" />\r\n                  <IPermission class = \"System.Data.SqlClient.SqlClientPermission, System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Unrestricted = \"true\" />\r\n                  <IPermission class = \"System.Diagnostics.PerformanceCounterPermission, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Unrestricted = \"true\" />\r\n                  <IPermission class = \"System.Net.DnsPermission, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Unrestricted = \"true\" />\r\n                  <IPermission class = \"System.Net.SocketPermission, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Unrestricted = \"true\" />\r\n                  <IPermission class = \"System.Net.WebPermission, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Unrestricted = \"true\" />\r\n                  <IPermission class = \"System.Security.Permissions.DataProtectionPermission, System.Security, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\"\r\n                               version = \"1\"\r\n                               Unrestricted = \"true\" />\r\n                  <IPermission class = \"System.Security.Permissions.EnvironmentPermission, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Unrestricted = \"true\" />\r\n                  <IPermission class = \"System.Diagnostics.EventLogPermission, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Unrestricted = \"true\" />\r\n                  <IPermission class = \"System.Security.Permissions.FileDialogPermission, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Unrestricted = \"true\" />\r\n                  <IPermission class = \"System.Security.Permissions.FileIOPermission, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Unrestricted = \"true\" /> \r\n                  <IPermission class = \"System.Security.Permissions.IsolatedStorageFilePermission, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Unrestricted = \"true\" />\r\n                  <IPermission class = \"System.Security.Permissions.KeyContainerPermission, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Unrestricted = \"true\" />\r\n                  <IPermission class = \"System.Drawing.Printing.PrintingPermission, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\"\r\n                               version = \"1\"\r\n                               Unrestricted = \"true\" />\r\n                  <IPermission class = \"System.Security.Permissions.ReflectionPermission, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Unrestricted = \"true\" />\r\n                  <IPermission class = \"System.Security.Permissions.RegistryPermission, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Unrestricted = \"true\" />\r\n                  <IPermission class = \"System.Security.Permissions.SecurityPermission, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Flags = \"Assertion, UnmanagedCode, Execution, ControlThread, ControlEvidence, ControlPolicy, ControlAppDomain, SerializationFormatter, ControlDomainPolicy, ControlPrincipal, RemotingConfiguration, Infrastructure, BindingRedirects\" />\r\n                  <IPermission class = \"System.Security.Permissions.UIPermission, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Unrestricted = \"true\" />\r\n                  <IPermission class = \"System.Security.Permissions.StorePermission, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Unrestricted = \"true\" />\r\n                  <IPermission class = \"System.Security.Permissions.TypeDescriptorPermission, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Unrestricted = \"true\" />\r\n               </PermissionSet>";

		// Token: 0x040009CE RID: 2510
		private static readonly string s_executionXml = "<PermissionSet class = \"System.Security.NamedPermissionSet\"\r\n                             version = \"1\"\r\n                             Name = \"Execution\"\r\n                             Description = \"" + Environment.GetResourceString("Policy_PS_Execution") + "\">\r\n                  <IPermission class = \"System.Security.Permissions.SecurityPermission, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Flags = \"Execution\" />\r\n               </PermissionSet>";

		// Token: 0x040009CF RID: 2511
		private static readonly string s_fullTrustXml = "<PermissionSet class = \"System.Security.NamedPermissionSet\" \r\n                             version = \"1\" \r\n                             Unrestricted = \"true\" \r\n                             Name = \"FullTrust\" \r\n                             Description = \"" + Environment.GetResourceString("Policy_PS_FullTrust") + "\" />";

		// Token: 0x040009D0 RID: 2512
		private static readonly string s_internetXml = "<PermissionSet class = \"System.Security.NamedPermissionSet\"\r\n                             version = \"1\"\r\n                             Name = \"Internet\"\r\n                             Description = \"" + Environment.GetResourceString("Policy_PS_Internet") + "\">\r\n                  <IPermission class = \"System.Drawing.Printing.PrintingPermission, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\"\r\n                               version = \"1\"\r\n                               Level = \"SafePrinting\" />\r\n                  <IPermission class = \"System.Security.Permissions.FileDialogPermission, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Access = \"Open\" />\r\n                  <IPermission class = \"System.Security.Permissions.IsolatedStorageFilePermission, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               UserQuota = \"1024000\"\r\n                               Allowed = \"ApplicationIsolationByUser\" />\r\n                  <IPermission class = \"System.Security.Permissions.SecurityPermission, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Flags = \"Execution\" />\r\n                  <IPermission class = \"System.Security.Permissions.UIPermission, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Window = \"SafeTopLevelWindows\"\r\n                               Clipboard = \"OwnClipboard\" />\r\n               </PermissionSet>";

		// Token: 0x040009D1 RID: 2513
		private static readonly string s_localIntranetXml = "<PermissionSet class = \"System.Security.NamedPermissionSet\"\r\n                             version = \"1\"\r\n                             Name = \"LocalIntranet\"\r\n                             Description = \"" + Environment.GetResourceString("Policy_PS_LocalIntranet") + "\" >\r\n                  <IPermission class = \"System.Drawing.Printing.PrintingPermission, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\"\r\n                              version = \"1\"\r\n                              Level = \"DefaultPrinting\" />\r\n                  <IPermission class = \"System.Net.DnsPermission, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Unrestricted = \"true\" />\r\n                  <IPermission class = \"System.Security.Permissions.EnvironmentPermission, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Read = \"USERNAME\" />\r\n                  <IPermission class = \"System.Security.Permissions.FileDialogPermission, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Unrestricted = \"true\" />\r\n                  <IPermission class = \"System.Security.Permissions.IsolatedStorageFilePermission, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Allowed = \"AssemblyIsolationByUser\"\r\n                               UserQuota = \"9223372036854775807\"\r\n                               Expiry = \"9223372036854775807\"\r\n                               Permanent = \"true\" />\r\n                  <IPermission class = \"System.Security.Permissions.ReflectionPermission, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Flags = \"ReflectionEmit, RestrictedMemberAccess\" />\r\n                  <IPermission class = \"System.Security.Permissions.SecurityPermission, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Flags = \"Execution, Assertion, BindingRedirects \" />\r\n                  <IPermission class = \"System.Security.Permissions.TypeDescriptorPermission, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Flags = \"RestrictedRegistrationAccess\" />\r\n                  <IPermission class = \"System.Security.Permissions.UIPermission, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Unrestricted = \"true\" />\r\n               </PermissionSet>";

		// Token: 0x040009D2 RID: 2514
		private static readonly string s_nothingXml = "<PermissionSet class = \"System.Security.NamedPermissionSet\"\r\n                             version = \"1\"\r\n                             Name = \"Nothing\"\r\n                             Description = \"" + Environment.GetResourceString("Policy_PS_Nothing") + "\" />";

		// Token: 0x040009D3 RID: 2515
		private static readonly string s_skipVerificationXml = "<PermissionSet class = \"System.Security.NamedPermissionSet\"\r\n                             version = \"1\"\r\n                             Name = \"SkipVerification\"\r\n                             Description = \"" + Environment.GetResourceString("Policy_PS_SkipVerification") + "\">\r\n                  <IPermission class = \"System.Security.Permissions.SecurityPermission, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\n                               version = \"1\"\r\n                               Flags = \"SkipVerification\" />\r\n               </PermissionSet>";

		// Token: 0x040009D4 RID: 2516
		private const string s_wpfExtensionXml = "<PermissionSet class = \"System.Security.PermissionSet\"\r\n                             version = \"1\">\r\n                  <IPermission class = \"System.Security.Permissions.MediaPermission, WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\"\r\n                               version = \"1\"\r\n                               Audio=\"SafeAudio\" Video=\"SafeVideo\" Image=\"SafeImage\" />\r\n                  <IPermission class = \"System.Security.Permissions.WebBrowserPermission, WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\"\r\n                               version = \"1\"\r\n                               Level=\"Safe\" />\r\n              </PermissionSet>";

		// Token: 0x040009D5 RID: 2517
		private const string s_wpfExtensionUnrestrictedXml = "<PermissionSet class = \"System.Security.PermissionSet\"\r\n                             version = \"1\">\r\n                  <IPermission class = \"System.Security.Permissions.MediaPermission, WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\"\r\n                               version = \"1\"\r\n                               Unrestricted = \"true\" />\r\n                  <IPermission class = \"System.Security.Permissions.WebBrowserPermission, WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\"\r\n                               version = \"1\"\r\n                               Unrestricted = \"true\" />\r\n              </PermissionSet>";

		// Token: 0x040009D6 RID: 2518
		private static NamedPermissionSet s_everything;

		// Token: 0x040009D7 RID: 2519
		private static NamedPermissionSet s_execution;

		// Token: 0x040009D8 RID: 2520
		private static NamedPermissionSet s_fullTrust;

		// Token: 0x040009D9 RID: 2521
		private static NamedPermissionSet s_internet;

		// Token: 0x040009DA RID: 2522
		private static NamedPermissionSet s_localIntranet;

		// Token: 0x040009DB RID: 2523
		private static NamedPermissionSet s_nothing;

		// Token: 0x040009DC RID: 2524
		private static NamedPermissionSet s_skipVerification;
	}
}
