using System;

namespace Steamworks
{
	// Token: 0x02000143 RID: 323
	public enum EResult
	{
		// Token: 0x04000760 RID: 1888
		k_EResultNone,
		// Token: 0x04000761 RID: 1889
		k_EResultOK,
		// Token: 0x04000762 RID: 1890
		k_EResultFail,
		// Token: 0x04000763 RID: 1891
		k_EResultNoConnection,
		// Token: 0x04000764 RID: 1892
		k_EResultInvalidPassword = 5,
		// Token: 0x04000765 RID: 1893
		k_EResultLoggedInElsewhere,
		// Token: 0x04000766 RID: 1894
		k_EResultInvalidProtocolVer,
		// Token: 0x04000767 RID: 1895
		k_EResultInvalidParam,
		// Token: 0x04000768 RID: 1896
		k_EResultFileNotFound,
		// Token: 0x04000769 RID: 1897
		k_EResultBusy,
		// Token: 0x0400076A RID: 1898
		k_EResultInvalidState,
		// Token: 0x0400076B RID: 1899
		k_EResultInvalidName,
		// Token: 0x0400076C RID: 1900
		k_EResultInvalidEmail,
		// Token: 0x0400076D RID: 1901
		k_EResultDuplicateName,
		// Token: 0x0400076E RID: 1902
		k_EResultAccessDenied,
		// Token: 0x0400076F RID: 1903
		k_EResultTimeout,
		// Token: 0x04000770 RID: 1904
		k_EResultBanned,
		// Token: 0x04000771 RID: 1905
		k_EResultAccountNotFound,
		// Token: 0x04000772 RID: 1906
		k_EResultInvalidSteamID,
		// Token: 0x04000773 RID: 1907
		k_EResultServiceUnavailable,
		// Token: 0x04000774 RID: 1908
		k_EResultNotLoggedOn,
		// Token: 0x04000775 RID: 1909
		k_EResultPending,
		// Token: 0x04000776 RID: 1910
		k_EResultEncryptionFailure,
		// Token: 0x04000777 RID: 1911
		k_EResultInsufficientPrivilege,
		// Token: 0x04000778 RID: 1912
		k_EResultLimitExceeded,
		// Token: 0x04000779 RID: 1913
		k_EResultRevoked,
		// Token: 0x0400077A RID: 1914
		k_EResultExpired,
		// Token: 0x0400077B RID: 1915
		k_EResultAlreadyRedeemed,
		// Token: 0x0400077C RID: 1916
		k_EResultDuplicateRequest,
		// Token: 0x0400077D RID: 1917
		k_EResultAlreadyOwned,
		// Token: 0x0400077E RID: 1918
		k_EResultIPNotFound,
		// Token: 0x0400077F RID: 1919
		k_EResultPersistFailed,
		// Token: 0x04000780 RID: 1920
		k_EResultLockingFailed,
		// Token: 0x04000781 RID: 1921
		k_EResultLogonSessionReplaced,
		// Token: 0x04000782 RID: 1922
		k_EResultConnectFailed,
		// Token: 0x04000783 RID: 1923
		k_EResultHandshakeFailed,
		// Token: 0x04000784 RID: 1924
		k_EResultIOFailure,
		// Token: 0x04000785 RID: 1925
		k_EResultRemoteDisconnect,
		// Token: 0x04000786 RID: 1926
		k_EResultShoppingCartNotFound,
		// Token: 0x04000787 RID: 1927
		k_EResultBlocked,
		// Token: 0x04000788 RID: 1928
		k_EResultIgnored,
		// Token: 0x04000789 RID: 1929
		k_EResultNoMatch,
		// Token: 0x0400078A RID: 1930
		k_EResultAccountDisabled,
		// Token: 0x0400078B RID: 1931
		k_EResultServiceReadOnly,
		// Token: 0x0400078C RID: 1932
		k_EResultAccountNotFeatured,
		// Token: 0x0400078D RID: 1933
		k_EResultAdministratorOK,
		// Token: 0x0400078E RID: 1934
		k_EResultContentVersion,
		// Token: 0x0400078F RID: 1935
		k_EResultTryAnotherCM,
		// Token: 0x04000790 RID: 1936
		k_EResultPasswordRequiredToKickSession,
		// Token: 0x04000791 RID: 1937
		k_EResultAlreadyLoggedInElsewhere,
		// Token: 0x04000792 RID: 1938
		k_EResultSuspended,
		// Token: 0x04000793 RID: 1939
		k_EResultCancelled,
		// Token: 0x04000794 RID: 1940
		k_EResultDataCorruption,
		// Token: 0x04000795 RID: 1941
		k_EResultDiskFull,
		// Token: 0x04000796 RID: 1942
		k_EResultRemoteCallFailed,
		// Token: 0x04000797 RID: 1943
		k_EResultPasswordUnset,
		// Token: 0x04000798 RID: 1944
		k_EResultExternalAccountUnlinked,
		// Token: 0x04000799 RID: 1945
		k_EResultPSNTicketInvalid,
		// Token: 0x0400079A RID: 1946
		k_EResultExternalAccountAlreadyLinked,
		// Token: 0x0400079B RID: 1947
		k_EResultRemoteFileConflict,
		// Token: 0x0400079C RID: 1948
		k_EResultIllegalPassword,
		// Token: 0x0400079D RID: 1949
		k_EResultSameAsPreviousValue,
		// Token: 0x0400079E RID: 1950
		k_EResultAccountLogonDenied,
		// Token: 0x0400079F RID: 1951
		k_EResultCannotUseOldPassword,
		// Token: 0x040007A0 RID: 1952
		k_EResultInvalidLoginAuthCode,
		// Token: 0x040007A1 RID: 1953
		k_EResultAccountLogonDeniedNoMail,
		// Token: 0x040007A2 RID: 1954
		k_EResultHardwareNotCapableOfIPT,
		// Token: 0x040007A3 RID: 1955
		k_EResultIPTInitError,
		// Token: 0x040007A4 RID: 1956
		k_EResultParentalControlRestricted,
		// Token: 0x040007A5 RID: 1957
		k_EResultFacebookQueryError,
		// Token: 0x040007A6 RID: 1958
		k_EResultExpiredLoginAuthCode,
		// Token: 0x040007A7 RID: 1959
		k_EResultIPLoginRestrictionFailed,
		// Token: 0x040007A8 RID: 1960
		k_EResultAccountLockedDown,
		// Token: 0x040007A9 RID: 1961
		k_EResultAccountLogonDeniedVerifiedEmailRequired,
		// Token: 0x040007AA RID: 1962
		k_EResultNoMatchingURL,
		// Token: 0x040007AB RID: 1963
		k_EResultBadResponse,
		// Token: 0x040007AC RID: 1964
		k_EResultRequirePasswordReEntry,
		// Token: 0x040007AD RID: 1965
		k_EResultValueOutOfRange,
		// Token: 0x040007AE RID: 1966
		k_EResultUnexpectedError,
		// Token: 0x040007AF RID: 1967
		k_EResultDisabled,
		// Token: 0x040007B0 RID: 1968
		k_EResultInvalidCEGSubmission,
		// Token: 0x040007B1 RID: 1969
		k_EResultRestrictedDevice,
		// Token: 0x040007B2 RID: 1970
		k_EResultRegionLocked,
		// Token: 0x040007B3 RID: 1971
		k_EResultRateLimitExceeded,
		// Token: 0x040007B4 RID: 1972
		k_EResultAccountLoginDeniedNeedTwoFactor,
		// Token: 0x040007B5 RID: 1973
		k_EResultItemDeleted,
		// Token: 0x040007B6 RID: 1974
		k_EResultAccountLoginDeniedThrottle,
		// Token: 0x040007B7 RID: 1975
		k_EResultTwoFactorCodeMismatch,
		// Token: 0x040007B8 RID: 1976
		k_EResultTwoFactorActivationCodeMismatch,
		// Token: 0x040007B9 RID: 1977
		k_EResultAccountAssociatedToMultiplePartners,
		// Token: 0x040007BA RID: 1978
		k_EResultNotModified,
		// Token: 0x040007BB RID: 1979
		k_EResultNoMobileDevice,
		// Token: 0x040007BC RID: 1980
		k_EResultTimeNotSynced,
		// Token: 0x040007BD RID: 1981
		k_EResultSmsCodeFailed,
		// Token: 0x040007BE RID: 1982
		k_EResultAccountLimitExceeded,
		// Token: 0x040007BF RID: 1983
		k_EResultAccountActivityLimitExceeded,
		// Token: 0x040007C0 RID: 1984
		k_EResultPhoneActivityLimitExceeded,
		// Token: 0x040007C1 RID: 1985
		k_EResultRefundToWallet,
		// Token: 0x040007C2 RID: 1986
		k_EResultEmailSendFailure,
		// Token: 0x040007C3 RID: 1987
		k_EResultNotSettled,
		// Token: 0x040007C4 RID: 1988
		k_EResultNeedCaptcha,
		// Token: 0x040007C5 RID: 1989
		k_EResultGSLTDenied,
		// Token: 0x040007C6 RID: 1990
		k_EResultGSOwnerDenied,
		// Token: 0x040007C7 RID: 1991
		k_EResultInvalidItemType,
		// Token: 0x040007C8 RID: 1992
		k_EResultIPBanned,
		// Token: 0x040007C9 RID: 1993
		k_EResultGSLTExpired,
		// Token: 0x040007CA RID: 1994
		k_EResultInsufficientFunds,
		// Token: 0x040007CB RID: 1995
		k_EResultTooManyPending,
		// Token: 0x040007CC RID: 1996
		k_EResultNoSiteLicensesFound,
		// Token: 0x040007CD RID: 1997
		k_EResultWGNetworkSendExceeded,
		// Token: 0x040007CE RID: 1998
		k_EResultAccountNotFriends,
		// Token: 0x040007CF RID: 1999
		k_EResultLimitedUserAccount,
		// Token: 0x040007D0 RID: 2000
		k_EResultCantRemoveItem,
		// Token: 0x040007D1 RID: 2001
		k_EResultAccountDeleted,
		// Token: 0x040007D2 RID: 2002
		k_EResultExistingUserCancelledLicense,
		// Token: 0x040007D3 RID: 2003
		k_EResultCommunityCooldown,
		// Token: 0x040007D4 RID: 2004
		k_EResultNoLauncherSpecified,
		// Token: 0x040007D5 RID: 2005
		k_EResultMustAgreeToSSA,
		// Token: 0x040007D6 RID: 2006
		k_EResultLauncherMigrated,
		// Token: 0x040007D7 RID: 2007
		k_EResultSteamRealmMismatch,
		// Token: 0x040007D8 RID: 2008
		k_EResultInvalidSignature,
		// Token: 0x040007D9 RID: 2009
		k_EResultParseFailure,
		// Token: 0x040007DA RID: 2010
		k_EResultNoVerifiedPhone
	}
}
