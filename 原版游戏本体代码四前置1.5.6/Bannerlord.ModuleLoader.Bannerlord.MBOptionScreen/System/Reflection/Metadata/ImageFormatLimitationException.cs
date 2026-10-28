using System.Runtime.Serialization;

namespace System.Reflection.Metadata;

[Serializable]
internal class ImageFormatLimitationException : Exception
{
	public ImageFormatLimitationException()
	{
	}

	public ImageFormatLimitationException(string? message)
		: base(message)
	{
	}

	public ImageFormatLimitationException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	protected ImageFormatLimitationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
