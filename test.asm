COORD STRUCT
	XCoord		DB ?
	YCoord		DB ?
	YSubCoord	DB ?
ENDS

BOT STRUCT 
	Value		WORD ?
	Coord		COORD ?
	Value2		WORD ?
ENDS

	;BOT		[0, [0,0,0], 0]
	;BOT		4 DUP ?
	;BOT     4 DUP 0

	ld		a,(IX+BOT.COORD)
	ld		a,(IX+BOT.COORD.XCoord)
	ld		a,(IX+BOT.COORD.YCoord)
	ld		a,(IX+BOT.COORD.YSubCoord)
	ld		a,(IX+BOT.Value)
	ld		a,(IX+BOT.Value+1)
	ld		a,(IX+BOT.Value2)
	ld		de,BOT.Value2

	ld		a,sizeof(BOT)



	; TODO
	; - BYTE/WORD/DB/DW etc...
	; - sizeof (Type/Member)
	; - data declarations 
	; - support for DUP