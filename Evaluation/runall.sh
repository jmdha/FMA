echo "== General Graphs =="
Rscript general_graphs.R general.csv CPDDLMutexed
echo "== Refinement Graphs =="
Rscript refinement_graphs.R refinement.csv CPDDLMutexed
echo "== Testing Graphs =="
Rscript testing_graphs.R solve.csv S_CPDDL LAMA_FIRST