/**
 * Returns the name of a game given its package name
 */
let knownGameNames: {
    [packageName: string]: string;
} = {
    "com.beatgames.beatsaber": "Beat Saber",

}

export function GetGameName(packageName: string) {
    return knownGameNames[packageName] ?? packageName;
}


/**
 * Sleeps for a given amount of milliseconds
 * @param ms 
 * @returns 
 */
export function Sleep(ms: number) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

export function CompareStringsAlphabetically(a: string, b: string) {
    return a.localeCompare(b);
}


// User agent string for Oculus Quest webview is hardcoded, so we can just hardcode the check for it as well
const _isOnQuest = window.navigator.userAgent == "Mozilla/5.0 (X11; Linux x86_64; Quest) AppleWebKit/537.36 (KHTML, like Gecko) OculusBrowser/23.2.0.4.49.401374055 SamsungBrowser/4.0 Chrome/104.0.5112.111 VR Safari/537.36";

export function IsOnQuest() {
    return _isOnQuest;
}

// get port from window.location
export function GetPort(): number {
    return Number.parseInt(window.location.port);
}

// Assume that the websocket port is always one higher than the http port
export function GetWSPort(): number {
    return GetPort() + 1;
}

// 
export function GetWSFullURL(route?: string): string {
    // if https, use wss
    if (window.location.protocol == "https:") {
        return `wss://${window.location.hostname}:${GetWSPort()}${route ?? ""}`;
    }

    return `ws://${window.location.hostname}:${GetWSPort()}${route ?? ""}`;
}