use std::collections::HashMap;

use itertools::Itertools;

use crate::input_handling::Record;

fn calculate_score(time_used: &f64, time_limit: &f64) -> f64 {
    if *time_used < 1.0 {
        return 1.0;
    } else {
        return 1.0 - (time_used.log2() / time_limit.log2());
    }
}

pub fn generate_scores(
    records: Vec<Record>,
    time_limit: &f64,
) -> HashMap<String, HashMap<String, f64>> {
    let mut scores: HashMap<String, HashMap<String, f64>> = HashMap::new();
    for record in records.into_iter() {
        let domain_entry = scores.entry(record.domain).or_default();
        let reconstruction_entry = domain_entry.entry(record.name).or_default();
        if let Some(time_used) = record.total_time {
            let score = calculate_score(&time_used, time_limit);
            *reconstruction_entry += score;
        }
    }
    scores
}

pub fn generate_report(scores: HashMap<String, HashMap<String, f64>>) -> String {
    let domains: Vec<String> = scores.keys().cloned().sorted().collect();
    let methods: Vec<String> = scores
        .iter()
        .flat_map(|(_, v)| v.keys().cloned())
        .unique()
        .sorted()
        .collect();
    let domain_width = domains
        .iter()
        .max_by(|a, b| a.len().cmp(&b.len()))
        .unwrap()
        .len()
        + 1;
    let method_width = methods
        .iter()
        .max_by(|a, b| a.len().cmp(&b.len()))
        .unwrap()
        .len()
        + 1;
    let mut s = format!("{:1$}", "domain", domain_width);
    for method in methods.iter() {
        s.push_str(&format!("{:>1$}", method, method_width));
    }
    s.push_str("\n");
    for domain in domains.into_iter() {
        let score_entry = &scores[&domain];
        s.push_str(&format!("{:1$}", domain, domain_width));
        for method in methods.iter() {
            s.push_str(&format!("{:1$.0}", score_entry[method], method_width));
        }
        s.push_str("\n");
    }
    s.push_str(&format!("{:1$}", "sum", domain_width));
    for method in methods.iter() {
        let sum: f64 = scores.iter().map(|(_, v)| v[method]).sum();
        s.push_str(&format!("{:1$.0}", sum, method_width));
    }
    s.push_str("\n");

    s
}

pub fn generate_csv(scores: HashMap<String, HashMap<String, f64>>) -> String {
    let domains: Vec<String> = scores.keys().cloned().sorted().collect();
    let methods: Vec<String> = scores
        .iter()
        .flat_map(|(_, v)| v.keys().cloned())
        .unique()
        .sorted()
        .collect();
    let mut s = "domains,".to_string();
    for method in methods.iter() {
        s.push_str(&format!("{},", method));
    }
    s.push_str("\n");
    for domain in domains.into_iter() {
        let score_entry = &scores[&domain];
        s.push_str(&format!("{},", domain));
        for method in methods.iter() {
            s.push_str(&format!("{},", score_entry[method]));
        }
        s.push_str("\n");
    }
    s
}
