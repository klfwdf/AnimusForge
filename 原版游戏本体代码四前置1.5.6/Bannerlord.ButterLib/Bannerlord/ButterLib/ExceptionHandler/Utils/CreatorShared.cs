using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Bannerlord.ButterLib.ExceptionHandler.Extensions;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace Bannerlord.ButterLib.ExceptionHandler.Utils;

internal static class CreatorShared
{
	public static Stream GetMiniDump()
	{
		try
		{
			if (!MiniDump.TryDump(out MemoryStream dataStream))
			{
				return Stream.Null;
			}
			return dataStream;
		}
		catch (Exception)
		{
			return Stream.Null;
		}
	}

	public static Stream GetSaveFile()
	{
		try
		{
			string directoryFullPath = PlatformFileHelperPCExtended.GetDirectoryFullPath(new PlatformDirectoryPath(PlatformFileType.User, "Game Saves\\"));
			if (string.IsNullOrEmpty(directoryFullPath))
			{
				return Stream.Null;
			}
			FileInfo fileInfo = (from x in new DirectoryInfo(directoryFullPath).EnumerateFiles("*.sav", SearchOption.TopDirectoryOnly)
				orderby x.LastWriteTimeUtc descending
				select x).FirstOrDefault();
			if (fileInfo == null)
			{
				return Stream.Null;
			}
			return fileInfo.OpenRead();
		}
		catch (Exception)
		{
			return Stream.Null;
		}
	}

	public static Stream GetScreenshot()
	{
		try
		{
			string text = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName() + ".bmp");
			Utilities.TakeScreenshot(text);
			using Image image = Image.FromFile(text);
			using EncoderParameters encoderParameters = new EncoderParameters(1);
			encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 80L);
			MemoryStream memoryStream = new MemoryStream();
			image.Save(memoryStream, ImageCodecInfo.GetImageDecoders().First((ImageCodecInfo codec) => codec.FormatID == ImageFormat.Jpeg.Guid), encoderParameters);
			return memoryStream;
		}
		catch (Exception)
		{
			return Stream.Null;
		}
	}
}
