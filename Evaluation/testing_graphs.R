library(dplyr) 

source("Tools/style.R")
source("Tools/scatterPlots.R")
source("Tools/graphNames.R")
source("Tools/clamper.R")

# Handle arguments
args = commandArgs(trailingOnly=TRUE)
args[1] <- "solve.csv"
args[2] <- "StrippedMeta"
args[3] <- "Downward"
if (length(args) != 3) {
  stop("3 arguments must be supplied! The source data file, and one for each target reconstruction type", call.=FALSE)
}
AName <- recon_names(args[2])
BName <- recon_names(args[3])

data <- read.csv(
  args[1], 
  header = T, 
  sep = ",", 
  colClasses = c(
    'character','character','character',
    'numeric','numeric',
    'numeric'
  )
)
data <- rename_data(data)
if (nrow(data[data$name == AName,]) == 0)
  stop(paste("Column name '", args[2], "' not found in dataset!"), call.=FALSE)
if (nrow(data[data$name == BName,]) == 0)
  stop(paste("Column name '", args[3], "' not found in dataset!"), call.=FALSE)
data <- max_unsolved(data, "total_time")
data <- max_unsolved(data, "search_time")

AData = data[data$name == AName,]
AData$problem <- sub('[.]', '_', make.names(AData$problem, unique=TRUE))
BData = data[data$name == BName,]
BData$problem <- sub('[.]', '_', make.names(BData$problem, unique=TRUE))
combined <- merge(AData, BData, by = c("domain", "problem"), suffixes=c(".A", ".B"))
combined <- combined %>% select(-contains('name.A'))
combined <- combined %>% select(-contains('name.B'))

dir.create(file.path("out"), showWarnings = FALSE)

print("Generating: Search Scatterplot")
sideA <- combined$search_time.A
sideB <- combined$search_time.B
sideDomains <- combined$domain
searchData <- data.frame(x = sideA, y = sideB, domain = sideDomains)
generate_scatterplot(searchData, AName, BName, "Search Time (s)", paste("out/searchTime_", AName, "_vs_", BName, ".pdf", sep = ""))

print("Generating: Total Scatterplot")
sideA <- combined$total_time.A
sideB <- combined$total_time.B
sideDomains <- combined$domain
searchData <- data.frame(x = sideA, y = sideB, domain = sideDomains)
generate_scatterplot(searchData, AName, BName, "Total Time (s)", paste("out/totalTime_", AName, "_vs_", BName, ".pdf", sep = ""))