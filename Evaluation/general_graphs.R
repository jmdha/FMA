library(dplyr) 

source("Tools/style.R")
source("Tools/tables.R")
source("Tools/scatterPlots.R")
source("Tools/graphNames.R")

# Handle arguments
args = commandArgs(trailingOnly=TRUE)
#args[1] <- "general.csv"
#args[2] <- "CPDDLMutexed+ReducesPlanLengthTop10+ReducesPlanLengthTop2"
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

data <- data[data$id == args[2],]

# Do note, these are just hardcoded mutex group numbers in here, since they never change.
namevector <- c("Mutex.Groups")
data[ , namevector] <- NA
data$Mutex.Groups[data$domain == "barman"] <- 9
data$Mutex.Groups[data$domain == "blocksworld"] <- 3
data$Mutex.Groups[data$domain == "child-snack"] <- 1
data$Mutex.Groups[data$domain == "depots"] <- 6
data$Mutex.Groups[data$domain == "driverlog"] <- 4
data$Mutex.Groups[data$domain == "floor-tile"] <- 3
data$Mutex.Groups[data$domain == "grid"] <- 4
data$Mutex.Groups[data$domain == "gripper-strips"] <- 3
data$Mutex.Groups[data$domain == "hiking"] <- 5
data$Mutex.Groups[data$domain == "logistics-strips"] <- 1
data$Mutex.Groups[data$domain == "miconic"] <- 1
data$Mutex.Groups[data$domain == "parking"] <- 3
data$Mutex.Groups[data$domain == "rover"] <- 4
data$Mutex.Groups[data$domain == "satellite"] <- 1
data$Mutex.Groups[data$domain == "scanalyzer3d"] <- 2
data$Mutex.Groups[data$domain == "woodworking"] <- 7

data <- rename_domains(data)

data$Pre.Not.Useful_ <- data$Total.Candidates - data$Pre.Not.Useful.Removed
data$Total.Refined <- data$Total.Refined - data$Post.Duplicates.Removed
data$Post.Not.Useful_ <- data$Total.Refined - data$Post.Not.Useful.Removed

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