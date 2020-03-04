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
public sealed class Factory_GenComponentSystem : TSystem_Factory<GenComponentSystem> {  }

	[Guid("E92BE793-1836-4E0C-B29C-C8D2CF92E799")]
	public class GenComponentSystem : ReactiveSystem<Ent>
	{
		public				GenComponentSystem		( Contexts contexts ) : base( contexts.Get<Main>() )
		{
			_contexts			= contexts;
		}

		private				Contexts				_contexts;
		private const		String					COMPONENT_TEMPLATE		=
@"[Entitas.CodeGeneration.Attributes.DontGenerate(false)]
public partial class ${FullComponentName} : Entitas.IComponent {
${memberList}
}
";

		protected override	ICollector<Ent>			GetTrigger				( IContext<Ent> context )
		{
			return context.CreateCollector( Matcher<Ent>
				.AllOf( Matcher_<Main,Comp>.I )
				.NoneOf(
					Matcher<Main,DontGenerateComp>.I,
					Matcher<Main,AlreadyImplementedComp>.I,
					Matcher<Main,EventListenerComp>.I ) );
		}

		protected override	Boolean					Filter					( Ent entity )
		{
			return entity.Has_<Comp>()
				&& !entity.Is<DontGenerateComp>()
				&& !entity.Is<AlreadyImplementedComp>();
		}

		protected override	void					Execute					( List<Ent> entities )
		{
			for ( var i = 0; i < entities.Count; i++ )
			{
				var ent				= entities[i];

				var contents		= COMPONENT_TEMPLATE
					.Replace("${FullComponentName}", ent.Get_<Comp>().Name );

				contents = contents
					.Replace(
						"${memberList}",
						ent.Has_<PublicFieldsComp>()
							? GenerateMemberAssignmentList( ent.Get_<PublicFieldsComp>().Values )
							: ""
					);

				var filePath		= "Components" + Path.DirectorySeparatorChar + ent.Get_<Comp>().Name.AddComponentSuffix(  ) + ".cs";
				var generatedBy		= GetType(  ).FullName;
				var fileEnt			= _contexts.Get<Main>().CreateEntity(  );
				fileEnt.Add_( new GeneratedFileComp( filePath, contents.WrapInNamespace( _contexts ), generatedBy ) );
			}
		}

		private				String				GenerateMemberAssignmentList( List<FieldInfo> memberData )
		{
			return String.Join( "\n", memberData
				.Select( info => $"    public {info.TypeName} {info.FieldName};" )
				.ToArray(  )
			);
        }
	}
}