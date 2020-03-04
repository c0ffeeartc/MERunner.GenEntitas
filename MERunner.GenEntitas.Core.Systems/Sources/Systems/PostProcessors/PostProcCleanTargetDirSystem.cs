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
public sealed class Factory_PostProcCleanTargetDirSystem : TSystem_Factory<PostProcCleanTargetDirSystem> {  }

	[Guid("8283A655-B9BA-4106-ADC8-4245C4CAF059")]
	public class PostProcCleanTargetDirSystem : ReactiveSystem<Ent>
	{
		public				PostProcCleanTargetDirSystem ( Contexts contexts ) : base( contexts.Get<Main>() )
		{
			_contexts			= contexts;
		}

		private				Contexts				_contexts;

		protected override	ICollector<Ent>			GetTrigger				( IContext<Ent> context )
		{
			return context.CreateCollector( Matcher_<Main,GeneratedFileComp>.I );
		}

		protected override	Boolean					Filter					( Ent entity )
		{
			return entity.Has_<GeneratedFileComp>();
		}

		protected override	void					Execute					( List<Ent> entities )
		{
			var settings			= _contexts.Get<Settings>();
			if ( settings.Is<RunInDryMode>() )
			{
				return;
			}

			var generatePath		= Path.Combine( settings.Get_<GeneratePath>().Value, "Generated" );
			var dirInfo				= new DirectoryInfo( generatePath );
			if ( !dirInfo.Exists )
			{
				return;
			}
			foreach ( var file in dirInfo.GetFiles( ) )
			{
				file.Delete(  );
			}
			foreach ( var dir in dirInfo.GetDirectories( ) )
			{
				dir.Delete( true );
			}
		}
	}
}
