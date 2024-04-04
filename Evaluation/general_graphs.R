library(dplyr) 

source("Tools/style.R")
source("Tools/tables.R")
source("Tools/scatterPlots.R")

# Handle arguments
args = commandArgs(trailingOnly=TRUE)
args[1] <- "general.csv"
if (length(args) != 1) {
  stop("1 arguments must be supplied! The source data file", call.=FALSE)
}

data <- read.csv(
  args[1], 
  header = T, 
  sep = ",", 
  colClasses = c(
    'character','numeric',
    'numeric','numeric',
    'numeric','numeric',
    'numeric','numeric'
  )
)

tableData <- data %>% select(
  contains('domain'), 
  contains('total.candidates'), 
  contains('total.refined'))
tableData <- aggregate(. ~ domain, data=tableData, FUN=sum)
generate_table(
  tableData,
  paste("out/general.tex", sep = ""),
  10,
  10,
  c(
    "$Domain$",
    "$C$",
    "$C_{valid}$"
  ),
  "General Process Info. $C$ is the initial meta action candidates. $C_{valid}$ is the total valid meta actions either initially valid or refined.",
  "tab:general"
)