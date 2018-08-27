﻿using System;
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

                    case "instructionSet":
                        InstructionSet.DumpAll();
                        return false;

                    case "ast":
                        if (Value != null)
                        {
                            _astFile = Value;
                        }
                        else
                        {
                            _astFile = ":default";
                        }
                        break;

                    case "sym":
                        if (Value != null)
                        {
                            _symbolsFile = Value;
                        }
                        else
                        {
                            _symbolsFile = ":default";
                        }
                        break;

                    case "list":
                        if (Value != null)
                        {
                            _listFile = Value;
                        }
                        else
                        {
                            _listFile = ":default";
                        }
                        break;

                    case "define":
                        if (Value == null)
                        {
                            throw new InvalidOperationException("--define: requires argument value");
                        }
                        else
                        {
                            var eqPos = Value.IndexOf('=');
                            if (eqPos < 0)
                                _userDefines.Add(Value, null);
                            else
                                _userDefines.Add(Value.Substring(0, eqPos), Value.Substring(eqPos + 1));
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
        Dictionary<string, string> _userDefines = new Dictionary<string, string>();

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
            Console.WriteLine("  --output:filename      Output file");
            Console.WriteLine("  --list[:filename]      Generate a list file");
            Console.WriteLine("  --sym[:filename]       Generate a symbols file");
            Console.WriteLine("  --ast[:filename]       Dump the AST");
            Console.WriteLine("  --define:symbol[=expr] Define a symbol with optional value");
            Console.WriteLine("  --instructionSet       Display a list of all support instructions");

            Console.WriteLine();
            Console.WriteLine("Numeric arguments can be in decimal (no prefix) or hex if prefixed with '0x'.");

            Console.WriteLine();
            Console.WriteLine("Response file containing arguments can be specified using the @ prefix");

            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("    yaza input.asm");
            Console.WriteLine();
        }

        public TextWriter OpenTextWriter(string filename, string defExt)
        {
            if (filename == ":default")
                return new StreamWriter(System.IO.Path.ChangeExtension(_inputFile, defExt));
            else
                return new StreamWriter(filename);
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

            // Define built in symbols
            var exprNodeIP = new ExprNodeIP();
            root.Define("$", exprNodeIP);

            // Define user specified symboles
            foreach (var kv in _userDefines)
            {
                var defParser = new Parser();
                if (kv.Value != null)
                {
                    try
                    {
                        var exprNode = defParser.ParseExpression(kv.Value);
                        root.Define(kv.Key, exprNode);
                    }
                    catch (CodeException x)
                    {
                        throw new InvalidOperationException(x.Message + " in command line symbol definition");
                    }
                }
                else
                {
                    root.Define(kv.Key, new ExprNodeLiteral(1));
                }
            }

            // Run the "Define Symbols" pass
            root.DefineSymbols(null);


            if (_astFile != null)
            {
                using (var w = OpenTextWriter(_astFile, ".ast.txt"))
                {
                    root.Dump(w, 0);
                }
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
                    generateContext.ListFile = OpenTextWriter(_listFile, "lst");

                generateContext.EnterSourceFile(file.SourcePosition);
                root.Generate(null, generateContext);
                generateContext.LeaveSourceFile();

                if (_listFile != null)
                {
                    generateContext.ListFile.Dispose();
                    generateContext.ListFile = null;
                }

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
                using (var w = OpenTextWriter(_symbolsFile, "sym"))
                    root.DumpSymbols(w);
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
