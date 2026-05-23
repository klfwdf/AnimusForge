using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace TaleWorlds.TwoDimension.Standalone.Native.OpenGL;

[SuppressUnmanagedCodeSecurity]
internal static class Opengl32
{
	public const uint GL_VENDOR = 7936u;

	public const uint GL_RENDERER = 7937u;

	public const uint GL_VERSION = 7938u;

	public const uint GL_EXTENSIONS = 7939u;

	[DllImport("Opengl32.dll", EntryPoint = "glDrawArrays")]
	public static extern void DrawArrays(BeginMode mode, int first, int count);

	[DllImport("Opengl32.dll", EntryPoint = "glGetIntegerv")]
	public static extern void GetInteger(Target target, int[] parameters);

	[DllImport("Opengl32.dll", EntryPoint = "glScissor")]
	public static extern void Scissor(int x, int y, int width, int height);

	[DllImport("Opengl32.dll", EntryPoint = "glGetString")]
	private static extern IntPtr GetStringInner(uint name);

	public static string GetString(uint name)
	{
		new ASCIIEncoding();
		return Marshal.PtrToStringAnsi(GetStringInner(name));
	}

	[DllImport("Opengl32.dll", EntryPoint = "glPixelStorei")]
	public static extern void PixelStore(Target pname, int param);

	[DllImport("Opengl32.dll", EntryPoint = "glPixelZoom")]
	public static extern void PixelZoom(float xfactor, float yfactor);

	[DllImport("Opengl32.dll", EntryPoint = "glReadPixels")]
	public static extern void ReadPixels(int x, int y, int width, int height, PixelFormat format, DataType type, byte[] pixels);

	[DllImport("Opengl32.dll", EntryPoint = "glCopyPixels")]
	public static extern void CopyPixels(int x, int y, int width, int height, PixelFormat type);

	[DllImport("Opengl32.dll", EntryPoint = "glDeleteTextures")]
	public static extern void DeleteTextures(int n, int[] textures);

	[DllImport("Opengl32.dll", EntryPoint = "glBlendFunc")]
	public static extern void BlendFunc(BlendingSourceFactor sfactor, BlendingDestinationFactor dfactor);

	[DllImport("Opengl32.dll", EntryPoint = "glViewport")]
	public static extern void Viewport(int x, int y, int width, int height);

	[DllImport("Opengl32.dll", EntryPoint = "glMultMatrixf")]
	public static extern void MultMatrix(float[] matrix);

	[DllImport("Opengl32.dll", EntryPoint = "glLoadMatrixf")]
	public static extern void LoadMatrix(float[] matrix);

	[DllImport("Opengl32.dll", EntryPoint = "glLoadMatrixf")]
	public static extern void LoadMatrix(ref Matrix4x4 matrix);

	[DllImport("Opengl32.dll", EntryPoint = "glColorMaterial")]
	public static extern void ColorMaterial(uint face, uint mode);

	[DllImport("Opengl32.dll", EntryPoint = "glTexCoordPointer")]
	public static extern void TexCoordPointer(int size, DataType type, int stride, float[] vertexData);

	[DllImport("Opengl32.dll", EntryPoint = "glVertexPointer")]
	public static extern void VertexPointer(int size, DataType type, int stride, float[] vertexData);

	[DllImport("Opengl32.dll", EntryPoint = "glNormalPointer")]
	public static extern void NormalPointer(DataType type, int stride, float[] normalData);

	[DllImport("Opengl32.dll", EntryPoint = "glDrawElements")]
	public static extern void DrawElements(BeginMode mode, int count, DataType type, uint[] indices);

	[DllImport("Opengl32.dll", EntryPoint = "glDisableClientState")]
	public static extern void DisableClientState(uint array);

	[DllImport("Opengl32.dll", EntryPoint = "glEnableClientState")]
	public static extern void EnableClientState(uint array);

	[DllImport("Opengl32.dll", EntryPoint = "glTranslatef")]
	public static extern void Translate(float x, float y, float z);

	[DllImport("Opengl32.dll", EntryPoint = "glLightfv")]
	public static extern void Lightfv(uint light, uint pname, float[] parameters);

	[DllImport("Opengl32.dll", EntryPoint = "glHint")]
	public static extern void Hint(uint target, uint mode);

	[DllImport("Opengl32.dll", EntryPoint = "glMatrixMode")]
	public static extern void MatrixMode(MatrixMode mode);

	[DllImport("Opengl32.dll", EntryPoint = "glDepthFunc")]
	public static extern void DepthFunc(uint mode);

	[DllImport("Opengl32.dll", EntryPoint = "glShadeModel")]
	public static extern void ShadeModel(ShadingModel func);

	[DllImport("Opengl32.dll", EntryPoint = "glClearDepth")]
	public static extern void ClearDepth(double depth);

	[DllImport("Opengl32.dll", EntryPoint = "glPopMatrix")]
	public static extern void PopMatrix();

	[DllImport("Opengl32.dll", EntryPoint = "glPushMatrix")]
	public static extern void PushMatrix();

	[DllImport("Opengl32.dll", EntryPoint = "glRotated")]
	public static extern void Rotate(double angle, double x, double y, double z);

	[DllImport("Opengl32.dll", EntryPoint = "glRotatef")]
	public static extern void Rotate(float angle, float x, float y, float z);

	[DllImport("Opengl32.dll", EntryPoint = "glScaled")]
	public static extern void Scale(double x, double y, double z);

	[DllImport("Opengl32.dll", EntryPoint = "glScalef")]
	public static extern void Scale(float x, float y, float z);

	[DllImport("Opengl32.dll", EntryPoint = "glLoadIdentity")]
	public static extern void LoadIdentity();

	[DllImport("Opengl32.dll", EntryPoint = "glClear")]
	public static extern void Clear(AttribueMask mask);

	[DllImport("Opengl32.dll", EntryPoint = "glBegin")]
	public static extern void Begin(BeginMode mode);

	[DllImport("Opengl32.dll", EntryPoint = "glEnd")]
	public static extern void End();

	[DllImport("Opengl32.dll", EntryPoint = "glVertex2i")]
	public static extern void Vertex(int x, int y);

	[DllImport("Opengl32.dll", EntryPoint = "glVertex3f")]
	public static extern void Vertex(float x, float y, float z);

	[DllImport("Opengl32.dll", EntryPoint = "glColor3f")]
	public static extern void Color(float red, float green, float blue);

	[DllImport("Opengl32.dll", EntryPoint = "glColor4f")]
	public static extern void Color(float red, float green, float blue, float alpha);

	[DllImport("Opengl32.dll", EntryPoint = "glClearColor")]
	public static extern void ClearColor(float red, float green, float blue, float alpha);

	[DllImport("Opengl32.dll", EntryPoint = "glTexImage2D")]
	public static extern void TexImage2D(Target target, int level, uint internalformat, int width, int height, int border, PixelFormat format, DataType type, IntPtr pixels);

	[DllImport("Opengl32.dll", EntryPoint = "glTexImage2D")]
	public static extern void TexImage2D(Target target, int level, uint internalformat, int width, int height, int border, PixelFormat format, DataType type, byte[] pixels);

	[DllImport("Opengl32.dll", EntryPoint = "glGenTextures")]
	public static extern void GenTextures(int size, ref int textures);

	[DllImport("Opengl32.dll", EntryPoint = "glBindTexture")]
	public static extern void BindTexture(Target target, int texture);

	[DllImport("Opengl32.dll", EntryPoint = "glEnable")]
	public static extern void Enable(Target cap);

	[DllImport("Opengl32.dll", EntryPoint = "glDisable")]
	public static extern void Disable(Target cap);

	[DllImport("Opengl32.dll", EntryPoint = "glTexCoord2f")]
	public static extern void TexCoord(float s, float t);

	[DllImport("Opengl32.dll", EntryPoint = "glTexParameteri")]
	public static extern void TexParameteri(Target target, TextureParameterName pname, int param);

	[DllImport("Opengl32.dll", EntryPoint = "glGetError")]
	public static extern uint GetError();

	[DllImport("Opengl32.dll", EntryPoint = "glFlush")]
	public static extern void Flush();

	[DllImport("Opengl32.dll", EntryPoint = "glFinish")]
	public static extern void Finish();

	[DllImport("Opengl32.dll")]
	public static extern IntPtr wglCreateContext(IntPtr hdc);

	[DllImport("Opengl32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool wglMakeCurrent(IntPtr hdc, IntPtr hglrc);

	[DllImport("opengl32.dll")]
	public static extern IntPtr wglGetProcAddress(string name);

	[DllImport("opengl32.dll")]
	public static extern bool wglDeleteContext(IntPtr hglrc);

	[DllImport("opengl32.dll")]
	public static extern IntPtr wglGetCurrentContext();
}
