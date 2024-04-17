library(dplyr) 

source("Tools/style.R")
source("Tools/tables.R")
source("Tools/scatterPlots.R")

# Handle arguments
args = commandArgs(trailingOnly=TRUE)
#args[1] <- "general.csv"
#args[2] <- "CPDDLInvariantMetaActions"
if (length(args) != 2) {
  stop("2 arguments must be supplied! The source data file and the method to generate tables for", call.=FALSE)
}

data <- read.csv(
  args[1], 
  header = T, 
  sep = ",", 
  colClasses = c(
    'character',
    'character','numeric',
    'numeric','numeric',
    'numeric','numeric',
    'numeric','numeric'
  )
)

data <- data[data$id == args[2],]

tableData <- data %>% select(
  contains('domain'), 
  contains('total.candidates'), 
  contains('pre.not.useful.removed'), 
  contains('total.refined'), 
  contains('post.not.useful.removed'))
tableData <- aggregate(. ~ domain, data=tableData, FUN=sum)
generate_table(
  tableData,
  paste("out/usefulness.tex", sep = ""),
  c(
    "$Domain$",
    "$C$",
    "$PreRemoved$",
    "$C_{valid}$",
    "$PostRemoved$"
  ),
  "Usefulness pruning information. $C$ is the initial candidate meta actions. $PreRemoved$ is the candidates removed by the pre-usefulness check. $C_{valid}$ is the valid refinements found in the end.. $PostRemoved$ is the candidates removed by the post-usefulness check.",
  "tab:usefulness"
)