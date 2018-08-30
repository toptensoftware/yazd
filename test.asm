bot_bullet_max  equ     2

bot_bullet struct
        x       byte    ?
        :       byte    bot_bullet_max-1 DUP ?
        y       byte    ?
        :       byte    bot_bullet_max-1 DUP ?
        y2      byte    ?
        :       byte    bot_bullet_max-1 DUP ?
        dir     byte    ?
        :       byte    bot_bullet_max-1 DUP ?
ends

player_bullet_max      EQU 2

player_bullet struct
        x       byte ?
        :       player_bullet_max-1 dup ?
        y       byte ?
        :       player_bullet_max-1 dup ?
        y2      byte ?
        :       player_bullet_max-1 dup ?
        dir     byte ?
        :       player_bullet_max-1 dup ?
ends

