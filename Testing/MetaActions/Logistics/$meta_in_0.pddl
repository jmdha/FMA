(:action $meta_in_0
:parameters (?obj1 - object ?c0_1 - object ?obj2 - object)
:precondition 
(and
(at ?obj1 ?c0_1)
(obj ?obj1)
(truck ?obj2)
)
:effect 
(and
(in ?obj1 ?obj2)
(not (at ?obj1 ?c0_1))
)
)
