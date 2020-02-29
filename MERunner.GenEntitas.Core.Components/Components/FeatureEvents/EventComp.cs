using System;
using System.Collections.Generic;
using Entitas;
using Entitas.CodeGeneration.Attributes;
using Entitas.Generic;

namespace GenEntitas
{

public struct EventComp
		: IComponent
		, ICompData
		, Scope<Main>
{
	public					List<EventInfo>			Values;

	public EventComp( List<EventInfo> values)
	{
		Values = values;
	}
}

public class EventInfo
{
	public					EventInfo				( EventTarget eventTarget, EventType eventType, int priority )
	{
		EventTarget			= eventTarget;
		EventType			= eventType;
		Priority			= priority;
	}

	public					EventTarget				EventTarget;
	public					EventType				EventType;
	public					Int32					Priority;
}

public class EventListenerComp
		: IComponent
		, ICompFlag
		, Scope<Main>
{
}
}
