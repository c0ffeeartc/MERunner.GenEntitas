using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using Entitas;
using Entitas.Generic;
using MERunner;
using Ent = Entitas.Generic.Entity<Main>;


namespace GenEntitas
{
[Export(typeof(ISystem_Factory))]
public sealed class Factory_GenContextsSystem : TSystem_Factory<GenContextsSystem> {  }

	[Guid("3FE790AB-3A60-41E5-8DF3-157D8B2B835E")]
	public class GenContextsSystem : ReactiveSystem<Ent>
	{
		public				GenContextsSystem	( Contexts contexts ) : base( contexts.Get<Main>() )
		{
			_contexts			= contexts;
		}

		public				GenContextsSystem	(  ) : this( Hub.Contexts )
		{
		}

		private				Contexts			_contexts;
		private const		string				TEMPLATE =
@"public partial class Contexts : Entitas.IContexts {

    public static Contexts sharedInstance {
        get {
            if (_sharedInstance == null) {
                _sharedInstance = new Contexts();
            }

            return _sharedInstance;
        }
        set { _sharedInstance = value; }
    }

    static Contexts _sharedInstance;

${contextPropertiesList}

    public Entitas.IContext[] allContexts { get { return new Entitas.IContext [] { ${contextList} }; } }

    public Contexts() {
${contextAssignmentsList}

        var postConstructors = System.Linq.Enumerable.Where(
            GetType().GetMethods(),
            method => System.Attribute.IsDefined(method, typeof(Entitas.CodeGeneration.Attributes.PostConstructorAttribute))
        );

        foreach (var postConstructor in postConstructors) {
            postConstructor.Invoke(this, null);
        }
    }

    public void Reset() {
        var contexts = allContexts;
        for (int i = 0; i < contexts.Length; i++) {
            contexts[i].Reset();
        }
    }
}
";
		private const		String				CONTEXT_PROPERTY_TEMPLATE	= @"    public ${ContextType} ${contextName} { get; set; }";
		private const		String					CONTEXT_LIST_TEMPLATE	= @"${contextName}";
		private const		String				CONTEXT_ASSIGNMENT_TEMPLATE	= @"        ${contextName} = new ${ContextType}();";

		private				String					Generate				( string[] contextNames )
		{
			var contextPropertiesList = string.Join("\n", contextNames
				.Select(contextName => CONTEXT_PROPERTY_TEMPLATE.Replace(contextName))
				.ToArray());

			var contextList = string.Join(", ", contextNames
				.Select(contextName => CONTEXT_LIST_TEMPLATE.Replace(contextName))
				.ToArray());

			var contextAssignmentsList = string.Join("\n", contextNames
				.Select(contextName => CONTEXT_ASSIGNMENT_TEMPLATE.Replace(contextName))
				.ToArray());

			return TEMPLATE
				.Replace("${contextPropertiesList}", contextPropertiesList)
				.Replace("${contextList}", contextList)
				.Replace("${contextAssignmentsList}", contextAssignmentsList);
		}

		protected override	ICollector<Ent>			GetTrigger				( IContext<Ent> context )
		{
			return context.CreateCollector( Matcher_<Main,ContextComp>.I );
		}

		protected override	Boolean					Filter					( Ent entity )
		{
			return entity.Has_<ContextComp>();
		}

		protected override	void					Execute					( List<Ent> entities )
		{
			var contextNames	= new List<String>(  );
			for ( var i = 0; i < entities.Count; i++ )
			{
				var ent = entities[i];
				contextNames.Add( ent.Get_<ContextComp>().Name );
			}
			contextNames.Sort( ( a, b ) => String.Compare( a, b, StringComparison.Ordinal ) );
			var fileEnt			= _contexts.Get<Main>().CreateEntity(  );
			var contents		= Generate( contextNames.ToArray(  ) );
			fileEnt.Add_( new GeneratedFileComp( "Contexts.cs", contents.WrapInNamespace( _contexts ), GetType(  ).FullName ) );
		}
	}
}