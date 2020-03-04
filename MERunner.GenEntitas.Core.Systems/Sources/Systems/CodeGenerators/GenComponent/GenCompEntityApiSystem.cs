using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using DesperateDevs.Utils;
using Entitas;
using Entitas.Generic;
using MERunner;
using Ent = Entitas.Generic.Entity<Main>;

namespace GenEntitas
{
[Export(typeof(ISystem_Factory))]
public sealed class Factory_GenCompEntityApiSystem : TSystem_Factory<GenCompEntityApiSystem> {  }

	[Guid("16BE46FC-D969-4E05-B250-7B3371AC34E8")]
	public class GenCompEntityApiSystem : ReactiveSystem<Ent>
	{
		public				GenCompEntityApiSystem	( Contexts contexts ) : base( contexts.Get<Main>() )
		{
			_contexts = contexts;
		}

		private				Contexts				_contexts;

        private const		String					STANDARD_TEMPLATE		=
@"public partial class ${EntityType} {

    public ${ComponentType} ${validComponentName} { get { return (${ComponentType})GetComponent(${Index}); } }
    public bool has${ComponentName} { get { return HasComponent(${Index}); } }

    public void Add${ComponentName}(${newMethodParameters}) {
        var index = ${Index};
        var componentPool = GetComponentPool(index);
        var component = componentPool.Count > 0 ? (${ComponentType})componentPool.Pop() : new ${ComponentType}();
${memberAssignmentList}
        AddComponent(index, component);
    }

    public void Replace${ComponentName}(${newMethodParameters}) {
        var index = ${Index};
        var componentPool = GetComponentPool(index);
        var component = componentPool.Count > 0 ? (${ComponentType})componentPool.Pop() : new ${ComponentType}();
${memberAssignmentList}
        ReplaceComponent(index, component);
    }

    public void Remove${ComponentName}() {
        RemoveComponent(${Index});
    }
}
";

		private const		String					FLAG_TEMPLATE			=
            @"public partial class ${EntityType} {

    static readonly ${ComponentType} ${componentName}Component = new ${ComponentType}();

    public bool ${prefixedComponentName} {
        get { return HasComponent(${Index}); }
        set {
            if (value != ${prefixedComponentName}) {
                var index = ${Index};
                if (value) {
                    var componentPool = GetComponentPool(index);
                    var component = componentPool.Count > 0
                            ? componentPool.Pop()
                            : ${componentName}Component;

                    AddComponent(index, component);
                } else {
                    RemoveComponent(index);
                }
            }
        }
    }
}
";

		protected override	ICollector<Ent>			GetTrigger				( IContext<Ent> context )
		{
			return context.CreateCollector( Matcher<Ent>
				.AllOf(
					Matcher_<Main,Comp>.I )
				.NoneOf(
					Matcher<Main,DontGenerateComp>.I ) );
		}

		protected override	Boolean					Filter					( Ent entity )
		{
			return entity.Has_<Comp>()
				&& !entity.Is<DontGenerateComp>();
		}

		protected override	void					Execute					( List<Ent> entities )
		{
			foreach ( var ent in entities )
			{
				var contextNames = ent.Get_<ContextNamesComp>().Values;
				foreach ( var contextName in contextNames )
				{
					var template		= ent.Has_<PublicFieldsComp>() ? STANDARD_TEMPLATE : FLAG_TEMPLATE;

					var filePath		= contextName + Path.DirectorySeparatorChar + "Components" + Path.DirectorySeparatorChar + contextName + ent.Get_<Comp>().Name.AddComponentSuffix(  ) + ".cs";

					var contents = template
						.Replace( _contexts, ent, contextName );

					if ( ent.Has_<PublicFieldsComp>() )
					{
						contents = contents
							.Replace( "${memberAssignmentList}", GenerateMemberAssignmentList( ent.Get_<PublicFieldsComp>().Values.ToArray(  ) ) );
					}

					var generatedBy		= GetType().FullName;

					var fileEnt			= _contexts.Get<Main>().CreateEntity(  );
					fileEnt.Add_( new GeneratedFileComp( filePath, contents.WrapInNamespace( _contexts ), generatedBy ) );
				}
			}
		}

		private				String				GenerateMemberAssignmentList( FieldInfo[] memberData )
		{
			return String.Join("\n", memberData
				.Select(info => "        component." + info.FieldName + " = new" + info.FieldName.UppercaseFirst() + ";")
				.ToArray()
			);
        }
	}
}
