use memory_stats::memory_stats;

pub fn memory_usage() -> Option<String> {
    if let Some(usage) = memory_stats() {
        let usage = usage.physical_mem as f64;
        if usage < 1000.0 {
            return Some(format!("{: >2.0}B", usage));
        } else if usage < 1000.0 * 1000.0 {
            return Some(format!("{: >4.1}KB", usage / (1000.0)));
        } else if usage < 1000.0 * 1000.0 * 1000.0 {
            return Some(format!("{: >4.1}MB", usage / (1000.0 * 1000.0)));
        } else {
            return Some(format!("{: >4.1}GB", usage / (1000.0 * 1000.0 * 1000.0)));
        }
    } else {
        None
    }
}
