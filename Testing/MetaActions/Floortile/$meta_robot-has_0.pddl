(:action $meta_robot-has_0
:parameters (?r - robot ?c1_1 - color ?c - color)
:precondition 
(and (robot-has ?r ?c1_1))
:effect 
(and
(robot-has ?r ?c)
(not (robot-has ?r ?c1_1))
)
)
