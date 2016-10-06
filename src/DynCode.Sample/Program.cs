using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyModel;
using System.Reflection;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace DynCode.Sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Get entry assembly:");
            var entryAssembly = Assembly.GetEntryAssembly();
            Console.WriteLine($"Entry assembly is {entryAssembly.FullName} in path {entryAssembly.Location}");

            Console.WriteLine("Showing references");
            ShowReferences();

            AssemblyLoadContext.Default.Resolving += CustomResolving;

            Console.WriteLine("Load assembly from path");
            var path = Path.GetFullPath(@"..\..\..\DynCode.Sample\src\DynamicAssembly\bin\Debug\netstandard1.6\DynamicAssembly.dll");
            var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
            Console.WriteLine($"Laoded assembly is {asm.FullName} in path {asm.Location}");
            CreateInstanceFromFirstAssemblyType(asm);

            Console.WriteLine("Create and load assembly from file");
            path = Path.GetFullPath(@"..\..\..\DynCode.Sample\src\Code\File.cs");
            asm = Compile("MyDynamicCreatedAsm", new List<string> { File.ReadAllText(path, Encoding.UTF8) });
            Console.WriteLine($"Created assembly is {asm.FullName} in path {asm.Location}");
            CreateInstanceFromFirstAssemblyType(asm);
        }

        private static Assembly CustomResolving(AssemblyLoadContext arg1, AssemblyName arg2)
        {
            Console.WriteLine($"Try resolve: {arg2.FullName}");
            //Maybe Load from different path e.g. Addon Path.
            return arg1.LoadFromAssemblyPath(@"C:\Addons\" + arg2.Name + ".dll");
        }

        public static void ShowReferences()
        {
            var context = DependencyContext.Default;

            if (!context.CompileLibraries.Any())
                Console.WriteLine("Compilation libraries empty");

            foreach (var compilationLibrary in context.CompileLibraries)
            {
                foreach (var resolvedPath in compilationLibrary.ResolveReferencePaths())
                {
                    Console.WriteLine($"Compilation {compilationLibrary.Name}:{Path.GetFileName(resolvedPath)}");
                    if (!File.Exists(resolvedPath))
                        Console.WriteLine($"Compilation library resolved to non existent path {resolvedPath}");
                }
            }

            foreach (var runtimeLibrary in context.RuntimeLibraries)
            {
                foreach (var assembly in runtimeLibrary.GetDefaultAssemblyNames(context))
                    Console.WriteLine($"Runtime {runtimeLibrary.Name}:{assembly.Name}");
            }
        }

        private static Assembly Compile(string assemblyName, IEnumerable<string> codes, IEnumerable<string> usings = null)
        {
            if (codes == null || !codes.Any())
                throw new ArgumentNullException(nameof(codes));

            //we need to get a tree per source
            var trees = new List<SyntaxTree>();
            var additionalUsings = usings != null ? usings.Select(s => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(s))).ToArray() : new UsingDirectiveSyntax[0];

            foreach (var code in codes)
            {
                // Parse the script to a SyntaxTree
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var root = (CompilationUnitSyntax)syntaxTree.GetRoot();

                if (additionalUsings.Any())
                    root = root.AddUsings(additionalUsings);

                trees.Add(SyntaxFactory.SyntaxTree(root));
            }

            // Compile the SyntaxTree to an in memory assembly
            var compilation = CSharpCompilation.Create(
                assemblyName,
                trees,
                GetMethaDataReferences(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                );

            using (var outputStream = new MemoryStream())
            {
                using (var pdbStream = new MemoryStream())
                {
                    var result = compilation.Emit(outputStream);
                    if (result.Success)
                    {
                        outputStream.Position = 0;
                        return AssemblyLoadContext.Default.LoadFromStream(outputStream);
                    }
                    else
                    {
                        Console.WriteLine(result.Diagnostics.Select(x => $"{x.ToString()}{Environment.NewLine}"));
                        return null;
                    }
                }
            }
        }

        private static IEnumerable<MetadataReference> GetMethaDataReferences()
        {
            return DependencyContext.Default
                .CompileLibraries
                .SelectMany(x => x.ResolveReferencePaths())
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => MetadataReference.CreateFromFile(path));
        }

        private static void CreateInstanceFromFirstAssemblyType(Assembly asm)
        {
            var firstType = asm.GetExportedTypes().FirstOrDefault();
            if (firstType != null)
            {
                var instance = Activator.CreateInstance(firstType);

                if (instance != null)
                    Console.WriteLine($"Instance of {firstType} created!");
            }
        }


    }
}
