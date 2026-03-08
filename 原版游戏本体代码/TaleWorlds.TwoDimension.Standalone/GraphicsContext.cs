using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension.Standalone.Native;
using TaleWorlds.TwoDimension.Standalone.Native.OpenGL;
using TaleWorlds.TwoDimension.Standalone.Native.Windows;

namespace TaleWorlds.TwoDimension.Standalone
{
	// Token: 0x02000005 RID: 5
	public class GraphicsContext
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000006 RID: 6 RVA: 0x00002060 File Offset: 0x00000260
		// (set) Token: 0x06000007 RID: 7 RVA: 0x00002068 File Offset: 0x00000268
		internal WindowsForm Control { get; set; }

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000008 RID: 8 RVA: 0x00002071 File Offset: 0x00000271
		// (set) Token: 0x06000009 RID: 9 RVA: 0x00002078 File Offset: 0x00000278
		public static GraphicsContext Active { get; private set; }

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x0600000A RID: 10 RVA: 0x00002080 File Offset: 0x00000280
		// (set) Token: 0x0600000B RID: 11 RVA: 0x00002088 File Offset: 0x00000288
		internal Dictionary<string, OpenGLTexture> LoadedTextures { get; private set; }

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x0600000C RID: 12 RVA: 0x00002091 File Offset: 0x00000291
		// (set) Token: 0x0600000D RID: 13 RVA: 0x00002099 File Offset: 0x00000299
		public Matrix4x4 ProjectionMatrix
		{
			get
			{
				return this._projectionMatrix;
			}
			set
			{
				this._projectionMatrix = value;
			}
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600000E RID: 14 RVA: 0x000020A2 File Offset: 0x000002A2
		// (set) Token: 0x0600000F RID: 15 RVA: 0x000020AA File Offset: 0x000002AA
		public Matrix4x4 ViewMatrix
		{
			get
			{
				return this._viewMatrix;
			}
			set
			{
				this._viewMatrix = value;
				this._modelViewMatrix = this._viewMatrix * this._modelMatrix;
			}
		}

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000010 RID: 16 RVA: 0x000020CA File Offset: 0x000002CA
		// (set) Token: 0x06000011 RID: 17 RVA: 0x000020D2 File Offset: 0x000002D2
		public Matrix4x4 ModelMatrix
		{
			get
			{
				return this._modelMatrix;
			}
			set
			{
				this._modelMatrix = value;
				this._modelViewMatrix = this._viewMatrix * this._modelMatrix;
			}
		}

		// Token: 0x06000012 RID: 18 RVA: 0x000020F4 File Offset: 0x000002F4
		public GraphicsContext()
		{
			this.LoadedTextures = new Dictionary<string, OpenGLTexture>();
			this._loadedShaders = new Dictionary<string, Shader>();
			this._stopwatch = new Stopwatch();
			this.MaxTimeToRenderOneFrame = 16;
		}

		// Token: 0x06000013 RID: 19 RVA: 0x00002168 File Offset: 0x00000368
		public void CreateContext(ResourceDepot resourceDepot)
		{
			this._resourceDepot = resourceDepot;
			this._handleDeviceContext = User32.GetDC(this.Control.Handle);
			if (this._handleDeviceContext == IntPtr.Zero)
			{
				Debug.Print("Can't get device context", 0, Debug.DebugColor.White, 17592186044416UL);
			}
			if (!Opengl32.wglMakeCurrent(this._handleDeviceContext, IntPtr.Zero))
			{
				Debug.Print("Can't reset context", 0, Debug.DebugColor.White, 17592186044416UL);
			}
			PixelFormatDescriptor pixelFormatDescriptor = default(PixelFormatDescriptor);
			Marshal.SizeOf(typeof(PixelFormatDescriptor));
			pixelFormatDescriptor.nSize = (ushort)Marshal.SizeOf(typeof(PixelFormatDescriptor));
			pixelFormatDescriptor.nVersion = 1;
			pixelFormatDescriptor.dwFlags = 37U;
			pixelFormatDescriptor.iPixelType = 0;
			pixelFormatDescriptor.cColorBits = 32;
			pixelFormatDescriptor.cRedBits = 0;
			pixelFormatDescriptor.cRedShift = 0;
			pixelFormatDescriptor.cGreenBits = 0;
			pixelFormatDescriptor.cGreenShift = 0;
			pixelFormatDescriptor.cBlueBits = 0;
			pixelFormatDescriptor.cBlueShift = 0;
			pixelFormatDescriptor.cAlphaBits = 8;
			pixelFormatDescriptor.cAlphaShift = 0;
			pixelFormatDescriptor.cAccumBits = 0;
			pixelFormatDescriptor.cAccumRedBits = 0;
			pixelFormatDescriptor.cAccumGreenBits = 0;
			pixelFormatDescriptor.cAccumBlueBits = 0;
			pixelFormatDescriptor.cAccumAlphaBits = 0;
			pixelFormatDescriptor.cDepthBits = 24;
			pixelFormatDescriptor.cStencilBits = 8;
			pixelFormatDescriptor.cAuxBuffers = 0;
			pixelFormatDescriptor.iLayerType = 0;
			pixelFormatDescriptor.bReserved = 0;
			pixelFormatDescriptor.dwLayerMask = 0U;
			pixelFormatDescriptor.dwVisibleMask = 0U;
			pixelFormatDescriptor.dwDamageMask = 0U;
			int iPixelFormat = Gdi32.ChoosePixelFormat(this._handleDeviceContext, ref pixelFormatDescriptor);
			if (!Gdi32.SetPixelFormat(this._handleDeviceContext, iPixelFormat, ref pixelFormatDescriptor))
			{
				Debug.Print("can't set pixel format", 0, Debug.DebugColor.White, 17592186044416UL);
			}
			this._handleRenderContext = Opengl32.wglCreateContext(this._handleDeviceContext);
			if (this._handleRenderContext == IntPtr.Zero)
			{
				throw new OpenGlLoadException("Could not create default OpenGL context.");
			}
			this.SetActive();
			string @string = Opengl32.GetString(7938U);
			string string2 = Opengl32.GetString(7936U);
			string string3 = Opengl32.GetString(7937U);
			Watchdog.LogProperty("crash_tags.txt", "Runtime", "DefaultContextVersionOpenGL", @string);
			Watchdog.LogProperty("crash_tags.txt", "Runtime", "DefaultContextVendorOpenGL", string2);
			Watchdog.LogProperty("crash_tags.txt", "Runtime", "DefaultContextRendererOpenGL", string3);
			IntPtr handleRenderContext = this._handleRenderContext;
			this._handleRenderContext = IntPtr.Zero;
			GraphicsContext.Active = null;
			Opengl32ARB.LoadContextExtension(this._handleDeviceContext);
			int[] array = new int[10];
			int num = 0;
			array[num++] = 8337;
			array[num++] = 3;
			array[num++] = 8338;
			array[num++] = 3;
			array[num++] = 37158;
			array[num++] = 1;
			array[num++] = 0;
			this._handleRenderContext = Opengl32ARB.wglCreateContextAttribs(this._handleDeviceContext, IntPtr.Zero, array);
			if (this._handleRenderContext == IntPtr.Zero)
			{
				throw new OpenGlLoadException("Could not create OpenGL context.");
			}
			this.SetActive();
			string string4 = Opengl32.GetString(7938U);
			string string5 = Opengl32.GetString(7936U);
			string string6 = Opengl32.GetString(7937U);
			Watchdog.LogProperty("crash_tags.txt", "Runtime", "ContextVersionOpenGL", string4);
			Watchdog.LogProperty("crash_tags.txt", "Runtime", "ContextVendorOpenGL", string5);
			Watchdog.LogProperty("crash_tags.txt", "Runtime", "ContextRendererOpenGL", string6);
			Opengl32ARB.LoadExtensions(this._handleDeviceContext);
			Opengl32.wglDeleteContext(handleRenderContext);
			Opengl32.ShadeModel(ShadingModel.Smooth);
			Opengl32.ClearColor(0f, 0f, 0f, 0f);
			Opengl32.ClearDepth(1.0);
			Opengl32.Disable(Target.DepthTest);
			Opengl32.Hint(3152U, 4354U);
			this.ProjectionMatrix = Matrix4x4.Identity;
			this.ModelMatrix = Matrix4x4.Identity;
			this.ViewMatrix = Matrix4x4.Identity;
			this._simpleVAO = VertexArrayObject.Create();
			this._textureVAO = VertexArrayObject.CreateWithUVBuffer();
		}

		// Token: 0x06000014 RID: 20 RVA: 0x00002565 File Offset: 0x00000765
		public void SetActive()
		{
			if (GraphicsContext.Active != this)
			{
				if (Opengl32.wglMakeCurrent(this._handleDeviceContext, this._handleRenderContext))
				{
					GraphicsContext.Active = this;
					return;
				}
				Debug.Print("Can't activate context", 0, Debug.DebugColor.White, 17592186044416UL);
			}
		}

		// Token: 0x06000015 RID: 21 RVA: 0x000025A0 File Offset: 0x000007A0
		public void BeginFrame(int width, int height)
		{
			this._anyInvalidMatricesThisFrame = false;
			this._stopwatch.Start();
			this.Resize(width, height);
			Opengl32.Clear(AttribueMask.ColorBufferBit);
			Opengl32.ClearDepth(1.0);
			Opengl32.Disable(Target.DepthTest);
			Opengl32.Disable(Target.SCISSOR_TEST);
			Opengl32.Disable(Target.STENCIL_TEST);
			Opengl32.Disable(Target.Blend);
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002608 File Offset: 0x00000808
		public void SwapBuffers()
		{
			int num = (int)this._stopwatch.ElapsedMilliseconds;
			int num2 = 0;
			if (this.MaxTimeToRenderOneFrame > num)
			{
				num2 = this.MaxTimeToRenderOneFrame - num;
			}
			if (num2 > 0)
			{
				Thread.Sleep(num2);
			}
			Gdi32.SwapBuffers(this._handleDeviceContext);
			this._stopwatch.Restart();
			if (this._anyInvalidMatricesThisFrame)
			{
				this._failedRenderFrames++;
			}
			if (this._failedRenderFrames >= 100)
			{
				Debug.ShowMessageBox("Launcher render error", "ERROR", 4U);
				throw new Exception("[Launcher]: More than 100 frames had a render fail");
			}
		}

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000017 RID: 23 RVA: 0x00002692 File Offset: 0x00000892
		public bool IsActive
		{
			get
			{
				return GraphicsContext.Active == this;
			}
		}

		// Token: 0x06000018 RID: 24 RVA: 0x0000269C File Offset: 0x0000089C
		public void DestroyContext()
		{
			Opengl32.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
			Opengl32.wglDeleteContext(this._handleRenderContext);
			User32.ReleaseDC(this.Control.Handle, this._handleDeviceContext);
		}

		// Token: 0x06000019 RID: 25 RVA: 0x000026D4 File Offset: 0x000008D4
		public void SetScissor(ScissorTestInfo scissorTestInfo)
		{
			Opengl32.GetInteger(Target.VIEWPORT, this._scissorParameters);
			SimpleRectangle simpleRectangle = scissorTestInfo.GetSimpleRectangle();
			Opengl32.Scissor((int)simpleRectangle.X, this._scissorParameters[3] - (int)simpleRectangle.Height - (int)simpleRectangle.Y, (int)simpleRectangle.Width, (int)simpleRectangle.Height);
			Opengl32.Enable(Target.SCISSOR_TEST);
		}

		// Token: 0x0600001A RID: 26 RVA: 0x00002738 File Offset: 0x00000938
		public void ResetScissor()
		{
			Opengl32.Disable(Target.SCISSOR_TEST);
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00002744 File Offset: 0x00000944
		public Shader GetOrLoadShader(string shaderName)
		{
			if (!this._loadedShaders.ContainsKey(shaderName))
			{
				string filePath = this._resourceDepot.GetFilePath(shaderName + ".vert");
				string filePath2 = this._resourceDepot.GetFilePath(shaderName + ".frag");
				string vertexShaderCode = File.ReadAllText(filePath);
				string fragmentShaderCode = File.ReadAllText(filePath2);
				Shader shader = Shader.CreateShader(this, vertexShaderCode, fragmentShaderCode);
				this._loadedShaders.Add(shaderName, shader);
				return shader;
			}
			return this._loadedShaders[shaderName];
		}

		// Token: 0x0600001C RID: 28 RVA: 0x000027C0 File Offset: 0x000009C0
		public void DrawImage(SimpleMaterial material, in ImageDrawObject drawObject)
		{
			Shader shader = this.PrepareRender(material, drawObject.Rectangle);
			this.DrawImageAux(shader, material, drawObject);
			VertexArrayObject.UnBind();
			shader.StopUsing();
		}

		// Token: 0x0600001D RID: 29 RVA: 0x000027F0 File Offset: 0x000009F0
		public void DrawText(TextMaterial material, in TextDrawObject drawObject)
		{
			Shader shader = this.PrepareRender(material, drawObject.Rectangle);
			this.DrawTextAux(shader, material, drawObject);
			VertexArrayObject.UnBind();
			shader.StopUsing();
		}

		// Token: 0x0600001E RID: 30 RVA: 0x00002820 File Offset: 0x00000A20
		public void DrawPolygon(PrimitivePolygonMaterial material, in ImageDrawObject drawObject)
		{
			Shader shader = this.PrepareRender(material, drawObject.Rectangle);
			this.DrawPolygonAux(shader, material, drawObject);
			VertexArrayObject.UnBind();
			shader.StopUsing();
		}

		// Token: 0x0600001F RID: 31 RVA: 0x00002850 File Offset: 0x00000A50
		private Shader PrepareRender(Material material, in Rectangle2D rect)
		{
			Shader orLoadShader = this.GetOrLoadShader(material.GetType().Name);
			Rectangle2D rectangle2D = rect;
			MatrixFrame cachedVisualMatrixFrame = rectangle2D.GetCachedVisualMatrixFrame();
			if (this.IsValidMatrixFrame(cachedVisualMatrixFrame))
			{
				this.ModelMatrix = new Matrix4x4(cachedVisualMatrixFrame.rotation.s.x, cachedVisualMatrixFrame.rotation.s.y, cachedVisualMatrixFrame.rotation.s.z, cachedVisualMatrixFrame.rotation.s.w, cachedVisualMatrixFrame.rotation.f.x, cachedVisualMatrixFrame.rotation.f.y, cachedVisualMatrixFrame.rotation.f.z, cachedVisualMatrixFrame.rotation.f.w, cachedVisualMatrixFrame.rotation.u.x, cachedVisualMatrixFrame.rotation.u.y, cachedVisualMatrixFrame.rotation.u.z, cachedVisualMatrixFrame.rotation.u.w, cachedVisualMatrixFrame.origin.x, cachedVisualMatrixFrame.origin.y, 0f, cachedVisualMatrixFrame.origin.w);
			}
			else
			{
				this.ModelMatrix = new Matrix4x4(250f, 0f, 0f, 0f, 0f, 100f, 0f, 0f, 0f, 0f, 0f, 1f, 50f, 50f, 0f, 1f);
				this._anyInvalidMatricesThisFrame = true;
			}
			orLoadShader.Use();
			Matrix4x4 matrix = this._modelMatrix * this._viewMatrix * this._projectionMatrix;
			orLoadShader.SetMatrix("MVP", matrix);
			return orLoadShader;
		}

		// Token: 0x06000020 RID: 32 RVA: 0x00002A10 File Offset: 0x00000C10
		private bool IsValidMatrixFrame(in MatrixFrame matrixFrame)
		{
			MatrixFrame matrixFrame2 = matrixFrame;
			if (!matrixFrame2.IsZero)
			{
				Vec3 vec = matrixFrame.origin;
				if (vec.IsValidXYZW)
				{
					vec = matrixFrame.rotation.f;
					if (vec.IsValidXYZW)
					{
						vec = matrixFrame.rotation.s;
						if (vec.IsValidXYZW)
						{
							vec = matrixFrame.rotation.u;
							return vec.IsValidXYZW;
						}
					}
				}
			}
			return false;
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002A7C File Offset: 0x00000C7C
		private void DrawImageAux(Shader shader, SimpleMaterial material, in ImageDrawObject drawObject)
		{
			if (material.Texture != null)
			{
				OpenGLTexture texture = material.Texture.PlatformTexture as OpenGLTexture;
				shader.SetTexture("Texture", texture);
			}
			shader.SetBoolean("OverlayEnabled", material.OverlayEnabled);
			if (material.OverlayEnabled)
			{
				OpenGLTexture texture2 = material.OverlayTexture.PlatformTexture as OpenGLTexture;
				shader.SetVector2("StartCoord", material.StartCoordinate);
				shader.SetVector2("Size", material.Size);
				shader.SetTexture("OverlayTexture", texture2);
				shader.SetVector2("OverlayOffset", new Vector2(material.OverlayXOffset, material.OverlayYOffset));
			}
			float value = MathF.Clamp(material.HueFactor / 360f, -0.5f, 0.5f);
			float value2 = MathF.Clamp(material.SaturationFactor / 360f, -0.5f, 0.5f);
			float value3 = MathF.Clamp(material.ValueFactor / 360f, -0.5f, 0.5f);
			shader.SetColor("InputColor", material.Color);
			shader.SetFloat("ColorFactor", material.ColorFactor);
			shader.SetFloat("AlphaFactor", material.AlphaFactor);
			shader.SetFloat("HueFactor", value);
			shader.SetFloat("SaturationFactor", value2);
			shader.SetFloat("ValueFactor", value3);
			this._textureVAO.Bind();
			if (material.CircularMaskingEnabled)
			{
				shader.SetBoolean("CircularMaskingEnabled", true);
				shader.SetVector2("MaskingCenter", material.CircularMaskingCenter);
				shader.SetFloat("MaskingRadius", material.CircularMaskingRadius);
				shader.SetFloat("MaskingSmoothingRadius", material.CircularMaskingSmoothingRadius);
			}
			else
			{
				shader.SetBoolean("CircularMaskingEnabled", false);
			}
			Vector2 vector;
			vector..ctor(drawObject.Uvs.x, drawObject.Uvs.y);
			Vector2 vector2;
			vector2..ctor(drawObject.Uvs.z, drawObject.Uvs.w);
			float[] vertices = new float[] { 0f, 0f, 0f, 1f, 1f, 1f, 1f, 0f };
			uint[] indices = new uint[] { 0U, 1U, 2U, 0U, 2U, 3U };
			float[] uvs = new float[] { vector.X, vector.Y, vector.X, vector2.Y, vector2.X, vector2.Y, vector2.X, vector.Y };
			this._textureVAO.LoadVertexData(vertices);
			this._textureVAO.LoadUVData(uvs);
			this._textureVAO.LoadIndexData(indices);
			this.DrawElements(indices, material.Blending);
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00002D1C File Offset: 0x00000F1C
		private void DrawTextAux(Shader shader, TextMaterial textMaterial, in TextDrawObject drawObject)
		{
			if (textMaterial.Texture != null)
			{
				OpenGLTexture texture = textMaterial.Texture.PlatformTexture as OpenGLTexture;
				shader.SetTexture("Texture", texture);
			}
			shader.SetColor("InputColor", textMaterial.Color);
			shader.SetColor("GlowColor", textMaterial.GlowColor);
			shader.SetColor("OutlineColor", textMaterial.OutlineColor);
			shader.SetFloat("OutlineAmount", textMaterial.OutlineAmount);
			shader.SetFloat("ScaleFactor", 1.5f / textMaterial.ScaleFactor);
			shader.SetFloat("SmoothingConstant", textMaterial.SmoothingConstant);
			shader.SetFloat("GlowRadius", textMaterial.GlowRadius);
			shader.SetFloat("Blur", textMaterial.Blur);
			shader.SetFloat("ShadowOffset", textMaterial.ShadowOffset);
			shader.SetFloat("ShadowAngle", textMaterial.ShadowAngle);
			shader.SetFloat("ColorFactor", textMaterial.ColorFactor);
			shader.SetFloat("AlphaFactor", textMaterial.AlphaFactor);
			this._textureVAO.Bind();
			this._textureVAO.LoadVertexData(drawObject.Text_Vertices);
			this._textureVAO.LoadUVData(drawObject.Text_TextureCoordinates);
			this._textureVAO.LoadIndexData(drawObject.Text_Indices);
			this.DrawElements(drawObject.Text_Indices, textMaterial.Blending);
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00002E70 File Offset: 0x00001070
		private void DrawPolygonAux(Shader shader, PrimitivePolygonMaterial material, in ImageDrawObject drawObject)
		{
			Color color = material.Color;
			shader.SetColor("Color", color);
			new Vector2(drawObject.Uvs.x, drawObject.Uvs.y);
			new Vector2(drawObject.Uvs.z, drawObject.Uvs.w);
			float[] vertices = new float[] { 0f, 0f, 0f, 1f, 1f, 1f, 1f, 0f };
			uint[] indices = new uint[] { 0U, 1U, 2U, 0U, 2U, 3U };
			this._simpleVAO.Bind();
			this._textureVAO.LoadVertexData(vertices);
			this.DrawElements(indices, material.Blending);
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00002F10 File Offset: 0x00001110
		private void DrawElements(uint[] indices, bool blending)
		{
			this.SetBlending(blending);
			using (new AutoPinner(indices))
			{
				Opengl32.DrawElements(BeginMode.Triangles, indices.Length, DataType.UnsignedInt, null);
			}
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00002F58 File Offset: 0x00001158
		internal void Resize(int width, int height)
		{
			if (!this.IsActive)
			{
				this.SetActive();
			}
			this._screenWidth = width;
			this._screenHeight = height;
			Opengl32.Viewport(0, 0, width, height);
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00002F7F File Offset: 0x0000117F
		public void LoadTextureUsing(OpenGLTexture texture, ResourceDepot resourceDepot, string name)
		{
			if (!this.LoadedTextures.ContainsKey(name))
			{
				texture.LoadFromFile(resourceDepot, name);
				this.LoadedTextures.Add(name, texture);
				return;
			}
			texture.CopyFrom(this.LoadedTextures[name]);
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00002FB8 File Offset: 0x000011B8
		public OpenGLTexture LoadTexture(ResourceDepot resourceDepot, string name)
		{
			OpenGLTexture openGLTexture;
			if (this.LoadedTextures.ContainsKey(name))
			{
				openGLTexture = this.LoadedTextures[name];
			}
			else
			{
				openGLTexture = OpenGLTexture.FromFile(resourceDepot, name);
				this.LoadedTextures.Add(name, openGLTexture);
			}
			return openGLTexture;
		}

		// Token: 0x06000028 RID: 40 RVA: 0x00002FFC File Offset: 0x000011FC
		public OpenGLTexture GetTexture(string textureName)
		{
			OpenGLTexture result = null;
			if (this.LoadedTextures.ContainsKey(textureName))
			{
				result = this.LoadedTextures[textureName];
			}
			return result;
		}

		// Token: 0x06000029 RID: 41 RVA: 0x00003027 File Offset: 0x00001227
		public void SetBlending(bool enable)
		{
			this._blendingMode = enable;
			if (this._blendingMode)
			{
				Opengl32.Enable(Target.Blend);
				Opengl32ARB.BlendFuncSeparate(BlendingSourceFactor.SourceAlpha, BlendingDestinationFactor.OneMinusSourceAlpha, BlendingSourceFactor.One, BlendingDestinationFactor.One);
				return;
			}
			Opengl32.Disable(Target.Blend);
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00003063 File Offset: 0x00001263
		public void SetVertexArrayClientState(bool enable)
		{
			if (this._vertexArrayClientState != enable)
			{
				this._vertexArrayClientState = enable;
				if (this._vertexArrayClientState)
				{
					Opengl32.EnableClientState(32884U);
					return;
				}
				Opengl32.DisableClientState(32884U);
			}
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00003092 File Offset: 0x00001292
		public void SetTextureCoordArrayClientState(bool enable)
		{
			if (this._textureCoordArrayClientState != enable)
			{
				this._textureCoordArrayClientState = enable;
				if (this._textureCoordArrayClientState)
				{
					Opengl32.EnableClientState(32888U);
					return;
				}
				Opengl32.DisableClientState(32888U);
			}
		}

		// Token: 0x04000001 RID: 1
		public const int MaxFrameRate = 60;

		// Token: 0x04000002 RID: 2
		public readonly int MaxTimeToRenderOneFrame;

		// Token: 0x04000004 RID: 4
		private IntPtr _handleDeviceContext;

		// Token: 0x04000005 RID: 5
		private IntPtr _handleRenderContext;

		// Token: 0x04000008 RID: 8
		private int[] _scissorParameters = new int[4];

		// Token: 0x04000009 RID: 9
		private Matrix4x4 _projectionMatrix = Matrix4x4.Identity;

		// Token: 0x0400000A RID: 10
		private Matrix4x4 _modelMatrix = Matrix4x4.Identity;

		// Token: 0x0400000B RID: 11
		private Matrix4x4 _viewMatrix = Matrix4x4.Identity;

		// Token: 0x0400000C RID: 12
		private Matrix4x4 _modelViewMatrix = Matrix4x4.Identity;

		// Token: 0x0400000D RID: 13
		private Stopwatch _stopwatch;

		// Token: 0x0400000E RID: 14
		private Dictionary<string, Shader> _loadedShaders;

		// Token: 0x0400000F RID: 15
		private VertexArrayObject _simpleVAO;

		// Token: 0x04000010 RID: 16
		private VertexArrayObject _textureVAO;

		// Token: 0x04000011 RID: 17
		private int _screenWidth;

		// Token: 0x04000012 RID: 18
		private int _screenHeight;

		// Token: 0x04000013 RID: 19
		private int _failedRenderFrames;

		// Token: 0x04000014 RID: 20
		private bool _anyInvalidMatricesThisFrame;

		// Token: 0x04000015 RID: 21
		private ResourceDepot _resourceDepot;

		// Token: 0x04000016 RID: 22
		private bool _blendingMode;

		// Token: 0x04000017 RID: 23
		private bool _vertexArrayClientState;

		// Token: 0x04000018 RID: 24
		private bool _textureCoordArrayClientState;
	}
}
