
interface getPatchingModStatusResponse {
    isPatched: boolean;
    isInstalled: boolean;
    canBePatched: boolean;
    version: string;
    versionCode: string;
    moddedJson?: {
        patcherName: string;
        patcherVersion: string;
        modloaderName: string;
        modloaderVersion: string;
        modifiedFiles: string[];
    };
}
/**
 * Gets the status of the patching mod
 * @returns 
 */
export async function getPatchedModdingStatus(): Promise<getPatchingModStatusResponse> {
    let result = await fetch("/api/patching/getmodstatus");
    return await result.json();
}

export enum ModLoaderType {
    QuestLoader = 0,
    Scotland2 = 1,
}

export interface IPatchOptions {
    handTracking: boolean;
    handTrackingVersion: HandtrackingType;
    externalStorage: boolean;
    debug: boolean;
    openXR: boolean,
    resignOnly: boolean,
    customPackageId: string,
    modloader: ModLoaderType,
    otherPermissions: string[],
    otherFeatures: {
        name: string,
        required: boolean,
    }[],
    splashImageBase64: string,
}


export async function setPatchingOptions(permissions: IPatchOptions): Promise<boolean> {
    let result = await fetch(`/api/patching/patchoptions`,
        {
            method: "POST",
            body: JSON.stringify(permissions),
            headers: {
                "Content-Type": "application/json"
            }
        }
    );
    return result.ok;
}

export enum HandtrackingType {
    None = 0,
    V2 = 3,
    V2_1 = 4,
}

export async function getPatchingOptions(): Promise<IPatchOptions> {
    let result = await fetch("/api/patching/patchoptions");
    return await result.json();
}


export async function patchCurrentApp(): Promise<{
    msg: string;
    success: boolean;
}> {

    let result = await fetch("/api/patching/patchapk", {
        method: "POST",
    });
    if (!result.ok) {
        throw new Error("Failed to patch app");
    }
    return await result.json();
}