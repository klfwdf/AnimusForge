using System;

namespace System.Security.Cryptography
{
	// Token: 0x020002A5 RID: 677
	internal static class Constants
	{
		// Token: 0x04000CFE RID: 3326
		internal const int S_OK = 0;

		// Token: 0x04000CFF RID: 3327
		internal const int NTE_FILENOTFOUND = -2147024894;

		// Token: 0x04000D00 RID: 3328
		internal const int NTE_NO_KEY = -2146893811;

		// Token: 0x04000D01 RID: 3329
		internal const int NTE_BAD_KEYSET = -2146893802;

		// Token: 0x04000D02 RID: 3330
		internal const int NTE_KEYSET_NOT_DEF = -2146893799;

		// Token: 0x04000D03 RID: 3331
		internal const int KP_IV = 1;

		// Token: 0x04000D04 RID: 3332
		internal const int KP_MODE = 4;

		// Token: 0x04000D05 RID: 3333
		internal const int KP_MODE_BITS = 5;

		// Token: 0x04000D06 RID: 3334
		internal const int KP_EFFECTIVE_KEYLEN = 19;

		// Token: 0x04000D07 RID: 3335
		internal const int ALG_CLASS_SIGNATURE = 8192;

		// Token: 0x04000D08 RID: 3336
		internal const int ALG_CLASS_DATA_ENCRYPT = 24576;

		// Token: 0x04000D09 RID: 3337
		internal const int ALG_CLASS_HASH = 32768;

		// Token: 0x04000D0A RID: 3338
		internal const int ALG_CLASS_KEY_EXCHANGE = 40960;

		// Token: 0x04000D0B RID: 3339
		internal const int ALG_TYPE_DSS = 512;

		// Token: 0x04000D0C RID: 3340
		internal const int ALG_TYPE_RSA = 1024;

		// Token: 0x04000D0D RID: 3341
		internal const int ALG_TYPE_BLOCK = 1536;

		// Token: 0x04000D0E RID: 3342
		internal const int ALG_TYPE_STREAM = 2048;

		// Token: 0x04000D0F RID: 3343
		internal const int ALG_TYPE_ANY = 0;

		// Token: 0x04000D10 RID: 3344
		internal const int CALG_MD5 = 32771;

		// Token: 0x04000D11 RID: 3345
		internal const int CALG_SHA1 = 32772;

		// Token: 0x04000D12 RID: 3346
		internal const int CALG_SHA_256 = 32780;

		// Token: 0x04000D13 RID: 3347
		internal const int CALG_SHA_384 = 32781;

		// Token: 0x04000D14 RID: 3348
		internal const int CALG_SHA_512 = 32782;

		// Token: 0x04000D15 RID: 3349
		internal const int CALG_RSA_KEYX = 41984;

		// Token: 0x04000D16 RID: 3350
		internal const int CALG_RSA_SIGN = 9216;

		// Token: 0x04000D17 RID: 3351
		internal const int CALG_DSS_SIGN = 8704;

		// Token: 0x04000D18 RID: 3352
		internal const int CALG_DES = 26113;

		// Token: 0x04000D19 RID: 3353
		internal const int CALG_RC2 = 26114;

		// Token: 0x04000D1A RID: 3354
		internal const int CALG_3DES = 26115;

		// Token: 0x04000D1B RID: 3355
		internal const int CALG_3DES_112 = 26121;

		// Token: 0x04000D1C RID: 3356
		internal const int CALG_AES_128 = 26126;

		// Token: 0x04000D1D RID: 3357
		internal const int CALG_AES_192 = 26127;

		// Token: 0x04000D1E RID: 3358
		internal const int CALG_AES_256 = 26128;

		// Token: 0x04000D1F RID: 3359
		internal const int CALG_RC4 = 26625;

		// Token: 0x04000D20 RID: 3360
		internal const int PROV_RSA_FULL = 1;

		// Token: 0x04000D21 RID: 3361
		internal const int PROV_DSS_DH = 13;

		// Token: 0x04000D22 RID: 3362
		internal const int PROV_RSA_AES = 24;

		// Token: 0x04000D23 RID: 3363
		internal const int AT_KEYEXCHANGE = 1;

		// Token: 0x04000D24 RID: 3364
		internal const int AT_SIGNATURE = 2;

		// Token: 0x04000D25 RID: 3365
		internal const int PUBLICKEYBLOB = 6;

		// Token: 0x04000D26 RID: 3366
		internal const int PRIVATEKEYBLOB = 7;

		// Token: 0x04000D27 RID: 3367
		internal const int CRYPT_OAEP = 64;

		// Token: 0x04000D28 RID: 3368
		internal const uint CRYPT_VERIFYCONTEXT = 4026531840U;

		// Token: 0x04000D29 RID: 3369
		internal const uint CRYPT_NEWKEYSET = 8U;

		// Token: 0x04000D2A RID: 3370
		internal const uint CRYPT_DELETEKEYSET = 16U;

		// Token: 0x04000D2B RID: 3371
		internal const uint CRYPT_MACHINE_KEYSET = 32U;

		// Token: 0x04000D2C RID: 3372
		internal const uint CRYPT_SILENT = 64U;

		// Token: 0x04000D2D RID: 3373
		internal const uint CRYPT_EXPORTABLE = 1U;

		// Token: 0x04000D2E RID: 3374
		internal const uint CLR_KEYLEN = 1U;

		// Token: 0x04000D2F RID: 3375
		internal const uint CLR_PUBLICKEYONLY = 2U;

		// Token: 0x04000D30 RID: 3376
		internal const uint CLR_EXPORTABLE = 3U;

		// Token: 0x04000D31 RID: 3377
		internal const uint CLR_REMOVABLE = 4U;

		// Token: 0x04000D32 RID: 3378
		internal const uint CLR_HARDWARE = 5U;

		// Token: 0x04000D33 RID: 3379
		internal const uint CLR_ACCESSIBLE = 6U;

		// Token: 0x04000D34 RID: 3380
		internal const uint CLR_PROTECTED = 7U;

		// Token: 0x04000D35 RID: 3381
		internal const uint CLR_UNIQUE_CONTAINER = 8U;

		// Token: 0x04000D36 RID: 3382
		internal const uint CLR_ALGID = 9U;

		// Token: 0x04000D37 RID: 3383
		internal const uint CLR_PP_CLIENT_HWND = 10U;

		// Token: 0x04000D38 RID: 3384
		internal const uint CLR_PP_PIN = 11U;

		// Token: 0x04000D39 RID: 3385
		internal const string OID_RSA_SMIMEalgCMS3DESwrap = "1.2.840.113549.1.9.16.3.6";

		// Token: 0x04000D3A RID: 3386
		internal const string OID_RSA_MD5 = "1.2.840.113549.2.5";

		// Token: 0x04000D3B RID: 3387
		internal const string OID_RSA_RC2CBC = "1.2.840.113549.3.2";

		// Token: 0x04000D3C RID: 3388
		internal const string OID_RSA_DES_EDE3_CBC = "1.2.840.113549.3.7";

		// Token: 0x04000D3D RID: 3389
		internal const string OID_OIWSEC_desCBC = "1.3.14.3.2.7";

		// Token: 0x04000D3E RID: 3390
		internal const string OID_OIWSEC_SHA1 = "1.3.14.3.2.26";

		// Token: 0x04000D3F RID: 3391
		internal const string OID_OIWSEC_SHA256 = "2.16.840.1.101.3.4.2.1";

		// Token: 0x04000D40 RID: 3392
		internal const string OID_OIWSEC_SHA384 = "2.16.840.1.101.3.4.2.2";

		// Token: 0x04000D41 RID: 3393
		internal const string OID_OIWSEC_SHA512 = "2.16.840.1.101.3.4.2.3";

		// Token: 0x04000D42 RID: 3394
		internal const string OID_OIWSEC_RIPEMD160 = "1.3.36.3.2.1";
	}
}
