use std::{fs, path::PathBuf, process::Command};

use spingus::sas_plan::{export_sas, SASPlan};

use super::random_file_name;

pub fn check_val(
    domain_path: &PathBuf,
    problem_path: &PathBuf,
    val_path: &PathBuf,
    temp_dir: &PathBuf,
    plan: &SASPlan,
) -> bool {
    let plan_path = random_file_name(temp_dir);
    let _ = fs::write(&plan_path, export_sas(plan));
    let mut cmd = Command::new(val_path.to_str().unwrap());
    cmd.args([
        "-v",
        domain_path.to_str().unwrap(),
        problem_path.to_str().unwrap(),
        &plan_path,
    ]);
    let out = cmd.output();
    let _ = fs::remove_file(plan_path);
    match out {
        Ok(o) => !is_error(String::from_utf8(o.stdout).unwrap()),
        Err(_) => {
            eprintln!("Failed to run VAL");
            return true;
        }
    }
}

fn is_error(stdout: String) -> bool {
    stdout.contains("Failed plans")
}
