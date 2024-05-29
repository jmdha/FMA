(:action $meta_at-robot_0
:parameters (?c0_0 - object ?x - object)
:precondition 
(and
(at-robot ?c0_0)
(open ?x)
)
:effect 
(and
(at-robot ?x)
(not (at-robot ?c0_0))
)
)
