import { InternalPatchingOptions } from "../store";

interface getPatchingModStatusResponse {
    isPatched: boolean;
    isInstalled: boolean;
    canBePatched: boolean;
    version: string;
    versionCode: string;
    moddedJson: {
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

interface PatchingRequestPermissions {
    handTracking: boolean;
    handTrackingVersion: HandtrackingTypes;
    externalStorage: boolean;
    debug: boolean;
    otherPermissions: string[];
}


export async function setPatchingOptions(permissions: InternalPatchingOptions): Promise<boolean> {
    let apiFriendlyPermissions: PatchingRequestPermissions = {
        handTracking: permissions.handtracking != HandtrackingTypes.None,
        handTrackingVersion: permissions.handtracking,
        externalStorage: permissions.addExternalStorage,
        debug: permissions.addDebug,
        otherPermissions: permissions.additionalPermissions,
    }
    
    let result = await fetch(`/api/patching/patchoptions`,
        {
            method: "POST",
            body: JSON.stringify(apiFriendlyPermissions),
            headers: {
                "Content-Type": "application/json"
            }
        }
    );

    return result.ok;
}   

export enum HandtrackingTypes {
    None = 0,
    V1 = 1,
    V1HighFrequency = 2,
    V2 = 3,
}

export async function getPatchingOptions(): Promise<PatchingRequestPermissions> {
    let result = await fetch("/api/patching/patchoptions");
    return await result.json();
}


export async function patchCurrentApp(): Promise<boolean>  {
    
    let result = await fetch("/api/patching/patchapk");
    if (!result.ok) {
        throw new Error("Failed to patch app");
    }
    return await result.json();
}