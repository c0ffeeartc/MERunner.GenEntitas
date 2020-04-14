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
public sealed class Factory_GenCompMatcherApiSystem : TSystem_Factory<GenCompMatcherApiSystem> {  }

	[Guid("C1C3194D-5B71-435B-A0F6-23A4DD3E79CC")]
	public class GenCompMatcherApiSystem : ReactiveSystem<Ent>
	{
		public				GenCompMatcherApiSystem	( Contexts contexts ) : base( contexts.Get<Main>() )
		{
			_contexts = contexts;
		}

		private				Contexts				_contexts;

		private const		String					TEMPLATE		=
@"public sealed partial class ${MatcherType} {

    static Entitas.IMatcher<${EntityType}> _matcher${ComponentName};

    public static Entitas.IMatcher<${EntityType}> ${ComponentName} {
        get {
            if (_matcher${ComponentName} == null) {
                var matcher = (Entitas.Matcher<${EntityType}>)Entitas.Matcher<${EntityType}>.AllOf(${Index});
                matcher.componentNames = ${componentNames};
                _matcher${ComponentName} = matcher;
            }

            return _matcher${ComponentName};
        }
    }
}
";

		protected override	ICollector<Ent>			GetTrigger				( IContext<Ent> context )
		{
			return context.CreateCollector( Matcher<Ent>
				.AllOf(
					Matcher<Main,Comp>.I )
				.NoneOf(
					Matcher<Main,DontGenerateComp>.I ) );
		}

		protected override	Boolean					Filter					( Ent entity )
		{
			return entity.Has_<Comp>()
				&& !entity.Is<DontGenerateComp>();
		}

		protected override	void					Execute					( List<Ent> entities )
		{
			foreach ( var ent in entities )
			{
				var contextNames = ent.Get_<ContextNamesComp>().Values;
				foreach ( var contextName in contextNames )
				{
					var filePath		= contextName + Path.DirectorySeparatorChar + "Components" + Path.DirectorySeparatorChar + contextName + ent.Get_<Comp>().Name.AddComponentSuffix(  ) + ".cs";
					var contents		= TEMPLATE
						.Replace("${componentNames}", contextName + CodeGeneratorExtentions.LOOKUP + ".componentNames")
						.Replace( _contexts, ent, contextName );
					var generatedBy		= GetType().FullName;

					var fileEnt			= _contexts.Get<Main>().CreateEntity(  );
					fileEnt.Add_( new GeneratedFileComp( filePath, contents.WrapInNamespace( _contexts ), generatedBy ) );
				}
			}
		}
	}
}
