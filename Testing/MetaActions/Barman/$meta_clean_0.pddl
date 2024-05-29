(:action $meta_clean_0
:parameters (?c - shot ?c4_1 - beverage)
:precondition 
(and
(used ?c ?c4_1)
(not (contains ?c ?c4_1))
)
:effect 
(and
(clean ?c)
(not (used ?c ?c4_1))
)
)
