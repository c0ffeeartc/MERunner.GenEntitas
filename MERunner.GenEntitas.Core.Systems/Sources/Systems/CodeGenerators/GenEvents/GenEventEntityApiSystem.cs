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
public sealed class Factory_GenEventEntityApiSystem : TSystem_Factory<GenEventEntityApiSystem> {  }

	[Guid("2FB50011-6494-47B1-8228-B1ACEA3995E8")]
	public class GenEventEntityApiSystem : ReactiveSystem<Ent>
	{
		public				GenEventEntityApiSystem	( Contexts contexts ) : base( contexts.Get<Main>() )
		{
			_contexts			= contexts;
		}

		private				Contexts				_contexts;

        private const		String					TEMPLATE				=
@"public partial class ${EntityType} {

    public void Add${EventListener}(I${EventListener} value) {
        var listeners = has${EventListener}
            ? ${eventListener}.value
            : new System.Collections.Generic.List<I${EventListener}>();
        listeners.Add(value);
        Replace${EventListener}(listeners);
    }

    public void Remove${EventListener}(I${EventListener} value, bool removeComponentWhenEmpty = true) {
        var listeners = ${eventListener}.value;
        listeners.Remove(value);
        if (removeComponentWhenEmpty && listeners.Count == 0) {
            Remove${EventListener}();
        } else {
            Replace${EventListener}(listeners);
        }
    }
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
						var filePath		= contextName + Path.DirectorySeparatorChar + "Components" + Path.DirectorySeparatorChar + contextName + ent.EventListener( _contexts, contextName, eventInfo).AddComponentSuffix() + ".cs";

						var contents = TEMPLATE
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