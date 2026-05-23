using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006C7 RID: 1735
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("a504e5b0-8ccf-4cb4-9902-c9d1b9abd033")]
	[ComImport]
	internal interface ICMS
	{
		// Token: 0x17000CAF RID: 3247
		// (get) Token: 0x06005040 RID: 20544
		IDefinitionIdentity Identity
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CB0 RID: 3248
		// (get) Token: 0x06005041 RID: 20545
		ISection FileSection
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CB1 RID: 3249
		// (get) Token: 0x06005042 RID: 20546
		ISection CategoryMembershipSection
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CB2 RID: 3250
		// (get) Token: 0x06005043 RID: 20547
		ISection COMRedirectionSection
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CB3 RID: 3251
		// (get) Token: 0x06005044 RID: 20548
		ISection ProgIdRedirectionSection
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CB4 RID: 3252
		// (get) Token: 0x06005045 RID: 20549
		ISection CLRSurrogateSection
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CB5 RID: 3253
		// (get) Token: 0x06005046 RID: 20550
		ISection AssemblyReferenceSection
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CB6 RID: 3254
		// (get) Token: 0x06005047 RID: 20551
		ISection WindowClassSection
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CB7 RID: 3255
		// (get) Token: 0x06005048 RID: 20552
		ISection StringSection
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CB8 RID: 3256
		// (get) Token: 0x06005049 RID: 20553
		ISection EntryPointSection
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CB9 RID: 3257
		// (get) Token: 0x0600504A RID: 20554
		ISection PermissionSetSection
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CBA RID: 3258
		// (get) Token: 0x0600504B RID: 20555
		ISectionEntry MetadataSectionEntry
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CBB RID: 3259
		// (get) Token: 0x0600504C RID: 20556
		ISection AssemblyRequestSection
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CBC RID: 3260
		// (get) Token: 0x0600504D RID: 20557
		ISection RegistryKeySection
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CBD RID: 3261
		// (get) Token: 0x0600504E RID: 20558
		ISection DirectorySection
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CBE RID: 3262
		// (get) Token: 0x0600504F RID: 20559
		ISection FileAssociationSection
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CBF RID: 3263
		// (get) Token: 0x06005050 RID: 20560
		ISection CompatibleFrameworksSection
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CC0 RID: 3264
		// (get) Token: 0x06005051 RID: 20561
		ISection EventSection
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CC1 RID: 3265
		// (get) Token: 0x06005052 RID: 20562
		ISection EventMapSection
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CC2 RID: 3266
		// (get) Token: 0x06005053 RID: 20563
		ISection EventTagSection
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CC3 RID: 3267
		// (get) Token: 0x06005054 RID: 20564
		ISection CounterSetSection
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CC4 RID: 3268
		// (get) Token: 0x06005055 RID: 20565
		ISection CounterSection
		{
			[SecurityCritical]
			get;
		}
	}
}
