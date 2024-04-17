echo "== General Graphs =="
Rscript general_graphs.R general.csv CPDDLInvariantMetaActions
echo "== Refinement Graphs =="
Rscript refinement_graphs.R refinement.csv CPDDLInvariantMetaActions
echo "== Testing Graphs =="
Rscript testing_graphs.R solve.csv S_CPDDL Downward