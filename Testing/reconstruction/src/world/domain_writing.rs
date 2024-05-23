use super::World;

impl World {
    fn export_types(&self) -> String {
        self.types
            .iterate()
            .map(|(name, parent)| format!("\t\t{} - {}\n", name, parent))
            .collect()
    }

    fn export_predicates(&self) -> String {
        self.predicates
            .iterate()
            .map(|(name, parameters)| format!("\t\t({}{})\n", name, parameters.export()))
            .collect()
    }

    fn export_constants(&self) -> String {
        self.objects
            .iterate_constants()
            .map(|o| {
                format!(
                    "\t\t{} - {}\n",
                    self.objects.name(o),
                    self.types.name(self.objects.object_type(o))
                )
            })
            .collect()
    }

    fn export_actions(&self) -> String {
        self.actions.iter().map(|a| format!("{}", a)).collect()
    }

    fn export_meta_actions(&self, banned_meta_actions: &Vec<usize>) -> String {
        self.meta_actions
            .iter()
            .enumerate()
            .filter(|(i, ..)| !banned_meta_actions.contains(i))
            .map(|(_, a)| format!("{}", a))
            .collect()
    }

    pub fn export_meta_domain(&self, banned_meta_actions: &Vec<usize>) -> String {
        format!(
            "(define
    (domain {})
    (:types
{}  )
    (:predicates
{}
)
    (:constants
{}
)
{}
{})",
            self.domain_name,
            self.export_types(),
            self.export_predicates(),
            self.export_constants(),
            self.export_actions(),
            self.export_meta_actions(banned_meta_actions)
        )
    }
}
