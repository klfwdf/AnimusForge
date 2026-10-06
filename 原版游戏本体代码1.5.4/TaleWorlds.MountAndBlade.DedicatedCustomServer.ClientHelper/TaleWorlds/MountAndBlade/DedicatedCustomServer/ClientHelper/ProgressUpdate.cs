namespace TaleWorlds.MountAndBlade.DedicatedCustomServer.ClientHelper;

public class ProgressUpdate
{
	private const float bytesPerMegaByte = 1048576f;

	public long BytesRead { get; private set; }

	public long TotalBytes { get; private set; }

	public float MegaBytesRead => (float)BytesRead / 1048576f;

	public float TotalMegaBytes => (float)TotalBytes / 1048576f;

	public float ProgressRatio => (float)BytesRead / (float)TotalBytes;

	public ProgressUpdate(long bytesRead, long totalBytes)
	{
		BytesRead = bytesRead;
		TotalBytes = totalBytes;
	}
}
