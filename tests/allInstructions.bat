@echo off
z80asm --list=allInstructions.z80asm.lst --output=allInstructions.z80asm.bin allInstructions.asm
..\yaza\bin\Debug\yaza --list:allInstructions.yaza.lst --output:allInstructions.yaza.bin allInstructions.asm
fc allInstructions.z80asm.bin allInstructions.yaza.bin