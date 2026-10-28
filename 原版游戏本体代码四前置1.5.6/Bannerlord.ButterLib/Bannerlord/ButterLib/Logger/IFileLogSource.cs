using Serilog.Sinks.File;

namespace Bannerlord.ButterLib.Logger;

internal interface IFileLogSource : ILogSource
{
	string Path { get; }

	IFlushableFileSink[] Sinks { get; }

	void Deconstruct(out string name, out string path, out IFlushableFileSink[] sinks);
}
