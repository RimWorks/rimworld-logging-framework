# rimlogging-bundle worker

Cloudflare Worker that accepts RimLogging bug-report bundles from the C# `ProxyClient` and turns them into secret GitHub gists.

## Endpoint

`POST https://rimlogging-bundle.cryptiklemur.workers.dev/v1/bundle`

Body: a JSON `BundlePayload` (see `src/types.ts`).
Response on success: `{ "url": "https://gist.github.com/...", "id": "..." }`.

## Errors

| Status | Meaning |
|--------|---------|
| 400    | malformed JSON or schema validation failure |
| 404    | wrong path |
| 405    | wrong method |
| 413    | body exceeds 5 MB |
| 415    | wrong content-type |
| 429    | rate limited (10 bundles per hour per IP) |
| 502    | GitHub API failure |

## Local development

```bash
cd worker
npm install
npm run dev      # wrangler dev (uses local KV)
npm test         # vitest with @cloudflare/vitest-pool-workers
npm run typecheck
```

## Deploy

```bash
cd worker

# One-time setup:
npx wrangler kv namespace create RATELIMIT
#   -> copy the printed id into wrangler.toml under [[kv_namespaces]]

npx wrangler secret put GITHUB_TOKEN
#   -> paste a fine-grained PAT with the `gist` scope

# Deploy:
npm run deploy
```

CI deploys automatically on push to `beta` when `worker/**` changes. See `.github/workflows/worker-deploy.yml`.

## Rate limiting

Fixed window: 10 successful or attempted bundles per hour, keyed on `CF-Connecting-IP`. Stored in KV under `rl:{ip}` with TTL = remaining window seconds.
