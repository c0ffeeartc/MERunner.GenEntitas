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
public sealed class Factory_PostProcLineEndings : TSystem_Factory<PostProcLineEndings> {  }

	[Guid("16FCAE59-CEBD-4E32-95B4-A195015FD14F")]
	public class PostProcLineEndings : ReactiveSystem<Ent>
	{
		public				PostProcLineEndings		( Contexts contexts ) : base( contexts.Get<Main>() )
		{
		}

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
			foreach ( var ent in entities )
			{
        		var contents		= ent.Get_<GeneratedFileComp>().Contents.Replace("\n", Environment.NewLine);
				ent.Replace_( new GeneratedFileComp(
					ent.Get_<GeneratedFileComp>().FilePath,
					contents,
					ent.Get_<GeneratedFileComp>().GeneratedBy ) );
			}
		}
	}
}