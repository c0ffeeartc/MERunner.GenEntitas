using System;
using System.Collections.Generic;
using Entitas;
using Entitas.Generic;
using Microsoft.CodeAnalysis;

namespace GenEntitas
{

public struct INamedTypeSymbolComponent : IComponent
		, ICompData
		, Scope<Main>
{
	// [PrimaryEntityIndex]  // TODO
	public					INamedTypeSymbol		Value;

	public INamedTypeSymbolComponent( INamedTypeSymbol value)
	{
		Value = value;
	}
}

public struct RoslynPathToSolution : IComponent
		, ICompData
		, Scope<Settings>
		, IUnique
{
	public					String					Value;

	public RoslynPathToSolution( String value)
	{
		Value = value;
	}
}

public struct RoslynAllTypes : IComponent
		, ICompData
		, Scope<Main>
		, IUnique
{
	public					List<INamedTypeSymbol>	Values;

	public RoslynAllTypes( List<INamedTypeSymbol> values)
	{
		Values = values;
	}
}

public struct RoslynComponentTypes : IComponent
		, ICompData
		, Scope<Main>
		, IUnique
{
	public					List<INamedTypeSymbol>	Values;

	public RoslynComponentTypes( List<INamedTypeSymbol> values)
	{
		Values = values;
	}
}

}