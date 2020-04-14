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
public sealed class Factory_GenNonICompSystem : TSystem_Factory<GenNonICompSystem> {  }

	[Guid("3E6C4884-71E8-4593-8D36-64ECAF7DC525")]
	public class GenNonICompSystem : ReactiveSystem<Ent>
	{
		public				GenNonICompSystem		( Contexts contexts ) : base( contexts.Get<Main>() )
		{
			_contexts			= contexts;
		}

		private				Contexts				_contexts;
		private const		String					COMPONENT_TEMPLATE		=
@"[Entitas.CodeGeneration.Attributes.DontGenerate(false)]
public sealed class ${FullComponentName} : Entitas.IComponent {
    public ${Type} value;
}
";

		protected override	ICollector<Ent>			GetTrigger				( IContext<Ent> context )
		{
			return context.CreateCollector( Matcher<Ent>
				.AllOf(
					Matcher<Main,NonIComp>.I )
				.NoneOf(
					Matcher<Main,DontGenerateComp>.I ) );
		}

		protected override	Boolean					Filter					( Ent entity )
		{
			return entity.Has_<NonIComp>()
				&& !entity.Is<DontGenerateComp>();
		}

		protected override	void					Execute					( List<Ent> entities )
		{
			for ( var i = 0; i < entities.Count; i++ )
			{
				var ent				= entities[i];

				var nonIComp		= ent.Get_<NonIComp>();
				var filePath		= "Components" + Path.DirectorySeparatorChar + nonIComp.FullCompName + ".cs";
				var contents		= Generate( nonIComp.FullCompName, nonIComp.FieldTypeName );
				var generatedBy		= GetType(  ).FullName;

				var fileEnt			= _contexts.Get<Main>().CreateEntity(  );
				fileEnt.Add_( new GeneratedFileComp( filePath, contents.WrapInNamespace( _contexts ), generatedBy ) );
			}
		}

		private				String					Generate				( String fullComponentName, String fieldTypeName )
		{
			return COMPONENT_TEMPLATE
				.Replace("${FullComponentName}", fullComponentName)
				.Replace("${Type}", fieldTypeName );
		}
	}
}
