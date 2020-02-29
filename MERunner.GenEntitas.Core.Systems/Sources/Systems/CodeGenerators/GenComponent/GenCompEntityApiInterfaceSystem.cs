using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Runtime.InteropServices;
using Entitas;
using Entitas.Generic;
using MERunner;
using Ent = Entitas.Generic.Entity<Main>;

namespace GenEntitas
{
[Export(typeof(ISystem_Factory))]
public sealed class Factory_GenCompEntityApiInterfaceSystem : TSystem_Factory<GenCompEntityApiInterfaceSystem> {  }

	[Guid("015C78AA-656F-4D00-8F46-6C255FDFA8BD")]
	public class GenCompEntityApiInterfaceSystem : ReactiveSystem<Ent>
	{
		public				GenCompEntityApiInterfaceSystem	( Contexts contexts ) : base( contexts.Get<Main>() )
		{
			_contexts = contexts;
		}

		public		GenCompEntityApiInterfaceSystem	(  ) : this( Hub.Contexts )
		{
		}

		private				Contexts				_contexts;

		private const		String					STANDARD_TEMPLATE		=
@"public partial interface I${ComponentName}Entity {

    ${ComponentType} ${componentName} { get; }
    bool has${ComponentName} { get; }

    void Add${ComponentName}(${newMethodParameters});
    void Replace${ComponentName}(${newMethodParameters});
    void Remove${ComponentName}();
}
";

		private const		String					FLAG_TEMPLATE			=
@"public partial interface I${ComponentName}Entity {
    bool ${prefixedComponentName} { get; set; }
}
";

		private const		String					ENTITY_INTERFACE_TEMPLATE = "public partial class ${EntityType} : I${ComponentName}Entity { }\n";

		protected override	ICollector<Ent>			GetTrigger				( IContext<Ent> context )
		{
			return context.CreateCollector( Matcher<Ent>
				.AllOf(
					Matcher_<Main,Comp>.I,
					Matcher_<Main,ContextNamesComp>.I )
				.NoneOf(
					Matcher<Main,DontGenerateComp>.I ) );
		}

		protected override	Boolean					Filter					( Ent entity )
		{
			return entity.Has_<Comp>()
				&& entity.Has_<ContextNamesComp>()
				&& !entity.Is<DontGenerateComp>();
		}

		protected override	void					Execute					( List<Ent> entities )
		{
			foreach ( var ent in entities )
			{
				if ( !ent.Is<GenCompEntApiInterface_ForSingleContext>()
					&& ent.Get_<ContextNamesComp>().Values.Count < 2 )
				{
					continue;
				}

				{
					var template		= ent.Has_<PublicFieldsComp>() ? STANDARD_TEMPLATE : FLAG_TEMPLATE;
					var filePath		= "Components" + Path.DirectorySeparatorChar + "Interfaces" + Path.DirectorySeparatorChar + "I" + ent.ComponentName( _contexts ) + "Entity.cs";
					var contents		= template.Replace( _contexts, ent, String.Empty );
					var generatedBy		= GetType(  ).FullName;

					var fileEnt			= _contexts.Get<Main>().CreateEntity(  );
					fileEnt.Add_( new GeneratedFileComp( filePath, contents.WrapInNamespace( _contexts ), generatedBy ) );
				}

				var contextNames = ent.Get_<ContextNamesComp>().Values;
				foreach ( var contextName in contextNames )
				{
					var filePath		= contextName + Path.DirectorySeparatorChar + "Components" + Path.DirectorySeparatorChar + ent.ComponentNameWithContext(contextName).AddComponentSuffix() + ".cs";
					var contents		= ENTITY_INTERFACE_TEMPLATE.Replace( _contexts, ent, contextName);
					var generatedBy		= GetType(  ).FullName;

					var fileEnt			= _contexts.Get<Main>().CreateEntity(  );
					fileEnt.Add_( new GeneratedFileComp( filePath, contents.WrapInNamespace( _contexts ), generatedBy ) );
				}
			}
		}
	}
}
