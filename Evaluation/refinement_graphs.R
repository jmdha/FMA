library(dplyr) 

source("Tools/style.R")
source("Tools/tables.R")
source("Tools/scatterPlots.R")
source("Tools/graphNames.R")

# Handle arguments
args = commandArgs(trailingOnly=TRUE)
#args[1] <- "refinement.csv"
#args[2] <- "CPDDLMutexed"
if (length(args) != 2) {
  stop("2 arguments must be supplied! The source data file and the method to generate tables for", call.=FALSE)
}

data <- read.csv(
  args[1], 
  header = T, 
  sep = ",", 
  colClasses = c(
    'character','character','character',
    'numeric','numeric',
    'character','character',
    'numeric','numeric',
    'numeric','numeric'
  )
)

data <- rename_domains(data)
data <- data[data$id == args[2],]

tableData <- data %>% select(
  contains('domain'), 
  contains('final.refinement.possibilities'), 
  contains('valid.refinements'),
  contains('succeded'))
names(tableData)[names(tableData) == "succeded"] <- "unrefinable"
tableData$unrefinable[tableData$unrefinable == "True"] <- 0
tableData$unrefinable[tableData$unrefinable == "False"] <- 1
tableData$unrefinable <- as.numeric(as.character(tableData$unrefinable))
tableData <- aggregate(. ~ domain, data=tableData, FUN=sum)

alreadyValid <- data %>% select(
  contains('domain'),
  contains('already.valid')
)
alreadyValid$already.valid[alreadyValid$already.valid == "True"] <- 1
alreadyValid$already.valid[alreadyValid$already.valid == "False"] <- 0
alreadyValid$already.valid <- as.numeric(as.character(alreadyValid$already.valid))
alreadyValid <- aggregate(. ~ domain, data=alreadyValid, FUN=sum)

for(domain in unique(tableData$domain)){
  tableData$valid.refinements[tableData$domain == domain] <- tableData$valid.refinements[tableData$domain == domain] - alreadyValid$already.valid[alreadyValid$domain == domain]
}
generate_table(
  tableData,
  paste("out/refinement.tex", sep = ""),
  c(
    "$Domain$",
    "$R$",
    "$R_{valid}$",
    "$Unrefinable$"
  ),
  "\\textit{Refinement Process Info. $R$ is the initial refinement options for all the candidates for a domain. $R_{valid}$ is the valid refinement options. Unrefinables are potential failures.}",
  "tab:refinement"
)