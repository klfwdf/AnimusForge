using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal abstract class Dependency
{
	public Type ServiceType { get; set; }

	public string ServiceName { get; set; }

	public Delegate FactoryExpression { get; set; }

	public abstract string Name { get; }

	public bool IsRequired { get; set; }

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		return stringBuilder.AppendFormat("[Requested dependency: ServiceType:{0}, ServiceName:{1}]", ServiceType, ServiceName).ToString();
	}
}
