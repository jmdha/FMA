source("./Tools/graphNames.R")

parse_general <- function(fileName){
  data <- read.csv(
    fileName, 
    header = T, 
    sep = ",", 
    colClasses = c(
      'character',
      'character','numeric',
      'numeric','numeric',
      'numeric','numeric',
      'numeric'
    )
  )
  data <- rename_domains_testing(data)
  data <- rename_domains(data)
  data <- rename_data(data)
  return (data)
}

parse_solve <- function(fileName){
  data <- read.csv(
    fileName, 
    header = T, 
    sep = ",", 
    colClasses = c(
      'character','character','character',
      'numeric','numeric', 'numeric',
      'numeric','numeric'
    )
  )
  data <- rename_domains_testing(data)
  data <- rename_domains(data)
  data <- rename_data(data)
  return (data)
}

parse_mutex_groups <- function(fileName){
  data <- read.csv(
    fileName, 
    header = T, 
    sep = ",", 
    colClasses = c(
      'character','numeric'
    )
  )
  data <- rename_domains_testing(data)
  data <- rename_domains(data)
  data <- rename_data(data)
  return (data)
}

parse_reconstruction_solve <- function(fileName){
  data <- read.csv(
    fileName, 
    header = T, 
    sep = ",", 
    colClasses = c(
      'character','character','character',
      'numeric','numeric', 'numeric',
      'numeric','numeric', 'numeric',
      'numeric','numeric', 'numeric',
      'numeric','numeric', 'numeric',
      'numeric'
    )
  )
  data <- rename_domains_testing(data)
  data <- rename_domains(data)
  data <- rename_data(data)
  return (data)
}