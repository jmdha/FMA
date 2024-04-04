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
  contains('valid.refinements'),
  contains('succeded'))
names(tableData)[names(tableData) == "succeded"] <- "unrefinable"
tableData$unrefinable[tableData$unrefinable == "True"] <- 0
tableData$unrefinable[tableData$unrefinable == "False"] <- 1
tableData$unrefinable = as.numeric(as.character(tableData$unrefinable))
tableData <- aggregate(. ~ domain, data=tableData, FUN=sum)
generate_table(
  tableData,
  paste("out/refinement.tex", sep = ""),
  10,
  10,
  c(
    "$Domain$",
    "$R$",
    "$R_{valid}$",
    "$Unrefinable$"
  ),
  "Refinement Process Info. $R$ is the initial refinement options for all the candidates for a domain. $R_{valid}$ is the valid refinement options.",
  "tab:refinement"
)