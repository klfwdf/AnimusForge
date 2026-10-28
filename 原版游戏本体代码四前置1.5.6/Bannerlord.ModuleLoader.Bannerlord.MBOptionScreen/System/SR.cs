using System.Resources;
using FxResources.System.Reflection.Metadata;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = GetUsingResourceKeysSwitchValue();

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string ImageTooSmall => GetResourceString("ImageTooSmall");

	internal static string InvalidCorHeaderSize => GetResourceString("InvalidCorHeaderSize");

	internal static string InvalidHandle => GetResourceString("InvalidHandle");

	internal static string UnexpectedHandleKind => GetResourceString("UnexpectedHandleKind");

	internal static string UnexpectedOpCode => GetResourceString("UnexpectedOpCode");

	internal static string InvalidLocalSignatureToken => GetResourceString("InvalidLocalSignatureToken");

	internal static string InvalidMetadataSectionSpan => GetResourceString("InvalidMetadataSectionSpan");

	internal static string InvalidMethodHeader1 => GetResourceString("InvalidMethodHeader1");

	internal static string InvalidMethodHeader2 => GetResourceString("InvalidMethodHeader2");

	internal static string InvalidPESignature => GetResourceString("InvalidPESignature");

	internal static string InvalidSehHeader => GetResourceString("InvalidSehHeader");

	internal static string InvalidToken => GetResourceString("InvalidToken");

	internal static string MetadataImageDoesNotRepresentAnAssembly => GetResourceString("MetadataImageDoesNotRepresentAnAssembly");

	internal static string StandaloneDebugMetadataImageDoesNotContainModuleTable => GetResourceString("StandaloneDebugMetadataImageDoesNotContainModuleTable");

	internal static string PEImageNotAvailable => GetResourceString("PEImageNotAvailable");

	internal static string MissingDataDirectory => GetResourceString("MissingDataDirectory");

	internal static string NotMetadataHeapHandle => GetResourceString("NotMetadataHeapHandle");

	internal static string NotMetadataTableOrUserStringHandle => GetResourceString("NotMetadataTableOrUserStringHandle");

	internal static string SectionTooSmall => GetResourceString("SectionTooSmall");

	internal static string StreamMustSupportReadAndSeek => GetResourceString("StreamMustSupportReadAndSeek");

	internal static string UnknownFileFormat => GetResourceString("UnknownFileFormat");

	internal static string UnknownPEMagicValue => GetResourceString("UnknownPEMagicValue");

	internal static string MetadataTableNotSorted => GetResourceString("MetadataTableNotSorted");

	internal static string ModuleTableInvalidNumberOfRows => GetResourceString("ModuleTableInvalidNumberOfRows");

	internal static string UnknownTables => GetResourceString("UnknownTables");

	internal static string IllegalTablesInCompressedMetadataStream => GetResourceString("IllegalTablesInCompressedMetadataStream");

	internal static string TableRowCountSpaceTooSmall => GetResourceString("TableRowCountSpaceTooSmall");

	internal static string OutOfBoundsRead => GetResourceString("OutOfBoundsRead");

	internal static string OutOfBoundsWrite => GetResourceString("OutOfBoundsWrite");

	internal static string MetadataHeaderTooSmall => GetResourceString("MetadataHeaderTooSmall");

	internal static string MetadataSignature => GetResourceString("MetadataSignature");

	internal static string NotEnoughSpaceForVersionString => GetResourceString("NotEnoughSpaceForVersionString");

	internal static string StreamHeaderTooSmall => GetResourceString("StreamHeaderTooSmall");

	internal static string NotEnoughSpaceForStreamHeaderName => GetResourceString("NotEnoughSpaceForStreamHeaderName");

	internal static string NotEnoughSpaceForStringStream => GetResourceString("NotEnoughSpaceForStringStream");

	internal static string NotEnoughSpaceForBlobStream => GetResourceString("NotEnoughSpaceForBlobStream");

	internal static string NotEnoughSpaceForGUIDStream => GetResourceString("NotEnoughSpaceForGUIDStream");

	internal static string NotEnoughSpaceForMetadataStream => GetResourceString("NotEnoughSpaceForMetadataStream");

	internal static string InvalidMetadataStreamFormat => GetResourceString("InvalidMetadataStreamFormat");

	internal static string MetadataTablesTooSmall => GetResourceString("MetadataTablesTooSmall");

	internal static string MetadataTableHeaderTooSmall => GetResourceString("MetadataTableHeaderTooSmall");

	internal static string WinMDMissingMscorlibRef => GetResourceString("WinMDMissingMscorlibRef");

	internal static string UnexpectedStreamEnd => GetResourceString("UnexpectedStreamEnd");

	internal static string InvalidMethodRva => GetResourceString("InvalidMethodRva");

	internal static string CantGetOffsetForVirtualHeapHandle => GetResourceString("CantGetOffsetForVirtualHeapHandle");

	internal static string InvalidNumberOfSections => GetResourceString("InvalidNumberOfSections");

	internal static string InvalidSignature => GetResourceString("InvalidSignature");

	internal static string PEImageDoesNotHaveMetadata => GetResourceString("PEImageDoesNotHaveMetadata");

	internal static string InvalidCodedIndex => GetResourceString("InvalidCodedIndex");

	internal static string InvalidCompressedInteger => GetResourceString("InvalidCompressedInteger");

	internal static string InvalidDocumentName => GetResourceString("InvalidDocumentName");

	internal static string RowIdOrHeapOffsetTooLarge => GetResourceString("RowIdOrHeapOffsetTooLarge");

	internal static string EnCMapNotSorted => GetResourceString("EnCMapNotSorted");

	internal static string InvalidSerializedString => GetResourceString("InvalidSerializedString");

	internal static string StreamTooLarge => GetResourceString("StreamTooLarge");

	internal static string ImageTooSmallOrContainsInvalidOffsetOrCount => GetResourceString("ImageTooSmallOrContainsInvalidOffsetOrCount");

	internal static string MetadataStringDecoderEncodingMustBeUtf8 => GetResourceString("MetadataStringDecoderEncodingMustBeUtf8");

	internal static string InvalidConstantValue => GetResourceString("InvalidConstantValue");

	internal static string InvalidConstantValueOfType => GetResourceString("InvalidConstantValueOfType");

	internal static string InvalidImportDefinitionKind => GetResourceString("InvalidImportDefinitionKind");

	internal static string ValueTooLarge => GetResourceString("ValueTooLarge");

	internal static string BlobTooLarge => GetResourceString("BlobTooLarge");

	internal static string InvalidTypeSize => GetResourceString("InvalidTypeSize");

	internal static string HandleBelongsToFutureGeneration => GetResourceString("HandleBelongsToFutureGeneration");

	internal static string InvalidRowCount => GetResourceString("InvalidRowCount");

	internal static string InvalidEntryPointToken => GetResourceString("InvalidEntryPointToken");

	internal static string TooManySubnamespaces => GetResourceString("TooManySubnamespaces");

	internal static string TooManyExceptionRegions => GetResourceString("TooManyExceptionRegions");

	internal static string SequencePointValueOutOfRange => GetResourceString("SequencePointValueOutOfRange");

	internal static string InvalidDirectoryRVA => GetResourceString("InvalidDirectoryRVA");

	internal static string InvalidDirectorySize => GetResourceString("InvalidDirectorySize");

	internal static string InvalidDebugDirectoryEntryCharacteristics => GetResourceString("InvalidDebugDirectoryEntryCharacteristics");

	internal static string UnexpectedCodeViewDataSignature => GetResourceString("UnexpectedCodeViewDataSignature");

	internal static string UnexpectedEmbeddedPortablePdbDataSignature => GetResourceString("UnexpectedEmbeddedPortablePdbDataSignature");

	internal static string InvalidPdbChecksumDataFormat => GetResourceString("InvalidPdbChecksumDataFormat");

	internal static string UnexpectedSignatureHeader => GetResourceString("UnexpectedSignatureHeader");

	internal static string UnexpectedSignatureHeader2 => GetResourceString("UnexpectedSignatureHeader2");

	internal static string NotTypeDefOrRefHandle => GetResourceString("NotTypeDefOrRefHandle");

	internal static string UnexpectedSignatureTypeCode => GetResourceString("UnexpectedSignatureTypeCode");

	internal static string SignatureTypeSequenceMustHaveAtLeastOneElement => GetResourceString("SignatureTypeSequenceMustHaveAtLeastOneElement");

	internal static string NotTypeDefOrRefOrSpecHandle => GetResourceString("NotTypeDefOrRefOrSpecHandle");

	internal static string UnexpectedDebugDirectoryType => GetResourceString("UnexpectedDebugDirectoryType");

	internal static string HeapSizeLimitExceeded => GetResourceString("HeapSizeLimitExceeded");

	internal static string BuilderMustAligned => GetResourceString("BuilderMustAligned");

	internal static string BuilderAlreadyLinked => GetResourceString("BuilderAlreadyLinked");

	internal static string ReturnedBuilderSizeTooSmall => GetResourceString("ReturnedBuilderSizeTooSmall");

	internal static string SignatureNotVarArg => GetResourceString("SignatureNotVarArg");

	internal static string LabelDoesntBelongToBuilder => GetResourceString("LabelDoesntBelongToBuilder");

	internal static string ControlFlowBuilderNotAvailable => GetResourceString("ControlFlowBuilderNotAvailable");

	internal static string BaseReaderMustBeFullMetadataReader => GetResourceString("BaseReaderMustBeFullMetadataReader");

	internal static string ModuleAlreadyAdded => GetResourceString("ModuleAlreadyAdded");

	internal static string AssemblyAlreadyAdded => GetResourceString("AssemblyAlreadyAdded");

	internal static string ExpectedListOfSize => GetResourceString("ExpectedListOfSize");

	internal static string ExpectedArrayOfSize => GetResourceString("ExpectedArrayOfSize");

	internal static string ExpectedNonEmptyList => GetResourceString("ExpectedNonEmptyList");

	internal static string ExpectedNonEmptyArray => GetResourceString("ExpectedNonEmptyArray");

	internal static string ExpectedNonEmptyString => GetResourceString("ExpectedNonEmptyString");

	internal static string ReadersMustBeDeltaReaders => GetResourceString("ReadersMustBeDeltaReaders");

	internal static string SignatureProviderReturnedInvalidSignature => GetResourceString("SignatureProviderReturnedInvalidSignature");

	internal static string UnknownSectionName => GetResourceString("UnknownSectionName");

	internal static string HashTooShort => GetResourceString("HashTooShort");

	internal static string UnexpectedArrayLength => GetResourceString("UnexpectedArrayLength");

	internal static string ValueMustBeMultiple => GetResourceString("ValueMustBeMultiple");

	internal static string MustNotReturnNull => GetResourceString("MustNotReturnNull");

	internal static string MetadataVersionTooLong => GetResourceString("MetadataVersionTooLong");

	internal static string RowCountMustBeZero => GetResourceString("RowCountMustBeZero");

	internal static string RowCountOutOfRange => GetResourceString("RowCountOutOfRange");

	internal static string SizeMismatch => GetResourceString("SizeMismatch");

	internal static string DataTooBig => GetResourceString("DataTooBig");

	internal static string UnsupportedFormatVersion => GetResourceString("UnsupportedFormatVersion");

	internal static string DistanceBetweenInstructionAndLabelTooBig => GetResourceString("DistanceBetweenInstructionAndLabelTooBig");

	internal static string LabelNotMarked => GetResourceString("LabelNotMarked");

	internal static string MethodHasNoExceptionRegions => GetResourceString("MethodHasNoExceptionRegions");

	internal static string InvalidExceptionRegionBounds => GetResourceString("InvalidExceptionRegionBounds");

	internal static string UnexpectedValue => GetResourceString("UnexpectedValue");

	internal static string UnexpectedValueUnknownType => GetResourceString("UnexpectedValueUnknownType");

	internal static string SwitchInstructionEncoderTooFewBranches => GetResourceString("SwitchInstructionEncoderTooFewBranches");

	internal static string SwitchInstructionEncoderTooManyBranches => GetResourceString("SwitchInstructionEncoderTooManyBranches");

	internal static string Argument_InvalidTypeName => GetResourceString("Argument_InvalidTypeName");

	internal static string InvalidOperation_MaxNodesExceeded => GetResourceString("InvalidOperation_MaxNodesExceeded");

	internal static string Argument_HasToBeArrayClass => GetResourceString("Argument_HasToBeArrayClass");

	internal static string InvalidOperation_NotGenericType => GetResourceString("InvalidOperation_NotGenericType");

	internal static string InvalidOperation_NotNestedType => GetResourceString("InvalidOperation_NotNestedType");

	internal static string InvalidOperation_NoElement => GetResourceString("InvalidOperation_NoElement");

	internal static string InvalidAssemblyName => GetResourceString("InvalidAssemblyName");

	internal static string Arg_NotSimpleTypeName => GetResourceString("Arg_NotSimpleTypeName");

	private static bool GetUsingResourceKeysSwitchValue()
	{
		if (!AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled))
		{
			return false;
		}
		return isEnabled;
	}

	internal static bool UsingResourceKeys()
	{
		return s_usingResourceKeys;
	}

	private static string GetResourceString(string resourceKey)
	{
		if (UsingResourceKeys())
		{
			return resourceKey;
		}
		string result = null;
		try
		{
			result = ResourceManager.GetString(resourceKey);
		}
		catch (MissingManifestResourceException)
		{
		}
		return result;
	}

	private static string GetResourceString(string resourceKey, string defaultString)
	{
		string resourceString = GetResourceString(resourceKey);
		if (!(resourceKey == resourceString) && resourceString != null)
		{
			return resourceString;
		}
		return defaultString;
	}

	internal static string Format(string resourceFormat, object? p1)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1);
		}
		return string.Format(resourceFormat, p1);
	}

	internal static string Format(string resourceFormat, object? p1, object? p2)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2);
		}
		return string.Format(resourceFormat, p1, p2);
	}

	internal static string Format(string resourceFormat, object? p1, object? p2, object? p3)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2, p3);
		}
		return string.Format(resourceFormat, p1, p2, p3);
	}

	internal static string Format(string resourceFormat, params object?[]? args)
	{
		if (args != null)
		{
			if (UsingResourceKeys())
			{
				return resourceFormat + ", " + string.Join(", ", args);
			}
			return string.Format(resourceFormat, args);
		}
		return resourceFormat;
	}

	internal static string Format(IFormatProvider? provider, string resourceFormat, object? p1)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1);
		}
		return string.Format(provider, resourceFormat, p1);
	}

	internal static string Format(IFormatProvider? provider, string resourceFormat, object? p1, object? p2)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2);
		}
		return string.Format(provider, resourceFormat, p1, p2);
	}

	internal static string Format(IFormatProvider? provider, string resourceFormat, object? p1, object? p2, object? p3)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2, p3);
		}
		return string.Format(provider, resourceFormat, p1, p2, p3);
	}

	internal static string Format(IFormatProvider? provider, string resourceFormat, params object?[]? args)
	{
		if (args != null)
		{
			if (UsingResourceKeys())
			{
				return resourceFormat + ", " + string.Join(", ", args);
			}
			return string.Format(provider, resourceFormat, args);
		}
		return resourceFormat;
	}
}
