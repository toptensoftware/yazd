	org 0x400

vram: equ 0x1234 ; zx
entry:
	call exit1
	ld a,(ix+0) ; Stuff pp


	exit: