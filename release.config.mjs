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
        'semantic-release-replace-plugin',
        {
            replacements: [
                {
                    files: ['RimWorks.RimLogging/BuildInfo.cs'],
                    from: 'Revision = ".*"',
                    to: 'Revision = "${nextRelease.version}"',
                    results: [{ file: 'RimWorks.RimLogging/BuildInfo.cs', hasChanged: true, numMatches: 1, numReplacements: 1 }],
                    countMatches: true,
                },
                {
                    files: ['RimWorks.RimLogging/BuildInfo.cs'],
                    from: 'BuildTime = ".*"',
                    to: () => `BuildTime = "${new Date().toISOString()}"`,
                    results: [{ file: 'RimWorks.RimLogging/BuildInfo.cs', hasChanged: true, numMatches: 1, numReplacements: 1 }],
                    countMatches: true,
                },
            ],
        },
    ],
    [
        '@semantic-release/exec',
        {
            prepareCmd:
                "dotnet pack RimWorks.RimLogging/RimWorks.RimLogging.csproj -c Release -p:Version=${nextRelease.version} -p:PackageVersion=${nextRelease.version} -p:FileVersion=${nextRelease.version.replace(/-.*/, '')}.0 -p:AssemblyVersion=${nextRelease.version.replace(/-.*/, '')}.0 -p:InformationalVersion=${nextRelease.version} -o ./nupkgs",
            publishCmd:
                "dotnet nuget push './nupkgs/*.nupkg' --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate",
        },
    ],
    [
        '@semantic-release/github',
        {
            assets: [{ path: './nupkgs/*.nupkg' }],
        },
    ],
    [
        '@semantic-release/git',
        {
            assets: ['RimWorks.RimLogging/BuildInfo.cs'],
            message: 'chore(release): ${nextRelease.version} [skip ci]\n\n${nextRelease.notes}',
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
