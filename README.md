# Udon Custom Event Args
## Description
This extension adds support for calling `SendCustomEvent` on methods which have arguments and supports overloaded methods.

## Why use this?
 - Supports calling custom methods on third party behaviours without modifying their source code.
   - No inheriting from custom base classes, adding custom attributes to methods, etc.
 - Can be used to create flexible event handlers that use existing methods of different behaviours where creating standardized methods and variables to pass data around would be infeasible.

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
 - This extension only supports passing arguments to methods using `SendCustomEvent`.  Other variants such as `SendCustomNetworkEvent`, `SendCustomEventDelayedSeconds` and `SendCustomEventDelayedFrames` are not supported.
 - Calling methods marked with the `RecursiveMethod` attribute may not work correctly.
