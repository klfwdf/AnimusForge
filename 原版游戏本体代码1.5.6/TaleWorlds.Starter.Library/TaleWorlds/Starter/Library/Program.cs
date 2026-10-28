using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;

namespace TaleWorlds.Starter.Library;

public class Program
{
	private delegate void ControllerDelegate(Delegate currentDomainInitializer);

	private delegate void InitializerDelegate(Delegate argument);

	private delegate void StartMethodDelegate(string args);

	private static string[] _args;

	private static void WriteErrorLog(string text)
	{
		string text2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Mount and Blade II Bannerlord");
		if (!Directory.Exists(text2))
		{
			Directory.CreateDirectory(text2);
		}
		text2 = Path.Combine(text2, "logs");
		if (!Directory.Exists(text2))
		{
			Directory.CreateDirectory(text2);
		}
		File.WriteAllText(Path.Combine(text2, "starter_log.txt"), text);
	}

	private static int Starter()
	{
		string text = "";
		try
		{
			Assembly.LoadFrom("TaleWorlds.DotNet.dll").GetType("TaleWorlds.DotNet.Controller").GetMethod("SetEngineMethodsAsDotNet")
				.Invoke(null, new object[3]
				{
					new ControllerDelegate(MBDotNet.PassControllerMethods),
					new InitializerDelegate(MBDotNet.PassManagedInitializeMethodPointerDotNet),
					new InitializerDelegate(MBDotNet.PassManagedEngineCallbackMethodPointersDotNet)
				});
			for (int i = 0; i < _args.Length; i++)
			{
				string text2 = _args[i];
				text += text2;
				if (i + 1 < _args.Length)
				{
					text += " ";
				}
			}
			MBDotNet.SetCurrentDirectory(Directory.GetCurrentDirectory());
		}
		catch (FileNotFoundException ex)
		{
			string text3 = "Exception: " + ex;
			text3 = text3 + "Fusion Log: " + ex.FusionLog + "\n";
			text3 = text3 + "Exception detailed: " + ex.ToString() + "\n";
			if (ex.InnerException != null)
			{
				text3 = string.Concat(text3, "Inner Exception: ", ex.InnerException, "\n");
			}
			WriteErrorLog(text3);
			return 25;
		}
		catch (Exception ex2)
		{
			string text4 = "Exception: " + ex2;
			if (ex2.InnerException != null)
			{
				text4 = text4 + "Inner Exception: " + ex2.InnerException;
			}
			WriteErrorLog(text4);
			return 25;
		}
		return MBDotNet.WotsMainDotNet(text);
	}

	[STAThread]
	public static int Main(string[] args)
	{
		Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
		Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
		_args = args;
		return Starter();
	}
}
