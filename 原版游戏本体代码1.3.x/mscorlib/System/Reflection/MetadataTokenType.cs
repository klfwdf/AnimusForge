using System;

namespace System.Reflection
{
	// Token: 0x020005FA RID: 1530
	[Serializable]
	internal enum MetadataTokenType
	{
		// Token: 0x04001D34 RID: 7476
		Module,
		// Token: 0x04001D35 RID: 7477
		TypeRef = 16777216,
		// Token: 0x04001D36 RID: 7478
		TypeDef = 33554432,
		// Token: 0x04001D37 RID: 7479
		FieldDef = 67108864,
		// Token: 0x04001D38 RID: 7480
		MethodDef = 100663296,
		// Token: 0x04001D39 RID: 7481
		ParamDef = 134217728,
		// Token: 0x04001D3A RID: 7482
		InterfaceImpl = 150994944,
		// Token: 0x04001D3B RID: 7483
		MemberRef = 167772160,
		// Token: 0x04001D3C RID: 7484
		CustomAttribute = 201326592,
		// Token: 0x04001D3D RID: 7485
		Permission = 234881024,
		// Token: 0x04001D3E RID: 7486
		Signature = 285212672,
		// Token: 0x04001D3F RID: 7487
		Event = 335544320,
		// Token: 0x04001D40 RID: 7488
		Property = 385875968,
		// Token: 0x04001D41 RID: 7489
		ModuleRef = 436207616,
		// Token: 0x04001D42 RID: 7490
		TypeSpec = 452984832,
		// Token: 0x04001D43 RID: 7491
		Assembly = 536870912,
		// Token: 0x04001D44 RID: 7492
		AssemblyRef = 587202560,
		// Token: 0x04001D45 RID: 7493
		File = 637534208,
		// Token: 0x04001D46 RID: 7494
		ExportedType = 654311424,
		// Token: 0x04001D47 RID: 7495
		ManifestResource = 671088640,
		// Token: 0x04001D48 RID: 7496
		GenericPar = 704643072,
		// Token: 0x04001D49 RID: 7497
		MethodSpec = 721420288,
		// Token: 0x04001D4A RID: 7498
		String = 1879048192,
		// Token: 0x04001D4B RID: 7499
		Name = 1895825408,
		// Token: 0x04001D4C RID: 7500
		BaseType = 1912602624,
		// Token: 0x04001D4D RID: 7501
		Invalid = 2147483647
	}
}
