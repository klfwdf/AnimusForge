using System.IO;
using StbSharp;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension.Standalone.Native.OpenGL;

namespace TaleWorlds.TwoDimension.Standalone;

public class OpenGLTexture : ITexture
{
	private int _width;

	private int _height;

	private string _name;

	private GraphicsContext _context;

	private int _id;

	private bool _clampToEdge;

	public bool IsValid => true;

	public int Width => _width;

	public int Height => _height;

	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
		}
	}

	internal static OpenGLTexture ActiveTexture { get; private set; }

	internal int Id => _id;

	public bool ClampToEdge
	{
		get
		{
			return _clampToEdge;
		}
		set
		{
			_clampToEdge = value;
			if (ActiveTexture != this)
			{
				MakeActive();
			}
			else
			{
				SetTextureParameters();
			}
		}
	}

	public void Initialize(string name, int width, int height)
	{
		_context = GraphicsContext.Active;
		Name = name;
		_id = 0;
		Opengl32.GenTextures(1, ref _id);
		_width = width;
		_height = height;
	}

	public void CopyFrom(OpenGLTexture texture)
	{
		_width = texture._width;
		_height = texture._height;
		Name = texture.Name;
		_id = texture._id;
		_context = texture._context;
	}

	public void Delete()
	{
		Opengl32.DeleteTextures(1, new int[1] { _id });
		Debug.Print("texture deleted! : " + Name);
	}

	internal void MakeActive()
	{
		if (ActiveTexture != this)
		{
			Opengl32.BindTexture(Target.Texture2D, _id);
			ActiveTexture = this;
			SetTextureParameters();
		}
	}

	private void SetTextureParameters()
	{
		Opengl32.TexParameteri(Target.Texture2D, TextureParameterName.TextureMinFilter, 9729);
		Opengl32.TexParameteri(Target.Texture2D, TextureParameterName.TextureMagFilter, 9729);
		if (ClampToEdge)
		{
			Opengl32.TexParameteri(Target.Texture2D, TextureParameterName.TextureWrapS, 33071);
			Opengl32.TexParameteri(Target.Texture2D, TextureParameterName.TextureWrapT, 33071);
		}
	}

	public static OpenGLTexture FromFile(ResourceDepot resourceDepot, string name)
	{
		OpenGLTexture openGLTexture = new OpenGLTexture();
		openGLTexture.LoadFromFile(resourceDepot, name);
		return openGLTexture;
	}

	public static OpenGLTexture FromFile(string fullFilePath)
	{
		OpenGLTexture openGLTexture = new OpenGLTexture();
		openGLTexture.LoadFromFile(fullFilePath);
		return openGLTexture;
	}

	public void Release()
	{
		Delete();
	}

	public void LoadFromFile(ResourceDepot resourceDepot, string name)
	{
		string filePath = resourceDepot.GetFilePath(name + ".png");
		LoadFromFile(filePath);
	}

	public void LoadFromFile(string fullPathName)
	{
		if (!File.Exists(fullPathName))
		{
			Debug.Print("File not found: " + fullPathName);
			return;
		}
		Image image = null;
		using (Stream stream = new MemoryStream(File.ReadAllBytes(fullPathName)))
		{
			image = new ImageReader().Read(stream);
		}
		if (image == null)
		{
			Debug.Print("Error while reading file: " + fullPathName);
			return;
		}
		int width = image.Width;
		int height = image.Height;
		Initialize(Path.GetFileName(fullPathName), width, height);
		MakeActive();
		PixelFormat format = PixelFormat.Red;
		uint internalformat = 0u;
		bool flag = true;
		switch (image.Comp)
		{
		case 1:
			format = PixelFormat.Red;
			internalformat = 33321u;
			break;
		case 3:
			format = PixelFormat.RGB;
			internalformat = 32849u;
			break;
		case 4:
			format = PixelFormat.RGBA;
			internalformat = 32856u;
			break;
		default:
			flag = false;
			Debug.Print("Unknown image format at file: " + fullPathName + ". Supported formats are: Single-Channel, RGB and RGBA.");
			break;
		}
		if (flag)
		{
			Opengl32.TexImage2D(Target.Texture2D, 0, internalformat, width, height, 0, format, DataType.UnsignedByte, image.Data);
		}
	}

	public bool IsLoaded()
	{
		return true;
	}
}
