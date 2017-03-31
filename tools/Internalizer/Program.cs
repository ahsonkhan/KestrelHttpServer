// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

namespace Internalizer
{
    public class Program
    {
        private const string MicrosoftAspnetcoreServerKestrelInternal = "Microsoft.AspNetCore.Server.Kestrel.Internal";
        private const string DefaultUsings = @"
using System;
using System.Runtime;
using System.Buffers;

";
        private static readonly UTF8Encoding _utf8Encoding = new UTF8Encoding(false);
        private static readonly Regex _usingRegex = new Regex("using\\s+([\\w.]+)\\s*;", RegexOptions.Compiled);
        private static readonly Regex _namespaceRegex = new Regex("namespace\\s+([\\w.]+)", RegexOptions.Compiled);

        public static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("Missing path to file list");
                return 1;
            }
            Run(args[0]);

            return 0;
        }

        public static void Run(string csFileList)
        {
            var files = File.ReadAllLines(csFileList);
            var namespaces = new HashSet<string>();
            var fileContents = files.Select(file =>
            {
                var text = File.ReadAllText(file);
                return new FileEntry(path: file, oldText : text, newText : text);
            }).ToArray();

            foreach (var fileEntry in fileContents)
            {
                fileEntry.NewText = ProcessNamespaces(fileEntry.NewText, namespaces);
            }

            foreach (var fileEntry in fileContents)
            {
                fileEntry.NewText = ProcessUsings(fileEntry.NewText, namespaces);
                if (fileEntry.NewText != fileEntry.OldText)
                {
                    File.WriteAllText(fileEntry.Path, fileEntry.NewText, _utf8Encoding);
                }
            }
        }

        private static string ProcessNamespaces(string contents, HashSet<string> namespaces)
        {
            return _namespaceRegex.Replace(contents, match =>
            {
                var ns = match.Groups[1].Value;
                namespaces.Add(ns);
                return $"namespace {MicrosoftAspnetcoreServerKestrelInternal}.{ns}";
            });
        }

        private static string ProcessUsings(string contents, HashSet<string> namespaces)
        {
            return DefaultUsings + _usingRegex.Replace(contents, match =>
            {
                var ns = match.Groups[1].Value;
                if (namespaces.Contains(ns))
                {
                    return $"using {MicrosoftAspnetcoreServerKestrelInternal}.{ns};";
                }
                return match.Value;
            });
        }
    }

    public class FileEntry
    {
        public string Path { get; set; }
        public string OldText { get; set; }
        public string NewText { get; set; }

        public FileEntry(string path, string oldText, string newText)
        {
            Path = path;
            OldText = oldText;
            NewText = newText;
        }
    }
}
