	org 0x400

	include "inc.asm"

label:
	db 23,24
	db "Hello world"

	djnz	$

	ld a, 0
	jr z,label

	incbin "test.asm"
