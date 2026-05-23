using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace TaleWorlds.TwoDimension.Standalone.Native.OpenGL;

internal static class Opengl32ARB
{
	public delegate void BlendFuncSeparateDelegate(BlendingSourceFactor srcRGB, BlendingDestinationFactor dstRGB, BlendingSourceFactor srcAlpha, BlendingDestinationFactor dstAlpha);

	public delegate void ActiveTextureDelegate(TextureUnit textureUnit);

	public delegate void BindVertexArrayDelegate(uint buffer);

	public delegate void GenVertexArraysDelegate(int size, uint[] buffers);

	public delegate void VertexAttribPointerDelegate(uint index, int size, DataType type, byte normalized, int stride, IntPtr pointer);

	public delegate void EnableVertexAttribArrayDelegate(uint index);

	public delegate void DisableVertexAttribArrayDelegate(int index);

	public delegate void GenBuffersDelegate(int size, uint[] buffers);

	public delegate void BindBufferDelegate(BufferBindingTarget target, uint buffer);

	public delegate void BufferDataDelegate(BufferBindingTarget target, int size, IntPtr data, int usage);

	public delegate void BufferSubDataDelegate(BufferBindingTarget target, int offset, int size, IntPtr data);

	public delegate void DetachShaderDelegate(int program, int shader);

	public delegate int DeleteShaderDelegate(int shader);

	private delegate int GetUniformLocationDelegate(int program, byte[] parameter);

	public delegate void GetProgramInfoLogDelegate(int shader, int maxLength, out int length, byte[] output);

	public delegate void GetShaderInfoLogDelegate(int shader, int maxLength, out int length, byte[] output);

	public delegate void GetProgramivDelegate(int program, int paremeterName, out int returnValue);

	public delegate void GetShaderivDelegate(int shader, int paremeterName, out int returnValue);

	private delegate void UniformMatrix4fvDelegate(int location, int count, byte isTranspose, float[] matrix);

	public delegate void Uniform4fDelegate(int location, float f1, float f2, float f3, float f4);

	public delegate void Uniform1iDelegate(int location, int i);

	public delegate void Uniform1fDelegate(int location, float f);

	public delegate void Uniform2fDelegate(int location, float f1, float f2);

	public delegate void UseProgramDelegate(int program);

	public delegate void DeleteProgramDelegate(int program);

	public delegate void LinkProgramDelegate(int program);

	public delegate void AttachShaderDelegate(int program, int shader);

	private delegate void ShaderSourceDelegate(int shader, int count, IntPtr[] shaderSource, int[] length);

	public delegate int CompileShaderDelegate(int shader);

	public delegate int CreateProgramObjectDelegate();

	public delegate int CreateShaderObjectDelegate(ShaderType shaderType);

	public delegate IntPtr wglCreateContextAttribsDelegate(IntPtr hDC, IntPtr hShareContext, int[] attribList);

	private static bool _extensionsLoaded;

	public static BlendFuncSeparateDelegate BlendFuncSeparate;

	public static ActiveTextureDelegate ActiveTexture;

	public static BindVertexArrayDelegate BindVertexArray;

	public static GenVertexArraysDelegate GenVertexArrays;

	public static VertexAttribPointerDelegate VertexAttribPointer;

	public static EnableVertexAttribArrayDelegate EnableVertexAttribArray;

	public static DisableVertexAttribArrayDelegate DisableVertexAttribArray;

	public static GenBuffersDelegate GenBuffers;

	public static BindBufferDelegate BindBuffer;

	public static BufferDataDelegate BufferData;

	public static BufferSubDataDelegate BufferSubData;

	public static DetachShaderDelegate DetachShader;

	public static DeleteShaderDelegate DeleteShader;

	private static GetUniformLocationDelegate GetUniformLocationInternal;

	public static GetProgramInfoLogDelegate GetProgramInfoLog;

	public static GetShaderInfoLogDelegate GetShaderInfoLog;

	public static GetProgramivDelegate GetProgramiv;

	public static GetShaderivDelegate GetShaderiv;

	private static UniformMatrix4fvDelegate UniformMatrix4fvInternal;

	public static Uniform4fDelegate Uniform4f;

	public static Uniform1iDelegate Uniform1i;

	public static Uniform1fDelegate Uniform1f;

	public static Uniform2fDelegate Uniform2f;

	public static UseProgramDelegate UseProgram;

	public static DeleteProgramDelegate DeleteProgram;

	public static LinkProgramDelegate LinkProgram;

	public static AttachShaderDelegate AttachShader;

	private static ShaderSourceDelegate ShaderSourceInternal;

	public static CompileShaderDelegate CompileShader;

	public static CreateProgramObjectDelegate CreateProgramObject;

	public static CreateShaderObjectDelegate CreateShaderObject;

	public static wglCreateContextAttribsDelegate wglCreateContextAttribs;

	public const int GL_COMPILE_STATUS = 35713;

	public const int GL_LINK_STATUS = 35714;

	public const int GL_INFO_LOG_LENGTH = 35716;

	public const int StaticDraw = 35044;

	public const int DynamicDraw = 35048;

	public static void LoadContextExtension(IntPtr hdc)
	{
		wglCreateContextAttribs = LoadFunction<wglCreateContextAttribsDelegate>("wglCreateContextAttribsARB");
	}

	public static void LoadExtensions(IntPtr hdc)
	{
		if (!_extensionsLoaded)
		{
			CreateProgramObject = LoadFunction<CreateProgramObjectDelegate>("glCreateProgram");
			CreateShaderObject = LoadFunction<CreateShaderObjectDelegate>("glCreateShader");
			CompileShader = LoadFunction<CompileShaderDelegate>("glCompileShader");
			ShaderSourceInternal = LoadFunction<ShaderSourceDelegate>("glShaderSource");
			AttachShader = LoadFunction<AttachShaderDelegate>("glAttachShader");
			LinkProgram = LoadFunction<LinkProgramDelegate>("glLinkProgram");
			ActiveTexture = LoadFunction<ActiveTextureDelegate>("glActiveTexture");
			DeleteProgram = LoadFunction<DeleteProgramDelegate>("glDeleteProgram");
			UseProgram = LoadFunction<UseProgramDelegate>("glUseProgram");
			UniformMatrix4fvInternal = LoadFunction<UniformMatrix4fvDelegate>("glUniformMatrix4fv");
			Uniform4f = LoadFunction<Uniform4fDelegate>("glUniform4f");
			Uniform1i = LoadFunction<Uniform1iDelegate>("glUniform1i");
			Uniform1f = LoadFunction<Uniform1fDelegate>("glUniform1f");
			Uniform2f = LoadFunction<Uniform2fDelegate>("glUniform2f");
			GetShaderiv = LoadFunction<GetShaderivDelegate>("glGetShaderiv");
			GetShaderInfoLog = LoadFunction<GetShaderInfoLogDelegate>("glGetShaderInfoLog");
			GetProgramInfoLog = LoadFunction<GetProgramInfoLogDelegate>("glGetProgramInfoLog");
			GetProgramiv = LoadFunction<GetProgramivDelegate>("glGetProgramiv");
			GetUniformLocationInternal = LoadFunction<GetUniformLocationDelegate>("glGetUniformLocation");
			DetachShader = LoadFunction<DetachShaderDelegate>("glDetachShader");
			DeleteShader = LoadFunction<DeleteShaderDelegate>("glDeleteShader");
			GenBuffers = LoadFunction<GenBuffersDelegate>("glGenBuffers");
			BindBuffer = LoadFunction<BindBufferDelegate>("glBindBuffer");
			BufferData = LoadFunction<BufferDataDelegate>("glBufferData");
			BufferSubData = LoadFunction<BufferSubDataDelegate>("glBufferSubData");
			EnableVertexAttribArray = LoadFunction<EnableVertexAttribArrayDelegate>("glEnableVertexAttribArray");
			DisableVertexAttribArray = LoadFunction<DisableVertexAttribArrayDelegate>("glDisableVertexAttribArray");
			VertexAttribPointer = LoadFunction<VertexAttribPointerDelegate>("glVertexAttribPointer");
			GenVertexArrays = LoadFunction<GenVertexArraysDelegate>("glGenVertexArrays");
			BindVertexArray = LoadFunction<BindVertexArrayDelegate>("glBindVertexArray");
			BlendFuncSeparate = LoadFunction<BlendFuncSeparateDelegate>("glBlendFuncSeparate");
			_extensionsLoaded = true;
		}
	}

	private static T LoadFunction<T>(string name) where T : class
	{
		IntPtr intPtr = Opengl32.wglGetProcAddress(name);
		if (intPtr != IntPtr.Zero)
		{
			return Marshal.GetDelegateForFunctionPointer(intPtr, typeof(T)) as T;
		}
		StandaloneApplicationUtility.TerminateWithMessageBox("Graphics driver error", "Could not load OpenGL function: " + name + ". Please make sure the OpenGL driver for the gpu that game runs on is verified and up to date.");
		return null;
	}

	public static void ShaderSource(int shader, string shaderSource)
	{
		string[] array = shaderSource.Split(Environment.NewLine.ToCharArray());
		byte[][] array2 = new byte[array.Length][];
		int[] array3 = new int[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i];
			byte[] array4 = new byte[Encoding.UTF8.GetByteCount(text) + 2];
			Encoding.UTF8.GetBytes(text, 0, text.Length, array4, 0);
			array4[^2] = 10;
			array4[^1] = 0;
			array2[i] = array4;
			array3[i] = array4.Length - 1;
		}
		AutoPinner[] array5 = new AutoPinner[array.Length];
		IntPtr[] array6 = new IntPtr[array.Length];
		for (int j = 0; j < array.Length; j++)
		{
			AutoPinner autoPinner = new AutoPinner(array2[j]);
			IntPtr intPtr = autoPinner;
			array6[j] = intPtr;
			array5[j] = autoPinner;
		}
		ShaderSourceInternal(shader, array.Length, array6, array3);
		AutoPinner[] array7 = array5;
		for (int k = 0; k < array7.Length; k++)
		{
			array7[k].Dispose();
		}
	}

	public static int GetUniformLocation(int program, string parameter)
	{
		byte[] array = new byte[Encoding.ASCII.GetByteCount(parameter) + 1];
		Encoding.UTF8.GetBytes(parameter, 0, parameter.Length, array, 0);
		return GetUniformLocationInternal(program, array);
	}

	public static void UniformMatrix4fv(int location, int count, bool isTranspose, in Matrix4x4 matrix)
	{
		float[] matrix2 = new float[16]
		{
			matrix.M11, matrix.M12, matrix.M13, matrix.M14, matrix.M21, matrix.M22, matrix.M23, matrix.M24, matrix.M31, matrix.M32,
			matrix.M33, matrix.M34, matrix.M41, matrix.M42, matrix.M43, matrix.M44
		};
		UniformMatrix4fvInternal(location, count, (byte)(isTranspose ? 1u : 0u), matrix2);
	}
}
