namespace BUTR.MessageBoxPInvoke.Helpers;

internal enum MessageBoxModal : uint
{
	Application = 0u,
	System = 0x1000u,
	Task = 0x2000u
}
