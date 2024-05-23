use std::collections::HashMap;

use itertools::Itertools;

use crate::{
    fact::Fact,
    world::{
        action::Action,
        atom::{Argument, Atom},
        parameter::Parameters,
        World,
    },
};

use spingus::{sas_plan::SASPlan, term::Term};

pub(super) fn generate_macro(
    meta_action: &Action,
    operators: Vec<(&Action, Vec<usize>)>,
) -> (Action, SASPlan) {
    let name: String = operators
        .iter()
        .map(|(a, ..)| format!("_{}", a.name))
        .collect();
    let mut cul_args: Vec<usize> = Vec::new();
    let mut cul_pre: HashMap<Fact, bool> = HashMap::new();
    let mut cul_eff: HashMap<Fact, bool> = HashMap::new();

    for (action, args) in operators.iter() {
        let new_args = args.iter().filter(|a| !cul_args.contains(a)).collect_vec();
        cul_args.extend(new_args);
        combine_pre(&mut cul_pre, &cul_eff, &action.precondition, args);
        combine_eff(&mut cul_eff, &action.effect, args);
    }

    let effect: Vec<Atom> = cul_eff
        .into_iter()
        .filter(|(fact, value)| !(cul_pre.contains_key(fact) && cul_pre[fact] == *value))
        .sorted()
        .map(|(fact, value)| Atom {
            predicate: fact.predicate(),
            parameters: fact
                .parameters()
                .iter()
                .map(|f_a| match World::global().objects.is_constant(*f_a) {
                    true => Argument::Constant(*f_a),
                    false => Argument::Parameter(cul_args.iter().position(|a| a == f_a).unwrap()),
                })
                .collect(),
            value,
        })
        .collect();
    let precondition = cul_pre
        .into_iter()
        .sorted()
        .map(|(fact, value)| Atom {
            predicate: fact.predicate(),
            parameters: fact
                .parameters()
                .iter()
                .map(|f_a| match World::global().objects.is_constant(*f_a) {
                    true => Argument::Constant(*f_a),
                    false => Argument::Parameter(cul_args.iter().position(|a| a == f_a).unwrap()),
                })
                .collect(),
            value,
        })
        .collect();
    // macro parameter maps to meta parameter
    let mut fixed_parameters: HashMap<usize, usize> = HashMap::new();
    for atom in meta_action.effect.iter() {
        for corresponding_atom in effect.iter().filter(|a| a.predicate == atom.predicate) {
            for (i, parameter) in corresponding_atom.parameters.iter().enumerate() {
                match parameter {
                    Argument::Parameter(p) => {
                        let _ = fixed_parameters.insert(
                            *p,
                            match atom.parameters[i] {
                                Argument::Parameter(p) => p,
                                Argument::Constant(o) => o,
                            },
                        );
                    }
                    Argument::Constant(_) => {}
                };
            }
        }
    }

    let parameter_names: Vec<String> = cul_args
        .iter()
        .enumerate()
        .map(|(i, _)| match fixed_parameters.contains_key(&i) {
            true => format!("{}", fixed_parameters[&i]),
            false => format!("O{}", i),
        })
        .collect();
    let replacement_plan = operators
        .iter()
        .map(|(action, args)| Term {
            name: action.name.to_owned(),
            parameters: args
                .iter()
                .map(|a| {
                    parameter_names[cul_args.iter().position(|c_arg| a == c_arg).unwrap()]
                        .to_owned()
                })
                .collect(),
        })
        .collect();
    let parameter_types = cul_args
        .iter()
        .map(|a| World::global().objects.object_type(*a))
        .collect();
    let parameters = Parameters {
        names: parameter_names,
        types: parameter_types,
    };

    (
        Action {
            name,
            parameters,
            precondition,
            effect,
        },
        replacement_plan,
    )
}

fn combine_pre(
    cul_pre: &mut HashMap<Fact, bool>,
    cul_eff: &HashMap<Fact, bool>,
    pre: &Vec<Atom>,
    args: &Vec<usize>,
) {
    for atom in pre.iter() {
        let corresponding: Vec<usize> = atom.map_args(args);
        let fact = Fact::new(atom.predicate, corresponding);
        if !cul_pre.contains_key(&fact) && !cul_eff.contains_key(&fact) {
            cul_pre.insert(fact, atom.value);
        }
    }
}

fn combine_eff(cul_eff: &mut HashMap<Fact, bool>, eff: &Vec<Atom>, args: &Vec<usize>) {
    for atom in eff.iter() {
        let corresponding: Vec<usize> = atom.map_args(args);
        let fact = Fact::new(atom.predicate, corresponding);
        cul_eff.insert(fact, atom.value);
    }
}
