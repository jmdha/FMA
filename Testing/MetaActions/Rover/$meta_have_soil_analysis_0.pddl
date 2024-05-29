(:action $meta_have_soil_analysis_0
:parameters (?w - waypoint ?r - rover)
:precondition 
(and
(at_soil_sample ?w)
(equipped_for_soil_analysis ?r)
)
:effect 
(and
(have_soil_analysis ?r ?w)
(not (at_soil_sample ?w))
)
)
