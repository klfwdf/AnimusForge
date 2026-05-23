using System;
using System.Runtime.CompilerServices;
using TaleWorlds.Library;

namespace TaleWorlds.TwoDimension.Standalone
{
	// Token: 0x0200000D RID: 13
	public class TwoDimensionPlatform : ITwoDimensionPlatform, ITwoDimensionResourceContext
	{
		// Token: 0x17000019 RID: 25
		// (get) Token: 0x060000AA RID: 170 RVA: 0x00004960 File Offset: 0x00002B60
		float ITwoDimensionPlatform.Width
		{
			get
			{
				return (float)this._form.Width;
			}
		}

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x060000AB RID: 171 RVA: 0x0000496E File Offset: 0x00002B6E
		float ITwoDimensionPlatform.Height
		{
			get
			{
				return (float)this._form.Height;
			}
		}

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x060000AC RID: 172 RVA: 0x0000497C File Offset: 0x00002B7C
		float ITwoDimensionPlatform.ReferenceWidth
		{
			get
			{
				return 1154f;
			}
		}

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x060000AD RID: 173 RVA: 0x00004983 File Offset: 0x00002B83
		float ITwoDimensionPlatform.ReferenceHeight
		{
			get
			{
				return 701f;
			}
		}

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x060000AE RID: 174 RVA: 0x0000498A File Offset: 0x00002B8A
		float ITwoDimensionPlatform.ApplicationTime
		{
			get
			{
				return (float)Environment.TickCount;
			}
		}

		// Token: 0x060000AF RID: 175 RVA: 0x00004992 File Offset: 0x00002B92
		public TwoDimensionPlatform(GraphicsForm form, bool isAssetsUnderDefaultFolders)
		{
			this._form = form;
			this._isAssetsUnderDefaultFolders = isAssetsUnderDefaultFolders;
			this._graphicsContext = this._form.GraphicsContext;
		}

		// Token: 0x060000B0 RID: 176 RVA: 0x000049B9 File Offset: 0x00002BB9
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void ITwoDimensionPlatform.DrawImage(SimpleMaterial material, in ImageDrawObject drawObject2D, int layer)
		{
			this._graphicsContext.DrawImage(material, drawObject2D);
		}

		// Token: 0x060000B1 RID: 177 RVA: 0x000049C8 File Offset: 0x00002BC8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void ITwoDimensionPlatform.DrawText(TextMaterial material, in TextDrawObject drawObject2D, int layer)
		{
			this._graphicsContext.DrawText(material, drawObject2D);
		}

		// Token: 0x060000B2 RID: 178 RVA: 0x000049D7 File Offset: 0x00002BD7
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void ITwoDimensionPlatform.OnFrameBegin()
		{
		}

		// Token: 0x060000B3 RID: 179 RVA: 0x000049D9 File Offset: 0x00002BD9
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void ITwoDimensionPlatform.OnFrameEnd()
		{
		}

		// Token: 0x060000B4 RID: 180 RVA: 0x000049DB File Offset: 0x00002BDB
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void ITwoDimensionPlatform.Clear()
		{
		}

		// Token: 0x060000B5 RID: 181 RVA: 0x000049E0 File Offset: 0x00002BE0
		Texture ITwoDimensionResourceContext.LoadTexture(ResourceDepot resourceDepot, string name)
		{
			OpenGLTexture openGLTexture = new OpenGLTexture();
			string name2 = name;
			if (!this._isAssetsUnderDefaultFolders)
			{
				string[] array = name.Split(new char[] { '\\' });
				name2 = array[array.Length - 1];
			}
			openGLTexture.LoadFromFile(resourceDepot, name2);
			return new Texture(openGLTexture);
		}

		// Token: 0x060000B6 RID: 182 RVA: 0x00004A23 File Offset: 0x00002C23
		void ITwoDimensionPlatform.PlaySound(string soundName)
		{
			Debug.Print("Playing sound: " + soundName, 0, Debug.DebugColor.White, 17592186044416UL);
		}

		// Token: 0x060000B7 RID: 183 RVA: 0x00004A41 File Offset: 0x00002C41
		void ITwoDimensionPlatform.SetScissor(ScissorTestInfo scissorTestInfo)
		{
			this._graphicsContext.SetScissor(scissorTestInfo);
		}

		// Token: 0x060000B8 RID: 184 RVA: 0x00004A4F File Offset: 0x00002C4F
		void ITwoDimensionPlatform.ResetScissors()
		{
			this._graphicsContext.ResetScissor();
		}

		// Token: 0x060000B9 RID: 185 RVA: 0x00004A5C File Offset: 0x00002C5C
		void ITwoDimensionPlatform.CreateSoundEvent(string soundName)
		{
			Debug.Print("Created sound event: " + soundName, 0, Debug.DebugColor.White, 17592186044416UL);
		}

		// Token: 0x060000BA RID: 186 RVA: 0x00004A7A File Offset: 0x00002C7A
		void ITwoDimensionPlatform.StopAndRemoveSoundEvent(string soundName)
		{
			Debug.Print("Stopped sound event: " + soundName, 0, Debug.DebugColor.White, 17592186044416UL);
		}

		// Token: 0x060000BB RID: 187 RVA: 0x00004A98 File Offset: 0x00002C98
		void ITwoDimensionPlatform.PlaySoundEvent(string soundName)
		{
			Debug.Print("Played sound event: " + soundName, 0, Debug.DebugColor.White, 17592186044416UL);
		}

		// Token: 0x060000BC RID: 188 RVA: 0x00004AB6 File Offset: 0x00002CB6
		void ITwoDimensionPlatform.OpenOnScreenKeyboard(string initialText, string descriptionText, int maxLength, int keyboardTypeEnum)
		{
			Debug.Print("Opened on-screen keyboard", 0, Debug.DebugColor.White, 17592186044416UL);
		}

		// Token: 0x060000BD RID: 189 RVA: 0x00004ACE File Offset: 0x00002CCE
		void ITwoDimensionPlatform.BeginDebugPanel(string panelTitle)
		{
		}

		// Token: 0x060000BE RID: 190 RVA: 0x00004AD0 File Offset: 0x00002CD0
		void ITwoDimensionPlatform.EndDebugPanel()
		{
		}

		// Token: 0x060000BF RID: 191 RVA: 0x00004AD2 File Offset: 0x00002CD2
		void ITwoDimensionPlatform.DrawDebugText(string text)
		{
			Debug.Print(text, 0, Debug.DebugColor.White, 17592186044416UL);
		}

		// Token: 0x060000C0 RID: 192 RVA: 0x00004AE6 File Offset: 0x00002CE6
		bool ITwoDimensionPlatform.IsDebugModeEnabled()
		{
			return false;
		}

		// Token: 0x060000C1 RID: 193 RVA: 0x00004AE9 File Offset: 0x00002CE9
		bool ITwoDimensionPlatform.DrawDebugTreeNode(string text)
		{
			return false;
		}

		// Token: 0x060000C2 RID: 194 RVA: 0x00004AEC File Offset: 0x00002CEC
		void ITwoDimensionPlatform.DrawCheckbox(string label, ref bool isChecked)
		{
		}

		// Token: 0x060000C3 RID: 195 RVA: 0x00004AEE File Offset: 0x00002CEE
		bool ITwoDimensionPlatform.IsDebugItemHovered()
		{
			return false;
		}

		// Token: 0x060000C4 RID: 196 RVA: 0x00004AF1 File Offset: 0x00002CF1
		void ITwoDimensionPlatform.PopDebugTreeNode()
		{
		}

		// Token: 0x04000040 RID: 64
		private GraphicsContext _graphicsContext;

		// Token: 0x04000041 RID: 65
		private GraphicsForm _form;

		// Token: 0x04000042 RID: 66
		private bool _isAssetsUnderDefaultFolders;
	}
}
