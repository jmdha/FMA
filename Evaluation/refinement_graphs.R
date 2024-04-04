
source("Tools/style.R")

# Handle arguments
args = commandArgs(trailingOnly=TRUE)
args[1] <- "refinement.csv"
if (length(args) != 1) {
  stop("1 arguments must be supplied! The source data file", call.=FALSE)
}

data <- read.csv(
  args[1], 
  header = T, 
  sep = ",", 
  colClasses = c(
    'character','character',
    'numeric','numeric',
    'character','character',
    'numeric','numeric',
    'numeric','numeric'
  )
)
