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
public sealed class Factory_GenEntitySystem : TSystem_Factory<GenEntitySystem> {  }

	[Guid("C3FCAC41-1750-4727-9A19-7F15ABA1D2AE")]
	public class GenEntitySystem : ReactiveSystem<Ent>
	{
		public				GenEntitySystem			( Contexts contexts ) : base( contexts.Get<Main>() )
		{
			_contexts			= contexts;
		}

		private				Contexts				_contexts;
		private const		String					TEMPLATE				=
@"public sealed partial class ${EntityType} : Entitas.Entity {
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

				var filePath		= contextName + Path.DirectorySeparatorChar + contextName.AddEntitySuffix(  ) + ".cs";
				var contents		= TEMPLATE.Replace( contextName );
				var generatedBy		= GetType(  ).FullName;

				var fileEnt			= _contexts.Get<Main>().CreateEntity(  );
				fileEnt.Add_( new GeneratedFileComp( filePath, contents.WrapInNamespace( _contexts ), generatedBy ) );
			}
		}
	}
}
