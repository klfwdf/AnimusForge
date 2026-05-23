using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000015 RID: 21
	[Serializable]
	public sealed class LoginResult : FunctionResult
	{
		// Token: 0x17000016 RID: 22
		// (get) Token: 0x0600006A RID: 106 RVA: 0x000029DC File Offset: 0x00000BDC
		// (set) Token: 0x0600006B RID: 107 RVA: 0x000029E4 File Offset: 0x00000BE4
		[JsonProperty]
		public PeerId PeerId { get; private set; }

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x0600006C RID: 108 RVA: 0x000029ED File Offset: 0x00000BED
		// (set) Token: 0x0600006D RID: 109 RVA: 0x000029F5 File Offset: 0x00000BF5
		[JsonProperty]
		public SessionKey SessionKey { get; private set; }

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x0600006E RID: 110 RVA: 0x000029FE File Offset: 0x00000BFE
		// (set) Token: 0x0600006F RID: 111 RVA: 0x00002A06 File Offset: 0x00000C06
		[JsonProperty]
		public bool Successful { get; private set; }

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x06000070 RID: 112 RVA: 0x00002A0F File Offset: 0x00000C0F
		// (set) Token: 0x06000071 RID: 113 RVA: 0x00002A17 File Offset: 0x00000C17
		[JsonProperty]
		public string ErrorCode { get; private set; }

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x06000072 RID: 114 RVA: 0x00002A20 File Offset: 0x00000C20
		// (set) Token: 0x06000073 RID: 115 RVA: 0x00002A28 File Offset: 0x00000C28
		[JsonProperty]
		public Dictionary<string, string> ErrorParameters { get; private set; }

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x06000074 RID: 116 RVA: 0x00002A31 File Offset: 0x00000C31
		// (set) Token: 0x06000075 RID: 117 RVA: 0x00002A39 File Offset: 0x00000C39
		[JsonProperty]
		public string ProviderResponse { get; private set; }

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x06000076 RID: 118 RVA: 0x00002A42 File Offset: 0x00000C42
		// (set) Token: 0x06000077 RID: 119 RVA: 0x00002A4A File Offset: 0x00000C4A
		[JsonProperty]
		public LoginResultObject LoginResultObject { get; private set; }

		// Token: 0x06000078 RID: 120 RVA: 0x00002A53 File Offset: 0x00000C53
		public LoginResult()
		{
		}

		// Token: 0x06000079 RID: 121 RVA: 0x00002A5B File Offset: 0x00000C5B
		public LoginResult(PeerId peerId, SessionKey sessionKey, LoginResultObject loginResultObject)
		{
			this.PeerId = peerId;
			this.SessionKey = sessionKey;
			this.Successful = true;
			this.ErrorCode = "";
			this.LoginResultObject = loginResultObject;
		}

		// Token: 0x0600007A RID: 122 RVA: 0x00002A8A File Offset: 0x00000C8A
		public LoginResult(PeerId peerId, SessionKey sessionKey)
			: this(peerId, sessionKey, null)
		{
		}

		// Token: 0x0600007B RID: 123 RVA: 0x00002A95 File Offset: 0x00000C95
		public LoginResult(string errorCode, Dictionary<string, string> parameters = null)
		{
			this.ErrorCode = errorCode;
			this.Successful = false;
			this.ErrorParameters = parameters;
		}
	}
}
