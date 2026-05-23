using System;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Messaging;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x02000836 RID: 2102
	[Serializable]
	internal class CrossAppDomainChannel : IChannel, IChannelSender, IChannelReceiver
	{
		// Token: 0x17000EE3 RID: 3811
		// (get) Token: 0x060059D2 RID: 22994 RVA: 0x0013CC91 File Offset: 0x0013AE91
		// (set) Token: 0x060059D3 RID: 22995 RVA: 0x0013CCA7 File Offset: 0x0013AEA7
		private static CrossAppDomainChannel gAppDomainChannel
		{
			get
			{
				return Thread.GetDomain().RemotingData.ChannelServicesData.xadmessageSink;
			}
			set
			{
				Thread.GetDomain().RemotingData.ChannelServicesData.xadmessageSink = value;
			}
		}

		// Token: 0x17000EE4 RID: 3812
		// (get) Token: 0x060059D4 RID: 22996 RVA: 0x0013CCC0 File Offset: 0x0013AEC0
		internal static CrossAppDomainChannel AppDomainChannel
		{
			get
			{
				if (CrossAppDomainChannel.gAppDomainChannel == null)
				{
					CrossAppDomainChannel gAppDomainChannel = new CrossAppDomainChannel();
					object obj = CrossAppDomainChannel.staticSyncObject;
					lock (obj)
					{
						if (CrossAppDomainChannel.gAppDomainChannel == null)
						{
							CrossAppDomainChannel.gAppDomainChannel = gAppDomainChannel;
						}
					}
				}
				return CrossAppDomainChannel.gAppDomainChannel;
			}
		}

		// Token: 0x060059D5 RID: 22997 RVA: 0x0013CD18 File Offset: 0x0013AF18
		[SecurityCritical]
		internal static void RegisterChannel()
		{
			CrossAppDomainChannel appDomainChannel = CrossAppDomainChannel.AppDomainChannel;
			ChannelServices.RegisterChannelInternal(appDomainChannel, false);
		}

		// Token: 0x17000EE5 RID: 3813
		// (get) Token: 0x060059D6 RID: 22998 RVA: 0x0013CD32 File Offset: 0x0013AF32
		public virtual string ChannelName
		{
			[SecurityCritical]
			get
			{
				return "XAPPDMN";
			}
		}

		// Token: 0x17000EE6 RID: 3814
		// (get) Token: 0x060059D7 RID: 22999 RVA: 0x0013CD39 File Offset: 0x0013AF39
		public virtual string ChannelURI
		{
			get
			{
				return "XAPPDMN_URI";
			}
		}

		// Token: 0x17000EE7 RID: 3815
		// (get) Token: 0x060059D8 RID: 23000 RVA: 0x0013CD40 File Offset: 0x0013AF40
		public virtual int ChannelPriority
		{
			[SecurityCritical]
			get
			{
				return 100;
			}
		}

		// Token: 0x060059D9 RID: 23001 RVA: 0x0013CD44 File Offset: 0x0013AF44
		[SecurityCritical]
		public string Parse(string url, out string objectURI)
		{
			objectURI = url;
			return null;
		}

		// Token: 0x17000EE8 RID: 3816
		// (get) Token: 0x060059DA RID: 23002 RVA: 0x0013CD4A File Offset: 0x0013AF4A
		public virtual object ChannelData
		{
			[SecurityCritical]
			get
			{
				return new CrossAppDomainData(Context.DefaultContext.InternalContextID, Thread.GetDomain().GetId(), Identity.ProcessGuid);
			}
		}

		// Token: 0x060059DB RID: 23003 RVA: 0x0013CD6C File Offset: 0x0013AF6C
		[SecurityCritical]
		public virtual IMessageSink CreateMessageSink(string url, object data, out string objectURI)
		{
			objectURI = null;
			IMessageSink result = null;
			if (url != null && data == null)
			{
				if (url.StartsWith("XAPPDMN", StringComparison.Ordinal))
				{
					throw new RemotingException(Environment.GetResourceString("Remoting_AppDomains_NYI"));
				}
			}
			else
			{
				CrossAppDomainData crossAppDomainData = data as CrossAppDomainData;
				if (crossAppDomainData != null && crossAppDomainData.ProcessGuid.Equals(Identity.ProcessGuid))
				{
					result = CrossAppDomainSink.FindOrCreateSink(crossAppDomainData);
				}
			}
			return result;
		}

		// Token: 0x060059DC RID: 23004 RVA: 0x0013CDC6 File Offset: 0x0013AFC6
		[SecurityCritical]
		public virtual string[] GetUrlsForUri(string objectURI)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_Method"));
		}

		// Token: 0x060059DD RID: 23005 RVA: 0x0013CDD7 File Offset: 0x0013AFD7
		[SecurityCritical]
		public virtual void StartListening(object data)
		{
		}

		// Token: 0x060059DE RID: 23006 RVA: 0x0013CDD9 File Offset: 0x0013AFD9
		[SecurityCritical]
		public virtual void StopListening(object data)
		{
		}

		// Token: 0x040028E6 RID: 10470
		private const string _channelName = "XAPPDMN";

		// Token: 0x040028E7 RID: 10471
		private const string _channelURI = "XAPPDMN_URI";

		// Token: 0x040028E8 RID: 10472
		private static object staticSyncObject = new object();

		// Token: 0x040028E9 RID: 10473
		private static PermissionSet s_fullTrust = new PermissionSet(PermissionState.Unrestricted);
	}
}
