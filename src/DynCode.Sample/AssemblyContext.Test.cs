using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace DynCode.Sample
{
    public static class AssemblyContext
    {
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern IntPtr GetLoadContextForAssembly(RuntimeAssembly assembly);

        // Returns the load context in which the specified assembly has been loaded
        public static AssemblyLoadContext GetLoadContext(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            AssemblyLoadContext loadContextForAssembly = null;

            RuntimeAssembly rtAsm = assembly as RuntimeAssembly;

            // We only support looking up load context for runtime assemblies.
            if (rtAsm != null)
            {
                IntPtr ptrAssemblyLoadContext = GetLoadContextForAssembly(rtAsm);
                if (ptrAssemblyLoadContext == IntPtr.Zero)
                {
                    // If the load context is returned null, then the assembly was bound using the TPA binder
                    // and we shall return reference to the active "Default" binder - which could be the TPA binder
                    // or an overridden CLRPrivBinderAssemblyLoadContext instance.
                    loadContextForAssembly = AssemblyLoadContext.Default;
                }
                else
                {
                    loadContextForAssembly = (AssemblyLoadContext)(GCHandle.FromIntPtr(ptrAssemblyLoadContext).Target);
                }
            }

            return loadContextForAssembly;
        }
    }
}
