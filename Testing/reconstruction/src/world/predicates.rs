use std::collections::{HashMap, HashSet};

use spingus::domain::action::string_expression::StringExpression;

use super::{
    parameter::{translate_parameters, Parameters},
    types::Types,
};

pub struct Predicates {
    index_map: HashMap<String, usize>,
    static_predicates: HashSet<usize>,
    parameters: HashMap<usize, Parameters>,
}

impl Predicates {
    pub fn index(&self, name: &str) -> usize {
        self.index_map[name]
    }

    pub fn name(&self, index: usize) -> &str {
        match index {
            0 => "=",
            _ => self.index_map.iter().find(|(_, v)| **v == index).unwrap().0,
        }
    }

    #[allow(unused)]
    pub fn arity(&self, index: usize) -> usize {
        self.parameters[&index].arity()
    }

    pub fn is_static(&self, index: usize) -> bool {
        self.static_predicates.contains(&index)
    }

    pub fn iterate<'a>(&'a self) -> impl Iterator<Item = (&String, &Parameters)> + 'a {
        self.index_map
            .iter()
            .map(|(name, index)| (name, &self.parameters[index]))
    }

    #[allow(unused)]
    pub fn count(&self) -> usize {
        self.index_map.len()
    }
}

fn find_static_predicates(
    actions: &spingus::domain::action::Actions,
    index_map: &HashMap<String, usize>,
) -> HashSet<usize> {
    let mut mutable_predicates: HashSet<usize> = HashSet::new();
    let mut queue: Vec<&StringExpression> = actions.iter().map(|a| &a.effect).collect();

    while !queue.is_empty() {
        let e: &StringExpression = queue.pop().unwrap();
        match e {
            StringExpression::Predicate(p) => {
                mutable_predicates.insert(index_map[&p.name]);
            }
            StringExpression::And(e) => queue.extend(e),
            StringExpression::Not(e) => queue.push(e),
            _ => {}
        };
    }

    index_map
        .iter()
        .filter(|(_, v)| !mutable_predicates.contains(v))
        .map(|(_, v)| *v)
        .collect()
}

pub(super) fn translate_predicates(
    types: &Types,
    actions: &spingus::domain::action::Actions,
    predicates: spingus::domain::predicate::Predicates,
) -> Predicates {
    let index_map: HashMap<String, usize> = predicates
        .iter()
        .enumerate()
        .map(|(i, p)| (p.name.to_owned(), i + 1))
        .collect();
    let parameters: HashMap<usize, Parameters> = predicates
        .into_iter()
        .map(|p| {
            let index = index_map[&p.name];
            (index, translate_parameters(types, p.parameters))
        })
        .collect();
    let static_predicates = find_static_predicates(actions, &index_map);
    println!("predicate_count={}", index_map.len());
    println!("static_predicates={}", static_predicates.len());

    Predicates {
        index_map,
        static_predicates,
        parameters,
    }
}
