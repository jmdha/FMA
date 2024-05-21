echo "== General Graphs =="
Rscript general_graphs.R general.csv CPDDLMutexed+ReducesPlanLengthTop10+ReducesPlanLengthTop2
echo "== Testing Graphs =="
Rscript testing_graphs.R solve.csv S_CPDDL LAMA_FIRST
echo "== Coverage Graphs =="
Rscript coverage_graphs.R solve.csv LAMA_FIRST S_CPDDL