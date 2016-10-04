using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace DynCode.Sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(@"C:\Users\Benjamin\Source\GitHub\DynCode.Sample\src\DynamicAssembly\bin\Debug\netstandard1.6\DynamicAssembly.dll");



        }
    }
}
