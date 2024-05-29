(:action $meta_at_tent_0
:parameters (?x1 - tent ?c0_1 - place ?x2 - place)
:precondition 
(and
(at_tent ?x1 ?c0_1)
(at_tent ?x1 ?c0_1)
(next ?c0_1 ?x2)
)
:effect 
(and
(at_tent ?x1 ?x2)
(not (at_tent ?x1 ?c0_1))
)
)
