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
public sealed class Factory_PostProcMergeFilesSystem : TSystem_Factory<PostProcMergeFilesSystem> {  }

	[Guid("DAFB3479-BCDD-4CFB-BA35-5D072FC9FD34")]
	public class PostProcMergeFilesSystem : ReactiveSystem<Ent>
	{
		public				PostProcMergeFilesSystem ( Contexts contexts ) : base( contexts.Get<Main>() )
		{
		}

		protected override	ICollector<Ent>			GetTrigger				( IContext<Ent> context )
		{
			return context.CreateCollector( Matcher<Main,GeneratedFileComp>.I );
		}

		protected override	Boolean					Filter					( Ent entity )
		{
			return entity.Has_<GeneratedFileComp>();
		}

		protected override	void					Execute					( List<Ent> entities )
		{
			var filePathToEnt = new Dictionary<String, Ent>(  );
			foreach ( var ent in entities )
			{
				var filePath = ent.Get_<GeneratedFileComp>().FilePath;
				if ( filePathToEnt.ContainsKey( filePath ) )
				{
					var prevEnt			= filePathToEnt[filePath];
					filePathToEnt[filePath].Replace_( new GeneratedFileComp( 
						filePath,
						prevEnt.Get_<GeneratedFileComp>().Contents + "\n" + ent.Get_<GeneratedFileComp>().Contents,
						prevEnt.Get_<GeneratedFileComp>().GeneratedBy + ", " + ent.Get_<GeneratedFileComp>().GeneratedBy
						) );
					 ent.Flag<Destroy>( true );
					continue;
				}
				filePathToEnt[filePath] = ent;
			}
		}
	}
}
