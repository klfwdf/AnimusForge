using System;
using System.IO;
using StbSharp;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension.Standalone.Native.OpenGL;

namespace TaleWorlds.TwoDimension.Standalone
{
	// Token: 0x0200000A RID: 10
	public class OpenGLTexture : ITexture
	{
		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000063 RID: 99 RVA: 0x0000410C File Offset: 0x0000230C
		public bool IsValid
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x06000064 RID: 100 RVA: 0x0000410F File Offset: 0x0000230F
		public int Width
		{
			get
			{
				return this._width;
			}
		}

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x06000065 RID: 101 RVA: 0x00004117 File Offset: 0x00002317
		public int Height
		{
			get
			{
				return this._height;
			}
		}

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x06000066 RID: 102 RVA: 0x0000411F File Offset: 0x0000231F
		// (set) Token: 0x06000067 RID: 103 RVA: 0x00004127 File Offset: 0x00002327
		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				this._name = value;
			}
		}

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x06000068 RID: 104 RVA: 0x00004130 File Offset: 0x00002330
		// (set) Token: 0x06000069 RID: 105 RVA: 0x00004137 File Offset: 0x00002337
		internal static OpenGLTexture ActiveTexture { get; private set; }

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x0600006A RID: 106 RVA: 0x0000413F File Offset: 0x0000233F
		internal int Id
		{
			get
			{
				return this._id;
			}
		}

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x0600006B RID: 107 RVA: 0x00004147 File Offset: 0x00002347
		// (set) Token: 0x0600006C RID: 108 RVA: 0x0000414F File Offset: 0x0000234F
		public bool ClampToEdge
		{
			get
			{
				return this._clampToEdge;
			}
			set
			{
				this._clampToEdge = value;
				if (OpenGLTexture.ActiveTexture != this)
				{
					this.MakeActive();
					return;
				}
				this.SetTextureParameters();
			}
		}

		// Token: 0x0600006E RID: 110 RVA: 0x00004175 File Offset: 0x00002375
		public void Initialize(string name, int width, int height)
		{
			this._context = GraphicsContext.Active;
			this.Name = name;
			this._id = 0;
			Opengl32.GenTextures(1, ref this._id);
			this._width = width;
			this._height = height;
		}

		// Token: 0x0600006F RID: 111 RVA: 0x000041AA File Offset: 0x000023AA
		public void CopyFrom(OpenGLTexture texture)
		{
			this._width = texture._width;
			this._height = texture._height;
			this.Name = texture.Name;
			this._id = texture._id;
			this._context = texture._context;
		}

		// Token: 0x06000070 RID: 112 RVA: 0x000041E8 File Offset: 0x000023E8
		public void Delete()
		{
			Opengl32.DeleteTextures(1, new int[] { this._id });
			Debug.Print("texture deleted! : " + this.Name, 0, Debug.DebugColor.White, 17592186044416UL);
		}

		// Token: 0x06000071 RID: 113 RVA: 0x00004220 File Offset: 0x00002420
		internal void MakeActive()
		{
			if (OpenGLTexture.ActiveTexture != this)
			{
				Opengl32.BindTexture(Target.Texture2D, this._id);
				OpenGLTexture.ActiveTexture = this;
				this.SetTextureParameters();
			}
		}

		// Token: 0x06000072 RID: 114 RVA: 0x00004248 File Offset: 0x00002448
		private void SetTextureParameters()
		{
			Opengl32.TexParameteri(Target.Texture2D, TextureParameterName.TextureMinFilter, 9729);
			Opengl32.TexParameteri(Target.Texture2D, TextureParameterName.TextureMagFilter, 9729);
			if (this.ClampToEdge)
			{
				Opengl32.TexParameteri(Target.Texture2D, TextureParameterName.TextureWrapS, 33071);
				Opengl32.TexParameteri(Target.Texture2D, TextureParameterName.TextureWrapT, 33071);
			}
		}

		// Token: 0x06000073 RID: 115 RVA: 0x000042AD File Offset: 0x000024AD
		public static OpenGLTexture FromFile(ResourceDepot resourceDepot, string name)
		{
			OpenGLTexture openGLTexture = new OpenGLTexture();
			openGLTexture.LoadFromFile(resourceDepot, name);
			return openGLTexture;
		}

		// Token: 0x06000074 RID: 116 RVA: 0x000042BC File Offset: 0x000024BC
		public static OpenGLTexture FromFile(string fullFilePath)
		{
			OpenGLTexture openGLTexture = new OpenGLTexture();
			openGLTexture.LoadFromFile(fullFilePath);
			return openGLTexture;
		}

		// Token: 0x06000075 RID: 117 RVA: 0x000042CA File Offset: 0x000024CA
		public void Release()
		{
			this.Delete();
		}

		// Token: 0x06000076 RID: 118 RVA: 0x000042D4 File Offset: 0x000024D4
		public void LoadFromFile(ResourceDepot resourceDepot, string name)
		{
			string filePath = resourceDepot.GetFilePath(name + ".png");
			this.LoadFromFile(filePath);
		}

		// Token: 0x06000077 RID: 119 RVA: 0x000042FC File Offset: 0x000024FC
		public void LoadFromFile(string fullPathName)
		{
			if (!File.Exists(fullPathName))
			{
				Debug.Print("File not found: " + fullPathName, 0, Debug.DebugColor.White, 17592186044416UL);
				return;
			}
			Image image = null;
			using (Stream stream = new MemoryStream(File.ReadAllBytes(fullPathName)))
			{
				image = new ImageReader().Read(stream, 0);
			}
			if (image == null)
			{
				Debug.Print("Error while reading file: " + fullPathName, 0, Debug.DebugColor.White, 17592186044416UL);
				return;
			}
			int width = image.Width;
			int height = image.Height;
			this.Initialize(Path.GetFileName(fullPathName), width, height);
			this.MakeActive();
			PixelFormat format = PixelFormat.Red;
			uint internalformat = 0U;
			bool flag = true;
			switch (image.Comp)
			{
			case 1:
				format = PixelFormat.Red;
				internalformat = 33321U;
				goto IL_112;
			case 3:
				format = PixelFormat.RGB;
				internalformat = 32849U;
				goto IL_112;
			case 4:
				format = PixelFormat.RGBA;
				internalformat = 32856U;
				goto IL_112;
			}
			flag = false;
			Debug.Print("Unknown image format at file: " + fullPathName + ". Supported formats are: Single-Channel, RGB and RGBA.", 0, Debug.DebugColor.White, 17592186044416UL);
			IL_112:
			if (flag)
			{
				Opengl32.TexImage2D(Target.Texture2D, 0, internalformat, width, height, 0, format, DataType.UnsignedByte, image.Data);
			}
		}

		// Token: 0x06000078 RID: 120 RVA: 0x0000444C File Offset: 0x0000264C
		public bool IsLoaded()
		{
			return true;
		}

		// Token: 0x04000035 RID: 53
		private int _width;

		// Token: 0x04000036 RID: 54
		private int _height;

		// Token: 0x04000037 RID: 55
		private string _name;

		// Token: 0x04000039 RID: 57
		private GraphicsContext _context;

		// Token: 0x0400003A RID: 58
		private int _id;

		// Token: 0x0400003B RID: 59
		private bool _clampToEdge;
	}
}
