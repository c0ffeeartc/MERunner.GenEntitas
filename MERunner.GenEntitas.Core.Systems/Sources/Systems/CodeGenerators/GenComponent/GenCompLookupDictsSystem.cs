using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Entitas;
using Entitas.Generic;
using Ent = Entitas.Generic.Entity<Main>;
using MERunner;

namespace GenEntitas
{
[Export(typeof(ISystem_Factory))]
public sealed class Factory_GenCompLookupDictsSystem : TSystem_Factory<GenCompLookupDictsSystem> {  }

	[Guid("62A85490-CA0F-4D5B-AB5E-EC8FF7D04979")]
	public class GenCompLookupDictsSystem : ReactiveSystem<Ent>
	{
		public			GenCompLookupDictsSystem	( Contexts contexts ) : base( contexts.Get<Main>() )
		{
			_contexts = contexts;
		}

		private				Contexts				_contexts;

		private const		String					TEMPLATE				=
@"public static partial class ${Lookup}
{
    public static readonly System.Collections.Generic.Dictionary<System.Type,int> TypeToI = new System.Collections.Generic.Dictionary<System.Type,int>()
    {
${kTypeVIndexList}
    };
}
";

		private const		String					K_TYPE_V_INDEX			= @"        { typeof(${ComponentType}), ${Index} },";

		protected override	ICollector<Ent>			GetTrigger				( IContext<Ent> context )
		{
			return context.CreateCollector( Matcher<Ent>
				.AllOf(
					Matcher<Main,Comp>.I )
				.NoneOf(
					Matcher<Main,DontGenerateComp>.I ) );
		}

		protected override	Boolean					Filter					( Ent entity )
		{
			return entity.Has_<Comp>() && !entity.Is<DontGenerateComp>();
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

			foreach (var contextName in contextEnts.Keys.ToArray())
			{
				contextEnts[contextName] = contextEnts[contextName]
					.OrderBy( ent => ent.Get_<Comp>().FullTypeName)
					.ToList();
			}


			foreach ( var kv in contextEnts )
			{
				var ents = kv.Value;

				var componentConstantsList = string.Join("\n", ents.ToArray()
					.Select((ent, index) => K_TYPE_V_INDEX
						.Replace("${ComponentType}", ent.Get_<Comp>().FullTypeName )
						.Replace("${Index}", index.ToString())).ToArray());

				var contextName			= kv.Key;
				var filePath		= contextName + Path.DirectorySeparatorChar + contextName + "ComponentsLookupDicts.cs";
				var generatedBy		= GetType().FullName;

				var contents = TEMPLATE
					.Replace("${Lookup}", contextName + CodeGeneratorExtentions.LOOKUP)
					.Replace("${kTypeVIndexList}", componentConstantsList);

				var fileEnt			= _contexts.Get<Main>().CreateEntity(  );
				fileEnt.Add_( new GeneratedFileComp( filePath, contents.WrapInNamespace( _contexts ), generatedBy ) );
			}
		}
	}
}