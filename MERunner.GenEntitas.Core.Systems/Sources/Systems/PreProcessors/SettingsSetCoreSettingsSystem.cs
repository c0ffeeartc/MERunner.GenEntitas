using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Entitas;
using Entitas.Generic;
using MERunner;
using Ent = Entitas.Generic.Entity<Settings>;

namespace GenEntitas
{
[Export(typeof(ISystem_Factory))]
public sealed class Factory_SettingsSetCoreSettingsSystem : TSystem_Factory<SettingsSetCoreSettingsSystem> {  }

	[Guid("6F2E73C6-B6D3-42C6-AA29-7A832DA32F3E")]
	public class SettingsSetCoreSettingsSystem : ReactiveSystem<Ent>
	{
		public				SettingsSetCoreSettingsSystem	( Contexts contexts ) : base( contexts.Get<Settings>() )
		{
			_contexts			= contexts;
		}

		private				Contexts				_contexts;

		protected override	ICollector<Ent>			GetTrigger				( IContext<Ent> context )
		{
			return context.CreateCollector( Matcher<Ent>.AllOf( Matcher_<Settings,SettingsDict>.I ) );
		}

		protected override	Boolean					Filter					( Ent entity )
		{
			return entity.Has_<SettingsDict>(  );
		}

		protected override	void					Execute					( List<Ent> entities )
		{
			var settingsGrammar		= Hub.SettingsGrammar;
			var settingsContext		= _contexts.Get<Settings>(  );
			var d					= _contexts
				.Get<Settings>(  )
				.Get_<SettingsDict>(  )
				.Dict;

			if ( d.ContainsKey( nameof( LogGeneratedPaths ) ) )
			{
				settingsContext.Flag<LogGeneratedPaths>( settingsGrammar.BoolFromStr( d[nameof( LogGeneratedPaths )].FirstOrDefault(  ) ) );
			}
			else
			{
				settingsContext.Flag<LogGeneratedPaths>( true );
			}

			if ( d.ContainsKey( nameof( IgnoreNamespaces ) ) )
			{
				settingsContext.Flag<IgnoreNamespaces>( settingsGrammar.BoolFromStr( d[nameof( IgnoreNamespaces )].FirstOrDefault(  ) ) );
			}
			else
			{
				settingsContext.Flag<IgnoreNamespaces>( false );
			}

			if ( d.ContainsKey( nameof( RunInDryMode ) ) )
			{
				settingsContext.Flag<RunInDryMode>( settingsGrammar.BoolFromStr( d[nameof( RunInDryMode )].FirstOrDefault(  ) ) );
			}
			else
			{
				settingsContext.Flag<RunInDryMode>( false );
			}

			settingsContext.Replace_( d.ContainsKey( nameof( GeneratedNamespace ) )
				? new GeneratedNamespace( d[nameof( GeneratedNamespace )].FirstOrDefault(  ) )
				: new GeneratedNamespace( "" ) );

			settingsContext.Replace_( d.ContainsKey( nameof( GeneratePath ) )
				? new GeneratePath( d[nameof( GeneratePath )].FirstOrDefault(  ) )
				: new GeneratePath( "" ) );

			settingsContext.Replace_( d.ContainsKey( nameof( WriteGeneratedPathsToCsProj ) )
				? new WriteGeneratedPathsToCsProj( d[nameof( WriteGeneratedPathsToCsProj )].FirstOrDefault(  ) )
				: new WriteGeneratedPathsToCsProj( "" ) );

			if ( !Directory.Exists( settingsContext.Get_<GeneratePath>(  ).Value ) )
			{
				throw new DirectoryNotFoundException( $"Generate path does not exist: '{settingsContext.Get_<GeneratePath>(  ).Value}'" );
			}
		}
	}
}