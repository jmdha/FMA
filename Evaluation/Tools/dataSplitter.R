split_data <- function(data, AName, BName) {
	if (nrow(data[data$name == AName,]) == 0)
	  stop(paste("Column name '", args[2], "' not found in dataset!"), call.=FALSE)
	if (nrow(data[data$name == BName,]) == 0)
	  stop(paste("Column name '", args[3], "' not found in dataset!"), call.=FALSE)

	AData = data[data$name == AName,]
	BData = data[data$name == BName,]
	combined <- merge(AData, BData, by = c("domain", "problem"), suffixes=c(".A", ".B"))
	combined <- combined %>% select(-contains('name.A'))
	combined <- combined %>% select(-contains('name.B'))

	return (combined)
}