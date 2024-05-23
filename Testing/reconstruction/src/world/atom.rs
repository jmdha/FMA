use spingus::domain::action::string_expression::StringExpression;

use super::{objects::Objects, parameter::Parameters, predicates::Predicates, World};

#[derive(Debug, PartialEq, Eq)]
pub enum Argument {
    // Refers to the index of some parameter
    Parameter(usize),
    // Refers to the index of some constant
    Constant(usize),
}

#[derive(Debug, PartialEq, Eq)]
pub struct Atom {
    /// NOTE: predicate of 0 indicates equality
    pub predicate: usize,
    pub parameters: Vec<Argument>,
    pub value: bool,
}

impl Atom {
    pub fn is_nullary(&self) -> bool {
        self.parameters.len() == 0
    }

    pub fn is_unary(&self) -> bool {
        self.parameters.len() == 1
    }

    pub fn map_args(&self, args: &Vec<usize>) -> Vec<usize> {
        self.parameters
            .iter()
            .map(|p| match p {
                Argument::Parameter(i) => args[*i],
                Argument::Constant(o) => *o,
            })
            .collect()
    }

    pub fn export(&self, args: &Vec<String>) -> String {
        let s = format!(
            "({}{})",
            World::global().predicates.name(self.predicate),
            self.parameters
                .iter()
                .map(|p| format!(
                    " {}",
                    match p {
                        Argument::Parameter(i) => args[*i].to_owned(),
                        Argument::Constant(o) => World::global().objects.name(*o).to_owned(),
                    }
                ))
                .collect::<String>()
        );
        match self.value {
            true => s,
            false => format!("(not {})", s),
        }
    }
}

pub(super) fn convert_expression(
    predicates: &Predicates,
    objects: &Objects,
    parameters: &Parameters,
    expression: StringExpression,
) -> Vec<Atom> {
    let mut atoms: Vec<Atom> = Vec::new();

    let mut queue: Vec<(StringExpression, bool)> = vec![(expression, true)];

    while !queue.is_empty() {
        let e = queue.pop().unwrap();
        match e {
            (StringExpression::Predicate(p), v) => atoms.push(Atom {
                predicate: predicates.index(&p.name),
                parameters: p
                    .parameters
                    .iter()
                    .map(|n| match parameters.contains(n) {
                        true => Argument::Parameter(parameters.index(n)),
                        false => Argument::Constant(objects.index(n)),
                    })
                    .collect(),
                value: v,
            }),
            (StringExpression::Equal(e), v) => atoms.push(Atom {
                predicate: 0,
                parameters: e
                    .iter()
                    .map(|n| match parameters.contains(n) {
                        true => Argument::Parameter(parameters.index(n)),
                        false => Argument::Constant(objects.index(n)),
                    })
                    .collect(),
                value: v,
            }),
            (StringExpression::And(e), v) => queue.extend(e.into_iter().map(|e| (e, v))),
            (StringExpression::Not(e), v) => queue.push((*e, !v)),
            _ => todo!(),
        };
    }

    atoms
}
