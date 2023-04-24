export interface ModRawEntry {
    name: string;
    description: string;
    id: string;
    version: string;
    download: string;
    source: string;
    author: string;
}
/**
 * This is the processed version of the mod entry
 */
export interface ModEntry {
    name: string;
    description: string;
    id: string;
    versions: Array<ModVersion>;
    source: string;
    author: string;
}

export interface ModVersion {
    version: string;
    download: string;
}

export function ParseModVersions (versions: Array<ModRawEntry>): Array<ModEntry> {
    let mods: Array<ModEntry> = [];
    versions.forEach((version) => {
        let mod = mods.find((mod) => mod.id === version.id);
        if (mod) {
            mod.versions.push({
                version: version.version,
                download: version.download,
            });
        } else {
            mods.push({
                name: version.name,
                description: version.description,
                id: version.id,
                author: version.author,
                versions: [
                    {
                        version: version.version,
                        download: version.download,
                    },
                ],
                source: version.source,
            });
        }
    });
    return mods;
}

