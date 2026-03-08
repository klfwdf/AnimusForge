using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Security.Util;

namespace System.Security.Policy
{
	// Token: 0x02000375 RID: 885
	[ComVisible(true)]
	[Serializable]
	public sealed class Hash : EvidenceBase, ISerializable
	{
		// Token: 0x06002BD1 RID: 11217 RVA: 0x000A3114 File Offset: 0x000A1314
		[SecurityCritical]
		internal Hash(SerializationInfo info, StreamingContext context)
		{
			Dictionary<Type, byte[]> dictionary = info.GetValueNoThrow("Hashes", typeof(Dictionary<Type, byte[]>)) as Dictionary<Type, byte[]>;
			if (dictionary != null)
			{
				this.m_hashes = dictionary;
				return;
			}
			this.m_hashes = new Dictionary<Type, byte[]>();
			byte[] array = info.GetValueNoThrow("Md5", typeof(byte[])) as byte[];
			if (array != null)
			{
				this.m_hashes[typeof(MD5)] = array;
			}
			byte[] array2 = info.GetValueNoThrow("Sha1", typeof(byte[])) as byte[];
			if (array2 != null)
			{
				this.m_hashes[typeof(SHA1)] = array2;
			}
			byte[] array3 = info.GetValueNoThrow("RawData", typeof(byte[])) as byte[];
			if (array3 != null)
			{
				this.GenerateDefaultHashes(array3);
			}
		}

		// Token: 0x06002BD2 RID: 11218 RVA: 0x000A31E8 File Offset: 0x000A13E8
		public Hash(Assembly assembly)
		{
			if (assembly == null)
			{
				throw new ArgumentNullException("assembly");
			}
			if (assembly.IsDynamic)
			{
				throw new ArgumentException(Environment.GetResourceString("Security_CannotGenerateHash"), "assembly");
			}
			this.m_hashes = new Dictionary<Type, byte[]>();
			this.m_assembly = assembly as RuntimeAssembly;
			if (this.m_assembly == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeAssembly"), "assembly");
			}
		}

		// Token: 0x06002BD3 RID: 11219 RVA: 0x000A3266 File Offset: 0x000A1466
		private Hash(Hash hash)
		{
			this.m_assembly = hash.m_assembly;
			this.m_rawData = hash.m_rawData;
			this.m_hashes = new Dictionary<Type, byte[]>(hash.m_hashes);
		}

		// Token: 0x06002BD4 RID: 11220 RVA: 0x000A3298 File Offset: 0x000A1498
		private Hash(Type hashType, byte[] hashValue)
		{
			this.m_hashes = new Dictionary<Type, byte[]>();
			byte[] array = new byte[hashValue.Length];
			Array.Copy(hashValue, array, array.Length);
			this.m_hashes[hashType] = hashValue;
		}

		// Token: 0x06002BD5 RID: 11221 RVA: 0x000A32D6 File Offset: 0x000A14D6
		public static Hash CreateSHA1(byte[] sha1)
		{
			if (sha1 == null)
			{
				throw new ArgumentNullException("sha1");
			}
			return new Hash(typeof(SHA1), sha1);
		}

		// Token: 0x06002BD6 RID: 11222 RVA: 0x000A32F6 File Offset: 0x000A14F6
		public static Hash CreateSHA256(byte[] sha256)
		{
			if (sha256 == null)
			{
				throw new ArgumentNullException("sha256");
			}
			return new Hash(typeof(SHA256), sha256);
		}

		// Token: 0x06002BD7 RID: 11223 RVA: 0x000A3316 File Offset: 0x000A1516
		public static Hash CreateMD5(byte[] md5)
		{
			if (md5 == null)
			{
				throw new ArgumentNullException("md5");
			}
			return new Hash(typeof(MD5), md5);
		}

		// Token: 0x06002BD8 RID: 11224 RVA: 0x000A3336 File Offset: 0x000A1536
		public override EvidenceBase Clone()
		{
			return new Hash(this);
		}

		// Token: 0x06002BD9 RID: 11225 RVA: 0x000A333E File Offset: 0x000A153E
		[OnSerializing]
		private void OnSerializing(StreamingContext ctx)
		{
			this.GenerateDefaultHashes();
		}

		// Token: 0x06002BDA RID: 11226 RVA: 0x000A3348 File Offset: 0x000A1548
		[SecurityCritical]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			this.GenerateDefaultHashes();
			byte[] value;
			if (this.m_hashes.TryGetValue(typeof(MD5), out value))
			{
				info.AddValue("Md5", value);
			}
			byte[] value2;
			if (this.m_hashes.TryGetValue(typeof(SHA1), out value2))
			{
				info.AddValue("Sha1", value2);
			}
			info.AddValue("RawData", null);
			info.AddValue("PEFile", IntPtr.Zero);
			info.AddValue("Hashes", this.m_hashes);
		}

		// Token: 0x170005D6 RID: 1494
		// (get) Token: 0x06002BDB RID: 11227 RVA: 0x000A33D8 File Offset: 0x000A15D8
		public byte[] SHA1
		{
			get
			{
				byte[] array = null;
				if (!this.m_hashes.TryGetValue(typeof(SHA1), out array))
				{
					array = this.GenerateHash(Hash.GetDefaultHashImplementationOrFallback(typeof(SHA1), typeof(SHA1)));
				}
				byte[] array2 = new byte[array.Length];
				Array.Copy(array, array2, array2.Length);
				return array2;
			}
		}

		// Token: 0x170005D7 RID: 1495
		// (get) Token: 0x06002BDC RID: 11228 RVA: 0x000A3434 File Offset: 0x000A1634
		public byte[] SHA256
		{
			get
			{
				byte[] array = null;
				if (!this.m_hashes.TryGetValue(typeof(SHA256), out array))
				{
					array = this.GenerateHash(Hash.GetDefaultHashImplementationOrFallback(typeof(SHA256), typeof(SHA256)));
				}
				byte[] array2 = new byte[array.Length];
				Array.Copy(array, array2, array2.Length);
				return array2;
			}
		}

		// Token: 0x170005D8 RID: 1496
		// (get) Token: 0x06002BDD RID: 11229 RVA: 0x000A3490 File Offset: 0x000A1690
		public byte[] MD5
		{
			get
			{
				byte[] array = null;
				if (!this.m_hashes.TryGetValue(typeof(MD5), out array))
				{
					array = this.GenerateHash(Hash.GetDefaultHashImplementationOrFallback(typeof(MD5), typeof(MD5)));
				}
				byte[] array2 = new byte[array.Length];
				Array.Copy(array, array2, array2.Length);
				return array2;
			}
		}

		// Token: 0x06002BDE RID: 11230 RVA: 0x000A34EC File Offset: 0x000A16EC
		public byte[] GenerateHash(HashAlgorithm hashAlg)
		{
			if (hashAlg == null)
			{
				throw new ArgumentNullException("hashAlg");
			}
			byte[] array = this.GenerateHash(hashAlg.GetType());
			byte[] array2 = new byte[array.Length];
			Array.Copy(array, array2, array2.Length);
			return array2;
		}

		// Token: 0x06002BDF RID: 11231 RVA: 0x000A3528 File Offset: 0x000A1728
		private byte[] GenerateHash(Type hashType)
		{
			Type hashIndexType = Hash.GetHashIndexType(hashType);
			byte[] array = null;
			if (!this.m_hashes.TryGetValue(hashIndexType, out array))
			{
				if (this.m_assembly == null)
				{
					throw new InvalidOperationException(Environment.GetResourceString("Security_CannotGenerateHash"));
				}
				array = Hash.GenerateHash(hashType, this.GetRawData());
				this.m_hashes[hashIndexType] = array;
			}
			return array;
		}

		// Token: 0x06002BE0 RID: 11232 RVA: 0x000A3588 File Offset: 0x000A1788
		private static byte[] GenerateHash(Type hashType, byte[] assemblyBytes)
		{
			byte[] result;
			using (HashAlgorithm hashAlgorithm = HashAlgorithm.Create(hashType.FullName))
			{
				result = hashAlgorithm.ComputeHash(assemblyBytes);
			}
			return result;
		}

		// Token: 0x06002BE1 RID: 11233 RVA: 0x000A35C8 File Offset: 0x000A17C8
		private void GenerateDefaultHashes()
		{
			if (this.m_assembly != null)
			{
				this.GenerateDefaultHashes(this.GetRawData());
			}
		}

		// Token: 0x06002BE2 RID: 11234 RVA: 0x000A35E4 File Offset: 0x000A17E4
		private void GenerateDefaultHashes(byte[] assemblyBytes)
		{
			Type[] array = new Type[]
			{
				Hash.GetHashIndexType(typeof(SHA1)),
				Hash.GetHashIndexType(typeof(SHA256)),
				Hash.GetHashIndexType(typeof(MD5))
			};
			foreach (Type type in array)
			{
				Type defaultHashImplementation = Hash.GetDefaultHashImplementation(type);
				if (defaultHashImplementation != null && !this.m_hashes.ContainsKey(type))
				{
					this.m_hashes[type] = Hash.GenerateHash(defaultHashImplementation, assemblyBytes);
				}
			}
		}

		// Token: 0x06002BE3 RID: 11235 RVA: 0x000A3678 File Offset: 0x000A1878
		private static Type GetDefaultHashImplementationOrFallback(Type hashAlgorithm, Type fallbackImplementation)
		{
			Type defaultHashImplementation = Hash.GetDefaultHashImplementation(hashAlgorithm);
			if (!(defaultHashImplementation != null))
			{
				return fallbackImplementation;
			}
			return defaultHashImplementation;
		}

		// Token: 0x06002BE4 RID: 11236 RVA: 0x000A3698 File Offset: 0x000A1898
		private static Type GetDefaultHashImplementation(Type hashAlgorithm)
		{
			if (hashAlgorithm.IsAssignableFrom(typeof(MD5)))
			{
				if (!CryptoConfig.AllowOnlyFipsAlgorithms)
				{
					return typeof(MD5CryptoServiceProvider);
				}
				return null;
			}
			else
			{
				if (hashAlgorithm.IsAssignableFrom(typeof(SHA256)))
				{
					return Type.GetType("System.Security.Cryptography.SHA256CryptoServiceProvider, System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				}
				return hashAlgorithm;
			}
		}

		// Token: 0x06002BE5 RID: 11237 RVA: 0x000A36EC File Offset: 0x000A18EC
		private static Type GetHashIndexType(Type hashType)
		{
			Type type = hashType;
			while (type != null && type.BaseType != typeof(HashAlgorithm))
			{
				type = type.BaseType;
			}
			if (type == null)
			{
				type = typeof(HashAlgorithm);
			}
			return type;
		}

		// Token: 0x06002BE6 RID: 11238 RVA: 0x000A373C File Offset: 0x000A193C
		private byte[] GetRawData()
		{
			byte[] array = null;
			if (this.m_assembly != null)
			{
				if (this.m_rawData != null)
				{
					array = this.m_rawData.Target as byte[];
				}
				if (array == null)
				{
					array = this.m_assembly.GetRawBytes();
					this.m_rawData = new WeakReference(array);
				}
			}
			return array;
		}

		// Token: 0x06002BE7 RID: 11239 RVA: 0x000A3790 File Offset: 0x000A1990
		private SecurityElement ToXml()
		{
			this.GenerateDefaultHashes();
			SecurityElement securityElement = new SecurityElement("System.Security.Policy.Hash");
			securityElement.AddAttribute("version", "2");
			foreach (KeyValuePair<Type, byte[]> keyValuePair in this.m_hashes)
			{
				SecurityElement securityElement2 = new SecurityElement("hash");
				securityElement2.AddAttribute("algorithm", keyValuePair.Key.Name);
				securityElement2.AddAttribute("value", Hex.EncodeHexString(keyValuePair.Value));
				securityElement.AddChild(securityElement2);
			}
			return securityElement;
		}

		// Token: 0x06002BE8 RID: 11240 RVA: 0x000A3840 File Offset: 0x000A1A40
		public override string ToString()
		{
			return this.ToXml().ToString();
		}

		// Token: 0x040011B1 RID: 4529
		private RuntimeAssembly m_assembly;

		// Token: 0x040011B2 RID: 4530
		private Dictionary<Type, byte[]> m_hashes;

		// Token: 0x040011B3 RID: 4531
		private WeakReference m_rawData;
	}
}
