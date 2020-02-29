using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Runtime.InteropServices;
using Entitas;
using Entitas.CodeGeneration.Plugins;
using Entitas.Generic;
using MERunner;
using Ent = Entitas.Generic.Entity<Main>;

namespace GenEntitas
{
[Export(typeof(ISystem_Factory))]
public sealed class Factory_GenEventListenerCompSystem : TSystem_Factory<GenEventListenerCompSystem> {  }

	[Guid("162F214A-DFCC-47C2-B906-EC0DC7586F8F")]
	public class GenEventListenerCompSystem : ReactiveSystem<Ent>
	{
		public			GenEventListenerCompSystem	( Contexts contexts ) : base( contexts.Get<Main>() )
		{
			_contexts			= contexts;
		}

		public			GenEventListenerCompSystem	(  ) : this( Hub.Contexts )
		{
		}

		private				Contexts				_contexts;
		private const		String					TEMPLATE				=
@"[Entitas.CodeGeneration.Attributes.DontGenerate(false)]
public sealed class ${EventListenerComponent} : Entitas.IComponent {
    public System.Collections.Generic.List<I${EventListener}> value;
}
";

		protected override	ICollector<Ent>			GetTrigger				( IContext<Ent> context )
		{
			return context.CreateCollector( Matcher<Ent>
				.AllOf(
					Matcher_<Main,Comp>.I,
					Matcher_<Main,EventComp>.I ) );
		}

		protected override	Boolean					Filter					( Ent entity )
		{
			return entity.Has_<Comp>()
				&& entity.Has_<EventComp>();
		}

		protected override	void					Execute					( List<Ent> entities )
		{
			foreach ( var ent in entities )
			{
				var contextNames = ent.Get_<ContextNamesComp>().Values;
				foreach ( var contextName in contextNames )
				{
					var eventInfos = ent.Get_<EventComp>().Values;
					foreach ( var eventInfo in eventInfos )
					{
						var filePath		= "Events" + Path.DirectorySeparatorChar + "Components" + Path.DirectorySeparatorChar + ent.EventListener( _contexts, contextName, eventInfo).AddComponentSuffix() + ".cs";
						var contents		= TEMPLATE.Replace( _contexts, ent, contextName, eventInfo);
						var generatedBy		= GetType().FullName;

						var fileEnt			= _contexts.Get<Main>().CreateEntity(  );
						fileEnt.Add_( new GeneratedFileComp( filePath, contents.WrapInNamespace( _contexts ), generatedBy ) );
					}
				}
			}
		}
	}
}