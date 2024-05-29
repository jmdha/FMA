(:action $meta_walked_0
:parameters (?x1 - couple ?c3_1 - place ?x2 - place)
:precondition 
(and
(walked ?x1 ?c3_1)
(walked ?x1 ?c3_1)
(next ?c3_1 ?x2)
)
:effect 
(and
(walked ?x1 ?x2)
(not (walked ?x1 ?c3_1))
)
)
