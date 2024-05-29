(:action $meta_handempty_0
:parameters (?h - hand ?c2_1 - container)
:precondition 
(and
(holding ?h ?c2_1)
(not (ontable ?c2_1))
)
:effect 
(and
(handempty ?h)
(not (holding ?h ?c2_1))
(ontable ?c2_1)
)
)
