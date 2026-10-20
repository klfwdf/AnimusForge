using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.DiplomacyHotfix;

public sealed class SubModule : MBSubModuleBase
{
    private bool _attempted;

    protected override void OnSubModuleLoad()
    {
        base.OnSubModuleLoad();
        var af = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "AnimusForge");
        if (af != null) Apply(af);
        else AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoaded;
    }

    private void OnAssemblyLoaded(object sender, AssemblyLoadEventArgs e)
    {
        if (e.LoadedAssembly.GetName().Name != "AnimusForge") return;
        AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoaded;
        Apply(e.LoadedAssembly);
    }

    private void Apply(Assembly assembly)
    {
        if (_attempted) return;
        _attempted = true;
        try { HotfixInstaller.Install(assembly, WriteLog); }
        catch (Exception ex) { WriteLog("startup_rejected:" + ex.GetType().Name + ":" + ex.Message); }
    }

    private static void WriteLog(string message)
    {
        try
        {
            string bin = Path.GetDirectoryName(typeof(SubModule).Assembly.Location);
            string module = Path.GetFullPath(Path.Combine(bin, "..", ".."));
            string logs = Path.Combine(module, "Logs");
            Directory.CreateDirectory(logs);
            File.AppendAllText(Path.Combine(logs, "DiplomacyHotfix.log"), DateTime.UtcNow.ToString("o") + " " + message + Environment.NewLine);
        }
        catch (IOException) { System.Diagnostics.Trace.WriteLine(message); }
        catch (UnauthorizedAccessException) { System.Diagnostics.Trace.WriteLine(message); }
    }
}
