(:action $meta_at_0
:parameters (?obj - truck ?c0_1 - location ?loc - location)
:precondition 
(and
(at ?obj ?c0_1)
(empty ?obj)
(link ?loc ?c0_1)
)
:effect 
(and
(at ?obj ?loc)
(not (at ?obj ?c0_1))
)
)
