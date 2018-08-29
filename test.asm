HIGHSCORE STRUCT
	NAME	DB	20 dup ?
	SCORE	DW  ?
ENDS



ALIEN   STRUCT
    POS     COORD   ?
    MODE    DB      ?
ENDS

COORD   STRUCT
    X   DB  ?
    Y   DB  ?
ENDS


PLAYER  STRUCT
    POS     COORD   ?
    FRAME   DB      ?
ENDS

	HIGHSCORE	[ 99, 1 ]

	DB  5*3
	DW	10
	DW  "Woah!",0
	DB	3 DUP ('Hello', 0)

	DB		?
	COORD	?

	ALIEN	?

	ALIEN { 
		MODE: 1, 
		POS: [3,9]
	}
