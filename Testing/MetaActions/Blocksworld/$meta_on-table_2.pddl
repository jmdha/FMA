(:action $meta_on-table_2
:parameters (?x - object ?c2_1 - object)
:precondition 
(and
(on ?x ?c2_1)
(not (clear ?c2_1))
)
:effect 
(and
(on-table ?x)
(not (on ?x ?c2_1))
(clear ?c2_1)
)
)
