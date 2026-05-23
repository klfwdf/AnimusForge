using System.Numerics;
using System.Text;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension.Standalone.Native.OpenGL;

namespace TaleWorlds.TwoDimension.Standalone;

public class Shader
{
	private GraphicsContext _graphicsContext;

	private int _program;

	private int _currentTextureUnit;

	private Shader(GraphicsContext graphicsContext, int program)
	{
		_graphicsContext = graphicsContext;
		_program = program;
	}

	public static Shader CreateShader(GraphicsContext graphicsContext, string vertexShaderCode, string fragmentShaderCode)
	{
		int num = CompileShaders(vertexShaderCode, fragmentShaderCode);
		if (num < 0)
		{
			return null;
		}
		return new Shader(graphicsContext, num);
	}

	public static int CompileShaders(string vertexShaderCode, string fragmentShaderCode)
	{
		bool flag = false;
		int shader = Opengl32ARB.CreateShaderObject(ShaderType.VertexShader);
		Opengl32ARB.ShaderSource(shader, vertexShaderCode);
		Opengl32ARB.CompileShader(shader);
		int returnValue = -1;
		Opengl32ARB.GetShaderiv(shader, 35713, out returnValue);
		if (returnValue != 1)
		{
			int returnValue2 = -1;
			Opengl32ARB.GetShaderiv(shader, 35716, out returnValue2);
			int length = -1;
			byte[] array = new byte[4096];
			Opengl32ARB.GetShaderInfoLog(shader, 4096, out length, array);
			Encoding.ASCII.GetString(array);
			flag = true;
		}
		int shader2 = Opengl32ARB.CreateShaderObject(ShaderType.FragmentShader);
		Opengl32ARB.ShaderSource(shader2, fragmentShaderCode);
		Opengl32ARB.CompileShader(shader2);
		Opengl32ARB.GetShaderiv(shader2, 35713, out returnValue);
		if (returnValue != 1)
		{
			int returnValue3 = -1;
			Opengl32ARB.GetShaderiv(shader2, 35716, out returnValue3);
			int length2 = -1;
			byte[] array2 = new byte[4096];
			Opengl32ARB.GetShaderInfoLog(shader2, 4096, out length2, array2);
			Encoding.ASCII.GetString(array2);
			flag = true;
		}
		int num = Opengl32ARB.CreateProgramObject();
		Opengl32ARB.AttachShader(num, shader);
		Opengl32ARB.AttachShader(num, shader2);
		Opengl32ARB.LinkProgram(num);
		Opengl32ARB.GetProgramiv(num, 35714, out returnValue);
		if (returnValue != 1)
		{
			int returnValue4 = -1;
			Opengl32ARB.GetProgramiv(num, 35716, out returnValue4);
			int length3 = -1;
			byte[] array3 = new byte[4096];
			Opengl32ARB.GetProgramInfoLog(num, 4096, out length3, array3);
			Encoding.ASCII.GetString(array3);
			flag = true;
		}
		Opengl32ARB.DetachShader(num, shader);
		Opengl32ARB.DetachShader(num, shader2);
		Opengl32ARB.DeleteShader(shader);
		Opengl32ARB.DeleteShader(shader2);
		if (flag)
		{
			Opengl32ARB.DeleteProgram(num);
			return -1;
		}
		return num;
	}

	public void SetTexture(string name, OpenGLTexture texture)
	{
		if (_currentTextureUnit == 0)
		{
			Opengl32ARB.ActiveTexture(TextureUnit.Texture0);
		}
		else if (_currentTextureUnit == 1)
		{
			Opengl32ARB.ActiveTexture(TextureUnit.Texture1);
		}
		Opengl32.BindTexture(Target.Texture2D, texture?.Id ?? (-1));
		int uniformLocation = Opengl32ARB.GetUniformLocation(_program, name);
		Opengl32ARB.Uniform1i(uniformLocation, _currentTextureUnit);
		_currentTextureUnit++;
	}

	public void SetColor(string name, Color color)
	{
		int uniformLocation = Opengl32ARB.GetUniformLocation(_program, name);
		Opengl32ARB.Uniform4f(uniformLocation, color.Red, color.Green, color.Blue, color.Alpha);
	}

	public void Use()
	{
		Opengl32ARB.UseProgram(_program);
	}

	public void StopUsing()
	{
		_currentTextureUnit = 0;
		Opengl32ARB.UseProgram(0);
	}

	public void SetMatrix(string name, in Matrix4x4 matrix)
	{
		Opengl32ARB.UniformMatrix4fv(Opengl32ARB.GetUniformLocation(_program, name), 1, isTranspose: false, in matrix);
	}

	public void SetBoolean(string name, bool value)
	{
		int uniformLocation = Opengl32ARB.GetUniformLocation(_program, name);
		Opengl32ARB.Uniform1i(uniformLocation, value ? 1 : 0);
	}

	public void SetFloat(string name, float value)
	{
		int uniformLocation = Opengl32ARB.GetUniformLocation(_program, name);
		Opengl32ARB.Uniform1f(uniformLocation, value);
	}

	public void SetVector2(string name, Vector2 value)
	{
		int uniformLocation = Opengl32ARB.GetUniformLocation(_program, name);
		Opengl32ARB.Uniform2f(uniformLocation, value.X, value.Y);
	}
}
