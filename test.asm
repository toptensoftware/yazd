	org 0x400

	include "inc.asm"

if 10 < 30

	jp blah

elseif 

	jp blah2

endif


label:
	db 23,24
	db "Hello world"

	ds 10,7

	djnz	$

	ld a, 0
	jr z,label

	incbin "test.asm"
