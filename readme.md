# P10

## Setup
First, you have to run the `setup.sh` script from within the `Dependencies` folder.
Then, from the root of the project, write `dotnet build --configuration Release` to build all the .NET parts.
You can install the experiments tool (labyr) by writing `cargo install labyr`.
Now you can run `dotnet run` on the different projects in the solution.

## Experiments

To run experiments (Requires rust package `labyr`):

```bash
labyr run-experiments.toml --working-dir .
```
