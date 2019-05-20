KB_BIT(chk_bit) MACRO
	LD	E, 0 + (chk_bit << 3)		; Bit code
	BIT	chk_bit, B
	JR	Z, send
	SET	6, E
send:
	Nop
	Nop
ENDM

KB_BIT 1
KB_BIT 2