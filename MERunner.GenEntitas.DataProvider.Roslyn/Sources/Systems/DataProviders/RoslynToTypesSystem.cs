﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Text;
using DesperateDevs.Utils;
using Entitas;
using Entitas.CodeGeneration.Attributes;
using Entitas.Generic;
using MERunner;
using Microsoft.Build.Construction;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Ent = Entitas.Generic.Entity<Settings>;
using Project = Microsoft.CodeAnalysis.Project;

namespace GenEntitas
{
[Export(typeof(ISystem_Factory))]
public sealed class Factory_RoslynToTypesSystem : TSystem_Factory<RoslynToTypesSystem> {  }

	[Guid("0F5D3CA4-FC93-469B-8D73-9C570EFE04F8")]
	public class RoslynToTypesSystem : ReactiveSystem<Ent>
	{
		public				RoslynToTypesSystem	( Contexts contexts ) : base( contexts.Get<Settings>() )
		{
			_contexts			= contexts;
		}

		private				Contexts				_contexts;

		protected override	ICollector<Ent>			GetTrigger				( IContext<Ent> context )
		{
			return context.CreateCollector( Matcher<Settings,RoslynPathToSolution>.I );
		}

		protected override	Boolean					Filter					( Ent entity )
		{
			return entity.Has_<RoslynPathToSolution>()
				&& !String.IsNullOrEmpty( entity.Get_<RoslynPathToSolution>().Value );
		}

		protected override	void					Execute					( List<Ent> ents )
		{
			{ // FIXME: HACK: remove after msbuild crossplatform fix
			var settingsDict		= _contexts.Get<Settings>().Get_<SettingsDict>().Dict;
			var MSBUILD_EXE_PATH	= "MSBUILD_EXE_PATH";

			if( !settingsDict.ContainsKey( MSBUILD_EXE_PATH ) )
			{
				throw new ArgumentException( "Roslyn requires `MSBUILD_EXE_PATH` in settings with path to msbuild or xbuild 14.0" );
			}

			var msBuildPathValue	= settingsDict[MSBUILD_EXE_PATH][0];
			var nativeSharedMethod	= typeof(SolutionFile).Assembly.GetType("Microsoft.Build.Shared.NativeMethodsShared");
			var isMonoField			= nativeSharedMethod.GetField("_isMono", BindingFlags.Static | BindingFlags.NonPublic);
			isMonoField.SetValue(null, true);
			Environment.SetEnvironmentVariable( MSBUILD_EXE_PATH, msBuildPathValue );
			}

			var pathToSolution		= ents[0].Get_<RoslynPathToSolution>().Value;
			var allTypes			= CollectAllInformation( pathToSolution );

			_contexts.Get<Main>().Replace_( new RoslynAllTypes( allTypes ) );

			var prefix = typeof( RoslynToTypesSystem ) + ": ";
			Console.WriteLine(prefix + "All types: " + allTypes.Count() );

			var possibleComponents = allTypes.Where(t => t.ToDisplayString().EndsWith("Component"));
			Console.WriteLine( prefix + "TypeName.EndsWith(\"Component\"): " + possibleComponents.Count(  ) );

			var compTypes = new List<INamedTypeSymbol>(  );

            foreach (var type in allTypes
                .Where( type => type.Implements( typeof( IComponent ) ) )
                .Where(type => !type.IsAbstract)
                .Where(type => GetContextNames(type).Any()))  // force usage of contextAttribute for IComponent
            {
                try
                {
//                    Console.WriteLine(prefix + "Found component " + type );
					compTypes.Add( type );
                }
                catch (Exception)
                {
                    Console.Error.WriteLine(prefix + "Fail to handle type: " + type.ToDisplayString());
                }
            }
            Console.WriteLine( prefix + "Components: " + compTypes.Count(  ) );

			var nonCompTypes = new List<INamedTypeSymbol>(  );

            foreach (var type in allTypes
               .Where( type => !type.Implements( typeof( IComponent ) ) )
               .Where(type => !type.IsGenericType)
               .Where(type => GetContextNames(type).Any()))
            {
                try
                {
//                    Console.WriteLine(prefix + "Found non-component " + type);
					nonCompTypes.Add( type );
                }
                catch (Exception)
                {
                    Console.Error.WriteLine( prefix + "Fail to handle type: " + type.ToDisplayString(  ) );
                }
            }
            Console.WriteLine( prefix + "Non-Components: " + nonCompTypes.Count(  ) );

			compTypes.AddRange( nonCompTypes );
			_contexts.Get<Main>().Replace_( new RoslynComponentTypes( compTypes.ToList(  ) ) );
		}

		private List<INamedTypeSymbol> CollectAllInformation(string pathToSolutionOrProjectFile)
		{
			using (var workspace = MSBuildWorkspace.Create())
			{
				var typeSymbols = new List<INamedTypeSymbol>(  );
				var sb = new StringBuilder(  );
				var filePaths = pathToSolutionOrProjectFile.Split(',');
				foreach ( var pathName in filePaths )
				{
					if (pathName.EndsWith(".sln"))
					{
						var solution = workspace.OpenSolutionAsync(pathName).Result;
						sb.Append( solution.FilePath + ": {" );

						var projectCount = 0;
						foreach (var project in solution.Projects)
						{
							var result = AnalyzeProject(project);
							typeSymbols.AddRange(result);
							sb.Append( project.Name ) ;
							sb.Append( ", " ) ;
							projectCount++;
						}
						if ( projectCount > 0 )
						{
							sb.Remove( sb.Length - 2, 2 );
						}
						sb.Append( "}, " );
					}
					else
					{
						var project = workspace.OpenProjectAsync(pathName).Result;
						var result = AnalyzeProject(project);
						typeSymbols.AddRange(result);
						sb.Append( project.Name ) ;
						sb.Append( ", " ) ;
					}
				}
				Console.WriteLine( typeof(RoslynToTypesSystem) + ": " + filePaths.Length.ToString() + " : " + sb );
				return typeSymbols;
			}
		}

        private List<INamedTypeSymbol> AnalyzeProject(Project project)
        {
            var compilation = project.GetCompilationAsync().Result;
            var allTypes = compilation.GetSymbolsWithName(x => true, SymbolFilter.Type).OfType<ITypeSymbol>().Where(t => t is INamedTypeSymbol).OfType<INamedTypeSymbol>();
			return allTypes.ToList(  );
		}

        public static List<string> GetContextNames(ISymbol type)
        {
            List<string> contextAttributes = new List<string>();

            foreach (var attribute in type.GetAttributes())
            {
                if ( attribute.AttributeClass.BaseType != null
                     && attribute.AttributeClass.BaseType.ToDisplayString().Contains(typeof (ContextAttribute).ToCompilableString()))
                {
                    // we need to go deeper!
                    var declaration = attribute.AttributeConstructor.DeclaringSyntaxReferences.First().GetSyntax();
                    var baseConstructorInit = (ConstructorInitializerSyntax) declaration.DescendantNodes().First(x => x.IsKind(SyntaxKind.BaseConstructorInitializer));
                    var name = (LiteralExpressionSyntax) baseConstructorInit.ArgumentList.Arguments.First().Expression;                    
                    contextAttributes.Add(name.ToString().Replace("\"",""));
                }
                if (attribute.AttributeClass.ToDisplayString().Contains(typeof(ContextAttribute).ToCompilableString()))
                {
                    var name = (string)attribute.ConstructorArguments.First().Value;
                    contextAttributes.Add(name);
                }
            }
            return contextAttributes;
        }
	}
}