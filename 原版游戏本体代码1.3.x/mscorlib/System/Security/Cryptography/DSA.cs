using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Util;
using System.Text;

namespace System.Security.Cryptography
{
	// Token: 0x0200025D RID: 605
	[ComVisible(true)]
	public abstract class DSA : AsymmetricAlgorithm
	{
		// Token: 0x0600216B RID: 8555 RVA: 0x000764D2 File Offset: 0x000746D2
		public new static DSA Create()
		{
			return DSA.Create("System.Security.Cryptography.DSA");
		}

		// Token: 0x0600216C RID: 8556 RVA: 0x000764DE File Offset: 0x000746DE
		public new static DSA Create(string algName)
		{
			return (DSA)CryptoConfig.CreateFromName(algName);
		}

		// Token: 0x0600216D RID: 8557 RVA: 0x000764EC File Offset: 0x000746EC
		public static DSA Create(int keySizeInBits)
		{
			DSA dsa = (DSA)CryptoConfig.CreateFromName("DSA-FIPS186-3");
			dsa.KeySize = keySizeInBits;
			if (dsa.KeySize != keySizeInBits)
			{
				throw new CryptographicException();
			}
			return dsa;
		}

		// Token: 0x0600216E RID: 8558 RVA: 0x00076520 File Offset: 0x00074720
		public static DSA Create(DSAParameters parameters)
		{
			DSA dsa = (DSA)CryptoConfig.CreateFromName("DSA-FIPS186-3");
			dsa.ImportParameters(parameters);
			return dsa;
		}

		// Token: 0x0600216F RID: 8559
		public abstract byte[] CreateSignature(byte[] rgbHash);

		// Token: 0x06002170 RID: 8560
		public abstract bool VerifySignature(byte[] rgbHash, byte[] rgbSignature);

		// Token: 0x06002171 RID: 8561 RVA: 0x00076545 File Offset: 0x00074745
		protected virtual byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
		{
			throw DSA.DerivedClassMustOverride();
		}

		// Token: 0x06002172 RID: 8562 RVA: 0x0007654C File Offset: 0x0007474C
		protected virtual byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
		{
			throw DSA.DerivedClassMustOverride();
		}

		// Token: 0x06002173 RID: 8563 RVA: 0x00076553 File Offset: 0x00074753
		public byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			return this.SignData(data, 0, data.Length, hashAlgorithm);
		}

		// Token: 0x06002174 RID: 8564 RVA: 0x00076570 File Offset: 0x00074770
		public virtual byte[] SignData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			if (offset < 0 || offset > data.Length)
			{
				throw new ArgumentOutOfRangeException("offset");
			}
			if (count < 0 || count > data.Length - offset)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			if (string.IsNullOrEmpty(hashAlgorithm.Name))
			{
				throw DSA.HashAlgorithmNameNullOrEmpty();
			}
			byte[] rgbHash = this.HashData(data, offset, count, hashAlgorithm);
			return this.CreateSignature(rgbHash);
		}

		// Token: 0x06002175 RID: 8565 RVA: 0x000765E0 File Offset: 0x000747E0
		public virtual byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			if (string.IsNullOrEmpty(hashAlgorithm.Name))
			{
				throw DSA.HashAlgorithmNameNullOrEmpty();
			}
			byte[] rgbHash = this.HashData(data, hashAlgorithm);
			return this.CreateSignature(rgbHash);
		}

		// Token: 0x06002176 RID: 8566 RVA: 0x0007661F File Offset: 0x0007481F
		public bool VerifyData(byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			return this.VerifyData(data, 0, data.Length, signature, hashAlgorithm);
		}

		// Token: 0x06002177 RID: 8567 RVA: 0x0007663C File Offset: 0x0007483C
		public virtual bool VerifyData(byte[] data, int offset, int count, byte[] signature, HashAlgorithmName hashAlgorithm)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			if (offset < 0 || offset > data.Length)
			{
				throw new ArgumentOutOfRangeException("offset");
			}
			if (count < 0 || count > data.Length - offset)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			if (signature == null)
			{
				throw new ArgumentNullException("signature");
			}
			if (string.IsNullOrEmpty(hashAlgorithm.Name))
			{
				throw DSA.HashAlgorithmNameNullOrEmpty();
			}
			byte[] rgbHash = this.HashData(data, offset, count, hashAlgorithm);
			return this.VerifySignature(rgbHash, signature);
		}

		// Token: 0x06002178 RID: 8568 RVA: 0x000766BC File Offset: 0x000748BC
		public virtual bool VerifyData(Stream data, byte[] signature, HashAlgorithmName hashAlgorithm)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			if (signature == null)
			{
				throw new ArgumentNullException("signature");
			}
			if (string.IsNullOrEmpty(hashAlgorithm.Name))
			{
				throw DSA.HashAlgorithmNameNullOrEmpty();
			}
			byte[] rgbHash = this.HashData(data, hashAlgorithm);
			return this.VerifySignature(rgbHash, signature);
		}

		// Token: 0x06002179 RID: 8569 RVA: 0x0007670C File Offset: 0x0007490C
		public override void FromXmlString(string xmlString)
		{
			if (xmlString == null)
			{
				throw new ArgumentNullException("xmlString");
			}
			DSAParameters parameters = default(DSAParameters);
			Parser parser = new Parser(xmlString);
			SecurityElement topElement = parser.GetTopElement();
			string text = topElement.SearchForTextOfLocalName("P");
			if (text == null)
			{
				throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidFromXmlString", new object[] { "DSA", "P" }));
			}
			parameters.P = Convert.FromBase64String(Utils.DiscardWhiteSpaces(text));
			string text2 = topElement.SearchForTextOfLocalName("Q");
			if (text2 == null)
			{
				throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidFromXmlString", new object[] { "DSA", "Q" }));
			}
			parameters.Q = Convert.FromBase64String(Utils.DiscardWhiteSpaces(text2));
			string text3 = topElement.SearchForTextOfLocalName("G");
			if (text3 == null)
			{
				throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidFromXmlString", new object[] { "DSA", "G" }));
			}
			parameters.G = Convert.FromBase64String(Utils.DiscardWhiteSpaces(text3));
			string text4 = topElement.SearchForTextOfLocalName("Y");
			if (text4 == null)
			{
				throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidFromXmlString", new object[] { "DSA", "Y" }));
			}
			parameters.Y = Convert.FromBase64String(Utils.DiscardWhiteSpaces(text4));
			string text5 = topElement.SearchForTextOfLocalName("J");
			if (text5 != null)
			{
				parameters.J = Convert.FromBase64String(Utils.DiscardWhiteSpaces(text5));
			}
			string text6 = topElement.SearchForTextOfLocalName("X");
			if (text6 != null)
			{
				parameters.X = Convert.FromBase64String(Utils.DiscardWhiteSpaces(text6));
			}
			string text7 = topElement.SearchForTextOfLocalName("Seed");
			string text8 = topElement.SearchForTextOfLocalName("PgenCounter");
			if (text7 != null && text8 != null)
			{
				parameters.Seed = Convert.FromBase64String(Utils.DiscardWhiteSpaces(text7));
				parameters.Counter = Utils.ConvertByteArrayToInt(Convert.FromBase64String(Utils.DiscardWhiteSpaces(text8)));
			}
			else if (text7 != null || text8 != null)
			{
				if (text7 == null)
				{
					throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidFromXmlString", new object[] { "DSA", "Seed" }));
				}
				throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidFromXmlString", new object[] { "DSA", "PgenCounter" }));
			}
			this.ImportParameters(parameters);
		}

		// Token: 0x0600217A RID: 8570 RVA: 0x00076958 File Offset: 0x00074B58
		public override string ToXmlString(bool includePrivateParameters)
		{
			DSAParameters dsaparameters = this.ExportParameters(includePrivateParameters);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("<DSAKeyValue>");
			stringBuilder.Append("<P>" + Convert.ToBase64String(dsaparameters.P) + "</P>");
			stringBuilder.Append("<Q>" + Convert.ToBase64String(dsaparameters.Q) + "</Q>");
			stringBuilder.Append("<G>" + Convert.ToBase64String(dsaparameters.G) + "</G>");
			stringBuilder.Append("<Y>" + Convert.ToBase64String(dsaparameters.Y) + "</Y>");
			if (dsaparameters.J != null)
			{
				stringBuilder.Append("<J>" + Convert.ToBase64String(dsaparameters.J) + "</J>");
			}
			if (dsaparameters.Seed != null)
			{
				stringBuilder.Append("<Seed>" + Convert.ToBase64String(dsaparameters.Seed) + "</Seed>");
				stringBuilder.Append("<PgenCounter>" + Convert.ToBase64String(Utils.ConvertIntToByteArray(dsaparameters.Counter)) + "</PgenCounter>");
			}
			if (includePrivateParameters)
			{
				stringBuilder.Append("<X>" + Convert.ToBase64String(dsaparameters.X) + "</X>");
			}
			stringBuilder.Append("</DSAKeyValue>");
			return stringBuilder.ToString();
		}

		// Token: 0x0600217B RID: 8571
		public abstract DSAParameters ExportParameters(bool includePrivateParameters);

		// Token: 0x0600217C RID: 8572
		public abstract void ImportParameters(DSAParameters parameters);

		// Token: 0x0600217D RID: 8573 RVA: 0x00076AB1 File Offset: 0x00074CB1
		private static Exception DerivedClassMustOverride()
		{
			return new NotImplementedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
		}

		// Token: 0x0600217E RID: 8574 RVA: 0x00076AC2 File Offset: 0x00074CC2
		internal static Exception HashAlgorithmNameNullOrEmpty()
		{
			return new ArgumentException(Environment.GetResourceString("Cryptography_HashAlgorithmNameNullOrEmpty"), "hashAlgorithm");
		}
	}
}
