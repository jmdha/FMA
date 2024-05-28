library(dplyr, warn.conflicts = FALSE) 

source("Tools/style.R")
source("Tools/tables.R")
source("Tools/graphNames.R")
source("Tools/parsers.R")

# Handle arguments
args = commandArgs(trailingOnly=TRUE)
#args[1] <- "solve.csv"
#args[2] <- "S_CPDDL"
#args[3] <- "LAMA_FIRST"
if (length(args) < 2) {
  stop("At least 2 arguments must be supplied!", call.=FALSE)
}

targets <- args[2:length(args)]
for(target in targets)
  targets[targets == target] <- recon_names(target)

data <- parse_solve(args[1])

solved <- data[data$exit_code == 0,]

result <- data.frame(Domain=character(), P=integer())
for(target in targets)
  result[target] <- integer()

for(domain in unique(data$domain))
{
  newRow <- c(domain,length(unique(data[data$domain == domain,]$problem)))
  
  for(target in targets){
    this <- solved[solved$name == target,]
    this <- this[this$domain == domain,]
    newRow <- c(newRow, length(this$problem))
  }
  
  result[nrow(result) + 1,] = newRow
}

totalRow <- list("Total")
for(i in 2:ncol(result))
	totalRow <- append(totalRow, sum(sapply(result[i], as.integer)))
result[nrow(result) + 1,] <- totalRow 

dir.create(file.path("Out"), showWarnings = FALSE)

generate_table(
  result,
  paste("out/coverage.tex", sep = ""),
  colnames(result),
  "\\textit{Coverage of how many problems each method was able to solve within the time limit. P is the amount of problems for the given domain. Do note, this is without reconstruction but purely based on being able to find a plan.}",
  "tab:coverage"
)
