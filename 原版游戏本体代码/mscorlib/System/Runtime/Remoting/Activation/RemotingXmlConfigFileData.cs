using System;
using System.Collections;
using System.Reflection;

namespace System.Runtime.Remoting.Activation
{
	// Token: 0x0200089D RID: 2205
	internal class RemotingXmlConfigFileData
	{
		// Token: 0x06005D33 RID: 23859 RVA: 0x00146BF0 File Offset: 0x00144DF0
		internal void AddInteropXmlElementEntry(string xmlElementName, string xmlElementNamespace, string urtTypeName, string urtAssemblyName)
		{
			this.TryToLoadTypeIfApplicable(urtTypeName, urtAssemblyName);
			RemotingXmlConfigFileData.InteropXmlElementEntry value = new RemotingXmlConfigFileData.InteropXmlElementEntry(xmlElementName, xmlElementNamespace, urtTypeName, urtAssemblyName);
			this.InteropXmlElementEntries.Add(value);
		}

		// Token: 0x06005D34 RID: 23860 RVA: 0x00146C20 File Offset: 0x00144E20
		internal void AddInteropXmlTypeEntry(string xmlTypeName, string xmlTypeNamespace, string urtTypeName, string urtAssemblyName)
		{
			this.TryToLoadTypeIfApplicable(urtTypeName, urtAssemblyName);
			RemotingXmlConfigFileData.InteropXmlTypeEntry value = new RemotingXmlConfigFileData.InteropXmlTypeEntry(xmlTypeName, xmlTypeNamespace, urtTypeName, urtAssemblyName);
			this.InteropXmlTypeEntries.Add(value);
		}

		// Token: 0x06005D35 RID: 23861 RVA: 0x00146C50 File Offset: 0x00144E50
		internal void AddPreLoadEntry(string typeName, string assemblyName)
		{
			this.TryToLoadTypeIfApplicable(typeName, assemblyName);
			RemotingXmlConfigFileData.PreLoadEntry value = new RemotingXmlConfigFileData.PreLoadEntry(typeName, assemblyName);
			this.PreLoadEntries.Add(value);
		}

		// Token: 0x06005D36 RID: 23862 RVA: 0x00146C7C File Offset: 0x00144E7C
		internal RemotingXmlConfigFileData.RemoteAppEntry AddRemoteAppEntry(string appUri)
		{
			RemotingXmlConfigFileData.RemoteAppEntry remoteAppEntry = new RemotingXmlConfigFileData.RemoteAppEntry(appUri);
			this.RemoteAppEntries.Add(remoteAppEntry);
			return remoteAppEntry;
		}

		// Token: 0x06005D37 RID: 23863 RVA: 0x00146CA0 File Offset: 0x00144EA0
		internal void AddServerActivatedEntry(string typeName, string assemName, ArrayList contextAttributes)
		{
			this.TryToLoadTypeIfApplicable(typeName, assemName);
			RemotingXmlConfigFileData.TypeEntry value = new RemotingXmlConfigFileData.TypeEntry(typeName, assemName, contextAttributes);
			this.ServerActivatedEntries.Add(value);
		}

		// Token: 0x06005D38 RID: 23864 RVA: 0x00146CCC File Offset: 0x00144ECC
		internal RemotingXmlConfigFileData.ServerWellKnownEntry AddServerWellKnownEntry(string typeName, string assemName, ArrayList contextAttributes, string objURI, WellKnownObjectMode objMode)
		{
			this.TryToLoadTypeIfApplicable(typeName, assemName);
			RemotingXmlConfigFileData.ServerWellKnownEntry serverWellKnownEntry = new RemotingXmlConfigFileData.ServerWellKnownEntry(typeName, assemName, contextAttributes, objURI, objMode);
			this.ServerWellKnownEntries.Add(serverWellKnownEntry);
			return serverWellKnownEntry;
		}

		// Token: 0x06005D39 RID: 23865 RVA: 0x00146CFC File Offset: 0x00144EFC
		private void TryToLoadTypeIfApplicable(string typeName, string assemblyName)
		{
			if (!RemotingXmlConfigFileData.LoadTypes)
			{
				return;
			}
			Assembly assembly = Assembly.Load(assemblyName);
			if (assembly == null)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_AssemblyLoadFailed", new object[] { assemblyName }));
			}
			Type type = assembly.GetType(typeName, false, false);
			if (type == null)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_BadType", new object[] { typeName }));
			}
		}

		// Token: 0x040029FC RID: 10748
		internal static volatile bool LoadTypes;

		// Token: 0x040029FD RID: 10749
		internal string ApplicationName;

		// Token: 0x040029FE RID: 10750
		internal RemotingXmlConfigFileData.LifetimeEntry Lifetime;

		// Token: 0x040029FF RID: 10751
		internal bool UrlObjRefMode = RemotingConfigHandler.UrlObjRefMode;

		// Token: 0x04002A00 RID: 10752
		internal RemotingXmlConfigFileData.CustomErrorsEntry CustomErrors;

		// Token: 0x04002A01 RID: 10753
		internal ArrayList ChannelEntries = new ArrayList();

		// Token: 0x04002A02 RID: 10754
		internal ArrayList InteropXmlElementEntries = new ArrayList();

		// Token: 0x04002A03 RID: 10755
		internal ArrayList InteropXmlTypeEntries = new ArrayList();

		// Token: 0x04002A04 RID: 10756
		internal ArrayList PreLoadEntries = new ArrayList();

		// Token: 0x04002A05 RID: 10757
		internal ArrayList RemoteAppEntries = new ArrayList();

		// Token: 0x04002A06 RID: 10758
		internal ArrayList ServerActivatedEntries = new ArrayList();

		// Token: 0x04002A07 RID: 10759
		internal ArrayList ServerWellKnownEntries = new ArrayList();

		// Token: 0x02000C7E RID: 3198
		internal class ChannelEntry
		{
			// Token: 0x060070CD RID: 28877 RVA: 0x00184D9A File Offset: 0x00182F9A
			internal ChannelEntry(string typeName, string assemblyName, Hashtable properties)
			{
				this.TypeName = typeName;
				this.AssemblyName = assemblyName;
				this.Properties = properties;
			}

			// Token: 0x04003810 RID: 14352
			internal string TypeName;

			// Token: 0x04003811 RID: 14353
			internal string AssemblyName;

			// Token: 0x04003812 RID: 14354
			internal Hashtable Properties;

			// Token: 0x04003813 RID: 14355
			internal bool DelayLoad;

			// Token: 0x04003814 RID: 14356
			internal ArrayList ClientSinkProviders = new ArrayList();

			// Token: 0x04003815 RID: 14357
			internal ArrayList ServerSinkProviders = new ArrayList();
		}

		// Token: 0x02000C7F RID: 3199
		internal class ClientWellKnownEntry
		{
			// Token: 0x060070CE RID: 28878 RVA: 0x00184DCD File Offset: 0x00182FCD
			internal ClientWellKnownEntry(string typeName, string assemName, string url)
			{
				this.TypeName = typeName;
				this.AssemblyName = assemName;
				this.Url = url;
			}

			// Token: 0x04003816 RID: 14358
			internal string TypeName;

			// Token: 0x04003817 RID: 14359
			internal string AssemblyName;

			// Token: 0x04003818 RID: 14360
			internal string Url;
		}

		// Token: 0x02000C80 RID: 3200
		internal class ContextAttributeEntry
		{
			// Token: 0x060070CF RID: 28879 RVA: 0x00184DEA File Offset: 0x00182FEA
			internal ContextAttributeEntry(string typeName, string assemName, Hashtable properties)
			{
				this.TypeName = typeName;
				this.AssemblyName = assemName;
				this.Properties = properties;
			}

			// Token: 0x04003819 RID: 14361
			internal string TypeName;

			// Token: 0x0400381A RID: 14362
			internal string AssemblyName;

			// Token: 0x0400381B RID: 14363
			internal Hashtable Properties;
		}

		// Token: 0x02000C81 RID: 3201
		internal class InteropXmlElementEntry
		{
			// Token: 0x060070D0 RID: 28880 RVA: 0x00184E07 File Offset: 0x00183007
			internal InteropXmlElementEntry(string xmlElementName, string xmlElementNamespace, string urtTypeName, string urtAssemblyName)
			{
				this.XmlElementName = xmlElementName;
				this.XmlElementNamespace = xmlElementNamespace;
				this.UrtTypeName = urtTypeName;
				this.UrtAssemblyName = urtAssemblyName;
			}

			// Token: 0x0400381C RID: 14364
			internal string XmlElementName;

			// Token: 0x0400381D RID: 14365
			internal string XmlElementNamespace;

			// Token: 0x0400381E RID: 14366
			internal string UrtTypeName;

			// Token: 0x0400381F RID: 14367
			internal string UrtAssemblyName;
		}

		// Token: 0x02000C82 RID: 3202
		internal class CustomErrorsEntry
		{
			// Token: 0x060070D1 RID: 28881 RVA: 0x00184E2C File Offset: 0x0018302C
			internal CustomErrorsEntry(CustomErrorsModes mode)
			{
				this.Mode = mode;
			}

			// Token: 0x04003820 RID: 14368
			internal CustomErrorsModes Mode;
		}

		// Token: 0x02000C83 RID: 3203
		internal class InteropXmlTypeEntry
		{
			// Token: 0x060070D2 RID: 28882 RVA: 0x00184E3B File Offset: 0x0018303B
			internal InteropXmlTypeEntry(string xmlTypeName, string xmlTypeNamespace, string urtTypeName, string urtAssemblyName)
			{
				this.XmlTypeName = xmlTypeName;
				this.XmlTypeNamespace = xmlTypeNamespace;
				this.UrtTypeName = urtTypeName;
				this.UrtAssemblyName = urtAssemblyName;
			}

			// Token: 0x04003821 RID: 14369
			internal string XmlTypeName;

			// Token: 0x04003822 RID: 14370
			internal string XmlTypeNamespace;

			// Token: 0x04003823 RID: 14371
			internal string UrtTypeName;

			// Token: 0x04003824 RID: 14372
			internal string UrtAssemblyName;
		}

		// Token: 0x02000C84 RID: 3204
		internal class LifetimeEntry
		{
			// Token: 0x1700135C RID: 4956
			// (get) Token: 0x060070D3 RID: 28883 RVA: 0x00184E60 File Offset: 0x00183060
			// (set) Token: 0x060070D4 RID: 28884 RVA: 0x00184E68 File Offset: 0x00183068
			internal TimeSpan LeaseTime
			{
				get
				{
					return this._leaseTime;
				}
				set
				{
					this._leaseTime = value;
					this.IsLeaseTimeSet = true;
				}
			}

			// Token: 0x1700135D RID: 4957
			// (get) Token: 0x060070D5 RID: 28885 RVA: 0x00184E78 File Offset: 0x00183078
			// (set) Token: 0x060070D6 RID: 28886 RVA: 0x00184E80 File Offset: 0x00183080
			internal TimeSpan RenewOnCallTime
			{
				get
				{
					return this._renewOnCallTime;
				}
				set
				{
					this._renewOnCallTime = value;
					this.IsRenewOnCallTimeSet = true;
				}
			}

			// Token: 0x1700135E RID: 4958
			// (get) Token: 0x060070D7 RID: 28887 RVA: 0x00184E90 File Offset: 0x00183090
			// (set) Token: 0x060070D8 RID: 28888 RVA: 0x00184E98 File Offset: 0x00183098
			internal TimeSpan SponsorshipTimeout
			{
				get
				{
					return this._sponsorshipTimeout;
				}
				set
				{
					this._sponsorshipTimeout = value;
					this.IsSponsorshipTimeoutSet = true;
				}
			}

			// Token: 0x1700135F RID: 4959
			// (get) Token: 0x060070D9 RID: 28889 RVA: 0x00184EA8 File Offset: 0x001830A8
			// (set) Token: 0x060070DA RID: 28890 RVA: 0x00184EB0 File Offset: 0x001830B0
			internal TimeSpan LeaseManagerPollTime
			{
				get
				{
					return this._leaseManagerPollTime;
				}
				set
				{
					this._leaseManagerPollTime = value;
					this.IsLeaseManagerPollTimeSet = true;
				}
			}

			// Token: 0x04003825 RID: 14373
			internal bool IsLeaseTimeSet;

			// Token: 0x04003826 RID: 14374
			internal bool IsRenewOnCallTimeSet;

			// Token: 0x04003827 RID: 14375
			internal bool IsSponsorshipTimeoutSet;

			// Token: 0x04003828 RID: 14376
			internal bool IsLeaseManagerPollTimeSet;

			// Token: 0x04003829 RID: 14377
			private TimeSpan _leaseTime;

			// Token: 0x0400382A RID: 14378
			private TimeSpan _renewOnCallTime;

			// Token: 0x0400382B RID: 14379
			private TimeSpan _sponsorshipTimeout;

			// Token: 0x0400382C RID: 14380
			private TimeSpan _leaseManagerPollTime;
		}

		// Token: 0x02000C85 RID: 3205
		internal class PreLoadEntry
		{
			// Token: 0x060070DC RID: 28892 RVA: 0x00184EC8 File Offset: 0x001830C8
			public PreLoadEntry(string typeName, string assemblyName)
			{
				this.TypeName = typeName;
				this.AssemblyName = assemblyName;
			}

			// Token: 0x0400382D RID: 14381
			internal string TypeName;

			// Token: 0x0400382E RID: 14382
			internal string AssemblyName;
		}

		// Token: 0x02000C86 RID: 3206
		internal class RemoteAppEntry
		{
			// Token: 0x060070DD RID: 28893 RVA: 0x00184EDE File Offset: 0x001830DE
			internal RemoteAppEntry(string appUri)
			{
				this.AppUri = appUri;
			}

			// Token: 0x060070DE RID: 28894 RVA: 0x00184F04 File Offset: 0x00183104
			internal void AddWellKnownEntry(string typeName, string assemName, string url)
			{
				RemotingXmlConfigFileData.ClientWellKnownEntry value = new RemotingXmlConfigFileData.ClientWellKnownEntry(typeName, assemName, url);
				this.WellKnownObjects.Add(value);
			}

			// Token: 0x060070DF RID: 28895 RVA: 0x00184F28 File Offset: 0x00183128
			internal void AddActivatedEntry(string typeName, string assemName, ArrayList contextAttributes)
			{
				RemotingXmlConfigFileData.TypeEntry value = new RemotingXmlConfigFileData.TypeEntry(typeName, assemName, contextAttributes);
				this.ActivatedObjects.Add(value);
			}

			// Token: 0x0400382F RID: 14383
			internal string AppUri;

			// Token: 0x04003830 RID: 14384
			internal ArrayList WellKnownObjects = new ArrayList();

			// Token: 0x04003831 RID: 14385
			internal ArrayList ActivatedObjects = new ArrayList();
		}

		// Token: 0x02000C87 RID: 3207
		internal class ServerWellKnownEntry : RemotingXmlConfigFileData.TypeEntry
		{
			// Token: 0x060070E0 RID: 28896 RVA: 0x00184F4B File Offset: 0x0018314B
			internal ServerWellKnownEntry(string typeName, string assemName, ArrayList contextAttributes, string objURI, WellKnownObjectMode objMode)
				: base(typeName, assemName, contextAttributes)
			{
				this.ObjectURI = objURI;
				this.ObjectMode = objMode;
			}

			// Token: 0x04003832 RID: 14386
			internal string ObjectURI;

			// Token: 0x04003833 RID: 14387
			internal WellKnownObjectMode ObjectMode;
		}

		// Token: 0x02000C88 RID: 3208
		internal class SinkProviderEntry
		{
			// Token: 0x060070E1 RID: 28897 RVA: 0x00184F66 File Offset: 0x00183166
			internal SinkProviderEntry(string typeName, string assemName, Hashtable properties, bool isFormatter)
			{
				this.TypeName = typeName;
				this.AssemblyName = assemName;
				this.Properties = properties;
				this.IsFormatter = isFormatter;
			}

			// Token: 0x04003834 RID: 14388
			internal string TypeName;

			// Token: 0x04003835 RID: 14389
			internal string AssemblyName;

			// Token: 0x04003836 RID: 14390
			internal Hashtable Properties;

			// Token: 0x04003837 RID: 14391
			internal ArrayList ProviderData = new ArrayList();

			// Token: 0x04003838 RID: 14392
			internal bool IsFormatter;
		}

		// Token: 0x02000C89 RID: 3209
		internal class TypeEntry
		{
			// Token: 0x060070E2 RID: 28898 RVA: 0x00184F96 File Offset: 0x00183196
			internal TypeEntry(string typeName, string assemName, ArrayList contextAttributes)
			{
				this.TypeName = typeName;
				this.AssemblyName = assemName;
				this.ContextAttributes = contextAttributes;
			}

			// Token: 0x04003839 RID: 14393
			internal string TypeName;

			// Token: 0x0400383A RID: 14394
			internal string AssemblyName;

			// Token: 0x0400383B RID: 14395
			internal ArrayList ContextAttributes;
		}
	}
}
