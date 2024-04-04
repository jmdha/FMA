bold <- function(x){
	paste0('\\textbf{', x, '}')
}

topRowLines <- function(rows) 
	c(-1, 0, rows)
topRowBottomRowLines <- function(rows) 
	c(-1, 0, rows - 1, rows)

generateRowDefinition <- function(columnCount, ignoreFirst = FALSE) {
	source <- "|"
	if (ignoreFirst == TRUE)
		source <- "|0|X|"
	else
		source <- "|X|"
	for (i in 2:columnCount)
		source <- paste(source, "l|", sep="")
	return (source)
}
