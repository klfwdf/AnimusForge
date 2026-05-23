using System;
using System.Runtime.InteropServices;

namespace System.Security.Claims
{
	// Token: 0x02000320 RID: 800
	[ComVisible(false)]
	public static class ClaimValueTypes
	{
		// Token: 0x04000FE8 RID: 4072
		private const string XmlSchemaNamespace = "http://www.w3.org/2001/XMLSchema";

		// Token: 0x04000FE9 RID: 4073
		public const string Base64Binary = "http://www.w3.org/2001/XMLSchema#base64Binary";

		// Token: 0x04000FEA RID: 4074
		public const string Base64Octet = "http://www.w3.org/2001/XMLSchema#base64Octet";

		// Token: 0x04000FEB RID: 4075
		public const string Boolean = "http://www.w3.org/2001/XMLSchema#boolean";

		// Token: 0x04000FEC RID: 4076
		public const string Date = "http://www.w3.org/2001/XMLSchema#date";

		// Token: 0x04000FED RID: 4077
		public const string DateTime = "http://www.w3.org/2001/XMLSchema#dateTime";

		// Token: 0x04000FEE RID: 4078
		public const string Double = "http://www.w3.org/2001/XMLSchema#double";

		// Token: 0x04000FEF RID: 4079
		public const string Fqbn = "http://www.w3.org/2001/XMLSchema#fqbn";

		// Token: 0x04000FF0 RID: 4080
		public const string HexBinary = "http://www.w3.org/2001/XMLSchema#hexBinary";

		// Token: 0x04000FF1 RID: 4081
		public const string Integer = "http://www.w3.org/2001/XMLSchema#integer";

		// Token: 0x04000FF2 RID: 4082
		public const string Integer32 = "http://www.w3.org/2001/XMLSchema#integer32";

		// Token: 0x04000FF3 RID: 4083
		public const string Integer64 = "http://www.w3.org/2001/XMLSchema#integer64";

		// Token: 0x04000FF4 RID: 4084
		public const string Sid = "http://www.w3.org/2001/XMLSchema#sid";

		// Token: 0x04000FF5 RID: 4085
		public const string String = "http://www.w3.org/2001/XMLSchema#string";

		// Token: 0x04000FF6 RID: 4086
		public const string Time = "http://www.w3.org/2001/XMLSchema#time";

		// Token: 0x04000FF7 RID: 4087
		public const string UInteger32 = "http://www.w3.org/2001/XMLSchema#uinteger32";

		// Token: 0x04000FF8 RID: 4088
		public const string UInteger64 = "http://www.w3.org/2001/XMLSchema#uinteger64";

		// Token: 0x04000FF9 RID: 4089
		private const string SoapSchemaNamespace = "http://schemas.xmlsoap.org/";

		// Token: 0x04000FFA RID: 4090
		public const string DnsName = "http://schemas.xmlsoap.org/claims/dns";

		// Token: 0x04000FFB RID: 4091
		public const string Email = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";

		// Token: 0x04000FFC RID: 4092
		public const string Rsa = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/rsa";

		// Token: 0x04000FFD RID: 4093
		public const string UpnName = "http://schemas.xmlsoap.org/claims/UPN";

		// Token: 0x04000FFE RID: 4094
		private const string XmlSignatureConstantsNamespace = "http://www.w3.org/2000/09/xmldsig#";

		// Token: 0x04000FFF RID: 4095
		public const string DsaKeyValue = "http://www.w3.org/2000/09/xmldsig#DSAKeyValue";

		// Token: 0x04001000 RID: 4096
		public const string KeyInfo = "http://www.w3.org/2000/09/xmldsig#KeyInfo";

		// Token: 0x04001001 RID: 4097
		public const string RsaKeyValue = "http://www.w3.org/2000/09/xmldsig#RSAKeyValue";

		// Token: 0x04001002 RID: 4098
		private const string XQueryOperatorsNameSpace = "http://www.w3.org/TR/2002/WD-xquery-operators-20020816";

		// Token: 0x04001003 RID: 4099
		public const string DaytimeDuration = "http://www.w3.org/TR/2002/WD-xquery-operators-20020816#dayTimeDuration";

		// Token: 0x04001004 RID: 4100
		public const string YearMonthDuration = "http://www.w3.org/TR/2002/WD-xquery-operators-20020816#yearMonthDuration";

		// Token: 0x04001005 RID: 4101
		private const string Xacml10Namespace = "urn:oasis:names:tc:xacml:1.0";

		// Token: 0x04001006 RID: 4102
		public const string Rfc822Name = "urn:oasis:names:tc:xacml:1.0:data-type:rfc822Name";

		// Token: 0x04001007 RID: 4103
		public const string X500Name = "urn:oasis:names:tc:xacml:1.0:data-type:x500Name";
	}
}
