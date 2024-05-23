use std::{
    sync::atomic::{AtomicUsize, Ordering},
    time::SystemTime,
};

use std::time::UNIX_EPOCH;

static START_TIME: AtomicUsize = AtomicUsize::new(0);

fn time() -> usize {
    SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .unwrap()
        .as_nanos() as usize
}

pub fn init_time() {
    START_TIME.store(time(), Ordering::SeqCst);
}

pub fn run_time() -> String {
    let start_time = START_TIME.load(Ordering::SeqCst);
    assert_ne!(start_time, 0); // Uninitialized - Run init_time
    let curr_time = time();
    let elapsed = curr_time - start_time;
    let elapsed = elapsed as f64 / (1000.0 * 1000.0);

    if elapsed < 1000.0 {
        format!("{: >6.2}ms", elapsed)
    } else {
        format!("{: >7.2}s", elapsed / 1000.0)
    }
}
