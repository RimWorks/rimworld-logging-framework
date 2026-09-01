const plugins = [
    [
        '@semantic-release/commit-analyzer',
        {
            releaseRules: [
                { scope: 'worker', release: false },
                { scope: 'about', release: 'patch' },
                { type: 'refactor', release: 'patch' },
                { type: 'style', release: 'patch' },
                { type: 'ci', release: 'patch' },
            ],
        },
    ],
    '@semantic-release/release-notes-generator',
    [
        '@semantic-release/exec',
        {
            prepareCmd:
                "dotnet pack Source/RimWorks.RimLogging/RimWorks.RimLogging.csproj -c Release -p:Version=${nextRelease.version} -p:PackageVersion=${nextRelease.version} -p:FileVersion=${nextRelease.version.replace(/-.*/, '')}.0 -p:AssemblyVersion=${nextRelease.version.replace(/-.*/, '')}.0 -p:InformationalVersion=${nextRelease.version} -o ./nupkgs && rm -rf dist && mkdir -p dist/RimLogging && cp -r About Assemblies Concord Defs Harmony Languages loadFolders.xml LICENSE README.md dist/RimLogging/ && cd dist && zip -qr RimLogging-${nextRelease.version}.zip RimLogging",
            publishCmd:
                "dotnet nuget push './nupkgs/*.nupkg' --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate",
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
    [
        'semantic-release-steam',
        {
            appId: '294100',
            branchTargets: { main: 'stable' },
            mods: [
                {
                    name: 'RimLogging',
                    path: '.',
                    previewfile: new URL('./About/Preview.png', import.meta.url).pathname,
                    workshopIds: { stable: '3733484696' },
                },
            ],
        },
    ],
];

/** @type {import('semantic-release').GlobalConfig} */
export default {
    branches: ['main', { name: 'beta', prerelease: true }],
    plugins,
};
