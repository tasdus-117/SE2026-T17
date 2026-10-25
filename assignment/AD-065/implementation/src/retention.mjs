import { policy } from './config.mjs';

export function retainOperationalEvents(store, now = Date.now()) {
  return store.purge(now - policy.retentionDays * 24 * 60 * 60 * 1000);
}
