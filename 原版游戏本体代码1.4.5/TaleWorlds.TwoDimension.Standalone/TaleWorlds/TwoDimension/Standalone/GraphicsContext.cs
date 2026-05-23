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

namespace TaleWorlds.TwoDimension.Standalone;

public class GraphicsContext
{
	public const int MaxFrameRate = 60;

	public readonly int MaxTimeToRenderOneFrame;

	private IntPtr _handleDeviceContext;

	private IntPtr _handleRenderContext;

	private int[] _scissorParameters = new int[4];

	private MatrixFrame _modelMatrix = MatrixFrame.Identity.Filled();

	private MatrixFrame _viewMatrix = MatrixFrame.Identity.Filled();

	private MatrixFrame _projectionMatrix = MatrixFrame.Identity.Filled();

	private Stopwatch _stopwatch;

	private Dictionary<string, Shader> _loadedShaders;

	private VertexArrayObject _simpleVAO;

	private VertexArrayObject _textureVAO;

	private int _screenWidth;

	private int _screenHeight;

	private int _failedRenderFrames;

	private bool _anyInvalidMatricesThisFrame;

	private bool _forceContextReactivation;

	private ResourceDepot _resourceDepot;

	private bool _blendingMode;

	private bool _vertexArrayClientState;

	private bool _textureCoordArrayClientState;

	internal WindowsForm Control { get; set; }

	public static GraphicsContext Active { get; private set; }

	internal Dictionary<string, OpenGLTexture> LoadedTextures { get; private set; }

	public MatrixFrame ProjectionMatrix
	{
		get
		{
			return _projectionMatrix;
		}
		set
		{
			_projectionMatrix = value;
		}
	}

	public MatrixFrame ViewMatrix
	{
		get
		{
			return _viewMatrix;
		}
		set
		{
			_viewMatrix = value;
		}
	}

	public MatrixFrame ModelMatrix
	{
		get
		{
			return _modelMatrix;
		}
		set
		{
			_modelMatrix = value;
		}
	}

	public bool IsActive => Active == this;

	public GraphicsContext()
	{
		LoadedTextures = new Dictionary<string, OpenGLTexture>();
		_loadedShaders = new Dictionary<string, Shader>();
		_stopwatch = new Stopwatch();
		MaxTimeToRenderOneFrame = 16;
	}

	public void CreateContext(ResourceDepot resourceDepot)
	{
		_resourceDepot = resourceDepot;
		_handleDeviceContext = User32.GetDC(Control.Handle);
		if (_handleDeviceContext == IntPtr.Zero)
		{
			TaleWorlds.Library.Debug.Print("Can't get device context");
		}
		if (!Opengl32.wglMakeCurrent(_handleDeviceContext, IntPtr.Zero))
		{
			TaleWorlds.Library.Debug.Print("Can't reset context");
		}
		PixelFormatDescriptor ppfd = default(PixelFormatDescriptor);
		Marshal.SizeOf(typeof(PixelFormatDescriptor));
		ppfd.nSize = (ushort)Marshal.SizeOf(typeof(PixelFormatDescriptor));
		ppfd.nVersion = 1;
		ppfd.dwFlags = 37u;
		ppfd.iPixelType = 0;
		ppfd.cColorBits = 32;
		ppfd.cRedBits = 0;
		ppfd.cRedShift = 0;
		ppfd.cGreenBits = 0;
		ppfd.cGreenShift = 0;
		ppfd.cBlueBits = 0;
		ppfd.cBlueShift = 0;
		ppfd.cAlphaBits = 8;
		ppfd.cAlphaShift = 0;
		ppfd.cAccumBits = 0;
		ppfd.cAccumRedBits = 0;
		ppfd.cAccumGreenBits = 0;
		ppfd.cAccumBlueBits = 0;
		ppfd.cAccumAlphaBits = 0;
		ppfd.cDepthBits = 24;
		ppfd.cStencilBits = 8;
		ppfd.cAuxBuffers = 0;
		ppfd.iLayerType = 0;
		ppfd.bReserved = 0;
		ppfd.dwLayerMask = 0u;
		ppfd.dwVisibleMask = 0u;
		ppfd.dwDamageMask = 0u;
		int iPixelFormat = Gdi32.ChoosePixelFormat(_handleDeviceContext, ref ppfd);
		if (!Gdi32.SetPixelFormat(_handleDeviceContext, iPixelFormat, ref ppfd))
		{
			TaleWorlds.Library.Debug.Print("can't set pixel format");
		}
		_handleRenderContext = Opengl32.wglCreateContext(_handleDeviceContext);
		if (_handleRenderContext == IntPtr.Zero)
		{
			StandaloneApplicationUtility.TerminateWithMessageBox("Graphics driver error", "Could not create default OpenGL context.");
		}
		SetActive();
		string value = Opengl32.GetString(7938u);
		string value2 = Opengl32.GetString(7936u);
		string value3 = Opengl32.GetString(7937u);
		Watchdog.LogProperty("crash_tags.txt", "Runtime", "DefaultContextVersionOpenGL", value);
		Watchdog.LogProperty("crash_tags.txt", "Runtime", "DefaultContextVendorOpenGL", value2);
		Watchdog.LogProperty("crash_tags.txt", "Runtime", "DefaultContextRendererOpenGL", value3);
		IntPtr handleRenderContext = _handleRenderContext;
		_handleRenderContext = IntPtr.Zero;
		Active = null;
		Opengl32ARB.LoadContextExtension(_handleDeviceContext);
		int[] array = new int[10];
		int num = 0;
		array[num++] = 8337;
		array[num++] = 3;
		array[num++] = 8338;
		array[num++] = 3;
		array[num++] = 37158;
		array[num++] = 1;
		array[num++] = 0;
		_handleRenderContext = Opengl32ARB.wglCreateContextAttribs(_handleDeviceContext, IntPtr.Zero, array);
		if (_handleRenderContext == IntPtr.Zero)
		{
			StandaloneApplicationUtility.TerminateWithMessageBox("Graphics driver error", "Could not create OpenGL context.");
		}
		SetActive();
		string value4 = Opengl32.GetString(7938u);
		string value5 = Opengl32.GetString(7936u);
		string value6 = Opengl32.GetString(7937u);
		Watchdog.LogProperty("crash_tags.txt", "Runtime", "ContextVersionOpenGL", value4);
		Watchdog.LogProperty("crash_tags.txt", "Runtime", "ContextVendorOpenGL", value5);
		Watchdog.LogProperty("crash_tags.txt", "Runtime", "ContextRendererOpenGL", value6);
		Opengl32ARB.LoadExtensions(_handleDeviceContext);
		Opengl32.wglDeleteContext(handleRenderContext);
		Opengl32.ShadeModel(ShadingModel.Smooth);
		Opengl32.ClearColor(0f, 0f, 0f, 0f);
		Opengl32.ClearDepth(1.0);
		Opengl32.Disable(Target.DepthTest);
		Opengl32.Hint(3152u, 4354u);
		ProjectionMatrix = MatrixFrame.Identity.Filled();
		ViewMatrix = MatrixFrame.Identity.Filled();
		ModelMatrix = MatrixFrame.Identity.Filled();
		_simpleVAO = VertexArrayObject.Create();
		_textureVAO = VertexArrayObject.CreateWithUVBuffer();
	}

	public void SetActive()
	{
		if (Active != this)
		{
			if (Opengl32.wglMakeCurrent(_handleDeviceContext, _handleRenderContext))
			{
				Active = this;
			}
			else
			{
				TaleWorlds.Library.Debug.Print("Can't activate context");
			}
		}
	}

	public void BeginFrame(int width, int height)
	{
		_anyInvalidMatricesThisFrame = false;
		_stopwatch.Start();
		if (_forceContextReactivation)
		{
			TaleWorlds.Library.Debug.Print("[LAUNCHER]: Display or DPI change detected, reactivating GL context.");
			_forceContextReactivation = false;
			Active = null;
			SetActive();
		}
		IntPtr intPtr = Opengl32.wglGetCurrentContext();
		if (intPtr == IntPtr.Zero || intPtr != _handleRenderContext)
		{
			Active = null;
			SetActive();
			intPtr = Opengl32.wglGetCurrentContext();
			if (intPtr == IntPtr.Zero)
			{
				_anyInvalidMatricesThisFrame = true;
				return;
			}
		}
		Resize(width, height);
		Opengl32.Clear(AttribueMask.ColorBufferBit);
		Opengl32.ClearDepth(1.0);
		Opengl32.Disable(Target.DepthTest);
		Opengl32.Disable(Target.SCISSOR_TEST);
		Opengl32.Disable(Target.STENCIL_TEST);
		Opengl32.Disable(Target.Blend);
	}

	public void SwapBuffers()
	{
		int num = (int)_stopwatch.ElapsedMilliseconds;
		int num2 = 0;
		if (MaxTimeToRenderOneFrame > num)
		{
			num2 = MaxTimeToRenderOneFrame - num;
		}
		if (num2 > 0)
		{
			Thread.Sleep(num2);
		}
		if (!Gdi32.SwapBuffers(_handleDeviceContext))
		{
			_forceContextReactivation = true;
		}
		_stopwatch.Restart();
		if (_anyInvalidMatricesThisFrame)
		{
			_failedRenderFrames++;
		}
		else
		{
			_failedRenderFrames = 0;
		}
		if (_failedRenderFrames >= 100)
		{
			TaleWorlds.Library.Debug.ShowMessageBox("Launcher render error", "ERROR", 4u);
			throw new Exception("[Launcher]: More than 100 consecutive frames had a render fail");
		}
	}

	public void RequestContextReactivation()
	{
		_forceContextReactivation = true;
	}

	public void DestroyContext()
	{
		Opengl32.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
		Opengl32.wglDeleteContext(_handleRenderContext);
		User32.ReleaseDC(Control.Handle, _handleDeviceContext);
	}

	public void SetScissor(ScissorTestInfo scissorTestInfo)
	{
		Opengl32.GetInteger(Target.VIEWPORT, _scissorParameters);
		SimpleRectangle simpleRectangle = scissorTestInfo.GetSimpleRectangle();
		Opengl32.Scissor((int)simpleRectangle.X, _scissorParameters[3] - (int)simpleRectangle.Height - (int)simpleRectangle.Y, (int)simpleRectangle.Width, (int)simpleRectangle.Height);
		Opengl32.Enable(Target.SCISSOR_TEST);
	}

	public void ResetScissor()
	{
		Opengl32.Disable(Target.SCISSOR_TEST);
	}

	public Shader GetOrLoadShader(string shaderName)
	{
		if (!_loadedShaders.ContainsKey(shaderName))
		{
			try
			{
				string filePath = _resourceDepot.GetFilePath(shaderName + ".vert");
				string filePath2 = _resourceDepot.GetFilePath(shaderName + ".frag");
				string vertexShaderCode = File.ReadAllText(filePath);
				string fragmentShaderCode = File.ReadAllText(filePath2);
				Shader shader = Shader.CreateShader(this, vertexShaderCode, fragmentShaderCode);
				_loadedShaders.Add(shaderName, shader);
				return shader;
			}
			catch (Exception)
			{
				_loadedShaders.Add(shaderName, null);
				return null;
			}
		}
		return _loadedShaders[shaderName];
	}

	public void DrawImage(SimpleMaterial material, in ImageDrawObject drawObject)
	{
		Shader shader = PrepareRender(material, in drawObject.Rectangle);
		if (shader != null)
		{
			DrawImageAux(shader, material, in drawObject);
			VertexArrayObject.UnBind();
			shader.StopUsing();
		}
	}

	public void DrawText(TextMaterial material, in TextDrawObject drawObject)
	{
		Shader shader = PrepareRender(material, in drawObject.Rectangle);
		if (shader != null)
		{
			DrawTextAux(shader, material, in drawObject);
			VertexArrayObject.UnBind();
			shader.StopUsing();
		}
	}

	public void DrawPolygon(PrimitivePolygonMaterial material, in ImageDrawObject drawObject)
	{
		Shader shader = PrepareRender(material, in drawObject.Rectangle);
		if (shader != null)
		{
			DrawPolygonAux(shader, material, in drawObject);
			VertexArrayObject.UnBind();
			shader.StopUsing();
		}
	}

	private Shader PrepareRender(Material material, in Rectangle2D rect)
	{
		Shader orLoadShader = GetOrLoadShader(material.GetType().Name);
		if (orLoadShader == null)
		{
			_anyInvalidMatricesThisFrame = true;
			return null;
		}
		if (_screenWidth <= 0 || _screenHeight <= 0)
		{
			_anyInvalidMatricesThisFrame = true;
			return null;
		}
		MatrixFrame cachedVisualMatrixFrame = rect.GetCachedVisualMatrixFrame();
		if (cachedVisualMatrixFrame.AreAllComponentsValid() && !cachedVisualMatrixFrame.IsZero)
		{
			ModelMatrix = cachedVisualMatrixFrame;
		}
		else
		{
			ModelMatrix = ValidateModelMatrix(cachedVisualMatrixFrame);
			_anyInvalidMatricesThisFrame = true;
		}
		MatrixFrame matrixFrame = ValidateModelMatrix(_modelMatrix);
		MatrixFrame matrixFrame2 = ValidateViewMatrix(in _viewMatrix);
		MatrixFrame matrixFrame3 = ValidateProjectionMatrix(in _projectionMatrix);
		orLoadShader.Use();
		if (Opengl32.GetError() != 0)
		{
			_anyInvalidMatricesThisFrame = true;
			orLoadShader.StopUsing();
			return null;
		}
		orLoadShader.SetMatrix("MVP", matrixFrame.ToMatrix4x4() * matrixFrame2.ToMatrix4x4() * matrixFrame3.ToMatrix4x4());
		return orLoadShader;
	}

	private static MatrixFrame ValidateModelMatrix(MatrixFrame modelMatrix)
	{
		if (!modelMatrix.origin.IsValidXYZW)
		{
			modelMatrix.origin = new Vec3(0f, 0f, 0f, 0f);
		}
		if (!modelMatrix.rotation.s.IsValidXYZW)
		{
			modelMatrix.rotation.s = new Vec3(100f, 0f, 0f, 0f);
		}
		if (!modelMatrix.rotation.f.IsValidXYZW)
		{
			modelMatrix.rotation.f = new Vec3(0f, 100f, 0f, 0f);
		}
		if (!modelMatrix.rotation.u.IsValidXYZW)
		{
			modelMatrix.rotation.u = new Vec3(0f, 0f, 1f, 0f);
		}
		modelMatrix.Fill();
		return modelMatrix;
	}

	private static MatrixFrame ValidateViewMatrix(in MatrixFrame viewMatrix)
	{
		if (viewMatrix.AreAllComponentsValid())
		{
			return viewMatrix;
		}
		return MatrixFrame.CreateLookAt(in Vec3.Up, in Vec3.Zero, in Vec3.Forward);
	}

	private static MatrixFrame ValidateProjectionMatrix(in MatrixFrame projectionMatrix)
	{
		if (projectionMatrix.AreAllComponentsValid())
		{
			return projectionMatrix;
		}
		return MatrixExtensions.CreateOrthographicOffCenter(0f, 900f, 600f, 0f, 0f, 1f);
	}

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
		float value = TaleWorlds.Library.MathF.Clamp(material.HueFactor / 360f, -0.5f, 0.5f);
		float value2 = TaleWorlds.Library.MathF.Clamp(material.SaturationFactor / 360f, -0.5f, 0.5f);
		float value3 = TaleWorlds.Library.MathF.Clamp(material.ValueFactor / 360f, -0.5f, 0.5f);
		shader.SetColor("InputColor", material.Color);
		shader.SetFloat("ColorFactor", material.ColorFactor);
		shader.SetFloat("AlphaFactor", material.AlphaFactor);
		shader.SetFloat("HueFactor", value);
		shader.SetFloat("SaturationFactor", value2);
		shader.SetFloat("ValueFactor", value3);
		_textureVAO.Bind();
		if (material.CircularMaskingEnabled)
		{
			shader.SetBoolean("CircularMaskingEnabled", value: true);
			shader.SetVector2("MaskingCenter", material.CircularMaskingCenter);
			shader.SetFloat("MaskingRadius", material.CircularMaskingRadius);
			shader.SetFloat("MaskingSmoothingRadius", material.CircularMaskingSmoothingRadius);
		}
		else
		{
			shader.SetBoolean("CircularMaskingEnabled", value: false);
		}
		Vector2 vector = new Vector2(drawObject.Uvs.x, drawObject.Uvs.y);
		Vector2 vector2 = new Vector2(drawObject.Uvs.z, drawObject.Uvs.w);
		float[] vertices = new float[8] { 0f, 0f, 0f, 1f, 1f, 1f, 1f, 0f };
		uint[] indices = new uint[6] { 0u, 1u, 2u, 0u, 2u, 3u };
		float[] uvs = new float[8] { vector.X, vector.Y, vector.X, vector2.Y, vector2.X, vector2.Y, vector2.X, vector.Y };
		_textureVAO.LoadVertexData(vertices);
		_textureVAO.LoadUVData(uvs);
		_textureVAO.LoadIndexData(indices);
		DrawElements(indices, material.Blending);
	}

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
		_textureVAO.Bind();
		_textureVAO.LoadVertexData(drawObject.Text_Vertices);
		_textureVAO.LoadUVData(drawObject.Text_TextureCoordinates);
		_textureVAO.LoadIndexData(drawObject.Text_Indices);
		DrawElements(drawObject.Text_Indices, textMaterial.Blending);
	}

	private void DrawPolygonAux(Shader shader, PrimitivePolygonMaterial material, in ImageDrawObject drawObject)
	{
		Color color = material.Color;
		shader.SetColor("Color", color);
		new Vector2(drawObject.Uvs.x, drawObject.Uvs.y);
		new Vector2(drawObject.Uvs.z, drawObject.Uvs.w);
		float[] vertices = new float[8] { 0f, 0f, 0f, 1f, 1f, 1f, 1f, 0f };
		uint[] indices = new uint[6] { 0u, 1u, 2u, 0u, 2u, 3u };
		_simpleVAO.Bind();
		_textureVAO.LoadVertexData(vertices);
		DrawElements(indices, material.Blending);
	}

	private void DrawElements(uint[] indices, bool blending)
	{
		SetBlending(blending);
		using (new AutoPinner(indices))
		{
			Opengl32.DrawElements(BeginMode.Triangles, indices.Length, DataType.UnsignedInt, null);
		}
	}

	internal void Resize(int width, int height)
	{
		if (!IsActive)
		{
			SetActive();
		}
		_screenWidth = width;
		_screenHeight = height;
		Opengl32.Viewport(0, 0, width, height);
	}

	public void LoadTextureUsing(OpenGLTexture texture, ResourceDepot resourceDepot, string name)
	{
		if (!LoadedTextures.ContainsKey(name))
		{
			texture.LoadFromFile(resourceDepot, name);
			LoadedTextures.Add(name, texture);
		}
		else
		{
			texture.CopyFrom(LoadedTextures[name]);
		}
	}

	public OpenGLTexture LoadTexture(ResourceDepot resourceDepot, string name)
	{
		OpenGLTexture openGLTexture = null;
		if (LoadedTextures.ContainsKey(name))
		{
			openGLTexture = LoadedTextures[name];
		}
		else
		{
			openGLTexture = OpenGLTexture.FromFile(resourceDepot, name);
			LoadedTextures.Add(name, openGLTexture);
		}
		return openGLTexture;
	}

	public OpenGLTexture GetTexture(string textureName)
	{
		OpenGLTexture result = null;
		if (LoadedTextures.ContainsKey(textureName))
		{
			result = LoadedTextures[textureName];
		}
		return result;
	}

	public void SetBlending(bool enable)
	{
		_blendingMode = enable;
		if (_blendingMode)
		{
			Opengl32.Enable(Target.Blend);
			Opengl32ARB.BlendFuncSeparate(BlendingSourceFactor.SourceAlpha, BlendingDestinationFactor.OneMinusSourceAlpha, BlendingSourceFactor.One, BlendingDestinationFactor.One);
		}
		else
		{
			Opengl32.Disable(Target.Blend);
		}
	}

	public void SetVertexArrayClientState(bool enable)
	{
		if (_vertexArrayClientState != enable)
		{
			_vertexArrayClientState = enable;
			if (_vertexArrayClientState)
			{
				Opengl32.EnableClientState(32884u);
			}
			else
			{
				Opengl32.DisableClientState(32884u);
			}
		}
	}

	public void SetTextureCoordArrayClientState(bool enable)
	{
		if (_textureCoordArrayClientState != enable)
		{
			_textureCoordArrayClientState = enable;
			if (_textureCoordArrayClientState)
			{
				Opengl32.EnableClientState(32888u);
			}
			else
			{
				Opengl32.DisableClientState(32888u);
			}
		}
	}
}
