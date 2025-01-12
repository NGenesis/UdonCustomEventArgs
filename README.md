# Udon Custom Event Args
## Description
This package extends support in U# for executing custom methods using `SendCustomEvent` and its variants with support for arguments, return values and overloaded methods.

## Why use this?
 - Adds support for calling custom events with arguments, return values and overloaded methods.
 - Supports calling custom methods on third party behaviours without modifying their source code.
   - No inheriting from custom base classes, adding custom attributes to methods, etc.
 - Can be used to create flexible event handlers that use existing methods of different behaviours where creating standardized methods and variables to pass data around would be infeasible.

## Supported APIs
| Name | Arguments | Return Value | Method Overloads | API |
| --- | --- | --- | --- | --- |
| `SendCustomEvent` | :heavy_check_mark: | :x: | :heavy_check_mark: | `void SendCustomEvent<T0, ..., TN>(string eventName, T0 arg0, ..., TN argN)` |
| `SendCustomEventArgs` | :heavy_check_mark: | :x: | :heavy_check_mark: | `void SendCustomEventArgs(string eventName, object[] args)` |
| `SendCustomEventDelayedSeconds` | :heavy_check_mark: | :x: | :heavy_check_mark: | `void SendCustomEventDelayedSeconds<T0, ..., TN>(string eventName, float delaySeconds, EventTiming eventTiming, T arg0, ..., T argN)` |
| `SendCustomEventDelayedSecondsArgs` | :heavy_check_mark: | :x: | :heavy_check_mark: | `void SendCustomEventDelayedSecondsArgs(string eventName, float delaySeconds, EventTiming eventTiming, object[] args)` |
| `SendCustomEventDelayedFrames` | :heavy_check_mark: | :x: | :heavy_check_mark: | `void SendCustomEventDelayedFrames<T0, ..., TN>(string eventName, int delayFrames, EventTiming eventTiming, T arg0, ..., T argN)` |
| `SendCustomEventDelayedFramesArgs` | :heavy_check_mark: | :x: | :heavy_check_mark: | `void SendCustomEventDelayedFramesArgs(string eventName, int delayFrames, EventTiming eventTiming, object[] args)` |
| `TryExecuteCustomEvent` | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | `bool TryExecuteCustomEvent<TResult, T0, ..., TN>(string eventName, out TResult returnValue, T arg0, ..., T argN)` |
| `TryExecuteCustomEventArgs` | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | `bool TryExecuteCustomEventArgs<TResult>(string eventName, out TResult returnValue, object[] args)` |

## Installation & Usage
[![VPM Package Version](https://img.shields.io/vpm/v/com.genesis.udoncustomeventargs?repository_url=https%3A%2F%2Fngenesis.github.io%2FUdonCustomEventArgs%2Findex.json)](https://ngenesis.github.io/UdonCustomEventArgs)

1. Install the [VCC package](https://ngenesis.github.io/UdonCustomEventArgs/index.json) through [VRChat Creator Companion](https://vcc.docs.vrchat.com/guides/community-repositories/#how-to-add-a-community-repository), no additional setup is required.
2. In your code, call one of the following with your method/event name followed by the arguments that the method/event requires:
```csharp
// Generic method
this.SendCustomEvent("eventname", arg0, ..., argN);

// Non-generic method
this.SendCustomEventArgs("eventname", new object[] { arg0, ..., argN });
```

## Examples
```csharp
using UdonSharp;
using UnityEngine;
using VRC.Udon;

// Some behaviour in a third party package that you wouldn't be able to modify the source code of
public class SomeThirdPartyBehaviour : UdonSharpBehaviour
{
    public void DoThing(int intValue)
    {
        Debug.Log($"DoThing_A {intValue}");
    }
}

public class MyInteractEventHandler
{
    public UdonSharpBehaviour EventTarget;
    public string EventName;
    public object[] EventArgs;
}

public class MyPlayerTriggerEventHandler
{
    public UdonSharpBehaviour EventTarget;
    public string EventName;
}

// A behaviour you created
public class MyBehaviour : UdonSharpBehaviour
{
    public SomeThirdPartyBehaviour AnotherBehaviour;
    public List<MyInteractEventHandler> InteractEventHandlers = new List<MyInteractEventHandler>();
    public List<MyPlayerTriggerEventHandler> PlayerTriggerEventHandlers = new List<MyPlayerTriggerEventHandler>();

    void Start()
    {
        // Calling a method on the current behaviour
        this.SendCustomEvent(nameof(Test), 1.0f, 2, "hello", true, this); // Displays "Test_A 1.0 2 hello true ThisBehaviour"
        this.SendCustomEvent(nameof(Test), 3.0f, "world", 4, false, AnotherBehaviour); // Displays "Test_B 3.0 world 4 false AnotherBehaviour"
        this.SendCustomEvent(nameof(Test), 5); // Displays "Test_C 5"
        this.SendCustomEvent(nameof(Test)); // Displays "Test_D"

        // Calling a method from another instance of a behaviour
        AnotherBehaviour.SendCustomEvent(nameof(SomeThirdPartyBehaviour.DoThing), 6); // Displays "DoThing_A 6"

        // An alternative form of calling a method which can accept an object array containing each method argument
        this.SendCustomEventArgs(nameof(Test), new object[] { 3.0f, "world", 4, false, AnotherBehaviour }); // Displays "Test_B 3.0 world 4 false AnotherBehaviour"

        // Calling methods with arguments which cannot be bound to a specific overload will fall back to the default behaviour of calling the event without arguments
        this.SendCustomEvent(nameof(Test), "these", "args", "are", "ignored"); // Displays "Test_D"

        // Store some event handlers to call later when Interact is triggered
        AddInteractEventHandler(this, nameof(Test), new object[] { 123.0f, "interact", 6, false, AnotherBehaviour });
        AddInteractEventHandler(AnotherBehaviour, nameof(SomeThirdPartyBehaviour.DoThing), new object[] { 42 });

        // Store some event handlers to call later when OnPlayerTriggerEnter is triggered
        AddPlayerTriggerEventHandler(this, nameof(Test));
        AddPlayerTriggerEventHandler(AnotherBehaviour, nameof(SomeThirdPartyBehaviour.DoThing));

        // Calling methods and getting their return value
        if(this.TryExecuteCustomEvent(nameof(GetSum), out int sum, 2, 3)) Debug.Log($"2 + 3 = {sum}"); // Displays "2 + 3 = 5"

        // Calling methods with a delay
        this.SendCustomEventDelayedSeconds(nameof(DelayedTest), 5f, EventTiming.Update, "This message appears later!"); // Displays "This message appears later!" after 5 seconds
        this.SendCustomEventDelayedFrames(nameof(DelayedTest), 1, EventTiming.LateUpdate, "This message appears on the next frame!"); // Displays "This message appears on the next frame!" in the next frame
    }

    public override void Interact()
    {
        // Displays "Test_B 123.0 interact 6 false AnotherBehaviour" and "DoThing_A 42"
        foreach(var handler in InteractEventHandlers) handler.EventTarget.SendCustomEventArgs(handler.EventName, handler.EventArgs);
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        // Displays "Test_C XXXX" and "DoThing_A XXXX" where XXXX is the playerId that was passed in
        foreach(var handler in PlayerTriggerEventHandlers) handler.EventTarget.SendCustomEvent(handler.EventName, player.playerId);
    }

    public void Test(float floatValue, int intValue, string stringValue, bool boolValue, UdonSharpBehaviour behaviourValue)
    {
        Debug.Log($"Test_A {floatValue} {intValue} {stringValue} {boolValue} {behaviourValue.name}");
    }

    public void Test(float floatValue, string stringValue, int intValue, bool boolValue, UdonBehaviour behaviourValue)
    {
        Debug.Log($"Test_B {floatValue} {stringValue} {intValue} {boolValue} {behaviourValue.name}");
    }

    public void Test(int intValue)
    {
        Debug.Log($"Test_C {intValue}");
    }

    public void Test()
    {
        Debug.Log($"Test_D");
    }

    public int GetSum(int a, int b)
    {
        return a + b;
    }

    public void DelayedTest(string message)
    {
        Debug.Log(message);
    }

    public void AddInteractEventHandler(UdonSharpBehaviour target, string eventName, object[] eventArgs)
    {
        var handler = new MyInteractEventHandler();
        handler.EventTarget = target;
        handler.EventName = eventName;
        handler.EventArgs = eventArgs;
        InteractEventHandlers.Add(handler);
    }

    public void AddPlayerTriggerEventHandler(UdonSharpBehaviour target, string eventName)
    {
        var handler = new MyPlayerTriggerEventHandler();
        handler.EventTarget = target;
        handler.EventName = eventName;
        PlayerTriggerEventHandlers.Add(handler);
    }
}
```
Note: `List` and non-UdonSharpBehaviour classes are supported in [U# 1.2 beta](https://github.com/MerlinVR/UdonSharp/releases) or higher.

## Notes & Caveats
 - Calling methods with arguments which cannot be bound to a specific overload will fall back to the default behaviour of calling the event without arguments.
 - Calling methods with arguments using `SendCustomNetworkEvent` is not currently supported.
 - Calling methods marked with the `RecursiveMethod` attribute may not work correctly.
