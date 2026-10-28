using System;
using System.Security.Cryptography;
using System.Text;
using TaleWorlds.Library;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.Diamond.AccessProvider.Test;

public class TestLoginAccessProvider : ILoginAccessProvider
{
	private string _userName;

	void ILoginAccessProvider.Initialize(string preferredUserName, PlatformInitParams initParams)
	{
		_userName = preferredUserName;
	}

	string ILoginAccessProvider.GetUserName()
	{
		return _userName;
	}

	PlayerId ILoginAccessProvider.GetPlayerId()
	{
		return GetPlayerIdFromUserName(_userName);
	}

	AccessObjectResult ILoginAccessProvider.CreateAccessObject()
	{
		return AccessObjectResult.CreateSuccess(new TestAccessObject(_userName, Environment.GetEnvironmentVariable("TestLoginAccessProvider.Password")));
	}

	public static ulong GetInt64HashCode(string strText)
	{
		ulong result = 0uL;
		if (!string.IsNullOrEmpty(strText))
		{
			byte[] bytes = Encoding.Unicode.GetBytes(strText);
			SHA256CryptoServiceProvider sHA256CryptoServiceProvider = new SHA256CryptoServiceProvider();
			byte[] value = sHA256CryptoServiceProvider.ComputeHash(bytes);
			ulong num = BitConverter.ToUInt64(value, 0);
			ulong num2 = BitConverter.ToUInt64(value, 8);
			ulong num3 = BitConverter.ToUInt64(value, 16);
			ulong num4 = BitConverter.ToUInt64(value, 24);
			result = num ^ num2 ^ num3 ^ num4;
			sHA256CryptoServiceProvider.Dispose();
		}
		return result;
	}

	public static PlayerId GetPlayerIdFromUserName(string userName)
	{
		return new PlayerId(1, 0uL, GetInt64HashCode(userName));
	}
}
