# tldr-sharp

A commandline tool to grab tldr pages written in C# (c-sharp).

+ checks os version to show relevant documents
+ locally stores the tldr pages (next to runtime)
+ faster than node
+ standalone binary

<br>

Invoked from the commandline using the binary name.

```
tldr-sharp

Display simple help pages for command-line tools from the tldr-pages project.
More information: https://tldr.sh.

- Print the tldr page for a specific command
tldr-sharp <command>
```

<br>

### *example usage:*
```sh
$ tldr-sharp dotnet
# dotnet

> Cross platform .NET command-line tools for .NET Core.
> Some subcommands such as `dotnet build` have their own usage documentation.
> More information: <https://learn.microsoft.com/dotnet/core/tools>.

- Initialize a new .NET project:

`dotnet new template_short_name`

- Restore NuGet packages:

`dotnet restore`

- Build and execute the .NET project in the current directory:

`dotnet run`

- Run a packaged dotnet application (only needs the runtime, the rest of the commands require the .NET Core SDK installed):

`dotnet path/to/application.dll`
```

<br>

### Check out tldr-pages here:
Project using the community-driven man pages.

The tldr-pages project is a collection of community-maintained help pages for command-line tools,
that aims to be a simpler, more approachable complement to traditional man pages.

[tldr-pages](https://github.com/tldr-pages/tldr)

<br>

### *benchmark*

benchmark captured with `/usr/bin/time -v`

**csharp**
```sh
        Command being timed: "tldr-sharp node"
        User time (seconds): 0.01
        System time (seconds): 0.00
        Percent of CPU this job got: 175%
        Elapsed (wall clock) time (h:mm:ss or m:ss): 0:00.01
        Average shared text size (kbytes): 0
        Average unshared data size (kbytes): 0
        Average stack size (kbytes): 0
        Average total size (kbytes): 0
        Maximum resident set size (kbytes): 14724
        Average resident set size (kbytes): 0
        Major (requiring I/O) page faults: 0
        Minor (reclaiming a frame) page faults: 1343
        Voluntary context switches: 7
        Involuntary context switches: 0
        Swaps: 0
        File system inputs: 0
        File system outputs: 0
        Socket messages sent: 0
        Socket messages received: 0
        Signals delivered: 0
        Page size (bytes): 4096
        Exit status: 0
```
**node**
```sh
Command being timed: "tldr node"
        User time (seconds): 0.33
        System time (seconds): 0.06
        Percent of CPU this job got: 125%
        Elapsed (wall clock) time (h:mm:ss or m:ss): 0:00.31
        Average shared text size (kbytes): 0
        Average unshared data size (kbytes): 0
        Average stack size (kbytes): 0
        Average total size (kbytes): 0
        Maximum resident set size (kbytes): 130616
        Average resident set size (kbytes): 0
        Major (requiring I/O) page faults: 2
        Minor (reclaiming a frame) page faults: 29933
        Voluntary context switches: 282
        Involuntary context switches: 15
        Swaps: 0
        File system inputs: 8
        File system outputs: 0
        Socket messages sent: 0
        Socket messages received: 0
        Signals delivered: 0
        Page size (bytes): 4096
        Exit status: 0
```
