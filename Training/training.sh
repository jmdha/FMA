#!/bin/bash

OUT="$1"
GENERATION_STRATEGY="$2"
DOMAIN="$3"
PROBLEMS="$4"

../../Training/P10/bin/Release/net8.0/P10 --domain ${DOMAIN} --problems ${PROBLEMS} --generation-strategies ${GENERATION_STRATEGY} --stackelberg-path ../../Dependencies/stackelberg-planner/src/fast-downward.py --fast-downward-path ../../Dependencies/fast-downward/fast-downward.py