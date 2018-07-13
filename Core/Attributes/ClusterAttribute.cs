using System;

namespace Postulate.Orm.Attributes
{
	public enum ClusterOption
	{
		PrimaryKey,
		Identity
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class ClusterAttribute : Attribute
	{
		private readonly ClusterOption _option;

		public ClusterAttribute(ClusterOption option)
		{
			_option = option;
		}

		public ClusterOption Option { get { return _option; } }

		public string Syntax(ClusterOption option)
		{
			return (option == _option) ? "CLUSTERED " : string.Empty;
		}
	}
}