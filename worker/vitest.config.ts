import { cloudflareTest } from '@cloudflare/vitest-plugin';
import { defineConfig } from 'vitest/config';

export default defineConfig({
  plugins: [
    cloudflareTest({
      miniflare: {
        kvNamespaces: ['RATELIMIT'],
        bindings: { GITHUB_TOKEN: 'test-token' },
      },
      wrangler: { configPath: './wrangler.toml' },
    }),
  ],
});
