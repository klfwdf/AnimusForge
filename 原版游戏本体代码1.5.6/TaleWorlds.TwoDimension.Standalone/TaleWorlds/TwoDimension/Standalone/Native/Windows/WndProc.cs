using System;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
