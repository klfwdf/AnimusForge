using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Internal;

namespace System.Reflection.Metadata;

[DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
internal readonly struct SequencePoint : IEquatable<System.Reflection.Metadata.SequencePoint>
{
	public const int HiddenLine = 16707566;

	public System.Reflection.Metadata.DocumentHandle Document { get; }

	public int Offset { get; }

	public int StartLine { get; }

	public int EndLine { get; }

	public int StartColumn { get; }

	public int EndColumn { get; }

	public bool IsHidden => StartLine == 16707566;

	internal SequencePoint(System.Reflection.Metadata.DocumentHandle document, int offset)
	{
		Document = document;
		Offset = offset;
		StartLine = 16707566;
		StartColumn = 0;
		EndLine = 16707566;
		EndColumn = 0;
	}

	internal SequencePoint(System.Reflection.Metadata.DocumentHandle document, int offset, int startLine, ushort startColumn, int endLine, ushort endColumn)
	{
		Document = document;
		Offset = offset;
		StartLine = startLine;
		StartColumn = startColumn;
		EndLine = endLine;
		EndColumn = endColumn;
	}

	public override int GetHashCode()
	{
		return System.Reflection.Internal.Hash.Combine(Document.RowId, System.Reflection.Internal.Hash.Combine(Offset, System.Reflection.Internal.Hash.Combine(StartLine, System.Reflection.Internal.Hash.Combine(StartColumn, System.Reflection.Internal.Hash.Combine(EndLine, EndColumn)))));
	}

	public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj)
	{
		if (obj is System.Reflection.Metadata.SequencePoint other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(System.Reflection.Metadata.SequencePoint other)
	{
		if (Document == other.Document && Offset == other.Offset && StartLine == other.StartLine && StartColumn == other.StartColumn && EndLine == other.EndLine)
		{
			return EndColumn == other.EndColumn;
		}
		return false;
	}

	private string GetDebuggerDisplay()
	{
		if (!IsHidden)
		{
			return $"{Offset}: ({StartLine}, {StartColumn}) - ({EndLine}, {EndColumn})";
		}
		return "<hidden>";
	}
}
