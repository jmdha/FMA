#!/bin/bash

OUT="$1"
ALIAS="$2"
DOMAIN="$3"
PROBLEM="$4"

../../../../Dependencies/fast-downward/fast-downward.py --plan-file ${OUT} --alias ${ALIAS} ${DOMAIN} ${PROBLEM}