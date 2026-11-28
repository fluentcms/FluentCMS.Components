// node tests/FluentCMS.Components.Browser/test-startup.mjs [artifacts|environment|logging]
import { spawnSync } from 'node:child_process';
import { mkdtemp, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { basename, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const cases = ['artifacts', 'environment', 'logging'];
const selected = process.argv.slice(2);
const root = fileURLToPath(new URL('../../', import.meta.url));

for (const name of selected.length ? selected : cases) {
  if (!cases.includes(name)) throw new Error(`Unknown startup test: ${name}`);

  const mode = name === 'environment' ? 'Wasm' : 'Static';
  const env = {
    ...process.env,
    FLUENTCMS_MODES: mode.toLowerCase(),
    DOTNET_ENVIRONMENT: name === 'environment' ? 'Production' : 'Development',
    Logging__LogLevel__Default: name === 'logging' ? 'Warning' : 'Information',
  };
  const args = ['test', '--project', 'tests/FluentCMS.Components.Browser',
    '--filter-display-name', `*EveryModeBecomesWhatItsAddressNames(mode: ${mode})`,
    '--minimum-expected-tests', '1', '--output', 'Detailed'];
  const artifacts = name === 'artifacts' ? await mkdtemp(join(tmpdir(), 'gallery-')) : null;
  try {
    // A fresh configuration means a previous default-bin build cannot mask the bug.
    if (artifacts) args.push('--configuration', basename(artifacts), '--artifacts-path', artifacts);
    console.log(`[${name}] FLUENTCMS_MODES=${env.FLUENTCMS_MODES} DOTNET_ENVIRONMENT=${env.DOTNET_ENVIRONMENT} Logging__LogLevel__Default=${env.Logging__LogLevel__Default}`);
    console.log('dotnet', args.map(arg => arg.includes(' ') ? `"${arg}"` : arg).join(' '));
    const result = spawnSync('dotnet', args, { cwd: root, env, stdio: 'inherit' });
    if (result.error) throw result.error;
    if (result.status !== 0) {
      process.exitCode = result.status ?? 1;
      break;
    }
  } finally {
    if (artifacts) await rm(artifacts, { recursive: true, force: true, maxRetries: 6, retryDelay: 200 });
  }
}
