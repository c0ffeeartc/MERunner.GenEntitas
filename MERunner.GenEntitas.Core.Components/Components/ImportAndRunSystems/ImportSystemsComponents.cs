using System;
using System.Collections.Generic;
using Entitas;
using Entitas.Generic;

namespace GenEntitas
{
public class SystemGuids
		: IComponent
		, ICompData
		, Scope<Settings>
		, IUnique
{
	public					List<Guid>			Values;
}

public class SystemsImportedComponent
		: IComponent
		, ICompData
		, Scope<Main>
		, IUnique
{
	public					List<ISystem>		Values;
}

public class SystemsOrderedComponent
		: IComponent
		, ICompData
		, Scope<Main>
		, IUnique
{
	public					List<ISystem>		Values;
}

}