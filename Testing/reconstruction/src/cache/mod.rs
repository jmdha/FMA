mod cache_data;
pub mod generation;
mod lifted;

use std::{
    collections::HashMap,
    sync::atomic::{AtomicUsize, Ordering},
};

use crate::{
    state::State,
    world::{action::Action, World},
};
use spingus::{sas_plan::SASPlan, term::Term};

pub static INVALID_REPLACEMENTS: AtomicUsize = AtomicUsize::new(0);

pub trait Cache {
    /// Retrives replacement from cache from given init to goal
    fn get_replacement(&self, meta_term: &Term, init: &State, goal: &State) -> Option<SASPlan>;
    fn add_entry(&mut self, meta_term: &Term, replacement_plan: &SASPlan);
}

pub(super) fn generate_plan(
    init: &State,
    replacement_macro: &Action,
    replacement_plan: &SASPlan,
    parameters: &Vec<usize>,
) -> Option<SASPlan> {
    let macro_parameters = &replacement_macro.parameters;
    let actions: Vec<String> = replacement_plan.iter().map(|t| t.name.to_owned()).collect();
    let replacements: Vec<&Action> = actions
        .iter()
        .map(|n| World::global().get_action(n))
        .collect();
    let mut state = init.clone();
    let mut plan: SASPlan = Vec::new();
    for (action, step) in replacements.iter().zip(replacement_plan.iter()) {
        let name = action.name.to_owned();
        let parameters: Vec<usize> = step
            .parameters
            .iter()
            .map(|n| {
                let index = macro_parameters.index(n);
                parameters[index]
            })
            .collect();
        let parameters_named = World::global().objects.names_cloned(&parameters);
        for pre in action.precondition.iter() {
            if state.has_nary(pre.predicate, &pre.map_args(&parameters)) != pre.value {
                INVALID_REPLACEMENTS.fetch_add(1, Ordering::SeqCst);
                return None;
            }
        }
        state.apply(action, &parameters);
        plan.push(Term {
            name,
            parameters: parameters_named,
        })
    }
    Some(plan)
}

pub(super) fn find_fixed(arguments: &Vec<usize>, macro_action: &Action) -> HashMap<usize, usize> {
    macro_action
        .parameters
        .iterate()
        .enumerate()
        .filter_map(|(i, (name, _))| match name.to_uppercase().contains('O') {
            true => None,
            false => {
                let parameter_index = name.replace('?', "").parse::<usize>().unwrap();
                Some((i, arguments[parameter_index]))
            }
        })
        .collect()
}
