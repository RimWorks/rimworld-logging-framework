import { readFileSync } from 'node:fs';

import {
    MOD_RELEASE_RULES,
    VERSION_ARGS,
    buildVersions,
    declaredVersions,
    nugetPush,
    steamMod,
} from '@rimworks/mod-ci';

const SOLUTION = 'RimWorks.RimLogging.slnx';
const versions = declaredVersions(readFileSync('loadFolders.xml', 'utf8'));

/** @type {import('semantic-release').GlobalConfig} */
export default {
    branches: ['main', { name: 'beta', prerelease: true }],
    plugins: [
        [
            '@semantic-release/commit-analyzer',
            {
                releaseRules: [
                    { scope: 'worker', release: false },
                    { scope: 'about', release: 'patch' },
                    ...MOD_RELEASE_RULES,
                ],
            },
        ],
        '@semantic-release/release-notes-generator',
        [
            '@semantic-release/exec',
            {
                prepareCmd: [
                    `npx write-stamp ${SOLUTION}`,
                    ...buildVersions({ solution: SOLUTION, versions }),
                    `dotnet pack Source/RimWorks.RimLogging/RimWorks.RimLogging.csproj -c Release ${VERSION_ARGS} -o ./nupkgs`,
                    // after the last build, before the zip is cut
                    'npx verify-ship-list .',
                    'npx package-mod RimLogging ${nextRelease.version}',
                ].join(' && '),
                publishCmd: nugetPush('./nupkgs/*.nupkg'),
            },
        ],
        [
            '@semantic-release/github',
            {
                assets: [
                    { path: './nupkgs/*.nupkg' },
                    { path: './dist/RimLogging-*.zip', label: 'RimLogging mod (drop into RimWorld/Mods)' },
                ],
            },
        ],
        ...steamMod({
            name: 'RimLogging',
            workshopId: '3733484696',
            previewfile: new URL('./About/Preview.png', import.meta.url).pathname,
        }),
    ],
};
