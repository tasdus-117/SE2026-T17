import { Store } from './store.mjs';
import { CombatService } from './application.mjs';
import { createServer } from './transport.mjs';
import { retainOperationalEvents } from './retention.mjs';

const store = new Store(process.env.AD065_DB ?? 'arena.sqlite', Number(process.env.AD065_VERSION ?? 2));
retainOperationalEvents(store);
const timer = setInterval(() => retainOperationalEvents(store), 60 * 60 * 1000);
timer.unref();
const server = createServer(new CombatService(store), { token: process.env.AD065_TOKEN });
server.listen({ port: Number(process.env.PORT ?? 8065), host: '127.0.0.1', backlog: 2048 });
process.on('SIGINT', () => server.close(() => { clearInterval(timer); store.close(); }));
