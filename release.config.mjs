import { readFileSync } from 'node:fs';

import { declaredVersions, releaseConfig } from '@rimworks/mod-ci';

/** @type {import('semantic-release').GlobalConfig} */
export default releaseConfig({
    solution: 'RimWorks.RimLogging.slnx',
    versions: declaredVersions(readFileSync('loadFolders.xml', 'utf8')),
    branches: ['main', { name: 'beta', prerelease: true }],
    extraRules: [
        { scope: 'worker', release: false },
        { scope: 'about', release: 'patch' },
    ],
    mods: [
        {
            name: 'RimLogging',
            workshopId: '3733484696',
            previewfile: new URL('./About/Preview.png', import.meta.url).pathname,
        },
    ],
    pack: 'Source/RimWorks.RimLogging/RimWorks.RimLogging.csproj',
    packOut: './nupkgs',
    nupkgGlob: './nupkgs/*.nupkg',
    assets: [
        { path: './nupkgs/*.nupkg' },
        { path: './dist/RimLogging-*.zip', label: 'RimLogging mod (drop into RimWorld/Mods)' },
    ],
});
