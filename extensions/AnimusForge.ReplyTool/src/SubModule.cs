using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.ReplyTool;

public sealed class SubModule : MBSubModuleBase
{
    private int _attempted;
    protected override void OnSubModuleLoad()
    {
        base.OnSubModuleLoad();
        AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoaded;
        Assembly af = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "AnimusForge");
        if (af != null) Apply(af);
    }
    private void OnAssemblyLoaded(object sender, AssemblyLoadEventArgs args)
    {
        if (args.LoadedAssembly.GetName().Name == "AnimusForge") Apply(args.LoadedAssembly);
    }
    private void Apply(Assembly af)
    {
        if (Interlocked.Exchange(ref _attempted, 1) != 0) return;
        AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoaded;
        try
        {
            string prompt = File.ReadAllText(Path.Combine(ModuleRoot, "ModuleData", "ReplyToolPrompt.json"));
            ReplyToolInstaller.Install(af, prompt, WriteLog);
        }
        catch (Exception error) { WriteLog("startup_rejected:" + error.GetType().Name); }
    }
    protected override void OnSubModuleUnloaded()
    {
        AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoaded;
        ReplyToolInstaller.Uninstall();
        base.OnSubModuleUnloaded();
    }
    private static string ModuleRoot => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(SubModule).Assembly.Location), "..", ".."));
    private static void WriteLog(string value)
    {
        try
        {
            string directory = Path.Combine(ModuleRoot, "Logs");
            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, "ReplyTool.log"), DateTime.UtcNow.ToString("o") + " " + value + Environment.NewLine);
        }
        catch (IOException) { System.Diagnostics.Trace.WriteLine(value); }
        catch (UnauthorizedAccessException) { System.Diagnostics.Trace.WriteLine(value); }
    }
}
