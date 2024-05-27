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

## Arguments
As mentioned, all the R scripts require a set of arguments, all of which are listed here for each script:

### General Table
This generates a table of the learning part, with the initial candidates, how many was removed by pre usefulness check and so on:
```bash
Rscript general_graphs.R Results/general.csv Results/mutexgroups.csv CPDDLMutexed+UsedInPlans+ReducesPlanLengthTop2
```

### Coverage Table
This generates a coverage table, between two of the methods in the solve csv:
```bash
Rscript coverage_graphs.R Results/solve.csv LAMA_FIRST CPDDL 
```

### Solve Graphs (Non-reconstruction)
This generates all the scatter plots to compare two methods in the solve csv.
```bash
Rscript testing_graphs.R Results/solve.csv CPDDL LAMA_FIRST
```

### Solve Graphs (Reconstruction)
This generates all the scatter plots to compare two methods in the solve csv with reconstruction.
```bash
Rscript testing_reconstruction_graphs.R Results/solve.csv CPDDL PHAM
```