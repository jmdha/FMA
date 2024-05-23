use crate::{state::State, tools::random_name};

use super::World;

impl World {
    pub fn export_problem(&self, init: &State, goal: &State) -> String {
        format!(
            "(define (problem {})
    (:domain {})
    (:objects{}
    )
    (:init{}
    )
    (:goal (and {}
    ))
)",
            random_name(),
            self.domain_name,
            self.objects
                .iterate_named()
                .map(|(object, type_name)| format!("\n\t\t{} - {}", object, type_name))
                .collect::<String>(),
            init.export(),
            goal.export()
        )
    }
}
