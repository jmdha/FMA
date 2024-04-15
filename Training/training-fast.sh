#!/bin/bash

OUT="$1"
GENERATION_STRATEGY="$2"
USEFULNESS_STRATEGY_POST="$3"
DOMAIN="$4"
PROBLEMS="${@:5}"

../../../../Training/P10/bin/Release/net8.0/P10 --domain ${DOMAIN} --problems ${PROBLEMS} --generation-strategies ${GENERATION_STRATEGY} --stackelberg-path ../../../../Dependencies/stackelberg-planner/src/fast-downward.py --fast-downward-path ../../../../Dependencies/fast-downward/fast-downward.py --cpddl-path ../../../../Dependencies/cpddl/bin/pddl --learning-cache-path /tmp/P10/.cache --pre-usefulness-strategy UsedInPlans --post-usefulness-strategy ${USEFULNESS_STRATEGY_POST}
