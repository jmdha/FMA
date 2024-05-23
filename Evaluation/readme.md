# Evaluation

This folder contains R projects to generate graphs of the training and testing results.
There are a few different scripts that can be run to generate graphs, where the primary difference is that one set of scripts generate graphs for basic results while the others generate for results with reconstruction.

All the scripts will output graphs and tables in a folder called `Out` that is gitignored.

The main scripts are in this folder and is all sufixed with a `_graphs.R` name.

They are all made to be run in the terminal with a given set of arguments.
As an example, to generate the coverage table from the report, you would call:
```bash
Rscript coverage_graphs.R Results/solve.csv LAMA_FIRST S_CPDDL 
```
Where you ofcourse have to unzip one of the result zip files in the `Results` folder for it to work.

If you where to add other new methods or domains, you can make their name looks nicer by editing the [Graph Names](./Tools/graphNames.R) file.
