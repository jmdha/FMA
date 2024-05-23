use crate::{
    fact::Fact,
    world::{action::Action, World},
};
use std::collections::HashSet;

#[derive(PartialEq, Eq, Clone, Debug)]
pub struct State {
    facts: HashSet<Fact>,
}

impl State {
    pub fn new(facts: &Vec<Fact>) -> Self {
        let facts: HashSet<Fact> = facts.iter().cloned().collect();
        Self { facts }
    }

    pub fn from_init() -> Self {
        State::new(&World::global().init)
    }

    pub fn apply(&mut self, action: &Action, arguments: &Vec<usize>) {
        for atom in action.effect.iter() {
            let corresponding: Vec<usize> = atom.map_args(arguments);
            let fact = Fact::new(atom.predicate, corresponding);
            match atom.value {
                true => self.facts.insert(fact),
                false => self.facts.remove(&fact),
            };
        }
    }

    fn get(&self) -> &HashSet<Fact> {
        &self.facts
    }

    pub fn has_nullary(&self, predicate: usize) -> bool {
        let fact = Fact::new_nullary(predicate);
        self.facts.contains(&fact)
    }

    pub fn has_unary(&self, predicate: usize, arg: usize) -> bool {
        let fact = Fact::new_unary(predicate, arg);
        self.facts.contains(&fact)
    }

    pub fn has_nary(&self, predicate: usize, arguments: &Vec<usize>) -> bool {
        let fact = Fact::new(predicate, arguments.clone());
        self.facts.contains(&fact)
    }

    pub fn diff(&self, state: &State) -> Vec<(Fact, bool)> {
        let mut diff = vec![];
        for i in self.get().difference(state.get()) {
            diff.push((*i, false))
        }
        for i in state.get().difference(self.get()) {
            diff.push((*i, true))
        }
        diff.sort();
        diff
    }

    pub fn export(&self) -> String {
        let mut s: String = "".to_owned();
        self.facts
            .iter()
            .for_each(|fact| s.push_str(&format!("\n\t\t({})", fact.to_string())));
        s
    }
}
