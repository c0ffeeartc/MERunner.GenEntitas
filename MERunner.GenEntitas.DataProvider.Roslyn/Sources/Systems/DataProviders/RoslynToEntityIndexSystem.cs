﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using DesperateDevs.Utils;
using Entitas;
using Entitas.CodeGeneration.Attributes;
using Entitas.CodeGeneration.Plugins;
using Entitas.Generic;
using MERunner;
using Microsoft.CodeAnalysis;
using Ent = Entitas.Generic.Entity<Main>;

namespace GenEntitas.DataProvider.Roslyn
{
[Export(typeof(ISystem_Factory))]
public sealed class Factory_RoslynToEntityIndexSystem : TSystem_Factory<RoslynToEntityIndexSystem> {  }

	[Guid("0173C3E7-746D-45CC-866C-C743C0753938")]
	public class RoslynToEntityIndexSystem : ReactiveSystem<Ent>
	{
		public				RoslynToEntityIndexSystem	( Contexts contexts ) : base( contexts.Get<Main>() )
		{
			_contexts			= contexts;
		}

		private				Contexts				_contexts;

		protected override	ICollector<Ent>			GetTrigger				( IContext<Ent> context )
		{
			return context.CreateCollector( Matcher<Main,RoslynAllTypes>.I );
		}

		protected override	Boolean					Filter					( Ent entity )
		{
			return entity.Has_<RoslynAllTypes>();
		}

		protected override	void					Execute					( List<Ent> entities )
		{
			var types				= _contexts.Get<Main>().Get_<RoslynAllTypes>().Values;
			var typeToEntIndexData	= new Dictionary<String, List<EntityIndexData>>(  );
			var entityIndexDatas	= GetData( types );
			foreach ( var data in entityIndexDatas )
			{
				var key				= data.GetComponentType(  );
				if ( !typeToEntIndexData.ContainsKey( key ) )
				{
					typeToEntIndexData[key] = new List<EntityIndexData>(  );
				}
				typeToEntIndexData[key].Add( data );
			}

			var compEnts				= _contexts.Get<Main>().GetGroup( Matcher<Ent>
				.AllOf(
					Matcher<Main,Comp>.I,
					Matcher<Main,PublicFieldsComp>.I ) )
				.GetEntities(  );

			foreach( var ent in compEnts )
			{
				var compTypeName = ent.Get_<Comp>().FullTypeName;
				if ( !typeToEntIndexData.ContainsKey( compTypeName ) )
				{
					continue;
				}

				var entIndexDatas		= typeToEntIndexData[compTypeName];
				var publicFields		= ent.Get_<PublicFieldsComp>().Values;

				foreach ( var entityIndexData in entIndexDatas )
				{
					foreach ( var field in publicFields )
					{
						if ( entityIndexData.GetMemberName(  ) != field.FieldName )
						{
							continue;
						}
						var entIndexInfo		= new EntityIndexInfo(  );
						field.EntityIndexInfo	= entIndexInfo;

						entIndexInfo.EntityIndexData	= entityIndexData;
						entIndexInfo.Type				= entityIndexData.GetEntityIndexType(  );
						entIndexInfo.IsCustom			= entityIndexData.IsCustom(  );
						entIndexInfo.CustomMethods		= entityIndexData.IsCustom(  ) ? entityIndexData.GetCustomMethods(  ) : null;
						entIndexInfo.Name				= entityIndexData.GetEntityIndexName(  );
						entIndexInfo.ContextNames		= entityIndexData.GetContextNames(  );
						entIndexInfo.ComponentType		= entityIndexData.GetComponentType(  );
						entIndexInfo.MemberType			= entityIndexData.GetKeyType(  );
						entIndexInfo.MemberName			= entityIndexData.GetMemberName(  );
						entIndexInfo.HasMultple			= entityIndexData.GetHasMultiple(  );
					}
				}

				ent.Replace_( new PublicFieldsComp( publicFields ) );
			}

			var customEntityIndexDatas = GetCustomData( types );
			foreach ( var data in customEntityIndexDatas )
			{
				var ent			= _contexts.Get<Main>().CreateEntity(  );
				ent.Add_( new CustomEntityIndexComp( data ) );
			}
		}

		private				EntityIndexData[]		GetData					( List<INamedTypeSymbol> types )
		{
			var entityIndexData = types
				.Where( t => !t.IsAbstract )
                .Where( t => t.Implements( typeof( IComponent ) ) )
				.ToDictionary(
					type => type,
					type => RoslynToCompsSystem.GetPublicFieldAndPropertySymbols( type ) )
				.Where( kv => kv.Value.Any(info => info.HasAttribute( typeof( AbstractEntityIndexAttribute ) ) ) )
				.SelectMany( kv => createEntityIndexData( kv.Key, kv.Value ) )
				.ToArray(  );

			return entityIndexData;
		}

		private				EntityIndexData[]		GetCustomData			( List<INamedTypeSymbol> types )
		{
			var customEntityIndexData = types
				.Where( typeSymbol => !typeSymbol.IsAbstract )
				.Where( typeSymbol => typeSymbol.HasAttribute( typeof( CustomEntityIndexAttribute ) ) )
				.Select( createCustomEntityIndexData );
			return customEntityIndexData.ToArray(  );
		}

		private				EntityIndexData[]		createEntityIndexData	( INamedTypeSymbol type, List<ISymbol> infos )
		{
			var hasMultiple = infos.Count( i => i.CountAttribute( typeof( AbstractEntityIndexAttribute ) ) == 1 ) > 1;
			return infos
				.Where( i => i.CountAttribute( typeof( AbstractEntityIndexAttribute ) ) == 1 )
				.Select( info => {
					var data				= new EntityIndexData();
					var attribute			= info.GetAttribute( typeof( AbstractEntityIndexAttribute ) );
					var entityIndexType		= GetEntityIndexType( attribute.AttributeClass );
					var typeName			= ( info is IFieldSymbol fieldSymbol )
						? fieldSymbol.Type.ToDisplayString(  )
						: ( info is IPropertySymbol symbol )
							? symbol.Type.ToDisplayString(  )
							: null;

					data.SetEntityIndexType( entityIndexType.ToString(  ) );
					data.IsCustom(false);
					data.SetEntityIndexName(type.ToString().ToComponentName(_contexts.Get<Settings>().Is<IgnoreNamespaces>()));
					data.SetKeyType( typeName );
					data.SetComponentType( type.ToString(  ) );
					data.SetMemberName(info.Name);
					data.SetHasMultiple(hasMultiple);
					data.SetContextNames(type.GetContextNames(  ).ToArray(  ));

					return data;
				}).ToArray();
		}

		private				EntityIndexData			createCustomEntityIndexData(INamedTypeSymbol type)
		{
			var data				= new EntityIndexData();

			var attribute			= type.GetAttributes(  ).First( attr => attr.AttributeClass.ToString(  ) == typeof( CustomEntityIndexAttribute ).FullName );
			var attrArg0			= attribute.ConstructorArguments[0].Value as INamedTypeSymbol;  // FIXME: get string contextName

			data.SetEntityIndexType( type.ToString(  ) );
			data.IsCustom( true );
			data.SetEntityIndexName( type.ToString(  ).RemoveDots(  ) );
			data.SetHasMultiple( false );
			data.SetContextNames( new[] { attrArg0.Name.ShortTypeName(  ).RemoveContextSuffix(  ) } );

			var getMethods = type
				.GetMembers(  )
				.Where( m => m.Kind == SymbolKind.Method
					&& m is IMethodSymbol
					&& m.DeclaredAccessibility == Accessibility.Public
					&& m.HasAttribute( typeof( EntityIndexGetMethodAttribute ) ) )
				.Select( m  => new MethodData(
					(m as IMethodSymbol).ReturnType.ToDisplayString(  ),
					(m as IMethodSymbol).Name,
					(m as IMethodSymbol).Parameters
						.Select( p => new MemberData( p.Type.ToString(  ), p.Name ) )
						.ToArray( ) ) )
				.ToArray();

			data.SetCustomMethods( getMethods );

			return data;
		}

		private				String					GetEntityIndexType		( INamedTypeSymbol attribute )
		{
			if ( attribute.IsTypeOrHasBaseType( typeof( EntityIndexAttribute ) ) )
			{
				return "Entitas.EntityIndex";
			}

			if ( attribute.IsTypeOrHasBaseType( typeof( PrimaryEntityIndexAttribute ) ) )
			{
				return "Entitas.PrimaryEntityIndex";
			}

			throw new Exception("Unhandled EntityIndexType: " + attribute );
		}
	}
}