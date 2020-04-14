using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Entitas;
using Entitas.Generic;
using MERunner;
using Ent = Entitas.Generic.Entity<Main>;

namespace GenEntitas
{
[Export(typeof(ISystem_Factory))]
public sealed class Factory_Gen_AllEventSystems_System : TSystem_Factory<Gen_AllEventSystems_System> {  }

	[Guid("28160892-AD0E-4080-BC2F-2D356926CD06")]
	public class Gen_AllEventSystems_System : ReactiveSystem<Ent>
	{
		public				Gen_AllEventSystems_System	( Contexts contexts ) : base( contexts.Get<Main>() )
		{
			_contexts			= contexts;
		}

		private				Contexts				_contexts;
		private const		String					TEMPLATE				=
@"public sealed class AllEventSystems : Feature {

    public AllEventSystems(Contexts contexts) {
${systemsList}
    }
}
";

		private const		String					SYSTEM_ADD_TEMPLATE		= @"        Add(new ${Event}EventSystem(contexts)); // priority: ${priority}";

		protected override	ICollector<Ent>			GetTrigger				( IContext<Ent> context )
		{
			return context.CreateCollector( Matcher<Ent>
				.AllOf(
					Matcher<Main,Comp>.I,
					Matcher<Main,EventComp>.I ) );
		}

		protected override	Boolean					Filter					( Ent entity )
		{
			return entity.Has_<Comp>()
				&& entity.Has_<EventComp>();
		}

		protected override	void					Execute					( List<Ent> eventEnts )
		{
			var orderedEventData = eventEnts
				.SelectMany(ent => ent.Get_<EventComp>().Values.Select(eventData => new DataTuple { Ent = ent, EventInfo = eventData }).ToArray())
				.OrderBy(tuple => tuple.EventInfo.Priority)
				.ThenBy(tuple => tuple.Ent.ComponentName( _contexts ))
				.ToArray();

			{
				var filePath		= "Events" + Path.DirectorySeparatorChar + "AllEventSystems.cs";
				var contents		= TEMPLATE
					.Replace("${systemsList}", GenerateSystemList( orderedEventData ) )
					;

				var generatedBy		= GetType().FullName;

				var fileEnt			= _contexts.Get<Main>().CreateEntity(  );
				fileEnt.Add_( new GeneratedFileComp( filePath, contents.WrapInNamespace( _contexts ), generatedBy ) );
			}
		}

		private				String					GenerateSystemList		( DataTuple[] data )
		{
			return string.Join("\n", data
				.SelectMany( tuple => GenerateSystemListForData( tuple ) )
				.ToArray());
		}

		private				String[]			GenerateSystemListForData	( DataTuple data )
		{
			return data.Ent.Get_<ContextNamesComp>().Values
				.Select(ctxName => GenerateAddSystem(ctxName, data))
				.ToArray();
		}

		private				String					GenerateAddSystem		(string contextName, DataTuple data )
		{
			return SYSTEM_ADD_TEMPLATE
				.Replace( _contexts, data.Ent, contextName, data.EventInfo)
				.Replace("${priority}", data.EventInfo.Priority.ToString())
				.Replace("${Event}", data.Ent.Event( _contexts, contextName, data.EventInfo));
		}

		struct DataTuple
		{
			public Entity<Main> Ent;
			public EventInfo EventInfo;
		}
	}
}