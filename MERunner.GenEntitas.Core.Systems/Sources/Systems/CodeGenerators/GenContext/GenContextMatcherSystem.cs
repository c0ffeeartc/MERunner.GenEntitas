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
public sealed class Factory_GenContextMatcherSystem : TSystem_Factory<GenContextMatcherSystem> {  }

	[Guid("EF20AD61-F096-4CDC-A3A4-57BAC33AF31B")]
	public class GenContextMatcherSystem : ReactiveSystem<Ent>
	{
		public				GenContextMatcherSystem	( Contexts contexts ) : base( contexts.Get<Main>() )
		{
			_contexts			= contexts;
		}

		private				Contexts			_contexts;
		private const		String				TEMPLATE					=
@"public sealed partial class ${MatcherType} {

    public static Entitas.IAllOfMatcher<${EntityType}> AllOf(params int[] indices) {
        return Entitas.Matcher<${EntityType}>.AllOf(indices);
    }

    public static Entitas.IAllOfMatcher<${EntityType}> AllOf(params Entitas.IMatcher<${EntityType}>[] matchers) {
          return Entitas.Matcher<${EntityType}>.AllOf(matchers);
    }

    public static Entitas.IAnyOfMatcher<${EntityType}> AnyOf(params int[] indices) {
          return Entitas.Matcher<${EntityType}>.AnyOf(indices);
    }

    public static Entitas.IAnyOfMatcher<${EntityType}> AnyOf(params Entitas.IMatcher<${EntityType}>[] matchers) {
          return Entitas.Matcher<${EntityType}>.AnyOf(matchers);
    }
}
";

		protected override	ICollector<Ent>			GetTrigger				( IContext<Ent> context )
		{
			return context.CreateCollector( Matcher<Main,ContextComp>.I );
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
				var filePath		= contextName + Path.DirectorySeparatorChar + contextName.AddMatcherSuffix(  ) + ".cs";
				var contents		= TEMPLATE.Replace( contextName );
				var generatedBy		= GetType(  ).FullName;
				var fileEnt			= _contexts.Get<Main>().CreateEntity(  );
				fileEnt.Add_( new GeneratedFileComp( filePath, contents.WrapInNamespace(_contexts), generatedBy ) );
			}
		}
	}
}