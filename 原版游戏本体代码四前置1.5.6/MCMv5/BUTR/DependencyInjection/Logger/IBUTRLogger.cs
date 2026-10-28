namespace BUTR.DependencyInjection.Logger;

public interface IBUTRLogger
{
	void LogMessage(LogLevel logLevel, string message, params object[] args);
}
public interface IBUTRLogger<T> : IBUTRLogger
{
}
