library(dplyr, warn.conflicts = FALSE) 

source("Tools/style.R")
source("Tools/scatterPlots.R")
source("Tools/graphNames.R")
source("Tools/clamper.R")
source("Tools/parsers.R")

# Handle arguments
args = commandArgs(trailingOnly=TRUE)
#args[1] <- "solve.csv"
#args[2] <- "general.csv"
#args[3] <- "CPDDLMutexed+ReducesPlanLengthTop10+ReducesPlanLengthTop2"
#args[4] <- "S_CPDDL"
#args[5] <- "LAMA_FIRST"
if (length(args) != 5) {
  stop("5 arguments must be supplied!", call.=FALSE)
}

generalData <- parse_general(args[2])
data <- parse_solve(args[1])

metaDomains <- generalData[generalData$Total.Refined - generalData$Post.Not.Useful.Removed > 0,]$domain
data <- data[data$domain %in% metaDomains,]

AName <- recon_names(args[4])
BName <- recon_names(args[5])

if (nrow(data[data$name == AName,]) == 0)
  stop(paste("Column name '", args[2], "' not found in dataset!"), call.=FALSE)
if (nrow(data[data$name == BName,]) == 0)
  stop(paste("Column name '", args[3], "' not found in dataset!"), call.=FALSE)
data <- max_unsolved(data, "total_time")
data <- max_unsolved(data, "search_time")
data <- max_unsolved(data, "plan_length")

AData = data[data$name == AName,]
BData = data[data$name == BName,]
combined <- merge(AData, BData, by = c("domain", "problem"), suffixes=c(".A", ".B"))
combined <- combined %>% select(-contains('name.A'))
combined <- combined %>% select(-contains('name.B'))

dir.create(file.path("Out"), showWarnings = FALSE)

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

#print("Generating: Solve Scatterplot")
#sideA <- combined$solution_time.A
#sideB <- combined$solution_time.B
#sideDomains <- combined$domain
#searchData <- data.frame(x = sideA, y = sideB, domain = sideDomains)
#generate_scatterplot(searchData, AName, BName, "Solution Time (s)", paste("out/solutionTime_", AName, "_vs_", BName, ".pdf", sep = ""))

#print("Generating: Meta Plan length Scatterplot")
#sideA <- combined$meta_plan_length.A
#sideB <- combined$plan_length.B
#sideDomains <- combined$domain
#searchData <- data.frame(x = sideA, y = sideB, domain = sideDomains)
#generate_scatterplot(searchData, AName, BName, "Meta Plan Length", paste("out/metaPlanLength_", AName, "_vs_", BName, ".pdf", sep = ""))

print("Generating: Final Plan length Scatterplot")
sideA <- combined$plan_length.A
sideB <- combined$plan_length.B
sideDomains <- combined$domain
searchData <- data.frame(x = sideA, y = sideB, domain = sideDomains)
generate_scatterplot(searchData, AName, BName, "Plan Length", paste("out/planLength_", AName, "_vs_", BName, ".pdf", sep = ""))