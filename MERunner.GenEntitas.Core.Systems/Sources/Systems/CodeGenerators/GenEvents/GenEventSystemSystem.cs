﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using DesperateDevs.Utils;
using Entitas;
using Entitas.CodeGeneration.Attributes;
using Entitas.Generic;
using MERunner;
using Ent = Entitas.Generic.Entity<Main>;

namespace GenEntitas
{
[Export(typeof(ISystem_Factory))]
public sealed class Factory_GenEventSystemSystem : TSystem_Factory<GenEventSystemSystem> {  }

	[Guid("9D74C6E5-14D2-4678-A81D-C916C008C8D6")]
	public class GenEventSystemSystem : ReactiveSystem<Ent>
	{
		public				GenEventSystemSystem	( Contexts contexts ) : base( contexts.Get<Main>() )
		{
			_contexts			= contexts;
		}

		private				Contexts				_contexts;
		private const		String					TEMPLATE_ANY			=
            @"public sealed class ${Event}EventSystem : Entitas.ReactiveSystem<${EntityType}> {

    readonly Entitas.IGroup<${EntityType}> _listeners;
    readonly System.Collections.Generic.List<${EntityType}> _entityBuffer;
    readonly System.Collections.Generic.List<I${EventListener}> _listenerBuffer;

    public ${Event}EventSystem(Contexts contexts) : base(contexts.${contextName}) {
        _listeners = contexts.${contextName}.GetGroup(${MatcherType}.${EventListener});
        _entityBuffer = new System.Collections.Generic.List<${EntityType}>();
        _listenerBuffer = new System.Collections.Generic.List<I${EventListener}>();
    }

    protected override Entitas.ICollector<${EntityType}> GetTrigger(Entitas.IContext<${EntityType}> context) {
        return Entitas.CollectorContextExtension.CreateCollector(
            context, Entitas.TriggerOnEventMatcherExtension.${GroupEvent}(${MatcherType}.${ComponentName})
        );
    }

    protected override bool Filter(${EntityType} entity) {
        return ${filter};
    }

    protected override void Execute(System.Collections.Generic.List<${EntityType}> entities) {
        foreach (var e in entities) {
            ${cachedAccess}
            foreach (var listenerEntity in _listeners.GetEntities(_entityBuffer)) {
                _listenerBuffer.Clear();
                _listenerBuffer.AddRange(listenerEntity.${eventListener}.value);
                foreach (var listener in _listenerBuffer) {
                    listener.On${Event}(e${methodArgs});
                }
            }
        }
    }
}
";

		private const		String					TEMPLATE_SELF			=
@"public sealed class ${Event}EventSystem : Entitas.ReactiveSystem<${EntityType}> {

    readonly System.Collections.Generic.List<I${EventListener}> _listenerBuffer;

    public ${Event}EventSystem(Contexts contexts) : base(contexts.${contextName}) {
        _listenerBuffer = new System.Collections.Generic.List<I${EventListener}>();
    }

    protected override Entitas.ICollector<${EntityType}> GetTrigger(Entitas.IContext<${EntityType}> context) {
        return Entitas.CollectorContextExtension.CreateCollector(
            context, Entitas.TriggerOnEventMatcherExtension.${GroupEvent}(${MatcherType}.${ComponentName})
        );
    }

    protected override bool Filter(${EntityType} entity) {
        return ${filter};
    }

    protected override void Execute(System.Collections.Generic.List<${EntityType}> entities) {
        foreach (var e in entities) {
            ${cachedAccess}
            _listenerBuffer.Clear();
            _listenerBuffer.AddRange(e.${eventListener}.value);
            foreach (var listener in _listenerBuffer) {
                listener.On${Event}(e${methodArgs});
            }
        }
    }
}
";

		protected override	ICollector<Ent>			GetTrigger				( IContext<Ent> context )
		{
			return context.CreateCollector( Matcher<Ent>
				.AllOf(
					Matcher<Main,Comp>.I,
					Matcher<Main,EventComp>.I ) );
		}

		protected override	Boolean					Filter					( Ent entity )
		{
			return entity.Has_<Comp>() && entity.Has_<EventComp>();
		}

		protected override	void					Execute					( List<Ent> entities )
		{
			foreach ( var ent in entities )
			{
				var contextNames = ent.Get_<ContextNamesComp>().Values;
				foreach ( var contextName in contextNames )
				{
					foreach ( var eventInfo in ent.Get_<EventComp>().Values )
					{
						CreateFileEnt( ent, eventInfo, contextName );
					}
				}
			}
		}

		private void CreateFileEnt( Ent ent, EventInfo eventInfo, string contextName )
		{
			var methodArgs = ent.GetEventMethodArgs( eventInfo, ", " + ( !ent.Has_<PublicFieldsComp>()
				? ent.PrefixedComponentName( _contexts )
				: GetMethodArgs( ent.Get_<PublicFieldsComp>().Values.ToArray( ) ) ) );

			var filePath = "Events" + Path.DirectorySeparatorChar + "Systems" + Path.DirectorySeparatorChar +
			               ent.Event( _contexts, contextName, eventInfo ) + "EventSystem.cs";

			var template = eventInfo.EventTarget == EventTarget.Self
				? TEMPLATE_SELF
				: TEMPLATE_ANY;

			var cachedAccess = !ent.Has_<PublicFieldsComp>()
				? string.Empty
				: "var component = e." + ent.ComponentName( _contexts ).LowercaseFirst( ) + ";";

			if ( eventInfo.EventType == EventType.Removed )
			{
				methodArgs = string.Empty;
				cachedAccess = string.Empty;
			}

			var contents = template
				.Replace( "${GroupEvent}", eventInfo.EventType.ToString( ) )
				.Replace( "${filter}", GetFilter( ent, contextName, eventInfo ) )
				.Replace( "${cachedAccess}", cachedAccess )
				.Replace( "${methodArgs}", methodArgs )
				.Replace( _contexts, ent, contextName, eventInfo );

			var generatedBy = GetType( ).FullName;

			var fileEnt = _contexts.Get<Main>().CreateEntity( );
			fileEnt.Add_( new GeneratedFileComp( filePath, contents.WrapInNamespace( _contexts ), generatedBy ) );
		}

		private				String					GetFilter				( Ent ent, string contextName, EventInfo eventInfo )
		{
			var filter			= string.Empty;
			if (!ent.Has_<PublicFieldsComp>())
			{
				switch (eventInfo.EventType)
				{
					case EventType.Added:
						filter = "entity." + ent.PrefixedComponentName( _contexts );
						break;
					case EventType.Removed:
						filter = "!entity." + ent.PrefixedComponentName( _contexts );
						break;
				}
			}
			else
			{
				switch (eventInfo.EventType)
				{
					case EventType.Added:
						filter = "entity.has" + ent.ComponentName( _contexts );
						break;
					case EventType.Removed:
						filter = "!entity.has" + ent.ComponentName( _contexts );
						break;
				}
			}

			if (eventInfo.EventTarget == EventTarget.Self) {
				filter += " && entity.has" + ent.EventListener( _contexts, contextName, eventInfo);
			}

			return filter;
		}

		private				String					GetMethodArgs			(FieldInfo[] memberData)
		{
			return string.Join(", ", memberData
				.Select(info => "component." + info.FieldName)
				.ToArray()
			);
		}
	}
}