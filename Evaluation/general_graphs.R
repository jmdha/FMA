library(dplyr, warn.conflicts = FALSE) 

source("Tools/style.R")
source("Tools/tables.R")
source("Tools/scatterPlots.R")
source("Tools/graphNames.R")
source("Tools/parsers.R")

# Handle arguments
args = commandArgs(trailingOnly=TRUE)
#args[1] <- "general.csv"
#args[2] <- "mutexgroups.csv"
#args[3] <- "CPDDLMutexed+ReducesPlanLengthTop10+ReducesPlanLengthTop2"
if (length(args) != 3) {
  stop("3 arguments must be supplied!", call.=FALSE)
}

data <- parse_general(args[1])
data <- data[data$id == args[3],]
groups <- parse_mutex_groups(args[2])

data <- merge(data, groups)

data$Pre.Not.Useful_ <- data$Total.Candidates - data$Pre.Not.Useful.Removed
data$Total.Refined <- data$Total.Refined - data$Post.Duplicates.Removed
data$Post.Not.Useful_ <- data$Total.Refined - data$Post.Not.Useful.Removed

dir.create(file.path("Out"), showWarnings = FALSE)

tableData <- data %>% select(
  contains('domain'), 
  contains('mutex.groups'), 
  contains('total.candidates'), 
  contains('pre.not.useful_'), 
  contains('total.refined'), 
  contains('post.not.useful_'))
tableData <- aggregate(. ~ domain, data=tableData, FUN=sum)
generate_table(
  tableData,
  paste("out/usefulness.tex", sep = ""),
  c(
    "$Domain$",
	"$G$",
    "$C$",
    "$C_{pre}$",
    "$M_{valid}$",
    "$M_{post}$"
  ),
  "\\textit{Usefulness pruning information. $G$ is the amount of mutex groups for each domain. $C$ is the initial candidate meta actions. $C_{pre}$ is the candidates after the pre-usefulness check. $M_{valid}$ is the valid refined meta actions. $M_{post}$ is the valid meta actions after the post-usefulness check.}",
  "tab:usefulness"
)