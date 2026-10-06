namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public enum WindowMessage : uint
{
	Quit = 18u,
	Close = 16u,
	Size = 5u,
	DisplayChange = 126u,
	DpiChanged = 736u,
	DeviceChange = 537u,
	KeyDown = 256u,
	KeyUp = 257u,
	RightButtonUp = 517u,
	RightButtonDown = 516u,
	LeftButtonUp = 514u,
	LeftButtonDown = 513u,
	MouseMove = 512u,
	MouseWheel = 522u,
	KillFocus = 8u,
	SetFocus = 7u
}
