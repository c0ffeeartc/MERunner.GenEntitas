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
public sealed class Factory_GenContextEventSystemsSystem : TSystem_Factory<GenContextEventSystemsSystem> {  }

	[Guid("DAF1F159-1E60-44B0-8510-DE0A001E18E8")]
	public class GenContextEventSystemsSystem : ReactiveSystem<Ent>
	{
		public				GenContextEventSystemsSystem	( Contexts contexts ) : base( contexts.Get<Main>() )
		{
			_contexts			= contexts;
		}

		private				Contexts				_contexts;
		private const		String					TEMPLATE				=
@"public sealed class ${ContextName}EventSystems : Feature {

    public ${ContextName}EventSystems(Contexts contexts) {
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

		protected override	void					Execute					( List<Ent> entities )
		{
			var contextEnts = new Dictionary<String, List<Ent>>(  );
			foreach ( var ent in entities )
			{
				foreach ( var contextName in ent.Get_<ContextNamesComp>().Values )
				{
					if ( !contextEnts.ContainsKey( contextName ) )
					{
						contextEnts[contextName] = new List<Ent>( );
					}
					contextEnts[contextName].Add( ent );
				}
			}

			var contextNameToDataTuples = new Dictionary<string, List<DataTuple>>();
			foreach (var contextName in contextEnts.Keys.ToArray())
			{
				var orderedEventData = contextEnts[contextName]
					.SelectMany(ent => ent.Get_<EventComp>().Values.Select(eventData => new DataTuple { Ent = ent, EventInfo = eventData }).ToArray())
					.OrderBy(tuple => tuple.EventInfo.Priority)
					.ThenBy(tuple => tuple.Ent.ComponentName( _contexts ))
					.ToList();

				contextNameToDataTuples.Add(contextName, orderedEventData);
			}

			foreach ( var contextName in contextNameToDataTuples.Keys )
			{
				var dataTuples		= contextNameToDataTuples[contextName].ToArray();

				var filePath		= "Events" + Path.DirectorySeparatorChar + contextName + "EventSystems.cs";
				var contents		= TEMPLATE
					.Replace("${systemsList}", GenerateSystemList(contextName, dataTuples))
					.Replace(contextName);

				var generatedBy		= GetType().FullName;

				var fileEnt			= _contexts.Get<Main>().CreateEntity(  );
				fileEnt.Add_( new GeneratedFileComp( filePath, contents.WrapInNamespace( _contexts ), generatedBy ) );
			}
		}

		private				String					GenerateSystemList		( string contextName, DataTuple[] data )
		{
			return string.Join("\n", data
				.SelectMany(tuple => GenerateSystemListForData(contextName, tuple))
				.ToArray());
		}

		private				String[]			GenerateSystemListForData	(string contextName, DataTuple data)
		{
			return data.Ent.Get_<ContextNamesComp>().Values
				.Where(ctxName => ctxName == contextName)
				.Select(ctxName => GenerateAddSystem(ctxName, data))
				.ToArray();
		}

		private				String					GenerateAddSystem		(string contextName, DataTuple data)
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