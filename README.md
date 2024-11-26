# Udon Custom Event Args
## Description
This extension adds support for calling `SendCustomEvent` on methods which have arguments and supports overloaded methods.

## Installation & Usage
1. Install the package through your preferred package manager, no additional setup is required.
2. In your code, call one of the following with your method/event name followed by the arguments that the method/event requires:
```csharp
// Generic method
this.SendCustomEvent("eventname", argT0, ..., argTN);

// Non-generic method
this.SendCustomEventArgs("eventname", new object[] { arg0, ..., argN });
```

## Examples
```csharp
using UdonSharp;
using UnityEngine;
using VRC.Udon;

public class MyBehaviour : UdonSharpBehaviour
{
    public MyBehaviour AnotherBehaviour;

    void Start()
    {
        // Calling a method on the current behaviour
        this.SendCustomEvent("Test", 1.0f, 2, "hello", true, this); // Displays "Test_A 1.0 2 hello true ThisBehaviour"
        this.SendCustomEvent("Test", 3.0f, "world", 4, false, AnotherBehaviour); // Displays "Test_B 3.0 world 4 false AnotherBehaviour"
        this.SendCustomEvent("Test", 5); // Displays "Test_C 5"
        this.SendCustomEvent("Test"); // Displays "Test_D"

        // Calling a method from another instance of a behaviour
        AnotherBehaviour.SendCustomEvent(nameof(DoThing), 6); // Displays "DoThing_A 6"

        // An alternative form of calling a method which can accept an object array containing each method argument
        this.SendCustomEventArgs(nameof(Test), new object[] { 3.0f, "world", 4, false, AnotherBehaviour }); // Displays "Test_B 3.0 world 4 false AnotherBehaviour"

        // Calling methods with arguments which cannot be bound to a specific overload will fall back to the default behaviour of calling the event without arguments
        this.SendCustomEvent(nameof(Test), "these", "args", "are", "ignored"); // Displays "Test_D"
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

    public void DoThing(int intValue)
    {
        Debug.Log($"DoThing_A {intValue}");
    }
}
```

## Notes & Caveats
 - Calling methods with arguments which cannot be bound to a specific overload will fall back to the default behaviour of calling the event without arguments.
 - This extension only supports passing arguments to methods using `SendCustomEvent`.  Other variants such as `SendCustomNetworkEvent`, `SendCustomEventDelayedSeconds` and `SendCustomEventDelayedFrames` are not supported.
 - Calling methods marked with the `RecursiveMethod` attribute may not work correctly.