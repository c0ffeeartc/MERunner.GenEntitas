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
public sealed class Factory_GenContextSystem : TSystem_Factory<GenContextSystem> {  }

	[Guid("EAE3F8D4-5E36-46BB-89B0-ECF8C691BB18")]
	public class GenContextSystem : ReactiveSystem<Ent>
	{
		public				GenContextSystem		( Contexts contexts ) : base( contexts.Get<Main>() )
		{
			_contexts			= contexts;
		}

		public				GenContextSystem		(  ) : this( Hub.Contexts )
		{
		}

		private				Contexts				_contexts;
		private const		String					TEMPLATE				=
            @"public sealed partial class ${ContextType} : Entitas.Context<${EntityType}> {

    public ${ContextType}()
        : base(
            ${Lookup}.TotalComponents,
            0,
            new Entitas.ContextInfo(
                ""${ContextName}"",
                ${Lookup}.componentNames,
                ${Lookup}.componentTypes
            ),
            (entity) =>

#if (ENTITAS_FAST_AND_UNSAFE)
                new Entitas.UnsafeAERC(),
#else
                new Entitas.SafeAERC(entity),
#endif
            () => new ${EntityType}()
        ) {
    }
}
";
		protected override	ICollector<Ent>			GetTrigger				( IContext<Ent> context )
		{
			return context.CreateCollector( Matcher_<Main,ContextComp>.I );
		}

		protected override	Boolean					Filter					( Ent entity )
		{
			return entity.Has_<ContextComp>();
		}

		protected override	void					Execute					( List<Ent> entities )
		{
			for ( var i = 0; i < entities.Count; i++ )
			{
				var ent				= entities[i];
				var contextName		= ent.Get_<ContextComp>().Name;

				var filePath		= contextName + Path.DirectorySeparatorChar + contextName.AddContextSuffix(  ) + ".cs";
				var contents		= TEMPLATE.Replace( contextName );
				var generatedBy		= GetType(  ).FullName;

				var fileEnt			= _contexts.Get<Main>().CreateEntity(  );
				fileEnt.Add_( new GeneratedFileComp( filePath, contents.WrapInNamespace( _contexts ), generatedBy ) );
			}
		}
	}
}
