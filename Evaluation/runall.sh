echo "== General Graphs =="
Rscript general_graphs.R general.csv
echo "== Refinement Graphs =="
Rscript refinement_graphs.R refinement.csv
echo "== Testing Graphs =="
Rscript testing_graphs.R solve.csv