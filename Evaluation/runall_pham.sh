echo "== Testing Graphs =="
Rscript testing_graphs.R solve_pham.csv S_PHAM LAMA_FIRST
echo "== Coverage Graphs =="
Rscript coverage_graphs.R solve_pham.csv LAMA_FIRST S_PHAM