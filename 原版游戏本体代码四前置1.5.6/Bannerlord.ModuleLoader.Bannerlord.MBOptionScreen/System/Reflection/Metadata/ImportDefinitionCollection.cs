using System.Collections;
using System.Collections.Generic;
using System.Reflection.Internal;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata;

internal readonly struct ImportDefinitionCollection : IEnumerable<ImportDefinition>, IEnumerable
{
	public struct Enumerator : IEnumerator<ImportDefinition>, IDisposable, IEnumerator
	{
		private System.Reflection.Metadata.BlobReader _reader;

		private ImportDefinition _current;

		public ImportDefinition Current => _current;

		object IEnumerator.Current => _current;

		internal Enumerator(System.Reflection.Internal.MemoryBlock block)
		{
			_reader = new System.Reflection.Metadata.BlobReader(block);
			_current = default(ImportDefinition);
		}

		public bool MoveNext()
		{
			if (_reader.RemainingBytes == 0)
			{
				return false;
			}
			ImportDefinitionKind importDefinitionKind = (ImportDefinitionKind)_reader.ReadByte();
			switch (importDefinitionKind)
			{
			case ImportDefinitionKind.ImportType:
			{
				System.Reflection.Metadata.Handle typeOrNamespace = _reader.ReadTypeHandle();
				_current = new ImportDefinition(importDefinitionKind, default(System.Reflection.Metadata.BlobHandle), default(AssemblyReferenceHandle), typeOrNamespace);
				break;
			}
			case ImportDefinitionKind.ImportNamespace:
			{
				System.Reflection.Metadata.Handle typeOrNamespace = System.Reflection.Metadata.Ecma335.MetadataTokens.BlobHandle(_reader.ReadCompressedInteger());
				_current = new ImportDefinition(importDefinitionKind, default(System.Reflection.Metadata.BlobHandle), default(AssemblyReferenceHandle), typeOrNamespace);
				break;
			}
			case ImportDefinitionKind.ImportAssemblyNamespace:
			{
				AssemblyReferenceHandle assembly = System.Reflection.Metadata.Ecma335.MetadataTokens.AssemblyReferenceHandle(_reader.ReadCompressedInteger());
				System.Reflection.Metadata.Handle typeOrNamespace = System.Reflection.Metadata.Ecma335.MetadataTokens.BlobHandle(_reader.ReadCompressedInteger());
				_current = new ImportDefinition(importDefinitionKind, default(System.Reflection.Metadata.BlobHandle), assembly, typeOrNamespace);
				break;
			}
			case ImportDefinitionKind.ImportAssemblyReferenceAlias:
				_current = new ImportDefinition(importDefinitionKind, System.Reflection.Metadata.Ecma335.MetadataTokens.BlobHandle(_reader.ReadCompressedInteger()));
				break;
			case ImportDefinitionKind.AliasAssemblyReference:
				_current = new ImportDefinition(importDefinitionKind, System.Reflection.Metadata.Ecma335.MetadataTokens.BlobHandle(_reader.ReadCompressedInteger()), System.Reflection.Metadata.Ecma335.MetadataTokens.AssemblyReferenceHandle(_reader.ReadCompressedInteger()));
				break;
			case ImportDefinitionKind.AliasType:
			{
				System.Reflection.Metadata.BlobHandle alias2 = System.Reflection.Metadata.Ecma335.MetadataTokens.BlobHandle(_reader.ReadCompressedInteger());
				System.Reflection.Metadata.Handle typeOrNamespace = _reader.ReadTypeHandle();
				_current = new ImportDefinition(importDefinitionKind, alias2, default(AssemblyReferenceHandle), typeOrNamespace);
				break;
			}
			case ImportDefinitionKind.ImportXmlNamespace:
			case ImportDefinitionKind.AliasNamespace:
			{
				System.Reflection.Metadata.BlobHandle alias = System.Reflection.Metadata.Ecma335.MetadataTokens.BlobHandle(_reader.ReadCompressedInteger());
				System.Reflection.Metadata.Handle typeOrNamespace = System.Reflection.Metadata.Ecma335.MetadataTokens.BlobHandle(_reader.ReadCompressedInteger());
				_current = new ImportDefinition(importDefinitionKind, alias, default(AssemblyReferenceHandle), typeOrNamespace);
				break;
			}
			case ImportDefinitionKind.AliasAssemblyNamespace:
				_current = new ImportDefinition(importDefinitionKind, System.Reflection.Metadata.Ecma335.MetadataTokens.BlobHandle(_reader.ReadCompressedInteger()), System.Reflection.Metadata.Ecma335.MetadataTokens.AssemblyReferenceHandle(_reader.ReadCompressedInteger()), System.Reflection.Metadata.Ecma335.MetadataTokens.BlobHandle(_reader.ReadCompressedInteger()));
				break;
			default:
				throw new BadImageFormatException(System.SR.Format(System.SR.InvalidImportDefinitionKind, importDefinitionKind));
			}
			return true;
		}

		public void Reset()
		{
			_reader.Reset();
			_current = default(ImportDefinition);
		}

		void IDisposable.Dispose()
		{
		}
	}

	private readonly System.Reflection.Internal.MemoryBlock _block;

	internal ImportDefinitionCollection(System.Reflection.Internal.MemoryBlock block)
	{
		_block = block;
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(_block);
	}

	IEnumerator<ImportDefinition> IEnumerable<ImportDefinition>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
