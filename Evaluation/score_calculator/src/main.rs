use clap::Parser;
use input_handling::get_records;
use score_generation::{generate_csv, generate_report, generate_scores};
use std::error::Error;
use std::fs;
use std::path::PathBuf;

mod input_handling;
mod score_generation;

/// Simple program to calculate IPC 2023 competition scores for the learning track.
/// It can both accept stdin and an input argument
#[derive(Parser, Default, Debug)]
#[command(term_width = 0)]
pub struct Args {
    /// Path to input file
    input: Option<PathBuf>,
    /// Path to output to, if not given simply prints to stdout
    #[arg(short, long)]
    out: Option<PathBuf>,
    /// Specifies the time limit used for finding plans.
    /// Scores are calculated accordingly
    #[arg(short, long)]
    #[clap(value_parser = humantime::parse_duration, default_value = "30m")]
    time_limit: std::time::Duration,
    #[arg(short, long)]
    comma_seperated: bool,
}

fn main() -> Result<(), Box<dyn Error>> {
    let args = Args::parse();
    let records = get_records(&args.input)?;
    let scores = generate_scores(records, &args.time_limit.as_secs_f64());
    let report = match args.comma_seperated {
        true => generate_csv(scores),
        false => generate_report(scores),
    };
    match &args.out {
        Some(path) => fs::write(path, report)?,
        None => print!("{}", report),
    }
    Ok(())
}
