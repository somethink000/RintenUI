using System.Reflection;

namespace Rinten
{
	[AttributeUsage( AttributeTargets.Method )]
	public sealed partial class PureAttribute : Attribute
	{

	}

	[AttributeUsage( AttributeTargets.Method )]
	public sealed partial class ImpureAttribute : Attribute
	{

	}

	[AttributeUsage( AttributeTargets.Parameter )]
	public sealed partial class ActionGraphTargetAttribute : Attribute
	{

	}

	/// <summary>In ActionGraph, this type parameter can only be satisfied by a type <c>TArg</c>, such that there exists at least one non-abstract type that extends / implements both <c>TArg</c> and <see cref="BaseType"/>.</summary>
	[AttributeUsage( AttributeTargets.GenericParameter, AllowMultiple = true )]
	public sealed class HasImplementationAttribute : Attribute
	{
		/// <summary>Base class or interface for which there must exist an extending / implementing type.</summary>
		public Type BaseType { get; }

		public HasImplementationAttribute( Type baseType )
		{
			BaseType = baseType;
		}
	}

	/// <summary>In ActionGraph, this parameter should only be configurable in the inspector as a property and not have a dedicated input.</summary>
	[AttributeUsage( AttributeTargets.Parameter )]
	public sealed partial class ActionGraphPropertyAttribute : Attribute
	{

	}

	[AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Constructor )]
	public sealed class ActionGraphIgnoreAttribute : Attribute
	{

	}

	/// <summary>Don't cache instances of this type when serializing action graph references, force them to be always serialized separately. We need this for component / game object references so we can update IDs when duplicating objects / instantiating prefabs.</summary>
	[AttributeUsage( AttributeTargets.Struct | AttributeTargets.Class )]
	public sealed partial class ActionGraphExposeWhenCachedAttribute : Attribute
	{

	}

	/// <summary>Makes a method, property or constructor a node in action graphs, under this identifier.</summary>
	[AttributeUsage( AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Constructor )]
	public partial class ActionGraphNodeAttribute : Attribute
	{
		public string Identifier { get; }

		public bool DefaultInputSignal { get; set; } = true;

		public bool DefaultOutputSignal { get; set; } = true;

		public bool InheritAsync { get; set; } = false;

		public ActionGraphNodeAttribute( string identifier )
		{
			Identifier = identifier;
		}
	}

	/// <summary>Display this node as an operator, with no header or socket labels, and a big icon in the middle.</summary>

	[AttributeUsage( AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Constructor )]
	public sealed class ActionGraphOperatorAttribute : Attribute
	{

	}



	[AttributeUsage( AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Constructor )]
	public sealed class ActionGraphIncludeAttribute : Attribute
	{
		/// <summary>If true, double-clicking on an output of the declaring type will auto-expand this member.</summary>
		public bool AutoExpand { get; set; }
	}

	/// <summary>Force a delegate-type property to only have a single attached Action Graph.</summary>
	[AttributeUsage( AttributeTargets.Field | AttributeTargets.Property )]
	public sealed class SingleActionAttribute : Attribute
	{

	}
}
