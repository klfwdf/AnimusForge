using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x02000850 RID: 2128
	[SecurityCritical]
	[ComVisible(true)]
	[SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.Infrastructure)]
	public abstract class BaseChannelWithProperties : BaseChannelObjectWithProperties
	{
		// Token: 0x17000F04 RID: 3844
		// (get) Token: 0x06005A3B RID: 23099 RVA: 0x0013D894 File Offset: 0x0013BA94
		public override IDictionary Properties
		{
			[SecurityCritical]
			get
			{
				ArrayList arrayList = new ArrayList();
				arrayList.Add(this);
				if (this.SinksWithProperties != null)
				{
					IServerChannelSink serverChannelSink = this.SinksWithProperties as IServerChannelSink;
					if (serverChannelSink != null)
					{
						while (serverChannelSink != null)
						{
							IDictionary properties = serverChannelSink.Properties;
							if (properties != null)
							{
								arrayList.Add(properties);
							}
							serverChannelSink = serverChannelSink.NextChannelSink;
						}
					}
					else
					{
						for (IClientChannelSink clientChannelSink = (IClientChannelSink)this.SinksWithProperties; clientChannelSink != null; clientChannelSink = clientChannelSink.NextChannelSink)
						{
							IDictionary properties2 = clientChannelSink.Properties;
							if (properties2 != null)
							{
								arrayList.Add(properties2);
							}
						}
					}
				}
				return new AggregateDictionary(arrayList);
			}
		}

		// Token: 0x04002902 RID: 10498
		protected IChannelSinkBase SinksWithProperties;
	}
}
