(:action $meta_in_0
:parameters (?obj1 - obj ?c1_1 - location ?obj - truck)
:precondition 
(and (at ?obj1 ?c1_1))
:effect 
(and
(in ?obj1 ?obj)
(not (at ?obj1 ?c1_1))
)
)
