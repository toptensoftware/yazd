using System;
using System.Collections.Generic;
using System.IO;
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

                    case "ast":
                        if (Value != null)
                        {
                            _ast = new StreamWriter(Value);
                        }
                        else
                        {
                            _ast = Console.Out;
                        }
                        break;

                    case "symbols":
                        if (Value != null)
                        {
                            _symbols = new StreamWriter(Value);
                        }
                        else
                        {
                            _symbols = Console.Out;
                        }
                        break;

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
        TextWriter _ast;
        TextWriter _symbols;

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
            Console.WriteLine("  --instructionList      Display a list of all support instructions");
            Console.WriteLine("  --ast[:filename]       Dump the AST to filename or stdout");
            Console.WriteLine("  --symbols[:filename]   Dump symbols to filename or stdout");

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

            // Step 1 - Parse the input file
            var p = new Parser();
            var file = p.Parse(_inputFile, System.IO.Path.GetFullPath(_inputFile));

            // Step 2 - Create the root scope and define all symbols
            var root = new AstScope("global");
            root.AddElement(file);
            root.DefineSymbols(null);

            if (_ast != null)
            {
                root.Dump(_ast, 0);
                if (_ast != Console.Out)
                    _ast.Dispose();
                _ast = null;
            }

            // Step 3 - Map instructions
            var layoutContext = new LayoutContext();
            root.Layout(null, layoutContext);

            if (Log.ErrorCount == 0)
            {
                // Step 4 - Generate Code
                var generateContext = new GenerateContext(layoutContext);

                root.Generate(null, generateContext);
            }


            // Close symbols file
            if (_symbols != null)
            {
                if (Log.ErrorCount == 0)
                    root.DumpSymbols(_symbols);

                if (_symbols != Console.Out)
                    _symbols.Dispose();
                _symbols = null;
            }

            Log.DumpSummary();

            return 0;
        }

        static int Main(string[] args)
        {
            try
            {
                return new Program().Run(args);
            }
            catch (InvalidOperationException x)
            {
                Console.WriteLine("{0}", x.Message);
                return 7;
            }
            catch (IOException x)
            {
                Console.WriteLine("File Error - {0}", x.Message);
                return 7;
            }
        }
    }
}
