echo "== General Graphs =="
Rscript general_graphs.R Results/general.csv Results/mutexgroups.csv CPDDLMutexed+ReducesPlanLengthTop10+ReducesPlanLengthTop2
echo "== Testing Graphs =="
Rscript testing_graphs.R Results/solve.csv Results/general.csv CPDDLMutexed+ReducesPlanLengthTop10+ReducesPlanLengthTop2 S_CPDDL LAMA_FIRST
echo "== Coverage Graphs =="
Rscript coverage_graphs.R Results/solve.csv LAMA_FIRST S_CPDDL 