namespace TaleWorlds.Diamond;

public static class ServerFailureReason
{
	public const string SessionNotFound = "SessionNotFound";

	public const string InvalidCredentials = "InvalidCredentials";

	public const string InvalidCertificate = "InvalidCertificate";

	public const string UnknownMessageType = "UnknownMessageType";

	public const string FeatureNotSupported = "FeatureNotSupported";

	public const string PeerTypeMismatch = "PeerTypeMismatch";

	public const string ServerError = "ServerError";

	public const string HandlerFailed = "HandlerFailed";
}
