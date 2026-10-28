namespace System.Buffers;

internal enum OperationStatus
{
	Done,
	DestinationTooSmall,
	NeedMoreData,
	InvalidData
}
