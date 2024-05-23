use super::{types::Types, World};

#[derive(Debug, PartialEq, Eq)]
pub struct Parameters {
    pub names: Vec<String>,
    pub types: Vec<usize>,
}

impl Parameters {
    pub fn index(&self, name: &str) -> usize {
        self.names
            .iter()
            .position(|n| n == name)
            .expect(&format!("{:?} - {}", self.names, name))
    }
    #[allow(unused)]
    pub fn arity(&self) -> usize {
        self.types.len()
    }

    pub fn contains(&self, name: &String) -> bool {
        self.names.contains(name)
    }

    pub fn iterate<'a>(&'a self) -> impl Iterator<Item = (&String, &String)> + 'a {
        self.names
            .iter()
            .zip(self.types.iter())
            .map(|(name, type_index)| (name, World::global().types.name(*type_index)))
    }

    pub fn export(&self) -> String {
        self.iterate()
            .map(|(p_name, t_name)| format!(" {} - {}", p_name, t_name))
            .collect()
    }
}

pub(super) fn translate_parameters(
    types: &Types,
    parameters: spingus::domain::parameter::Parameters,
) -> Parameters {
    let mut names: Vec<String> = Vec::new();
    let mut type_indexes: Vec<usize> = Vec::new();
    parameters.into_iter().for_each(|p| match p {
        spingus::domain::parameter::Parameter::Untyped { name } => {
            names.push(name);
            type_indexes.push(types.default());
        }
        spingus::domain::parameter::Parameter::Typed { name, type_name } => {
            names.push(name);
            type_indexes.push(types.index(&type_name));
        }
        _ => todo!(),
    });
    Parameters {
        names,
        types: type_indexes,
    }
}
