using System;
using System.Collections.Generic;
using DesperateDevs.Utils;
using Entitas;
using Entitas.CodeGeneration.Attributes;
using Entitas.CodeGeneration.Plugins;
using Entitas.Generic;

namespace GenEntitas {

    public static class CodeGeneratorExtentions {

        public const string LOOKUP = "ComponentsLookup";

        public static string Replace(this string template, string contextName) {
            return template
                .Replace("${ContextName}", contextName)
                .Replace("${contextName}", contextName.LowercaseFirst())
                .Replace("${ContextType}", contextName.AddContextSuffix())
                .Replace("${EntityType}", contextName.AddEntitySuffix())
                .Replace("${MatcherType}", contextName.AddMatcherSuffix())
                .Replace("${Lookup}", contextName + LOOKUP);
        }

		public static		void					ProvideEventCompNewEnts	( Contexts contexts, Entity<Main> ent )
		{
			var mainContext			= contexts.Get<Main>();
			var settingsContext		= contexts.Get<Settings>();
			foreach ( var contextName in ent.Get_<ContextNamesComp>().Values )
			{
				foreach ( var eventInfo in ent.Get_<EventComp>().Values )
				{
					var componentName				= ent.Get_<Comp>().FullTypeName.ToComponentName( settingsContext.Is<IgnoreNamespaces>() );
					var optionalContextName			= ent.Get_<ContextNamesComp>().Values.Count > 1 ? contextName : string.Empty;
					var eventTypeSuffix				= ent.GetEventTypeSuffix( eventInfo );
					var theAnySuffix				= eventInfo.EventTarget == EventTarget.Any ? "Any" : "";
					var listenerComponentName		= optionalContextName + theAnySuffix + componentName + eventTypeSuffix + "Listener";
					var eventCompFullTypeName		= listenerComponentName.AddComponentSuffix();

					var eventListenerCompEnt			= mainContext.CreateEntity(  );
					eventListenerCompEnt.Flag<EventListenerComp>( true );

					eventListenerCompEnt.Add_( new Comp( listenerComponentName, eventCompFullTypeName ) );
					eventListenerCompEnt.Add_( new ContextNamesComp( new List<String>{ contextName } ) );
					eventListenerCompEnt.Add_( new PublicFieldsComp( new List<FieldInfo>
						{
							new FieldInfo( "System.Collections.Generic.List<I" + listenerComponentName + ">", "value" )
						} ) );
				}
			}
		}
    }
}
