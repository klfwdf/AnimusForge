using System;

namespace TaleWorlds.Network;

public class ServiceException : Exception
{
	public string ExceptionMessage { get; set; }

	public string ExceptionType { get; set; }

	public ServiceException(string type, string message)
		: base("ServiceException")
	{
		ExceptionType = type;
		ExceptionMessage = message;
	}
}
