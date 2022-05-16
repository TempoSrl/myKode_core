using System;
using Microsoft.CSharp;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using Microsoft.CodeAnalysis.Emit;

namespace mdl {

    /// <summary>
    /// Helper class for compiling c# code
    /// </summary>
    public class Compiler {


        private static long _nfun;

        /// <summary>
        /// Number of temporary vars declared
        /// </summary>
        public int Nvars;

        /// <summary>
        /// Retursn a new function name
        /// </summary>
        /// <returns></returns>
        public static string GetNewFunName() {
            _nfun++;
            return $"dynamic_fun{_nfun}";
        }

        /// <summary>
        /// Get a new variable name
        /// </summary>
        /// <returns></returns>
        public string GetNewVarName() {
            Nvars++;
            return $"o{Nvars}";
        }

        private readonly List<string> _segments = new List<string>();

        /// <summary>
        /// Adding a segment of code to compile
        /// </summary>
        /// <param name="segment"></param>
        public void AddSegment(string segment) {
            _segments.Add(segment);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeName">Name of the class containing the method to compile</param>
        /// <param name="funcBody">Function body</param>
        /// <param name="referencedAssemblies"></param>
        /// <param name="referencedDll"></param>
        /// <returns></returns>
        public MethodInfo Compile(string typeName, string funcBody, IEnumerable<string> referencedAssemblies, IEnumerable<string> referencedDll) {
            var assembly = CompileAssembly(typeName, funcBody, referencedAssemblies, referencedDll);
            if (assembly == null)
                return null;
            var program = assembly.GetType(typeName);
            var main = program.GetMethod("apply");
            return main;
        }

        public Assembly CompileAssembly(string typeName, string funcBody, IEnumerable<string> referencedAssemblies, IEnumerable<string> referencedDll) {
            IEnumerable<string> DefaultNamespaces =
         new[]{
                "System",
                "System.Data",
                "System.IO",
                "System.Net",
                "System.Linq",
                "System.Text",
                "System.Text.RegularExpressions",
                "System.Collections",
                "System.Collections.Generic",
                "System.Dynamic",
                "mdl"
         };

         //https://www.tugberkugurlu.com/archive/compiling-c-sharp-code-into-memory-and-executing-it-with-roslyn
       

            string runTimePath = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();

			//         var DefaultReferences = new List<MetadataReference>();
			//         DefaultReferences.Append(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
			//         DefaultReferences.Append(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location));
			//         DefaultReferences.Append(MetadataReference.CreateFromFile(typeof(Dictionary<,>).Assembly.Location));

			//         foreach (var dll in referencedDll) {
			//             DefaultReferences.Add(MetadataReference.CreateFromFile(Path.Combine(runTimePath, dll)));
			//}

			//         DefaultReferences.Append(MetadataReference.CreateFromFile(typeof(Dictionary<,>).Assembly.Location));

		

			var trustedAssembliesPaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator);
            var neededAssemblies = new[]
            {
                "System.Private.CoreLib",
                    "System.Runtime.dll",
                    "System.Collections",
                    "mscorlib.dll",
            };

            var references = trustedAssembliesPaths
                .Where(p => referencedDll.Contains(Path.GetFileName(p)) || neededAssemblies.Contains(Path.GetFileNameWithoutExtension(p))) //
                .Select(p => MetadataReference.CreateFromFile(p))
                .ToList();

            Assembly.GetEntryAssembly().GetReferencedAssemblies()
            .ToList()
            .ForEach(assembly => references.Add(MetadataReference.CreateFromFile(Assembly.Load(assembly).Location)));

            StringBuilder code = new StringBuilder();
            foreach (var ass in referencedAssemblies) {
                code.AppendLine($"using {ass};");
            }
            foreach (var segment in _segments) {
                code.AppendLine(segment);
            }
            code.AppendLine(funcBody);

            var parsedSyntaxTree =  SyntaxFactory.ParseSyntaxTree(code.ToString(), 
                CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5), "");


            //"mdl_aux." + newClassName
            var compilation
               = CSharpCompilation.Create($"{typeName}", 
               syntaxTrees: new SyntaxTree[] { parsedSyntaxTree }, 
               references: references, //DefaultReferences,
               options:new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithOverflowChecks(true).WithOptimizationLevel(OptimizationLevel.Debug)
                    .WithUsings(DefaultNamespaces)
                 );

            try {
                using (var ms = new MemoryStream()) {
                    var result = compilation.Emit(ms);  //Path.GetTempFileName()

                    if (!result.Success) {
                        IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                            diagnostic.IsWarningAsError ||
                            diagnostic.Severity == DiagnosticSeverity.Error);

                        foreach (Diagnostic diagnostic in failures) {
                            Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                        }
                    }
                    else {
                        ms.Seek(0, SeekOrigin.Begin);
                        Console.WriteLine(result.Success ? "Sucess!!" : "Failed");
                        return Assembly.Load(ms.ToArray());
                    }
                }


            }
            catch (Exception ex) {
                Console.WriteLine(ex);               
            }
            return null;

        }
        public static bool compileEnabled = false;



        /// <summary>
        /// 
        /// </summary>
        /// <param name="funcBody">Function body</param>
        /// <param name="referencedAssemblies"></param>
        /// <param name="referencedDll"></param>
        /// <returns></returns>
        public Assembly oldCompileAssembly(string funcBody, IEnumerable<string> referencedAssemblies, IEnumerable<string> referencedDll) {
            if (!compileEnabled)
                return null;
            //int handler = metaprofiler.StartTimer("compileAssembly*" + typeName);
            //var options = new ProviderOptions(CompilerVersion: "v3.7");  includes C# 8.0 (Visual Studio 2019 version 16.7, .NET Core 3.1)
            //options.CompilerVersion =;

            var options = new Dictionary<string, string> { { "CompilerVersion", "v3.7" } }; // 

            var provider = new CSharpCodeProvider(); //options
                                                     //var parameters = CreateCompilerParameters(m_References)

            var parameters = new CompilerParameters() {
                GenerateInMemory = true,    //false - external file generation
                GenerateExecutable = false,
                TempFiles = new TempFileCollection(Path.GetTempPath(), false) // new TempFileCollection(".",false)
            };

            var code = new StringBuilder();
            foreach (var ass in referencedAssemblies) {
                code.AppendLine($"using {ass};");
            }
            foreach (var ass in referencedDll) {
                parameters.ReferencedAssemblies.Add(ass);
            }

            parameters.ReferencedAssemblies.Add(typeof(MetaData).Assembly.Location);

            foreach (var segment in _segments) {
                code.AppendLine(segment);
            }
            code.AppendLine(funcBody);
            try {
                var results = provider.CompileAssemblyFromSource(parameters, code.ToString());
                provider.Dispose();
                if (results.Errors.HasErrors) {
                    var sb = new StringBuilder();
                    foreach (CompilerError error in results.Errors) {
                        sb.AppendLine($"Error (Line {error.Line} {error.ErrorNumber}): {error.ErrorText} \n\r");
                        sb.AppendLine($"column{error.Column}");
                        sb.AppendLine($"code:");
                        sb.AppendLine($"{code}");
                    }

                    ErrorLogger.Logger.MarkEvent(sb.ToString());
                    //metaprofiler.StopTimer(handler);
                    return null;
                }
                return results.CompiledAssembly;
            }
            catch (Exception e) {
                compileEnabled = false;
                ErrorLogger.Logger.logException("Compiling " + code, e);
                return null;
            }

            //metaprofiler.StopTimer(handler);

        }
    }


}


