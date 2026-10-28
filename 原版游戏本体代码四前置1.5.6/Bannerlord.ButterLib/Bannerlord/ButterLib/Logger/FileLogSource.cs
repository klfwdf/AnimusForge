using System.IO;
using Serilog.Sinks.File;

namespace Bannerlord.ButterLib.Logger;

internal record FileLogSource : IFileLogSource, ILogSource
{
	public string Name { get; init; }

	public string Path { get; init; }

	public IFlushableFileSink[] Sinks { get; init; }

	public FileLogSource(string path, IFlushableFileSink[] sinks)
	{
		Name = System.IO.Path.GetFileNameWithoutExtension(path);
		Path = path;
		Sinks = sinks;
	}

	public void Deconstruct(out string name, out string path, out IFlushableFileSink[] sinks)
	{
		name = Name;
		path = Path;
		sinks = Sinks;
	}
}
