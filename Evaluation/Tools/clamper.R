max_unsolved <- function(data, target) {
	highest <- max(data[,target], na.rm=TRUE)
	data[is.na(data[,target]),][,target] <- highest
	return (data)
}

min_unsolved <- function(data, target) {
	lowest <- min(data[,target], na.rm=TRUE)
	data[is.na(data[,target]),][,target] <- lowest
	return (data)
}