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
#args[2] <- "S_CPDDL"
#args[3] <- "S_PHAM"
if (length(args) != 3) {
  stop("3 arguments must be supplied!", call.=FALSE)
}

data <- parse_reconstruction_solve(args[1])
AName <- recon_names(args[2])
BName <- recon_names(args[3])

data <- max_unsolved(data, "total_time")
data <- max_unsolved(data, "search_time")
data <- max_unsolved_two(data, "plan_length", "meta_plan_length")
data <- min_unsolved(data, "invalid_meta_actions")

combined <- split_data(data, AName, BName)

dir.create(file.path("Out"), showWarnings = FALSE)

# Ignore domains with invalid meta actions
#invalids <- unique(combined[combined$invalid_meta_actions.A > 0 | combined$invalid_meta_actions.B > 0,]$domain)
#print("Domains that contains invalid meta actions:")
#print(invalids)
#combined <- combined[!combined$domain %in% invalids,]

print("Generating: Reconstruction Search Time")
sideA <- combined$search_time.A
sideB <- combined$search_time.B
sideDomains <- combined$domain
searchData <- data.frame(x = sideA, y = sideB, domain = sideDomains)
generate_scatterplot_nolegend(searchData, AName, BName, "Search Time (s)", paste("out/reconstruction_searchTime_", AName, "_vs_", BName, ".pdf", sep = ""))
generate_scatterplot_onlylegend(searchData, AName, BName, "", paste("out/legend_reconstruction_", AName, "_", BName, ".pdf", sep = ""))

print("Generating: Reconstruction Total Time")
sideA <- combined$total_time.A
sideB <- combined$total_time.B
sideDomains <- combined$domain
searchData <- data.frame(x = sideA, y = sideB, domain = sideDomains)
generate_scatterplot_nolegend(searchData, AName, BName, "Total Time (s)", paste("out/reconstruction_totalTime_", AName, "_vs_", BName, ".pdf", sep = ""))

print("Generating: Final Plan Length")
sideA <- combined$plan_length.A
sideB <- combined$plan_length.B
sideDomains <- combined$domain
searchData <- data.frame(x = sideA, y = sideB, domain = sideDomains)
generate_scatterplot_nolegend(searchData, AName, BName, "Final Plan Length", paste("out/reconstruction_planLength_", AName, "_vs_", BName, ".pdf", sep = ""))

print("Generating: Meta Plan Length")
sideA <- combined$meta_plan_length.A
sideB <- combined$meta_plan_length.B
sideDomains <- combined$domain
searchData <- data.frame(x = sideA, y = sideB, domain = sideDomains)
generate_scatterplot_nolegend(searchData, AName, BName, "Meta Plan Length", paste("out/reconstruction_metaPlanLength_", AName, "_vs_", BName, ".pdf", sep = ""))
