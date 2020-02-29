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
public sealed class Factory_GenEventListenerInterfaceSystem : TSystem_Factory<GenEventListenerInterfaceSystem> {  }

	[Guid("3070B6D8-4982-4AD0-847E-954C49B1585F")]
	public class GenEventListenerInterfaceSystem : ReactiveSystem<Ent>
	{
		public		GenEventListenerInterfaceSystem	( Contexts contexts ) : base( contexts.Get<Main>() )
		{
			_contexts			= contexts;
		}

		public		GenEventListenerInterfaceSystem	(  ) : this( Hub.Contexts )
		{
		}

		private				Contexts				_contexts;

		private const		String					TEMPLATE				=
@"public interface I${EventListener} {
    void On${Event}(${ContextName}Entity entity${methodParameters});
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
						var filePath		= "Events" + Path.DirectorySeparatorChar + "Interfaces" + Path.DirectorySeparatorChar + "I" + ent.EventListener( _contexts, contextName, eventInfo) + ".cs";

						var contents = TEMPLATE
							.Replace("${methodParameters}", ent.GetEventMethodArgs(eventInfo
								, ent.Has_<PublicFieldsComp>()
									? ", " + ent.Get_<PublicFieldsComp>().Values.GetMethodParameters(false)
									: ""))
							.Replace( _contexts, ent, contextName, eventInfo);

						var generatedBy		= GetType().FullName;

						var fileEnt			= _contexts.Get<Main>().CreateEntity(  );
						fileEnt.Add_( new GeneratedFileComp( filePath, contents.WrapInNamespace( _contexts ), generatedBy ) );
					}
				}
			}
		}
	}
}