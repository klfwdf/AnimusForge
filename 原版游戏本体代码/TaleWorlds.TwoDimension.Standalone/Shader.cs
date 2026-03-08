using System;
using System.Numerics;
using System.Text;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension.Standalone.Native.OpenGL;

namespace TaleWorlds.TwoDimension.Standalone
{
	// Token: 0x0200000B RID: 11
	public class Shader
	{
		// Token: 0x06000079 RID: 121 RVA: 0x0000444F File Offset: 0x0000264F
		private Shader(GraphicsContext graphicsContext, int program)
		{
			this._graphicsContext = graphicsContext;
			this._program = program;
		}

		// Token: 0x0600007A RID: 122 RVA: 0x00004468 File Offset: 0x00002668
		public static Shader CreateShader(GraphicsContext graphicsContext, string vertexShaderCode, string fragmentShaderCode)
		{
			int program = Shader.CompileShaders(vertexShaderCode, fragmentShaderCode);
			return new Shader(graphicsContext, program);
		}

		// Token: 0x0600007B RID: 123 RVA: 0x00004484 File Offset: 0x00002684
		public static int CompileShaders(string vertexShaderCode, string fragmentShaderCode)
		{
			int shader = Opengl32ARB.CreateShaderObject(ShaderType.VertexShader);
			Opengl32ARB.ShaderSource(shader, vertexShaderCode);
			Opengl32ARB.CompileShader(shader);
			int num = -1;
			Opengl32ARB.GetShaderiv(shader, 35713, out num);
			if (num != 1)
			{
				int num2 = -1;
				Opengl32ARB.GetShaderiv(shader, 35716, out num2);
				int num3 = -1;
				byte[] array = new byte[4096];
				Opengl32ARB.GetShaderInfoLog(shader, 4096, out num3, array);
				Encoding.ASCII.GetString(array);
			}
			int shader2 = Opengl32ARB.CreateShaderObject(ShaderType.FragmentShader);
			Opengl32ARB.ShaderSource(shader2, fragmentShaderCode);
			Opengl32ARB.CompileShader(shader2);
			Opengl32ARB.GetShaderiv(shader2, 35713, out num);
			if (num != 1)
			{
				int num4 = -1;
				Opengl32ARB.GetShaderiv(shader2, 35716, out num4);
				int num5 = -1;
				byte[] array2 = new byte[4096];
				Opengl32ARB.GetShaderInfoLog(shader2, 4096, out num5, array2);
				Encoding.ASCII.GetString(array2);
			}
			int num6 = Opengl32ARB.CreateProgramObject();
			Opengl32ARB.AttachShader(num6, shader);
			Opengl32ARB.AttachShader(num6, shader2);
			Opengl32ARB.LinkProgram(num6);
			Opengl32ARB.GetProgramiv(num6, 35714, out num);
			if (num != 1)
			{
				int num7 = -1;
				Opengl32ARB.GetProgramiv(num6, 35716, out num7);
				int num8 = -1;
				byte[] array3 = new byte[4096];
				Opengl32ARB.GetProgramInfoLog(num6, 4096, out num8, array3);
				Encoding.ASCII.GetString(array3);
			}
			Opengl32ARB.DetachShader(num6, shader);
			Opengl32ARB.DetachShader(num6, shader2);
			Opengl32ARB.DeleteShader(shader);
			Opengl32ARB.DeleteShader(shader2);
			return num6;
		}

		// Token: 0x0600007C RID: 124 RVA: 0x0000464C File Offset: 0x0000284C
		public void SetTexture(string name, OpenGLTexture texture)
		{
			if (this._currentTextureUnit == 0)
			{
				Opengl32ARB.ActiveTexture(TextureUnit.Texture0);
			}
			else if (this._currentTextureUnit == 1)
			{
				Opengl32ARB.ActiveTexture(TextureUnit.Texture1);
			}
			Opengl32.BindTexture(Target.Texture2D, (texture != null) ? texture.Id : (-1));
			int uniformLocation = Opengl32ARB.GetUniformLocation(this._program, name);
			Opengl32ARB.Uniform1i(uniformLocation, this._currentTextureUnit);
			this._currentTextureUnit++;
		}

		// Token: 0x0600007D RID: 125 RVA: 0x000046CC File Offset: 0x000028CC
		public void SetColor(string name, Color color)
		{
			int uniformLocation = Opengl32ARB.GetUniformLocation(this._program, name);
			Opengl32ARB.Uniform4f(uniformLocation, color.Red, color.Green, color.Blue, color.Alpha);
		}

		// Token: 0x0600007E RID: 126 RVA: 0x00004709 File Offset: 0x00002909
		public void Use()
		{
			Opengl32ARB.UseProgram(this._program);
		}

		// Token: 0x0600007F RID: 127 RVA: 0x0000471B File Offset: 0x0000291B
		public void StopUsing()
		{
			this._currentTextureUnit = 0;
			Opengl32ARB.UseProgram(0);
		}

		// Token: 0x06000080 RID: 128 RVA: 0x0000472F File Offset: 0x0000292F
		public void SetMatrix(string name, Matrix4x4 matrix)
		{
			Opengl32ARB.UniformMatrix4fv(Opengl32ARB.GetUniformLocation(this._program, name), 1, false, matrix);
		}

		// Token: 0x06000081 RID: 129 RVA: 0x00004748 File Offset: 0x00002948
		public void SetBoolean(string name, bool value)
		{
			int uniformLocation = Opengl32ARB.GetUniformLocation(this._program, name);
			Opengl32ARB.Uniform1i(uniformLocation, value ? 1 : 0);
		}

		// Token: 0x06000082 RID: 130 RVA: 0x00004774 File Offset: 0x00002974
		public void SetFloat(string name, float value)
		{
			int uniformLocation = Opengl32ARB.GetUniformLocation(this._program, name);
			Opengl32ARB.Uniform1f(uniformLocation, value);
		}

		// Token: 0x06000083 RID: 131 RVA: 0x0000479C File Offset: 0x0000299C
		public void SetVector2(string name, Vector2 value)
		{
			int uniformLocation = Opengl32ARB.GetUniformLocation(this._program, name);
			Opengl32ARB.Uniform2f(uniformLocation, value.X, value.Y);
		}

		// Token: 0x0400003C RID: 60
		private GraphicsContext _graphicsContext;

		// Token: 0x0400003D RID: 61
		private int _program;

		// Token: 0x0400003E RID: 62
		private int _currentTextureUnit;
	}
}
