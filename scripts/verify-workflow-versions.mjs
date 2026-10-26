#!/usr/bin/env node
import { readdir, readFile } from 'node:fs/promises';

const WORKFLOWS = '.github/workflows';

const loadFolders = await readFile('loadFolders.xml', 'utf8');
const declared = [...new Set([...loadFolders.matchAll(/<v(\d+\.\d+)>/g)].map((m) => m[1]))];
if (declared.length === 0) throw new Error('loadFolders.xml declares no versions');

const problems = [];

for (const name of (await readdir(WORKFLOWS)).filter((f) => /\.ya?ml$/.test(f))) {
  const text = await readFile(`${WORKFLOWS}/${name}`, 'utf8');
  const branchRows = [...text.matchAll(/^\s+(\d+\.\d+)=\S+$/gm)].map((m) => m[1]);
  const stageSteps = [...text.matchAll(/stage-game-refs/g)].length;

  for (const version of declared) {
    if (branchRows.length > 1 && !branchRows.includes(version)) {
      problems.push(`${name}: the game-image branches block has no ${version}= row`);
    }
    if (stageSteps > 1 && !text.includes(`game-${version}`)) {
      problems.push(`${name}: no stage-game-refs step for ${version}`);
    }
  }
}

if (problems.length > 0) {
  for (const problem of problems) process.stderr.write(`::error::${problem}\n`);
  process.exit(1);
}

process.stdout.write(`every multi-version workflow covers ${declared.join(', ')}\n`);
