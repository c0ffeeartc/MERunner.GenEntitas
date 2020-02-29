using System;
using System.Collections.Generic;
using System.Linq;
using DesperateDevs.Utils;
using Entitas;
using Entitas.CodeGeneration.Attributes;
using Entitas.CodeGeneration.Plugins;
using Entitas.Generic;

namespace GenEntitas {

    public static class CodeGeneratorExtentions2 {

        public const string LOOKUP = "ComponentsLookup";

        public static string ComponentName( this Entity<Main> ent, Contexts contexts ) {
            return ent.Get_<Comp>().FullTypeName.ToComponentName( contexts.Get<Settings>().Is<IgnoreNamespaces>() );
        }

        public static string ComponentNameWithContext(this Entity<Main> ent, string contextName) {
            return contextName + ent.Get_<Comp>().Name;
        }

		public static string WrapInNamespace( this string s, Contexts contexts )
		{
			var value	= contexts.Get<Settings>().Has_<GeneratedNamespace>()
				? contexts.Get<Settings>().Get_<GeneratedNamespace>().Value
				: null;
			return String.IsNullOrEmpty( value ) ? s : $"namespace {value} {{\n{s}\n}}\n";
		}

		public static string Replace( this string template, Contexts contexts, Entity<Main> ent, string contextName )
		{
			if ( ent.Has_<Comp>() )
			{
				var componentName = ent.ComponentName( contexts );
				template = template
					.Replace(contextName)
					.Replace("${ComponentType}", ent.Get_<Comp>().FullTypeName)
					.Replace("${ComponentName}", componentName)
					.Replace("${validComponentName}", componentName.LowercaseFirst())
					.Replace("${componentName}", componentName.LowercaseFirst())
					.Replace("${Index}", contextName + LOOKUP + "." + componentName)
					.Replace("${prefixedComponentName}", ent.PrefixedComponentName(contexts));
			}

			if ( ent.Has_<PublicFieldsComp>() )
			{
				var comp = ent.Get_<PublicFieldsComp>();
				template = template
					.Replace("${newMethodParameters}", GetMethodParameters(comp.Values, true))
					.Replace("${methodParameters}", GetMethodParameters(comp.Values, false))
					.Replace("${newMethodArgs}", GetMethodArgs(comp.Values, true))
					.Replace("${methodArgs}", GetMethodArgs(comp.Values, false));
			}
			return template;
		}

        public static string Replace( this string template, Contexts contexts, Entity<Main> ent, string contextName,
	        EventInfo eventInfo ) {
            var eventListener = ent.EventListener( contexts, contextName, eventInfo);

            return template
                .Replace( contexts, ent, contextName)
                .Replace("${EventListenerComponent}", eventListener.AddComponentSuffix())
                .Replace("${Event}", ent.Event( contexts, contextName, eventInfo))
                .Replace("${EventListener}", eventListener)
                .Replace("${eventListener}", eventListener.LowercaseFirst(  ) )
                .Replace("${EventType}", ent.GetEventTypeSuffix(eventInfo));
        }

        public static string PrefixedComponentName(this Entity<Main> ent, Contexts contexts ) {
        	var uniquePrefix = ent.Has_<UniquePrefixComp>() ? ent.Get_<UniquePrefixComp>().Value : "";
            return uniquePrefix.LowercaseFirst() + ent.ComponentName( contexts );
        }

		public static string Event( this Entity<Main> ent, Contexts contexts, string contextName, EventInfo eventInfo ) {
			var optionalContextName = ent.Has_<ContextNamesComp>() && ent.Get_<ContextNamesComp>().Values.Count > 1 ? contextName : string.Empty;
			var theAnyPrefix = eventInfo.EventTarget == EventTarget.Any ? "Any" : "";
			return optionalContextName + theAnyPrefix + ent.ComponentName( contexts ) + ent.GetEventTypeSuffix(eventInfo);
		}

		public static string EventListener( this Entity<Main> ent, Contexts contexts, string contextName, EventInfo eventInfo ) {
			return ent.Event( contexts, contextName, eventInfo) + "Listener";
		}

        public static string GetEventMethodArgs(this Entity<Main> ent, EventInfo eventInfo, string args) {
            if (!ent.Has_<PublicFieldsComp>()) {
                return string.Empty;
            }

            return eventInfo.EventType == EventType.Removed
                ? string.Empty
                : args;
        }

		public static string GetEventTypeSuffix(this Entity<Main> ent, EventInfo eventInfo) {
			return eventInfo.EventType == EventType.Removed ? "Removed" : string.Empty;
		}

        public static string GetMethodParameters(this List<FieldInfo> memberData, bool newPrefix) {
            return string.Join(", ", memberData
                .Select(info => info.TypeName + (newPrefix ? " new" + info.FieldName.UppercaseFirst() : " " + info.FieldName))
                .ToArray());
        }

        public static string GetMethodArgs(List<FieldInfo> memberData, bool newPrefix) {
            return string.Join(", ", memberData
                .Select(info => (newPrefix ? "new" + info.FieldName.UppercaseFirst() : info.FieldName))
                .ToArray()
            );
        }
    }
}
