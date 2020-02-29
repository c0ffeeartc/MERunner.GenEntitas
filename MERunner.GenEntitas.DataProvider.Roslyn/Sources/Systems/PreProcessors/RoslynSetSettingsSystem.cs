using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using Entitas;
using Entitas.Generic;
using MERunner;
using Ent = Entitas.Generic.Entity<Settings>;

namespace GenEntitas
{
[Export(typeof(ISystem_Factory))]
public sealed class Factory_RoslynSetSettingsSystem : TSystem_Factory<RoslynSetSettingsSystem> {  }

	[Guid("C893C745-BED3-48E6-8800-401EA88B8CE0")]
	public class RoslynSetSettingsSystem : ReactiveSystem<Ent>
	{
		public				RoslynSetSettingsSystem	( Contexts contexts ) : base( contexts.Get<Settings>() )
		{
			_contexts			= contexts;
		}

		public				RoslynSetSettingsSystem	(  ) : this( Hub.Contexts )
		{
		}

		private				Contexts				_contexts;

		protected override	ICollector<Ent>			GetTrigger				( IContext<Ent> context )
		{
			return context.CreateCollector( Matcher_<Settings,SettingsDict>.I );
		}

		protected override	Boolean					Filter					( Ent entity )
		{
			return entity.Has_<SettingsDict>();
		}

		protected override	void					Execute					( List<Ent> entities )
		{
			var d					= entities[0].Get_<SettingsDict>().Dict;

			_contexts.Get<Settings>().Replace_( new RoslynPathToSolution( d.ContainsKey( nameof( RoslynPathToSolution ) )
				? d[nameof( RoslynPathToSolution )].FirstOrDefault(  )
				: "" ) );
		}
	}
}