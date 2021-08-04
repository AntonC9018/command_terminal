# This is a fork!

Some / most of the functionality described below has been cut / modified / augmented / improved.
The project is coupled with the code generator Kari, [see the main project](https://github.com/AntonC9018/a-particular-project).

`Kari` will eventually become stand-alone, faster, if people want to use this fork.

# Unity Command Terminal

A simple and highly performant in-game drop down Console.

![gif](./demo.gif)

Command Terminal is based on [an implementation by Jonathan Blow](https://youtu.be/N2UdveBwWY4) done in the Jai programming language.


## Setup

**If your project is monolithic (does not make use of asmdef's):**

1. Just clone the repository somewhere in your `Assets` folder. By default, it would try linking to a `Common` asmdef project, specified by `"references": [ "Common" ]` in the asmdef file. If you don't want to have a separate `Common` project, just remove that reference.

2. Run `Kari` with plugins `Terminal` and `Flags`, with `-monolithicProject true`. The other default settings should do fine in your case.


**If your project is modular:**

1. Clone or better add `CommandTerminal` as a submodule to your `Assets` folder. I recommend splitting scripts from other assets by putting them in an `Assets/Source` folder instead.

2. Make sure you have a `Common` project, where you would put generic code, like `Kari`'s flag attributes.

3. Run `Kari` with plugins `Terminal` and `Flags`, setting `-monolithicProject false`, and other settings, see `Kari`'s readme.


**After that:**

1. In your runner script, call `CommandsInitialization.InitializeBuiltinCommands();`. See [this](https://github.com/AntonC9018/a-particular-project/blob/a7e55f9484a22f0940c0818f8cd81704263903c9/Game/Assets/Source/Main.cs), [this](https://docs.unity3d.com/ScriptReference/RuntimeInitializeOnLoadMethodAttribute-ctor.html).

2. Add a `Terminal` component to a game object. Configure the values as you see fit.

## Usage

The console window can be toggled with a hotkey (default is backtick), and another hotkey can be used to toggle the full size window (default is shift+backtick).

Enter `help` in the console to view all available commands, use the up and down arrow keys to traverse the command history, and the tab key to autocomplete commands. Typing in `command_name -help` would also give the help info for `command_name`.

## Registering Commands

There are 3 options to register commands to be used in the Command Terminal. 2 of which are coupled with `Kari`.

### 1. Using the Command attribute:

This is the simplest method. The commands will be automatically picked up by `Kari` and be available among the default commands in the terminal.

This method is prefered over the next one if you don't need to do meta stuff, that is, interacting with the terminal logs or see other shell commands, or look at the raw arguments, etc.

Commands registered this way are transformed into command classes by `Kari`, which are then instantiated by your runner script and used by the terminal shell.

The command method must be static and public, in any non-nested class.

```csharp
// The command gets the name `Add`.
// The usage will be `add 1 2`, which will print 3.
// Typing in `add` will print the help message, with parameter types.
// Typing in `add 1` or `add 1 2 3` will generate an error message.
[Command(Help = "Adds 2 numbers")]
public static int Add(int a, int b) 
{
    int result = a + b;
    return result;
}

// The command gets the name `OtherName`.
// The usage will be `othername 1 2`, which will print -1.
[Command(Name = "OtherName", Help = "Subtracts 2 numbers")]
public static int Sub(int a, int b)
{
    int result = a - b;
    return a - b;
} 
```


There is support for options and option-like arguments

```C#
[Command]
public static void Example(
    [Argument]                                        int a, // defines a normal positional argument
    [Argument("Help message")]                        int b, // defines a positional argument
    [Argument("cc", "Help", IsOptionLike = false)]    int c, // defines an option-like argument
    [Argument(IsOptionLike = true)]                   int d = 1, // defines an option-like argument
    [Argument("ee", "Help")]                          int e = 2, // defines an option-like argument
)
{
}
```

The usage of `example` will be:
```
example  1 2 3 4 5        a = 1, b = 2, c = 3, d = 4, e = 5
example  1 2              error: c not given a value.
example  1 2 3            a = 1, b = 2, c = 3, d = 1, e = 2
example  1 2 -cc=5        a = 1, b = 2, c = 5, d = 1, e = 2
example  1 2 -cc=9 -ee=8  a = 1, b = 2, c = 9, d = 1, e = 8
example  1 2 -cc=9 -d=8   a = 1, b = 2, c = 9, d = 8, e = 2
example  1                error: b and c not given a value.
example                   prints help. 
```


Options are just like option-like arguments, but cannot be positional.

```C#
[Command]
public static void Example(
    [Argument] int a,       // positional argument
    [Option]   int b,       // required option argument
    [Option]   int c = 5    // option argument with default value
)
{}
```

The usage of `example` will be:
```
example                 prints help.
example  1              error: option b not given a value
example  1 2            error: extra argument `2`, b not given a value
example  1 -b=2         a = 1, b = 2, c = 5
example  1 -b=2 -c=3    a = 1, b = 2, c = 3
```


Options may declare themselves as flags, in which case they must be bool.

```C#
public static void Example(
    [Option(IsFlag = true)]     bool a, // flags are false by default
    [Option]                    bool b = true,
    [Option(IsFlag = true)]     bool c = true
)
{}
```

The usage of `example` will be:
```
example                       a = false, b = true, c = true
example -a -b -c              error: b cannot be used like a flag
example -a -b=true -c         a = true, b = true, c = true
example -a -b=false -c=false  a = true, b = false, c = false
example -b=false -c=false     a = false, b = false, c = false
```

Your arguments and options may use a user-defined parser, see below. The flags are limite to the bool type, but are not limited to the default true/false parser. The example below uses a switch parser, which uses `on/off` instead of `true/false`:
```C#
public static void Example(
    [Option(IsFlag = true, Parser = "Switch")]   bool flag
)
{}
```

Examples:
```
example            flag = false
example -flag      flag = true
example -flag=on   flag = true
example -flag=off  flag = false
```


### 2. Using a FrontCommand method:

The front command will receive the command context, which will allow you to do meta stuff with the terminal, or parse the arguments in a special way.

Do not use this method if you don't need to interact with the terminal, since custom parsers, arguments and options are more than enough.

The method must be public and static, in any non-nested class.

```C#
// Kari assumes it takes any number of arguments by default
[FrontCommand(Name = "Example", Help = "Does stuff")]
public static void Example(CommandContext context) 
{
    var commands = context.Shell.Commands; // all currently registered commands
    if (commands.ContainsKey("help")) {}   // do not worry about capitalization, it's handled by the object
    
    context.Log("Hello");                  // logs "Hello" to the terminal via the logger.
    context.LogError("Hello");             // logs "Hello" to the terminal via the logger, sets context.HasErrors to true.

    var logger = context.Logger;           // get the logger, to e.g. loop through the messages.

    var variables = context.Variables;     // all defined variables, without the '$' prefix
    context.Log(variables["world"]);       // logs the value of $world

    // Parse the first argument as int. 
    // The name is needed for better error messages.
    int value = context.ParseArgument(index: 0, name: "SomeArgument", Parsers.Int); 
    // Parse -SomeOption as a required bool option
    bool value2 = context.ParseOption(name: "SomeOption", Parsers.Bool);
    // Parse -Other as an int64 with default value of 69
    long value3 = context.ParseOption(name: "Other", defaultValue: 69, Parsers.Int64);

    // You can also access the command name.
    context.Command;
    // The list of unparsed arguments.
    context.Arguments;
    // The dictionary of options.
    context.Options;
    // And the exact issued command string
    context.Scanner.Source;

    // Logs errors for all unused options
    context.EndParsing();

    // If any of the parsing has generated an error
    // In out case `context.LogError("Hello");` always generates errors prior to parsing.
    if (context.HasErrors) return;

    // Do the actual work once the arguments are parsed
}
```

If your command will always take a certain number of positional arguments, specify it in the attribute.

```C#
// Takes between 1 and 2 arguments.
[FrontCommand(MinimumNumberOfArguments = 1, MaximumNumberOfArguments = 2)]
public static void Example1(CommandContext context) 
{}

// Takes exactly 1 argument.
[FrontCommand(NumberOfArguments = 1)]
public static void Example2(CommandContext context) 
{}
```

### 3. Manually adding Commands

You can instantiate and register new commands at runtime.

```C#
public class MyCommand : CommandBase
{
    public MyCommand() : base(
        minimumNumberOfArguments: 1, 
        maximumNumberOfArguments: 2, 
        help: "Help", 
        extendedHelp: "Help with more info")
    {}

    // Just like a FrontCommand
    public override void Execute(CommandContext context) {}
}

// Add it to the list of commands
// First get the terminal component of your terminal game object
var Terminal = get_that_component();
// Then register the command
Terminal.Shell.RegisterCommand("MyCommandName", new MyCommand());
```

You may also create commands from delegates at runtime. The delegates must take a `CommandContext` as the only argument.

```C#
void lambda(CommandContext context) {}
var command = new GenericCommand(
    minimumNumberOfArguments: 1, 
    maximumNumberOfArguments: 2, 
    helpMessage: "123",
    extendedHelp: "123123",
    proc: lambda)
Terminal.Shell.RegisterCommand("MyDynamicCommand", command);
```


## Parsers

Parsers are named methods used to either parse custom types or parse existing types in a special way.
For example, here I have implemented a Switch parser:

```C#
// Self-reference is a nice technique here.
// The code generator is able to infer that `nameof(Parsers.Switch)` evaluates to "Switch"
// even if the corresponding symbol does not exist.
// This really is extra: you don't really need that, since the code generator gives you errors 
// if it finds a reference to a non-existent parser.
[Parser(nameof(Parsers.Switch))]
public static ParseSummary ParseSwitch(string input, out bool output)
{
    if (string.Equals(input, "ON", System.StringComparison.OrdinalIgnoreCase))
    {
        output = true;
        return ParseSummary.Success;
    }
    
    if (string.Equals(input, "OFF", System.StringComparison.OrdinalIgnoreCase))
    {
        output = false;
        return ParseSummary.Success;
    }

    output = false;
    return ParseSummary.TypeMismatch("Switch (on/off)", input);
}
```

Then, to use this parser, just specify it in the `Argument` or `Option` attribute:

```C#
[Command]
public static void Example(
    [Argument(Parser = "Switch")] bool value
) {}
```

Now the command can be used like this:
```
example          prints help
example  on      value = true
example  off     value = false
example  true    Error while parsing 1st argument 'value': Expected input compatible with type Switch (on/off), got 'true'.     
```

If your function takes a custom type, and you have defined a parser for it, its name may be omitted.
But be careful: if there are more than one custom parsers, the first one will be selected (essentially at random, potentially different between `Kari` runs).

```C#
public static void Example(
    [Argument] CustomType value
) {}
```


You may define parsers with the same name for different types. So this would be possible, assuming you have defined the 2 parsers for the 2 types:

```C#
public static void Example(
    [Argument(Parser = "MyParser")] CustomType value
    [Argument(Parser = "MyParser")] int value
) {}
```

I'm unsure if this feature is useful, so I would probably avoid it.