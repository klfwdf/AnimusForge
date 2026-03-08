using System;
using System.Deployment.Internal.Isolation.Manifest;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Security;
using System.Security.Policy;

namespace System.Runtime.Hosting
{
	// Token: 0x02000A56 RID: 2646
	[ComVisible(true)]
	public class ApplicationActivator
	{
		// Token: 0x060066C5 RID: 26309 RVA: 0x00159F78 File Offset: 0x00158178
		public virtual ObjectHandle CreateInstance(ActivationContext activationContext)
		{
			return this.CreateInstance(activationContext, null);
		}

		// Token: 0x060066C6 RID: 26310 RVA: 0x00159F84 File Offset: 0x00158184
		[SecuritySafeCritical]
		public virtual ObjectHandle CreateInstance(ActivationContext activationContext, string[] activationCustomData)
		{
			if (activationContext == null)
			{
				throw new ArgumentNullException("activationContext");
			}
			if (CmsUtils.CompareIdentities(AppDomain.CurrentDomain.ActivationContext, activationContext))
			{
				ManifestRunner manifestRunner = new ManifestRunner(AppDomain.CurrentDomain, activationContext);
				return new ObjectHandle(manifestRunner.ExecuteAsAssembly());
			}
			AppDomainSetup appDomainSetup = new AppDomainSetup(new ActivationArguments(activationContext, activationCustomData));
			AppDomainSetup setupInformation = AppDomain.CurrentDomain.SetupInformation;
			appDomainSetup.AppDomainManagerType = setupInformation.AppDomainManagerType;
			appDomainSetup.AppDomainManagerAssembly = setupInformation.AppDomainManagerAssembly;
			return ApplicationActivator.CreateInstanceHelper(appDomainSetup);
		}

		// Token: 0x060066C7 RID: 26311 RVA: 0x0015A004 File Offset: 0x00158204
		[SecuritySafeCritical]
		protected static ObjectHandle CreateInstanceHelper(AppDomainSetup adSetup)
		{
			if (adSetup.ActivationArguments == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MissingActivationArguments"));
			}
			adSetup.ActivationArguments.ActivateInstance = true;
			Evidence evidence = AppDomain.CurrentDomain.Evidence;
			Evidence evidence2 = CmsUtils.MergeApplicationEvidence(null, adSetup.ActivationArguments.ApplicationIdentity, adSetup.ActivationArguments.ActivationContext, adSetup.ActivationArguments.ActivationData);
			HostSecurityManager hostSecurityManager = AppDomain.CurrentDomain.HostSecurityManager;
			ApplicationTrust applicationTrust = hostSecurityManager.DetermineApplicationTrust(evidence2, evidence, new TrustManagerContext());
			if (applicationTrust == null || !applicationTrust.IsApplicationTrustedToRun)
			{
				throw new PolicyException(Environment.GetResourceString("Policy_NoExecutionPermission"), -2146233320, null);
			}
			ObjRef objRef = AppDomain.nCreateInstance(adSetup.ActivationArguments.ApplicationIdentity.FullName, adSetup, evidence2, (evidence2 == null) ? AppDomain.CurrentDomain.InternalEvidence : null, AppDomain.CurrentDomain.GetSecurityDescriptor());
			if (objRef == null)
			{
				return null;
			}
			return RemotingServices.Unmarshal(objRef) as ObjectHandle;
		}
	}
}
