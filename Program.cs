using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

if (args.Length == 0)
{
    Console.Write("Usage: tldr-sharp");
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write(" <command> \n");
    Console.ResetColor();
    Console.WriteLine("> tldr-sharp curl \n");
    return;
}

var commandArgument = args[0];

const string pageLocation = "/home/choc/funnn/tldr-sharp/tldr-pages/pages";

ParseFileNamesToCommandNameIndex(out var commandIx);

if (commandIx.TryGetValue(commandArgument, out var value))
{
    GetContentOfFile(value);
}
else
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write($"{commandArgument} ");
    Console.ResetColor();
    Console.Write("page not found \n");
}

return;

void ParseFileNamesToCommandNameIndex(out Dictionary<string, string> commandIndex)
{
    commandIndex = new Dictionary<string, string>();
    var commandRegex = new Regex(@"\/([^\/]+)\.md$");
    var list = Directory.EnumerateFiles(pageLocation, "*.md", SearchOption.AllDirectories);
    foreach (var path in list)
    {
        var match = commandRegex.Match(path);
        var result = match.Groups[1].Value;
        commandIndex.TryAdd(result, path);
    }
}

void GetContentOfFile(string filePath)
{
    using var filestream = new FileInfo(filePath).Open(FileMode.Open, FileAccess.Read);
    using var streamReader = new StreamReader(filestream);
    var builder = new StringBuilder();

    while (filestream.CanRead)
    {
        builder.Clear();

        while (true)
        {
            var readByte = streamReader.Read();

            if (readByte == -1) // <EOF>
            {
                streamReader.Close();
                break;
            }

            builder.Append((char)readByte);
        }

        filestream.Close();
        break;
    }

    Console.WriteLine(builder.ToString());
}
