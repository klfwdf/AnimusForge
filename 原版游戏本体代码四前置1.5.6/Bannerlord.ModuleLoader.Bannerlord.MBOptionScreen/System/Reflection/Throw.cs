using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection;

internal static class Throw
{
	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void InvalidCast()
	{
		throw new InvalidCastException();
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void InvalidArgument(string message, string parameterName)
	{
		throw new ArgumentException(message, parameterName);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void InvalidArgument_OffsetForVirtualHeapHandle()
	{
		throw new ArgumentException(System.SR.CantGetOffsetForVirtualHeapHandle, "handle");
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static Exception InvalidArgument_UnexpectedHandleKind(System.Reflection.Metadata.HandleKind kind)
	{
		throw new ArgumentException(System.SR.Format(System.SR.UnexpectedHandleKind, kind));
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static Exception InvalidArgument_Handle(string parameterName)
	{
		throw new ArgumentException(System.SR.InvalidHandle, parameterName);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void SignatureNotVarArg()
	{
		throw new InvalidOperationException(System.SR.SignatureNotVarArg);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void ControlFlowBuilderNotAvailable()
	{
		throw new InvalidOperationException(System.SR.ControlFlowBuilderNotAvailable);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void InvalidOperationBuilderAlreadyLinked()
	{
		throw new InvalidOperationException(System.SR.BuilderAlreadyLinked);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void InvalidOperation(string message)
	{
		throw new InvalidOperationException(message);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void InvalidOperation_LabelNotMarked(int id)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.LabelNotMarked, id));
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void LabelDoesntBelongToBuilder(string parameterName)
	{
		throw new ArgumentException(System.SR.LabelDoesntBelongToBuilder, parameterName);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void HeapHandleRequired()
	{
		throw new ArgumentException(System.SR.NotMetadataHeapHandle, "handle");
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void EntityOrUserStringHandleRequired()
	{
		throw new ArgumentException(System.SR.NotMetadataTableOrUserStringHandle, "handle");
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void InvalidToken()
	{
		throw new ArgumentException(System.SR.InvalidToken, "token");
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void ArgumentNull(string parameterName)
	{
		throw new ArgumentNullException(parameterName);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void ArgumentEmptyString(string parameterName)
	{
		throw new ArgumentException(System.SR.ExpectedNonEmptyString, parameterName);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void ArgumentEmptyArray(string parameterName)
	{
		throw new ArgumentException(System.SR.ExpectedNonEmptyArray, parameterName);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void ValueArgumentNull()
	{
		throw new ArgumentNullException("value");
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void BuilderArgumentNull()
	{
		throw new ArgumentNullException("builder");
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void ArgumentOutOfRange(string parameterName)
	{
		throw new ArgumentOutOfRangeException(parameterName);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void ArgumentOutOfRange(string parameterName, string message)
	{
		throw new ArgumentOutOfRangeException(parameterName, message);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void BlobTooLarge(string parameterName)
	{
		throw new ArgumentOutOfRangeException(parameterName, System.SR.BlobTooLarge);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void IndexOutOfRange()
	{
		throw new ArgumentOutOfRangeException("index");
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void TableIndexOutOfRange()
	{
		throw new ArgumentOutOfRangeException("tableIndex");
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void ValueArgumentOutOfRange()
	{
		throw new ArgumentOutOfRangeException("value");
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void OutOfBounds()
	{
		throw new BadImageFormatException(System.SR.OutOfBoundsRead);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void WriteOutOfBounds()
	{
		throw new InvalidOperationException(System.SR.OutOfBoundsWrite);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void InvalidCodedIndex()
	{
		throw new BadImageFormatException(System.SR.InvalidCodedIndex);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void InvalidHandle()
	{
		throw new BadImageFormatException(System.SR.InvalidHandle);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void InvalidCompressedInteger()
	{
		throw new BadImageFormatException(System.SR.InvalidCompressedInteger);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void InvalidSerializedString()
	{
		throw new BadImageFormatException(System.SR.InvalidSerializedString);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void ImageTooSmall()
	{
		throw new BadImageFormatException(System.SR.ImageTooSmall);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void ImageTooSmallOrContainsInvalidOffsetOrCount()
	{
		throw new BadImageFormatException(System.SR.ImageTooSmallOrContainsInvalidOffsetOrCount);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void ReferenceOverflow()
	{
		throw new BadImageFormatException(System.SR.RowIdOrHeapOffsetTooLarge);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void TableNotSorted(System.Reflection.Metadata.Ecma335.TableIndex tableIndex)
	{
		throw new BadImageFormatException(System.SR.Format(System.SR.MetadataTableNotSorted, tableIndex));
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void InvalidOperation_TableNotSorted(System.Reflection.Metadata.Ecma335.TableIndex tableIndex)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.MetadataTableNotSorted, tableIndex));
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void InvalidOperation_PEImageNotAvailable()
	{
		throw new InvalidOperationException(System.SR.PEImageNotAvailable);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void TooManySubnamespaces()
	{
		throw new BadImageFormatException(System.SR.TooManySubnamespaces);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void ValueOverflow()
	{
		throw new BadImageFormatException(System.SR.ValueTooLarge);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void SequencePointValueOutOfRange()
	{
		throw new BadImageFormatException(System.SR.SequencePointValueOutOfRange);
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void HeapSizeLimitExceeded(HeapIndex heap)
	{
		throw new ImageFormatLimitationException(System.SR.Format(System.SR.HeapSizeLimitExceeded, heap));
	}

	[System.Diagnostics.CodeAnalysis.DoesNotReturn]
	internal static void PEReaderDisposed()
	{
		throw new ObjectDisposedException("PEReader");
	}
}
