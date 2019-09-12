		pop     hl
        ld      a,$20           ; ' '
        rst     0x10
L00C1:  ex      de,hl
        pop     hl
        inc     hl
