(:action $meta_at_0
:parameters (?obj - object ?c0_2 - object ?loc - object)
:precondition 
(and
(in ?obj ?c0_2)
(location ?loc)
)
:effect 
(and
(at ?obj ?loc)
(not (in ?obj ?c0_2))
)
)
