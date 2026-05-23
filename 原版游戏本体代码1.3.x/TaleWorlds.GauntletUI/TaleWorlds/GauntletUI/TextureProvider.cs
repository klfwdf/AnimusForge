using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x02000031 RID: 49
	public abstract class TextureProvider
	{
		// Token: 0x17000101 RID: 257
		// (get) Token: 0x0600035C RID: 860 RVA: 0x0000EDE5 File Offset: 0x0000CFE5
		// (set) Token: 0x0600035D RID: 861 RVA: 0x0000EDED File Offset: 0x0000CFED
		public string SourceInfo { get; set; }

		// Token: 0x0600035E RID: 862 RVA: 0x0000EDF6 File Offset: 0x0000CFF6
		public virtual void SetTargetSize(int width, int height)
		{
		}

		// Token: 0x0600035F RID: 863 RVA: 0x0000EDF8 File Offset: 0x0000CFF8
		public Texture GetTextureForRender(TwoDimensionContext context, string name = null)
		{
			return this.OnGetTextureForRender(context, name);
		}

		// Token: 0x06000360 RID: 864
		protected abstract Texture OnGetTextureForRender(TwoDimensionContext twoDimensionContext, string name);

		// Token: 0x06000361 RID: 865 RVA: 0x0000EE02 File Offset: 0x0000D002
		public virtual void Tick(float dt)
		{
		}

		// Token: 0x06000362 RID: 866 RVA: 0x0000EE04 File Offset: 0x0000D004
		public virtual void Clear(bool clearNextFrame)
		{
			this._getGetMethodCache.Clear();
		}

		// Token: 0x06000363 RID: 867 RVA: 0x0000EE14 File Offset: 0x0000D014
		public void SetProperty(string name, object value)
		{
			PropertyInfo property = base.GetType().GetProperty(name);
			if (property != null)
			{
				property.GetSetMethod().Invoke(this, new object[] { value });
			}
		}

		// Token: 0x06000364 RID: 868 RVA: 0x0000EE50 File Offset: 0x0000D050
		public object GetProperty(string name)
		{
			MethodInfo methodInfo;
			if (this._getGetMethodCache.TryGetValue(name, out methodInfo))
			{
				return methodInfo.Invoke(this, null);
			}
			PropertyInfo property = base.GetType().GetProperty(name);
			if (property != null)
			{
				MethodInfo getMethod = property.GetGetMethod();
				this._getGetMethodCache.Add(name, getMethod);
				return getMethod.Invoke(this, null);
			}
			return null;
		}

		// Token: 0x040001A4 RID: 420
		private Dictionary<string, MethodInfo> _getGetMethodCache = new Dictionary<string, MethodInfo>();
	}
}
