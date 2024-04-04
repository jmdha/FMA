#!/bin/bash

OUT="$1"
ALIAS="$2"
LEARNER_DIRECTORY="$3"
DOMAIN="$4"
PROBLEM="$5"

../../../../Dependencies/fast-downward/fast-downward.py --plan-file ${OUT} --alias ${ALIAS} ${LEARNER_DIRECTORY}/output/enhancedDomain.pddl ${PROBLEM}
