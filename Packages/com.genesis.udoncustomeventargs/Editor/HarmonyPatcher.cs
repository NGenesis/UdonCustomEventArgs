using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEditor;

namespace UdonCustomEventArgs.Editor
{
    [InitializeOnLoad]
    internal class HarmonyPatcher
    {
        private static Harmony Harmony;

        static HarmonyPatcher()
        {
            Harmony = new Harmony("com.genesis.udoncustomeventargs");

            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if(!assembly.FullName.Contains("UdonSharp.Editor")) continue;

                foreach(var type in assembly.GetTypes())
                {
                    if(!type.FullName.Equals("UdonSharp.Compiler.Symbols.MethodSymbol")) continue;

                    // UdonSharp.Compiler.Symbols.MethodSymbol
                    // public virtual void Emit(EmitContext context)
                    var Emit = type.GetMethod("Emit", BindingFlags.Public | BindingFlags.Instance);
                    var EmitPrefixPatch = typeof(MethodSymbol).GetMethod(nameof(MethodSymbol.Emit), BindingFlags.NonPublic | BindingFlags.Static);

                    Harmony.Patch(Emit, new HarmonyMethod(EmitPrefixPatch), null);
                    return;
                }
            }
        }

        // namespace UdonSharp.Compiler.Symbols
        // internal abstract class MethodSymbol : Symbol
        internal static class MethodSymbol
        {
            // public virtual void Emit(EmitContext context)
            internal static void Emit(object __instance, object context)
            {
                // EmitContext.MethodLinkage linkage = context.GetMethodLinkage(this, false);
                var linkage = context.GetType().GetMethod("GetMethodLinkage", BindingFlags.Public | BindingFlags.Instance).Invoke(context, new object[] { __instance, false });

                // context.RootTable
                var RootTable = context.GetType().GetProperty("RootTable", BindingFlags.Public | BindingFlags.Instance).GetValue(context);

                // context.RootTable.CreateReflectionValue(...)
                var CreateReflectionValue = RootTable.GetType().GetMethod("CreateReflectionValue", BindingFlags.Public | BindingFlags.Instance);

                // context.GetTypeSymbol(...)
                var GetTypeSymbol = context.GetType().GetMethod("GetTypeSymbol", new Type[] { typeof(Type) }); // BindingFlags.Public | BindingFlags.Instance

                // linkage.MethodExportName
                var MethodExportName = linkage.GetType().GetProperty("MethodExportName", BindingFlags.Public | BindingFlags.Instance).GetValue(linkage) as string;

                // linkage.ParameterValues
                var ParameterValues = linkage.GetType().GetProperty("ParameterValues", BindingFlags.Public | BindingFlags.Instance).GetValue(linkage) as IEnumerable<object>;

                // Generate reflection data for method arguments
                if(!string.IsNullOrEmpty(MethodExportName))
                {
                    var parameterNames = new List<string>();
                    var parameterTypes = new List<Type>();

                    // foreach(var parameter in linkage.ParameterValues)
                    foreach(var parameter in ParameterValues)
                    {
                        // parameterNames.Add(parameter.UniqueID);
                        var UniqueID = parameter.GetType().GetProperty("UniqueID", BindingFlags.Public | BindingFlags.Instance).GetValue(parameter) as string;
                        parameterNames.Add(UniqueID);

                        // parameterTypes.Add(parameter.UdonType.SystemType);
                        var UdonType = parameter.GetType().GetProperty("UdonType", BindingFlags.Public | BindingFlags.Instance).GetValue(parameter);
                        var SystemType = UdonType.GetType().GetProperty("SystemType", BindingFlags.Public | BindingFlags.Instance).GetValue(UdonType) as Type;
                        parameterTypes.Add(SystemType);
                    }

                    // context.RootTable.CreateReflectionValue($"__refl_argnames_{linkage.MethodExportName}", context.GetTypeSymbol(typeof(string).MakeArrayType()), parameterNames.ToArray());
                    CreateReflectionValue.Invoke(RootTable, new object[] { $"__refl_argnames_{MethodExportName}", GetTypeSymbol.Invoke(context, new object[] { typeof(string).MakeArrayType() }), parameterNames.ToArray() });
                
                    // context.RootTable.CreateReflectionValue($"__refl_argtypes_{linkage.MethodExportName}", context.GetTypeSymbol(typeof(Type).MakeArrayType()), parameterTypes.ToArray());
                    CreateReflectionValue.Invoke(RootTable, new object[] { $"__refl_argtypes_{MethodExportName}", GetTypeSymbol.Invoke(context, new object[] { typeof(Type).MakeArrayType() }), parameterTypes.ToArray() });
                }
            }
        }
    }
}