using System;
using UdonSharp;
using UnityEngine;
using VRC.Udon.Common.Enums;
using VRC.Udon.Common.Interfaces;

#if UDONCUSTOMEVENTARGS_DELAYEVENTS_LEGACY
using VRC.SDK3.Data;
#else
using System.Collections.Generic;
using UdonCustomEventArgs;

namespace UdonCustomEventArgs
{
    internal class UdonCustomEventSchedulerTimeEvent : IComparable<UdonCustomEventSchedulerTimeEvent>
    {
        public UdonCustomEventSchedulerTimeEvent(string eventName, object[] eventArgs, float eventTime, ulong eventID)
        {
            EventName = eventName;
            EventArgs = eventArgs;
            EventTime = eventTime;
            EventID = eventID;
        }

        public string EventName;
        public object[] EventArgs;
        public float EventTime;
        public ulong EventID;

        public int CompareTo(UdonCustomEventSchedulerTimeEvent other)
        {
            var num = EventTime.CompareTo(other.EventTime);
            if(num != 0) return num;
            return EventID.CompareTo(other.EventID);
        }
    }

    internal class UdonCustomEventSchedulerFrameEvent : IComparable<UdonCustomEventSchedulerFrameEvent>
    {
        public UdonCustomEventSchedulerFrameEvent(string eventName, object[] eventArgs, int eventFrame, ulong eventID)
        {
            EventName = eventName;
            EventArgs = eventArgs;
            EventFrame = eventFrame;
            EventID = eventID;
        }

        public string EventName;
        public object[] EventArgs;
        public int EventFrame;
        public ulong EventID;

        public int CompareTo(UdonCustomEventSchedulerFrameEvent other)
        {
            var num = EventFrame.CompareTo(other.EventFrame);
            if(num != 0) return num;
            return EventID.CompareTo(other.EventID);
        }
    }

    //internal class UdonCustomEventSchedulerTimeEventComparer : IComparer<UdonCustomEventSchedulerTimeEvent>, System.Collections.IComparer
    //{
    //    public int Compare(object a, object b) => Compare((UdonCustomEventSchedulerTimeEvent)a, (UdonCustomEventSchedulerTimeEvent)b);
    //    public int Compare(UdonCustomEventSchedulerTimeEvent a, UdonCustomEventSchedulerTimeEvent b) => a.CompareTo(b);
    //}

    //internal class UdonCustomEventSchedulerFrameEventComparer : IComparer<UdonCustomEventSchedulerFrameEvent>, System.Collections.IComparer
    //{
    //    public int Compare(object a, object b) => Compare((UdonCustomEventSchedulerFrameEvent)a, (UdonCustomEventSchedulerFrameEvent)b);
    //    public int Compare(UdonCustomEventSchedulerFrameEvent a, UdonCustomEventSchedulerFrameEvent b) => a.CompareTo(b);
    //}

    public class UdonCustomEventScheduler
    {
        private List<UdonCustomEventSchedulerTimeEvent>[] TimeEventQueues;
        private List<UdonCustomEventSchedulerFrameEvent>[] FrameEventQueues;
        private IUdonEventReceiver Instance;
        private ulong NextEventID;

        public UdonCustomEventScheduler(IUdonEventReceiver instance)
        {
            Instance = instance;

            TimeEventQueues = new List<UdonCustomEventSchedulerTimeEvent>[]
            {
                new List<UdonCustomEventSchedulerTimeEvent>(), // EventTiming.Update
                new List<UdonCustomEventSchedulerTimeEvent>() // EventTiming.LateUpdate
            };

            FrameEventQueues = new List<UdonCustomEventSchedulerFrameEvent>[]
            {
                new List<UdonCustomEventSchedulerFrameEvent>(), // EventTiming.Update
                new List<UdonCustomEventSchedulerFrameEvent>() // EventTiming.LateUpdate
            };
        }

        public void ProcessTimeEvent(EventTiming eventTiming)
        {
            var eventQueue = TimeEventQueues[(int)eventTiming];
            var queuedEvent = eventQueue[0];
            eventQueue.Remove(queuedEvent);
            Instance.SendCustomEventArgs(queuedEvent.EventName, queuedEvent.EventArgs);
        }

        public void ProcessFrameEvent(EventTiming eventTiming)
        {
            var eventQueue = FrameEventQueues[(int)eventTiming];
            var queuedEvent = eventQueue[0];
            eventQueue.Remove(queuedEvent);
            Instance.SendCustomEventArgs(queuedEvent.EventName, queuedEvent.EventArgs);
        }

        public void ScheduleTimeEvent(string eventName, float delaySeconds, EventTiming eventTiming, object[] eventArgs)
        {
            AddTimeEvent(TimeEventQueues[(int)eventTiming], new UdonCustomEventSchedulerTimeEvent(eventName, eventArgs, Time.time + Math.Max(delaySeconds, 0.001f), NextEventID++));
            Instance.SendCustomEventDelayedSeconds($"__UdonCustomEventScheduler_ScheduleTime{eventTiming}", delaySeconds, eventTiming);
        }

        public void ScheduleFrameEvent(string eventName, int delayFrames, EventTiming eventTiming, object[] eventArgs)
        {
            AddFrameEvent(FrameEventQueues[(int)eventTiming], new UdonCustomEventSchedulerFrameEvent(eventName, eventArgs, Time.frameCount + Math.Max(delayFrames, 1), NextEventID++));
            Instance.SendCustomEventDelayedFrames($"__UdonCustomEventScheduler_ScheduleFrame{eventTiming}", delayFrames, eventTiming);
        }

        private void AddTimeEvent(List<UdonCustomEventSchedulerTimeEvent> eventQueue, UdonCustomEventSchedulerTimeEvent item)
        {
            if(eventQueue.Count == 0)
            {
                eventQueue.Add(item);
                return;
            }

            if(eventQueue[eventQueue.Count - 1].CompareTo(item) <= 0)
            {
                eventQueue.Add(item);
                return;
            }

            if(eventQueue[0].CompareTo(item) >= 0)
            {
                eventQueue.Insert(0, item);
                return;
            }

            int index = BinarySearch(eventQueue, item);
            if(index < 0) index = ~index;
            eventQueue.Insert(index, item);
        }

        private void AddFrameEvent(List<UdonCustomEventSchedulerFrameEvent> eventQueue, UdonCustomEventSchedulerFrameEvent item)
        {
            if(eventQueue.Count == 0)
            {
                eventQueue.Add(item);
                return;
            }

            if(eventQueue[eventQueue.Count - 1].CompareTo(item) <= 0)
            {
                eventQueue.Add(item);
                return;
            }

            if(eventQueue[0].CompareTo(item) >= 0)
            {
                eventQueue.Insert(0, item);
                return;
            }

            int index = BinarySearch(eventQueue, item);
            if(index < 0) index = ~index;
            eventQueue.Insert(index, item);
        }

        private int BinarySearch(List<UdonCustomEventSchedulerFrameEvent> array, UdonCustomEventSchedulerFrameEvent target)
        {
            int low = 0;
            int high = array.Count - 1;

            while(low <= high)
            {
                int mid = low + ((high - low) >> 1); // Avoid overflow
                var midValue = array[mid];
                int comparison = midValue.CompareTo(target);

                if(comparison == 0) return mid; // Target found
                else if (comparison < 0) low = mid + 1; // Search in the right half
                else high = mid - 1; // Search in the left half
            }

            return ~low; // Target not found, return bitwise complement of the insertion point
        }

        private int BinarySearch(List<UdonCustomEventSchedulerTimeEvent> array, UdonCustomEventSchedulerTimeEvent target)
        {
            int low = 0;
            int high = array.Count - 1;

            while(low <= high)
            {
                int mid = low + ((high - low) >> 1); // Avoid overflow
                var midValue = array[mid];
                int comparison = midValue.CompareTo(target);

                if(comparison == 0) return mid; // Target found
                else if (comparison < 0) low = mid + 1; // Search in the right half
                else high = mid - 1; // Search in the left half
            }

            return ~low; // Target not found, return bitwise complement of the insertion point
        }
    }
}
#endif

public static class UdonCustomEventExtensions
{
    #region Custom events
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
        int eventIndex = 0;
        string resolvedEventName = string.Empty;
        float resolvedEventScore = 0f;
        while(resolvedEventScore < 1f)
        {
            var candidateEventName = $"__{eventIndex++}_{eventName}";

            // Ensure event exists
            var candidateEventTypesName = $"__refl_argtypes_{candidateEventName}";
            if(instance.GetProgramVariableType(candidateEventTypesName) == null) break;

            // Determine best event method to bind
            if(CanBindCustomEventArgs(instance, (Type[])instance.GetProgramVariable(candidateEventTypesName), args, out var candidateScore) && candidateScore > resolvedEventScore)
            {
                resolvedEventName = candidateEventName;
                resolvedEventScore = candidateScore;
            }
        }

        if(resolvedEventScore <= 0f)
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

    private static bool CanBindCustomEventArgs(this IUdonEventReceiver instance, Type[] argTypes, object[] args, out float score)
    {
        score = 0f;
        if(args == null || argTypes == null || args.Length != argTypes.Length) return false;

        for(var i = 0; i < args.Length; ++i)
        {
            var argType = argTypes[i];
            if(argType == null) return false;

            var arg = args[i];
            if(arg == null)
            {
                if(argType.IsValueType) return false;
            }
            else
            {
                if(!argType.IsAssignableFrom(arg.GetType())) return false;
                score += (argType == arg.GetType() ? 1f : 0.5f) / argTypes.Length;
            }
        }

        score = 1f;
        return true;
    }
    #endregion
    #region Returnable custom events
    public static bool TryExecuteCustomEvent<TResult>(this IUdonEventReceiver instance, string eventName, out TResult returnValue) => TryExecuteCustomEventArgs(instance, eventName, out returnValue);
    public static bool TryExecuteCustomEvent<TResult, T>(this IUdonEventReceiver instance, string eventName, out TResult returnValue, T value) => TryExecuteCustomEventArgs(instance, eventName, out returnValue, new object[] { value });
    public static bool TryExecuteCustomEvent<TResult, T0, T1>(this IUdonEventReceiver instance, string eventName, out TResult returnValue, T0 value0, T1 value1) => TryExecuteCustomEventArgs(instance, eventName, out returnValue, new object[] { value0, value1 });
    public static bool TryExecuteCustomEvent<TResult, T0, T1, T2>(this IUdonEventReceiver instance, string eventName, out TResult returnValue, T0 value0, T1 value1, T2 value2) => TryExecuteCustomEventArgs(instance, eventName, out returnValue, new object[] { value0, value1, value2 });
    public static bool TryExecuteCustomEvent<TResult, T0, T1, T2, T3>(this IUdonEventReceiver instance, string eventName, out TResult returnValue, T0 value0, T1 value1, T2 value2, T3 value3) => TryExecuteCustomEventArgs(instance, eventName, out returnValue, new object[] { value0, value1, value2, value3 });
    public static bool TryExecuteCustomEvent<TResult, T0, T1, T2, T3, T4>(this IUdonEventReceiver instance, string eventName, out TResult returnValue, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4) => TryExecuteCustomEventArgs(instance, eventName, out returnValue, new object[] { value0, value1, value2, value3, value4 });
    public static bool TryExecuteCustomEvent<TResult, T0, T1, T2, T3, T4, T5>(this IUdonEventReceiver instance, string eventName, out TResult returnValue, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5) => TryExecuteCustomEventArgs(instance, eventName, out returnValue, new object[] { value0, value1, value2, value3, value4, value5 });
    public static bool TryExecuteCustomEvent<TResult, T0, T1, T2, T3, T4, T5, T6>(this IUdonEventReceiver instance, string eventName, out TResult returnValue, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6) => TryExecuteCustomEventArgs(instance, eventName, out returnValue, new object[] { value0, value1, value2, value3, value4, value5, value6 });
    public static bool TryExecuteCustomEvent<TResult, T0, T1, T2, T3, T4, T5, T6, T7>(this IUdonEventReceiver instance, string eventName, out TResult returnValue, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7) => TryExecuteCustomEventArgs(instance, eventName, out returnValue, new object[] { value0, value1, value2, value3, value4, value5, value6, value7 });
    public static bool TryExecuteCustomEvent<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8>(this IUdonEventReceiver instance, string eventName, out TResult returnValue, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8) => TryExecuteCustomEventArgs(instance, eventName, out returnValue, new object[] { value0, value1, value2, value3, value4, value5, value6, value7, value8 });
    public static bool TryExecuteCustomEvent<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this IUdonEventReceiver instance, string eventName, out TResult returnValue, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9) => TryExecuteCustomEventArgs(instance, eventName, out returnValue, new object[] { value0, value1, value2, value3, value4, value5, value6, value7, value8, value9 });

    public static bool TryExecuteCustomEvent<TResult>(this UdonSharpBehaviour instance, string eventName, out TResult returnValue) => TryExecuteCustomEventArgs((IUdonEventReceiver)instance, eventName, out returnValue);
    public static bool TryExecuteCustomEvent<TResult, T>(this UdonSharpBehaviour instance, string eventName, out TResult returnValue, T value) => TryExecuteCustomEventArgs((IUdonEventReceiver)instance, eventName, out returnValue, new object[] { value });
    public static bool TryExecuteCustomEvent<TResult, T0, T1>(this UdonSharpBehaviour instance, string eventName, out TResult returnValue, T0 value0, T1 value1) => TryExecuteCustomEventArgs((IUdonEventReceiver)instance, eventName, out returnValue, new object[] { value0, value1 });
    public static bool TryExecuteCustomEvent<TResult, T0, T1, T2>(this UdonSharpBehaviour instance, string eventName, out TResult returnValue, T0 value0, T1 value1, T2 value2) => TryExecuteCustomEventArgs((IUdonEventReceiver)instance, eventName, out returnValue, new object[] { value0, value1, value2 });
    public static bool TryExecuteCustomEvent<TResult, T0, T1, T2, T3>(this UdonSharpBehaviour instance, string eventName, out TResult returnValue, T0 value0, T1 value1, T2 value2, T3 value3) => TryExecuteCustomEventArgs((IUdonEventReceiver)instance, eventName, out returnValue, new object[] { value0, value1, value2, value3 });
    public static bool TryExecuteCustomEvent<TResult, T0, T1, T2, T3, T4>(this UdonSharpBehaviour instance, string eventName, out TResult returnValue, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4) => TryExecuteCustomEventArgs((IUdonEventReceiver)instance, eventName, out returnValue, new object[] { value0, value1, value2, value3, value4 });
    public static bool TryExecuteCustomEvent<TResult, T0, T1, T2, T3, T4, T5>(this UdonSharpBehaviour instance, string eventName, out TResult returnValue, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5) => TryExecuteCustomEventArgs((IUdonEventReceiver)instance, eventName, out returnValue, new object[] { value0, value1, value2, value3, value4, value5 });
    public static bool TryExecuteCustomEvent<TResult, T0, T1, T2, T3, T4, T5, T6>(this UdonSharpBehaviour instance, string eventName, out TResult returnValue, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6) => TryExecuteCustomEventArgs((IUdonEventReceiver)instance, eventName, out returnValue, new object[] { value0, value1, value2, value3, value4, value5, value6 });
    public static bool TryExecuteCustomEvent<TResult, T0, T1, T2, T3, T4, T5, T6, T7>(this UdonSharpBehaviour instance, string eventName, out TResult returnValue, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7) => TryExecuteCustomEventArgs((IUdonEventReceiver)instance, eventName, out returnValue, new object[] { value0, value1, value2, value3, value4, value5, value6, value7 });
    public static bool TryExecuteCustomEvent<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8>(this UdonSharpBehaviour instance, string eventName, out TResult returnValue, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8) => TryExecuteCustomEventArgs((IUdonEventReceiver)instance, eventName, out returnValue, new object[] { value0, value1, value2, value3, value4, value5, value6, value7, value8 });
    public static bool TryExecuteCustomEvent<TResult, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this UdonSharpBehaviour instance, string eventName, out TResult returnValue, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9) => TryExecuteCustomEventArgs((IUdonEventReceiver)instance, eventName, out returnValue, new object[] { value0, value1, value2, value3, value4, value5, value6, value7, value8, value9 });

    public static bool TryExecuteCustomEventArgs<TResult>(this UdonSharpBehaviour instance, string eventName, out TResult returnValue) => TryExecuteCustomEventArgs((IUdonEventReceiver)instance, eventName, out returnValue);
    public static bool TryExecuteCustomEventArgs<TResult>(this IUdonEventReceiver instance, string eventName, out TResult returnValue)
    {
        if(instance == null)
        {
            returnValue = default;
            return false;
        }

        // Check return type
        if(instance.GetProgramVariableType($"__refl_returntype_{eventName}") == null)
        {
            returnValue = default;
            return false;
        }

        var returnValueName = (string)instance.GetProgramVariable($"__refl_returnname_{eventName}");
        if(returnValueName == null)
        {
            returnValue = default;
            return false;
        }

        instance.SendCustomEvent(eventName);
        returnValue = (TResult)instance.GetProgramVariable(returnValueName);
        return true;
    }

    public static bool TryExecuteCustomEventArgs<TResult>(this UdonSharpBehaviour instance, string eventName, out TResult returnValue, object[] args) => TryExecuteCustomEventArgs((IUdonEventReceiver)instance, eventName, out returnValue, args);
    public static bool TryExecuteCustomEventArgs<TResult>(this IUdonEventReceiver instance, string eventName, out TResult returnValue, object[] args)
    {
        if(instance == null)
        {
            returnValue = default;
            return false;
        }

        if(args == null || args.Length == 0) return TryExecuteCustomEventArgs(instance, eventName, out returnValue);

        // Find bindable event
        int eventIndex = 0;
        string resolvedEventName = string.Empty;
        float resolvedEventScore = 0f;
        while(resolvedEventScore < 1f)
        {
            var candidateEventName = $"__{eventIndex++}_{eventName}";

            // Ensure event exists
            var candidateEventTypesName = $"__refl_argtypes_{candidateEventName}";
            if(instance.GetProgramVariableType(candidateEventTypesName) == null) break;

            // Determine best event method to bind
            if(CanBindCustomEventArgs(instance, (Type[])instance.GetProgramVariable(candidateEventTypesName), args, out var candidateScore) && candidateScore > resolvedEventScore)
            {
                resolvedEventName = candidateEventName;
                resolvedEventScore = candidateScore;
            }
        }

        // No bindable events found, call event without arguments
        if(resolvedEventScore <= 0f) return TryExecuteCustomEventArgs(instance, eventName, out returnValue);

        // Check return type
        var resolvedEventReturnTypeName = $"__refl_returntype_{resolvedEventName}";
        if(instance.GetProgramVariableType(resolvedEventReturnTypeName) == null)
        {
            returnValue = default;
            return false;
        }

        var returnValueName = (string)instance.GetProgramVariable($"__refl_returnname_{resolvedEventName}");
        if(returnValueName == null)
        {
            returnValue = default;
            return false;
        }

        // Retrieve argument names
        var argNames = (string[])instance.GetProgramVariable($"__refl_argnames_{resolvedEventName}");

        // No argument overrides found, call event without arguments
        if(argNames == null || argNames.Length == 0) return TryExecuteCustomEventArgs(instance, eventName, out returnValue);

        // Bind arguments and call event
        for(var i = 0; i < args.Length && i < argNames.Length; ++i) instance.SetProgramVariable(argNames[i], args[i]);
        instance.SendCustomEvent(resolvedEventName);
        returnValue = (TResult)instance.GetProgramVariable(returnValueName);
        return true;
    }
    #endregion
    #region Delayed custom events
    public static void SendCustomEventDelayedSeconds(this IUdonEventReceiver instance, string eventName, float delaySeconds, params object[] args) => SendCustomEventArgsDelayedSeconds(instance, eventName, delaySeconds, EventTiming.Update, args);
    public static void SendCustomEventDelayedSeconds<T>(this IUdonEventReceiver instance, string eventName, float delaySeconds, T value) => SendCustomEventArgsDelayedSeconds(instance, eventName, delaySeconds, EventTiming.Update, new object[] { value });
    public static void SendCustomEventDelayedSeconds<T0, T1>(this IUdonEventReceiver instance, string eventName, float delaySeconds, T0 value0, T1 value1) => SendCustomEventArgsDelayedSeconds(instance, eventName, delaySeconds, EventTiming.Update, new object[] { value0, value1 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2>(this IUdonEventReceiver instance, string eventName, float delaySeconds, T0 value0, T1 value1, T2 value2) => SendCustomEventArgsDelayedSeconds(instance, eventName, delaySeconds, EventTiming.Update, new object[] { value0, value1, value2 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3>(this IUdonEventReceiver instance, string eventName, float delaySeconds, T0 value0, T1 value1, T2 value2, T3 value3) => SendCustomEventArgsDelayedSeconds(instance, eventName, delaySeconds, EventTiming.Update, new object[] { value0, value1, value2, value3 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3, T4>(this IUdonEventReceiver instance, string eventName, float delaySeconds, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4) => SendCustomEventArgsDelayedSeconds(instance, eventName, delaySeconds, EventTiming.Update, new object[] { value0, value1, value2, value3, value4 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3, T4, T5>(this IUdonEventReceiver instance, string eventName, float delaySeconds, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5) => SendCustomEventArgsDelayedSeconds(instance, eventName, delaySeconds, EventTiming.Update, new object[] { value0, value1, value2, value3, value4, value5 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3, T4, T5, T6>(this IUdonEventReceiver instance, string eventName, float delaySeconds, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6) => SendCustomEventArgsDelayedSeconds(instance, eventName, delaySeconds, EventTiming.Update, new object[] { value0, value1, value2, value3, value4, value5, value6 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3, T4, T5, T6, T7>(this IUdonEventReceiver instance, string eventName, float delaySeconds, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7) => SendCustomEventArgsDelayedSeconds(instance, eventName, delaySeconds, EventTiming.Update, new object[] { value0, value1, value2, value3, value4, value5, value6, value7 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this IUdonEventReceiver instance, string eventName, float delaySeconds, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8) => SendCustomEventArgsDelayedSeconds(instance, eventName, delaySeconds, EventTiming.Update, new object[] { value0, value1, value2, value3, value4, value5, value6, value7, value8 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this IUdonEventReceiver instance, string eventName, float delaySeconds, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9) => SendCustomEventArgsDelayedSeconds(instance, eventName, delaySeconds, EventTiming.Update, new object[] { value0, value1, value2, value3, value4, value5, value6, value7, value8, value9 });

    public static void SendCustomEventDelayedSeconds(this IUdonEventReceiver instance, string eventName, float delaySeconds, EventTiming eventTiming, params object[] args) => SendCustomEventArgsDelayedSeconds(instance, eventName, delaySeconds, eventTiming, args);
    public static void SendCustomEventDelayedSeconds<T>(this IUdonEventReceiver instance, string eventName, float delaySeconds, EventTiming eventTiming, T value) => SendCustomEventArgsDelayedSeconds(instance, eventName, delaySeconds, eventTiming, new object[] { value });
    public static void SendCustomEventDelayedSeconds<T0, T1>(this IUdonEventReceiver instance, string eventName, float delaySeconds, EventTiming eventTiming, T0 value0, T1 value1) => SendCustomEventArgsDelayedSeconds(instance, eventName, delaySeconds, eventTiming, new object[] { value0, value1 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2>(this IUdonEventReceiver instance, string eventName, float delaySeconds, EventTiming eventTiming, T0 value0, T1 value1, T2 value2) => SendCustomEventArgsDelayedSeconds(instance, eventName, delaySeconds, eventTiming, new object[] { value0, value1, value2 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3>(this IUdonEventReceiver instance, string eventName, float delaySeconds, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3) => SendCustomEventArgsDelayedSeconds(instance, eventName, delaySeconds, eventTiming, new object[] { value0, value1, value2, value3 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3, T4>(this IUdonEventReceiver instance, string eventName, float delaySeconds, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4) => SendCustomEventArgsDelayedSeconds(instance, eventName, delaySeconds, eventTiming, new object[] { value0, value1, value2, value3, value4 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3, T4, T5>(this IUdonEventReceiver instance, string eventName, float delaySeconds, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5) => SendCustomEventArgsDelayedSeconds(instance, eventName, delaySeconds, eventTiming, new object[] { value0, value1, value2, value3, value4, value5 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3, T4, T5, T6>(this IUdonEventReceiver instance, string eventName, float delaySeconds, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6) => SendCustomEventArgsDelayedSeconds(instance, eventName, delaySeconds, eventTiming, new object[] { value0, value1, value2, value3, value4, value5, value6 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3, T4, T5, T6, T7>(this IUdonEventReceiver instance, string eventName, float delaySeconds, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7) => SendCustomEventArgsDelayedSeconds(instance, eventName, delaySeconds, eventTiming, new object[] { value0, value1, value2, value3, value4, value5, value6, value7 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this IUdonEventReceiver instance, string eventName, float delaySeconds, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8) => SendCustomEventArgsDelayedSeconds(instance, eventName, delaySeconds, eventTiming, new object[] { value0, value1, value2, value3, value4, value5, value6, value7, value8 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this IUdonEventReceiver instance, string eventName, float delaySeconds, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9) => SendCustomEventArgsDelayedSeconds(instance, eventName, delaySeconds, eventTiming, new object[] { value0, value1, value2, value3, value4, value5, value6, value7, value8, value9 });

    public static void SendCustomEventDelayedSeconds(this UdonSharpBehaviour instance, string eventName, float delaySeconds, params object[] args) => SendCustomEventArgsDelayedSeconds((IUdonEventReceiver)instance, eventName, delaySeconds, EventTiming.Update, args);
    public static void SendCustomEventDelayedSeconds<T>(this UdonSharpBehaviour instance, string eventName, float delaySeconds, T value) => SendCustomEventArgsDelayedSeconds((IUdonEventReceiver)instance, eventName, delaySeconds, EventTiming.Update, new object[] { value });
    public static void SendCustomEventDelayedSeconds<T0, T1>(this UdonSharpBehaviour instance, string eventName, float delaySeconds, T0 value0, T1 value1) => SendCustomEventArgsDelayedSeconds((IUdonEventReceiver)instance, eventName, delaySeconds, EventTiming.Update, new object[] { value0, value1 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2>(this UdonSharpBehaviour instance, string eventName, float delaySeconds, T0 value0, T1 value1, T2 value2) => SendCustomEventArgsDelayedSeconds((IUdonEventReceiver)instance, eventName, delaySeconds, EventTiming.Update, new object[] { value0, value1, value2 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3>(this UdonSharpBehaviour instance, string eventName, float delaySeconds, T0 value0, T1 value1, T2 value2, T3 value3) => SendCustomEventArgsDelayedSeconds((IUdonEventReceiver)instance, eventName, delaySeconds, EventTiming.Update, new object[] { value0, value1, value2, value3 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3, T4>(this UdonSharpBehaviour instance, string eventName, float delaySeconds, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4) => SendCustomEventArgsDelayedSeconds((IUdonEventReceiver)instance, eventName, delaySeconds, EventTiming.Update, new object[] { value0, value1, value2, value3, value4 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3, T4, T5>(this UdonSharpBehaviour instance, string eventName, float delaySeconds, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5) => SendCustomEventArgsDelayedSeconds((IUdonEventReceiver)instance, eventName, delaySeconds, EventTiming.Update, new object[] { value0, value1, value2, value3, value4, value5 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3, T4, T5, T6>(this UdonSharpBehaviour instance, string eventName, float delaySeconds, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6) => SendCustomEventArgsDelayedSeconds((IUdonEventReceiver)instance, eventName, delaySeconds, EventTiming.Update, new object[] { value0, value1, value2, value3, value4, value5, value6 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3, T4, T5, T6, T7>(this UdonSharpBehaviour instance, string eventName, float delaySeconds, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7) => SendCustomEventArgsDelayedSeconds((IUdonEventReceiver)instance, eventName, delaySeconds, EventTiming.Update, new object[] { value0, value1, value2, value3, value4, value5, value6, value7 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this UdonSharpBehaviour instance, string eventName, float delaySeconds, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8) => SendCustomEventArgsDelayedSeconds((IUdonEventReceiver)instance, eventName, delaySeconds, EventTiming.Update, new object[] { value0, value1, value2, value3, value4, value5, value6, value7, value8 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this UdonSharpBehaviour instance, string eventName, float delaySeconds, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9) => SendCustomEventArgsDelayedSeconds((IUdonEventReceiver)instance, eventName, delaySeconds, EventTiming.Update, new object[] { value0, value1, value2, value3, value4, value5, value6, value7, value8, value9 });

    public static void SendCustomEventDelayedSeconds(this UdonSharpBehaviour instance, string eventName, float delaySeconds, EventTiming eventTiming, params object[] args) => SendCustomEventArgsDelayedSeconds((IUdonEventReceiver)instance, eventName, delaySeconds, eventTiming, args);
    public static void SendCustomEventDelayedSeconds<T>(this UdonSharpBehaviour instance, string eventName, float delaySeconds, EventTiming eventTiming, T value) => SendCustomEventArgsDelayedSeconds((IUdonEventReceiver)instance, eventName, delaySeconds, eventTiming, new object[] { value });
    public static void SendCustomEventDelayedSeconds<T0, T1>(this UdonSharpBehaviour instance, string eventName, float delaySeconds, EventTiming eventTiming, T0 value0, T1 value1) => SendCustomEventArgsDelayedSeconds((IUdonEventReceiver)instance, eventName, delaySeconds, eventTiming, new object[] { value0, value1 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2>(this UdonSharpBehaviour instance, string eventName, float delaySeconds, EventTiming eventTiming, T0 value0, T1 value1, T2 value2) => SendCustomEventArgsDelayedSeconds((IUdonEventReceiver)instance, eventName, delaySeconds, eventTiming, new object[] { value0, value1, value2 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3>(this UdonSharpBehaviour instance, string eventName, float delaySeconds, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3) => SendCustomEventArgsDelayedSeconds((IUdonEventReceiver)instance, eventName, delaySeconds, eventTiming, new object[] { value0, value1, value2, value3 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3, T4>(this UdonSharpBehaviour instance, string eventName, float delaySeconds, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4) => SendCustomEventArgsDelayedSeconds((IUdonEventReceiver)instance, eventName, delaySeconds, eventTiming, new object[] { value0, value1, value2, value3, value4 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3, T4, T5>(this UdonSharpBehaviour instance, string eventName, float delaySeconds, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5) => SendCustomEventArgsDelayedSeconds((IUdonEventReceiver)instance, eventName, delaySeconds, eventTiming, new object[] { value0, value1, value2, value3, value4, value5 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3, T4, T5, T6>(this UdonSharpBehaviour instance, string eventName, float delaySeconds, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6) => SendCustomEventArgsDelayedSeconds((IUdonEventReceiver)instance, eventName, delaySeconds, eventTiming, new object[] { value0, value1, value2, value3, value4, value5, value6 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3, T4, T5, T6, T7>(this UdonSharpBehaviour instance, string eventName, float delaySeconds, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7) => SendCustomEventArgsDelayedSeconds((IUdonEventReceiver)instance, eventName, delaySeconds, eventTiming, new object[] { value0, value1, value2, value3, value4, value5, value6, value7 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this UdonSharpBehaviour instance, string eventName, float delaySeconds, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8) => SendCustomEventArgsDelayedSeconds((IUdonEventReceiver)instance, eventName, delaySeconds, eventTiming, new object[] { value0, value1, value2, value3, value4, value5, value6, value7, value8 });
    public static void SendCustomEventDelayedSeconds<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this UdonSharpBehaviour instance, string eventName, float delaySeconds, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9) => SendCustomEventArgsDelayedSeconds((IUdonEventReceiver)instance, eventName, delaySeconds, eventTiming, new object[] { value0, value1, value2, value3, value4, value5, value6, value7, value8, value9 });

    public static void SendCustomEventDelayedFrames(this IUdonEventReceiver instance, string eventName, int delayFrames, params object[] args) => SendCustomEventArgsDelayedFrames(instance, eventName, delayFrames, EventTiming.Update, args);
    public static void SendCustomEventDelayedFrames<T>(this IUdonEventReceiver instance, string eventName, int delayFrames, T value) => SendCustomEventArgsDelayedFrames(instance, eventName, delayFrames, EventTiming.Update, new object[] { value });
    public static void SendCustomEventDelayedFrames<T0, T1>(this IUdonEventReceiver instance, string eventName, int delayFrames, T0 value0, T1 value1) => SendCustomEventArgsDelayedFrames(instance, eventName, delayFrames, EventTiming.Update, new object[] { value0, value1 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2>(this IUdonEventReceiver instance, string eventName, int delayFrames, T0 value0, T1 value1, T2 value2) => SendCustomEventArgsDelayedFrames(instance, eventName, delayFrames, EventTiming.Update, new object[] { value0, value1, value2 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3>(this IUdonEventReceiver instance, string eventName, int delayFrames, T0 value0, T1 value1, T2 value2, T3 value3) => SendCustomEventArgsDelayedFrames(instance, eventName, delayFrames, EventTiming.Update, new object[] { value0, value1, value2, value3 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3, T4>(this IUdonEventReceiver instance, string eventName, int delayFrames, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4) => SendCustomEventArgsDelayedFrames(instance, eventName, delayFrames, EventTiming.Update, new object[] { value0, value1, value2, value3, value4 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3, T4, T5>(this IUdonEventReceiver instance, string eventName, int delayFrames, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5) => SendCustomEventArgsDelayedFrames(instance, eventName, delayFrames, EventTiming.Update, new object[] { value0, value1, value2, value3, value4, value5 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3, T4, T5, T6>(this IUdonEventReceiver instance, string eventName, int delayFrames, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6) => SendCustomEventArgsDelayedFrames(instance, eventName, delayFrames, EventTiming.Update, new object[] { value0, value1, value2, value3, value4, value5, value6 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3, T4, T5, T6, T7>(this IUdonEventReceiver instance, string eventName, int delayFrames, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7) => SendCustomEventArgsDelayedFrames(instance, eventName, delayFrames, EventTiming.Update, new object[] { value0, value1, value2, value3, value4, value5, value6, value7 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this IUdonEventReceiver instance, string eventName, int delayFrames, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8) => SendCustomEventArgsDelayedFrames(instance, eventName, delayFrames, EventTiming.Update, new object[] { value0, value1, value2, value3, value4, value5, value6, value7, value8 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this IUdonEventReceiver instance, string eventName, int delayFrames, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9) => SendCustomEventArgsDelayedFrames(instance, eventName, delayFrames, EventTiming.Update, new object[] { value0, value1, value2, value3, value4, value5, value6, value7, value8, value9 });

    public static void SendCustomEventDelayedFrames(this IUdonEventReceiver instance, string eventName, int delayFrames, EventTiming eventTiming, params object[] args) => SendCustomEventArgsDelayedFrames(instance, eventName, delayFrames, eventTiming, args);
    public static void SendCustomEventDelayedFrames<T>(this IUdonEventReceiver instance, string eventName, int delayFrames, EventTiming eventTiming, T value) => SendCustomEventArgsDelayedFrames(instance, eventName, delayFrames, eventTiming, new object[] { value });
    public static void SendCustomEventDelayedFrames<T0, T1>(this IUdonEventReceiver instance, string eventName, int delayFrames, EventTiming eventTiming, T0 value0, T1 value1) => SendCustomEventArgsDelayedFrames(instance, eventName, delayFrames, eventTiming, new object[] { value0, value1 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2>(this IUdonEventReceiver instance, string eventName, int delayFrames, EventTiming eventTiming, T0 value0, T1 value1, T2 value2) => SendCustomEventArgsDelayedFrames(instance, eventName, delayFrames, eventTiming, new object[] { value0, value1, value2 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3>(this IUdonEventReceiver instance, string eventName, int delayFrames, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3) => SendCustomEventArgsDelayedFrames(instance, eventName, delayFrames, eventTiming, new object[] { value0, value1, value2, value3 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3, T4>(this IUdonEventReceiver instance, string eventName, int delayFrames, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4) => SendCustomEventArgsDelayedFrames(instance, eventName, delayFrames, eventTiming, new object[] { value0, value1, value2, value3, value4 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3, T4, T5>(this IUdonEventReceiver instance, string eventName, int delayFrames, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5) => SendCustomEventArgsDelayedFrames(instance, eventName, delayFrames, eventTiming, new object[] { value0, value1, value2, value3, value4, value5 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3, T4, T5, T6>(this IUdonEventReceiver instance, string eventName, int delayFrames, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6) => SendCustomEventArgsDelayedFrames(instance, eventName, delayFrames, eventTiming, new object[] { value0, value1, value2, value3, value4, value5, value6 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3, T4, T5, T6, T7>(this IUdonEventReceiver instance, string eventName, int delayFrames, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7) => SendCustomEventArgsDelayedFrames(instance, eventName, delayFrames, eventTiming, new object[] { value0, value1, value2, value3, value4, value5, value6, value7 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this IUdonEventReceiver instance, string eventName, int delayFrames, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8) => SendCustomEventArgsDelayedFrames(instance, eventName, delayFrames, eventTiming, new object[] { value0, value1, value2, value3, value4, value5, value6, value7, value8 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this IUdonEventReceiver instance, string eventName, int delayFrames, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9) => SendCustomEventArgsDelayedFrames(instance, eventName, delayFrames, eventTiming, new object[] { value0, value1, value2, value3, value4, value5, value6, value7, value8, value9 });
    
    public static void SendCustomEventDelayedFrames(this UdonSharpBehaviour instance, string eventName, int delayFrames, params object[] args) => SendCustomEventArgsDelayedFrames((IUdonEventReceiver)instance, eventName, delayFrames, EventTiming.Update, args);
    public static void SendCustomEventDelayedFrames<T>(this UdonSharpBehaviour instance, string eventName, int delayFrames, T value) => SendCustomEventArgsDelayedFrames((IUdonEventReceiver)instance, eventName, delayFrames, EventTiming.Update, new object[] { value });
    public static void SendCustomEventDelayedFrames<T0, T1>(this UdonSharpBehaviour instance, string eventName, int delayFrames, T0 value0, T1 value1) => SendCustomEventArgsDelayedFrames((IUdonEventReceiver)instance, eventName, delayFrames, EventTiming.Update, new object[] { value0, value1 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2>(this UdonSharpBehaviour instance, string eventName, int delayFrames, T0 value0, T1 value1, T2 value2) => SendCustomEventArgsDelayedFrames((IUdonEventReceiver)instance, eventName, delayFrames, EventTiming.Update, new object[] { value0, value1, value2 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3>(this UdonSharpBehaviour instance, string eventName, int delayFrames, T0 value0, T1 value1, T2 value2, T3 value3) => SendCustomEventArgsDelayedFrames((IUdonEventReceiver)instance, eventName, delayFrames, EventTiming.Update, new object[] { value0, value1, value2, value3 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3, T4>(this UdonSharpBehaviour instance, string eventName, int delayFrames, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4) => SendCustomEventArgsDelayedFrames((IUdonEventReceiver)instance, eventName, delayFrames, EventTiming.Update, new object[] { value0, value1, value2, value3, value4 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3, T4, T5>(this UdonSharpBehaviour instance, string eventName, int delayFrames, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5) => SendCustomEventArgsDelayedFrames((IUdonEventReceiver)instance, eventName, delayFrames, EventTiming.Update, new object[] { value0, value1, value2, value3, value4, value5 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3, T4, T5, T6>(this UdonSharpBehaviour instance, string eventName, int delayFrames, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6) => SendCustomEventArgsDelayedFrames((IUdonEventReceiver)instance, eventName, delayFrames, EventTiming.Update, new object[] { value0, value1, value2, value3, value4, value5, value6 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3, T4, T5, T6, T7>(this UdonSharpBehaviour instance, string eventName, int delayFrames, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7) => SendCustomEventArgsDelayedFrames((IUdonEventReceiver)instance, eventName, delayFrames, EventTiming.Update, new object[] { value0, value1, value2, value3, value4, value5, value6, value7 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this UdonSharpBehaviour instance, string eventName, int delayFrames, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8) => SendCustomEventArgsDelayedFrames((IUdonEventReceiver)instance, eventName, delayFrames, EventTiming.Update, new object[] { value0, value1, value2, value3, value4, value5, value6, value7, value8 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this UdonSharpBehaviour instance, string eventName, int delayFrames, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9) => SendCustomEventArgsDelayedFrames((IUdonEventReceiver)instance, eventName, delayFrames, EventTiming.Update, new object[] { value0, value1, value2, value3, value4, value5, value6, value7, value8, value9 });

    public static void SendCustomEventDelayedFrames(this UdonSharpBehaviour instance, string eventName, int delayFrames, EventTiming eventTiming, params object[] args) => SendCustomEventArgsDelayedFrames((IUdonEventReceiver)instance, eventName, delayFrames, eventTiming, args);
    public static void SendCustomEventDelayedFrames<T>(this UdonSharpBehaviour instance, string eventName, int delayFrames, EventTiming eventTiming, T value) => SendCustomEventArgsDelayedFrames((IUdonEventReceiver)instance, eventName, delayFrames, eventTiming, new object[] { value });
    public static void SendCustomEventDelayedFrames<T0, T1>(this UdonSharpBehaviour instance, string eventName, int delayFrames, EventTiming eventTiming, T0 value0, T1 value1) => SendCustomEventArgsDelayedFrames((IUdonEventReceiver)instance, eventName, delayFrames, eventTiming, new object[] { value0, value1 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2>(this UdonSharpBehaviour instance, string eventName, int delayFrames, EventTiming eventTiming, T0 value0, T1 value1, T2 value2) => SendCustomEventArgsDelayedFrames((IUdonEventReceiver)instance, eventName, delayFrames, eventTiming, new object[] { value0, value1, value2 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3>(this UdonSharpBehaviour instance, string eventName, int delayFrames, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3) => SendCustomEventArgsDelayedFrames((IUdonEventReceiver)instance, eventName, delayFrames, eventTiming, new object[] { value0, value1, value2, value3 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3, T4>(this UdonSharpBehaviour instance, string eventName, int delayFrames, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4) => SendCustomEventArgsDelayedFrames((IUdonEventReceiver)instance, eventName, delayFrames, eventTiming, new object[] { value0, value1, value2, value3, value4 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3, T4, T5>(this UdonSharpBehaviour instance, string eventName, int delayFrames, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5) => SendCustomEventArgsDelayedFrames((IUdonEventReceiver)instance, eventName, delayFrames, eventTiming, new object[] { value0, value1, value2, value3, value4, value5 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3, T4, T5, T6>(this UdonSharpBehaviour instance, string eventName, int delayFrames, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6) => SendCustomEventArgsDelayedFrames((IUdonEventReceiver)instance, eventName, delayFrames, eventTiming, new object[] { value0, value1, value2, value3, value4, value5, value6 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3, T4, T5, T6, T7>(this UdonSharpBehaviour instance, string eventName, int delayFrames, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7) => SendCustomEventArgsDelayedFrames((IUdonEventReceiver)instance, eventName, delayFrames, eventTiming, new object[] { value0, value1, value2, value3, value4, value5, value6, value7 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this UdonSharpBehaviour instance, string eventName, int delayFrames, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8) => SendCustomEventArgsDelayedFrames((IUdonEventReceiver)instance, eventName, delayFrames, eventTiming, new object[] { value0, value1, value2, value3, value4, value5, value6, value7, value8 });
    public static void SendCustomEventDelayedFrames<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this UdonSharpBehaviour instance, string eventName, int delayFrames, EventTiming eventTiming, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9) => SendCustomEventArgsDelayedFrames((IUdonEventReceiver)instance, eventName, delayFrames, eventTiming, new object[] { value0, value1, value2, value3, value4, value5, value6, value7, value8, value9 });
    
    public static void SendCustomEventArgsDelayedSeconds(this UdonSharpBehaviour instance, string eventName, float delaySeconds, object[] eventArgs) => SendCustomEventArgsDelayedSeconds((IUdonEventReceiver)instance, eventName, delaySeconds, EventTiming.Update, eventArgs);
    public static void SendCustomEventArgsDelayedSeconds(this UdonSharpBehaviour instance, string eventName, float delaySeconds, EventTiming eventTiming, object[] eventArgs) => SendCustomEventArgsDelayedSeconds((IUdonEventReceiver)instance, eventName, delaySeconds, eventTiming, eventArgs);

    public static void SendCustomEventArgsDelayedSeconds(this IUdonEventReceiver instance, string eventName, float delaySeconds, object[] eventArgs) => SendCustomEventArgsDelayedSeconds(instance, eventName, delaySeconds, EventTiming.Update, eventArgs);
    public static void SendCustomEventArgsDelayedSeconds(this IUdonEventReceiver instance, string eventName, float delaySeconds, EventTiming eventTiming, object[] eventArgs)
    {
        // Fall back to default custom event behaviour when scheduler is unavailable
        var eventScheduler = instance.GetEventScheduler();
        if(eventScheduler == null)
        {
            instance.SendCustomEventDelayedSeconds(eventName, delaySeconds, eventTiming);
            return;
        }
#if UDONCUSTOMEVENTARGS_DELAYEVENTS_LEGACY
        ScheduleTimeEvent(instance, eventScheduler, eventName, delaySeconds, eventTiming, eventArgs);
#else
        eventScheduler.ScheduleTimeEvent(eventName, delaySeconds, eventTiming, eventArgs);
#endif
    }

    public static void SendCustomEventArgsDelayedFrames(this UdonSharpBehaviour instance, string eventName, int delayFrames, object[] eventArgs) => SendCustomEventArgsDelayedFrames((IUdonEventReceiver)instance, eventName, delayFrames, EventTiming.Update, eventArgs);
    public static void SendCustomEventArgsDelayedFrames(this UdonSharpBehaviour instance, string eventName, int delayFrames, EventTiming eventTiming, object[] eventArgs) => SendCustomEventArgsDelayedFrames((IUdonEventReceiver)instance, eventName, delayFrames, eventTiming, eventArgs);

    public static void SendCustomEventArgsDelayedFrames(this IUdonEventReceiver instance, string eventName, int delayFrames, object[] eventArgs) => SendCustomEventArgsDelayedFrames(instance, eventName, delayFrames, EventTiming.Update, eventArgs);
    public static void SendCustomEventArgsDelayedFrames(this IUdonEventReceiver instance, string eventName, int delayFrames, EventTiming eventTiming, object[] eventArgs)
    {
        // Fall back to default custom event behaviour when scheduler is unavailable
        var eventScheduler = instance.GetEventScheduler();
        if(eventScheduler == null)
        {
            instance.SendCustomEventDelayedFrames(eventName, delayFrames, eventTiming);
            return;
        }
#if UDONCUSTOMEVENTARGS_DELAYEVENTS_LEGACY
        ScheduleFrameEvent(instance, eventScheduler, eventName, delayFrames, eventTiming, eventArgs);
#else
        eventScheduler.ScheduleFrameEvent(eventName, delayFrames, eventTiming, eventArgs);
#endif
    }
#if UDONCUSTOMEVENTARGS_DELAYEVENTS_LEGACY
    private static object[] GetEventScheduler(this IUdonEventReceiver instance)
    {
        if(instance.GetProgramVariableType("__CustomEventScheduler") == null) return null;

        var eventScheduler = (object[])instance.GetProgramVariable("__CustomEventScheduler");
        if(eventScheduler == null)
        {
            var TimeEventQueues = new object[]
            {
                new DataList(),
                new DataList()
            };

            var FrameEventQueues = new object[]
            {
                new DataList(),
                new DataList()
            };

            eventScheduler = new object[] // class UdonCustomEventScheduler
            {
                TimeEventQueues, // List<UdonCustomEventSchedulerTimeEvent>[] TimeEventQueues;
                FrameEventQueues, // List<UdonCustomEventSchedulerFrameEvent>[] FrameEventQueues;
                0u // ulong NextEventID;
            };

            instance.SetProgramVariable("__CustomEventScheduler", (object)eventScheduler);
        }

        return eventScheduler;
    }

    private static void ScheduleTimeEvent(IUdonEventReceiver instance, object[] eventScheduler, string eventName, float delaySeconds, EventTiming eventTiming, object[] eventArgs)
    {
        var NextEventID = (ulong)eventScheduler[2]; // NextEventID
        var eventQueue = (DataList)((object[])eventScheduler[0])[(int)eventTiming]; // TimeEventQueues[(int)eventTiming];

        var queuedEvent = new object[] // class UdonCustomEventSchedulerTimeEvent
        {
            eventName, // string EventName;
            eventArgs, // object[] EventArgs;
            Time.time + Math.Max(delaySeconds, 0.001f), // public float EventTime;
            NextEventID // ulong EventID;
        };

        eventScheduler[2] = ++NextEventID;

        AddTimeEvent(eventQueue, queuedEvent);
        instance.SendCustomEventDelayedSeconds($"__UdonCustomEventScheduler_ScheduleTime{eventTiming}", delaySeconds, eventTiming);
    }

    private static void ScheduleFrameEvent(IUdonEventReceiver instance, object[] eventScheduler, string eventName, int delayFrames, EventTiming eventTiming, object[] eventArgs)
    {
        var NextEventID = (ulong)eventScheduler[2];
        var eventQueue = (DataList)((object[])eventScheduler[1])[(int)eventTiming]; // FrameEventQueues[(int)eventTiming];

        var queuedEvent = new object[] // class UdonCustomEventSchedulerFrameEvent
        {
            eventName, // string EventName;
            eventArgs, // object[] EventArgs;
            Time.frameCount + Math.Max(delayFrames, 1), // int EventFrame;
            NextEventID // ulong EventID;
        };

        eventScheduler[2] = ++NextEventID;

        AddFrameEvent(eventQueue, queuedEvent);
        instance.SendCustomEventDelayedFrames($"__UdonCustomEventScheduler_ScheduleFrame{eventTiming}", delayFrames, eventTiming);
    }

    private static void AddTimeEvent(DataList eventQueue, object[] item)
    {
        if(eventQueue.Count == 0)
        {
            eventQueue.Add(new DataToken(item));
            return;
        }

        if(CompareTimeEvent((object[])eventQueue[eventQueue.Count - 1].Reference, item) <= 0)
        {
            eventQueue.Add(new DataToken(item));
            return;
        }

        if(CompareTimeEvent((object[])eventQueue[0].Reference, item) >= 0)
        {
            eventQueue.Insert(0, new DataToken(item));
            return;
        }

        int index = BinarySearchTimeEvent(eventQueue, item);
        if(index < 0) index = ~index;
        eventQueue.Insert(index, new DataToken(item));
    }

    private static void AddFrameEvent(DataList eventQueue, object[] item)
    {
        if(eventQueue.Count == 0)
        {
            eventQueue.Add(new DataToken(item));
            return;
        }

        if(CompareFrameEvent((object[])eventQueue[eventQueue.Count - 1].Reference, item) <= 0)
        {
            eventQueue.Add(new DataToken(item));
            return;
        }

        if(CompareFrameEvent((object[])eventQueue[0].Reference, item) >= 0)
        {
            eventQueue.Insert(0, new DataToken(item));
            return;
        }

        int index = BinarySearchFrameEvent(eventQueue, item);
        if(index < 0) index = ~index;
        eventQueue.Insert(index, new DataToken(item));
    }

    private static int BinarySearchTimeEvent(DataList array, object[] target)
    {
        int low = 0;
        int high = array.Count - 1;

        while(low <= high)
        {
            int mid = low + ((high - low) >> 1); // Avoid overflow
            var midValue = (object[])array[mid].Reference; // array[mid]
            int comparison = CompareTimeEvent(midValue, target); // midValue.CompareTo(target);

            if(comparison == 0) return mid; // Target found
            else if (comparison < 0) low = mid + 1; // Search in the right half
            else high = mid - 1; // Search in the left half
        }

        return ~low; // Target not found, return bitwise complement of the insertion point
    }

    private static int BinarySearchFrameEvent(DataList array, object[] target)
    {
        int low = 0;
        int high = array.Count - 1;

        while(low <= high)
        {
            int mid = low + ((high - low) >> 1); // Avoid overflow
            var midValue = (object[])array[mid].Reference; // array[mid]
            int comparison = CompareFrameEvent(midValue, target); // midValue.CompareTo(target);

            if(comparison == 0) return mid; // Target found
            else if (comparison < 0) low = mid + 1; // Search in the right half
            else high = mid - 1; // Search in the left half
        }

        return ~low; // Target not found, return bitwise complement of the insertion point
    }

    private static int CompareTimeEvent(object[] a, object[] b)
    {
        var num = ((float)a[2]).CompareTo((float)b[2]); // EventTime.CompareTo(b.EventTime);
        if(num != 0) return num;
        return ((ulong)a[3]).CompareTo((ulong)b[3]); // EventID.CompareTo(b.EventID);
    }

    private static int CompareFrameEvent(object[] a, object[] b)
    {
        var num = ((int)a[2]).CompareTo((int)b[2]); // EventFrame.CompareTo(b.EventFrame);
        if(num != 0) return num;
        return ((ulong)a[3]).CompareTo((ulong)b[3]); // EventID.CompareTo(b.EventID);
    }
#else
    private static UdonCustomEventScheduler GetEventScheduler(this IUdonEventReceiver instance)
    {
        if(instance.GetProgramVariableType("__CustomEventScheduler") == null) return null;

        var eventScheduler = (UdonCustomEventScheduler)instance.GetProgramVariable("__CustomEventScheduler");
        if(eventScheduler == null)
        {
            eventScheduler = new UdonCustomEventScheduler(instance);
            instance.SetProgramVariable("__CustomEventScheduler", (object)eventScheduler);
        }

        return eventScheduler;
    }
#endif
#endregion
}
