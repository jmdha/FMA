library(xtable)
source("./Tools/latexTableHelpers.R")

generate_table <- function(data, outName, colNames, caption, label) {
  names(data) <- colNames
	table <- xtable(
	  data, 
	  type = "latex", 
	  caption=caption,
	  label=label
	)
	align(table ) <- generateRowDefinition(ncol(table), TRUE)
	print(table, 
	      file = outName, 
	      include.rownames=FALSE,
	      tabular.environment = "tabularx",
	      width = "\\textwidth / 2",
	      hline.after = topRowLines(nrow(table)),
	      sanitize.text.function = function(x) {x},
	      latex.environments="centering",
	      sanitize.colnames.function = bold,
	      floating = TRUE,
	      rotate.colnames = TRUE)
	
	return ()
}