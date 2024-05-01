#!/bin/bash

OUT="$1"
GENERATION_STRATEGY="$2"
DOMAINNAME="$3"
DOMAIN="$4"
PROBLEMS="${@:5}"

../../../../Training/P10/bin/Release/net8.0/P10 --domain ${DOMAIN} --problems ${PROBLEMS} --generator ${GENERATION_STRATEGY} --args cpddlOutput\;../../../../Dependencies/p10-benchmarks/CPDDLGroups/${DOMAINNAME}.txt --stackelberg-path ../../../../Dependencies/stackelberg-planner/src/fast-downward.py --modified-stackelberg-path ../../../../Dependencies/modified-stackelberg-planner/src/fast-downward.py --fast-downward-path ../../../../Dependencies/fast-downward/fast-downward.py --validation-time-limit 120 --exploration-time-limit 120 --refinement-time-limit 240 --pre-usefulness-strategy UsedInPlans
