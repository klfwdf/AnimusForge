using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace TaleWorlds.TwoDimension.Standalone.Native.OpenGL
{
	// Token: 0x0200003C RID: 60
	internal static class Opengl32ARB
	{
		// Token: 0x06000176 RID: 374 RVA: 0x00005616 File Offset: 0x00003816
		public static void LoadContextExtension(IntPtr hdc)
		{
			Opengl32ARB.wglCreateContextAttribs = Opengl32ARB.LoadFunction<Opengl32ARB.wglCreateContextAttribsDelegate>("wglCreateContextAttribsARB");
		}

		// Token: 0x06000177 RID: 375 RVA: 0x00005628 File Offset: 0x00003828
		public static void LoadExtensions(IntPtr hdc)
		{
			if (!Opengl32ARB._extensionsLoaded)
			{
				Opengl32ARB.CreateProgramObject = Opengl32ARB.LoadFunction<Opengl32ARB.CreateProgramObjectDelegate>("glCreateProgram");
				Opengl32ARB.CreateShaderObject = Opengl32ARB.LoadFunction<Opengl32ARB.CreateShaderObjectDelegate>("glCreateShader");
				Opengl32ARB.CompileShader = Opengl32ARB.LoadFunction<Opengl32ARB.CompileShaderDelegate>("glCompileShader");
				Opengl32ARB.ShaderSourceInternal = Opengl32ARB.LoadFunction<Opengl32ARB.ShaderSourceDelegate>("glShaderSource");
				Opengl32ARB.AttachShader = Opengl32ARB.LoadFunction<Opengl32ARB.AttachShaderDelegate>("glAttachShader");
				Opengl32ARB.LinkProgram = Opengl32ARB.LoadFunction<Opengl32ARB.LinkProgramDelegate>("glLinkProgram");
				Opengl32ARB.ActiveTexture = Opengl32ARB.LoadFunction<Opengl32ARB.ActiveTextureDelegate>("glActiveTexture");
				Opengl32ARB.DeleteProgram = Opengl32ARB.LoadFunction<Opengl32ARB.DeleteProgramDelegate>("glDeleteProgram");
				Opengl32ARB.UseProgram = Opengl32ARB.LoadFunction<Opengl32ARB.UseProgramDelegate>("glUseProgram");
				Opengl32ARB.UniformMatrix4fvInternal = Opengl32ARB.LoadFunction<Opengl32ARB.UniformMatrix4fvDelegate>("glUniformMatrix4fv");
				Opengl32ARB.Uniform4f = Opengl32ARB.LoadFunction<Opengl32ARB.Uniform4fDelegate>("glUniform4f");
				Opengl32ARB.Uniform1i = Opengl32ARB.LoadFunction<Opengl32ARB.Uniform1iDelegate>("glUniform1i");
				Opengl32ARB.Uniform1f = Opengl32ARB.LoadFunction<Opengl32ARB.Uniform1fDelegate>("glUniform1f");
				Opengl32ARB.Uniform2f = Opengl32ARB.LoadFunction<Opengl32ARB.Uniform2fDelegate>("glUniform2f");
				Opengl32ARB.GetShaderiv = Opengl32ARB.LoadFunction<Opengl32ARB.GetShaderivDelegate>("glGetShaderiv");
				Opengl32ARB.GetShaderInfoLog = Opengl32ARB.LoadFunction<Opengl32ARB.GetShaderInfoLogDelegate>("glGetShaderInfoLog");
				Opengl32ARB.GetProgramInfoLog = Opengl32ARB.LoadFunction<Opengl32ARB.GetProgramInfoLogDelegate>("glGetProgramInfoLog");
				Opengl32ARB.GetProgramiv = Opengl32ARB.LoadFunction<Opengl32ARB.GetProgramivDelegate>("glGetProgramiv");
				Opengl32ARB.GetUniformLocationInternal = Opengl32ARB.LoadFunction<Opengl32ARB.GetUniformLocationDelegate>("glGetUniformLocation");
				Opengl32ARB.DetachShader = Opengl32ARB.LoadFunction<Opengl32ARB.DetachShaderDelegate>("glDetachShader");
				Opengl32ARB.DeleteShader = Opengl32ARB.LoadFunction<Opengl32ARB.DeleteShaderDelegate>("glDeleteShader");
				Opengl32ARB.GenBuffers = Opengl32ARB.LoadFunction<Opengl32ARB.GenBuffersDelegate>("glGenBuffers");
				Opengl32ARB.BindBuffer = Opengl32ARB.LoadFunction<Opengl32ARB.BindBufferDelegate>("glBindBuffer");
				Opengl32ARB.BufferData = Opengl32ARB.LoadFunction<Opengl32ARB.BufferDataDelegate>("glBufferData");
				Opengl32ARB.BufferSubData = Opengl32ARB.LoadFunction<Opengl32ARB.BufferSubDataDelegate>("glBufferSubData");
				Opengl32ARB.EnableVertexAttribArray = Opengl32ARB.LoadFunction<Opengl32ARB.EnableVertexAttribArrayDelegate>("glEnableVertexAttribArray");
				Opengl32ARB.DisableVertexAttribArray = Opengl32ARB.LoadFunction<Opengl32ARB.DisableVertexAttribArrayDelegate>("glDisableVertexAttribArray");
				Opengl32ARB.VertexAttribPointer = Opengl32ARB.LoadFunction<Opengl32ARB.VertexAttribPointerDelegate>("glVertexAttribPointer");
				Opengl32ARB.GenVertexArrays = Opengl32ARB.LoadFunction<Opengl32ARB.GenVertexArraysDelegate>("glGenVertexArrays");
				Opengl32ARB.BindVertexArray = Opengl32ARB.LoadFunction<Opengl32ARB.BindVertexArrayDelegate>("glBindVertexArray");
				Opengl32ARB.BlendFuncSeparate = Opengl32ARB.LoadFunction<Opengl32ARB.BlendFuncSeparateDelegate>("glBlendFuncSeparate");
				Opengl32ARB._extensionsLoaded = true;
			}
		}

		// Token: 0x06000178 RID: 376 RVA: 0x00005818 File Offset: 0x00003A18
		private static T LoadFunction<T>(string name) where T : class
		{
			IntPtr intPtr = Opengl32.wglGetProcAddress(name);
			if (intPtr != IntPtr.Zero)
			{
				return Marshal.GetDelegateForFunctionPointer(intPtr, typeof(T)) as T;
			}
			throw new OpenGlLoadException("Could not load OpenGL function " + name + ". Please make sure the OpenGL driver, for the gpu that game runs on, is verified and up to date.");
		}

		// Token: 0x06000179 RID: 377 RVA: 0x0000586C File Offset: 0x00003A6C
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
				array4[array4.Length - 2] = 10;
				array4[array4.Length - 1] = 0;
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
			Opengl32ARB.ShaderSourceInternal(shader, array.Length, array6, array3);
			AutoPinner[] array7 = array5;
			for (int k = 0; k < array7.Length; k++)
			{
				array7[k].Dispose();
			}
		}

		// Token: 0x0600017A RID: 378 RVA: 0x00005980 File Offset: 0x00003B80
		public static int GetUniformLocation(int program, string parameter)
		{
			byte[] array = new byte[Encoding.ASCII.GetByteCount(parameter) + 1];
			Encoding.UTF8.GetBytes(parameter, 0, parameter.Length, array, 0);
			return Opengl32ARB.GetUniformLocationInternal(program, array);
		}

		// Token: 0x0600017B RID: 379 RVA: 0x000059C4 File Offset: 0x00003BC4
		public static void UniformMatrix4fv(int location, int count, bool isTranspose, Matrix4x4 matrix)
		{
			float[] matrix2 = new float[]
			{
				matrix.M11, matrix.M12, matrix.M13, matrix.M14, matrix.M21, matrix.M22, matrix.M23, matrix.M24, matrix.M31, matrix.M32,
				matrix.M33, matrix.M34, matrix.M41, matrix.M42, matrix.M43, matrix.M44
			};
			Opengl32ARB.UniformMatrix4fvInternal(location, count, isTranspose ? 1 : 0, matrix2);
		}

		// Token: 0x04000262 RID: 610
		private static bool _extensionsLoaded;

		// Token: 0x04000263 RID: 611
		public static Opengl32ARB.BlendFuncSeparateDelegate BlendFuncSeparate;

		// Token: 0x04000264 RID: 612
		public static Opengl32ARB.ActiveTextureDelegate ActiveTexture;

		// Token: 0x04000265 RID: 613
		public static Opengl32ARB.BindVertexArrayDelegate BindVertexArray;

		// Token: 0x04000266 RID: 614
		public static Opengl32ARB.GenVertexArraysDelegate GenVertexArrays;

		// Token: 0x04000267 RID: 615
		public static Opengl32ARB.VertexAttribPointerDelegate VertexAttribPointer;

		// Token: 0x04000268 RID: 616
		public static Opengl32ARB.EnableVertexAttribArrayDelegate EnableVertexAttribArray;

		// Token: 0x04000269 RID: 617
		public static Opengl32ARB.DisableVertexAttribArrayDelegate DisableVertexAttribArray;

		// Token: 0x0400026A RID: 618
		public static Opengl32ARB.GenBuffersDelegate GenBuffers;

		// Token: 0x0400026B RID: 619
		public static Opengl32ARB.BindBufferDelegate BindBuffer;

		// Token: 0x0400026C RID: 620
		public static Opengl32ARB.BufferDataDelegate BufferData;

		// Token: 0x0400026D RID: 621
		public static Opengl32ARB.BufferSubDataDelegate BufferSubData;

		// Token: 0x0400026E RID: 622
		public static Opengl32ARB.DetachShaderDelegate DetachShader;

		// Token: 0x0400026F RID: 623
		public static Opengl32ARB.DeleteShaderDelegate DeleteShader;

		// Token: 0x04000270 RID: 624
		private static Opengl32ARB.GetUniformLocationDelegate GetUniformLocationInternal;

		// Token: 0x04000271 RID: 625
		public static Opengl32ARB.GetProgramInfoLogDelegate GetProgramInfoLog;

		// Token: 0x04000272 RID: 626
		public static Opengl32ARB.GetShaderInfoLogDelegate GetShaderInfoLog;

		// Token: 0x04000273 RID: 627
		public static Opengl32ARB.GetProgramivDelegate GetProgramiv;

		// Token: 0x04000274 RID: 628
		public static Opengl32ARB.GetShaderivDelegate GetShaderiv;

		// Token: 0x04000275 RID: 629
		private static Opengl32ARB.UniformMatrix4fvDelegate UniformMatrix4fvInternal;

		// Token: 0x04000276 RID: 630
		public static Opengl32ARB.Uniform4fDelegate Uniform4f;

		// Token: 0x04000277 RID: 631
		public static Opengl32ARB.Uniform1iDelegate Uniform1i;

		// Token: 0x04000278 RID: 632
		public static Opengl32ARB.Uniform1fDelegate Uniform1f;

		// Token: 0x04000279 RID: 633
		public static Opengl32ARB.Uniform2fDelegate Uniform2f;

		// Token: 0x0400027A RID: 634
		public static Opengl32ARB.UseProgramDelegate UseProgram;

		// Token: 0x0400027B RID: 635
		public static Opengl32ARB.DeleteProgramDelegate DeleteProgram;

		// Token: 0x0400027C RID: 636
		public static Opengl32ARB.LinkProgramDelegate LinkProgram;

		// Token: 0x0400027D RID: 637
		public static Opengl32ARB.AttachShaderDelegate AttachShader;

		// Token: 0x0400027E RID: 638
		private static Opengl32ARB.ShaderSourceDelegate ShaderSourceInternal;

		// Token: 0x0400027F RID: 639
		public static Opengl32ARB.CompileShaderDelegate CompileShader;

		// Token: 0x04000280 RID: 640
		public static Opengl32ARB.CreateProgramObjectDelegate CreateProgramObject;

		// Token: 0x04000281 RID: 641
		public static Opengl32ARB.CreateShaderObjectDelegate CreateShaderObject;

		// Token: 0x04000282 RID: 642
		public static Opengl32ARB.wglCreateContextAttribsDelegate wglCreateContextAttribs;

		// Token: 0x04000283 RID: 643
		public const int GL_COMPILE_STATUS = 35713;

		// Token: 0x04000284 RID: 644
		public const int GL_LINK_STATUS = 35714;

		// Token: 0x04000285 RID: 645
		public const int GL_INFO_LOG_LENGTH = 35716;

		// Token: 0x04000286 RID: 646
		public const int StaticDraw = 35044;

		// Token: 0x04000287 RID: 647
		public const int DynamicDraw = 35048;

		// Token: 0x0200004E RID: 78
		// (Invoke) Token: 0x060001A4 RID: 420
		public delegate void BlendFuncSeparateDelegate(BlendingSourceFactor srcRGB, BlendingDestinationFactor dstRGB, BlendingSourceFactor srcAlpha, BlendingDestinationFactor dstAlpha);

		// Token: 0x0200004F RID: 79
		// (Invoke) Token: 0x060001A8 RID: 424
		public delegate void ActiveTextureDelegate(TextureUnit textureUnit);

		// Token: 0x02000050 RID: 80
		// (Invoke) Token: 0x060001AC RID: 428
		public delegate void BindVertexArrayDelegate(uint buffer);

		// Token: 0x02000051 RID: 81
		// (Invoke) Token: 0x060001B0 RID: 432
		public delegate void GenVertexArraysDelegate(int size, uint[] buffers);

		// Token: 0x02000052 RID: 82
		// (Invoke) Token: 0x060001B4 RID: 436
		public delegate void VertexAttribPointerDelegate(uint index, int size, DataType type, byte normalized, int stride, IntPtr pointer);

		// Token: 0x02000053 RID: 83
		// (Invoke) Token: 0x060001B8 RID: 440
		public delegate void EnableVertexAttribArrayDelegate(uint index);

		// Token: 0x02000054 RID: 84
		// (Invoke) Token: 0x060001BC RID: 444
		public delegate void DisableVertexAttribArrayDelegate(int index);

		// Token: 0x02000055 RID: 85
		// (Invoke) Token: 0x060001C0 RID: 448
		public delegate void GenBuffersDelegate(int size, uint[] buffers);

		// Token: 0x02000056 RID: 86
		// (Invoke) Token: 0x060001C4 RID: 452
		public delegate void BindBufferDelegate(BufferBindingTarget target, uint buffer);

		// Token: 0x02000057 RID: 87
		// (Invoke) Token: 0x060001C8 RID: 456
		public delegate void BufferDataDelegate(BufferBindingTarget target, int size, IntPtr data, int usage);

		// Token: 0x02000058 RID: 88
		// (Invoke) Token: 0x060001CC RID: 460
		public delegate void BufferSubDataDelegate(BufferBindingTarget target, int offset, int size, IntPtr data);

		// Token: 0x02000059 RID: 89
		// (Invoke) Token: 0x060001D0 RID: 464
		public delegate void DetachShaderDelegate(int program, int shader);

		// Token: 0x0200005A RID: 90
		// (Invoke) Token: 0x060001D4 RID: 468
		public delegate int DeleteShaderDelegate(int shader);

		// Token: 0x0200005B RID: 91
		// (Invoke) Token: 0x060001D8 RID: 472
		private delegate int GetUniformLocationDelegate(int program, byte[] parameter);

		// Token: 0x0200005C RID: 92
		// (Invoke) Token: 0x060001DC RID: 476
		public delegate void GetProgramInfoLogDelegate(int shader, int maxLength, out int length, byte[] output);

		// Token: 0x0200005D RID: 93
		// (Invoke) Token: 0x060001E0 RID: 480
		public delegate void GetShaderInfoLogDelegate(int shader, int maxLength, out int length, byte[] output);

		// Token: 0x0200005E RID: 94
		// (Invoke) Token: 0x060001E4 RID: 484
		public delegate void GetProgramivDelegate(int program, int paremeterName, out int returnValue);

		// Token: 0x0200005F RID: 95
		// (Invoke) Token: 0x060001E8 RID: 488
		public delegate void GetShaderivDelegate(int shader, int paremeterName, out int returnValue);

		// Token: 0x02000060 RID: 96
		// (Invoke) Token: 0x060001EC RID: 492
		private delegate void UniformMatrix4fvDelegate(int location, int count, byte isTranspose, float[] matrix);

		// Token: 0x02000061 RID: 97
		// (Invoke) Token: 0x060001F0 RID: 496
		public delegate void Uniform4fDelegate(int location, float f1, float f2, float f3, float f4);

		// Token: 0x02000062 RID: 98
		// (Invoke) Token: 0x060001F4 RID: 500
		public delegate void Uniform1iDelegate(int location, int i);

		// Token: 0x02000063 RID: 99
		// (Invoke) Token: 0x060001F8 RID: 504
		public delegate void Uniform1fDelegate(int location, float f);

		// Token: 0x02000064 RID: 100
		// (Invoke) Token: 0x060001FC RID: 508
		public delegate void Uniform2fDelegate(int location, float f1, float f2);

		// Token: 0x02000065 RID: 101
		// (Invoke) Token: 0x06000200 RID: 512
		public delegate void UseProgramDelegate(int program);

		// Token: 0x02000066 RID: 102
		// (Invoke) Token: 0x06000204 RID: 516
		public delegate void DeleteProgramDelegate(int program);

		// Token: 0x02000067 RID: 103
		// (Invoke) Token: 0x06000208 RID: 520
		public delegate void LinkProgramDelegate(int program);

		// Token: 0x02000068 RID: 104
		// (Invoke) Token: 0x0600020C RID: 524
		public delegate void AttachShaderDelegate(int program, int shader);

		// Token: 0x02000069 RID: 105
		// (Invoke) Token: 0x06000210 RID: 528
		private delegate void ShaderSourceDelegate(int shader, int count, IntPtr[] shaderSource, int[] length);

		// Token: 0x0200006A RID: 106
		// (Invoke) Token: 0x06000214 RID: 532
		public delegate int CompileShaderDelegate(int shader);

		// Token: 0x0200006B RID: 107
		// (Invoke) Token: 0x06000218 RID: 536
		public delegate int CreateProgramObjectDelegate();

		// Token: 0x0200006C RID: 108
		// (Invoke) Token: 0x0600021C RID: 540
		public delegate int CreateShaderObjectDelegate(ShaderType shaderType);

		// Token: 0x0200006D RID: 109
		// (Invoke) Token: 0x06000220 RID: 544
		public delegate IntPtr wglCreateContextAttribsDelegate(IntPtr hDC, IntPtr hShareContext, int[] attribList);
	}
}
