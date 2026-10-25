import http from 'node:http';
import { timingSafeEqual } from 'node:crypto';
import { DomainError } from './domain.mjs';

export function createServer(service, { token, actor = 'alice', faultyResponse = false, onCommitted = () => {} }) {
  if (!token || token.length < 16) throw new Error('token must contain at least 16 characters');
  return http.createServer(async (request, response) => {
    const send = (status, data) => { response.writeHead(status, { 'content-type': 'application/json' }); response.end(JSON.stringify(data)); };
    try {
      if (request.method !== 'POST' || request.url !== '/v1/combat') return send(404, { error: 'not found' });
      const supplied = Buffer.from(request.headers.authorization ?? '');
      const expected = Buffer.from(`Bearer ${token}`);
      if (supplied.length !== expected.length || !timingSafeEqual(supplied, expected)) throw new DomainError('unauthorized', 401);
      let body = '';
      for await (const chunk of request) {
        body += chunk;
        if (Buffer.byteLength(body) > 4096) throw new DomainError('body too large', 413);
      }
      let input;
      try { input = JSON.parse(body); } catch { throw new DomainError('invalid json'); }
      if (!input || Array.isArray(input) || Object.keys(input).sort().join(',') !== 'commandId,matchId') throw new DomainError('invalid fields');
      const result = service.execute(input.matchId, input.commandId, actor);
      try { onCommitted(result); } catch { response.setHeader('x-observer-status', 'failed'); }
      return send(200, faultyResponse ? { ...result, health: result.health + 1 } : result);
    } catch (error) { return send(error.status ?? 500, { error: error.status ? error.message : 'internal error' }); }
  });
}
