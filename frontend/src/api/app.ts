interface getAppInfoResponse {
    version: string;
    browserIPs: string[];
}
/**
 * Gets the app info like version, name, etc.
 * @param params 
 */
export async function getAppInfo(): Promise<getAppInfoResponse> {
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

export async function setConfig(config: getConfigResponse) {
    let result = await fetch("/api/questappversionswitcher/config", {
        method: "POST",
        body: JSON.stringify(config)
    });
    return await result.json();
}

/**
 * Close the app
 * @returns 
 */
export async function exitApp() {
    let result = await fetch("/api/questappversionswitcher/kill");
    return await result.text();
}

/**
 * Close the app
 * @returns 
 */
export async function startGame() {
    let result = await fetch("/api/questappversionswitcher/kill");
    return await result.text();
}

/**
 * Change the port of WebUI
 * @param port 
 * @returns 
 */
export async function changePort(port: number) {
    let result = await fetch(`/api/questappversionswitcher/changeport?body=${port}`);

    if (result.status == 200) {
        return true;
    }
    if (result.status == 400) {
        throw new Error(await result.text()); 
    }
    throw new Error("Unknown error");
}

export interface IAppListItem {
    AppName: string;
    PackageName: string;
}

/**
 * Gets the list of apps installed on the quest
 * @param includeBackups include backups in the list of apps (default: true)
 */
export async function getAppList(includeBackups: boolean = true): Promise<IAppListItem[]> {
    let result = await fetch(
        includeBackups? "/api/android/installedappsandbackups" : "/api/android/installedapps"
    );
    return await result.json();
}


/**
 * Sets the oculus token and encrypts it with the given password on the server
 * @param token 
 * @param password 
 * @returns true if the token is valid
 * @throws Error if the token is invalid
 */
export async function setOculusToken(token: string, password: string) {
    let result = await fetch("/api/token", {
        method: "POST",
        body: JSON.stringify({
            token: token,
            password: encodeURIComponent(password)
        })
    });

    // If the token is invalid, the server will return 400 and the error message
    if (result.status == 400) {
        let text = await result.text();
        throw new Error(text);
    }
}

/**
 * Change current app to the given app id
 * @param appId
 * @returns true if the request was successful
 */
export async function changeManagedApp(appId: string): Promise<boolean> {
    let result = await fetch(`/api/questappversionswitcher/changeapp?body=${appId}`);

    if (result.status != 200) {
        return false;
    } else {
        return true
    }
}

/**
 * Uploads the logs to the server and returns the ID
 * @param password
 * @returns logs
 */
export async function collectLogs(password: string): Promise<string> {
    let result = await fetch("/api/questappversionswitcher/uploadlogs", {
        method: "POST",
        body: encodeURIComponent(password)
    });


    if (result.status == 200) {
        // Return password
        return await result.text();
    }
    if (result.status == 403) {
        throw new Error(await result.text());
    }
    throw new Error("Unknown error");
}

/**
 * Uploads the logs to oculusdb server and returns the ID 
 */
export async function uploadLogsToOculusDB(logs: string): Promise<string> {
    let result = await fetch("https://oculusdb.rui2015.me/api/v1/qavsreport", {
        method: "POST",
        body: logs
    });
    if (result.status == 200) {
        return await result.text();
    }
    throw new Error("Unknown error");
}

/**
 * Check if the user is logged in
 * @returns true if the user is logged in
 */
export async function getLoggedInStatus(): Promise<boolean> {
    let result = await fetch("/api/questappversionswitcher/loggedinstatus");
    return await result.text() == "2";
}