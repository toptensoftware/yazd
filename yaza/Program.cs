using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using yazd;

namespace yaza
{
    class Program
    {
        public bool ProcessArg(string arg)
        {
            if (arg == null)
                return true;

            if (arg.StartsWith("#"))
                return true;

            // Response file
            if (arg.StartsWith("@"))
            {
                // Get the fully qualified response file name
                string strResponseFile = System.IO.Path.GetFullPath(arg.Substring(1));

                // Load and parse the response file
                var args = Utils.ParseCommandLine(System.IO.File.ReadAllText(strResponseFile));

                // Set the current directory
                string OldCurrentDir = System.IO.Directory.GetCurrentDirectory();
                System.IO.Directory.SetCurrentDirectory(System.IO.Path.GetDirectoryName(strResponseFile));

                // Load the file
                bool bRetv = ProcessArgs(args);

                // Restore current directory
                System.IO.Directory.SetCurrentDirectory(OldCurrentDir);

                return bRetv;
            }

            // Args are in format [/-]<switchname>[:<value>];
            if (arg.StartsWith("/") || arg.StartsWith("-"))
            {
                string SwitchName = arg.Substring(arg.StartsWith("--") ? 2 : 1);
                string Value = null;

                int colonpos = SwitchName.IndexOf(':');
                if (colonpos >= 0)
                {
                    // Split it
                    Value = SwitchName.Substring(colonpos + 1);
                    SwitchName = SwitchName.Substring(0, colonpos);
                }

                switch (SwitchName)
                {
                    case "help":
                    case "h":
                    case "?":
                        ShowLogo();
                        ShowHelp();
                        return false;

                    case "v":
                        ShowLogo();
                        return false;

                    case "instructionList":
                        Instruction.DumpAll();
                        return false;

                    default:
                        throw new InvalidOperationException(string.Format("Unknown switch '{0}'", arg));
                }
            }
            else
            {
                if (_inputFile == null)
                    _inputFile = arg;
                else
                    throw new InvalidOperationException(string.Format("Too many command line arguments, don't know what to do with '{0}'", arg));
            }

            return true;
        }

        string _inputFile;

        public bool ProcessArgs(IEnumerable<string> args)
        {
            if (args == null)
                return true;

            // Parse args
            foreach (var a in args)
            {
                if (!ProcessArg(a))
                    return false;
            }

            return true;
        }


        public void ShowLogo()
        {
            System.Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            Console.WriteLine("yaza v{0} - Yet Another Z80 Assembler", v);
            Console.WriteLine("Copyright (C) 2012-2018 Topten Software. All Rights Reserved.");

            Console.WriteLine("");
        }

        public void ShowHelp()
        {
            Console.WriteLine("usage: yaza source.asm [options] [@responsefile]");
            Console.WriteLine();

            Console.WriteLine("Options:");
            Console.WriteLine("  --help                 Show these help instruction");
            Console.WriteLine("  --v                    Show version information");

            Console.WriteLine();
            Console.WriteLine("Numeric arguments can be in decimal (no prefix) or hex if prefixed with '0x'.");

            Console.WriteLine();
            Console.WriteLine("Response file containing arguments can be specified using the @ prefix");

            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("    yaza input.asm");
            Console.WriteLine();
        }

        public int Run(string[] args)
        {
            // Process command line
            if (!ProcessArgs(args))
                return 0;

            // Was there an input file specified?
            if (_inputFile == null)
            {
                ShowLogo();
                ShowHelp();
                return 7;
            }

            // Load file
            var filename = System.IO.Path.GetFullPath(_inputFile);
            var filetext = System.IO.File.ReadAllText(filename);
            var source = new StringSource(filetext, _inputFile, filename);
            var p = new Parser();
            var root = p.Parse(source);

            root.Dump(0);

            return 0;
        }

        static int Main(string[] args)
        {
            return new Program().Run(args);
        }
    }
}
