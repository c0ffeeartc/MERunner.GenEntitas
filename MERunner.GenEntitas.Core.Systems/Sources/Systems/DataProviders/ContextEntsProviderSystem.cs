using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Entitas;
using Entitas.Generic;
using MERunner;
using Ent = Entitas.Generic.Entity<Main>;

namespace GenEntitas
{
[Export(typeof(ISystem_Factory))]
public sealed class Factory_ContextEntsProviderSystem : TSystem_Factory<ContextEntsProviderSystem> {  }

	[Guid("695EB79E-E907-448A-886B-5ADA89E9E690")]
	public class ContextEntsProviderSystem : ReactiveSystem<Ent>
	{
		public				ContextEntsProviderSystem( Contexts contexts ) : base( contexts.Get<Main>(  ) )
		{
			_contexts			= contexts;
		}

		private				Contexts				_contexts;

		protected override	ICollector<Ent>			GetTrigger				( IContext<Ent> context )
		{
			return context.CreateCollector( Matcher<Ent>
				.AllOf(
					Matcher<Main,ContextNamesComp>.I )
				.NoneOf(
					Matcher<Main,DontGenerateComp>.I ) );
		}

		protected override	Boolean					Filter					( Ent entity )
		{
			return entity.Has_<ContextNamesComp>(  );
		}

		protected override	void					Execute					( List<Ent> entities )
		{
			var contextNames = new HashSet<String>(  );
			foreach ( var ent in entities )
			{
				contextNames.UnionWith( ent.Get_<ContextNamesComp>().Values );
			}

			foreach ( var name in contextNames )
			{
				var ent			= _contexts.Get<Main>().CreateEntity(  );
				ent.Add_( new ContextComp( name ) );
			}
		}
	}
}