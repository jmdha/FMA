(:action $meta_free_0
:parameters (?c2_1 - object ?g - object ?c1_1 - object)
:precondition 
(and
(carry ?c2_1 ?g)
(not (at ?c2_1 ?c1_1))
(room ?c1_1)
)
:effect 
(and
(free ?g)
(not (carry ?c2_1 ?g))
(at ?c2_1 ?c1_1)
)
)
