
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
export async function getPatchingModStatus(): Promise<getPatchingModStatusResponse> {
    let result = await fetch("/api/patching/getmodstatus");
    return await result.json();
}