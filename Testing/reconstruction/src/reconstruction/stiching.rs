use spingus::sas_plan::SASPlan;

pub fn stich(plan: &SASPlan, replacements: Vec<(usize, SASPlan)>) -> SASPlan {
    if replacements.is_empty() {
        return plan.clone();
    }

    let mut stiched_plan = plan.to_owned();

    replacements.iter().rev().for_each(|(i, plan)| {
        stiched_plan.remove(*i);
        stiched_plan.splice(*i..*i, plan.iter().cloned());
    });

    stiched_plan
}

#[cfg(test)]
mod test {
    use spingus::{
        sas_plan::{export_sas, parse_sas},
        term::Term,
    };

    use super::stich;

    #[test]
    fn stitch_none() {
        let meta_plan = "(a)\n(b)\n(c)\n";
        let parsed_plan = parse_sas(meta_plan).unwrap();
        let replacements = vec![];

        let stiched = stich(&parsed_plan, replacements);
        let stiched_s = export_sas(&stiched);

        assert_eq!(meta_plan, stiched_s);
    }

    #[test]
    fn stitch_first() {
        let meta_plan = vec![
            Term {
                name: "a".to_string(),
                parameters: vec![],
            },
            Term {
                name: "b".to_string(),
                parameters: vec![],
            },
            Term {
                name: "c".to_string(),
                parameters: vec![],
            },
        ];
        let index = 0 as usize;
        let replacement_plan = vec![
            Term {
                name: "1".to_string(),
                parameters: vec![],
            },
            Term {
                name: "2".to_string(),
                parameters: vec![],
            },
            Term {
                name: "3".to_string(),
                parameters: vec![],
            },
        ];

        let stiched = stich(&meta_plan, vec![(index, replacement_plan)]);

        assert_eq!(
            vec![
                Term {
                    name: "1".to_string(),
                    parameters: vec![],
                },
                Term {
                    name: "2".to_string(),
                    parameters: vec![],
                },
                Term {
                    name: "3".to_string(),
                    parameters: vec![],
                },
                Term {
                    name: "b".to_string(),
                    parameters: vec![],
                },
                Term {
                    name: "c".to_string(),
                    parameters: vec![],
                },
            ],
            stiched
        );
    }
}
