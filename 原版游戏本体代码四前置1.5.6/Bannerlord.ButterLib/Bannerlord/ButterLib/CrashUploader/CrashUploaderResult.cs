namespace Bannerlord.ButterLib.CrashUploader;

internal record CrashUploaderResult
{
	public CrashUploaderStatus Status { get; }

	public string? Url { get; private init; }

	public string? StatusCode { get; private init; }

	public string? Exception { get; private init; }

	public static CrashUploaderResult Success(string url)
	{
		return new CrashUploaderResult(CrashUploaderStatus.Success)
		{
			Url = url
		};
	}

	public static CrashUploaderResult MetadataNotFound()
	{
		return new CrashUploaderResult(CrashUploaderStatus.MetadataNotFound);
	}

	public static CrashUploaderResult ResponseIsNotHttpWebResponse()
	{
		return new CrashUploaderResult(CrashUploaderStatus.ResponseIsNotHttpWebResponse);
	}

	public static CrashUploaderResult WrongStatusCode(string statusCode)
	{
		return new CrashUploaderResult(CrashUploaderStatus.WrongStatusCode)
		{
			StatusCode = statusCode
		};
	}

	public static CrashUploaderResult ResponseStreamIsNull()
	{
		return new CrashUploaderResult(CrashUploaderStatus.ResponseStreamIsNull);
	}

	public static CrashUploaderResult ResponseUrlIsNullOrEmpty()
	{
		return new CrashUploaderResult(CrashUploaderStatus.UrlIsNullOrEmpty);
	}

	public static CrashUploaderResult FailedWithException(string exception)
	{
		return new CrashUploaderResult(CrashUploaderStatus.FailedWithException)
		{
			Exception = exception
		};
	}

	private CrashUploaderResult(CrashUploaderStatus status)
	{
		Status = status;
	}
}
