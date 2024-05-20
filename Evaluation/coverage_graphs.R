library(dplyr) 

source("Tools/style.R")
source("Tools/tables.R")
source("Tools/graphNames.R")

# Handle arguments
args = commandArgs(trailingOnly=TRUE)
#args[1] <- "solve.csv"
#args[2] <- "S_CPDDL"
#args[3] <- "LAMA_FIRST"
if (length(args) < 1) {
  stop("At least 1 argument must be supplied! The source data file, and one for each target reconstruction type", call.=FALSE)
}

targets <- args[2:length(args)]
for(target in targets)
  targets[targets == target] <- recon_names(target)

data <- read.csv(
  args[1], 
  header = T, 
  sep = ",", 
  colClasses = c(
    'character','character','character',
    'numeric','numeric', 'numeric',
    'numeric','numeric'
  )
)
data <- rename_data(data)

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

generate_table(
  result,
  paste("out/coverage.tex", sep = ""),
  colnames(result),
  "\\textit{Coverage of how many problems each method was able to solve within the time limit.}",
  "tab:coverage"
)
