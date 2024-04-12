recon_names <- function(name) { 
  	if (name == "Downward") return ("FD")
	if (name == "Stripped") return ("Stripped")
	return (name)
}

rename_data <- function(data) { 
	data[data=="Downward"] <- "FD"
	names(data)[names(data)=="Downward"] <- "FD"
	data[data=="Stripped"] <- "Stripped"
	names(data)[names(data)=="Stripped"] <- "Stripped"
	return (data)
}

