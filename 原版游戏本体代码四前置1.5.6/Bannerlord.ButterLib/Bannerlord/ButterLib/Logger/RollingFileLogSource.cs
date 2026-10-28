using System;
using System.IO;
using Serilog.Sinks.File;

namespace Bannerlord.ButterLib.Logger;

internal record RollingFileLogSource : IFileLogSource, ILogSource
{
	public string Name { get; init; }

	public string Path { get; init; }

	public IFlushableFileSink[] Sinks { get; init; }

	public RollingFileLogSource(string path, IFlushableFileSink[] sinks)
	{
		string text = DateTimeOffset.Now.ToString("yyyyMMdd");
		string directoryName = System.IO.Path.GetDirectoryName(path);
		string fileName = System.IO.Path.GetFileName(path);
		string text2 = (fileName.Contains("{Date}") ? fileName.Replace("{Date}", text) : (System.IO.Path.GetFileNameWithoutExtension(fileName) + text + System.IO.Path.GetExtension(fileName)));
		Name = System.IO.Path.GetFileNameWithoutExtension(text2);
		Path = System.IO.Path.Combine(directoryName, text2);
		Sinks = sinks;
	}

	public void Deconstruct(out string name, out string path, out IFlushableFileSink[] sinks)
	{
		name = Name;
		path = Path;
		sinks = Sinks;
	}
}
