using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Entitas;
using Entitas.Generic;
using MERunner;
using Ent = Entitas.Generic.Entity<Main>;

namespace GenEntitas
{
[Export(typeof(ISystem_Factory))]
public sealed class Factory_PostProcWriteToDiskSystem : TSystem_Factory<PostProcWriteToDiskSystem> {  }

	[Guid("B4D95DA8-FB2E-4491-9CEA-02A24A8A6C88")]
	public class PostProcWriteToDiskSystem : ReactiveSystem<Ent>
	{
		public				PostProcWriteToDiskSystem ( Contexts contexts ) : base( contexts.Get<Main>() )
		{
			_contexts			= contexts;
		}

		public				PostProcWriteToDiskSystem(  ) : this( Hub.Contexts )
		{
		}

		private				Contexts				_contexts;

		protected override	ICollector<Ent>			GetTrigger				( IContext<Ent> context )
		{
			return context.CreateCollector( Matcher<Ent>
				.AllOf(
					Matcher_<Main,GeneratedFileComp>.I )
				.NoneOf(
					Matcher<Main,Destroy>.I ) );
		}

		protected override	Boolean					Filter					( Ent entity )
		{
			return entity.Has_<GeneratedFileComp>()
				&& !entity.Is<Destroy>(  );
		}

		protected override	void					Execute					( List<Ent> entities )
		{
			var settings			= _contexts.Get<Settings>();
			var generatePath		= Path.Combine( settings.Get_<GeneratePath>().Value, "Generated" );
			var stringBuilder	= new StringBuilder(  );
			foreach ( var ent in entities )
			{
				var targetPath		= Path.Combine( generatePath, ent.Get_<GeneratedFileComp>().FilePath );

				if ( settings.Is<LogGeneratedPaths>() )
				{
					stringBuilder.Append( targetPath );
					stringBuilder.Append( " - " );
					stringBuilder.Append( ent.Get_<GeneratedFileComp>().GeneratedBy );
					stringBuilder.Append( "\n" );
				}

				if ( settings.Is<RunInDryMode>() )
				{
					continue;
				}

				var dirPath			= Path.GetDirectoryName( targetPath );
				if ( dirPath != null && !Directory.Exists( dirPath ) )
				{
					Directory.CreateDirectory( dirPath );
				}
				File.WriteAllText( targetPath, ent.Get_<GeneratedFileComp>().Contents );
			}

			if ( settings.Is<LogGeneratedPaths>() )
			{
				var s = stringBuilder.ToString(  );
				Console.Write( s );
#if UNITY_EDITOR
				UnityEngine.Debug.Log( s );
#endif
			}
		}
	}
}