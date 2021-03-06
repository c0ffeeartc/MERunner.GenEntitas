﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Entitas;
using Entitas.Generic;
using MERunner;
using Ent = Entitas.Generic.Entity<Settings>;

namespace GenEntitas
{
[Export(typeof(ISystem_Factory))]
public sealed class Factory_PostProcWriteGenPathsToCsprojSystem : TSystem_Factory<PostProcWriteGenPathsToCsprojSystem> {  }

	[Guid("9D790958-9D53-4C1F-B55A-EAEB4CC821A4")]
	public class PostProcWriteGenPathsToCsprojSystem : ReactiveSystem<Ent>
	{
		public				PostProcWriteGenPathsToCsprojSystem ( Contexts contexts ) : base( contexts.Get<Settings>() )
		{
			_contexts			= contexts;
			_generatedGroup		= contexts.Get<Main>().GetGroup( Matcher<Entity<Main>>
				.AllOf(
					Matcher<Main,GeneratedFileComp>.I )
				.NoneOf(
					Matcher<Main,Destroy>.I ) );
		}

		private				Contexts				_contexts;
		private				IGroup<Entity<Main>>	_generatedGroup;
		private				String					_generatePath;

		protected override	ICollector<Ent>			GetTrigger				( IContext<Ent> context )
		{
			return context.CreateCollector( Matcher<Settings,WriteGeneratedPathsToCsProj>.I );
		}

		protected override	Boolean					Filter					( Ent ent )
		{
			return ent.Has_<WriteGeneratedPathsToCsProj>();
		}

		protected override	void					Execute					( List<Ent> entities )
		{
			var path			= entities[0].Get_<WriteGeneratedPathsToCsProj>().Value;
			if ( String.IsNullOrEmpty( path ) )
			{
				return;
			}

			if ( !File.Exists( path ) )
			{
				throw new FileNotFoundException( path );
			}

			var settings		= _contexts.Get<Settings>();
			_generatePath		= Path.Combine( settings.Get_<GeneratePath>().Value, "Generated" );

			var contents		= File.ReadAllText( path );
			contents			= RemoveExistingGeneratedEntires( contents );
			contents			= AddGeneratedEntires( contents );

			File.WriteAllText( path, contents );
		}

		private				String					RemoveExistingGeneratedEntires	( String contents )
		{
			var pattern			= "\\s*<Compile Include=\"" + _generatePath.Replace("/", "\\").Replace("\\", "\\\\") + ".* \\/>";
			contents			= Regex.Replace(contents, pattern, string.Empty);
			return Regex.Replace(contents, "\\s*<ItemGroup>\\s*<\\/ItemGroup>", "" );
		}

		private				String					AddGeneratedEntires		( String contents )
		{
			var ents			= _generatedGroup.GetEntities(  );
			if ( ents.Length == 0 )
			{
				return contents;
			}

			var entryTemplate	= "    <Compile Include=\"" + _generatePath.Replace("/", "\\") + "\\{0}\" />";

			var entries			= new List<String>(  );
			foreach ( var ent in ents )
			{
				var entry = String.Format( entryTemplate, ent.Get_<GeneratedFileComp>().FilePath.Replace( "/", "\\" ) );
				entries.Add( entry );
			}

			var entriesItemGroup = String.Format("</ItemGroup>\n  <ItemGroup>\n{0}\n  </ItemGroup>", String.Join( "\r\n", entries ) );
			//Console.WriteLine( entriesItemGroup );
			return new Regex("<\\/ItemGroup>").Replace( contents, entriesItemGroup, 1 );
		}
	}
}