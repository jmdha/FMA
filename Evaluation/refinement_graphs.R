library(dplyr) 

source("Tools/style.R")
source("Tools/tables.R")
source("Tools/scatterPlots.R")

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

tableData <- data %>% select(
  contains('domain'), 
  contains('final.refinement.possibilities'), 
  contains('valid.refinements'))
tableData <- aggregate(. ~ domain, data=tableData, FUN=sum)
generate_table(
  tableData,
  paste("out/test.text", sep = ""),
  10,
  10,
  c(
    "$Domain$",
    "$R$",
    "$R_{valid}$"
  ),
  "something",
  "lab"
)