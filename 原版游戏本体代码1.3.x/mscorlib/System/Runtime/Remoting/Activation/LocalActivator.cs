using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Metadata;
using System.Security;

namespace System.Runtime.Remoting.Activation
{
	// Token: 0x02000892 RID: 2194
	[SecurityCritical]
	internal class LocalActivator : ContextAttribute, IActivator
	{
		// Token: 0x06005D02 RID: 23810 RVA: 0x0014652A File Offset: 0x0014472A
		internal LocalActivator()
			: base("RemoteActivationService.rem")
		{
		}

		// Token: 0x06005D03 RID: 23811 RVA: 0x00146538 File Offset: 0x00144738
		[SecurityCritical]
		public override bool IsContextOK(Context ctx, IConstructionCallMessage ctorMsg)
		{
			if (RemotingConfigHandler.Info == null)
			{
				return true;
			}
			RuntimeType runtimeType = ctorMsg.ActivationType as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"));
			}
			WellKnownClientTypeEntry wellKnownClientTypeEntry = RemotingConfigHandler.IsWellKnownClientType(runtimeType);
			string text = ((wellKnownClientTypeEntry == null) ? null : wellKnownClientTypeEntry.ObjectUrl);
			if (text != null)
			{
				ctorMsg.Properties["Connect"] = text;
				return false;
			}
			ActivatedClientTypeEntry activatedClientTypeEntry = RemotingConfigHandler.IsRemotelyActivatedClientType(runtimeType);
			string text2 = null;
			if (activatedClientTypeEntry == null)
			{
				object[] callSiteActivationAttributes = ctorMsg.CallSiteActivationAttributes;
				if (callSiteActivationAttributes != null)
				{
					for (int i = 0; i < callSiteActivationAttributes.Length; i++)
					{
						UrlAttribute urlAttribute = callSiteActivationAttributes[i] as UrlAttribute;
						if (urlAttribute != null)
						{
							text2 = urlAttribute.UrlValue;
						}
					}
				}
				if (text2 == null)
				{
					return true;
				}
			}
			else
			{
				text2 = activatedClientTypeEntry.ApplicationUrl;
			}
			string value;
			if (!text2.EndsWith("/", StringComparison.Ordinal))
			{
				value = text2 + "/RemoteActivationService.rem";
			}
			else
			{
				value = text2 + "RemoteActivationService.rem";
			}
			ctorMsg.Properties["Remote"] = value;
			return false;
		}

		// Token: 0x06005D04 RID: 23812 RVA: 0x00146634 File Offset: 0x00144834
		[SecurityCritical]
		public override void GetPropertiesForNewContext(IConstructionCallMessage ctorMsg)
		{
			if (ctorMsg.Properties.Contains("Remote"))
			{
				string remActivatorURL = (string)ctorMsg.Properties["Remote"];
				AppDomainLevelActivator appDomainLevelActivator = new AppDomainLevelActivator(remActivatorURL);
				IActivator activator = ctorMsg.Activator;
				if (activator.Level < ActivatorLevel.AppDomain)
				{
					appDomainLevelActivator.NextActivator = activator;
					ctorMsg.Activator = appDomainLevelActivator;
					return;
				}
				if (activator.NextActivator == null)
				{
					activator.NextActivator = appDomainLevelActivator;
					return;
				}
				while (activator.NextActivator.Level >= ActivatorLevel.AppDomain)
				{
					activator = activator.NextActivator;
				}
				appDomainLevelActivator.NextActivator = activator.NextActivator;
				activator.NextActivator = appDomainLevelActivator;
			}
		}

		// Token: 0x17000FFC RID: 4092
		// (get) Token: 0x06005D05 RID: 23813 RVA: 0x001466C9 File Offset: 0x001448C9
		// (set) Token: 0x06005D06 RID: 23814 RVA: 0x001466CC File Offset: 0x001448CC
		public virtual IActivator NextActivator
		{
			[SecurityCritical]
			get
			{
				return null;
			}
			[SecurityCritical]
			set
			{
				throw new InvalidOperationException();
			}
		}

		// Token: 0x17000FFD RID: 4093
		// (get) Token: 0x06005D07 RID: 23815 RVA: 0x001466D3 File Offset: 0x001448D3
		public virtual ActivatorLevel Level
		{
			[SecurityCritical]
			get
			{
				return ActivatorLevel.AppDomain;
			}
		}

		// Token: 0x06005D08 RID: 23816 RVA: 0x001466D8 File Offset: 0x001448D8
		private static MethodBase GetMethodBase(IConstructionCallMessage msg)
		{
			MethodBase methodBase = msg.MethodBase;
			if (null == methodBase)
			{
				throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_Message_MethodMissing"), msg.MethodName, msg.TypeName));
			}
			return methodBase;
		}

		// Token: 0x06005D09 RID: 23817 RVA: 0x0014671C File Offset: 0x0014491C
		[SecurityCritical]
		[ComVisible(true)]
		public virtual IConstructionReturnMessage Activate(IConstructionCallMessage ctorMsg)
		{
			if (ctorMsg == null)
			{
				throw new ArgumentNullException("ctorMsg");
			}
			if (ctorMsg.Properties.Contains("Remote"))
			{
				return LocalActivator.DoRemoteActivation(ctorMsg);
			}
			if (ctorMsg.Properties.Contains("Permission"))
			{
				Type activationType = ctorMsg.ActivationType;
				object[] activationAttributes = null;
				if (activationType.IsContextful)
				{
					IList contextProperties = ctorMsg.ContextProperties;
					if (contextProperties != null && contextProperties.Count > 0)
					{
						RemotePropertyHolderAttribute remotePropertyHolderAttribute = new RemotePropertyHolderAttribute(contextProperties);
						activationAttributes = new object[] { remotePropertyHolderAttribute };
					}
				}
				MethodBase methodBase = LocalActivator.GetMethodBase(ctorMsg);
				RemotingMethodCachedData reflectionCachedData = InternalRemotingServices.GetReflectionCachedData(methodBase);
				object[] args = Message.CoerceArgs(ctorMsg, reflectionCachedData.Parameters);
				object obj = Activator.CreateInstance(activationType, args, activationAttributes);
				if (RemotingServices.IsClientProxy(obj))
				{
					RedirectionProxy redirectionProxy = new RedirectionProxy((MarshalByRefObject)obj, activationType);
					RemotingServices.MarshalInternal(redirectionProxy, null, activationType);
					obj = redirectionProxy;
				}
				return ActivationServices.SetupConstructionReply(obj, ctorMsg, null);
			}
			return ctorMsg.Activator.Activate(ctorMsg);
		}

		// Token: 0x06005D0A RID: 23818 RVA: 0x00146804 File Offset: 0x00144A04
		internal static IConstructionReturnMessage DoRemoteActivation(IConstructionCallMessage ctorMsg)
		{
			IActivator activator = null;
			string url = (string)ctorMsg.Properties["Remote"];
			try
			{
				activator = (IActivator)RemotingServices.Connect(typeof(IActivator), url);
			}
			catch (Exception arg)
			{
				throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_Activation_ConnectFailed"), arg));
			}
			ctorMsg.Properties.Remove("Remote");
			return activator.Activate(ctorMsg);
		}
	}
}
