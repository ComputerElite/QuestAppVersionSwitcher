interface getAppInfoResponse {
    version: string;
    browserIPs: string[];
}
/**
 * Gets the app info like version, name, etc.
 * @param params 
 */
export async function getAppInfo() {
    let result = await fetch("/api/questappversionswitcher/about");
    return await result.json();
}