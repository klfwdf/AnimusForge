using System;

namespace Steamworks
{
	// Token: 0x02000158 RID: 344
	public enum EHTTPStatusCode
	{
		// Token: 0x040008A0 RID: 2208
		k_EHTTPStatusCodeInvalid,
		// Token: 0x040008A1 RID: 2209
		k_EHTTPStatusCode100Continue = 100,
		// Token: 0x040008A2 RID: 2210
		k_EHTTPStatusCode101SwitchingProtocols,
		// Token: 0x040008A3 RID: 2211
		k_EHTTPStatusCode200OK = 200,
		// Token: 0x040008A4 RID: 2212
		k_EHTTPStatusCode201Created,
		// Token: 0x040008A5 RID: 2213
		k_EHTTPStatusCode202Accepted,
		// Token: 0x040008A6 RID: 2214
		k_EHTTPStatusCode203NonAuthoritative,
		// Token: 0x040008A7 RID: 2215
		k_EHTTPStatusCode204NoContent,
		// Token: 0x040008A8 RID: 2216
		k_EHTTPStatusCode205ResetContent,
		// Token: 0x040008A9 RID: 2217
		k_EHTTPStatusCode206PartialContent,
		// Token: 0x040008AA RID: 2218
		k_EHTTPStatusCode300MultipleChoices = 300,
		// Token: 0x040008AB RID: 2219
		k_EHTTPStatusCode301MovedPermanently,
		// Token: 0x040008AC RID: 2220
		k_EHTTPStatusCode302Found,
		// Token: 0x040008AD RID: 2221
		k_EHTTPStatusCode303SeeOther,
		// Token: 0x040008AE RID: 2222
		k_EHTTPStatusCode304NotModified,
		// Token: 0x040008AF RID: 2223
		k_EHTTPStatusCode305UseProxy,
		// Token: 0x040008B0 RID: 2224
		k_EHTTPStatusCode307TemporaryRedirect = 307,
		// Token: 0x040008B1 RID: 2225
		k_EHTTPStatusCode400BadRequest = 400,
		// Token: 0x040008B2 RID: 2226
		k_EHTTPStatusCode401Unauthorized,
		// Token: 0x040008B3 RID: 2227
		k_EHTTPStatusCode402PaymentRequired,
		// Token: 0x040008B4 RID: 2228
		k_EHTTPStatusCode403Forbidden,
		// Token: 0x040008B5 RID: 2229
		k_EHTTPStatusCode404NotFound,
		// Token: 0x040008B6 RID: 2230
		k_EHTTPStatusCode405MethodNotAllowed,
		// Token: 0x040008B7 RID: 2231
		k_EHTTPStatusCode406NotAcceptable,
		// Token: 0x040008B8 RID: 2232
		k_EHTTPStatusCode407ProxyAuthRequired,
		// Token: 0x040008B9 RID: 2233
		k_EHTTPStatusCode408RequestTimeout,
		// Token: 0x040008BA RID: 2234
		k_EHTTPStatusCode409Conflict,
		// Token: 0x040008BB RID: 2235
		k_EHTTPStatusCode410Gone,
		// Token: 0x040008BC RID: 2236
		k_EHTTPStatusCode411LengthRequired,
		// Token: 0x040008BD RID: 2237
		k_EHTTPStatusCode412PreconditionFailed,
		// Token: 0x040008BE RID: 2238
		k_EHTTPStatusCode413RequestEntityTooLarge,
		// Token: 0x040008BF RID: 2239
		k_EHTTPStatusCode414RequestURITooLong,
		// Token: 0x040008C0 RID: 2240
		k_EHTTPStatusCode415UnsupportedMediaType,
		// Token: 0x040008C1 RID: 2241
		k_EHTTPStatusCode416RequestedRangeNotSatisfiable,
		// Token: 0x040008C2 RID: 2242
		k_EHTTPStatusCode417ExpectationFailed,
		// Token: 0x040008C3 RID: 2243
		k_EHTTPStatusCode4xxUnknown,
		// Token: 0x040008C4 RID: 2244
		k_EHTTPStatusCode429TooManyRequests = 429,
		// Token: 0x040008C5 RID: 2245
		k_EHTTPStatusCode444ConnectionClosed = 444,
		// Token: 0x040008C6 RID: 2246
		k_EHTTPStatusCode500InternalServerError = 500,
		// Token: 0x040008C7 RID: 2247
		k_EHTTPStatusCode501NotImplemented,
		// Token: 0x040008C8 RID: 2248
		k_EHTTPStatusCode502BadGateway,
		// Token: 0x040008C9 RID: 2249
		k_EHTTPStatusCode503ServiceUnavailable,
		// Token: 0x040008CA RID: 2250
		k_EHTTPStatusCode504GatewayTimeout,
		// Token: 0x040008CB RID: 2251
		k_EHTTPStatusCode505HTTPVersionNotSupported,
		// Token: 0x040008CC RID: 2252
		k_EHTTPStatusCode5xxUnknown = 599
	}
}
