using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TaleWorlds.Library;

public static class Common
{
	private static IPlatformFileHelper _fileHelper = null;

	private static DateTime lastGCTime = DateTime.Now;

	private static ParallelOptions _parallelOptions = null;

	public static IPlatformFileHelper PlatformFileHelper
	{
		get
		{
			return _fileHelper;
		}
		set
		{
			_fileHelper = value;
		}
	}

	public static string ConfigName => new DirectoryInfo(Directory.GetCurrentDirectory()).Name;

	public static ParallelOptions ParallelOptions
	{
		get
		{
			if (_parallelOptions == null)
			{
				_parallelOptions = new ParallelOptions();
				_parallelOptions.MaxDegreeOfParallelism = MathF.Max(Environment.ProcessorCount - 2, 1);
				Debug.Print($"Max Dexree of Parallelism is set to: {_parallelOptions.MaxDegreeOfParallelism}");
			}
			return _parallelOptions;
		}
	}

	public static byte[] CombineBytes(byte[] arr1, byte[] arr2, byte[] arr3 = null, byte[] arr4 = null, byte[] arr5 = null)
	{
		byte[] array = new byte[arr1.Length + arr2.Length + ((arr3 != null) ? arr3.Length : 0) + ((arr4 != null) ? arr4.Length : 0) + ((arr5 != null) ? arr5.Length : 0)];
		int num = 0;
		if (arr1.Length != 0)
		{
			Buffer.BlockCopy(arr1, 0, array, num, arr1.Length);
			num += arr1.Length;
		}
		if (arr2.Length != 0)
		{
			Buffer.BlockCopy(arr2, 0, array, num, arr2.Length);
			num += arr2.Length;
		}
		if (arr3 != null && arr3.Length != 0)
		{
			Buffer.BlockCopy(arr3, 0, array, num, arr3.Length);
			num += arr3.Length;
		}
		if (arr4 != null && arr4.Length != 0)
		{
			Buffer.BlockCopy(arr4, 0, array, num, arr4.Length);
			num += arr4.Length;
		}
		if (arr5 != null && arr5.Length != 0)
		{
			Buffer.BlockCopy(arr5, 0, array, num, arr5.Length);
		}
		return array;
	}

	public static string CreateNanoIdFrom(string input)
	{
		byte[] array = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(input));
		StringBuilder stringBuilder = new StringBuilder(8);
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		while (stringBuilder.Length < 8 && num3 < array.Length)
		{
			num2 = (num2 << 8) | array[num3++];
			num += 8;
			while (num >= 6)
			{
				num -= 6;
				int num4 = (num2 >> num) & 0x3F;
				stringBuilder.Append("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"[num4 % "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".Length]);
			}
		}
		return stringBuilder.ToString();
	}

	public static string CalculateMD5Hash(string input)
	{
		MD5 mD = MD5.Create();
		byte[] bytes = Encoding.ASCII.GetBytes(input);
		byte[] array = mD.ComputeHash(bytes);
		mD.Dispose();
		MBStringBuilder mBStringBuilder = default(MBStringBuilder);
		mBStringBuilder.Initialize(32, "CalculateMD5Hash");
		for (int i = 0; i < array.Length; i++)
		{
			mBStringBuilder.Append(array[i].ToString("X2"));
		}
		return mBStringBuilder.ToStringAndRelease();
	}

	public static string ToRoman(int number)
	{
		if (number < 0 || number > 3999)
		{
			Debug.FailedAssert("Requested roman number has to be between 1 and 3999. Fix number!", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\Common.cs", "ToRoman", 116);
		}
		if (number < 1)
		{
			return string.Empty;
		}
		if (number >= 1000)
		{
			return "M" + ToRoman(number - 1000);
		}
		if (number >= 900)
		{
			return "CM" + ToRoman(number - 900);
		}
		if (number >= 500)
		{
			return "D" + ToRoman(number - 500);
		}
		if (number >= 400)
		{
			return "CD" + ToRoman(number - 400);
		}
		if (number >= 100)
		{
			return "C" + ToRoman(number - 100);
		}
		if (number >= 90)
		{
			return "XC" + ToRoman(number - 90);
		}
		if (number >= 50)
		{
			return "L" + ToRoman(number - 50);
		}
		if (number >= 40)
		{
			return "XL" + ToRoman(number - 40);
		}
		if (number >= 10)
		{
			return "X" + ToRoman(number - 10);
		}
		if (number >= 9)
		{
			return "IX" + ToRoman(number - 9);
		}
		if (number >= 5)
		{
			return "V" + ToRoman(number - 5);
		}
		if (number >= 4)
		{
			return "IV" + ToRoman(number - 4);
		}
		if (number >= 1)
		{
			return "I" + ToRoman(number - 1);
		}
		Debug.FailedAssert("ToRoman error", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\Common.cs", "ToRoman", 132);
		return "";
	}

	public static int GetDJB2(string str)
	{
		int num = 5381;
		for (int i = 0; i < str.Length; i++)
		{
			num = (num << 5) + num + str[i];
		}
		return num;
	}

	public static byte[] SerializeObjectAsJson(object o)
	{
		string s = JsonConvert.SerializeObject(o, Formatting.Indented);
		return Encoding.UTF8.GetBytes(s);
	}

	public static string SerializeObjectAsJsonString(object o)
	{
		return JsonConvert.SerializeObject(o, Formatting.Indented);
	}

	public static T DeserializeObjectFromJson<T>(string json)
	{
		return JsonConvert.DeserializeObject<T>(json);
	}

	public static byte[] FromUrlSafeBase64(string base64)
	{
		string text = base64.Replace('_', '/').Replace('-', '+');
		switch (base64.Length % 4)
		{
		case 2:
			text += "==";
			break;
		case 3:
			text += "=";
			break;
		}
		return Convert.FromBase64String(text);
	}

	public static Type FindType(string typeName)
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		Type type = null;
		for (int i = 0; i < assemblies.Length; i++)
		{
			type = assemblies[i].GetType(typeName);
			if (type != null)
			{
				return type;
			}
		}
		return null;
	}

	public static void MemoryCleanupGC(bool forceTimer = false)
	{
		GC.Collect();
		lastGCTime = DateTime.Now;
	}

	public static object DynamicInvokeWithLog(this Delegate method, params object[] args)
	{
		object obj = null;
		try
		{
			return method.DynamicInvoke(args);
		}
		catch (Exception e)
		{
			PrintDynamicInvokeDebugInfo(e, method.Method, method.Target, args);
			obj = null;
			throw;
		}
	}

	public static object InvokeWithLog(this MethodInfo methodInfo, object obj, params object[] args)
	{
		object obj2 = null;
		try
		{
			return methodInfo.Invoke(obj, args);
		}
		catch (Exception e)
		{
			PrintDynamicInvokeDebugInfo(e, methodInfo, obj, args);
			obj2 = null;
			throw;
		}
	}

	public static object InvokeWithLog(this ConstructorInfo constructorInfo, params object[] args)
	{
		object obj = null;
		try
		{
			return constructorInfo.Invoke(args);
		}
		catch (Exception e)
		{
			MethodInfo methodInfo = GetMethodInfo((Expression<Action<object[]>>)((object[] a) => constructorInfo.Invoke(a)));
			PrintDynamicInvokeDebugInfo(e, methodInfo, null, args);
			obj = null;
			throw;
		}
	}

	private static string GetStackTraceRaw(Exception e, int skipCount = 0)
	{
		StackTrace stackTrace = new StackTrace(e, 0, fNeedFileInfo: false);
		MBStringBuilder mBStringBuilder = default(MBStringBuilder);
		mBStringBuilder.Initialize(16, "GetStackTraceRaw");
		for (int i = 0; i < stackTrace.FrameCount; i++)
		{
			if (i >= skipCount)
			{
				string text = "unknown_module.dll";
				try
				{
					StackFrame? frame = stackTrace.GetFrame(i);
					MethodBase method = frame.GetMethod();
					text = method.Module.Assembly.Location;
					int iLOffset = frame.GetILOffset();
					int metadataToken = method.MetadataToken;
					mBStringBuilder.AppendLine(text + "@" + metadataToken + "@" + iLOffset);
				}
				catch
				{
					mBStringBuilder.AppendLine(text + "@-1@-1");
				}
			}
		}
		return mBStringBuilder.ToStringAndRelease();
	}

	private static void WalkInnerExceptionRecursive(Exception InnerException, ref string StackStr)
	{
		if (InnerException != null)
		{
			WalkInnerExceptionRecursive(InnerException.InnerException, ref StackStr);
			string stackTraceRaw = GetStackTraceRaw(InnerException);
			StackStr += stackTraceRaw;
			StackStr += "---End of stack trace from previous location where exception was thrown ---";
			StackStr += Environment.NewLine;
		}
	}

	private static void PrintDynamicInvokeDebugInfo(Exception e, MethodInfo methodInfo, object obj, params object[] args)
	{
		string text = "Exception occurred inside invoke: " + methodInfo.Name;
		if (obj != null)
		{
			text = text + "\nTarget type: " + obj.GetType().FullName;
		}
		if (args != null)
		{
			text = text + "\nArgument count: " + args.Length;
			foreach (object obj2 in args)
			{
				text = ((obj2 != null) ? (text + "\nArgument type is " + obj2.GetType().FullName) : (text + "\nArgument is null"));
			}
		}
		string StackStr = "";
		if (e.InnerException != null)
		{
			WalkInnerExceptionRecursive(e, ref StackStr);
		}
		Exception ex = e;
		while (ex.InnerException != null)
		{
			ex = ex.InnerException;
		}
		text = text + "\nInner message: " + ex.Message;
		Debug.SetCrashReportCustomString(text);
		Debug.SetCrashReportCustomStack(StackStr);
		Debug.Print(text);
	}

	public static bool TextContainsSpecialCharacters(string text)
	{
		return text.Any((char x) => !char.IsWhiteSpace(x) && !char.IsLetterOrDigit(x) && !char.IsPunctuation(x));
	}

	public static uint ParseIpAddress(string address)
	{
		byte[] addressBytes = IPAddress.Parse(address).GetAddressBytes();
		return (uint)((addressBytes[0] << 24) + (addressBytes[1] << 16) + (addressBytes[2] << 8) + addressBytes[3]);
	}

	public static bool IsAllLetters(string text)
	{
		if (text == null)
		{
			return false;
		}
		for (int i = 0; i < text.Length; i++)
		{
			if (!char.IsLetter(text[i]))
			{
				return false;
			}
		}
		return true;
	}

	public static bool IsAllLettersOrWhiteSpaces(string text)
	{
		if (text == null)
		{
			return false;
		}
		foreach (char c in text)
		{
			if (!char.IsLetter(c) && !char.IsWhiteSpace(c))
			{
				return false;
			}
		}
		return true;
	}

	public static bool IsCharAsian(char character)
	{
		if ((character < '一' || character > '鿿') && (character < '㐀' || character > '䶿') && (character < '㐀' || character > '䶿') && (character < 131072 || character > 183983) && (character < '⺀' || character > '\u31ef') && (character < '豈' || character > '\ufaff') && (character < '︰' || character > '\ufe4f'))
		{
			if (character >= 993280)
			{
				return character <= 195103;
			}
			return false;
		}
		return true;
	}

	public static void SetInvariantCulture()
	{
		Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
		Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
	}

	public static MethodInfo GetMethodInfo(Expression<Action> expression)
	{
		return GetMethodInfo((LambdaExpression)expression);
	}

	public static MethodInfo GetMethodInfo<T>(Expression<Action<T>> expression)
	{
		return GetMethodInfo((LambdaExpression)expression);
	}

	public static MethodInfo GetMethodInfo<T, TResult>(Expression<Func<T, TResult>> expression)
	{
		return GetMethodInfo((LambdaExpression)expression);
	}

	public static MethodInfo GetMethodInfo(LambdaExpression expression)
	{
		return ((expression.Body as MethodCallExpression) ?? throw new ArgumentException("Invalid Expression. Expression should consist of a Method call only.")).Method;
	}
}
