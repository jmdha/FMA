library(dplyr, warn.conflicts = FALSE) 

source("Tools/style.R")
source("Tools/scatterPlots.R")
source("Tools/graphNames.R")
source("Tools/clamper.R")
source("Tools/parsers.R")
source("Tools/dataSplitter.R")

# Handle arguments
args = commandArgs(trailingOnly=TRUE)
#args[1] <- "Results/solve.csv"
#args[2] <- "CPDDL"
#args[3] <- "LAMA_FIRST"
if (length(args) != 3) {
  stop("3 arguments must be supplied!", call.=FALSE)
}

data <- parse_solve(args[1])
AName <- recon_names(args[2])
BName <- recon_names(args[3])

data <- max_unsolved(data, "total_time")
data <- max_unsolved(data, "search_time")
data <- max_unsolved(data, "plan_length")

combined <- split_data(data, AName, BName)

dir.create(file.path("Out"), showWarnings = FALSE)

print("Generating: Search Time")
sideA <- combined$search_time.A
sideB <- combined$search_time.B
sideDomains <- combined$domain
searchData <- data.frame(x = sideA, y = sideB, domain = sideDomains)
generate_scatterplot_nolegend(searchData, AName, BName, "Search Time (s)", paste("out/searchTime_", AName, "_vs_", BName, ".pdf", sep = ""))
generate_scatterplot_onlylegend(searchData, AName, BName, "", paste("out/legend_", AName, "_", BName, ".pdf", sep = ""))

print("Generating: Total Time")
sideA <- combined$total_time.A
sideB <- combined$total_time.B
sideDomains <- combined$domain
searchData <- data.frame(x = sideA, y = sideB, domain = sideDomains)
generate_scatterplot_nolegend(searchData, AName, BName, "Total Time (s)", paste("out/totalTime_", AName, "_vs_", BName, ".pdf", sep = ""))

print("Generating: Final Plan Length")
sideA <- combined$plan_length.A
sideB <- combined$plan_length.B
sideDomains <- combined$domain
searchData <- data.frame(x = sideA, y = sideB, domain = sideDomains)
generate_scatterplot_nolegend(searchData, AName, BName, "Plan Length", paste("out/planLength_", AName, "_vs_", BName, ".pdf", sep = ""))

