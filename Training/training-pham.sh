#!/bin/bash

OUT="$1"
DOMAINNAME="$3"
DOMAIN="$4"
PROBLEMS="${@:5}"

../../../../Training/FocusedMetaActions.Train/bin/Release/net8.0/FocusedMetaActions.Train --domain ${DOMAIN} --problems ${PROBLEMS} --generator Manual --args metaPath\;../../../../Dependencies/focused-meta-actions-benchmarks/Pham-Domains/${DOMAINNAME}/ --stackelberg-path ../../../../Dependencies/modified-stackelberg-planner/src/fast-downward.py --fast-downward-path ../../../../Dependencies/fast-downward/fast-downward.py --validation-time-limit 120 --exploration-time-limit 240 --refinement-time-limit 480 --cache-generation-time-limit 240 --usefulness-time-limit 60 --skip-refinement
