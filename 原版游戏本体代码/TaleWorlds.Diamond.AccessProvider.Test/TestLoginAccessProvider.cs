using System;
using System.Security.Cryptography;
using System.Text;
using TaleWorlds.Library;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.Diamond.AccessProvider.Test
{
	// Token: 0x02000002 RID: 2
	public class TestLoginAccessProvider : ILoginAccessProvider
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002048 File Offset: 0x00000248
		void ILoginAccessProvider.Initialize(string preferredUserName, PlatformInitParams initParams)
		{
			this._userName = preferredUserName;
		}

		// Token: 0x06000002 RID: 2 RVA: 0x00002051 File Offset: 0x00000251
		string ILoginAccessProvider.GetUserName()
		{
			return this._userName;
		}

		// Token: 0x06000003 RID: 3 RVA: 0x00002059 File Offset: 0x00000259
		PlayerId ILoginAccessProvider.GetPlayerId()
		{
			return TestLoginAccessProvider.GetPlayerIdFromUserName(this._userName);
		}

		// Token: 0x06000004 RID: 4 RVA: 0x00002066 File Offset: 0x00000266
		AccessObjectResult ILoginAccessProvider.CreateAccessObject()
		{
			return AccessObjectResult.CreateSuccess(new TestAccessObject(this._userName, Environment.GetEnvironmentVariable("TestLoginAccessProvider.Password")));
		}

		// Token: 0x06000005 RID: 5 RVA: 0x00002084 File Offset: 0x00000284
		public static ulong GetInt64HashCode(string strText)
		{
			ulong result = 0UL;
			if (!string.IsNullOrEmpty(strText))
			{
				byte[] bytes = Encoding.Unicode.GetBytes(strText);
				SHA256CryptoServiceProvider sha256CryptoServiceProvider = new SHA256CryptoServiceProvider();
				byte[] value = sha256CryptoServiceProvider.ComputeHash(bytes);
				ulong num = BitConverter.ToUInt64(value, 0);
				ulong num2 = BitConverter.ToUInt64(value, 8);
				ulong num3 = BitConverter.ToUInt64(value, 16);
				ulong num4 = BitConverter.ToUInt64(value, 24);
				result = num ^ num2 ^ num3 ^ num4;
				sha256CryptoServiceProvider.Dispose();
			}
			return result;
		}

		// Token: 0x06000006 RID: 6 RVA: 0x000020E7 File Offset: 0x000002E7
		public static PlayerId GetPlayerIdFromUserName(string userName)
		{
			return new PlayerId(1, 0UL, TestLoginAccessProvider.GetInt64HashCode(userName));
		}

		// Token: 0x04000001 RID: 1
		private string _userName;
	}
}
