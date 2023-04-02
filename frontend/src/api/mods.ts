export interface ILibrary {
    Provider: {
        FileExtension: string;
    };
    Id: string;
    Name: string;
    Author: string;
    Description: string;
    Version: {
        Major: number;
        Minor: number;
        Patch: number;
        PreRelease?: string;
        Build?: string;
        IsPreRelease: boolean;
    };
    VersionString: string;
    PackageVersion: string;
    Porter?: string,
    Robinson?: string,
    IsInstalled: boolean,
    IsLibrary: true,
    FileCopyTypes: Array<any>
}

export type IMod = ILibrary

export interface IOperation {
    type: number;
    name: string;
}

export interface ModListResponse {
    mods: Array<IMod>;
    libs: Array<ILibrary>;
    operations: Array<IOperation>;
}

// This file is used to fetch data from the backend
export async function getModsList(): Promise<ModListResponse> {
    const response = await fetch('/api/mods/mods');
    return await response.json();
};

export async function UploadMod(file: File) {
    const formData = new FormData();
    formData.append('file', file);

    const response = await fetch('/api/mods/upload', {
        method: 'POST',
        body: formData
    });
    return await response.json();
}