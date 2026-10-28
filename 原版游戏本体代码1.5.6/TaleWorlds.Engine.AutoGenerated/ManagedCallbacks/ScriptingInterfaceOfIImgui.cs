using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIImgui : IImgui
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void BeginDelegate(byte[] text);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void BeginMainThreadScopeDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void BeginWithCloseButtonDelegate(byte[] text, [MarshalAs(UnmanagedType.U1)] ref bool is_open);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool ButtonDelegate(byte[] text);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool CheckboxDelegate(byte[] text, [MarshalAs(UnmanagedType.U1)] ref bool is_checked);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool CollapsingHeaderDelegate(byte[] label);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ColumnsDelegate(int count, byte[] id, [MarshalAs(UnmanagedType.U1)] bool border);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool ComboDelegate(byte[] label, ref int selectedIndex, byte[] items);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool ComboCustomSeperatorDelegate(byte[] label, ref int selectedIndex, byte[] items, byte[] seperator);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void EndDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void EndMainThreadScopeDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool InputFloatDelegate(byte[] label, ref float val, float step, float stepFast, int decimalPrecision);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool InputFloat2Delegate(byte[] label, ref float val0, ref float val1, int decimalPrecision);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool InputFloat3Delegate(byte[] label, ref float val0, ref float val1, ref float val2, int decimalPrecision);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool InputFloat4Delegate(byte[] label, ref float val0, ref float val1, ref float val2, ref float val3, int decimalPrecision);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool InputIntDelegate(byte[] label, ref int value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int InputTextDelegate(byte[] label, byte[] inputTest, [MarshalAs(UnmanagedType.U1)] ref bool changed);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int InputTextMultilineCopyPasteDelegate(byte[] label, byte[] inputTest, int textBoxHeight, [MarshalAs(UnmanagedType.U1)] ref bool changed);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsItemHoveredDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void NewFrameDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void NewLineDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void NextColumnDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void PlotLinesDelegate(byte[] name, IntPtr values, int valuesCount, int valuesOffset, byte[] overlayText, float minScale, float maxScale, float graphWidth, float graphHeight, int stride);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void PopStyleColorDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ProgressBarDelegate(float value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void PushStyleColorDelegate(int style, ref Vec3 color);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool RadioButtonDelegate(byte[] label, [MarshalAs(UnmanagedType.U1)] bool active);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RenderDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SameLineDelegate(float posX, float spacingWidth);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SeparatorDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetTooltipDelegate(byte[] label);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool SliderFloatDelegate(byte[] label, ref float value, float min, float max);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool SmallButtonDelegate(byte[] label);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void TextDelegate(byte[] text);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool TreeNodeDelegate(byte[] name);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void TreePopDelegate();

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static BeginDelegate call_BeginDelegate;

	public static BeginMainThreadScopeDelegate call_BeginMainThreadScopeDelegate;

	public static BeginWithCloseButtonDelegate call_BeginWithCloseButtonDelegate;

	public static ButtonDelegate call_ButtonDelegate;

	public static CheckboxDelegate call_CheckboxDelegate;

	public static CollapsingHeaderDelegate call_CollapsingHeaderDelegate;

	public static ColumnsDelegate call_ColumnsDelegate;

	public static ComboDelegate call_ComboDelegate;

	public static ComboCustomSeperatorDelegate call_ComboCustomSeperatorDelegate;

	public static EndDelegate call_EndDelegate;

	public static EndMainThreadScopeDelegate call_EndMainThreadScopeDelegate;

	public static InputFloatDelegate call_InputFloatDelegate;

	public static InputFloat2Delegate call_InputFloat2Delegate;

	public static InputFloat3Delegate call_InputFloat3Delegate;

	public static InputFloat4Delegate call_InputFloat4Delegate;

	public static InputIntDelegate call_InputIntDelegate;

	public static InputTextDelegate call_InputTextDelegate;

	public static InputTextMultilineCopyPasteDelegate call_InputTextMultilineCopyPasteDelegate;

	public static IsItemHoveredDelegate call_IsItemHoveredDelegate;

	public static NewFrameDelegate call_NewFrameDelegate;

	public static NewLineDelegate call_NewLineDelegate;

	public static NextColumnDelegate call_NextColumnDelegate;

	public static PlotLinesDelegate call_PlotLinesDelegate;

	public static PopStyleColorDelegate call_PopStyleColorDelegate;

	public static ProgressBarDelegate call_ProgressBarDelegate;

	public static PushStyleColorDelegate call_PushStyleColorDelegate;

	public static RadioButtonDelegate call_RadioButtonDelegate;

	public static RenderDelegate call_RenderDelegate;

	public static SameLineDelegate call_SameLineDelegate;

	public static SeparatorDelegate call_SeparatorDelegate;

	public static SetTooltipDelegate call_SetTooltipDelegate;

	public static SliderFloatDelegate call_SliderFloatDelegate;

	public static SmallButtonDelegate call_SmallButtonDelegate;

	public static TextDelegate call_TextDelegate;

	public static TreeNodeDelegate call_TreeNodeDelegate;

	public static TreePopDelegate call_TreePopDelegate;

	public void Begin(string text)
	{
		byte[] array = null;
		if (text != null)
		{
			int byteCount = _utf8.GetByteCount(text);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(text, 0, text.Length, array, 0);
			array[byteCount] = 0;
		}
		call_BeginDelegate(array);
	}

	public void BeginMainThreadScope()
	{
		call_BeginMainThreadScopeDelegate();
	}

	public void BeginWithCloseButton(string text, ref bool is_open)
	{
		byte[] array = null;
		if (text != null)
		{
			int byteCount = _utf8.GetByteCount(text);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(text, 0, text.Length, array, 0);
			array[byteCount] = 0;
		}
		call_BeginWithCloseButtonDelegate(array, ref is_open);
	}

	public bool Button(string text)
	{
		byte[] array = null;
		if (text != null)
		{
			int byteCount = _utf8.GetByteCount(text);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(text, 0, text.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_ButtonDelegate(array);
	}

	public bool Checkbox(string text, ref bool is_checked)
	{
		byte[] array = null;
		if (text != null)
		{
			int byteCount = _utf8.GetByteCount(text);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(text, 0, text.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_CheckboxDelegate(array, ref is_checked);
	}

	public bool CollapsingHeader(string label)
	{
		byte[] array = null;
		if (label != null)
		{
			int byteCount = _utf8.GetByteCount(label);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(label, 0, label.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_CollapsingHeaderDelegate(array);
	}

	public void Columns(int count, string id, bool border)
	{
		byte[] array = null;
		if (id != null)
		{
			int byteCount = _utf8.GetByteCount(id);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(id, 0, id.Length, array, 0);
			array[byteCount] = 0;
		}
		call_ColumnsDelegate(count, array, border);
	}

	public bool Combo(string label, ref int selectedIndex, string items)
	{
		byte[] array = null;
		if (label != null)
		{
			int byteCount = _utf8.GetByteCount(label);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(label, 0, label.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (items != null)
		{
			int byteCount2 = _utf8.GetByteCount(items);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(items, 0, items.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		return call_ComboDelegate(array, ref selectedIndex, array2);
	}

	public bool ComboCustomSeperator(string label, ref int selectedIndex, string items, string seperator)
	{
		byte[] array = null;
		if (label != null)
		{
			int byteCount = _utf8.GetByteCount(label);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(label, 0, label.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (items != null)
		{
			int byteCount2 = _utf8.GetByteCount(items);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(items, 0, items.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		byte[] array3 = null;
		if (seperator != null)
		{
			int byteCount3 = _utf8.GetByteCount(seperator);
			array3 = ((byteCount3 < 1024) ? CallbackStringBufferManager.StringBuffer2 : new byte[byteCount3 + 1]);
			_utf8.GetBytes(seperator, 0, seperator.Length, array3, 0);
			array3[byteCount3] = 0;
		}
		return call_ComboCustomSeperatorDelegate(array, ref selectedIndex, array2, array3);
	}

	public void End()
	{
		call_EndDelegate();
	}

	public void EndMainThreadScope()
	{
		call_EndMainThreadScopeDelegate();
	}

	public bool InputFloat(string label, ref float val, float step, float stepFast, int decimalPrecision)
	{
		byte[] array = null;
		if (label != null)
		{
			int byteCount = _utf8.GetByteCount(label);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(label, 0, label.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_InputFloatDelegate(array, ref val, step, stepFast, decimalPrecision);
	}

	public bool InputFloat2(string label, ref float val0, ref float val1, int decimalPrecision)
	{
		byte[] array = null;
		if (label != null)
		{
			int byteCount = _utf8.GetByteCount(label);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(label, 0, label.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_InputFloat2Delegate(array, ref val0, ref val1, decimalPrecision);
	}

	public bool InputFloat3(string label, ref float val0, ref float val1, ref float val2, int decimalPrecision)
	{
		byte[] array = null;
		if (label != null)
		{
			int byteCount = _utf8.GetByteCount(label);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(label, 0, label.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_InputFloat3Delegate(array, ref val0, ref val1, ref val2, decimalPrecision);
	}

	public bool InputFloat4(string label, ref float val0, ref float val1, ref float val2, ref float val3, int decimalPrecision)
	{
		byte[] array = null;
		if (label != null)
		{
			int byteCount = _utf8.GetByteCount(label);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(label, 0, label.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_InputFloat4Delegate(array, ref val0, ref val1, ref val2, ref val3, decimalPrecision);
	}

	public bool InputInt(string label, ref int value)
	{
		byte[] array = null;
		if (label != null)
		{
			int byteCount = _utf8.GetByteCount(label);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(label, 0, label.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_InputIntDelegate(array, ref value);
	}

	public string InputText(string label, string inputTest, ref bool changed)
	{
		byte[] array = null;
		if (label != null)
		{
			int byteCount = _utf8.GetByteCount(label);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(label, 0, label.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (inputTest != null)
		{
			int byteCount2 = _utf8.GetByteCount(inputTest);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(inputTest, 0, inputTest.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		if (call_InputTextDelegate(array, array2, ref changed) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public string InputTextMultilineCopyPaste(string label, string inputTest, int textBoxHeight, ref bool changed)
	{
		byte[] array = null;
		if (label != null)
		{
			int byteCount = _utf8.GetByteCount(label);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(label, 0, label.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (inputTest != null)
		{
			int byteCount2 = _utf8.GetByteCount(inputTest);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(inputTest, 0, inputTest.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		if (call_InputTextMultilineCopyPasteDelegate(array, array2, textBoxHeight, ref changed) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public bool IsItemHovered()
	{
		return call_IsItemHoveredDelegate();
	}

	public void NewFrame()
	{
		call_NewFrameDelegate();
	}

	public void NewLine()
	{
		call_NewLineDelegate();
	}

	public void NextColumn()
	{
		call_NextColumnDelegate();
	}

	public void PlotLines(string name, float[] values, int valuesCount, int valuesOffset, string overlayText, float minScale, float maxScale, float graphWidth, float graphHeight, int stride)
	{
		byte[] array = null;
		if (name != null)
		{
			int byteCount = _utf8.GetByteCount(name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(name, 0, name.Length, array, 0);
			array[byteCount] = 0;
		}
		PinnedArrayData<float> pinnedArrayData = new PinnedArrayData<float>(values);
		IntPtr pointer = pinnedArrayData.Pointer;
		byte[] array2 = null;
		if (overlayText != null)
		{
			int byteCount2 = _utf8.GetByteCount(overlayText);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(overlayText, 0, overlayText.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		call_PlotLinesDelegate(array, pointer, valuesCount, valuesOffset, array2, minScale, maxScale, graphWidth, graphHeight, stride);
		pinnedArrayData.Dispose();
	}

	public void PopStyleColor()
	{
		call_PopStyleColorDelegate();
	}

	public void ProgressBar(float value)
	{
		call_ProgressBarDelegate(value);
	}

	public void PushStyleColor(int style, ref Vec3 color)
	{
		call_PushStyleColorDelegate(style, ref color);
	}

	public bool RadioButton(string label, bool active)
	{
		byte[] array = null;
		if (label != null)
		{
			int byteCount = _utf8.GetByteCount(label);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(label, 0, label.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_RadioButtonDelegate(array, active);
	}

	public void Render()
	{
		call_RenderDelegate();
	}

	public void SameLine(float posX, float spacingWidth)
	{
		call_SameLineDelegate(posX, spacingWidth);
	}

	public void Separator()
	{
		call_SeparatorDelegate();
	}

	public void SetTooltip(string label)
	{
		byte[] array = null;
		if (label != null)
		{
			int byteCount = _utf8.GetByteCount(label);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(label, 0, label.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetTooltipDelegate(array);
	}

	public bool SliderFloat(string label, ref float value, float min, float max)
	{
		byte[] array = null;
		if (label != null)
		{
			int byteCount = _utf8.GetByteCount(label);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(label, 0, label.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_SliderFloatDelegate(array, ref value, min, max);
	}

	public bool SmallButton(string label)
	{
		byte[] array = null;
		if (label != null)
		{
			int byteCount = _utf8.GetByteCount(label);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(label, 0, label.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_SmallButtonDelegate(array);
	}

	public void Text(string text)
	{
		byte[] array = null;
		if (text != null)
		{
			int byteCount = _utf8.GetByteCount(text);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(text, 0, text.Length, array, 0);
			array[byteCount] = 0;
		}
		call_TextDelegate(array);
	}

	public bool TreeNode(string name)
	{
		byte[] array = null;
		if (name != null)
		{
			int byteCount = _utf8.GetByteCount(name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(name, 0, name.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_TreeNodeDelegate(array);
	}

	public void TreePop()
	{
		call_TreePopDelegate();
	}
}
