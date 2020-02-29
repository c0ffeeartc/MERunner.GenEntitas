using System;
using System.Collections.Generic;
using Entitas;
using Entitas.Generic;

namespace GenEntitas
{

public struct AssemblyResolvePaths : IComponent
		, ICompData
		, Scope<Settings>
		, IUnique
{
	public					List<String>			Value;
}

public struct WriteGeneratedPathsToCsProj : IComponent
		, ICompData
		, Scope<Settings>
		, IUnique
{
	public					String					Value;

	public WriteGeneratedPathsToCsProj( String value)
	{
		Value = value;
	}
}

public class IgnoreNamespaces : IComponent
		, ICompFlag
		, Scope<Settings>
		, IUnique
{
}

public class RunInDryMode : IComponent
		, ICompFlag
		, Scope<Settings>
		, IUnique
{
}

public class LogGeneratedPaths : IComponent
		, ICompFlag
		, Scope<Settings>
		, IUnique
{
}

public struct GeneratePath : IComponent
		, ICompData
		, Scope<Settings>
		, IUnique
{
	public					String					Value;

	public GeneratePath( String value)
	{
		Value = value;
	}
}

public struct GeneratedNamespace : IComponent
		, ICompData
		, Scope<Settings>
		, IUnique
{
	public					String					Value;

	public GeneratedNamespace( String value)
	{
		Value = value;
	}
}

}