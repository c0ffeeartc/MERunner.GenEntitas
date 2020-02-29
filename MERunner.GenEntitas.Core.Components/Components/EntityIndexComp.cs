using System;
using System.Collections.Generic;
using Entitas;
using Entitas.CodeGeneration.Plugins;
using Entitas.Generic;

namespace GenEntitas
{

public struct EntityIndexComp
		: IComponent
		, ICompData
		, Scope<Main>
{
	public					List<EntityIndexInfo>	Values;
}

public struct CustomEntityIndexComp
		: IComponent
		, ICompData
		, Scope<Main>
{
	public					EntityIndexData			EntityIndexData;

	public CustomEntityIndexComp( EntityIndexData entityIndexData)
	{
		EntityIndexData = entityIndexData;
	}
}

public class EntityIndexInfo
{
	public					EntityIndexData			EntityIndexData;
	public					String					Type;
	public					Boolean					IsCustom;
	public					MethodData[]			CustomMethods;
	public					String					Name;
    public					String[]				ContextNames;
    public					String					ComponentType;
    public					String					MemberType;
	public					String					MemberName;
	public					Boolean					HasMultple;
	
}

}
