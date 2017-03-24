// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Internalizer
{
    public class Program
    {
        private static UTF8Encoding _utf8Encoding = new UTF8Encoding(false);

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
            foreach (var file in files)
            {
                var contents = File.ReadAllText(file);
                var newContents = Process(contents);
                if (newContents != contents)
                {
                    File.WriteAllText(file, newContents, _utf8Encoding);
                }
            }
        }

        private static string Process(string contents)
        {
            Regex r = new Regex("public((static|unsafe|partial|\\s)+(class|struct|interface|delegate))");
            return r.Replace(contents, match => match.Groups[1].Value);
        }
    }
}
