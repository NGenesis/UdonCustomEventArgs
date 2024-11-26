using UdonSharp;
using VRC.Udon.Common.Interfaces;
using System;

public static class UdonCustomEventExtensions
{
    public static void SendCustomEvent<T>(this IUdonEventReceiver instance, string eventName, T value) => SendCustomEventArgs(instance, eventName, new object[] { value });
    public static void SendCustomEvent<T0, T1>(this IUdonEventReceiver instance, string eventName, T0 value0, T1 value1) => SendCustomEventArgs(instance, eventName, new object[] { value0, value1 });
    public static void SendCustomEvent<T0, T1, T2>(this IUdonEventReceiver instance, string eventName, T0 value0, T1 value1, T2 value2) => SendCustomEventArgs(instance, eventName, new object[] { value0, value1, value2 });
    public static void SendCustomEvent<T0, T1, T2, T3>(this IUdonEventReceiver instance, string eventName, T0 value0, T1 value1, T2 value2, T3 value3) => SendCustomEventArgs(instance, eventName, new object[] { value0, value1, value2, value3 });
    public static void SendCustomEvent<T0, T1, T2, T3, T4>(this IUdonEventReceiver instance, string eventName, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4) => SendCustomEventArgs(instance, eventName, new object[] { value0, value1, value2, value3, value4 });
    public static void SendCustomEvent<T0, T1, T2, T3, T4, T5>(this IUdonEventReceiver instance, string eventName, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5) => SendCustomEventArgs(instance, eventName, new object[] { value0, value1, value2, value3, value4, value5 });
    public static void SendCustomEvent<T0, T1, T2, T3, T4, T5, T6>(this IUdonEventReceiver instance, string eventName, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6) => SendCustomEventArgs(instance, eventName, new object[] { value0, value1, value2, value3, value4, value5, value6 });
    public static void SendCustomEvent<T0, T1, T2, T3, T4, T5, T6, T7>(this IUdonEventReceiver instance, string eventName, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7) => SendCustomEventArgs(instance, eventName, new object[] { value0, value1, value2, value3, value4, value5, value6, value7 });
    public static void SendCustomEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this IUdonEventReceiver instance, string eventName, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8) => SendCustomEventArgs(instance, eventName, new object[] { value0, value1, value2, value3, value4, value5, value6, value7, value8 });
    public static void SendCustomEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this IUdonEventReceiver instance, string eventName, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9) => SendCustomEventArgs(instance, eventName, new object[] { value0, value1, value2, value3, value4, value5, value6, value7, value8, value9 });

    public static void SendCustomEvent<T>(this UdonSharpBehaviour instance, string eventName, T value) => SendCustomEventArgs((IUdonEventReceiver)instance, eventName, new object[] { value });
    public static void SendCustomEvent<T0, T1>(this UdonSharpBehaviour instance, string eventName, T0 value0, T1 value1) => SendCustomEventArgs((IUdonEventReceiver)instance, eventName, new object[] { value0, value1 });
    public static void SendCustomEvent<T0, T1, T2>(this UdonSharpBehaviour instance, string eventName, T0 value0, T1 value1, T2 value2) => SendCustomEventArgs((IUdonEventReceiver)instance, eventName, new object[] { value0, value1, value2 });
    public static void SendCustomEvent<T0, T1, T2, T3>(this UdonSharpBehaviour instance, string eventName, T0 value0, T1 value1, T2 value2, T3 value3) => SendCustomEventArgs((IUdonEventReceiver)instance, eventName, new object[] { value0, value1, value2, value3 });
    public static void SendCustomEvent<T0, T1, T2, T3, T4>(this UdonSharpBehaviour instance, string eventName, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4) => SendCustomEventArgs((IUdonEventReceiver)instance, eventName, new object[] { value0, value1, value2, value3, value4 });
    public static void SendCustomEvent<T0, T1, T2, T3, T4, T5>(this UdonSharpBehaviour instance, string eventName, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5) => SendCustomEventArgs((IUdonEventReceiver)instance, eventName, new object[] { value0, value1, value2, value3, value4, value5 });
    public static void SendCustomEvent<T0, T1, T2, T3, T4, T5, T6>(this UdonSharpBehaviour instance, string eventName, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6) => SendCustomEventArgs((IUdonEventReceiver)instance, eventName, new object[] { value0, value1, value2, value3, value4, value5, value6 });
    public static void SendCustomEvent<T0, T1, T2, T3, T4, T5, T6, T7>(this UdonSharpBehaviour instance, string eventName, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7) => SendCustomEventArgs((IUdonEventReceiver)instance, eventName, new object[] { value0, value1, value2, value3, value4, value5, value6, value7 });
    public static void SendCustomEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this UdonSharpBehaviour instance, string eventName, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8) => SendCustomEventArgs((IUdonEventReceiver)instance, eventName, new object[] { value0, value1, value2, value3, value4, value5, value6, value7, value8 });
    public static void SendCustomEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this UdonSharpBehaviour instance, string eventName, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9) => SendCustomEventArgs((IUdonEventReceiver)instance, eventName, new object[] { value0, value1, value2, value3, value4, value5, value6, value7, value8, value9 });

    public static void SendCustomEventArgs(this UdonSharpBehaviour instance, string eventName, object[] args) => SendCustomEventArgs((IUdonEventReceiver)instance, eventName, args);
    public static void SendCustomEventArgs(this IUdonEventReceiver instance, string eventName, object[] args)
    {
        if(instance == null) return;

        if(args == null || args.Length == 0)
        {
            // No arguments supplied, call event without arguments
            instance.SendCustomEvent(eventName);
            return;
        }

        // Find bindable event
        bool foundBindableEvent = false;
        int eventIndex = 0;
        string resolvedEventName = string.Empty;
        while(!foundBindableEvent)
        {
            resolvedEventName = $"__{eventIndex++}_{eventName}";

            // Ensure event exists
            var resolvedEventTypesName = $"__refl_argtypes_{resolvedEventName}";
            if(instance.GetProgramVariableType(resolvedEventTypesName) == null) break;
            if(CanBindCustomEventArgs(instance, (Type[])instance.GetProgramVariable(resolvedEventTypesName), args)) foundBindableEvent = true;
        }

        if(!foundBindableEvent)
        {
            // No bindable events found, call event without arguments
            instance.SendCustomEvent(eventName);
            return;
        }

        // Retrieve argument names
        var argNames = (string[])instance.GetProgramVariable($"__refl_argnames_{resolvedEventName}");
        if(argNames == null || argNames.Length == 0)
        {
            // No argument overrides found, call event without arguments
            instance.SendCustomEvent(eventName);
            return;
        }

        // Bind arguments and call event
        for(var i = 0; i < args.Length && i < argNames.Length; ++i) instance.SetProgramVariable(argNames[i], args[i]);
        instance.SendCustomEvent(resolvedEventName);
    }

    private static bool CanBindCustomEventArgs(this IUdonEventReceiver instance, Type[] argTypes, object[] args)
    {
        if(args == null || argTypes == null || args.Length != argTypes.Length) return false;

        for(var i = 0; i < args.Length; ++i)
        {
            if(args[i] != null && argTypes[i] != args[i].GetType()) return false;
        }

        return true;
    }
}
