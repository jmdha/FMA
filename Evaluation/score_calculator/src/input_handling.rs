use serde::Deserialize;
use std::path::PathBuf;
use std::{error::Error, io};

#[derive(Debug, Deserialize)]
pub struct Record {
    pub name: String,
    #[allow(dead_code)]
    pub domain: String,
    #[allow(dead_code)]
    pub problem: String,
    #[allow(dead_code)]
    #[serde(deserialize_with = "csv::invalid_option")]
    pub total_time: Option<f64>,
}

fn read_input_file(file: &PathBuf) -> Result<Vec<Record>, Box<dyn Error>> {
    let mut records = vec![];
    let mut rdr = csv::Reader::from_path(file)?;
    for result in rdr.deserialize() {
        let record: Record = result?;
        records.push(record);
    }
    Ok(records)
}

fn read_stdin() -> Result<Vec<Record>, Box<dyn Error>> {
    let mut records = vec![];
    let mut rdr = csv::Reader::from_reader(io::stdin());
    for result in rdr.deserialize() {
        let record: Record = result?;
        records.push(record);
    }
    Ok(records)
}

fn read(input_path: &Option<PathBuf>) -> Result<Vec<Record>, Box<dyn Error>> {
    let mut records = vec![];
    if let Some(input_path) = input_path {
        records.append(&mut read_input_file(input_path)?);
    }
    if !atty::is(atty::Stream::Stdin) {
        records.append(&mut read_stdin()?);
    }
    Ok(records)
}

pub fn get_records(input_path: &Option<PathBuf>) -> Result<Vec<Record>, Box<dyn Error>> {
    read(input_path)
}
