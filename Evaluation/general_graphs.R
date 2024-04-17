library(dplyr) 

source("Tools/style.R")
source("Tools/tables.R")
source("Tools/scatterPlots.R")

# Handle arguments
args = commandArgs(trailingOnly=TRUE)
#args[1] <- "general.csv"
if (length(args) != 1) {
  stop("1 arguments must be supplied! The source data file", call.=FALSE)
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