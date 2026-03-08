using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks
{
	// Token: 0x02000014 RID: 20
	internal class ScriptingInterfaceOfIImgui : IImgui
	{
		// Token: 0x0600025B RID: 603 RVA: 0x00012094 File Offset: 0x00010294
		public void Begin(string text)
		{
			byte[] array = null;
			if (text != null)
			{
				int byteCount = ScriptingInterfaceOfIImgui._utf8.GetByteCount(text);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(text, 0, text.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIImgui.call_BeginDelegate(array);
		}

		// Token: 0x0600025C RID: 604 RVA: 0x000120EE File Offset: 0x000102EE
		public void BeginMainThreadScope()
		{
			ScriptingInterfaceOfIImgui.call_BeginMainThreadScopeDelegate();
		}

		// Token: 0x0600025D RID: 605 RVA: 0x000120FC File Offset: 0x000102FC
		public void BeginWithCloseButton(string text, ref bool is_open)
		{
			byte[] array = null;
			if (text != null)
			{
				int byteCount = ScriptingInterfaceOfIImgui._utf8.GetByteCount(text);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(text, 0, text.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIImgui.call_BeginWithCloseButtonDelegate(array, ref is_open);
		}

		// Token: 0x0600025E RID: 606 RVA: 0x00012158 File Offset: 0x00010358
		public bool Button(string text)
		{
			byte[] array = null;
			if (text != null)
			{
				int byteCount = ScriptingInterfaceOfIImgui._utf8.GetByteCount(text);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(text, 0, text.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIImgui.call_ButtonDelegate(array);
		}

		// Token: 0x0600025F RID: 607 RVA: 0x000121B4 File Offset: 0x000103B4
		public bool Checkbox(string text, ref bool is_checked)
		{
			byte[] array = null;
			if (text != null)
			{
				int byteCount = ScriptingInterfaceOfIImgui._utf8.GetByteCount(text);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(text, 0, text.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIImgui.call_CheckboxDelegate(array, ref is_checked);
		}

		// Token: 0x06000260 RID: 608 RVA: 0x00012210 File Offset: 0x00010410
		public bool CollapsingHeader(string label)
		{
			byte[] array = null;
			if (label != null)
			{
				int byteCount = ScriptingInterfaceOfIImgui._utf8.GetByteCount(label);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(label, 0, label.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIImgui.call_CollapsingHeaderDelegate(array);
		}

		// Token: 0x06000261 RID: 609 RVA: 0x0001226C File Offset: 0x0001046C
		public void Columns(int count, string id, bool border)
		{
			byte[] array = null;
			if (id != null)
			{
				int byteCount = ScriptingInterfaceOfIImgui._utf8.GetByteCount(id);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(id, 0, id.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIImgui.call_ColumnsDelegate(count, array, border);
		}

		// Token: 0x06000262 RID: 610 RVA: 0x000122C8 File Offset: 0x000104C8
		public bool Combo(string label, ref int selectedIndex, string items)
		{
			byte[] array = null;
			if (label != null)
			{
				int byteCount = ScriptingInterfaceOfIImgui._utf8.GetByteCount(label);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(label, 0, label.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (items != null)
			{
				int byteCount2 = ScriptingInterfaceOfIImgui._utf8.GetByteCount(items);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(items, 0, items.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			return ScriptingInterfaceOfIImgui.call_ComboDelegate(array, ref selectedIndex, array2);
		}

		// Token: 0x06000263 RID: 611 RVA: 0x00012368 File Offset: 0x00010568
		public bool ComboCustomSeperator(string label, ref int selectedIndex, string items, string seperator)
		{
			byte[] array = null;
			if (label != null)
			{
				int byteCount = ScriptingInterfaceOfIImgui._utf8.GetByteCount(label);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(label, 0, label.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (items != null)
			{
				int byteCount2 = ScriptingInterfaceOfIImgui._utf8.GetByteCount(items);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(items, 0, items.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			byte[] array3 = null;
			if (seperator != null)
			{
				int byteCount3 = ScriptingInterfaceOfIImgui._utf8.GetByteCount(seperator);
				array3 = ((byteCount3 < 1024) ? CallbackStringBufferManager.StringBuffer2 : new byte[byteCount3 + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(seperator, 0, seperator.Length, array3, 0);
				array3[byteCount3] = 0;
			}
			return ScriptingInterfaceOfIImgui.call_ComboCustomSeperatorDelegate(array, ref selectedIndex, array2, array3);
		}

		// Token: 0x06000264 RID: 612 RVA: 0x00012455 File Offset: 0x00010655
		public void End()
		{
			ScriptingInterfaceOfIImgui.call_EndDelegate();
		}

		// Token: 0x06000265 RID: 613 RVA: 0x00012461 File Offset: 0x00010661
		public void EndMainThreadScope()
		{
			ScriptingInterfaceOfIImgui.call_EndMainThreadScopeDelegate();
		}

		// Token: 0x06000266 RID: 614 RVA: 0x00012470 File Offset: 0x00010670
		public bool InputFloat(string label, ref float val, float step, float stepFast, int decimalPrecision)
		{
			byte[] array = null;
			if (label != null)
			{
				int byteCount = ScriptingInterfaceOfIImgui._utf8.GetByteCount(label);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(label, 0, label.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIImgui.call_InputFloatDelegate(array, ref val, step, stepFast, decimalPrecision);
		}

		// Token: 0x06000267 RID: 615 RVA: 0x000124D0 File Offset: 0x000106D0
		public bool InputFloat2(string label, ref float val0, ref float val1, int decimalPrecision)
		{
			byte[] array = null;
			if (label != null)
			{
				int byteCount = ScriptingInterfaceOfIImgui._utf8.GetByteCount(label);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(label, 0, label.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIImgui.call_InputFloat2Delegate(array, ref val0, ref val1, decimalPrecision);
		}

		// Token: 0x06000268 RID: 616 RVA: 0x00012530 File Offset: 0x00010730
		public bool InputFloat3(string label, ref float val0, ref float val1, ref float val2, int decimalPrecision)
		{
			byte[] array = null;
			if (label != null)
			{
				int byteCount = ScriptingInterfaceOfIImgui._utf8.GetByteCount(label);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(label, 0, label.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIImgui.call_InputFloat3Delegate(array, ref val0, ref val1, ref val2, decimalPrecision);
		}

		// Token: 0x06000269 RID: 617 RVA: 0x00012590 File Offset: 0x00010790
		public bool InputFloat4(string label, ref float val0, ref float val1, ref float val2, ref float val3, int decimalPrecision)
		{
			byte[] array = null;
			if (label != null)
			{
				int byteCount = ScriptingInterfaceOfIImgui._utf8.GetByteCount(label);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(label, 0, label.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIImgui.call_InputFloat4Delegate(array, ref val0, ref val1, ref val2, ref val3, decimalPrecision);
		}

		// Token: 0x0600026A RID: 618 RVA: 0x000125F4 File Offset: 0x000107F4
		public bool InputInt(string label, ref int value)
		{
			byte[] array = null;
			if (label != null)
			{
				int byteCount = ScriptingInterfaceOfIImgui._utf8.GetByteCount(label);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(label, 0, label.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIImgui.call_InputIntDelegate(array, ref value);
		}

		// Token: 0x0600026B RID: 619 RVA: 0x00012650 File Offset: 0x00010850
		public string InputText(string label, string inputTest, ref bool changed)
		{
			byte[] array = null;
			if (label != null)
			{
				int byteCount = ScriptingInterfaceOfIImgui._utf8.GetByteCount(label);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(label, 0, label.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (inputTest != null)
			{
				int byteCount2 = ScriptingInterfaceOfIImgui._utf8.GetByteCount(inputTest);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(inputTest, 0, inputTest.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			if (ScriptingInterfaceOfIImgui.call_InputTextDelegate(array, array2, ref changed) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x0600026C RID: 620 RVA: 0x000126F8 File Offset: 0x000108F8
		public string InputTextMultilineCopyPaste(string label, string inputTest, int textBoxHeight, ref bool changed)
		{
			byte[] array = null;
			if (label != null)
			{
				int byteCount = ScriptingInterfaceOfIImgui._utf8.GetByteCount(label);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(label, 0, label.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (inputTest != null)
			{
				int byteCount2 = ScriptingInterfaceOfIImgui._utf8.GetByteCount(inputTest);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(inputTest, 0, inputTest.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			if (ScriptingInterfaceOfIImgui.call_InputTextMultilineCopyPasteDelegate(array, array2, textBoxHeight, ref changed) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x0600026D RID: 621 RVA: 0x000127A2 File Offset: 0x000109A2
		public bool IsItemHovered()
		{
			return ScriptingInterfaceOfIImgui.call_IsItemHoveredDelegate();
		}

		// Token: 0x0600026E RID: 622 RVA: 0x000127AE File Offset: 0x000109AE
		public void NewFrame()
		{
			ScriptingInterfaceOfIImgui.call_NewFrameDelegate();
		}

		// Token: 0x0600026F RID: 623 RVA: 0x000127BA File Offset: 0x000109BA
		public void NewLine()
		{
			ScriptingInterfaceOfIImgui.call_NewLineDelegate();
		}

		// Token: 0x06000270 RID: 624 RVA: 0x000127C6 File Offset: 0x000109C6
		public void NextColumn()
		{
			ScriptingInterfaceOfIImgui.call_NextColumnDelegate();
		}

		// Token: 0x06000271 RID: 625 RVA: 0x000127D4 File Offset: 0x000109D4
		public void PlotLines(string name, float[] values, int valuesCount, int valuesOffset, string overlayText, float minScale, float maxScale, float graphWidth, float graphHeight, int stride)
		{
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfIImgui._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			PinnedArrayData<float> pinnedArrayData = new PinnedArrayData<float>(values, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			byte[] array2 = null;
			if (overlayText != null)
			{
				int byteCount2 = ScriptingInterfaceOfIImgui._utf8.GetByteCount(overlayText);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(overlayText, 0, overlayText.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			ScriptingInterfaceOfIImgui.call_PlotLinesDelegate(array, pointer, valuesCount, valuesOffset, array2, minScale, maxScale, graphWidth, graphHeight, stride);
			pinnedArrayData.Dispose();
		}

		// Token: 0x06000272 RID: 626 RVA: 0x000128A3 File Offset: 0x00010AA3
		public void PopStyleColor()
		{
			ScriptingInterfaceOfIImgui.call_PopStyleColorDelegate();
		}

		// Token: 0x06000273 RID: 627 RVA: 0x000128AF File Offset: 0x00010AAF
		public void ProgressBar(float value)
		{
			ScriptingInterfaceOfIImgui.call_ProgressBarDelegate(value);
		}

		// Token: 0x06000274 RID: 628 RVA: 0x000128BC File Offset: 0x00010ABC
		public void PushStyleColor(int style, ref Vec3 color)
		{
			ScriptingInterfaceOfIImgui.call_PushStyleColorDelegate(style, ref color);
		}

		// Token: 0x06000275 RID: 629 RVA: 0x000128CC File Offset: 0x00010ACC
		public bool RadioButton(string label, bool active)
		{
			byte[] array = null;
			if (label != null)
			{
				int byteCount = ScriptingInterfaceOfIImgui._utf8.GetByteCount(label);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(label, 0, label.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIImgui.call_RadioButtonDelegate(array, active);
		}

		// Token: 0x06000276 RID: 630 RVA: 0x00012927 File Offset: 0x00010B27
		public void Render()
		{
			ScriptingInterfaceOfIImgui.call_RenderDelegate();
		}

		// Token: 0x06000277 RID: 631 RVA: 0x00012933 File Offset: 0x00010B33
		public void SameLine(float posX, float spacingWidth)
		{
			ScriptingInterfaceOfIImgui.call_SameLineDelegate(posX, spacingWidth);
		}

		// Token: 0x06000278 RID: 632 RVA: 0x00012941 File Offset: 0x00010B41
		public void Separator()
		{
			ScriptingInterfaceOfIImgui.call_SeparatorDelegate();
		}

		// Token: 0x06000279 RID: 633 RVA: 0x00012950 File Offset: 0x00010B50
		public void SetTooltip(string label)
		{
			byte[] array = null;
			if (label != null)
			{
				int byteCount = ScriptingInterfaceOfIImgui._utf8.GetByteCount(label);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(label, 0, label.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIImgui.call_SetTooltipDelegate(array);
		}

		// Token: 0x0600027A RID: 634 RVA: 0x000129AC File Offset: 0x00010BAC
		public bool SliderFloat(string label, ref float value, float min, float max)
		{
			byte[] array = null;
			if (label != null)
			{
				int byteCount = ScriptingInterfaceOfIImgui._utf8.GetByteCount(label);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(label, 0, label.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIImgui.call_SliderFloatDelegate(array, ref value, min, max);
		}

		// Token: 0x0600027B RID: 635 RVA: 0x00012A0C File Offset: 0x00010C0C
		public bool SmallButton(string label)
		{
			byte[] array = null;
			if (label != null)
			{
				int byteCount = ScriptingInterfaceOfIImgui._utf8.GetByteCount(label);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(label, 0, label.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIImgui.call_SmallButtonDelegate(array);
		}

		// Token: 0x0600027C RID: 636 RVA: 0x00012A68 File Offset: 0x00010C68
		public void Text(string text)
		{
			byte[] array = null;
			if (text != null)
			{
				int byteCount = ScriptingInterfaceOfIImgui._utf8.GetByteCount(text);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(text, 0, text.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIImgui.call_TextDelegate(array);
		}

		// Token: 0x0600027D RID: 637 RVA: 0x00012AC4 File Offset: 0x00010CC4
		public bool TreeNode(string name)
		{
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfIImgui._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIImgui._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIImgui.call_TreeNodeDelegate(array);
		}

		// Token: 0x0600027E RID: 638 RVA: 0x00012B1E File Offset: 0x00010D1E
		public void TreePop()
		{
			ScriptingInterfaceOfIImgui.call_TreePopDelegate();
		}

		// Token: 0x040001D6 RID: 470
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x040001D7 RID: 471
		public static ScriptingInterfaceOfIImgui.BeginDelegate call_BeginDelegate;

		// Token: 0x040001D8 RID: 472
		public static ScriptingInterfaceOfIImgui.BeginMainThreadScopeDelegate call_BeginMainThreadScopeDelegate;

		// Token: 0x040001D9 RID: 473
		public static ScriptingInterfaceOfIImgui.BeginWithCloseButtonDelegate call_BeginWithCloseButtonDelegate;

		// Token: 0x040001DA RID: 474
		public static ScriptingInterfaceOfIImgui.ButtonDelegate call_ButtonDelegate;

		// Token: 0x040001DB RID: 475
		public static ScriptingInterfaceOfIImgui.CheckboxDelegate call_CheckboxDelegate;

		// Token: 0x040001DC RID: 476
		public static ScriptingInterfaceOfIImgui.CollapsingHeaderDelegate call_CollapsingHeaderDelegate;

		// Token: 0x040001DD RID: 477
		public static ScriptingInterfaceOfIImgui.ColumnsDelegate call_ColumnsDelegate;

		// Token: 0x040001DE RID: 478
		public static ScriptingInterfaceOfIImgui.ComboDelegate call_ComboDelegate;

		// Token: 0x040001DF RID: 479
		public static ScriptingInterfaceOfIImgui.ComboCustomSeperatorDelegate call_ComboCustomSeperatorDelegate;

		// Token: 0x040001E0 RID: 480
		public static ScriptingInterfaceOfIImgui.EndDelegate call_EndDelegate;

		// Token: 0x040001E1 RID: 481
		public static ScriptingInterfaceOfIImgui.EndMainThreadScopeDelegate call_EndMainThreadScopeDelegate;

		// Token: 0x040001E2 RID: 482
		public static ScriptingInterfaceOfIImgui.InputFloatDelegate call_InputFloatDelegate;

		// Token: 0x040001E3 RID: 483
		public static ScriptingInterfaceOfIImgui.InputFloat2Delegate call_InputFloat2Delegate;

		// Token: 0x040001E4 RID: 484
		public static ScriptingInterfaceOfIImgui.InputFloat3Delegate call_InputFloat3Delegate;

		// Token: 0x040001E5 RID: 485
		public static ScriptingInterfaceOfIImgui.InputFloat4Delegate call_InputFloat4Delegate;

		// Token: 0x040001E6 RID: 486
		public static ScriptingInterfaceOfIImgui.InputIntDelegate call_InputIntDelegate;

		// Token: 0x040001E7 RID: 487
		public static ScriptingInterfaceOfIImgui.InputTextDelegate call_InputTextDelegate;

		// Token: 0x040001E8 RID: 488
		public static ScriptingInterfaceOfIImgui.InputTextMultilineCopyPasteDelegate call_InputTextMultilineCopyPasteDelegate;

		// Token: 0x040001E9 RID: 489
		public static ScriptingInterfaceOfIImgui.IsItemHoveredDelegate call_IsItemHoveredDelegate;

		// Token: 0x040001EA RID: 490
		public static ScriptingInterfaceOfIImgui.NewFrameDelegate call_NewFrameDelegate;

		// Token: 0x040001EB RID: 491
		public static ScriptingInterfaceOfIImgui.NewLineDelegate call_NewLineDelegate;

		// Token: 0x040001EC RID: 492
		public static ScriptingInterfaceOfIImgui.NextColumnDelegate call_NextColumnDelegate;

		// Token: 0x040001ED RID: 493
		public static ScriptingInterfaceOfIImgui.PlotLinesDelegate call_PlotLinesDelegate;

		// Token: 0x040001EE RID: 494
		public static ScriptingInterfaceOfIImgui.PopStyleColorDelegate call_PopStyleColorDelegate;

		// Token: 0x040001EF RID: 495
		public static ScriptingInterfaceOfIImgui.ProgressBarDelegate call_ProgressBarDelegate;

		// Token: 0x040001F0 RID: 496
		public static ScriptingInterfaceOfIImgui.PushStyleColorDelegate call_PushStyleColorDelegate;

		// Token: 0x040001F1 RID: 497
		public static ScriptingInterfaceOfIImgui.RadioButtonDelegate call_RadioButtonDelegate;

		// Token: 0x040001F2 RID: 498
		public static ScriptingInterfaceOfIImgui.RenderDelegate call_RenderDelegate;

		// Token: 0x040001F3 RID: 499
		public static ScriptingInterfaceOfIImgui.SameLineDelegate call_SameLineDelegate;

		// Token: 0x040001F4 RID: 500
		public static ScriptingInterfaceOfIImgui.SeparatorDelegate call_SeparatorDelegate;

		// Token: 0x040001F5 RID: 501
		public static ScriptingInterfaceOfIImgui.SetTooltipDelegate call_SetTooltipDelegate;

		// Token: 0x040001F6 RID: 502
		public static ScriptingInterfaceOfIImgui.SliderFloatDelegate call_SliderFloatDelegate;

		// Token: 0x040001F7 RID: 503
		public static ScriptingInterfaceOfIImgui.SmallButtonDelegate call_SmallButtonDelegate;

		// Token: 0x040001F8 RID: 504
		public static ScriptingInterfaceOfIImgui.TextDelegate call_TextDelegate;

		// Token: 0x040001F9 RID: 505
		public static ScriptingInterfaceOfIImgui.TreeNodeDelegate call_TreeNodeDelegate;

		// Token: 0x040001FA RID: 506
		public static ScriptingInterfaceOfIImgui.TreePopDelegate call_TreePopDelegate;

		// Token: 0x02000251 RID: 593
		// (Invoke) Token: 0x06000F57 RID: 3927
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void BeginDelegate(byte[] text);

		// Token: 0x02000252 RID: 594
		// (Invoke) Token: 0x06000F5B RID: 3931
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void BeginMainThreadScopeDelegate();

		// Token: 0x02000253 RID: 595
		// (Invoke) Token: 0x06000F5F RID: 3935
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void BeginWithCloseButtonDelegate(byte[] text, [MarshalAs(UnmanagedType.U1)] ref bool is_open);

		// Token: 0x02000254 RID: 596
		// (Invoke) Token: 0x06000F63 RID: 3939
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool ButtonDelegate(byte[] text);

		// Token: 0x02000255 RID: 597
		// (Invoke) Token: 0x06000F67 RID: 3943
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool CheckboxDelegate(byte[] text, [MarshalAs(UnmanagedType.U1)] ref bool is_checked);

		// Token: 0x02000256 RID: 598
		// (Invoke) Token: 0x06000F6B RID: 3947
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool CollapsingHeaderDelegate(byte[] label);

		// Token: 0x02000257 RID: 599
		// (Invoke) Token: 0x06000F6F RID: 3951
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ColumnsDelegate(int count, byte[] id, [MarshalAs(UnmanagedType.U1)] bool border);

		// Token: 0x02000258 RID: 600
		// (Invoke) Token: 0x06000F73 RID: 3955
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool ComboDelegate(byte[] label, ref int selectedIndex, byte[] items);

		// Token: 0x02000259 RID: 601
		// (Invoke) Token: 0x06000F77 RID: 3959
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool ComboCustomSeperatorDelegate(byte[] label, ref int selectedIndex, byte[] items, byte[] seperator);

		// Token: 0x0200025A RID: 602
		// (Invoke) Token: 0x06000F7B RID: 3963
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void EndDelegate();

		// Token: 0x0200025B RID: 603
		// (Invoke) Token: 0x06000F7F RID: 3967
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void EndMainThreadScopeDelegate();

		// Token: 0x0200025C RID: 604
		// (Invoke) Token: 0x06000F83 RID: 3971
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool InputFloatDelegate(byte[] label, ref float val, float step, float stepFast, int decimalPrecision);

		// Token: 0x0200025D RID: 605
		// (Invoke) Token: 0x06000F87 RID: 3975
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool InputFloat2Delegate(byte[] label, ref float val0, ref float val1, int decimalPrecision);

		// Token: 0x0200025E RID: 606
		// (Invoke) Token: 0x06000F8B RID: 3979
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool InputFloat3Delegate(byte[] label, ref float val0, ref float val1, ref float val2, int decimalPrecision);

		// Token: 0x0200025F RID: 607
		// (Invoke) Token: 0x06000F8F RID: 3983
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool InputFloat4Delegate(byte[] label, ref float val0, ref float val1, ref float val2, ref float val3, int decimalPrecision);

		// Token: 0x02000260 RID: 608
		// (Invoke) Token: 0x06000F93 RID: 3987
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool InputIntDelegate(byte[] label, ref int value);

		// Token: 0x02000261 RID: 609
		// (Invoke) Token: 0x06000F97 RID: 3991
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int InputTextDelegate(byte[] label, byte[] inputTest, [MarshalAs(UnmanagedType.U1)] ref bool changed);

		// Token: 0x02000262 RID: 610
		// (Invoke) Token: 0x06000F9B RID: 3995
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int InputTextMultilineCopyPasteDelegate(byte[] label, byte[] inputTest, int textBoxHeight, [MarshalAs(UnmanagedType.U1)] ref bool changed);

		// Token: 0x02000263 RID: 611
		// (Invoke) Token: 0x06000F9F RID: 3999
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsItemHoveredDelegate();

		// Token: 0x02000264 RID: 612
		// (Invoke) Token: 0x06000FA3 RID: 4003
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void NewFrameDelegate();

		// Token: 0x02000265 RID: 613
		// (Invoke) Token: 0x06000FA7 RID: 4007
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void NewLineDelegate();

		// Token: 0x02000266 RID: 614
		// (Invoke) Token: 0x06000FAB RID: 4011
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void NextColumnDelegate();

		// Token: 0x02000267 RID: 615
		// (Invoke) Token: 0x06000FAF RID: 4015
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void PlotLinesDelegate(byte[] name, IntPtr values, int valuesCount, int valuesOffset, byte[] overlayText, float minScale, float maxScale, float graphWidth, float graphHeight, int stride);

		// Token: 0x02000268 RID: 616
		// (Invoke) Token: 0x06000FB3 RID: 4019
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void PopStyleColorDelegate();

		// Token: 0x02000269 RID: 617
		// (Invoke) Token: 0x06000FB7 RID: 4023
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ProgressBarDelegate(float value);

		// Token: 0x0200026A RID: 618
		// (Invoke) Token: 0x06000FBB RID: 4027
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void PushStyleColorDelegate(int style, ref Vec3 color);

		// Token: 0x0200026B RID: 619
		// (Invoke) Token: 0x06000FBF RID: 4031
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool RadioButtonDelegate(byte[] label, [MarshalAs(UnmanagedType.U1)] bool active);

		// Token: 0x0200026C RID: 620
		// (Invoke) Token: 0x06000FC3 RID: 4035
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RenderDelegate();

		// Token: 0x0200026D RID: 621
		// (Invoke) Token: 0x06000FC7 RID: 4039
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SameLineDelegate(float posX, float spacingWidth);

		// Token: 0x0200026E RID: 622
		// (Invoke) Token: 0x06000FCB RID: 4043
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SeparatorDelegate();

		// Token: 0x0200026F RID: 623
		// (Invoke) Token: 0x06000FCF RID: 4047
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetTooltipDelegate(byte[] label);

		// Token: 0x02000270 RID: 624
		// (Invoke) Token: 0x06000FD3 RID: 4051
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool SliderFloatDelegate(byte[] label, ref float value, float min, float max);

		// Token: 0x02000271 RID: 625
		// (Invoke) Token: 0x06000FD7 RID: 4055
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool SmallButtonDelegate(byte[] label);

		// Token: 0x02000272 RID: 626
		// (Invoke) Token: 0x06000FDB RID: 4059
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void TextDelegate(byte[] text);

		// Token: 0x02000273 RID: 627
		// (Invoke) Token: 0x06000FDF RID: 4063
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool TreeNodeDelegate(byte[] name);

		// Token: 0x02000274 RID: 628
		// (Invoke) Token: 0x06000FE3 RID: 4067
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void TreePopDelegate();
	}
}
