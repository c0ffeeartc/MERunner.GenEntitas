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
public sealed class Factory_GenCompContextApiSystem : TSystem_Factory<GenCompContextApiSystem> {  }

	[Guid("45EE2A41-1018-426C-A7EE-14B86197068B")]
	public class GenCompContextApiSystem : ReactiveSystem<Ent>
	{
		public				GenCompContextApiSystem	( Contexts contexts ) : base( contexts.Get<Main>() )
		{
			_contexts = contexts;
		}

		private				Contexts				_contexts;

		private const		String					STANDARD_TEMPLATE		=
@"public partial class ${ContextType} {

    public ${EntityType} ${componentName}Entity { get { return GetGroup(${MatcherType}.${ComponentName}).GetSingleEntity(); } }
    public ${ComponentType} ${componentName} { get { return ${componentName}Entity.${componentName}; } }
    public bool has${ComponentName} { get { return ${componentName}Entity != null; } }

    public ${EntityType} Set${ComponentName}(${newMethodParameters}) {
        if (has${ComponentName}) {
            throw new Entitas.EntitasException(""Could not set ${ComponentName}!\n"" + this + "" already has an entity with ${ComponentType}!"",
                ""You should check if the context already has a ${componentName}Entity before setting it or use context.Replace${ComponentName}()."");
        }
        var entity = CreateEntity();
        entity.Add${ComponentName}(${newMethodArgs});
        return entity;
    }

    public void Replace${ComponentName}(${newMethodParameters}) {
        var entity = ${componentName}Entity;
        if (entity == null) {
            entity = Set${ComponentName}(${newMethodArgs});
        } else {
            entity.Replace${ComponentName}(${newMethodArgs});
        }
    }

    public void Remove${ComponentName}() {
        ${componentName}Entity.Destroy();
    }
}
";

		private const		String					FLAG_TEMPLATE			=
@"public partial class ${ContextType} {

    public ${EntityType} ${componentName}Entity { get { return GetGroup(${MatcherType}.${ComponentName}).GetSingleEntity(); } }

    public bool ${prefixedComponentName} {
        get { return ${componentName}Entity != null; }
        set {
            var entity = ${componentName}Entity;
            if (value != (entity != null)) {
                if (value) {
                    CreateEntity().${prefixedComponentName} = true;
                } else {
                    entity.Destroy();
                }
            }
        }
    }
}
";

		protected override	ICollector<Ent>			GetTrigger				( IContext<Ent> context )
		{
			return context.CreateCollector( Matcher<Ent>
				.AllOf(
					Matcher_<Main,Comp>.I,
					Matcher<Main,UniqueComp>.I )
				.NoneOf(
					Matcher<Main,DontGenerateComp>.I ) );
		}

		protected override	Boolean					Filter					( Ent entity )
		{
			return entity.Is<UniqueComp>()
				&& entity.Has_<Comp>()
				&& !entity.Is<DontGenerateComp>();
		}

		protected override	void					Execute					( List<Ent> entities )
		{
			foreach ( var ent in entities )
			{
				var contextNames	= ent.Get_<ContextNamesComp>().Values;
				var template		= ent.Has_<PublicFieldsComp>() ? STANDARD_TEMPLATE : FLAG_TEMPLATE;
				foreach ( var contextName in contextNames )
				{
					var filePath		= contextName + Path.DirectorySeparatorChar + "Components" + Path.DirectorySeparatorChar + contextName + ent.Get_<Comp>().Name.AddComponentSuffix(  ) + ".cs";
					var contents		= template.Replace( _contexts, ent, contextName );
					var generatedBy		= GetType().FullName;

					var fileEnt			= _contexts.Get<Main>().CreateEntity(  );
					fileEnt.Add_( new GeneratedFileComp( filePath, contents.WrapInNamespace( _contexts ), generatedBy ) );
				}
			}
		}
	}
}

