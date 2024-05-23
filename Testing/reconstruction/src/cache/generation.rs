use super::{cache_data::read_cache, lifted::LiftedCache, Cache};
use crate::tools::{status_print, Status};
use clap::ValueEnum;
use std::{collections::HashMap, path::PathBuf};

#[derive(Debug, Copy, Clone, PartialEq, Default, ValueEnum)]
pub enum CacheMethod {
    #[default]
    Lifted,
    None,
}

pub fn generate_cache(
    cache_path: &Option<PathBuf>,
    cache_type: CacheMethod,
    iterative_cache: bool,
) -> Option<Box<dyn Cache>> {
    if cache_type == CacheMethod::None || (cache_path.is_none() && !iterative_cache) {
        return None;
    }
    let data = cache_path
        .to_owned()
        .map_or(HashMap::new(), |p| read_cache(&p));
    status_print(Status::Cache, "Reading cache");
    match cache_type {
        CacheMethod::Lifted => Some(Box::new(LiftedCache::new(data))),
        _ => panic!(),
    }
}
