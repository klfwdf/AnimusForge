using System;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Activation;
using System.Security;
using System.Security.Permissions;

namespace System.Runtime.Remoting
{
	// Token: 0x020007BF RID: 1983
	[ComVisible(true)]
	public static class RemotingConfiguration
	{
		// Token: 0x060055D4 RID: 21972 RVA: 0x00130E3A File Offset: 0x0012F03A
		[SecuritySafeCritical]
		[Obsolete("Use System.Runtime.Remoting.RemotingConfiguration.Configure(string fileName, bool ensureSecurity) instead.", false)]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
		public static void Configure(string filename)
		{
			RemotingConfiguration.Configure(filename, false);
		}

		// Token: 0x060055D5 RID: 21973 RVA: 0x00130E43 File Offset: 0x0012F043
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
		public static void Configure(string filename, bool ensureSecurity)
		{
			RemotingConfigHandler.DoConfiguration(filename, ensureSecurity);
			RemotingServices.InternalSetRemoteActivationConfigured();
		}

		// Token: 0x17000E25 RID: 3621
		// (get) Token: 0x060055D6 RID: 21974 RVA: 0x00130E51 File Offset: 0x0012F051
		// (set) Token: 0x060055D7 RID: 21975 RVA: 0x00130E61 File Offset: 0x0012F061
		public static string ApplicationName
		{
			get
			{
				if (!RemotingConfigHandler.HasApplicationNameBeenSet())
				{
					return null;
				}
				return RemotingConfigHandler.ApplicationName;
			}
			[SecuritySafeCritical]
			[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
			set
			{
				RemotingConfigHandler.ApplicationName = value;
			}
		}

		// Token: 0x17000E26 RID: 3622
		// (get) Token: 0x060055D8 RID: 21976 RVA: 0x00130E69 File Offset: 0x0012F069
		public static string ApplicationId
		{
			[SecurityCritical]
			get
			{
				return Identity.AppDomainUniqueId;
			}
		}

		// Token: 0x17000E27 RID: 3623
		// (get) Token: 0x060055D9 RID: 21977 RVA: 0x00130E70 File Offset: 0x0012F070
		public static string ProcessId
		{
			[SecurityCritical]
			get
			{
				return Identity.ProcessGuid;
			}
		}

		// Token: 0x17000E28 RID: 3624
		// (get) Token: 0x060055DA RID: 21978 RVA: 0x00130E77 File Offset: 0x0012F077
		// (set) Token: 0x060055DB RID: 21979 RVA: 0x00130E7E File Offset: 0x0012F07E
		public static CustomErrorsModes CustomErrorsMode
		{
			get
			{
				return RemotingConfigHandler.CustomErrorsMode;
			}
			[SecuritySafeCritical]
			[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
			set
			{
				RemotingConfigHandler.CustomErrorsMode = value;
			}
		}

		// Token: 0x060055DC RID: 21980 RVA: 0x00130E88 File Offset: 0x0012F088
		public static bool CustomErrorsEnabled(bool isLocalRequest)
		{
			switch (RemotingConfiguration.CustomErrorsMode)
			{
			case CustomErrorsModes.On:
				return true;
			case CustomErrorsModes.Off:
				return false;
			case CustomErrorsModes.RemoteOnly:
				return !isLocalRequest;
			default:
				return true;
			}
		}

		// Token: 0x060055DD RID: 21981 RVA: 0x00130EBC File Offset: 0x0012F0BC
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
		public static void RegisterActivatedServiceType(Type type)
		{
			ActivatedServiceTypeEntry entry = new ActivatedServiceTypeEntry(type);
			RemotingConfiguration.RegisterActivatedServiceType(entry);
		}

		// Token: 0x060055DE RID: 21982 RVA: 0x00130ED6 File Offset: 0x0012F0D6
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
		public static void RegisterActivatedServiceType(ActivatedServiceTypeEntry entry)
		{
			RemotingConfigHandler.RegisterActivatedServiceType(entry);
			if (!RemotingConfiguration.s_ListeningForActivationRequests)
			{
				RemotingConfiguration.s_ListeningForActivationRequests = true;
				ActivationServices.StartListeningForRemoteRequests();
			}
		}

		// Token: 0x060055DF RID: 21983 RVA: 0x00130EF4 File Offset: 0x0012F0F4
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
		public static void RegisterWellKnownServiceType(Type type, string objectUri, WellKnownObjectMode mode)
		{
			WellKnownServiceTypeEntry entry = new WellKnownServiceTypeEntry(type, objectUri, mode);
			RemotingConfiguration.RegisterWellKnownServiceType(entry);
		}

		// Token: 0x060055E0 RID: 21984 RVA: 0x00130F10 File Offset: 0x0012F110
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
		public static void RegisterWellKnownServiceType(WellKnownServiceTypeEntry entry)
		{
			RemotingConfigHandler.RegisterWellKnownServiceType(entry);
		}

		// Token: 0x060055E1 RID: 21985 RVA: 0x00130F18 File Offset: 0x0012F118
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
		public static void RegisterActivatedClientType(Type type, string appUrl)
		{
			ActivatedClientTypeEntry entry = new ActivatedClientTypeEntry(type, appUrl);
			RemotingConfiguration.RegisterActivatedClientType(entry);
		}

		// Token: 0x060055E2 RID: 21986 RVA: 0x00130F33 File Offset: 0x0012F133
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
		public static void RegisterActivatedClientType(ActivatedClientTypeEntry entry)
		{
			RemotingConfigHandler.RegisterActivatedClientType(entry);
			RemotingServices.InternalSetRemoteActivationConfigured();
		}

		// Token: 0x060055E3 RID: 21987 RVA: 0x00130F40 File Offset: 0x0012F140
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
		public static void RegisterWellKnownClientType(Type type, string objectUrl)
		{
			WellKnownClientTypeEntry entry = new WellKnownClientTypeEntry(type, objectUrl);
			RemotingConfiguration.RegisterWellKnownClientType(entry);
		}

		// Token: 0x060055E4 RID: 21988 RVA: 0x00130F5B File Offset: 0x0012F15B
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
		public static void RegisterWellKnownClientType(WellKnownClientTypeEntry entry)
		{
			RemotingConfigHandler.RegisterWellKnownClientType(entry);
			RemotingServices.InternalSetRemoteActivationConfigured();
		}

		// Token: 0x060055E5 RID: 21989 RVA: 0x00130F68 File Offset: 0x0012F168
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
		public static ActivatedServiceTypeEntry[] GetRegisteredActivatedServiceTypes()
		{
			return RemotingConfigHandler.GetRegisteredActivatedServiceTypes();
		}

		// Token: 0x060055E6 RID: 21990 RVA: 0x00130F6F File Offset: 0x0012F16F
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
		public static WellKnownServiceTypeEntry[] GetRegisteredWellKnownServiceTypes()
		{
			return RemotingConfigHandler.GetRegisteredWellKnownServiceTypes();
		}

		// Token: 0x060055E7 RID: 21991 RVA: 0x00130F76 File Offset: 0x0012F176
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
		public static ActivatedClientTypeEntry[] GetRegisteredActivatedClientTypes()
		{
			return RemotingConfigHandler.GetRegisteredActivatedClientTypes();
		}

		// Token: 0x060055E8 RID: 21992 RVA: 0x00130F7D File Offset: 0x0012F17D
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
		public static WellKnownClientTypeEntry[] GetRegisteredWellKnownClientTypes()
		{
			return RemotingConfigHandler.GetRegisteredWellKnownClientTypes();
		}

		// Token: 0x060055E9 RID: 21993 RVA: 0x00130F84 File Offset: 0x0012F184
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
		public static ActivatedClientTypeEntry IsRemotelyActivatedClientType(Type svrType)
		{
			if (svrType == null)
			{
				throw new ArgumentNullException("svrType");
			}
			RuntimeType runtimeType = svrType as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"));
			}
			return RemotingConfigHandler.IsRemotelyActivatedClientType(runtimeType);
		}

		// Token: 0x060055EA RID: 21994 RVA: 0x00130FCB File Offset: 0x0012F1CB
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
		public static ActivatedClientTypeEntry IsRemotelyActivatedClientType(string typeName, string assemblyName)
		{
			return RemotingConfigHandler.IsRemotelyActivatedClientType(typeName, assemblyName);
		}

		// Token: 0x060055EB RID: 21995 RVA: 0x00130FD4 File Offset: 0x0012F1D4
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
		public static WellKnownClientTypeEntry IsWellKnownClientType(Type svrType)
		{
			if (svrType == null)
			{
				throw new ArgumentNullException("svrType");
			}
			RuntimeType runtimeType = svrType as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"));
			}
			return RemotingConfigHandler.IsWellKnownClientType(runtimeType);
		}

		// Token: 0x060055EC RID: 21996 RVA: 0x0013101B File Offset: 0x0012F21B
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
		public static WellKnownClientTypeEntry IsWellKnownClientType(string typeName, string assemblyName)
		{
			return RemotingConfigHandler.IsWellKnownClientType(typeName, assemblyName);
		}

		// Token: 0x060055ED RID: 21997 RVA: 0x00131024 File Offset: 0x0012F224
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
		public static bool IsActivationAllowed(Type svrType)
		{
			RuntimeType runtimeType = svrType as RuntimeType;
			if (svrType != null && runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"));
			}
			return RemotingConfigHandler.IsActivationAllowed(runtimeType);
		}

		// Token: 0x0400277D RID: 10109
		private static volatile bool s_ListeningForActivationRequests;
	}
}
