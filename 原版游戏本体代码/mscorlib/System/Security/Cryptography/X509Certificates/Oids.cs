using System;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002BC RID: 700
	internal static class Oids
	{
		// Token: 0x04000DCF RID: 3535
		internal static readonly byte[] Pkcs7Data = new byte[] { 42, 134, 72, 134, 247, 13, 1, 7, 1 };

		// Token: 0x04000DD0 RID: 3536
		internal static readonly byte[] Pkcs7Encrypted = new byte[] { 42, 134, 72, 134, 247, 13, 1, 7, 6 };

		// Token: 0x04000DD1 RID: 3537
		internal static readonly byte[] Pkcs12ShroudedKeyBag = new byte[]
		{
			42, 134, 72, 134, 247, 13, 1, 12, 10, 1,
			2
		};

		// Token: 0x04000DD2 RID: 3538
		internal static readonly byte[] PasswordBasedEncryptionScheme2 = new byte[] { 42, 134, 72, 134, 247, 13, 1, 5, 13 };

		// Token: 0x04000DD3 RID: 3539
		internal static readonly byte[] Pbkdf2 = new byte[] { 42, 134, 72, 134, 247, 13, 1, 5, 12 };

		// Token: 0x04000DD4 RID: 3540
		internal static readonly byte[] PbeWithMD5AndDESCBC = new byte[] { 42, 134, 72, 134, 247, 13, 1, 5, 3 };

		// Token: 0x04000DD5 RID: 3541
		internal static readonly byte[] PbeWithMD5AndRC2CBC = new byte[] { 42, 134, 72, 134, 247, 13, 1, 5, 6 };

		// Token: 0x04000DD6 RID: 3542
		internal static readonly byte[] PbeWithSha1AndDESCBC = new byte[] { 42, 134, 72, 134, 247, 13, 1, 5, 10 };

		// Token: 0x04000DD7 RID: 3543
		internal static readonly byte[] PbeWithSha1AndRC2CBC = new byte[] { 42, 134, 72, 134, 247, 13, 1, 5, 11 };

		// Token: 0x04000DD8 RID: 3544
		internal static readonly byte[] Pkcs12PbeWithShaAnd3Key3Des = new byte[] { 42, 134, 72, 134, 247, 13, 1, 12, 1, 3 };

		// Token: 0x04000DD9 RID: 3545
		internal static readonly byte[] Pkcs12PbeWithShaAnd2Key3Des = new byte[] { 42, 134, 72, 134, 247, 13, 1, 12, 1, 4 };

		// Token: 0x04000DDA RID: 3546
		internal static readonly byte[] Pkcs12PbeWithShaAnd128BitRC2 = new byte[] { 42, 134, 72, 134, 247, 13, 1, 12, 1, 5 };

		// Token: 0x04000DDB RID: 3547
		internal static readonly byte[] Pkcs12PbeWithShaAnd40BitRC2 = new byte[] { 42, 134, 72, 134, 247, 13, 1, 12, 1, 6 };

		// Token: 0x04000DDC RID: 3548
		internal static readonly byte[] Aes128Cbc = new byte[] { 96, 134, 72, 1, 101, 3, 4, 1, 2 };

		// Token: 0x04000DDD RID: 3549
		internal static readonly byte[] Aes192Cbc = new byte[] { 96, 134, 72, 1, 101, 3, 4, 1, 22 };

		// Token: 0x04000DDE RID: 3550
		internal static readonly byte[] Aes256Cbc = new byte[] { 96, 134, 72, 1, 101, 3, 4, 1, 42 };

		// Token: 0x04000DDF RID: 3551
		internal static readonly byte[] TripleDesCbc = new byte[] { 42, 134, 72, 134, 247, 13, 3, 7 };

		// Token: 0x04000DE0 RID: 3552
		internal static readonly byte[] Rc2Cbc = new byte[] { 42, 134, 72, 134, 247, 13, 3, 2 };

		// Token: 0x04000DE1 RID: 3553
		internal static readonly byte[] DesCbc = new byte[] { 43, 14, 3, 2, 7 };

		// Token: 0x04000DE2 RID: 3554
		internal static readonly byte[] HmacWithSha1 = new byte[] { 42, 134, 72, 134, 247, 13, 2, 7 };

		// Token: 0x04000DE3 RID: 3555
		internal static readonly byte[] HmacWithSha256 = new byte[] { 42, 134, 72, 134, 247, 13, 2, 9 };

		// Token: 0x04000DE4 RID: 3556
		internal static readonly byte[] HmacWithSha384 = new byte[] { 42, 134, 72, 134, 247, 13, 2, 10 };

		// Token: 0x04000DE5 RID: 3557
		internal static readonly byte[] HmacWithSha512 = new byte[] { 42, 134, 72, 134, 247, 13, 2, 11 };
	}
}
