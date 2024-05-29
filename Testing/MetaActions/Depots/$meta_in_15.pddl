(:action $meta_in_15
:parameters (?c3_3 - hoist ?x - crate ?c4_3 - hoist ?c5_3 - hoist ?y - truck)
:precondition 
(and
(lifting ?c3_3 ?x)
(lifting ?c4_3 ?x)
(lifting ?c5_3 ?x)
(not (available ?c4_3))
(not (available ?c5_3))
)
:effect 
(and
(in ?x ?y)
(not (lifting ?c3_3 ?x))
(not (lifting ?c4_3 ?x))
(not (lifting ?c5_3 ?x))
(available ?c4_3)
(available ?c5_3)
)
)
