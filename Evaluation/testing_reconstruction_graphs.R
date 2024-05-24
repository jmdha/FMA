library(dplyr, warn.conflicts = FALSE) 

source("Tools/style.R")
source("Tools/scatterPlots.R")
source("Tools/graphNames.R")
source("Tools/clamper.R")
source("Tools/parsers.R")

# Handle arguments
args = commandArgs(trailingOnly=TRUE)
args[1] <- "Results/solve.csv"
args[2] <- "S_CPDDL"
args[3] <- "S_PHAM"
if (length(args) != 3) {
  stop("3 arguments must be supplied!", call.=FALSE)
}

data <- parse_reconstruction_solve(args[1])
AName <- recon_names(args[2])
BName <- recon_names(args[3])

if (nrow(data[data$name == AName,]) == 0)
  stop(paste("Column name '", args[2], "' not found in dataset!"), call.=FALSE)
if (nrow(data[data$name == BName,]) == 0)
  stop(paste("Column name '", args[3], "' not found in dataset!"), call.=FALSE)
data <- max_unsolved(data, "total_time")
data <- max_unsolved(data, "search_time")
data <- max_unsolved_two(data, "plan_length", "meta_plan_length")
data <- max_unsolved(data, "solution_time")
data <- max_unsolved(data, "reconstruction_time")
data <- min_unsolved(data, "invalid_meta_actions")

AData = data[data$name == AName,]
BData = data[data$name == BName,]
combined <- merge(AData, BData, by = c("domain", "problem"), suffixes=c(".A", ".B"))
combined <- combined %>% select(-contains('name.A'))
combined <- combined %>% select(-contains('name.B'))

# Ignore domains with invalid meta actions
invalids <- unique(combined[combined$invalid_meta_actions.A > 0 | combined$invalid_meta_actions.B > 0,]$domain)
print("Domains that contains invalid meta actions:")
print(invalids)
combined <- combined[!combined$domain %in% invalids,]

print("Generating: Reconstruction Total Time")
sideA <- combined$total_time.A
sideB <- combined$total_time.B
sideDomains <- combined$domain
searchData <- data.frame(x = sideA, y = sideB, domain = sideDomains)
generate_scatterplot(searchData, AName, BName, "Total Time (s)", paste("out/reconstruction_totalTime_", AName, "_vs_", BName, ".pdf", sep = ""))

print("Generating: Reconstruction Search")
sideA <- combined$search_time.A
sideB <- combined$search_time.B
sideDomains <- combined$domain
searchData <- data.frame(x = sideA, y = sideB, domain = sideDomains)
generate_scatterplot(searchData, AName, BName, "Search Time (s)", paste("out/reconstruction_searchTime_", AName, "_vs_", BName, ".pdf", sep = ""))

print("Generating: Reconstruction Plan Lengths (A)")
sideA <- combined$plan_length.A
sideB <- combined$meta_plan_length.A
sideDomains <- combined$domain
searchData <- data.frame(x = sideA, y = sideB, domain = sideDomains)
generate_scatterplot_nolegend(searchData, "Final Plan Length", "Meta Plan Length", paste("Final vs Meta Plan Lengths (", AName , ")", sep = ""), paste("out/reconstruction_", AName, "_planLengths", ".pdf", sep = ""), 1)

print("Generating: Reconstruction Plan Lengths (B)")
sideA <- combined$plan_length.B
sideB <- combined$meta_plan_length.B
sideDomains <- combined$domain
searchData <- data.frame(x = sideA, y = sideB, domain = sideDomains)
generate_scatterplot_nolegend(searchData, "Final Plan Length", "Meta Plan Length", paste("Final vs Meta Plan Lengths (", BName , ")", sep = ""), paste("out/reconstruction_", BName, "_planLengths", ".pdf", sep = ""), 1)
