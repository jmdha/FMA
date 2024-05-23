use super::{
    atom::{convert_expression, Atom},
    objects::Objects,
    parameter::{translate_parameters, Parameters},
    predicates::Predicates,
    types::Types,
    World,
};
use core::fmt;

#[derive(Debug, PartialEq, Eq)]
pub struct Action {
    pub name: String,
    pub parameters: Parameters,
    pub precondition: Vec<Atom>,
    pub effect: Vec<Atom>,
}

impl Action {
    pub fn new(action: spingus::domain::action::Action) -> Self {
        translate_action(
            &World::global().types,
            &World::global().predicates,
            &World::global().objects,
            action,
        )
    }
}

impl fmt::Display for Action {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        writeln!(f, "\t(:action {}", self.name)?;
        writeln!(f, "\t\t:parameters ({})", self.parameters.export())?;
        writeln!(f, "\t\t:precondition (and")?;
        for atom in self.precondition.iter() {
            writeln!(f, "\t\t\t{}", atom.export(&self.parameters.names))?;
        }
        writeln!(f, "\t\t)")?;
        writeln!(f, "\t\t:effect (and")?;
        for atom in self.effect.iter() {
            writeln!(f, "\t\t\t{}", atom.export(&self.parameters.names))?;
        }
        writeln!(f, "\t\t)")?;
        writeln!(f, "\t)")?;
        Ok(())
    }
}

pub(super) fn translate_action(
    types: &Types,
    predicates: &Predicates,
    objects: &Objects,
    action: spingus::domain::action::Action,
) -> Action {
    let name = action.name;
    let parameters = translate_parameters(types, action.parameters);
    let precondition = match action.precondition {
        Some(e) => convert_expression(predicates, objects, &parameters, e),
        None => vec![],
    };
    let effect = convert_expression(predicates, objects, &parameters, action.effect);
    Action {
        name,
        parameters,
        precondition,
        effect,
    }
}
