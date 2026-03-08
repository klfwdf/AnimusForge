using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006F2 RID: 1778
	[StructLayout(LayoutKind.Sequential)]
	internal class AssemblyReferenceEntry
	{
		// Token: 0x04002371 RID: 9073
		public IReferenceIdentity ReferenceIdentity;

		// Token: 0x04002372 RID: 9074
		public uint Flags;

		// Token: 0x04002373 RID: 9075
		public AssemblyReferenceDependentAssemblyEntry DependentAssembly;
	}
}
