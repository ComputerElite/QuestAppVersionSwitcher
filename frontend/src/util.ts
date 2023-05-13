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

const oculusLink = "https://auth.meta.com/"
export function OpenOculusAuthLink() {
    window.location.href = oculusLink;
}

export function ValidateToken(token: string): {isValid: boolean, message: string} {
    if(token.includes("%")) {
        return {
            isValid: false,
            message: "You got your token from the wrong place. Go to the payload tab. Don't get it from the url."
        };
    }
    if(!token.startsWith("OC")) {
        return {
            isValid: false,
            message: "Tokens must start with 'OC'. Please get a new one."
        };
    }
    if(token.includes("|")) {
        return {
            isValid: false,
            message: "You seem to have entered a token of an application. Please get YOUR token. Usually this can be done by using another request in the network tab."
        };
    }
    if(token.includes(":")) {
        return {
            isValid: false,
            message: "Don't copy anything before the OC."
        };
    }
    if(/OC[0-9]{15}/g.test(token)) {
        return {
            isValid: false,
            message: "Don't change your token. This will only cause issues. Check another request for the right token."
        };
    }
    return {
        isValid: true,
        message: "Token seems to be valid.",
    };
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

// Get android version name from sdk version
export function GetAndroidVersionName(sdkVersion: number): string {
    if (sdkVersion == 0) {
        return "Unknown";
    }
    if (sdkVersion > 33) {
        return ">13";
    }
    if (sdkVersion == 33) {
        return "13";
    }
    if (sdkVersion == 32) {
        return "12L";
    }
    if (sdkVersion == 31) {
        return "12";
    }
    if (sdkVersion == 30) {
        return "11";
    }
    if (sdkVersion == 29) {
        return "10";
    }
    if (sdkVersion < 29) {
        return "<10";
    }
    return "Unknown";
}

let VersionUnderscoreRegex = /(\_\d*)/g;
export function RemoveVersionUnderscore(version: string): string {
    // Remove the _ and the number to show in the ui
    return version.replace(VersionUnderscoreRegex, "");
}