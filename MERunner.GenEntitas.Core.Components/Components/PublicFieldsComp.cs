using System;
using System.Collections.Generic;
using Entitas;
using Entitas.Generic;

namespace GenEntitas
{

public struct PublicFieldsComp
		: IComponent
		, ICompData
		, Scope<Main>
{
	public					List<FieldInfo>			Values;

	public PublicFieldsComp( List<FieldInfo> values)
	{
		Values = values;
	}
}

[Serializable]
public partial class FieldInfo
{
	public					FieldInfo				( String typeName, String fieldName )
	{
		TypeName			= typeName;
		FieldName			= fieldName;
	}

	public					String					TypeName;
	public					String					FieldName;
}

public partial class FieldInfo
{
	public					EntityIndexInfo			EntityIndexInfo;
}

}
