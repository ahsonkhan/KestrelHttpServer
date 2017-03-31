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
        private static UTF8Encoding _utf8Encoding = new UTF8Encoding(false);
        private static readonly HashSet<string> preservedNamespaces = new HashSet<string>()
        {
            "System",
            "System.Runtime",
            "System.Buffers"
        };

        private static readonly string preservedNamespacesUsings =
            string.Join(string.Empty,
                preservedNamespaces.Select(ns => "using " + ns + ";" + Environment.NewLine));

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
            foreach (var file in files)
            {
                var contents = File.ReadAllText(file);
                var newContents = ProcessNamespaces(contents, namespaces);
                if (newContents != contents)
                {
                    File.WriteAllText(file, newContents, _utf8Encoding);
                }
            }

            foreach (var file in files)
            {
                var contents = File.ReadAllText(file);
                var newContents = ProcessUsings(contents, namespaces);
                if (newContents != contents)
                {
                    File.WriteAllText(file, newContents, _utf8Encoding);
                }
            }
        }

        private static string ProcessNamespaces(string contents, HashSet<string> namespaces)
        {
            Regex r = new Regex("namespace\\s+([\\w.]+)");
            return r.Replace(contents, match =>
            {
                var ns = match.Groups[1].Value;
                namespaces.Add(ns);
                string result = "";
                result += "namespace " + MicrosoftAspnetcoreServerKestrelInternal + "." + ns;
                return result;
            });
        }

        private static string ProcessUsings(string contents, HashSet<string> namespaces)
        {
            Regex r = new Regex("using\\s+([\\w.]+)\\s*;");
            return preservedNamespacesUsings + r.Replace(contents, match =>
            {

                string result = match.Value;
                var ns = match.Groups[1].Value;
                if (namespaces.Contains(ns))
                {
                    result = "using Microsoft.AspNetCore.Server.Kestrel.Internal." + ns +";";
                }
                return result;
            });
        }
    }
}
