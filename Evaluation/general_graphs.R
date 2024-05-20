library(dplyr) 

source("Tools/style.R")
source("Tools/tables.R")
source("Tools/scatterPlots.R")
source("Tools/graphNames.R")

# Handle arguments
args = commandArgs(trailingOnly=TRUE)
#args[1] <- "general.csv"
#args[2] <- "CPDDLMutexed+UsedInPlans+ReducesMetaSearchTimeTop2"
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
    'numeric'
  )
)

data <- rename_domains(data)

data <- data[data$id == args[2],]

data$Total.Refined <- data$Total.Refined - data$Post.Duplicates.Removed

data$Final.Output <- data$Total.Refined - data$Post.Not.Useful.Removed

tableData <- data %>% select(
  contains('domain'), 
  contains('total.candidates'), 
  contains('pre.not.useful.removed'), 
  contains('total.refined'), 
  contains('post.not.useful.removed'),
  contains('final.output'))
tableData <- aggregate(. ~ domain, data=tableData, FUN=sum)
generate_table(
  tableData,
  paste("out/usefulness.tex", sep = ""),
  c(
    "$Domain$",
    "$C$",
    "$U_{pre}$",
    "$C_{valid}$",
    "$U_{post}$",
    "$M$"
  ),
  "\\textit{Usefulness pruning information. $C$ is the initial candidate meta actions. $U_{pre}$ is the candidates removed by the pre-usefulness check. $C_{valid}$ is the valid refinements found. $U_{post}$ is the candidates removed by the post-usefulness check. $M$ is the final number of meta actions.}",
  "tab:usefulness"
)