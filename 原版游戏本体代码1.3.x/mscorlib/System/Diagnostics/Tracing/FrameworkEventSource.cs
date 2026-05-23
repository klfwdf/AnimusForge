using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000434 RID: 1076
	[FriendAccessAllowed]
	[EventSource(Guid = "8E9F5090-2D75-4d03-8A81-E5AFBF85DAF1", Name = "System.Diagnostics.Eventing.FrameworkEventSource")]
	internal sealed class FrameworkEventSource : EventSource
	{
		// Token: 0x170007F5 RID: 2037
		// (get) Token: 0x06003592 RID: 13714 RVA: 0x000D1246 File Offset: 0x000CF446
		public static bool IsInitialized
		{
			get
			{
				return FrameworkEventSource.Log != null;
			}
		}

		// Token: 0x06003593 RID: 13715 RVA: 0x000D1250 File Offset: 0x000CF450
		private FrameworkEventSource()
			: base(new Guid(2392805520U, 11637, 19715, 138, 129, 229, 175, 191, 133, 218, 241), "System.Diagnostics.Eventing.FrameworkEventSource")
		{
		}

		// Token: 0x06003594 RID: 13716 RVA: 0x000D12A4 File Offset: 0x000CF4A4
		[NonEvent]
		[SecuritySafeCritical]
		private unsafe void WriteEvent(int eventId, long arg1, int arg2, string arg3, bool arg4)
		{
			if (base.IsEnabled())
			{
				if (arg3 == null)
				{
					arg3 = "";
				}
				fixed (string text = arg3)
				{
					char* ptr = text;
					if (ptr != null)
					{
						ptr += RuntimeHelpers.OffsetToStringData / 2;
					}
					EventSource.EventData* ptr2 = stackalloc EventSource.EventData[checked(unchecked((UIntPtr)4) * (UIntPtr)sizeof(EventSource.EventData))];
					ptr2->DataPointer = (IntPtr)((void*)(&arg1));
					ptr2->Size = 8;
					ptr2[1].DataPointer = (IntPtr)((void*)(&arg2));
					ptr2[1].Size = 4;
					ptr2[2].DataPointer = (IntPtr)((void*)ptr);
					ptr2[2].Size = (arg3.Length + 1) * 2;
					ptr2[3].DataPointer = (IntPtr)((void*)(&arg4));
					ptr2[3].Size = 4;
					base.WriteEventCore(eventId, 4, ptr2);
				}
			}
		}

		// Token: 0x06003595 RID: 13717 RVA: 0x000D1384 File Offset: 0x000CF584
		[NonEvent]
		[SecuritySafeCritical]
		private unsafe void WriteEvent(int eventId, long arg1, int arg2, string arg3)
		{
			if (base.IsEnabled())
			{
				if (arg3 == null)
				{
					arg3 = "";
				}
				fixed (string text = arg3)
				{
					char* ptr = text;
					if (ptr != null)
					{
						ptr += RuntimeHelpers.OffsetToStringData / 2;
					}
					EventSource.EventData* ptr2 = stackalloc EventSource.EventData[checked(unchecked((UIntPtr)3) * (UIntPtr)sizeof(EventSource.EventData))];
					ptr2->DataPointer = (IntPtr)((void*)(&arg1));
					ptr2->Size = 8;
					ptr2[1].DataPointer = (IntPtr)((void*)(&arg2));
					ptr2[1].Size = 4;
					ptr2[2].DataPointer = (IntPtr)((void*)ptr);
					ptr2[2].Size = (arg3.Length + 1) * 2;
					base.WriteEventCore(eventId, 3, ptr2);
				}
			}
		}

		// Token: 0x06003596 RID: 13718 RVA: 0x000D1438 File Offset: 0x000CF638
		[NonEvent]
		[SecuritySafeCritical]
		private unsafe void WriteEvent(int eventId, long arg1, string arg2, bool arg3, bool arg4)
		{
			if (base.IsEnabled())
			{
				if (arg2 == null)
				{
					arg2 = "";
				}
				fixed (string text = arg2)
				{
					char* ptr = text;
					if (ptr != null)
					{
						ptr += RuntimeHelpers.OffsetToStringData / 2;
					}
					EventSource.EventData* ptr2 = stackalloc EventSource.EventData[checked(unchecked((UIntPtr)4) * (UIntPtr)sizeof(EventSource.EventData))];
					ptr2->DataPointer = (IntPtr)((void*)(&arg1));
					ptr2->Size = 8;
					ptr2[1].DataPointer = (IntPtr)((void*)ptr);
					ptr2[1].Size = (arg2.Length + 1) * 2;
					ptr2[2].DataPointer = (IntPtr)((void*)(&arg3));
					ptr2[2].Size = 4;
					ptr2[3].DataPointer = (IntPtr)((void*)(&arg4));
					ptr2[3].Size = 4;
					base.WriteEventCore(eventId, 4, ptr2);
				}
			}
		}

		// Token: 0x06003597 RID: 13719 RVA: 0x000D1514 File Offset: 0x000CF714
		[NonEvent]
		[SecuritySafeCritical]
		private unsafe void WriteEvent(int eventId, long arg1, bool arg2, bool arg3)
		{
			if (base.IsEnabled())
			{
				EventSource.EventData* ptr = stackalloc EventSource.EventData[checked(unchecked((UIntPtr)3) * (UIntPtr)sizeof(EventSource.EventData))];
				ptr->DataPointer = (IntPtr)((void*)(&arg1));
				ptr->Size = 8;
				ptr[1].DataPointer = (IntPtr)((void*)(&arg2));
				ptr[1].Size = 4;
				ptr[2].DataPointer = (IntPtr)((void*)(&arg3));
				ptr[2].Size = 4;
				base.WriteEventCore(eventId, 3, ptr);
			}
		}

		// Token: 0x06003598 RID: 13720 RVA: 0x000D15A0 File Offset: 0x000CF7A0
		[NonEvent]
		[SecuritySafeCritical]
		private unsafe void WriteEvent(int eventId, long arg1, bool arg2, bool arg3, int arg4)
		{
			if (base.IsEnabled())
			{
				EventSource.EventData* ptr = stackalloc EventSource.EventData[checked(unchecked((UIntPtr)4) * (UIntPtr)sizeof(EventSource.EventData))];
				ptr->DataPointer = (IntPtr)((void*)(&arg1));
				ptr->Size = 8;
				ptr[1].DataPointer = (IntPtr)((void*)(&arg2));
				ptr[1].Size = 4;
				ptr[2].DataPointer = (IntPtr)((void*)(&arg3));
				ptr[2].Size = 4;
				ptr[3].DataPointer = (IntPtr)((void*)(&arg4));
				ptr[3].Size = 4;
				base.WriteEventCore(eventId, 4, ptr);
			}
		}

		// Token: 0x06003599 RID: 13721 RVA: 0x000D1657 File Offset: 0x000CF857
		[Event(1, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
		public void ResourceManagerLookupStarted(string baseName, string mainAssemblyName, string cultureName)
		{
			base.WriteEvent(1, baseName, mainAssemblyName, cultureName);
		}

		// Token: 0x0600359A RID: 13722 RVA: 0x000D1663 File Offset: 0x000CF863
		[Event(2, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
		public void ResourceManagerLookingForResourceSet(string baseName, string mainAssemblyName, string cultureName)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(2, baseName, mainAssemblyName, cultureName);
			}
		}

		// Token: 0x0600359B RID: 13723 RVA: 0x000D1677 File Offset: 0x000CF877
		[Event(3, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
		public void ResourceManagerFoundResourceSetInCache(string baseName, string mainAssemblyName, string cultureName)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(3, baseName, mainAssemblyName, cultureName);
			}
		}

		// Token: 0x0600359C RID: 13724 RVA: 0x000D168B File Offset: 0x000CF88B
		[Event(4, Level = EventLevel.Warning, Keywords = (EventKeywords)1L)]
		public void ResourceManagerFoundResourceSetInCacheUnexpected(string baseName, string mainAssemblyName, string cultureName)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(4, baseName, mainAssemblyName, cultureName);
			}
		}

		// Token: 0x0600359D RID: 13725 RVA: 0x000D169F File Offset: 0x000CF89F
		[Event(5, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
		public void ResourceManagerStreamFound(string baseName, string mainAssemblyName, string cultureName, string loadedAssemblyName, string resourceFileName)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(5, new object[] { baseName, mainAssemblyName, cultureName, loadedAssemblyName, resourceFileName });
			}
		}

		// Token: 0x0600359E RID: 13726 RVA: 0x000D16CC File Offset: 0x000CF8CC
		[Event(6, Level = EventLevel.Warning, Keywords = (EventKeywords)1L)]
		public void ResourceManagerStreamNotFound(string baseName, string mainAssemblyName, string cultureName, string loadedAssemblyName, string resourceFileName)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(6, new object[] { baseName, mainAssemblyName, cultureName, loadedAssemblyName, resourceFileName });
			}
		}

		// Token: 0x0600359F RID: 13727 RVA: 0x000D16F9 File Offset: 0x000CF8F9
		[Event(7, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
		public void ResourceManagerGetSatelliteAssemblySucceeded(string baseName, string mainAssemblyName, string cultureName, string assemblyName)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(7, new object[] { baseName, mainAssemblyName, cultureName, assemblyName });
			}
		}

		// Token: 0x060035A0 RID: 13728 RVA: 0x000D1721 File Offset: 0x000CF921
		[Event(8, Level = EventLevel.Warning, Keywords = (EventKeywords)1L)]
		public void ResourceManagerGetSatelliteAssemblyFailed(string baseName, string mainAssemblyName, string cultureName, string assemblyName)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(8, new object[] { baseName, mainAssemblyName, cultureName, assemblyName });
			}
		}

		// Token: 0x060035A1 RID: 13729 RVA: 0x000D1749 File Offset: 0x000CF949
		[Event(9, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
		public void ResourceManagerCaseInsensitiveResourceStreamLookupSucceeded(string baseName, string mainAssemblyName, string assemblyName, string resourceFileName)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(9, new object[] { baseName, mainAssemblyName, assemblyName, resourceFileName });
			}
		}

		// Token: 0x060035A2 RID: 13730 RVA: 0x000D1772 File Offset: 0x000CF972
		[Event(10, Level = EventLevel.Warning, Keywords = (EventKeywords)1L)]
		public void ResourceManagerCaseInsensitiveResourceStreamLookupFailed(string baseName, string mainAssemblyName, string assemblyName, string resourceFileName)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(10, new object[] { baseName, mainAssemblyName, assemblyName, resourceFileName });
			}
		}

		// Token: 0x060035A3 RID: 13731 RVA: 0x000D179B File Offset: 0x000CF99B
		[Event(11, Level = EventLevel.Error, Keywords = (EventKeywords)1L)]
		public void ResourceManagerManifestResourceAccessDenied(string baseName, string mainAssemblyName, string assemblyName, string canonicalName)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(11, new object[] { baseName, mainAssemblyName, assemblyName, canonicalName });
			}
		}

		// Token: 0x060035A4 RID: 13732 RVA: 0x000D17C4 File Offset: 0x000CF9C4
		[Event(12, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
		public void ResourceManagerNeutralResourcesSufficient(string baseName, string mainAssemblyName, string cultureName)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(12, baseName, mainAssemblyName, cultureName);
			}
		}

		// Token: 0x060035A5 RID: 13733 RVA: 0x000D17D9 File Offset: 0x000CF9D9
		[Event(13, Level = EventLevel.Warning, Keywords = (EventKeywords)1L)]
		public void ResourceManagerNeutralResourceAttributeMissing(string mainAssemblyName)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(13, mainAssemblyName);
			}
		}

		// Token: 0x060035A6 RID: 13734 RVA: 0x000D17EC File Offset: 0x000CF9EC
		[Event(14, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
		public void ResourceManagerCreatingResourceSet(string baseName, string mainAssemblyName, string cultureName, string fileName)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(14, new object[] { baseName, mainAssemblyName, cultureName, fileName });
			}
		}

		// Token: 0x060035A7 RID: 13735 RVA: 0x000D1815 File Offset: 0x000CFA15
		[Event(15, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
		public void ResourceManagerNotCreatingResourceSet(string baseName, string mainAssemblyName, string cultureName)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(15, baseName, mainAssemblyName, cultureName);
			}
		}

		// Token: 0x060035A8 RID: 13736 RVA: 0x000D182A File Offset: 0x000CFA2A
		[Event(16, Level = EventLevel.Warning, Keywords = (EventKeywords)1L)]
		public void ResourceManagerLookupFailed(string baseName, string mainAssemblyName, string cultureName)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(16, baseName, mainAssemblyName, cultureName);
			}
		}

		// Token: 0x060035A9 RID: 13737 RVA: 0x000D183F File Offset: 0x000CFA3F
		[Event(17, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
		public void ResourceManagerReleasingResources(string baseName, string mainAssemblyName)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(17, baseName, mainAssemblyName);
			}
		}

		// Token: 0x060035AA RID: 13738 RVA: 0x000D1853 File Offset: 0x000CFA53
		[Event(18, Level = EventLevel.Warning, Keywords = (EventKeywords)1L)]
		public void ResourceManagerNeutralResourcesNotFound(string baseName, string mainAssemblyName, string resName)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(18, baseName, mainAssemblyName, resName);
			}
		}

		// Token: 0x060035AB RID: 13739 RVA: 0x000D1868 File Offset: 0x000CFA68
		[Event(19, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
		public void ResourceManagerNeutralResourcesFound(string baseName, string mainAssemblyName, string resName)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(19, baseName, mainAssemblyName, resName);
			}
		}

		// Token: 0x060035AC RID: 13740 RVA: 0x000D187D File Offset: 0x000CFA7D
		[Event(20, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
		public void ResourceManagerAddingCultureFromConfigFile(string baseName, string mainAssemblyName, string cultureName)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(20, baseName, mainAssemblyName, cultureName);
			}
		}

		// Token: 0x060035AD RID: 13741 RVA: 0x000D1892 File Offset: 0x000CFA92
		[Event(21, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
		public void ResourceManagerCultureNotFoundInConfigFile(string baseName, string mainAssemblyName, string cultureName)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(21, baseName, mainAssemblyName, cultureName);
			}
		}

		// Token: 0x060035AE RID: 13742 RVA: 0x000D18A7 File Offset: 0x000CFAA7
		[Event(22, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
		public void ResourceManagerCultureFoundInConfigFile(string baseName, string mainAssemblyName, string cultureName)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(22, baseName, mainAssemblyName, cultureName);
			}
		}

		// Token: 0x060035AF RID: 13743 RVA: 0x000D18BC File Offset: 0x000CFABC
		[NonEvent]
		public void ResourceManagerLookupStarted(string baseName, Assembly mainAssembly, string cultureName)
		{
			if (base.IsEnabled())
			{
				this.ResourceManagerLookupStarted(baseName, FrameworkEventSource.GetName(mainAssembly), cultureName);
			}
		}

		// Token: 0x060035B0 RID: 13744 RVA: 0x000D18D4 File Offset: 0x000CFAD4
		[NonEvent]
		public void ResourceManagerLookingForResourceSet(string baseName, Assembly mainAssembly, string cultureName)
		{
			if (base.IsEnabled())
			{
				this.ResourceManagerLookingForResourceSet(baseName, FrameworkEventSource.GetName(mainAssembly), cultureName);
			}
		}

		// Token: 0x060035B1 RID: 13745 RVA: 0x000D18EC File Offset: 0x000CFAEC
		[NonEvent]
		public void ResourceManagerFoundResourceSetInCache(string baseName, Assembly mainAssembly, string cultureName)
		{
			if (base.IsEnabled())
			{
				this.ResourceManagerFoundResourceSetInCache(baseName, FrameworkEventSource.GetName(mainAssembly), cultureName);
			}
		}

		// Token: 0x060035B2 RID: 13746 RVA: 0x000D1904 File Offset: 0x000CFB04
		[NonEvent]
		public void ResourceManagerFoundResourceSetInCacheUnexpected(string baseName, Assembly mainAssembly, string cultureName)
		{
			if (base.IsEnabled())
			{
				this.ResourceManagerFoundResourceSetInCacheUnexpected(baseName, FrameworkEventSource.GetName(mainAssembly), cultureName);
			}
		}

		// Token: 0x060035B3 RID: 13747 RVA: 0x000D191C File Offset: 0x000CFB1C
		[NonEvent]
		public void ResourceManagerStreamFound(string baseName, Assembly mainAssembly, string cultureName, Assembly loadedAssembly, string resourceFileName)
		{
			if (base.IsEnabled())
			{
				this.ResourceManagerStreamFound(baseName, FrameworkEventSource.GetName(mainAssembly), cultureName, FrameworkEventSource.GetName(loadedAssembly), resourceFileName);
			}
		}

		// Token: 0x060035B4 RID: 13748 RVA: 0x000D193D File Offset: 0x000CFB3D
		[NonEvent]
		public void ResourceManagerStreamNotFound(string baseName, Assembly mainAssembly, string cultureName, Assembly loadedAssembly, string resourceFileName)
		{
			if (base.IsEnabled())
			{
				this.ResourceManagerStreamNotFound(baseName, FrameworkEventSource.GetName(mainAssembly), cultureName, FrameworkEventSource.GetName(loadedAssembly), resourceFileName);
			}
		}

		// Token: 0x060035B5 RID: 13749 RVA: 0x000D195E File Offset: 0x000CFB5E
		[NonEvent]
		public void ResourceManagerGetSatelliteAssemblySucceeded(string baseName, Assembly mainAssembly, string cultureName, string assemblyName)
		{
			if (base.IsEnabled())
			{
				this.ResourceManagerGetSatelliteAssemblySucceeded(baseName, FrameworkEventSource.GetName(mainAssembly), cultureName, assemblyName);
			}
		}

		// Token: 0x060035B6 RID: 13750 RVA: 0x000D1978 File Offset: 0x000CFB78
		[NonEvent]
		public void ResourceManagerGetSatelliteAssemblyFailed(string baseName, Assembly mainAssembly, string cultureName, string assemblyName)
		{
			if (base.IsEnabled())
			{
				this.ResourceManagerGetSatelliteAssemblyFailed(baseName, FrameworkEventSource.GetName(mainAssembly), cultureName, assemblyName);
			}
		}

		// Token: 0x060035B7 RID: 13751 RVA: 0x000D1992 File Offset: 0x000CFB92
		[NonEvent]
		public void ResourceManagerCaseInsensitiveResourceStreamLookupSucceeded(string baseName, Assembly mainAssembly, string assemblyName, string resourceFileName)
		{
			if (base.IsEnabled())
			{
				this.ResourceManagerCaseInsensitiveResourceStreamLookupSucceeded(baseName, FrameworkEventSource.GetName(mainAssembly), assemblyName, resourceFileName);
			}
		}

		// Token: 0x060035B8 RID: 13752 RVA: 0x000D19AC File Offset: 0x000CFBAC
		[NonEvent]
		public void ResourceManagerCaseInsensitiveResourceStreamLookupFailed(string baseName, Assembly mainAssembly, string assemblyName, string resourceFileName)
		{
			if (base.IsEnabled())
			{
				this.ResourceManagerCaseInsensitiveResourceStreamLookupFailed(baseName, FrameworkEventSource.GetName(mainAssembly), assemblyName, resourceFileName);
			}
		}

		// Token: 0x060035B9 RID: 13753 RVA: 0x000D19C6 File Offset: 0x000CFBC6
		[NonEvent]
		public void ResourceManagerManifestResourceAccessDenied(string baseName, Assembly mainAssembly, string assemblyName, string canonicalName)
		{
			if (base.IsEnabled())
			{
				this.ResourceManagerManifestResourceAccessDenied(baseName, FrameworkEventSource.GetName(mainAssembly), assemblyName, canonicalName);
			}
		}

		// Token: 0x060035BA RID: 13754 RVA: 0x000D19E0 File Offset: 0x000CFBE0
		[NonEvent]
		public void ResourceManagerNeutralResourcesSufficient(string baseName, Assembly mainAssembly, string cultureName)
		{
			if (base.IsEnabled())
			{
				this.ResourceManagerNeutralResourcesSufficient(baseName, FrameworkEventSource.GetName(mainAssembly), cultureName);
			}
		}

		// Token: 0x060035BB RID: 13755 RVA: 0x000D19F8 File Offset: 0x000CFBF8
		[NonEvent]
		public void ResourceManagerNeutralResourceAttributeMissing(Assembly mainAssembly)
		{
			if (base.IsEnabled())
			{
				this.ResourceManagerNeutralResourceAttributeMissing(FrameworkEventSource.GetName(mainAssembly));
			}
		}

		// Token: 0x060035BC RID: 13756 RVA: 0x000D1A0E File Offset: 0x000CFC0E
		[NonEvent]
		public void ResourceManagerCreatingResourceSet(string baseName, Assembly mainAssembly, string cultureName, string fileName)
		{
			if (base.IsEnabled())
			{
				this.ResourceManagerCreatingResourceSet(baseName, FrameworkEventSource.GetName(mainAssembly), cultureName, fileName);
			}
		}

		// Token: 0x060035BD RID: 13757 RVA: 0x000D1A28 File Offset: 0x000CFC28
		[NonEvent]
		public void ResourceManagerNotCreatingResourceSet(string baseName, Assembly mainAssembly, string cultureName)
		{
			if (base.IsEnabled())
			{
				this.ResourceManagerNotCreatingResourceSet(baseName, FrameworkEventSource.GetName(mainAssembly), cultureName);
			}
		}

		// Token: 0x060035BE RID: 13758 RVA: 0x000D1A40 File Offset: 0x000CFC40
		[NonEvent]
		public void ResourceManagerLookupFailed(string baseName, Assembly mainAssembly, string cultureName)
		{
			if (base.IsEnabled())
			{
				this.ResourceManagerLookupFailed(baseName, FrameworkEventSource.GetName(mainAssembly), cultureName);
			}
		}

		// Token: 0x060035BF RID: 13759 RVA: 0x000D1A58 File Offset: 0x000CFC58
		[NonEvent]
		public void ResourceManagerReleasingResources(string baseName, Assembly mainAssembly)
		{
			if (base.IsEnabled())
			{
				this.ResourceManagerReleasingResources(baseName, FrameworkEventSource.GetName(mainAssembly));
			}
		}

		// Token: 0x060035C0 RID: 13760 RVA: 0x000D1A6F File Offset: 0x000CFC6F
		[NonEvent]
		public void ResourceManagerNeutralResourcesNotFound(string baseName, Assembly mainAssembly, string resName)
		{
			if (base.IsEnabled())
			{
				this.ResourceManagerNeutralResourcesNotFound(baseName, FrameworkEventSource.GetName(mainAssembly), resName);
			}
		}

		// Token: 0x060035C1 RID: 13761 RVA: 0x000D1A87 File Offset: 0x000CFC87
		[NonEvent]
		public void ResourceManagerNeutralResourcesFound(string baseName, Assembly mainAssembly, string resName)
		{
			if (base.IsEnabled())
			{
				this.ResourceManagerNeutralResourcesFound(baseName, FrameworkEventSource.GetName(mainAssembly), resName);
			}
		}

		// Token: 0x060035C2 RID: 13762 RVA: 0x000D1A9F File Offset: 0x000CFC9F
		[NonEvent]
		public void ResourceManagerAddingCultureFromConfigFile(string baseName, Assembly mainAssembly, string cultureName)
		{
			if (base.IsEnabled())
			{
				this.ResourceManagerAddingCultureFromConfigFile(baseName, FrameworkEventSource.GetName(mainAssembly), cultureName);
			}
		}

		// Token: 0x060035C3 RID: 13763 RVA: 0x000D1AB7 File Offset: 0x000CFCB7
		[NonEvent]
		public void ResourceManagerCultureNotFoundInConfigFile(string baseName, Assembly mainAssembly, string cultureName)
		{
			if (base.IsEnabled())
			{
				this.ResourceManagerCultureNotFoundInConfigFile(baseName, FrameworkEventSource.GetName(mainAssembly), cultureName);
			}
		}

		// Token: 0x060035C4 RID: 13764 RVA: 0x000D1ACF File Offset: 0x000CFCCF
		[NonEvent]
		public void ResourceManagerCultureFoundInConfigFile(string baseName, Assembly mainAssembly, string cultureName)
		{
			if (base.IsEnabled())
			{
				this.ResourceManagerCultureFoundInConfigFile(baseName, FrameworkEventSource.GetName(mainAssembly), cultureName);
			}
		}

		// Token: 0x060035C5 RID: 13765 RVA: 0x000D1AE7 File Offset: 0x000CFCE7
		private static string GetName(Assembly assembly)
		{
			if (assembly == null)
			{
				return "<<NULL>>";
			}
			return assembly.FullName;
		}

		// Token: 0x060035C6 RID: 13766 RVA: 0x000D1AFE File Offset: 0x000CFCFE
		[Event(30, Level = EventLevel.Verbose, Keywords = (EventKeywords)18L)]
		public void ThreadPoolEnqueueWork(long workID)
		{
			base.WriteEvent(30, workID);
		}

		// Token: 0x060035C7 RID: 13767 RVA: 0x000D1B09 File Offset: 0x000CFD09
		[NonEvent]
		[SecuritySafeCritical]
		public unsafe void ThreadPoolEnqueueWorkObject(object workID)
		{
			this.ThreadPoolEnqueueWork((long)((ulong)(*(IntPtr*)(void*)JitHelpers.UnsafeCastToStackPointer<object>(ref workID))));
		}

		// Token: 0x060035C8 RID: 13768 RVA: 0x000D1B1F File Offset: 0x000CFD1F
		[Event(31, Level = EventLevel.Verbose, Keywords = (EventKeywords)18L)]
		public void ThreadPoolDequeueWork(long workID)
		{
			base.WriteEvent(31, workID);
		}

		// Token: 0x060035C9 RID: 13769 RVA: 0x000D1B2A File Offset: 0x000CFD2A
		[NonEvent]
		[SecuritySafeCritical]
		public unsafe void ThreadPoolDequeueWorkObject(object workID)
		{
			this.ThreadPoolDequeueWork((long)((ulong)(*(IntPtr*)(void*)JitHelpers.UnsafeCastToStackPointer<object>(ref workID))));
		}

		// Token: 0x060035CA RID: 13770 RVA: 0x000D1B40 File Offset: 0x000CFD40
		[Event(140, Level = EventLevel.Informational, Keywords = (EventKeywords)4L, ActivityOptions = EventActivityOptions.Disable, Task = (EventTask)1, Opcode = EventOpcode.Start, Version = 1)]
		private void GetResponseStart(long id, string uri, bool success, bool synchronous)
		{
			this.WriteEvent(140, id, uri, success, synchronous);
		}

		// Token: 0x060035CB RID: 13771 RVA: 0x000D1B52 File Offset: 0x000CFD52
		[Event(141, Level = EventLevel.Informational, Keywords = (EventKeywords)4L, ActivityOptions = EventActivityOptions.Disable, Task = (EventTask)1, Opcode = EventOpcode.Stop, Version = 1)]
		private void GetResponseStop(long id, bool success, bool synchronous, int statusCode)
		{
			this.WriteEvent(141, id, success, synchronous, statusCode);
		}

		// Token: 0x060035CC RID: 13772 RVA: 0x000D1B64 File Offset: 0x000CFD64
		[Event(142, Level = EventLevel.Informational, Keywords = (EventKeywords)4L, ActivityOptions = EventActivityOptions.Disable, Task = (EventTask)2, Opcode = EventOpcode.Start, Version = 1)]
		private void GetRequestStreamStart(long id, string uri, bool success, bool synchronous)
		{
			this.WriteEvent(142, id, uri, success, synchronous);
		}

		// Token: 0x060035CD RID: 13773 RVA: 0x000D1B76 File Offset: 0x000CFD76
		[Event(143, Level = EventLevel.Informational, Keywords = (EventKeywords)4L, ActivityOptions = EventActivityOptions.Disable, Task = (EventTask)2, Opcode = EventOpcode.Stop, Version = 1)]
		private void GetRequestStreamStop(long id, bool success, bool synchronous)
		{
			this.WriteEvent(143, id, success, synchronous);
		}

		// Token: 0x060035CE RID: 13774 RVA: 0x000D1B86 File Offset: 0x000CFD86
		[NonEvent]
		[SecuritySafeCritical]
		public void BeginGetResponse(object id, string uri, bool success, bool synchronous)
		{
			if (base.IsEnabled())
			{
				this.GetResponseStart(FrameworkEventSource.IdForObject(id), uri, success, synchronous);
			}
		}

		// Token: 0x060035CF RID: 13775 RVA: 0x000D1BA0 File Offset: 0x000CFDA0
		[NonEvent]
		[SecuritySafeCritical]
		public void EndGetResponse(object id, bool success, bool synchronous, int statusCode)
		{
			if (base.IsEnabled())
			{
				this.GetResponseStop(FrameworkEventSource.IdForObject(id), success, synchronous, statusCode);
			}
		}

		// Token: 0x060035D0 RID: 13776 RVA: 0x000D1BBA File Offset: 0x000CFDBA
		[NonEvent]
		[SecuritySafeCritical]
		public void BeginGetRequestStream(object id, string uri, bool success, bool synchronous)
		{
			if (base.IsEnabled())
			{
				this.GetRequestStreamStart(FrameworkEventSource.IdForObject(id), uri, success, synchronous);
			}
		}

		// Token: 0x060035D1 RID: 13777 RVA: 0x000D1BD4 File Offset: 0x000CFDD4
		[NonEvent]
		[SecuritySafeCritical]
		public void EndGetRequestStream(object id, bool success, bool synchronous)
		{
			if (base.IsEnabled())
			{
				this.GetRequestStreamStop(FrameworkEventSource.IdForObject(id), success, synchronous);
			}
		}

		// Token: 0x060035D2 RID: 13778 RVA: 0x000D1BEC File Offset: 0x000CFDEC
		[Event(150, Level = EventLevel.Informational, Keywords = (EventKeywords)16L, Task = (EventTask)3, Opcode = EventOpcode.Send)]
		public void ThreadTransferSend(long id, int kind, string info, bool multiDequeues)
		{
			if (base.IsEnabled())
			{
				this.WriteEvent(150, id, kind, info, multiDequeues);
			}
		}

		// Token: 0x060035D3 RID: 13779 RVA: 0x000D1C06 File Offset: 0x000CFE06
		[NonEvent]
		[SecuritySafeCritical]
		public unsafe void ThreadTransferSendObj(object id, int kind, string info, bool multiDequeues)
		{
			this.ThreadTransferSend((long)((ulong)(*(IntPtr*)(void*)JitHelpers.UnsafeCastToStackPointer<object>(ref id))), kind, info, multiDequeues);
		}

		// Token: 0x060035D4 RID: 13780 RVA: 0x000D1C20 File Offset: 0x000CFE20
		[Event(151, Level = EventLevel.Informational, Keywords = (EventKeywords)16L, Task = (EventTask)3, Opcode = EventOpcode.Receive)]
		public void ThreadTransferReceive(long id, int kind, string info)
		{
			if (base.IsEnabled())
			{
				this.WriteEvent(151, id, kind, info);
			}
		}

		// Token: 0x060035D5 RID: 13781 RVA: 0x000D1C38 File Offset: 0x000CFE38
		[NonEvent]
		[SecuritySafeCritical]
		public unsafe void ThreadTransferReceiveObj(object id, int kind, string info)
		{
			this.ThreadTransferReceive((long)((ulong)(*(IntPtr*)(void*)JitHelpers.UnsafeCastToStackPointer<object>(ref id))), kind, info);
		}

		// Token: 0x060035D6 RID: 13782 RVA: 0x000D1C50 File Offset: 0x000CFE50
		[Event(152, Level = EventLevel.Informational, Keywords = (EventKeywords)16L, Task = (EventTask)3, Opcode = (EventOpcode)11)]
		public void ThreadTransferReceiveHandled(long id, int kind, string info)
		{
			if (base.IsEnabled())
			{
				this.WriteEvent(152, id, kind, info);
			}
		}

		// Token: 0x060035D7 RID: 13783 RVA: 0x000D1C68 File Offset: 0x000CFE68
		[NonEvent]
		[SecuritySafeCritical]
		public unsafe void ThreadTransferReceiveHandledObj(object id, int kind, string info)
		{
			this.ThreadTransferReceive((long)((ulong)(*(IntPtr*)(void*)JitHelpers.UnsafeCastToStackPointer<object>(ref id))), kind, info);
		}

		// Token: 0x060035D8 RID: 13784 RVA: 0x000D1C80 File Offset: 0x000CFE80
		private static long IdForObject(object obj)
		{
			return (long)obj.GetHashCode() + 9223372032559808512L;
		}

		// Token: 0x040017E7 RID: 6119
		public static readonly FrameworkEventSource Log = new FrameworkEventSource();

		// Token: 0x02000B97 RID: 2967
		public static class Keywords
		{
			// Token: 0x04003525 RID: 13605
			public const EventKeywords Loader = (EventKeywords)1L;

			// Token: 0x04003526 RID: 13606
			public const EventKeywords ThreadPool = (EventKeywords)2L;

			// Token: 0x04003527 RID: 13607
			public const EventKeywords NetClient = (EventKeywords)4L;

			// Token: 0x04003528 RID: 13608
			public const EventKeywords DynamicTypeUsage = (EventKeywords)8L;

			// Token: 0x04003529 RID: 13609
			public const EventKeywords ThreadTransfer = (EventKeywords)16L;
		}

		// Token: 0x02000B98 RID: 2968
		[FriendAccessAllowed]
		public static class Tasks
		{
			// Token: 0x0400352A RID: 13610
			public const EventTask GetResponse = (EventTask)1;

			// Token: 0x0400352B RID: 13611
			public const EventTask GetRequestStream = (EventTask)2;

			// Token: 0x0400352C RID: 13612
			public const EventTask ThreadTransfer = (EventTask)3;
		}

		// Token: 0x02000B99 RID: 2969
		[FriendAccessAllowed]
		public static class Opcodes
		{
			// Token: 0x0400352D RID: 13613
			public const EventOpcode ReceiveHandled = (EventOpcode)11;
		}
	}
}
