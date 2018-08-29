# Using YAZA

## Command Line

	yaza sourceFile.asm [options] [@responsefile]

Options:

    --ast[:<filename>]         Dump the AST
    --define:<symbol>[=<expr>] Define a symbol with optional value
    --help                     Show these help instruction
    --include:<directory>      Specifies an additional include/incbin directory
    --instructionSet           Display a list of all support instructions
    --list[:<filename>]        Generate a list file
    --output:<filename>        Output file
    --sym[:<filename>]         Generate a symbols file
    --v                        Show version information

Response file containing arguments can be specified using the @ prefix

Defining a symbol without specifying a value sets the symbol's value as `1`.

Example:

    yaza myprogram.asm --lst

## Basics

Instructions should be written using the standard Zilog Z-80 instruction format.  A full list of supported instructions (including undocumented instructions) can be obtained using `yaza --instructionSet`.

Example:

~~~
        LD  HL,0F000h
        LD  DE,0F001h
        LD  BC,03FFh
        LDIR
~~~


## Case Insensitive

YAZA is case insensitive for everything except DEFBITS definitions.  Instruction mnemonics, registers, condition flags, symbols etc... can all be specified in any case and symbol look ups are case insensitive.


## Labels

Labels are declared as an identifier (`A`-`Z`, `a`-`z`, `0`-`9`, `_` and `$`) at the start of a line followed by a colon (`:`):

eg:

~~~
loop:   ADD HL,DE
        DJNZ loop
~~~

Labels can also be on a line by themselves:

~~~
loop:
        ADD HL,DE
        DJNZ loop
~~~

## Numbers

Various numbers formats are supported:

~~~
    LD  HL,0xFFFF       ; '0x' prefix for hex
    LD  HL,0n9999       ; '0n' prefix for decimal
    LD  HL,0t7777       ; '0n' prefix for octal
    LD  HL,0b1111       ; '0b' prefix for binary

    LD  HL,0FFFFh       ; 'h' suffix for hex
    LD  HL,9999d        ; 'd' suffix for decimal
    LD  HL,9999n        ; 'n' suffix for decimal
    LD  HL,7777o        ; 'o' suffix for octal
    LD  HL,7777q        ; 'q' suffix for octal
    LD  HL,7777t        ; 't' suffix for octal
    LD  HL,1111b        ; 'b' suffix for binary

    LD  HL,$FFFF        ; '$' prefix for hex
    LD  HL,&hFFFF       ; '&h' prefix for hex
    LD  HL,&n9999       ; '&n' prefix for decimal
    LD  HL,&d9999       ; '&n' prefix for decimal
    LD  HL,&o7777       ; '&o' prefix for decimal
    LD  HL,&t7777       ; '&t' prefix for decimal
    LD  HL,&b1001       ; '&b' prefix for binary
~~~

The `RADIX` directive can be used to set the default radix to either 2, 8, 10 or 16.

~~~
    RADIX 2             ; Select binary radix
    LD  HL,1000         ; 8

    RADIX 16            ; Select hex radix
    LD  HL,0F           ; 16
~~~

Note that when the hex radix is selected, numbers must still start with a digit which might mean introducing a leading `0` (ie: `0ff` not `ff`)

## Expressions

The following expression operators are supported (shown in order of operation)

```
( ) defined     ; grouping and the `defined` operator
~ ! - +         ; bitwise complement, logical not, negate, postive
* / %           ; multiply, divide, modulus
+ -             ; add, subtract
<< >>           ; shift left, shift right
<= >= < >       ; less-equal, greater-equal, less, greater
== !=           ; equal, not equal
&               ; bitwise AND
^               ; bitwise XOR
|               ; bitwise OR
&&              ; logical AND
||              ; logical OR
? :             ; ternery operator
```

All numeric expressions are evaluated as 32-bit signed integers.  An error will be generated if the final resulting value does fit in the target operand.  For boolean (aka logical) operands, zero is treated as false and any non-zero value is treated as true.

Note that parentheses are used for two purposes - expression group and pointer dereferencing.

eg: dereferencing:

    LD      A,(IX+0)            ; () means dereference pointer

eg: expresion grouping:

    LD      A, 5*(23+10)        ; () means 23+10 before *

So this will generate an error

    LD      HL,(23+5)*10        ; Invalid instruction because trying to dereference (23+5)

Instead, use this:

    LD      HL,0+(23+5)*10      ; This switches the expression parser out of dereference mode


The `defined` operator checks if a symbol has been defined.

eg:

~~~
if defined(DEBUG) && !defined(OPTIMIZED)
    ; debug code goes here
end if
~~~


## ORG Directive

The ORG directive changes the currently perceived CPU address:

~~~
    ORG     0x400
~~~


## SEEK Directive

The seek directive lets you rewind the output position of the generated code to overwrite previously generated data

~~~
    SEEK    0           ; Rewind to the start of the file
~~~

The current output position is available through the special variable `$ofs` (as in "offset").


Typically used to patch over `incbin`ed data.


## DB and DW Data Directives

Byte (8-bit) data can be declared with the `DB`, `DEFB`, `DM` or `DEFM` (they're all functional identical). 

~~~
PLAYERX:    DB  0
GAMEOVER:   DB  "GAME OVER",0
~~~

For word (16-bit) data use the `DW` or `DEFW` directive:

~~~
SCORE:      DW  0
HIGHSCRORE: DW  0
~~~

You can also reserve a specified number bytes with the `DS` or `DEFS` directive:

~~~
SCRATCHPAD: DS  1024
~~~

## EQU Directive

The `EQU` directive lets you define symbols.

~~~
VRAM        EQU     0xF000
VRAMSIZE    EQU     0x400

        ; Clear screen
        LD      HL,VRAM
        LD      DE,VRAM+1
        LD      BC,VRAMSIZE-1
        LD      (HL),0x20
        LDIR    
~~~

EQU's can themselves be defined as expressions and can reference other EQU definitions:

~~~
VRAM        EQU     0xF000
VRAMSIZE    EQU     0x400
ENDOFVRAM   EQU     (VRAM+VRAMSIZE)
~~~

The name of an EQU definition can optionally be followed by a colon:

~~~
VRAM:       EQU     0xF000
~~~

(but the definition must be on the same line as the name)

## Parameterized EQU Directives

EQU directives can be parameterized:

~~~
PCGCHAR(label)  EQU     0x80 + (label - pcgdata) / 16

pcgdata:
char1:
    ...
char2
    ...
char3
    ...

        LD HL,PCGCHAR(char2)
~~~

Parameterized equates can be overloaded based on the number of parameters.  ie: multiple EQUs with the same name but a different number of parameters can be defined and the appropriate one will be used depending on how many parameters are specified where it's called.


## Special Symbols

The `$` symbol refers to the current instruction address:

~~~
        DJNZ    $           ; loop to self
~~~

In an EQU directive, the `$` symbol resolves to the instruction address at the location the EQU was defined.  eg: this works as expected

~~~
levelData:
        incbin "levelData.bin"
levelDataSize EQU $-levelData
~~~

The `$$` symbol also refers to the current instruction address, but when used in an EQU it resolves to the location the EQU is being invoked from - not the location where the EQU was originally defined.


The `$ofs` symbol refers to the current output position in the generated binary file.  This can be used with the `SEEK` directive to position the output position (typically for patching imported `incbin` data).  When used in EQU definitions, the `$ofs` is bound to the place where the EQU is defined - not where it's referenced from.  The `$$ofs` symbol can be used to refer to the current output position of where the EQU is invoked from.

## Conditional Compilation

The `if`, `else`, `elseif` and `endif` directives can be used to conditionally include code.

Use the `--define` command line switch to set symbols to be used during compilation.  eg: suppose you want to use a common code base for multiple versions of a product you could compile different versions:

~~~
yaza myprogram.asm --define:targetMachine=MICROBEE --output:myprogram.microbee.bin
yaza myprogram.asm --define:targetMachine=TRS80 --output:myprogram.trs80.bin
~~~

~~~
TRS80       EQU     1
MICROBEE    EQU     2
SORCERER    EQU     3

if targetMachine==TRS80

    ; code for TRS-80

elseif targetMachine==MICROBEE

    ; code for Microbee

elseif targetMachine==SORCERER

    ; code for Sorcerer

else

    error   "Unsupported machine"

endif
~~~

Important:  each branch of the `if` directive (even those that evaluate to false) must be syntactically correct  code - `if` directives can't be used to "commment out" code.

## PROC Directive

The `PROC`/`ENDP` directives can be used to define a local symbol scope.

In the following example, both functions have a local label `loop` that won't conflict because they're each within their own local `PROC` scope.

~~~
DELAY1: PROC
        LD  DE,0x1000
loop:
        DEC DE
        LD  A,D
        OR  E
        JR  NZ,loop
        RET
ENDP

DELAY2: PROC
        LD  DE,0x2000
loop:
        DEC DE
        LD  A,D
        OR  E
        JR  NZ,loop
        RET
ENDP
~~~


Note: although `PROC`'s are typically used to define function scopes, they don't have to be.  The label before `PROC` is optional and PROCs can even be nested if so desired.  They're really just a scoping syntax and don't infer any other meaning.


## MACRO Directive

Macros let you define repeatable pieces of code:

~~~
; Macro to zero fill memory
ZFILL(ptr,len) MACRO
    LD  HL,ptr
    LD  DE,ptr+1
    LD  BC,len-1
    LD  (HL),0
    LDIR
ENDM

; Invoke Macro (NB: no parentheses to invoke macro)
ZFILL 0xF000, 0x400
~~~

Like parameterized EQU's, macros can be overridden based on the number of arguments.  eg:

~~~
SAVEREGS(r1,r2) MACRO
    PUSH r1
    PUSH r2
ENDM

SAVEREGS(r1,r2,r3) MACRO
    PUSH r1
    PUSH r2
    PUSH r3
ENDM

    SAVEREGS(ix,iy)     ; Will invoke the first macro
    SAVEREGS(ix,iy,hl)  ; Will invoke the second macro
~~~

## INCLUDE and INCBIN Directives

The `include` directive brings in the contents of another asm file:

```
    include "definitions.asm"
```

The `incbin` embeds binary data from a file:

```
    incbin "levelmap.bin"
```

In both cases the included file is searched for relative to the file containing the include/incbin directive and if not found any directories specified by the `--include` command line option are searched.


## STRUCT Directives

The `STRUCT` directive can be used to define structured data:

~~~
RECT STRUCT
    LEFT    dw  ?
    TOP     dw  ?
    WIDTH   dw  ?
    HEIGHT  dw  ?
ENDS
~~~

The fields of the the struct can be accessed from any expression and evaluate to the offset of the member field from the start of the struct:

~~~
    LD      A,RECT.TOP      ; equivalent to "LD A,2"
~~~

Structs are often used with the IX and IY registers:

~~~
    LD      IX,addressOfARectStructure
    LD		E,(IX+RECT.WIDTH)
    LD		D,(IX+RECT.WIDTH+1)
~~~

The `sizeof` operator can be used to get the size of a `STRUCT`:

~~~
    ; Move to next RECT
    LD		DE,sizeof(RECT)
    ADD		IX,DE
~~~

STRUCTs can be nested and declaration order doesn't matter:

~~~

PLAYER  STRUCT
    FRAME   DB      ?
    POS     COORD   ?           ; See below
ENDS

ALIEN   STRUCT
    MODE    DB      ?
    POS     COORD   ?           ; See below
ENDS

COORD   STRUCT
    X       DB      ?
    Y       DB      ?
ENDS

    LD  A,(IX+ALIEN.POS.Y)      ; "LD A,(IX+2)"
~~~


## DEFBITS and BITMAP Directive

The DEFBITS and BITMAP directives can be used to define bitmap style data directly in your source files.  This can be used for defining PCG character sets, level maps or any other form of bitmap style data.

The directive works as follows:

1. The DEFBITS directive is used to map a single character to a sequence of bits
2. The BITMAP directive defines a set of strings using those defined characters to build up a "bitmap"
3. YAZA breaks out a series of bytes from the assembled bitmap

The bitmap directive includes a block width and block height parameter that defines how the bitmaps is converted to bytes.

eg:

~~~
    DEFBITS "#", "1"            ; The # character will represent a one bit
    DEFBITS ".", "0"            ; The . character will represent a zero

    BITMAP  8,4                 ; How to break group the bits into bytes

        "################"
        "#..............#"
        "#..............#"
        "#..............#"
        "#..............#"
        "#..............#"
        "#..............#"
        "################"

    ENDB
~~~

The `8,4` parameters above specify how to break the bitmap pattern up into bytes.  In this case the pattern will be broken into blocks of 8 x 4 so the first block would be the top left quarter of the bitmap (8 bits across, 4 rows):

ie:

        "########"
        "#......."
        "#......."
        "#......."

These bits would the be converted to bytes:

        0xFF, 0x80, 0x80, 0x80

Then the next block to the right will be processed:

        "########"
        ".......#"
        ".......#"
        ".......#"

producing:

        0xFF, 0x01, 0x01, 0x01

This is repeated for all block divisions of the entire bitmap, producing the final result:

    0xFF, 0x80, 0x80, 0x80,     // Top left quarter
    0xFF, 0x01, 0x01, 0x01,     // Top right quarter
    0x80, 0x80, 0x80, 0xFF,     // Bottom left 
    0x01, 0x01, 0x01, 0xFF,     // Bottom right

Note that the `DEFBITS` directive can define multiple bits:

~~~
    DEFBITS "R","0001"
    DEFBITS "G","0010"
    DEFBITS "B","0100"

    BITMAP 8,1
        "RRGGGGBB"
    ENDB
~~~

Instead of using a string bit pattern in DEFBITS, you can also use a bit width expression and value expression:

~~~
    DEFBITS "X", 4, 1          ; Equivalent to "0001" (value = 1, width = 4 bits)
    DEFBITS "Y", 4, 2          ; Equivalent to "0010"
    DEFBITS "Z", 4, 3          ; Equivalent to "0010"
~~~

Unlike other definitions and symbols, DEFBITS pattern can be redefined:

~~~
    DEFBITS "X", "10"           ; Original definition
    BITMAP 8,1
        "XXXX"
    ENDB

    DEFBITS "X", "01"           ; New definition
    BITMAP 8,1
        "XXXX"
    ENDB
~~~


Another example:

    DEFBITS "#", "1"            ; The # character will represent a one bit
    DEFBITS ".", "0"            ; The . character will represent a zero

    BITMAP  1,8                 ; <== Note the block width of 1

        "#..#"
        "####"
        "#..#"
        "#..#"
        "#..#"
        "#..#"
        "#..#"
        "####"

    ENDB

Now each column represents a byte (with MSB bit at the top)

    0xFF, 0x41, 0x41, 0xFF


Finally, the `MSB`/`LSB` parameter can be used to bit swap the generated bytes:

Another example:

    BITMAP  8,1,LSB         ; LSB mean the first bits encountered are the LSB bits

        "#..#"
        "####"
        "#..#"
        "#..#"
        "#..#"
        "#..#"
        "#..#"
        "####"

    ENDB

Results in swapped bit order:

    0xFF, 0x82, 0x82, 0xFF



## ERROR and WARNING  directives

The `ERROR` and `WARNING` directives can be used to generate error and warning messages:

```
    ERROR "I refuse to compile"

    WARNING "This might crash"
```