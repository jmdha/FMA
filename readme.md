# Focused Meta Actions
This is the code repository of the master thesis `Focused Meta Action` by Kristian Skov Johansen and Jan M.D. Hansen.
This readme will serve as a way to navigate and understand the different sections of the project.
Generally, this project is split into a few couple of repositories, where the most important ones are:
* This one
    * The `Training` folder contains file and project needed for training.
    * The `Testing` folder contains file and project needed for testing.
    * The `Evaluation` folder contains all the automatic graph and table generation used in the paper.
* A [benchmark](https://github.com/kris701/FocusedMetaActionsData/tree/master) repository
* A repository for [labyr](https://github.com/jamadaha/labyr) (do note, this is not needed to run the project, its only used to run all the experiments from the paper)
* A repository for a [modified Stackelberg Planner](https://github.com/jamadaha/stackelberg-planner-sls) for state exploration and macro generation
* A repository containing the main [reconstruction method](https://github.com/kris701/MARMA) (Do note, this is from a previous semester project and is not 100p compatible with the formats of this project)

All the C# side uses a lot of NuGet packages made for this project. NuGet should be able to install them themselfs, but to be on the safe side, here is the list of packages:
* [PDDLSharp](https://github.com/kris701/PDDLSharp) to manipulate and parse PDDL files
* [CSVToolsSharp](https://github.com/kris701/CSVToolsSharp) to output CSV files
* [MetaActionGenerators](https://github.com/kris701/MetaActionGenerators) to generate initial meta actions
* [Stackelberg.MetaAction.Compiler](https://github.com/kris701/Stackelberg.MetaAction.Compiler) to compile domain+problem+meta action into a Stackelberg variant.

Do note, that this entire toolchain was made to work on linux, you may be able to use it on windows (if you can get the Stackelberg Planner and Fast Downward to compile) if you need to.
Otherwise, we recommend windows users to set up a [WSL](https://learn.microsoft.com/en-us/windows/wsl/install) instance to run it on.

## Setup
Firstly to run this entire toolchain, do `cd ./Dependencies` and execute the `setup.sh` script.
This *should* install all needed dependencies for the project.

When all the dependencies are installed, you can compile the training program by writing:
```bash
dotnet build --configuration Release --project Training/FocusedMetaActions.Train/FocusedMetaActions.Train.csproj
```
It should pop up with a bunch of different arguments you should give it before it can run.
Each argument have a little description that should help you.

### Examples for Training
Here are a few examples of arguments you could give to the training to run on (this is run from the `Training` folder):
```bash
dotnet run --configuration Release -- --output output --domain ../Dependencies/focused-meta-actions-benchmarks/Benchmarks/blocksworld/domain.pddl --problems ../Dependencies/focused-meta-actions-benchmarks/Benchmarks/blocksworld/training/p1.pddl ../Dependencies/focused-meta-actions-benchmarks/Benchmarks/blocksworld/training/p2.pddl ../Dependencies/focused-meta-actions-benchmarks/Benchmarks/blocksworld/training/p3.pddl ../Dependencies/focused-meta-actions-benchmarks/Benchmarks/blocksworld/training/p4.pddl ../Dependencies/focused-meta-actions-benchmarks/Benchmarks/blocksworld/training/p5.pddl ../Dependencies/focused-meta-actions-benchmarks/Benchmarks/blocksworld/usefulness/p1.pddl ../Dependencies/focused-meta-actions-benchmarks/Benchmarks/blocksworld/usefulness/p2.pddl ../Dependencies/focused-meta-actions-benchmarks/Benchmarks/blocksworld/usefulness/p3.pddl ../Dependencies/focused-meta-actions-benchmarks/Benchmarks/blocksworld/usefulness/p4.pddl ../Dependencies/focused-meta-actions-benchmarks/Benchmarks/blocksworld/usefulness/p5.pddl --generator CPDDLMutexed --args cpddlOutput;../Dependencies/focused-meta-actions-benchmarks/CPDDLGroups/blocksworld.txt --refinement-time-limit 120 --exploration-time-limit 120 --validation-time-limit 20 --cache-generation-time-limit 20 --pre-usefulness-strategy UsedInPlans --post-usefulness-strategy ReducesMetaSearchTimeTop2 --last-n-usefulness 5
```

### Examples for Testing
For testing, you can use the `labyr` package.
This is basically a small package made to run training+testing with and compare against Fast Downward.
This package uses script files to run. There are two script files in this repository, designed for the paper benchmarks.
You can run the script from the root of the project as follows:
```bash
dotnet build --configuration Release
labyr run-experiments-full.toml --work-dir Results --threads 0 --keep-working-dir
```
This will create a `results` folder in the end, containing the combined results of all the runs.

## How to run this on the Cluster
To be made...
