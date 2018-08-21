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
                        InstructionSet.DumpAll();
                        return false;

                    case "ast":
                        if (Value != null)
                        {
                            _astFile = Value;
                        }
                        else
                        {
                            _astFile = ":stdout";
                        }
                        break;

                    case "symbols":
                        if (Value != null)
                        {
                            _symbolsFile = Value;
                        }
                        else
                        {
                            _symbolsFile = ":stdout";
                        }
                        break;

                    case "list":
                        if (Value != null)
                        {
                            _listFile = Value;
                        }
                        else
                        {
                            _listFile = ":stdout";
                        }
                        break;


                    case "output":
                        if (Value != null)
                        {
                            _outputFile = Value;
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
        string _outputFile;
        string _astFile;
        string _symbolsFile;
        string _listFile;

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

        public TextWriter OpenTextWriter(string filename)
        {
            if (filename == ":stdout")
                return Console.Out;
            else
                return new StreamWriter(filename);
        }

        public void CloseTextWriter(TextWriter w)
        {
            if (w != Console.Out && w != null)
                w.Dispose();
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

            var exprNodeIP = new ExprNodeIP();
            root.Define("$", exprNodeIP);
            root.DefineSymbols(null);

            if (_astFile != null)
            {
                var w = OpenTextWriter(_astFile);
                root.Dump(w, 0);
                CloseTextWriter(w);
            }

            // Step 3 - Map instructions
            var layoutContext = new LayoutContext();
            exprNodeIP.SetContext(layoutContext);
            root.Layout(null, layoutContext);

            if (Log.ErrorCount == 0)
            {
                // Step 4 - Generate Code
                var generateContext = new GenerateContext(layoutContext);

                exprNodeIP.SetContext(generateContext);

                if (_listFile != null)
                    generateContext.ListFile = OpenTextWriter(_listFile);

                generateContext.EnterSourceFile(file.SourcePosition);
                root.Generate(null, generateContext);
                generateContext.LeaveSourceFile();

                CloseTextWriter(generateContext.ListFile);

                if (Log.ErrorCount == 0)
                {
                    var code = generateContext.GetGeneratedBytes();
                    var outputFile = _outputFile == null ? System.IO.Path.ChangeExtension(_inputFile, ".bin") : _outputFile;
                    System.IO.File.WriteAllBytes(outputFile, code);
                }
            }


            // Write symbols file
            if (_symbolsFile != null && Log.ErrorCount == 0)
            {
                var w = OpenTextWriter(_symbolsFile);
                root.DumpSymbols(w);
                CloseTextWriter(w);
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
