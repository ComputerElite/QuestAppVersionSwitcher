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

interface getConfigResponse {
    currentApp: string;
    serverPort: number;
    token: string;
    loginStep: number;
    password: string;
}

export async function getConfig(): Promise<getConfigResponse> {
    let result = await fetch("/api/questappversionswitcher/config");
    return await result.json();
}