using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using Entitas;
using Entitas.CodeGeneration.Attributes;

using Entitas.Generic;
using MERunner;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ent = Entitas.Generic.Entity<Main>;

namespace GenEntitas.DataProvider.Roslyn
{
[Export(typeof(ISystem_Factory))]
public sealed class Factory_RoslynToCompsSystem : TSystem_Factory<RoslynToCompsSystem> {  }

	[Guid("9D65F12D-1E7A-4467-AB9F-58A52BD5556E")]
	public class RoslynToCompsSystem : ReactiveSystem<Ent>
	{
		public				RoslynToCompsSystem	( Contexts contexts ) : base( contexts.Get<Main>() )
		{
			_contexts			= contexts;
		}

		public				RoslynToCompsSystem	(  ) : this( Hub.Contexts )
		{
		}

		private				Contexts				_contexts;

		protected override	ICollector<Ent>			GetTrigger				( IContext<Ent> context )
		{
			return context.CreateCollector( Matcher_<Main,RoslynComponentTypes>.I );
		}

		protected override	Boolean					Filter					( Ent ent )
		{
			return ent.Has_<RoslynComponentTypes>();
		}

		protected override	void					Execute					( List<Ent> ents )
		{
			var typeSymbols = ents[0].Get_<RoslynComponentTypes>().Values;
			foreach ( var t in typeSymbols )
			{
				var ent = _contexts.Get<Main>().CreateEntity();
				ent.Add_( new INamedTypeSymbolComponent( t ) );

				ProvideDontGenerate( ent );
				ProvideComp( ent );
				ProvideContextNamesComp( ent );
				ProvideEventComp( ent );
				ProvideUniqueComp( ent );
				ProvidePublicFieldsComp( ent );
				ProvideFlagPrefix( ent );
				ProvideGenCompEntApiInterface_ForSingleContextAttr( ent );
			}
		}

		private				void					ProvideDontGenerate		( Ent ent )
		{
			ent.Flag<DontGenerateComp>( ent.Get_<INamedTypeSymbolComponent>().Value.HasAttribute( typeof( DontGenerateAttribute ) ) );
		}

		private				void					ProvideComp				( Ent ent )
		{
			var t					= ent.Get_<INamedTypeSymbolComponent>().Value;
			if ( t.Implements( typeof( IComponent ) ) )
			{
				var fullName		= String.IsNullOrEmpty( t.ContainingNamespace.Name )
					? t.Name
					: t.ContainingNamespace.Name + "." + t.Name;
				ent.Add_( new Comp( t.Name, fullName ) );
				ent.Flag<AlreadyImplementedComp>( true );
			}
			else
			{
				ent.Add_( new NonIComp( t.Name, t.ContainingNamespace.Name + "." + t.Name ) );
			}
		}

		private				void					ProvideContextNamesComp	( Ent ent )
		{
			var t					= ent.Get_<INamedTypeSymbolComponent>().Value;
			var contextNames		= t.GetContextNames(  );
			if ( contextNames.Count == 0 )
			{
				contextNames.Add( "Undefined" );
				return;
			}

			ent.Add_( new ContextNamesComp( contextNames ) );
		}

		private				void					ProvideUniqueComp		( Ent ent )
		{
			ent.Flag<UniqueComp>( ent.Get_<INamedTypeSymbolComponent>().Value.HasAttribute( typeof( UniqueAttribute ) ) );
		}

		private				void					ProvideFlagPrefix		( Ent ent )
		{
			var prefix				= "is";
			foreach ( var attr in ent.Get_<INamedTypeSymbolComponent>().Value.GetAttributes(  ) )
			{
				if ( attr.AttributeClass.ToString(  ) == typeof( Entitas.CodeGeneration.Attributes.FlagPrefixAttribute ).FullName )
				{
					prefix = (String)attr.ConstructorArguments[0].Value;
					break;
				}
			}
			ent.Add_( new UniquePrefixComp( prefix ) );
		}

		private				void					ProvideGenCompEntApiInterface_ForSingleContextAttr( Ent ent )
		{
			ent.Flag<GenCompEntApiInterface_ForSingleContext>( ent.Get_<INamedTypeSymbolComponent>().Value.HasAttribute( typeof( GenCompEntApiInterface_ForSingleContextAttribute ) ) );
		}

		private				void					ProvidePublicFieldsComp	( Ent ent )
		{
			var type					= ent.Get_<INamedTypeSymbolComponent>().Value;
			var memberInfos				= GetPublicFieldAndPropertyInfos( type );
			if ( memberInfos.Count == 0 )
			{
				return;
			}

			ent.Add_( new PublicFieldsComp( memberInfos ) );
		}

		public static		List<ISymbol>	GetPublicFieldAndPropertySymbols ( INamedTypeSymbol type )
		{
			return type.GetMembers(  )
				.Where( member => ( ( member is IFieldSymbol || ( member is IPropertySymbol && IsAutoProperty( (IPropertySymbol)member ) ) )
					&& !member.IsStatic
					&& member.DeclaredAccessibility == Accessibility.Public 
					&& member.CanBeReferencedByName ) // We don't want any backing fields here.
					)
				.Select( m => m )
				.ToList(  );
		}

		// TODO: FIXME: doesn't handle inheritance
		private				List<FieldInfo>			GetPublicFieldAndPropertyInfos ( INamedTypeSymbol type )
		{
			return GetPublicFieldAndPropertySymbols( type )
				.Select( symbol => new FieldInfo( 
					( symbol is IFieldSymbol )
						? ((IFieldSymbol)symbol).Type.ToDisplayString(  )
						: ((IPropertySymbol)symbol).Type.ToDisplayString(  ),
					symbol.Name ) )
				 .ToList(  );
		}

		private static		Boolean					IsAutoProperty			( IPropertySymbol member )
        {
            var ret = member.SetMethod != null
				&& member.GetMethod != null
				&& !member.GetMethod.DeclaringSyntaxReferences
					.First()
					.GetSyntax()
					.DescendantNodes()
					.Any(x => x is MethodDeclarationSyntax)
				&& !member.SetMethod.DeclaringSyntaxReferences
					.First()
					.GetSyntax()
					.DescendantNodes()
					.Any(x => x is MethodDeclarationSyntax);
			return ret;
        }

		private				void					ProvideEventComp		( Ent ent )
		{
			var type			= ent.Get_<INamedTypeSymbolComponent>().Value;
			var eventInfos		= type.GetAttributes(  )
				.Where( attr => attr.AttributeClass.ToString(  ) == typeof( EventAttribute ).FullName )
				.Select( attr => new EventInfo(
					eventTarget			: (EventTarget)attr.ConstructorArguments[0].Value,
					eventType			: (EventType)attr.ConstructorArguments[1].Value,
					priority			: (Int32)attr.ConstructorArguments[2].Value ) )
				.ToList(  );

			if ( eventInfos.Count <= 0 )
			{
				return;
			}

			ent.Add_( new EventComp( eventInfos ) );
			CodeGeneratorExtentions.ProvideEventCompNewEnts( _contexts, ent );
		}
	}
}