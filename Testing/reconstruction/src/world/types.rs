use std::collections::HashMap;

#[derive(Debug)]
pub struct Types {
    index_map: HashMap<String, usize>,
    parent: HashMap<usize, usize>,
}

impl Types {
    pub fn default(&self) -> usize {
        self.index("object")
    }

    pub fn index(&self, name: &str) -> usize {
        self.index_map[name]
    }

    pub fn name(&self, index: usize) -> &String {
        &self.index_map.iter().find(|(_, v)| **v == index).unwrap().0
    }

    pub fn parent(&self, index: usize) -> Option<usize> {
        self.parent.get(&index).copied()
    }

    pub fn parent_name(&self, index: usize) -> String {
        match self.parent(index) {
            Some(parent) => self.name(parent).to_owned(),
            None => "object".to_string(),
        }
    }

    pub fn is_of_type(&self, type_index: usize, wished_index: usize) -> bool {
        if type_index == wished_index {
            return true;
        }
        match self.parent(type_index) {
            Some(parent) => self.is_of_type(parent, wished_index),
            None => false,
        }
    }

    pub fn iterate<'a>(&'a self) -> impl Iterator<Item = (&String, String)> + 'a {
        self.index_map
            .iter()
            .filter(|(_, index)| **index != self.default())
            .map(|(n, index)| (n, self.parent_name(*index)))
    }
}

pub(super) fn translate_types(types: Option<spingus::domain::types::Types>) -> Types {
    let mut index_map: HashMap<String, usize> = HashMap::new();
    let mut parent: HashMap<usize, usize> = HashMap::new();
    index_map.insert("object".to_string(), 0);

    if let Some(types) = types {
        for t in types.iter() {
            let type_name = t.name.to_lowercase();
            let type_index = match index_map.contains_key(&type_name) {
                true => index_map[&type_name],
                false => {
                    let index = index_map.len();
                    index_map.insert(type_name, index);
                    index
                }
            };
            for t in t.sub_types.iter() {
                let child_name = t.to_lowercase();
                let child_index = match index_map.contains_key(&child_name) {
                    true => index_map[&child_name],
                    false => {
                        let index = index_map.len();
                        index_map.insert(child_name, index);
                        index
                    }
                };
                parent.insert(child_index, type_index);
            }
        }
    }

    println!("type_count={}", index_map.len());

    Types { index_map, parent }
}
