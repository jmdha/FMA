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

rename_domains <- function(data){
  domains <- data$domain
  domains[domains == "barman"] <- "Barman"
  domains[domains == "blocksworld"] <- "Blocksworld"
  domains[domains == "child-snack"] <- "Childsnack"
  domains[domains == "depots"] <- "Depots"
  domains[domains == "driverlog"] <- "Driverlog"
  domains[domains == "floor-tile"] <- "Floortile"
  domains[domains == "grid"] <- "Grid"
  domains[domains == "gripper-strips"] <- "Gripper"
  domains[domains == "hiking"] <- "Hiking"
  domains[domains == "logistics-strips"] <- "Logistics"
  domains[domains == "miconic"] <- "Miconic"
  domains[domains == "parking"] <- "Parking"
  domains[domains == "rover"] <- "Rover"
  domains[domains == "satellite"] <- "Satellite"
  domains[domains == "scanalyzer3d"] <- "Scanalyzer"
  domains[domains == "woodworking"] <- "Woodworking"
  data$domain <- domains
  return (data)
}