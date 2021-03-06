﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Entitas;
using Entitas.Generic;
using MERunner;
using Ent = Entitas.Generic.Entity<Settings>;

namespace GenEntitas
{
[Export(typeof(ISystem_Factory))]
public sealed class Factory_RoslynFixPathToSolutionSystem : TSystem_Factory<RoslynFixPathToSolutionSystem> {  }

	[Guid("43419B58-1DD7-4080-A9A8-B6DF6D021F1D")]
	public class RoslynFixPathToSolutionSystem : ReactiveSystem<Ent>
	{
		public				RoslynFixPathToSolutionSystem						( Contexts contexts ) : base( contexts.Get<Settings>() )
		{
		}

		protected override	ICollector<Ent>			GetTrigger				( IContext<Ent> context )
		{
			return context.CreateCollector( Matcher<Settings,RoslynPathToSolution>.I );
		}

		protected override	Boolean					Filter					( Ent ent )
		{
			return ent.Has_<RoslynPathToSolution>()
				&& !String.IsNullOrEmpty( ent.Get_<RoslynPathToSolution>().Value );
		}

		protected override	void					Execute					( List<Ent> ents )
		{
			var ent					= ents[0];
			var pathToSolution		= ent.Get_<RoslynPathToSolution>().Value;
			if ( !pathToSolution.EndsWith( ".sln") )  // FIXME: can cause bug_
			{
				return;
			}

			var newPathToSolution	= WorkaroundProjectNames( pathToSolution );
			if ( pathToSolution != newPathToSolution )
			{
				ent.Replace_( new RoslynPathToSolution( newPathToSolution ) );
			}
		}

		private static		String					WorkaroundProjectNames	(String pathToSolution)
		{
			PlatformID p = Environment.OSVersion.Platform;
			if ( p != PlatformID.MacOSX && p != PlatformID.Unix && p != (PlatformID) 128 )
			{
				return pathToSolution;
			}

			var lines = File.ReadAllLines(pathToSolution);
			for (int k = 0; k < lines.Count(); ++k)
			{
				var line = lines[k];
				var match = Regex.Match(line, "Project\\((.*)\\) = \"(.*)\", .*, ");
				if ( !match.Success )
				{
					continue;
				}

//				line = line.Replace(
//					"= \"" + match.Groups[2] + "\"",
//					"= \"" + match.Groups[2] + k + "\"");
				line = line.Replace( '\\', Path.DirectorySeparatorChar );

				lines[k] = line;
			}

			var newSolutionFileName = pathToSolution.Replace(".sln", "-entitas.sln");
			File.WriteAllLines(newSolutionFileName, lines);
			return newSolutionFileName;
		}
	}
}