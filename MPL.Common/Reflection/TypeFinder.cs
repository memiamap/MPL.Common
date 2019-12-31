﻿using System;
using System.IO;
using System.Reflection;

namespace MPL.Common.Reflection
{
    /// <summary>
    /// A class that provides helper functions to find a type.
    /// </summary>
    public static class TypeFinder
    {
        #region Methods
        #region _Private_
        private static Assembly LoadAssembly(string assemblyPath, bool silentExceptions = true)
        {
            Assembly ReturnValue = null;

            try
            {
                if (File.Exists(assemblyPath))
                    ReturnValue = Assembly.LoadFrom(assemblyPath);
            }
            catch (Exception)
            {
                if (!silentExceptions)
                    throw;
            }

            return ReturnValue;
        }

        #endregion
        #region _Public_
        /// <summary>
        /// Finds the first type matching the specified type name from the specified assembly.
        /// </summary>
        /// <param name="assemblyName">A string containing the assembly name that contains the type.</param>
        /// <param name="typeName">A string containing the name of the type to find.</param>
        /// <param name="type">A Type that will be set to the found type.</param>
        /// <returns>A bool that indicates whether the type was found</returns>
        public static bool TryFindType(string assemblyName, string typeName, out Type type)
        {
            bool ReturnValue = false;
            Assembly TargetAssembly = null;

            // Defaults
            type = null;

            foreach (Assembly Item in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (Item.FullName == assemblyName)
                {
                    TargetAssembly = Item;
                    break;
                }
            }

            if (TargetAssembly == null)
                TryLoadAssembly(assemblyName, out TargetAssembly);

            if (TargetAssembly != null)
            {
                foreach (Type SubItem in TargetAssembly.GetExportedTypes())
                    if (SubItem.FullName == typeName)
                    {
                        type = SubItem;
                        ReturnValue = true;
                        break;
                    }
            }

            return ReturnValue;
        }

        /// <summary>
        /// Tries to load the specified Assembly,
        /// </summary>
        /// <param name="assemblyName">A string containing the name of the Assembly to try and load.</param>
        /// <param name="assembly">An Assembly that will be set to the loaded Assembly.</param>
        /// <returns></returns>
        public static bool TryLoadAssembly(string assemblyName, out Assembly assembly)
        {
            bool ReturnValue = false;

            // Defaults
            assembly = null;

            // Append .dll if missing
            if (!assemblyName.ToLower().EndsWith(".dll"))
                assemblyName += ".dll";

            try
            {
                string[] Paths;

                Paths = new string[] { assemblyName,
                                       Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), assemblyName),
                                       Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assemblyName),
                                       Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", assemblyName)};

                for (int i = 0; i < Paths.Length; i++)
                {
                    assembly = LoadAssembly(Paths[i]);
                    if (assembly != null)
                    {
                        ReturnValue = true;
                        break;
                    }
                }
            }
            catch (Exception)
            {
                ReturnValue = false;
            }

            return ReturnValue;
        }

        #endregion
        #endregion
    }
}