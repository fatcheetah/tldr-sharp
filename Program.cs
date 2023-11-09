using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System;

var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
var dataLocation = $"{baseDirectory}tldr-sharp.dat";

if (!args.Any())
{
    WriteHelp();
    return;
}

var arguments = args.Select(arg => arg.ToLower()).ToList();
var cmdBuilder = new StringBuilder();
var platform = string.Empty;

for (var i = 0; i < arguments.Count; i++)
{
    var arg = arguments[i];

    switch (arg)
    {
        case "-h" or "--help":
            WriteHelp();
            return;

        case "-v" or "--version":
            WriteVersion();
            return;

        case "-l" or "--list":
            DownloadPagesZipDeflateContents();
            ListCommands();
            return;

        case "-r" or "--random":
            DownloadPagesZipDeflateContents();
            GetRandomCommand();
            return;

        case "-p" or "--platform" when i == arguments.Count:
            Console.WriteLine("platform not specified");
            return;

            // Platform
            //
            //     Clients MUST default to displaying the page associated with the platform on which the client is running. For example, a client running on Windows 11 will default to displaying pages from the windows platform. Clients MAY provide a user-configurable option to override this behaviour, however.
            //
            //     If a page is not available for the host platform, clients MUST fall back to the special common platform.
            //
            //     If a page is not available for either the host platform or the common platform, then clients SHOULD search other platforms and display a page from there - along with a warning message.
            //
            //     For example, a user has a client on Windows and requests the apt page. The client consults the platforms in the following order:
            //
            // windows - Not available
            // common - Not available
            // osx - Not available
            // linux - Page found

        
        case "-p" or "--platform":
        {
            platform = arguments[i + 1] switch
            {
                "osx" or "macos" => "osx",
                "windows" or "win" => "windows",
                "linux" or "unix" => "linux",
                _ => platform
            };

            i++;
            break;
        }

        default:
            cmdBuilder.Append($"-{arg}");
            break;
    }
}

if (cmdBuilder.Length != 0)
{
    var command = cmdBuilder.ToString()[1..];

    if (platform != string.Empty)
    {
        Console.WriteLine($"{platform},{command}");
    }
    else
    {
        DownloadPagesZipDeflateContents();
        GetCommand(command);
    }

    return;
}

WriteHelp();
return;

void WriteVersion()
{
    Console.WriteLine(
        """
        tldr-sharp 2311.09
        client spec 2.0
        """
    );
}

void WriteHelp()
{
    ConsoleEx.WriteColor("# tldr-sharp\n\n", ConsoleColor.Yellow);
    Console.Write(
        """
        Display simple help pages for command-line tools from the tldr-pages project.
        More information: https://tldr.sh.

        - Print the tldr page for a specific command

        """);
    ConsoleEx.WriteColor("`tldr-sharp ", ConsoleColor.DarkBlue);
    ConsoleEx.WriteColor("<command>", ConsoleColor.Yellow);
    ConsoleEx.WriteColor("`\n\n", ConsoleColor.DarkBlue);
    Console.WriteLine(
        """
        -V,  --version           Display Version
        -l,  --list              List all commands for current platform
        -r,  --random            Show a random command
        -h,  --help              Show this information
        """);
}

void DownloadPagesZipDeflateContents()
{
    if (File.Exists(dataLocation)) return;

    try
    {
        const string url = "https://github.com/tldr-pages/tldr/archive/refs/tags/v2.0.zip";
        Console.WriteLine($"Downloading pages from {url} - file size: 5.8M");
        var client = new HttpClient();
        var httpStream = client.GetStreamAsync(url).Result;

        using var archive = new ZipArchive(httpStream, ZipArchiveMode.Read);
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream, Encoding.UTF8, false);

        var osPath = Environment.OSVersion.Platform switch
        {
            PlatformID.Unix => "linux",
            PlatformID.MacOSX => "osx",
            PlatformID.Other => "windows",
            _ => "windows",
        };

        var pagePath = $"tldr-2.0{Path.DirectorySeparatorChar}pages{Path.DirectorySeparatorChar}";
        var commonEntries = archive.Entries.Where(e => e.FullName.StartsWith($"{pagePath}common{Path.DirectorySeparatorChar}"));
        var osEntries = archive.Entries.Where(e => e.FullName.StartsWith($"{pagePath}{osPath}{Path.DirectorySeparatorChar}"));
        var entries = commonEntries.Concat(osEntries);

        var keys = new StringBuilder();

        foreach (var entry in entries)
        {
            var match = entry.FullName.LastIndexOf(Path.DirectorySeparatorChar) + 1;
            var keyName = entry.FullName[match..].Replace(".md", string.Empty);

            using var streamReader = new StreamReader(entry.Open());
            string contents = streamReader.ReadToEnd();

            keys.Append($"{keyName},");
            writer.Write(keyName);
            writer.Write(contents);
        }

        using var memoryHeadersStream = new MemoryStream();
        using var writerHeaders = new BinaryWriter(memoryHeadersStream, Encoding.UTF8);
        writerHeaders.Write(keys.ToString());

        memoryStream.Position = 0;
        memoryStream.CopyTo(memoryHeadersStream);
        memoryStream.Dispose();

        using var compressStream = File.Open(dataLocation, FileMode.Create);
        using var compressor = new BrotliStream(compressStream, compressionLevel: CompressionLevel.Optimal);

        memoryHeadersStream.Position = 0;
        memoryHeadersStream.CopyTo(compressor);

        compressor.Dispose();
        archive.Dispose();
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
}

void GetCommand(string commandName)
{
    using var dataFile = File.Open(dataLocation, FileMode.Open);
    using var decompressor = new BrotliStream(dataFile, CompressionMode.Decompress);
    using var reader = new BinaryReader(decompressor, encoding: Encoding.UTF8, false);

    try
    {
        var commandIndexes = reader.ReadString()
            .Split(",")
            .OrderBy(ci => ci);

        if (!commandIndexes.Contains(commandName))
        {
            ConsoleEx.WriteColor($"{commandName} ", ConsoleColor.Yellow);
            Console.Write("not found \n");
            return;
        }

        while (true)
        {
            var key = reader.ReadString();
            var value = reader.ReadString();

            if (key != commandName) continue;

            WriteContentOfFile(value);
            return;
        }
    }
    catch (EndOfStreamException)
    {
        return;
    }
}

void ListCommands()
{
    using var dataFile = File.Open(dataLocation, FileMode.Open);
    using var decompressor = new BrotliStream(dataFile, CompressionMode.Decompress);
    using var reader = new BinaryReader(decompressor, encoding: Encoding.UTF8, false);

    try
    {
        var commandIndexes = reader.ReadString()
            .Split(",")
            .OrderBy(ci => ci)
            .ToList();

        var commandBuilder = new StringBuilder();
        commandIndexes.ForEach(cmd => { commandBuilder.Append($"{cmd},"); });

        ConsoleEx.WriteColor(commandBuilder.ToString(), ConsoleColor.Yellow);
        Console.Write("\n");
    }
    catch (EndOfStreamException)
    {
        return;
    }
}

void GetRandomCommand()
{
    using var dataFile = File.Open(dataLocation, FileMode.Open);
    using var decompressor = new BrotliStream(dataFile, CompressionMode.Decompress);
    using var reader = new BinaryReader(decompressor, encoding: Encoding.UTF8, false);

    try
    {
        var commandIndexes = reader.ReadString()
            .Split(",")
            .OrderBy(ci => ci)
            .ToList();

        var index = new Random().Next(commandIndexes.Count);
        var keyCommand = commandIndexes[index];

        while (true)
        {
            var key = reader.ReadString();
            var value = reader.ReadString();

            if (key != keyCommand) continue;

            WriteContentOfFile(value);
            return;
        }
    }
    catch (EndOfStreamException)
    {
        return;
    }
}

void WriteContentOfFile(string value)
{
    var bytes = Encoding.UTF8.GetBytes(value);
    var firstLine = true;
    var inCodeBlock = false;
    for (int i = 0; i < value.Length; i++)
    {
        var character = value[i];
        switch (character)
        {
            case (char)10:
                firstLine = false;
                ConsoleEx.Write(character);
                break;
            case (char)58: // :
                ConsoleEx.Write(character);
                if (value[i + 1] == (char)10) i++;
                break;
            case (char)60: // <
            case (char)62: // >
                ConsoleEx.WriteColor(character, ConsoleColor.Red);
                continue;
            case (char)96: // `
                inCodeBlock ^= true;
                ConsoleEx.WriteColor(character, ConsoleColor.DarkBlue);
                break;
            case (char)123: // {
            case (char)125: // }
                continue;
            default:
                if (firstLine)
                {
                    ConsoleEx.WriteColor(character, ConsoleColor.Yellow);
                    break;
                }

                if (inCodeBlock)
                {
                    ConsoleEx.WriteColor(character, ConsoleColor.DarkBlue);
                    break;
                }

                ConsoleEx.Write(character);
                break;
        }
    }
}

public static class ConsoleEx
{
    public static void Write(int write)
    {
        Console.Write((char)write);
    }

    public static void WriteColor(int write, ConsoleColor foregroundColor)
    {
        Console.ForegroundColor = foregroundColor;
        Console.Write((char)write);
        Console.ResetColor();
    }

    public static void WriteColor(string write, ConsoleColor foregroundColor)
    {
        Console.ForegroundColor = foregroundColor;
        Console.Write(write);
        Console.ResetColor();
    }

    public static void ColorToggle(ConsoleColor foregroundColor)
    {
        if (Console.ForegroundColor == foregroundColor)
        {
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = foregroundColor;
    }
}