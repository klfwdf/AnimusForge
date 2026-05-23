using System;
using System.Runtime.InteropServices;

namespace System.Security.Claims
{
	// Token: 0x0200031F RID: 799
	[ComVisible(false)]
	public static class ClaimTypes
	{
		// Token: 0x04000FAF RID: 4015
		internal const string ClaimTypeNamespace = "http://schemas.microsoft.com/ws/2008/06/identity/claims";

		// Token: 0x04000FB0 RID: 4016
		public const string AuthenticationInstant = "http://schemas.microsoft.com/ws/2008/06/identity/claims/authenticationinstant";

		// Token: 0x04000FB1 RID: 4017
		public const string AuthenticationMethod = "http://schemas.microsoft.com/ws/2008/06/identity/claims/authenticationmethod";

		// Token: 0x04000FB2 RID: 4018
		public const string CookiePath = "http://schemas.microsoft.com/ws/2008/06/identity/claims/cookiepath";

		// Token: 0x04000FB3 RID: 4019
		public const string DenyOnlyPrimarySid = "http://schemas.microsoft.com/ws/2008/06/identity/claims/denyonlyprimarysid";

		// Token: 0x04000FB4 RID: 4020
		public const string DenyOnlyPrimaryGroupSid = "http://schemas.microsoft.com/ws/2008/06/identity/claims/denyonlyprimarygroupsid";

		// Token: 0x04000FB5 RID: 4021
		public const string DenyOnlyWindowsDeviceGroup = "http://schemas.microsoft.com/ws/2008/06/identity/claims/denyonlywindowsdevicegroup";

		// Token: 0x04000FB6 RID: 4022
		public const string Dsa = "http://schemas.microsoft.com/ws/2008/06/identity/claims/dsa";

		// Token: 0x04000FB7 RID: 4023
		public const string Expiration = "http://schemas.microsoft.com/ws/2008/06/identity/claims/expiration";

		// Token: 0x04000FB8 RID: 4024
		public const string Expired = "http://schemas.microsoft.com/ws/2008/06/identity/claims/expired";

		// Token: 0x04000FB9 RID: 4025
		public const string GroupSid = "http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid";

		// Token: 0x04000FBA RID: 4026
		public const string IsPersistent = "http://schemas.microsoft.com/ws/2008/06/identity/claims/ispersistent";

		// Token: 0x04000FBB RID: 4027
		public const string PrimaryGroupSid = "http://schemas.microsoft.com/ws/2008/06/identity/claims/primarygroupsid";

		// Token: 0x04000FBC RID: 4028
		public const string PrimarySid = "http://schemas.microsoft.com/ws/2008/06/identity/claims/primarysid";

		// Token: 0x04000FBD RID: 4029
		public const string Role = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

		// Token: 0x04000FBE RID: 4030
		public const string SerialNumber = "http://schemas.microsoft.com/ws/2008/06/identity/claims/serialnumber";

		// Token: 0x04000FBF RID: 4031
		public const string UserData = "http://schemas.microsoft.com/ws/2008/06/identity/claims/userdata";

		// Token: 0x04000FC0 RID: 4032
		public const string Version = "http://schemas.microsoft.com/ws/2008/06/identity/claims/version";

		// Token: 0x04000FC1 RID: 4033
		public const string WindowsAccountName = "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsaccountname";

		// Token: 0x04000FC2 RID: 4034
		public const string WindowsDeviceClaim = "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsdeviceclaim";

		// Token: 0x04000FC3 RID: 4035
		public const string WindowsDeviceGroup = "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsdevicegroup";

		// Token: 0x04000FC4 RID: 4036
		public const string WindowsUserClaim = "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsuserclaim";

		// Token: 0x04000FC5 RID: 4037
		public const string WindowsFqbnVersion = "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsfqbnversion";

		// Token: 0x04000FC6 RID: 4038
		public const string WindowsSubAuthority = "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowssubauthority";

		// Token: 0x04000FC7 RID: 4039
		internal const string ClaimType2005Namespace = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims";

		// Token: 0x04000FC8 RID: 4040
		public const string Anonymous = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/anonymous";

		// Token: 0x04000FC9 RID: 4041
		public const string Authentication = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication";

		// Token: 0x04000FCA RID: 4042
		public const string AuthorizationDecision = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authorizationdecision";

		// Token: 0x04000FCB RID: 4043
		public const string Country = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/country";

		// Token: 0x04000FCC RID: 4044
		public const string DateOfBirth = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/dateofbirth";

		// Token: 0x04000FCD RID: 4045
		public const string Dns = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/dns";

		// Token: 0x04000FCE RID: 4046
		public const string DenyOnlySid = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/denyonlysid";

		// Token: 0x04000FCF RID: 4047
		public const string Email = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";

		// Token: 0x04000FD0 RID: 4048
		public const string Gender = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/gender";

		// Token: 0x04000FD1 RID: 4049
		public const string GivenName = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname";

		// Token: 0x04000FD2 RID: 4050
		public const string Hash = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/hash";

		// Token: 0x04000FD3 RID: 4051
		public const string HomePhone = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/homephone";

		// Token: 0x04000FD4 RID: 4052
		public const string Locality = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/locality";

		// Token: 0x04000FD5 RID: 4053
		public const string MobilePhone = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/mobilephone";

		// Token: 0x04000FD6 RID: 4054
		public const string Name = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";

		// Token: 0x04000FD7 RID: 4055
		public const string NameIdentifier = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";

		// Token: 0x04000FD8 RID: 4056
		public const string OtherPhone = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/otherphone";

		// Token: 0x04000FD9 RID: 4057
		public const string PostalCode = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/postalcode";

		// Token: 0x04000FDA RID: 4058
		public const string Rsa = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/rsa";

		// Token: 0x04000FDB RID: 4059
		public const string Sid = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sid";

		// Token: 0x04000FDC RID: 4060
		public const string Spn = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/spn";

		// Token: 0x04000FDD RID: 4061
		public const string StateOrProvince = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/stateorprovince";

		// Token: 0x04000FDE RID: 4062
		public const string StreetAddress = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/streetaddress";

		// Token: 0x04000FDF RID: 4063
		public const string Surname = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname";

		// Token: 0x04000FE0 RID: 4064
		public const string System = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/system";

		// Token: 0x04000FE1 RID: 4065
		public const string Thumbprint = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/thumbprint";

		// Token: 0x04000FE2 RID: 4066
		public const string Upn = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn";

		// Token: 0x04000FE3 RID: 4067
		public const string Uri = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/uri";

		// Token: 0x04000FE4 RID: 4068
		public const string Webpage = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/webpage";

		// Token: 0x04000FE5 RID: 4069
		public const string X500DistinguishedName = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/x500distinguishedname";

		// Token: 0x04000FE6 RID: 4070
		internal const string ClaimType2009Namespace = "http://schemas.xmlsoap.org/ws/2009/09/identity/claims";

		// Token: 0x04000FE7 RID: 4071
		public const string Actor = "http://schemas.xmlsoap.org/ws/2009/09/identity/claims/actor";
	}
}
