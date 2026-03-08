using System;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002D6 RID: 726
	internal static class SR
	{
		// Token: 0x04000E29 RID: 3625
		internal const string Argument_InvalidValue = "Value was invalid.";

		// Token: 0x04000E2A RID: 3626
		internal const string Argument_SourceOverlapsDestination = "The destination buffer overlaps the source buffer.";

		// Token: 0x04000E2B RID: 3627
		internal const string Argument_UniversalValueIsFixed = "Tags with TagClass Universal must have the appropriate TagValue value for the data type being read or written.";

		// Token: 0x04000E2C RID: 3628
		internal const string BCryptAlgorithmHandle_ProviderNotFound = "A provider could not be found for algorithm '{0}'.";

		// Token: 0x04000E2D RID: 3629
		internal const string BCryptDeriveKeyPBKDF2_Failed = "A call to BCryptDeriveKeyPBKDF2 failed with code '{0}'.";

		// Token: 0x04000E2E RID: 3630
		internal const string ContentException_CerRequiresIndefiniteLength = "A constructed tag used a definite length encoding, which is invalid for CER data. The input may be encoded with BER or DER.";

		// Token: 0x04000E2F RID: 3631
		internal const string ContentException_ConstructedEncodingRequired = "The encoded value uses a primitive encoding, which is invalid for '{0}' values.";

		// Token: 0x04000E30 RID: 3632
		internal const string ContentException_DefaultMessage = "The ASN.1 value is invalid.";

		// Token: 0x04000E31 RID: 3633
		internal const string ContentException_InvalidTag = "The provided data does not represent a valid tag.";

		// Token: 0x04000E32 RID: 3634
		internal const string ContentException_InvalidUnderCerOrDer_TryBer = "The encoded value is not valid under the selected encoding, but it may be valid under the BER encoding.";

		// Token: 0x04000E33 RID: 3635
		internal const string ContentException_InvalidUnderCer_TryBerOrDer = "The encoded value is not valid under the selected encoding, but it may be valid under the BER or DER encoding.";

		// Token: 0x04000E34 RID: 3636
		internal const string ContentException_InvalidUnderDer_TryBerOrCer = "The encoded value is not valid under the selected encoding, but it may be valid under the BER or CER encoding.";

		// Token: 0x04000E35 RID: 3637
		internal const string ContentException_LengthExceedsPayload = "The encoded length exceeds the number of bytes remaining in the input buffer.";

		// Token: 0x04000E36 RID: 3638
		internal const string ContentException_LengthRuleSetConstraint = "The encoded length is not valid under the requested encoding rules, the value may be valid under the BER encoding.";

		// Token: 0x04000E37 RID: 3639
		internal const string ContentException_LengthTooBig = "The encoded length exceeds the maximum supported by this library (Int32.MaxValue).";

		// Token: 0x04000E38 RID: 3640
		internal const string ContentException_PrimitiveEncodingRequired = "The encoded value uses a constructed encoding, which is invalid for '{0}' values.";

		// Token: 0x04000E39 RID: 3641
		internal const string ContentException_SetOfNotSorted = "The encoded set is not sorted as required by the current encoding rules. The value may be valid under the BER encoding, or you can ignore the sort validation by specifying skipSortValidation=true.";

		// Token: 0x04000E3A RID: 3642
		internal const string ContentException_TooMuchData = "The last expected value has been read, but the reader still has pending data. This value may be from a newer schema, or is corrupt.";

		// Token: 0x04000E3B RID: 3643
		internal const string ContentException_WrongTag = "The provided data is tagged with '{0}' class value '{1}', but it should have been '{2}' class value '{3}'.";

		// Token: 0x04000E3C RID: 3644
		internal const string Cryptography_AlgKdfRequiresChars = "The KDF requires a char-based password input.";

		// Token: 0x04000E3D RID: 3645
		internal const string Cryptography_Der_Invalid_Encoding = "ASN1 corrupted data.";

		// Token: 0x04000E3E RID: 3646
		internal const string Cryptography_UnknownAlgorithmIdentifier = "The algorithm is unknown, not valid for the requested usage, or was not handled.";

		// Token: 0x04000E3F RID: 3647
		internal const string Cryptography_UnknownHashAlgorithm = "'{0}' is not a known hash algorithm.";
	}
}
