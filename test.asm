ZFILL(ptr,len) MACRO
    LD  HL,ptr
    LD  DE,ptr+1
    LD  BC,len-1
    LD  (HL),0
    LDIR
ENDM

ZFILL 0xF000, 0x400