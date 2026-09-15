using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

// C# callback can run on audit continuations without a PowerShell runspace.
public sealed class OfflineAssemblyResolver : IDisposable
{
    private readonly string[] directories;
    [ThreadStatic] private static HashSet<string> resolving;
    public OfflineAssemblyResolver(string[] directories)
    {
        this.directories = directories;
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
    }
    private Assembly Resolve(object sender, ResolveEventArgs args)
    {
        if (resolving == null) resolving = new HashSet<string>();
        if (!resolving.Add(args.Name)) return null;
        try
        {
            string name = new AssemblyName(args.Name).Name;
            foreach (string directory in directories)
            {
                string path = Path.Combine(directory, name + ".dll");
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        }
        finally { resolving.Remove(args.Name); }
    }
    public void Dispose() { AppDomain.CurrentDomain.AssemblyResolve -= Resolve; }
}
