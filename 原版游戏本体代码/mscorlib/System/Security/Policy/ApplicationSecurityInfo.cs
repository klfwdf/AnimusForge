using System;
using System.Deployment.Internal.Isolation;
using System.Deployment.Internal.Isolation.Manifest;
using System.Runtime.Hosting;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security.Util;
using System.Threading;

namespace System.Security.Policy
{
	// Token: 0x02000343 RID: 835
	[SecurityCritical]
	[ComVisible(true)]
	[SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.UnmanagedCode)]
	public sealed class ApplicationSecurityInfo
	{
		// Token: 0x06002981 RID: 10625 RVA: 0x000992E4 File Offset: 0x000974E4
		internal ApplicationSecurityInfo()
		{
		}

		// Token: 0x06002982 RID: 10626 RVA: 0x000992EC File Offset: 0x000974EC
		public ApplicationSecurityInfo(ActivationContext activationContext)
		{
			if (activationContext == null)
			{
				throw new ArgumentNullException("activationContext");
			}
			this.m_context = activationContext;
		}

		// Token: 0x17000569 RID: 1385
		// (get) Token: 0x06002983 RID: 10627 RVA: 0x0009930C File Offset: 0x0009750C
		// (set) Token: 0x06002984 RID: 10628 RVA: 0x00099355 File Offset: 0x00097555
		public ApplicationId ApplicationId
		{
			get
			{
				if (this.m_appId == null && this.m_context != null)
				{
					ICMS applicationComponentManifest = this.m_context.ApplicationComponentManifest;
					ApplicationId value = ApplicationSecurityInfo.ParseApplicationId(applicationComponentManifest);
					Interlocked.CompareExchange(ref this.m_appId, value, null);
				}
				return this.m_appId as ApplicationId;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				this.m_appId = value;
			}
		}

		// Token: 0x1700056A RID: 1386
		// (get) Token: 0x06002985 RID: 10629 RVA: 0x0009936C File Offset: 0x0009756C
		// (set) Token: 0x06002986 RID: 10630 RVA: 0x000993B5 File Offset: 0x000975B5
		public ApplicationId DeploymentId
		{
			get
			{
				if (this.m_deployId == null && this.m_context != null)
				{
					ICMS deploymentComponentManifest = this.m_context.DeploymentComponentManifest;
					ApplicationId value = ApplicationSecurityInfo.ParseApplicationId(deploymentComponentManifest);
					Interlocked.CompareExchange(ref this.m_deployId, value, null);
				}
				return this.m_deployId as ApplicationId;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				this.m_deployId = value;
			}
		}

		// Token: 0x1700056B RID: 1387
		// (get) Token: 0x06002987 RID: 10631 RVA: 0x000993CC File Offset: 0x000975CC
		// (set) Token: 0x06002988 RID: 10632 RVA: 0x00099580 File Offset: 0x00097780
		public PermissionSet DefaultRequestSet
		{
			get
			{
				if (this.m_defaultRequest == null)
				{
					PermissionSet value = new PermissionSet(PermissionState.None);
					if (this.m_context != null)
					{
						ICMS applicationComponentManifest = this.m_context.ApplicationComponentManifest;
						string defaultPermissionSetID = ((IMetadataSectionEntry)applicationComponentManifest.MetadataSectionEntry).defaultPermissionSetID;
						object obj = null;
						if (defaultPermissionSetID != null && defaultPermissionSetID.Length > 0)
						{
							((ISectionWithStringKey)applicationComponentManifest.PermissionSetSection).Lookup(defaultPermissionSetID, out obj);
							IPermissionSetEntry permissionSetEntry = obj as IPermissionSetEntry;
							if (permissionSetEntry != null)
							{
								SecurityElement securityElement = SecurityElement.FromString(permissionSetEntry.AllData.XmlSegment);
								string text = securityElement.Attribute("temp:Unrestricted");
								if (text != null)
								{
									securityElement.AddAttribute("Unrestricted", text);
								}
								string strA = securityElement.Attribute("SameSite");
								if (string.Compare(strA, "Site", StringComparison.OrdinalIgnoreCase) == 0)
								{
									Url url = new Url(this.m_context.Identity.CodeBase);
									URLString urlstring = url.GetURLString();
									NetCodeGroup netCodeGroup = new NetCodeGroup(new AllMembershipCondition());
									SecurityElement securityElement2 = netCodeGroup.CreateWebPermission(urlstring.Host, urlstring.Scheme, urlstring.Port, "System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
									if (securityElement2 != null)
									{
										securityElement.AddChild(securityElement2);
									}
									if (string.Compare("file:", 0, this.m_context.Identity.CodeBase, 0, 5, StringComparison.OrdinalIgnoreCase) == 0)
									{
										FileCodeGroup fileCodeGroup = new FileCodeGroup(new AllMembershipCondition(), FileIOPermissionAccess.Read | FileIOPermissionAccess.PathDiscovery);
										PolicyStatement policyStatement = fileCodeGroup.CalculatePolicy(url);
										if (policyStatement != null)
										{
											PermissionSet permissionSet = policyStatement.PermissionSet;
											if (permissionSet != null)
											{
												securityElement.AddChild(permissionSet.GetPermission(typeof(FileIOPermission)).ToXml());
											}
										}
									}
								}
								value = new ReadOnlyPermissionSet(securityElement);
							}
						}
					}
					Interlocked.CompareExchange(ref this.m_defaultRequest, value, null);
				}
				return this.m_defaultRequest as PermissionSet;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				this.m_defaultRequest = value;
			}
		}

		// Token: 0x1700056C RID: 1388
		// (get) Token: 0x06002989 RID: 10633 RVA: 0x00099598 File Offset: 0x00097798
		// (set) Token: 0x0600298A RID: 10634 RVA: 0x00099695 File Offset: 0x00097895
		public Evidence ApplicationEvidence
		{
			get
			{
				if (this.m_appEvidence == null)
				{
					Evidence evidence = new Evidence();
					if (this.m_context != null)
					{
						evidence = new Evidence();
						Url evidence2 = new Url(this.m_context.Identity.CodeBase);
						evidence.AddHostEvidence<Url>(evidence2);
						evidence.AddHostEvidence<Zone>(Zone.CreateFromUrl(this.m_context.Identity.CodeBase));
						if (string.Compare("file:", 0, this.m_context.Identity.CodeBase, 0, 5, StringComparison.OrdinalIgnoreCase) != 0)
						{
							evidence.AddHostEvidence<Site>(Site.CreateFromUrl(this.m_context.Identity.CodeBase));
						}
						evidence.AddHostEvidence<StrongName>(new StrongName(new StrongNamePublicKeyBlob(this.DeploymentId.m_publicKeyToken), this.DeploymentId.Name, this.DeploymentId.Version));
						evidence.AddHostEvidence<ActivationArguments>(new ActivationArguments(this.m_context));
					}
					Interlocked.CompareExchange(ref this.m_appEvidence, evidence, null);
				}
				return this.m_appEvidence as Evidence;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				this.m_appEvidence = value;
			}
		}

		// Token: 0x0600298B RID: 10635 RVA: 0x000996AC File Offset: 0x000978AC
		private static ApplicationId ParseApplicationId(ICMS manifest)
		{
			if (manifest.Identity == null)
			{
				return null;
			}
			return new ApplicationId(Hex.DecodeHexString(manifest.Identity.GetAttribute("", "publicKeyToken")), manifest.Identity.GetAttribute("", "name"), new Version(manifest.Identity.GetAttribute("", "version")), manifest.Identity.GetAttribute("", "processorArchitecture"), manifest.Identity.GetAttribute("", "culture"));
		}

		// Token: 0x0400110B RID: 4363
		private ActivationContext m_context;

		// Token: 0x0400110C RID: 4364
		private object m_appId;

		// Token: 0x0400110D RID: 4365
		private object m_deployId;

		// Token: 0x0400110E RID: 4366
		private object m_defaultRequest;

		// Token: 0x0400110F RID: 4367
		private object m_appEvidence;
	}
}
