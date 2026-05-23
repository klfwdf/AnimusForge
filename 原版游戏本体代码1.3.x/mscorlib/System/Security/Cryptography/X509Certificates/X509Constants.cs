using System;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002A9 RID: 681
	internal static class X509Constants
	{
		// Token: 0x04000D48 RID: 3400
		internal const uint CRYPT_EXPORTABLE = 1U;

		// Token: 0x04000D49 RID: 3401
		internal const uint CRYPT_USER_PROTECTED = 2U;

		// Token: 0x04000D4A RID: 3402
		internal const uint CRYPT_MACHINE_KEYSET = 32U;

		// Token: 0x04000D4B RID: 3403
		internal const uint CRYPT_USER_KEYSET = 4096U;

		// Token: 0x04000D4C RID: 3404
		internal const uint PKCS12_ALWAYS_CNG_KSP = 512U;

		// Token: 0x04000D4D RID: 3405
		internal const uint PKCS12_NO_PERSIST_KEY = 32768U;

		// Token: 0x04000D4E RID: 3406
		internal const uint CERT_QUERY_CONTENT_CERT = 1U;

		// Token: 0x04000D4F RID: 3407
		internal const uint CERT_QUERY_CONTENT_CTL = 2U;

		// Token: 0x04000D50 RID: 3408
		internal const uint CERT_QUERY_CONTENT_CRL = 3U;

		// Token: 0x04000D51 RID: 3409
		internal const uint CERT_QUERY_CONTENT_SERIALIZED_STORE = 4U;

		// Token: 0x04000D52 RID: 3410
		internal const uint CERT_QUERY_CONTENT_SERIALIZED_CERT = 5U;

		// Token: 0x04000D53 RID: 3411
		internal const uint CERT_QUERY_CONTENT_SERIALIZED_CTL = 6U;

		// Token: 0x04000D54 RID: 3412
		internal const uint CERT_QUERY_CONTENT_SERIALIZED_CRL = 7U;

		// Token: 0x04000D55 RID: 3413
		internal const uint CERT_QUERY_CONTENT_PKCS7_SIGNED = 8U;

		// Token: 0x04000D56 RID: 3414
		internal const uint CERT_QUERY_CONTENT_PKCS7_UNSIGNED = 9U;

		// Token: 0x04000D57 RID: 3415
		internal const uint CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED = 10U;

		// Token: 0x04000D58 RID: 3416
		internal const uint CERT_QUERY_CONTENT_PKCS10 = 11U;

		// Token: 0x04000D59 RID: 3417
		internal const uint CERT_QUERY_CONTENT_PFX = 12U;

		// Token: 0x04000D5A RID: 3418
		internal const uint CERT_QUERY_CONTENT_CERT_PAIR = 13U;

		// Token: 0x04000D5B RID: 3419
		internal const uint CERT_STORE_PROV_MEMORY = 2U;

		// Token: 0x04000D5C RID: 3420
		internal const uint CERT_STORE_PROV_SYSTEM = 10U;

		// Token: 0x04000D5D RID: 3421
		internal const uint CERT_STORE_NO_CRYPT_RELEASE_FLAG = 1U;

		// Token: 0x04000D5E RID: 3422
		internal const uint CERT_STORE_SET_LOCALIZED_NAME_FLAG = 2U;

		// Token: 0x04000D5F RID: 3423
		internal const uint CERT_STORE_DEFER_CLOSE_UNTIL_LAST_FREE_FLAG = 4U;

		// Token: 0x04000D60 RID: 3424
		internal const uint CERT_STORE_DELETE_FLAG = 16U;

		// Token: 0x04000D61 RID: 3425
		internal const uint CERT_STORE_SHARE_STORE_FLAG = 64U;

		// Token: 0x04000D62 RID: 3426
		internal const uint CERT_STORE_SHARE_CONTEXT_FLAG = 128U;

		// Token: 0x04000D63 RID: 3427
		internal const uint CERT_STORE_MANIFOLD_FLAG = 256U;

		// Token: 0x04000D64 RID: 3428
		internal const uint CERT_STORE_ENUM_ARCHIVED_FLAG = 512U;

		// Token: 0x04000D65 RID: 3429
		internal const uint CERT_STORE_UPDATE_KEYID_FLAG = 1024U;

		// Token: 0x04000D66 RID: 3430
		internal const uint CERT_STORE_BACKUP_RESTORE_FLAG = 2048U;

		// Token: 0x04000D67 RID: 3431
		internal const uint CERT_STORE_READONLY_FLAG = 32768U;

		// Token: 0x04000D68 RID: 3432
		internal const uint CERT_STORE_OPEN_EXISTING_FLAG = 16384U;

		// Token: 0x04000D69 RID: 3433
		internal const uint CERT_STORE_CREATE_NEW_FLAG = 8192U;

		// Token: 0x04000D6A RID: 3434
		internal const uint CERT_STORE_MAXIMUM_ALLOWED_FLAG = 4096U;

		// Token: 0x04000D6B RID: 3435
		internal const uint CERT_NAME_EMAIL_TYPE = 1U;

		// Token: 0x04000D6C RID: 3436
		internal const uint CERT_NAME_RDN_TYPE = 2U;

		// Token: 0x04000D6D RID: 3437
		internal const uint CERT_NAME_SIMPLE_DISPLAY_TYPE = 4U;

		// Token: 0x04000D6E RID: 3438
		internal const uint CERT_NAME_FRIENDLY_DISPLAY_TYPE = 5U;

		// Token: 0x04000D6F RID: 3439
		internal const uint CERT_NAME_DNS_TYPE = 6U;

		// Token: 0x04000D70 RID: 3440
		internal const uint CERT_NAME_URL_TYPE = 7U;

		// Token: 0x04000D71 RID: 3441
		internal const uint CERT_NAME_UPN_TYPE = 8U;
	}
}
