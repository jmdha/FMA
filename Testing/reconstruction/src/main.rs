mod cache;
mod fact;
mod instantiation;
mod macro_generation;
mod reconstruction;
mod state;
mod tools;
mod world;

use crate::instantiation::legal_count;
use crate::macro_generation::MacroMethod;
use crate::macro_generation::MACRO_METHOD;
use crate::reconstruction::downward_wrapper::{init_downward, Downward};
use crate::tools::val::check_val;
use crate::tools::{random_file_name, status_print};
use crate::world::{init_world, World};
use cache::generation::{generate_cache, CacheMethod};
use cache::Cache;
use cache::INVALID_REPLACEMENTS;
use clap::Parser;
use itertools::Itertools;
use reconstruction::reconstruction::reconstruct;
use spingus::sas_plan::{export_sas, SASPlan};
use std::fs;
use std::path::PathBuf;
use std::process::exit;
use std::sync::atomic::Ordering;
use std::time::Instant;
use tools::time::init_time;
use tools::Status;

#[derive(Parser, Default, Debug)]
#[command(term_width = 0)]
pub struct Args {
    /// Path to original domain
    #[arg(short = 'd')]
    domain: PathBuf,
    /// Path to original problem
    #[arg(short = 'p')]
    problem: PathBuf,
    /// Path to meta domain
    #[arg(short = 'm')]
    meta_domain: PathBuf,
    /// Path to fast-downward.
    #[arg(short = 'f')]
    downward: Option<PathBuf>,
    /// Path to write final solution to
    /// If not given, simply prints to stdout
    #[arg(short = 'o')]
    out: Option<PathBuf>,
    #[arg(long, default_value = "/tmp")]
    temp_dir: PathBuf,
    /// Path to a set of lifted macros used to cache meta action reconstruction
    #[arg(short = 'c')]
    cache: Option<PathBuf>,
    /// Type of caching
    #[arg(long, default_value = "lifted")]
    cache_method: CacheMethod,
    /// If given, adds entries found by planner to cache
    #[arg(short, long)]
    iterative_cache: bool,
    #[arg(long, default_value = "grounded")]
    macro_method: MacroMethod,
    /// Path to val
    /// If given checks reconstructed plan with VAL
    #[arg(short, long)]
    val: Option<PathBuf>,
}

fn meta_solve(
    cache: &mut Option<Box<dyn Cache>>,
    iterative: bool,
    domain_path: &PathBuf,
    problem_path: &PathBuf,
) -> SASPlan {
    let mut reconstruction_begin = None;
    let mut meta_solution_time: f64 = 0.0;
    let mut meta_search_time: f64 = 0.0;
    let meta_count = World::global().meta_actions.len();
    let mut banned_meta_actions: Vec<usize> = Vec::new();
    while banned_meta_actions.len() <= meta_count {
        let meta_domain = World::global().export_meta_domain(&banned_meta_actions);
        let meta_file = PathBuf::from(random_file_name(&Downward::global().temp_dir));
        let _ = fs::write(&meta_file, meta_domain);
        status_print(Status::Reconstruction, "Finding meta solution");
        let downward_result = match Downward::global().solve(&meta_file, problem_path) {
            Ok(p) => p,
            Err(err) => panic!("Had error finding meta solution: {}", err.to_string()),
        };
        println!("Found solution");
        meta_solution_time += downward_result.total_time;
        meta_search_time += downward_result.search_time;
        if reconstruction_begin.is_none() {
            reconstruction_begin = Some(Instant::now());
        }
        let reconstructed = reconstruct(cache, iterative, domain_path, &downward_result.plan);
        let _ = fs::remove_file(&meta_file);
        match reconstructed {
            Ok(plan) => {
                println!(
                    "reconstruction_time={:.4}",
                    reconstruction_begin.unwrap().elapsed().as_secs_f64()
                );
                println!("search_time={:.4}", meta_search_time);
                println!("solution_time={:.4}", meta_solution_time);
                println!(
                    "total_time={:.4}",
                    meta_solution_time + reconstruction_begin.unwrap().elapsed().as_secs_f64()
                );
                println!("meta_plan_length={}", downward_result.plan.len());
                println!(
                    "meta_actions_in_plan={}",
                    downward_result
                        .plan
                        .iter()
                        .filter(|s| World::global().is_meta_action(&s.name))
                        .count()
                );
                println!(
                    "meta_actions_in_plan_unique={}",
                    downward_result
                        .plan
                        .iter()
                        .filter(|s| World::global().is_meta_action(&s.name))
                        .unique_by(|s| &s.name)
                        .count()
                );
                println!("invalid_meta_actions={}", banned_meta_actions.len());
                return plan;
            }
            Err(err) => {
                println!(
                    "Invalid meta action: {}",
                    World::global().meta_actions[err].name
                );
                banned_meta_actions.push(err)
            }
        }
    }
    unreachable!();
}

fn main() {
    init_time();
    let args = Args::parse();
    println!("iterative_cache={}", args.iterative_cache);

    init_world(&args.domain, &args.meta_domain, &args.problem);
    init_downward(&args.downward, &args.temp_dir);
    let _ = MACRO_METHOD.set(args.macro_method);
    let cache_init_start = Instant::now();
    let mut cache = generate_cache(&args.cache, args.cache_method, args.iterative_cache);
    println!(
        "cache_init_time={:.4}",
        cache_init_start.elapsed().as_secs_f64()
    );

    let plan = meta_solve(
        &mut cache,
        args.iterative_cache,
        &args.domain,
        &args.problem,
    );
    println!("legal_operator_count={}", legal_count());
    if let Some(val_path) = args.val {
        status_print(Status::Validation, "Checking VAL");
        if check_val(
            &args.domain,
            &args.problem,
            &val_path,
            &args.temp_dir,
            &plan,
        ) {
            println!("---VALID---");
        } else {
            println!("---NOT VALID---");
        }
    }
    println!("final_plan_length={}", &plan.len());
    println!(
        "invalid_replacements={}",
        INVALID_REPLACEMENTS.load(Ordering::SeqCst)
    );
    if let Some(path) = args.out {
        let plan_export = export_sas(&plan);
        fs::write(path, plan_export).unwrap();
    }
    status_print(Status::Validation, "Finished");
    exit(0)
}
