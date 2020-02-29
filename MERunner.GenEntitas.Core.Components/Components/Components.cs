using System;
using System.Collections.Generic;
using Entitas;
using Entitas.Generic;

namespace GenEntitas
{

public struct Comp
		: IComponent
		, ICompData
		, Scope<Main>
{
	public					String					Name;
	// [PrimaryEntityIndex]  // TODO
	public					String					FullTypeName;

	public Comp( String name, String fullTypeName)
	{
		Name = name;
		FullTypeName = fullTypeName;
	}
}

public struct ContextComp
		: IComponent
		, ICompData
		, Scope<Main>
{
	public					String					Name;

	public ContextComp( String name)
	{
		Name = name;
	}
}

public sealed class AlreadyImplementedComp
		: IComponent
		, ICompFlag
		, Scope<Main>
{
}

public struct ContextNamesComp
		: IComponent
		, ICompData
		, Scope<Main>
{
	public					List<String>			Values;

	public ContextNamesComp( List<String> values)
	{
		Values = values;
	}
}


public sealed class DontGenerateComp
		: IComponent
		, ICompFlag
		, Scope<Main>
{
	//TODO
	//public					Boolean					GenerateIndex;
}

public struct GeneratedFileComp
		: IComponent
		, ICompData
		, Scope<Main>
{
	public					String					FilePath;
	public					String					Contents;
	public					String					GeneratedBy;

	public GeneratedFileComp( String filePath, String contents, String generatedBy)
	{
		FilePath = filePath;
		Contents = contents;
		GeneratedBy = generatedBy;
	}
}

public struct NonIComp
		: IComponent
		, ICompData
		, Scope<Main>
{
	public					String					FullCompName;
	public					String					FieldTypeName;

	public NonIComp( String fullCompName, String fieldTypeName)
	{
		FullCompName = fullCompName;
		FieldTypeName = fieldTypeName;
	}
}

public sealed class UniqueComp
		: IComponent
		, ICompFlag
		, Scope<Main>
{
}

public struct UniquePrefixComp
		: IComponent
		, ICompData
		, Scope<Main>
{
	public					String					Value;

	public UniquePrefixComp( String value)
	{
		Value = value;
	}
}

public sealed class GenCompEntApiInterface_ForSingleContext
		: IComponent
		, ICompFlag
		, Scope<Main>
{
}

public sealed class Destroy
		: IComponent
		, ICompFlag
		, Scope<Main>
		, Scope<Settings>
{
}
}