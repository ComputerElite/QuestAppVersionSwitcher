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
    hasCover: boolean,
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

export async function UploadMod(file: File): Promise<boolean> {
    const response = await fetch(`/api/mods/install?filename=${file.name}`, {
        method: 'POST',
        body: file
    });

    return true;
}


/**
 * Deletes a mod completely from quest
 * @param id 
 * @returns 
 */
export async function DeleteMod(id: string): Promise<boolean> {
    const response = await fetch(`/api/mods/delete?id=${id}`, {method: "POST"});
    return true;
}


/**
 * Disable or enable mods
 * @param id 
 * @param enable 
 */
export async function UpdateModState(id: string, enable: boolean) {
    await fetch(`/api/mods/${enable ? `enable` : `uninstall`}?id=${id}`, {method: "POST"});
}


export async function InstallModFromUrl(url: string): Promise<boolean> {
    const response = await fetch(`/api/mods/installfromurl`, {
        method: 'POST',
        body: url
    });

    if (response.ok) { return true; }

    return false;
}

export async function getOperations() {
    const response = await fetch('/api/mods/operations');
    return await response.json();
}

export async function getOperation(id: string) {
    const response = await fetch(`/api/mods/operations`, {
        method: 'DELETE',
        body: id
    });
    return await response.json();
}