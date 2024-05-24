recon_names <- function(name) { 
  if (name == "LAMA_FIRST") return ("LAMA")
	if (name == "S_CPDDL") return ("LAMA+F(top 2)")
	if (name == "S_PHAM") return ("LAMA+PTT(top 2)")
	return (name)
}

rename_data <- function(data) { 
	data[data=="LAMA_FIRST"] <- "LAMA"
	names(data)[names(data)=="LAMA_FIRST"] <- "LAMA"
	data[data=="S_CPDDL"] <- "LAMA+F(top 2)"
	names(data)[names(data)=="S_CPDDL"] <- "LAMA+F(top 2)"
	data[data=="S_PHAM"] <- "LAMA+PTT(top 2)"
	names(data)[names(data)=="S_PHAM"] <- "LAMA+PTT(top 2)"
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
  domains[domains == "rover"] <- "Rovers"
  domains[domains == "satellite"] <- "Satellite"
  domains[domains == "scanalyzer3d"] <- "Scanalyzer"
  domains[domains == "woodworking"] <- "Woodworking"
  data$domain <- domains
  return (data)
}

rename_domains_testing <- function(data){
  domains <- data$domain
  domains[domains == "barman"] <- "Barman"
  domains[domains == "blocksworld"] <- "Blocksworld"
  domains[domains == "childsnack"] <- "Childsnack"
  domains[domains == "depots"] <- "Depots"
  domains[domains == "driverlog"] <- "Driverlog"
  domains[domains == "floortile"] <- "Floortile"
  domains[domains == "grid"] <- "Grid"
  domains[domains == "gripper"] <- "Gripper"
  domains[domains == "hiking"] <- "Hiking"
  domains[domains == "logistics"] <- "Logistics"
  domains[domains == "miconic"] <- "Miconic"
  domains[domains == "parking"] <- "Parking"
  domains[domains == "rovers"] <- "Rovers"
  domains[domains == "satellite"] <- "Satellite"
  domains[domains == "scanalyzer"] <- "Scanalyzer"
  domains[domains == "woodworking"] <- "Woodworking"
  data$domain <- domains
  return (data)
}