# P10

## Setup
First, you have to run the `setup.sh` script from within the `Dependencies` folder.
Then, from the root of the project, write `dotnet build --configuration Release` to build all the .NET parts.
You can install the experiments tool (labyr) by writing `cargo install labyr`.
Now can also run `dotnet run` on the different projects in the solution.

## Experiments

To run experiments (Requires rust package `labyr`), execute the following commands from the project root folder:

```bash
dotnet build --configuration Release
labyr run-experiments-full.toml --work-dir Results --threads 0 --keep-working-dir
```
