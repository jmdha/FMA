(:action $meta_clear_2
:parameters (?c1_1 - object ?x - object)
:precondition 
(and
(on ?c1_1 ?x)
(not (on-table ?c1_1))
)
:effect 
(and
(clear ?x)
(not (on ?c1_1 ?x))
(on-table ?c1_1)
)
)
