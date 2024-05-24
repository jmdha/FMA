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

max_unsolved_two <- function(data, target1, target2) {
  highest <- max(data[,target1], data[,target2], na.rm=TRUE)
  data[is.na(data[,target1]),][,target1] <- highest
  data[is.na(data[,target2]),][,target2] <- highest
  return (data)
}