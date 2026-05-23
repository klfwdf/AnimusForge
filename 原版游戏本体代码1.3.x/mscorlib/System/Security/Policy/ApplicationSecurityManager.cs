using System;
using System.Deployment.Internal.Isolation.Manifest;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security.Util;

namespace System.Security.Policy
{
	// Token: 0x02000344 RID: 836
	[ComVisible(true)]
	public static class ApplicationSecurityManager
	{
		// Token: 0x0600298D RID: 10637 RVA: 0x0009975C File Offset: 0x0009795C
		[SecurityCritical]
		[SecurityPermission(SecurityAction.Assert, Unrestricted = true)]
		public static bool DetermineApplicationTrust(ActivationContext activationContext, TrustManagerContext context)
		{
			if (activationContext == null)
			{
				throw new ArgumentNullException("activationContext");
			}
			AppDomainManager domainManager = AppDomain.CurrentDomain.DomainManager;
			ApplicationTrust applicationTrust;
			if (domainManager != null)
			{
				HostSecurityManager hostSecurityManager = domainManager.HostSecurityManager;
				if (hostSecurityManager != null && (hostSecurityManager.Flags & HostSecurityManagerOptions.HostDetermineApplicationTrust) == HostSecurityManagerOptions.HostDetermineApplicationTrust)
				{
					applicationTrust = hostSecurityManager.DetermineApplicationTrust(CmsUtils.MergeApplicationEvidence(null, activationContext.Identity, activationContext, null), null, context);
					return applicationTrust != null && applicationTrust.IsApplicationTrustedToRun;
				}
			}
			applicationTrust = ApplicationSecurityManager.DetermineApplicationTrustInternal(activationContext, context);
			return applicationTrust != null && applicationTrust.IsApplicationTrustedToRun;
		}

		// Token: 0x1700056D RID: 1389
		// (get) Token: 0x0600298E RID: 10638 RVA: 0x000997D2 File Offset: 0x000979D2
		public static ApplicationTrustCollection UserApplicationTrusts
		{
			[SecuritySafeCritical]
			[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlPolicy)]
			get
			{
				return new ApplicationTrustCollection(true);
			}
		}

		// Token: 0x1700056E RID: 1390
		// (get) Token: 0x0600298F RID: 10639 RVA: 0x000997DA File Offset: 0x000979DA
		public static IApplicationTrustManager ApplicationTrustManager
		{
			[SecuritySafeCritical]
			[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlPolicy)]
			get
			{
				if (ApplicationSecurityManager.m_appTrustManager == null)
				{
					ApplicationSecurityManager.m_appTrustManager = ApplicationSecurityManager.DecodeAppTrustManager();
					if (ApplicationSecurityManager.m_appTrustManager == null)
					{
						throw new PolicyException(Environment.GetResourceString("Policy_NoTrustManager"));
					}
				}
				return ApplicationSecurityManager.m_appTrustManager;
			}
		}

		// Token: 0x06002990 RID: 10640 RVA: 0x00099814 File Offset: 0x00097A14
		[SecurityCritical]
		internal static ApplicationTrust DetermineApplicationTrustInternal(ActivationContext activationContext, TrustManagerContext context)
		{
			ApplicationTrustCollection applicationTrustCollection = new ApplicationTrustCollection(true);
			ApplicationTrust applicationTrust;
			if (context == null || !context.IgnorePersistedDecision)
			{
				applicationTrust = applicationTrustCollection[activationContext.Identity.FullName];
				if (applicationTrust != null)
				{
					return applicationTrust;
				}
			}
			applicationTrust = ApplicationSecurityManager.ApplicationTrustManager.DetermineApplicationTrust(activationContext, context);
			if (applicationTrust == null)
			{
				applicationTrust = new ApplicationTrust(activationContext.Identity);
			}
			applicationTrust.ApplicationIdentity = activationContext.Identity;
			if (applicationTrust.Persist)
			{
				applicationTrustCollection.Add(applicationTrust);
			}
			return applicationTrust;
		}

		// Token: 0x06002991 RID: 10641 RVA: 0x00099888 File Offset: 0x00097A88
		[SecurityCritical]
		private static IApplicationTrustManager DecodeAppTrustManager()
		{
			if (File.InternalExists(ApplicationSecurityManager.s_machineConfigFile))
			{
				string xml;
				using (FileStream fileStream = new FileStream(ApplicationSecurityManager.s_machineConfigFile, FileMode.Open, FileAccess.Read))
				{
					xml = new StreamReader(fileStream).ReadToEnd();
				}
				SecurityElement securityElement = SecurityElement.FromString(xml);
				SecurityElement securityElement2 = securityElement.SearchForChildByTag("mscorlib");
				if (securityElement2 != null)
				{
					SecurityElement securityElement3 = securityElement2.SearchForChildByTag("security");
					if (securityElement3 != null)
					{
						SecurityElement securityElement4 = securityElement3.SearchForChildByTag("policy");
						if (securityElement4 != null)
						{
							SecurityElement securityElement5 = securityElement4.SearchForChildByTag("ApplicationSecurityManager");
							if (securityElement5 != null)
							{
								SecurityElement securityElement6 = securityElement5.SearchForChildByTag("IApplicationTrustManager");
								if (securityElement6 != null)
								{
									IApplicationTrustManager applicationTrustManager = ApplicationSecurityManager.DecodeAppTrustManagerFromElement(securityElement6);
									if (applicationTrustManager != null)
									{
										return applicationTrustManager;
									}
								}
							}
						}
					}
				}
			}
			return ApplicationSecurityManager.DecodeAppTrustManagerFromElement(ApplicationSecurityManager.CreateDefaultApplicationTrustManagerElement());
		}

		// Token: 0x06002992 RID: 10642 RVA: 0x00099954 File Offset: 0x00097B54
		[SecurityCritical]
		private static SecurityElement CreateDefaultApplicationTrustManagerElement()
		{
			SecurityElement securityElement = new SecurityElement("IApplicationTrustManager");
			SecurityElement securityElement2 = securityElement;
			string name = "class";
			string str = "System.Security.Policy.TrustManager, System.Windows.Forms, Version=";
			Version version = ((RuntimeAssembly)Assembly.GetExecutingAssembly()).GetVersion();
			securityElement2.AddAttribute(name, str + ((version != null) ? version.ToString() : null) + ", Culture=neutral, PublicKeyToken=b77a5c561934e089");
			securityElement.AddAttribute("version", "1");
			return securityElement;
		}

		// Token: 0x06002993 RID: 10643 RVA: 0x000999B4 File Offset: 0x00097BB4
		[SecurityCritical]
		private static IApplicationTrustManager DecodeAppTrustManagerFromElement(SecurityElement elTrustManager)
		{
			new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Assert();
			string typeName = elTrustManager.Attribute("class");
			Type type = Type.GetType(typeName, false, false);
			if (type == null)
			{
				return null;
			}
			IApplicationTrustManager applicationTrustManager = Activator.CreateInstance(type) as IApplicationTrustManager;
			if (applicationTrustManager != null)
			{
				applicationTrustManager.FromXml(elTrustManager);
			}
			return applicationTrustManager;
		}

		// Token: 0x04001110 RID: 4368
		private static volatile IApplicationTrustManager m_appTrustManager = null;

		// Token: 0x04001111 RID: 4369
		private static string s_machineConfigFile = Config.MachineDirectory + "applicationtrust.config";
	}
}
