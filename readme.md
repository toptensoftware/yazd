# YAZD/YAZA - Yet Another Z80 Disassembler/Assembler

## YAZD

YAZD is a simple command line disassembler for Z80 binary code files.  It's based on the disassembler in [z80ex](http://z80ex.sourceforge.net/), ported to C#.

YAZD supports the following:

* Disassembly of all Z80 instructions, as supported by z80ex.
* Code path analysis can usually tell the difference between code and data.
* Generates labelled assembly language listings.
* Can also generate more detailed listing files with byte code and assembly source.
* Can detect procedure boundaries and generate call graphs
* Can generate reference listings to all external addresses and I/O ports.
* Can highlight all word literals (use to help find other memory address references).
* Can generate plain text, or hyperlinked HTML output files.
* Handles references to addresses not aligned with instruction (eg: self modifying code)
* Data segments are listed 1 DB byte per line with ASCII character in comments.

[Using YAZD](yazd.md)


## YAZA

YAZA is a Z-80 assembler that generates flat binary code (as opposed to relocatable .rel files).  It's very similar to [z80asm](https://www.nongnu.org/z80asm/) but includes a bunch of enhancements:

* Support for undocumented Z-80 instructions
* Powerful expression engine
* `include` and `incbin` support
* Scoped `PROC`s for local symbol support
* Macro Support
* Parameterized EQUates
* Bitmap data generation (eg: tile maps and PCG characters)

[Using YAZA](yaza.md)

## Download

Download here:

* <https://raw.github.com/toptensoftware/yazd/master/yazd.zip>

Requires:

* Windows and .NET 4.0 or later
* Linux/OSX with Mono 2.8 or later (not tested, should work)

