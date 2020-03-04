using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Entitas;
using Entitas.Generic;
using MERunner;
using Ent = Entitas.Generic.Entity<Main>;

namespace GenEntitas
{
[Export(typeof(ISystem_Factory))]
public sealed class Factory_PostProcApplyDiffToDiskSystem : TSystem_Factory<PostProcApplyDiffToDiskSystem> {  }

	// Replaces PostProcCleanTargetDirSystem, PostProcWriteToDiskSystem
	[Guid("3EB8A0DE-D615-4263-AE20-5FD966814030")]
	public class PostProcApplyDiffToDiskSystem : ReactiveSystem<Ent>
	{
		public				PostProcApplyDiffToDiskSystem					( Contexts contexts ) : base( contexts.Get<Main>() )
		{
			_contexts			= contexts;
		}

		private				Contexts				_contexts;
		private				String					_generatePath;
		private				Boolean					_isDryRun;

		protected override	ICollector<Ent>			GetTrigger				( IContext<Ent> context )
		{
			return context.CreateCollector( Matcher<Ent>
				.AllOf(
					Matcher_<Main,GeneratedFileComp>.I )
				.NoneOf(
					Matcher<Main,Destroy>.I ) );
		}

		protected override	Boolean					Filter					( Ent entity )
		{
			return entity.Has_<GeneratedFileComp>()
				&& !entity.Is<Destroy>();
		}

		protected override	void					Execute					( List<Ent> ents )
		{
			var settings			= _contexts.Get<Settings>(  );
			_generatePath			= Path.Combine( settings.Get_<GeneratePath>().Value, "Generated" );
			_isDryRun				= settings.Is<RunInDryMode>();
			var stringBuilder		= new StringBuilder(  );

			DeleteNonGenFiles( ents, stringBuilder );

			foreach ( var ent in ents )
			{
				WriteFile( ent, stringBuilder );
			}

			if ( settings.Is<LogGeneratedPaths>() )
			{
				var s				= stringBuilder.ToString(  );
				if ( String.IsNullOrEmpty( s ) )
				{
					Log( "No changes found since previous run\n" );
				}
				else
				{
					Log( s );
				}
			}
		}

		private				void					Log						( String s )
		{
#if UNITY_EDITOR
			UnityEngine.Debug.Log( s );
#else
			Console.Write( s );
#endif
		}

		private				void					CreateDirIfNeeded		( String dirPath )
		{
			if ( !_isDryRun
				&& !String.IsNullOrEmpty( dirPath )
				&& !Directory.Exists( dirPath ) )
			{
				Directory.CreateDirectory( dirPath );
			}
		}

		private				void					Delete					( String path )
		{
			if ( _isDryRun )
			{
				return;
			}
			File.Delete( path );
		}

		private				void					Write					( String path, String contents )
		{
			if ( _isDryRun )
			{
				return;
			}
			File.WriteAllText( path, contents );
		}

		private				void					DeleteNonGenFiles		( List<Ent> ents, StringBuilder sb )
		{
			var settings				= _contexts.Get<Settings>(  );
			var dirInfo					= new DirectoryInfo( _generatePath );
			CreateDirIfNeeded( _generatePath );

			var curFiles				= dirInfo.Exists
				? dirInfo.GetFiles( "*.cs", SearchOption.AllDirectories ).ToList(  )
				: new List<FileInfo>(  );

			var entGenPaths				= ents.Select( ent=> Path.Combine( dirInfo.FullName, ent.Get_<GeneratedFileComp>().FilePath ) ).ToList(  );

			for ( var i = 0; i < curFiles.Count; i++ )
			{
				var f					= curFiles[i];
				var path				= f.FullName;
				if ( entGenPaths.Contains( path ) )
				{
					continue;
				}
				Delete( path );

				if ( settings.Is<LogGeneratedPaths>() )
				{
					sb.Append( " - " );
					sb.Append( path );
					sb.Append( "\n" );
				}
			}
		}

		private				void					WriteFile				( Ent ent, StringBuilder sb )
		{
			var targetPath				= Path.Combine( _generatePath, ent.Get_<GeneratedFileComp>().FilePath );
			var dirPath					= Path.GetDirectoryName( targetPath );
			CreateDirIfNeeded( dirPath );

			var settings				= _contexts.Get<Settings>();
			var contents				= ent.Get_<GeneratedFileComp>().Contents;
			var writeState				= WriteFileState.Undefined;
			if ( !File.Exists( targetPath ) )
			{
				writeState				= WriteFileState.Create;
				Write( targetPath, contents );
			}
			else if ( String.Compare( File.ReadAllText( targetPath ), contents, StringComparison.Ordinal ) != 0 )
			{
				writeState				= WriteFileState.Change;
				Write( targetPath, contents );
			}
			else
			{
				writeState				= WriteFileState.Keep;
			}

			if ( settings.Is<LogGeneratedPaths>()
				&& writeState != WriteFileState.Keep )
			{
				switch ( writeState )
				{
					case WriteFileState.Create:
							sb.Append( " + " );
							break;
					case WriteFileState.Change:
							sb.Append( " * " );
							break;
					case WriteFileState.Keep:
							sb.Append( "   " );
							break;
					default:
						throw new NotImplementedException(  );
				}
				sb.Append( targetPath );
				sb.Append( " - " );
				sb.Append( ent.Get_<GeneratedFileComp>().GeneratedBy );
				sb.Append( "\n" );
			}
		}

		private enum WriteFileState
		{
			Undefined,
			Create,
			Change,
			Keep,
		}
	}
}
