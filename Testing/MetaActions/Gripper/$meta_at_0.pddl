(:action $meta_at_0
:parameters (?b - object ?c1_2 - object ?r - object)
:precondition 
(and
(carry ?b ?c1_2)
(not (free ?c1_2))
(room ?r)
)
:effect 
(and
(at ?b ?r)
(not (carry ?b ?c1_2))
(free ?c1_2)
)
)
