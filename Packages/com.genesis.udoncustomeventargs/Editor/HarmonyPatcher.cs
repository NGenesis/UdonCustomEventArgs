using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UdonSharp;
using UnityEditor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading.Tasks;

namespace UdonCustomEventArgs.Editor
{
    [InitializeOnLoad]
    internal class HarmonyPatcher
    {
        private static Harmony Harmony;
        
        private static readonly string UdonSharpPackageName = "com.merlin.udonsharp";
        private static readonly Version UdonSharpPackageVersion = new Version(1, 2, 0);
#if UDONSHARP_1_2_OR_NEWER
        private static bool UseLegacyDelayEvents => false;
#else
        private static bool UseLegacyDelayEvents => !Version.TryParse(UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages().FirstOrDefault(p => p.name == UdonSharpPackageName)?.version, out var packageVersion) || packageVersion < UdonSharpPackageVersion;
#endif
        private static readonly string UseLegacyDelayEventsDefineSymbol = "UDONCUSTOMEVENTARGS_DELAYEVENTS_LEGACY";

        private static void UpdatePackageDefineSymbols()
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(';');

            if(!UseLegacyDelayEvents && defines.Contains(UseLegacyDelayEventsDefineSymbol, StringComparer.OrdinalIgnoreCase))
            {
                var modifiedDefines = defines.Where(d => d != UseLegacyDelayEventsDefineSymbol);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", modifiedDefines));
            }
            else if(UseLegacyDelayEvents && !defines.Contains(UseLegacyDelayEventsDefineSymbol, StringComparer.OrdinalIgnoreCase))
            {
                var modifiedDefines = defines.AddItem(UseLegacyDelayEventsDefineSymbol);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", modifiedDefines));
            }
        }

        static HarmonyPatcher()
        {
            UpdatePackageDefineSymbols();

            Harmony = new Harmony("com.genesis.udoncustomeventargs");

            var assemblyTypes = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName.StartsWith("UdonSharp.Editor")).GetTypes();

            // UdonSharp.Compiler.Symbols.MethodSymbol
            var methodSymbolType = assemblyTypes.FirstOrDefault(t => t.FullName == "UdonSharp.Compiler.Symbols.MethodSymbol");
            if(methodSymbolType != null)
            {
                // public virtual void Emit(EmitContext context)
                var Emit = methodSymbolType.GetMethod("Emit", BindingFlags.Public | BindingFlags.Instance);
                var EmitPrefixPatch = typeof(MethodSymbol).GetMethod(nameof(MethodSymbol.Emit), BindingFlags.NonPublic | BindingFlags.Static);
                Harmony.Patch(Emit, new HarmonyMethod(EmitPrefixPatch));
            }

            // UdonSharp.Compiler.CompilationContext
            var compilationContextType = assemblyTypes.FirstOrDefault(t => t.FullName == "UdonSharp.Compiler.CompilationContext");
            if(compilationContextType != null)
            {
                // public ModuleBinding[] LoadSyntaxTreesAndCreateModules(IEnumerable<string> sourcePaths, string[] scriptingDefines)
                var LoadSyntaxTreesAndCreateModules = compilationContextType.GetMethod("LoadSyntaxTreesAndCreateModules", BindingFlags.Public | BindingFlags.Instance);
                var LoadSyntaxTreesAndCreateModulesPostfixPatch = typeof(CompilationContext).GetMethod(nameof(CompilationContext.LoadSyntaxTreesAndCreateModules), BindingFlags.NonPublic | BindingFlags.Static);
                Harmony.Patch(LoadSyntaxTreesAndCreateModules, null, new HarmonyMethod(LoadSyntaxTreesAndCreateModulesPostfixPatch));
            }
        }

        // namespace UdonSharp.Compiler
        // internal class CompilationContext
        internal static class CompilationContext
        {
            // public ModuleBinding[] LoadSyntaxTreesAndCreateModules(IEnumerable<string> sourcePaths, string[] scriptingDefines)
            internal static void LoadSyntaxTreesAndCreateModules(ref object __result)
            {
                // Skip injection when there are scripts to be upgraded
                if(!UdonSharpProgramAsset.GetAllUdonSharpPrograms().All(e => e.ScriptVersion >= UdonSharpProgramVersion.CurrentVersion)) return;

                Parallel.ForEach(__result as IEnumerable<object>, moduleBinding =>
                {
                    var filePath = moduleBinding.GetType().GetField("filePath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(moduleBinding) as string;
                    var tree = moduleBinding.GetType().GetField("tree", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    tree.SetValue(moduleBinding, InjectDispatcherMembers(filePath, tree.GetValue(moduleBinding) as SyntaxTree));
                });
            }

            internal static SyntaxTree InjectDispatcherMembers(string sourceFilePath, SyntaxTree sourceTree)
            {
#if UDONCUSTOMEVENTARGS_DELAYEVENTS_LEGACY
                var codeGenSource = @"
                public class UdonCustomEventSchedulerCodeGen
                {
                    [System.NonSerialized] public object[] __CustomEventScheduler;

                    public void __UdonCustomEventScheduler_ScheduleTimeUpdate() => __UdonCustomEventScheduler_ProcessEvent(0, VRC.Udon.Common.Enums.EventTiming.Update);
                    public void __UdonCustomEventScheduler_ScheduleFrameUpdate() => __UdonCustomEventScheduler_ProcessEvent(1, VRC.Udon.Common.Enums.EventTiming.Update);

                    public void __UdonCustomEventScheduler_ScheduleTimeLateUpdate() => __UdonCustomEventScheduler_ProcessEvent(0, VRC.Udon.Common.Enums.EventTiming.LateUpdate);
                    public void __UdonCustomEventScheduler_ScheduleFrameLateUpdate() => __UdonCustomEventScheduler_ProcessEvent(1, VRC.Udon.Common.Enums.EventTiming.LateUpdate);

                    private void __UdonCustomEventScheduler_ProcessEvent(int eventQueueType, VRC.Udon.Common.Enums.EventTiming eventTiming)
                    {
                        var eventQueue = (VRC.SDK3.Data.DataList)((object[])__CustomEventScheduler[eventQueueType])[(int)eventTiming]; // TimeEventQueues[(int)eventTiming]; or FrameEventQueues[(int)eventTiming];
                        var queuedEvent = eventQueue[0];
                        var queuedEventData = (object[])queuedEvent.Reference; // eventQueue[0]
                        eventQueue.Remove(queuedEvent);
                        this.SendCustomEventArgs((string)queuedEventData[0], (object[])queuedEventData[1]); // this.SendCustomEventArgs(queuedEvent.EventName, queuedEvent.EventArgs);
                    }
                }";
#else
                var codeGenSource = @"
                public class UdonCustomEventSchedulerCodeGen
                {
                    [System.NonSerialized] public UdonCustomEventArgs.UdonCustomEventScheduler __CustomEventScheduler;

                    public void __UdonCustomEventScheduler_ScheduleTimeUpdate() => __CustomEventScheduler.ProcessTimeEvent(VRC.Udon.Common.Enums.EventTiming.Update);
                    public void __UdonCustomEventScheduler_ScheduleFrameUpdate() => __CustomEventScheduler.ProcessFrameEvent(VRC.Udon.Common.Enums.EventTiming.Update);

                    public void __UdonCustomEventScheduler_ScheduleTimeLateUpdate() => __CustomEventScheduler.ProcessTimeEvent(VRC.Udon.Common.Enums.EventTiming.LateUpdate);
                    public void __UdonCustomEventScheduler_ScheduleFrameLateUpdate() => __CustomEventScheduler.ProcessFrameEvent(VRC.Udon.Common.Enums.EventTiming.LateUpdate);
                }";
#endif
                var dispatcherTree = CSharpSyntaxTree.ParseText(codeGenSource, CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.None).WithPreprocessorSymbols(new string[] { "UDONSHARP_1_2_OR_NEWER" }));
                var sourceRoot = sourceTree.GetCompilationUnitRoot();
                var dispatcherRoot = dispatcherTree.GetCompilationUnitRoot();
                var dispatcherClass = dispatcherRoot.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                bool isModified = false;

                // Find classes that inherit from UdonSharpBehaviour
                var modifiedRoot = sourceRoot.ReplaceNodes(sourceRoot.DescendantNodes().OfType<ClassDeclarationSyntax>(), (originalNode, _) =>
                {
                    // Inject dispatcher members into the class
                    var baseType = originalNode.BaseList?.Types.FirstOrDefault(bt => bt.ToString() == "UdonSharpBehaviour");
                    if(baseType != null)
                    {
                        isModified = true;
                        return originalNode.AddMembers(dispatcherClass.Members.ToArray());
                    }

                    return originalNode;
                });

                // Nothing was modified, return the original tree
                if(!isModified) return sourceTree;

                // Identify missing usings by comparing source and dispatcher usings
                var existingUsings = sourceRoot.Usings.Select(u => u.Name.ToString()).ToHashSet();
                var missingUsings = dispatcherRoot.Usings.Where(u => !existingUsings.Contains(u.Name.ToString()));

                // Add missing usings to the source root
                modifiedRoot = modifiedRoot.AddUsings(missingUsings.ToArray());

                // Return the modified syntax tree with updated root
                return sourceTree.WithRootAndOptions(modifiedRoot, sourceTree.Options);
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

                // linkage.ReturnValue
                var ReturnValue = linkage.GetType().GetProperty("ReturnValue", BindingFlags.Public | BindingFlags.Instance).GetValue(linkage);

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

                    // var ReturnValueName = linkage.ReturnValue.UniqueID;
                    var ReturnUniqueID = ReturnValue?.GetType()?.GetProperty("UniqueID", BindingFlags.Public | BindingFlags.Instance)?.GetValue(ReturnValue) as string;

                    // var ReturnValueType = linkage.ReturnValue.UdonType.SystemType;
                    var ReturnUdonType = ReturnValue?.GetType()?.GetProperty("UdonType", BindingFlags.Public | BindingFlags.Instance)?.GetValue(ReturnValue);
                    var ReturnSystemType = ReturnUdonType?.GetType()?.GetProperty("SystemType", BindingFlags.Public | BindingFlags.Instance)?.GetValue(ReturnUdonType) as Type;

                    // context.RootTable.CreateReflectionValue($"__refl_argnames_{linkage.MethodExportName}", context.GetTypeSymbol(typeof(string).MakeArrayType()), parameterNames.ToArray());
                    CreateReflectionValue.Invoke(RootTable, new object[] { $"__refl_argnames_{MethodExportName}", GetTypeSymbol.Invoke(context, new object[] { typeof(string).MakeArrayType() }), parameterNames.ToArray() });
                
                    // context.RootTable.CreateReflectionValue($"__refl_argtypes_{linkage.MethodExportName}", context.GetTypeSymbol(typeof(Type).MakeArrayType()), parameterTypes.ToArray());
                    CreateReflectionValue.Invoke(RootTable, new object[] { $"__refl_argtypes_{MethodExportName}", GetTypeSymbol.Invoke(context, new object[] { typeof(Type).MakeArrayType() }), parameterTypes.ToArray() });

                    // context.RootTable.CreateReflectionValue($"__refl_returnname_{linkage.MethodExportName}", context.GetTypeSymbol(typeof(string)), ReturnValueName);
                    CreateReflectionValue.Invoke(RootTable, new object[] { $"__refl_returnname_{MethodExportName}", GetTypeSymbol.Invoke(context, new object[] { typeof(string) }), ReturnUniqueID });
                
                    // context.RootTable.CreateReflectionValue($"__refl_returntype_{linkage.MethodExportName}", context.GetTypeSymbol(typeof(Type)), ReturnValueType);
                    CreateReflectionValue.Invoke(RootTable, new object[] { $"__refl_returntype_{MethodExportName}", GetTypeSymbol.Invoke(context, new object[] { typeof(Type) }), ReturnSystemType });
                }
            }
        }
    }
}