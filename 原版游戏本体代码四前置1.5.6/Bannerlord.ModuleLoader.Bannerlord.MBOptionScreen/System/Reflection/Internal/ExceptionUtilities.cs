namespace System.Reflection.Internal;

internal static class ExceptionUtilities
{
	internal static Exception UnexpectedValue(object value)
	{
		if (value != null && value.GetType().FullName != null)
		{
			return new InvalidOperationException(System.SR.Format(System.SR.UnexpectedValue, value, value.GetType().FullName));
		}
		return new InvalidOperationException(System.SR.Format(System.SR.UnexpectedValueUnknownType, value));
	}
}
